using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

// ---------- DefendLogic (4) ----------

[Route("DefendLogic_CheckOpenAct")]
public sealed class DefendCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("defend"));
}

[Route("DefendLogic_SelectDiff")]
public sealed class DefendSelectDiff : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var diff = root.Find("diff", "difficulty", "id").Uint() ?? 1;
        player.Mark("defend_diff", diff);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("DefendLogic_GetLevelAward")]
[Route("DefendLogic_GetReward")]
public sealed class DefendReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 200));
}

// ---------- stragglers ----------

[Route("VirCapture_CheckOpenAct")]
public sealed class VirCaptureCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("vircapture"));
}

[Route("VirCaptureLevel_SaveFightData")]
public sealed class VirCaptureSaveFight : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var damage = root.Find("damage", "score").Ulong();
        if (damage is { } d)
            player.CaptureSet("fight_damage", (long)d);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("SeasonBoss_CheckOpen")]
public sealed class SeasonBossCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("seasonboss"));
}

[Route("RoleCard_SetSupporterTeamIndex")]
public sealed class RoleCardTeamIndex : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var index = root.Find("index", "team_index").Uint() ?? 1;
        player.Mark("supporter_team_index", index);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("PlayerReturn_GetReward")]
public sealed class PlayerReturnReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 500));
}

[Route("BattlePassLogic_ClientRefresh")]
public sealed class BattlePassRefresh : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var level = player.GetExtra().GetValueOrDefault("bp_level", 1);
        var arg = JsonSerializer.Serialize(new { level, exp = 0, paid = false });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("BossPvpLogic_LevelSettlement")]
public sealed class BossPvpSettlement : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var score = root.Find("score", "point").Uint() ?? 0;
        player.AddBossDamage("pvp", score);
        return Task.FromResult(player.GrantJson(session.Server.Resources, 24, score / 10));
    }
}

[Route("Chess_LevelFail")]
public sealed class ChessLevelFail : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        session.Player!.Mark("chess_last_fail", (long)PlayerInstance.Now());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("ClimbTowerLogic_CheckCycleLevel")]
public sealed class ClimbTowerCycle : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var arg = JsonSerializer.Serialize(new { cycle_open = true, current = 1, max = 5 });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("ClimbTower_Award")]
public sealed class ClimbTowerAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 180));
}

[Route("DefendFightSavePlaceholder")]
public sealed class DefendFightPlaceholder : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}

[Route("FishingServer_ConvertFood")]
public sealed class FishingConvertFood : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var foodId = root.Find("food_id", "id").Uint() ?? 1;
        var count = root.Find("count", "num").Uint() ?? 1;
        if (!player.Inventory.Consume(foodId * 1000UL, count))
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var item = player.Inventory.Add(ItemGenres.Supplies * 1000 + 3, count * 2);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            items = new[] { new { id = item.Id, template = item.Template, count = item.Count } },
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("GachaTry_LevelFail")]
public sealed class GachaTryFail : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        session.Player!.Mark("gachatry_fail", (long)PlayerInstance.Now());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("GacheTry_GetAlreadyPassedAward")]
public sealed class GachaTryPassedAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 160));
}

[Route("IBLogic_GoodsRedDot")]
public sealed class IbGoodsRedDot : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var goodsId = root.Find("goods_id", "id").Uint() ?? 0;
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { goods_id = goodsId, red_dot = false })));
    }
}

[Route("OnlineActivity_Request")]
public sealed class OnlineActivityRequest : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var arg = JsonSerializer.Serialize(new { activities = Array.Empty<object>() });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("OnlineTraining_LevelFail")]
public sealed class OnlineTrainingFail : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        session.Player!.Mark("otraining_fail", (long)PlayerInstance.Now());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("Online_FirstPop")]
public sealed class OnlineFirstPop : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var shown = session.Player!.GetExtra().ContainsKey("online_first_pop");
        session.Player!.Mark("online_first_pop");
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { show = !shown })));
    }
}

[Route("FarmWarLogic_CheckActOpen")]
public sealed class FarmWarCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("farmwar"));
}
