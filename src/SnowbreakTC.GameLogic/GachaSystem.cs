using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed class GachaSystem
{
    public const string PityKey = "gacha_pity";
    public const uint PityLimit = 80;
    public const uint LaunchCostGold = 180;

    private readonly PlayerInstance _player;
    private readonly ResourceManager _resources;
    private readonly Random _rng = new();

    public GachaSystem(PlayerInstance player, ResourceManager resources)
    {
        _player = player;
        _resources = resources;
    }

    public List<GachaPoolRec>? Pools => GachaTable.Load(_resources);

    public LaunchResult Launch(uint poolId, uint times)
    {
        if (times is not (1 or 10))
            return LaunchResult.Fail("error.BadParam");
        var pools = Pools;
        if (pools is { Count: > 0 } && pools.All(p => p.Id != poolId))
            return LaunchResult.Fail("error.BadParam");
        if (!_player.ConsumeCurrency(AttrIds.Currency.Gold, LaunchCostGold * times))
            return LaunchResult.Fail("error.NoMoney");

        var pity = _player.GetExtra().GetValueOrDefault(PityKey);
        var rewards = new List<RewardItem>((int)times);
        for (var i = 0; i < times; i++)
        {
            pity++;
            var reward = Roll(pity >= PityLimit);
            if (pity >= PityLimit)
                pity = 0;
            rewards.Add(reward);
        }
        var extras = new Dictionary<string, long>(_player.GetExtra()) { [PityKey] = pity };
        _player.SetExtra(extras);
        return new LaunchResult(rewards);
    }

    private RewardItem Roll(bool guaranteed)
    {
        var weights = GachaTable.Probabilities(_resources);
        var rarity = PickRarity(weights);
        if (guaranteed && rarity < 5)
            rarity = 5;
        var detail = (uint)_rng.Next(1, 21);
        return new RewardItem
        {
            G = ItemGenres.Card,
            D = detail,
            P = rarity,
            L = 1,
            Count = 1,
        };
    }

    private uint PickRarity(List<ProbabilityRec>? weights)
    {
        if (weights is not { Count: > 0 })
            return (uint)_rng.Next(3, 6);
        var rec = weights[_rng.Next(weights.Count)];
        var total = rec.Rarity3 + rec.Rarity4 + rec.Rarity5 + rec.Rarity6;
        if (total == 0)
            return 3;
        var roll = (uint)_rng.Next(1, (int)total + 1);
        if (roll <= rec.Rarity3) return 3;
        roll -= rec.Rarity3;
        if (roll <= rec.Rarity4) return 4;
        roll -= rec.Rarity4;
        if (roll <= rec.Rarity5) return 5;
        return 6;
    }

    public List<ItemInfo> Deliver(IEnumerable<RewardItem> rewards)
    {
        var granted = new List<ItemInfo>();
        foreach (var reward in rewards)
        {
            if (reward.G == ItemGenres.Card)
                granted.Add(_player.Characters.AddCharacter(reward.D, reward.P));
            else if (reward.G == ItemGenres.Weapon)
                granted.Add(_player.Characters.AddWeapon(reward.D, reward.P)!);
            else
                granted.Add(_player.Inventory.Add(reward.G * 1000 + reward.D, reward.Count));
        }
        return granted;
    }

    public sealed record LaunchResult(List<RewardItem>? Rewards, string? Error = null)
    {
        public bool Ok => Error == null;
        public static LaunchResult Fail(string error) => new(null, error);
    }
}

public static class PlayerInstanceGachaExt
{
    private const string ExtraKey = "extras";

    public static Dictionary<string, long> GetExtra(this PlayerInstance player) =>
        player.LoadDict(ExtraKey);

    public static void SetExtra(this PlayerInstance player, Dictionary<string, long> value) =>
        player.SaveDict(ExtraKey, value);
}
