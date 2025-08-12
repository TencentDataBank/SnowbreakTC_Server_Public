using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class SignExt
{
    public static SignSystem Sign(this PlayerInstance player, GameSession session) =>
        new(player, session.Server.Resources);
}

[Route("Seven_day_getIndexAward")]
public sealed class SevenDayIndexAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var activityId = root.Find("activity_id", "activityid", "id").Uint() ?? 1;
        var dayId = root.Find("day_id", "dayid", "index").Uint() ?? 1;
        var rewards = player.Sign(session).Sign(activityId, dayId);
        player.Save();
        return rewards == null
            ? Task.FromResult(CallGsReply.Error("tip.already_signed"))
            : Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new
            {
                day = dayId,
                rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
            })));
    }
}

[Route("Seven_day_getAllRewardCanGet")]
public sealed class SevenDayAllCanGet : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var activityId = root.Find("activity_id", "activityid", "id").Uint() ?? 1;
        var (signed, total) = player.Sign(session).Progress(activityId);
        var arg = JsonSerializer.Serialize(new { activity_id = activityId, signed, total, can_get = total > signed });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Seven_day_getAchievementAward")]
public sealed class SevenDayAchievement : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}
