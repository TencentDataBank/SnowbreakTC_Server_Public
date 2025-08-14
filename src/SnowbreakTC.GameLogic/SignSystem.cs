using System.Text.Json.Serialization;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed record SignDayRec
{
    [JsonPropertyName("ActivityId")] public uint ActivityId { get; init; }
    [JsonPropertyName("DayId")] public uint DayId { get; init; }
    [JsonPropertyName("SignReward")] public List<List<uint>> SignReward { get; init; } = [];
}

public sealed class SignSystem
{
    public const string StateKey = "sign_state";

    private readonly PlayerInstance _player;
    private readonly ResourceManager _resources;

    public SignSystem(PlayerInstance player, ResourceManager resources)
    {
        _player = player;
        _resources = resources;
    }

    private List<SignDayRec>? Days => _resources.Table<SignDayRec>("activity/sign/signday.json");

    private string DayKey(uint activityId, uint dayId) => $"a{activityId}d{dayId}";

    public bool IsSigned(uint activityId, uint dayId) => _player.LoadDict(StateKey).ContainsKey(DayKey(activityId, dayId));

    public List<RewardItem>? Sign(uint activityId, uint dayId)
    {
        if (IsSigned(activityId, dayId))
            return null;
        var rec = Days?.FirstOrDefault(d => d.ActivityId == activityId && d.DayId == dayId);
        if (rec == null)
            return null;
        var extras = _player.GetExtra();
        extras[DayKey(activityId, dayId)] = (long)PlayerInstance.Now();
        _player.SetExtra(extras);
        return _player.Quest(_resources).DeliverRewards(
            rec.SignReward.Select(e => new RewardItem
            {
                G = e.Count > 0 ? e[0] : 0,
                D = e.Count > 1 ? e[1] : 0,
                P = e.Count > 2 ? e[2] : 0,
                L = e.Count > 3 ? e[3] : 0,
                Count = e.Count > 4 ? e[4] : 1,
            }));
    }

    public (uint SignedCount, uint TotalDays) Progress(uint activityId)
    {
        var days = Days?.Where(d => d.ActivityId == activityId).ToList() ?? [];
        var signed = days.Count(d => IsSigned(activityId, d.DayId));
        return ((uint)signed, (uint)days.Count);
    }
}
