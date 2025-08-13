using System.Text.Json;
using System.Text.Json.Serialization;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed record ChapterMainRec
{
    [JsonPropertyName("ID")] public uint Id { get; init; }
    [JsonPropertyName("Type")] public uint Type { get; init; }
    [JsonPropertyName("Condition")] public Dictionary<string, uint>? Condition { get; init; }
    [JsonPropertyName("Level")] public List<uint> Levels { get; init; } = [];
    [JsonPropertyName("StarAward")] public Dictionary<string, List<List<uint>>>? StarAward { get; init; }
}

public static class ChapterTable
{
    public static List<ChapterMainRec>? Load(ResourceManager resources) =>
        resources.Table<ChapterMainRec>("chapter/chapter_main.json");
}

public sealed class QuestSystem
{
    public const uint LevelStateGroup = 21;
    public const uint LevelPassGroup = 22;
    public const uint ChapterStarAwardGroup = 20;
    public const uint StarMaskMax = 7;

    private readonly PlayerInstance _player;
    private readonly ResourceManager _resources;

    public QuestSystem(PlayerInstance player, ResourceManager resources)
    {
        _player = player;
        _resources = resources;
    }

    public bool IsLevelPassed(uint levelId) => _player.GetAttr(LevelPassGroup, levelId) != 0;

    public bool IsChapterUnlocked(ChapterMainRec chapter)
    {
        if (chapter.Condition is not { Count: > 0 } conditions)
            return true;
        return conditions.Values.All(IsLevelPassed);
    }

    public uint ChapterStars(ChapterMainRec chapter) =>
        (uint)chapter.Levels.Where(IsLevelPassed).Sum(l => (long)(_player.GetAttr(LevelStateGroup, l) & StarMaskMax));

    public SettlementResult SettleLevel(uint levelId, uint starMask, uint? dropId)
    {
        starMask = Math.Clamp(starMask, 0, StarMaskMax);
        var firstPass = !IsLevelPassed(levelId);
        _player.SetAttr(LevelPassGroup, levelId, (uint)PlayerInstance.Now());
        _player.SetAttr(LevelStateGroup, levelId, starMask);

        var rewards = new List<RewardItem>();
        if (dropId is { } id && DropTable.Roll(_resources, id, Random.Shared) is { } dropped)
            rewards.AddRange(DeliverRewards(dropped));
        if (firstPass && starMask > 0 && ChapterTable.Load(_resources) is { Count: > 0 })
            rewards.AddRange(DeliverRewards(FirstPassBonus(levelId)));
        return new SettlementResult(rewards, firstPass);
    }

    public (List<RewardItem> Rewards, uint Threshold)? ClaimChapterStarAward(uint chapterId)
    {
        var chapter = ChapterTable.Load(_resources)?.FirstOrDefault(c => c.Id == chapterId);
        if (chapter?.StarAward == null || chapter.StarAward.Count == 0)
            return null;
        var stars = ChapterStars(chapter);
        foreach (var (thresholdText, entries) in chapter.StarAward)
        {
            if (!uint.TryParse(thresholdText, out var threshold) || stars < threshold)
                continue;
            var slot = ChapterAwardSlot(chapterId, threshold);
            if ((_player.GetAttr(ChapterStarAwardGroup, slot) & 1) != 0)
                continue;
            _player.SetAttr(ChapterStarAwardGroup, slot, 1);
            return (DeliverRewards(entries.Select(ParseReward)), threshold);
        }
        return null;
    }

    private uint ChapterAwardSlot(uint chapterId, uint threshold) => chapterId * 32 + threshold;

    private List<RewardItem> FirstPassBonus(uint levelId) =>
        [
            new RewardItem { G = ItemGenres.Useable, D = levelId % 100, Count = levelId / 100 % 10 + 1 },
        ];

    private static RewardItem ParseReward(IEnumerable<uint> e)
    {
        var v = e.ToArray();
        return new RewardItem
        {
            G = v.Length > 0 ? v[0] : 0,
            D = v.Length > 1 ? v[1] : 0,
            P = v.Length > 2 ? v[2] : 0,
            L = v.Length > 3 ? v[3] : 0,
            Count = v.Length > 4 ? v[4] : 1,
        };
    }

    public List<RewardItem> DeliverRewards(IEnumerable<RewardItem> rewards)
    {
        var granted = new List<RewardItem>();
        foreach (var reward in rewards)
        {
            ItemInfo item;
            switch (reward.G)
            {
                case ItemGenres.Card:
                    item = _player.Characters.AddCharacter(reward.D, Math.Max(reward.P, 1));
                    break;
                case ItemGenres.Weapon:
                    item = _player.Characters.AddWeapon(reward.D, Math.Max(reward.P, 1))!;
                    break;
                default:
                    item = _player.Inventory.Add(reward.G * 1000UL + reward.D, reward.Count);
                    break;
            }
            granted.Add(new RewardItem { G = reward.G, D = reward.D, Count = item.Count });
        }
        return granted;
    }

    public sealed record SettlementResult(List<RewardItem> Rewards, bool FirstPass);
}
