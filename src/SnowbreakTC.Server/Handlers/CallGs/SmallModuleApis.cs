using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class SmallModuleExt
{
    public static CallGsReply GrantJson(this PlayerInstance player, ResourceManager resources, uint suppliesId, uint count)
    {
        var rewards = player.Quest(resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = suppliesId, Count = count },
        ]);
        player.Save();
        return new CallGsReply(JsonSerializer.Serialize(new
        {
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        }));
    }

    public static CallGsReply FlagReply(this PlayerInstance player, string key, bool defaultOpen = true) =>
        new(JsonSerializer.Serialize(new { open = player.GetExtra().ContainsKey($"{key}_open") || defaultOpen }));

    public static void Mark(this PlayerInstance player, string key, long value = 1)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [key] = value });
        player.Save();
    }
}

// ---------- StarWish (13) ----------

[Route("StarWish_CheckOpenAct")]
public sealed class StarWishCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("starwish"));
}

[Route("StarWish_SetSubIDCompleted")]
[Route("StarWish_SetStoryQuestCompleted")]
[Route("StarWish_CleanSubIDCompleted")]
[Route("StarWish_CompleteShareAchievement")]
[Route("StarWish_AutoCompleteStoryQuest")]
public sealed class StarWishMarkers : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var subId = root.Find("sub_id", "subid", "quest_id", "id").Ulong();
        if (subId is { } s)
            player.Mark($"sw_{s}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("StarWish_GetScoreAward")]
public sealed class StarWishScoreAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var score = player.GetAttr(180, 2);
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = 100 + score },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            tbAwardList = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("StarWish_GetAllCompleteAward")]
[Route("StarWish_GetStoryPageAward")]
[Route("StarWish_GetStoryQuestAward")]
[Route("StarWish_GetRepeatQuestAward")]
[Route("StarWish_GetRepeatQuestExtraAward")]
public sealed class StarWishAwards : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 100));
}

[Route("StarWish_LotteryJigsaw")]
public sealed class StarWishLottery : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var piece = Random.Shared.Next(1, 9);
        session.Player!.CaptureSet("sw_piece", piece);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { piece })));
    }
}

// ---------- FarmWar (10) ----------

[Route("FarmWarLogic_ActiveGrowNode")]
[Route("FarmWarLogic_ActiveLimitGrowNode")]
public sealed class FarmWarActive : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var node = root.Find("node_id", "id").Ulong();
        if (node is { } n)
            player.Mark($"fw_node_{n}", (long)PlayerInstance.Now());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("FarmWarLogic_ResetGrow")]
[Route("FarmWarLogic_ResetGrowNode")]
[Route("FarmWarLogic_ResetLimitGrow")]
[Route("FarmWarLogic_ResetLimitGrowNode")]
[Route("FarmWarLogic_SwitchLimitGrowNode")]
public sealed class FarmWarReset : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var node = root.Find("node_id", "id").Ulong();
        if (node is { } n)
        {
            var extras = player.GetExtra();
            extras.Remove($"fw_node_{n}");
            player.SetExtra(extras);
            player.Save();
        }
        return Task.FromResult(new CallGsReply());
    }
}

[Route("FarmWarLogic_UpdateFormation")]
public sealed class FarmWarFormation : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var formation = root.Find("formation", "lineup", "id").Ulong();
        if (formation is { } f)
            player.Mark("fw_formation", (long)f);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("FarmWarLogic_GetQuestAward")]
public sealed class FarmWarQuestAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 200));
}

// ---------- RichMan / RichManLove (14) ----------

[Route("RichmanLogic_EnterMap")]
[Route("RichManLoveLogic_EnterMap")]
[Route("RichManLoveLogic_EnterMapLog")]
public sealed class RichManEnter : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var node = (long)Random.Shared.Next(1, 40);
        player.Mark("richman_node", node);
        var arg = JsonSerializer.Serialize(new { node });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("RichmanLogic_MapSettlement")]
[Route("RichManLoveLogic_MapSettlement")]
[Route("RichManLoveLogic_SettlementUnlockEnd")]
public sealed class RichManSettle : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var node = player.GetExtra().GetValueOrDefault("richman_node", 0);
        return Task.FromResult(player.GrantJson(session.Server.Resources, 24, (uint)(100 + node * 10)));
    }
}

[Route("RichManLoveLogic_ReceiveDice")]
public sealed class RichManReceiveDice : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var key = $"dice_{PlayerInstance.Now() / 86400}";
        if (player.GetExtra().ContainsKey(key))
            return Task.FromResult(CallGsReply.Error("tip.already_signed"));
        player.Mark(key);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { dice = 2 })));
    }
}

[Route("RichManLoveLogic_UseDice")]
public sealed class RichManUseDice : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var node = player.GetExtra().GetValueOrDefault("richman_node", 0) + Random.Shared.Next(1, 7);
        player.Mark("richman_node", node);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { dice = node % 6 + 1, node })));
    }
}

[Route("RichManLoveLogic_LikeEnd")]
[Route("RichmanLogic_VoteGirl")]
public sealed class RichManVote : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var girl = root.Find("girl_id", "id").Ulong();
        if (girl is { } g)
            player.Mark($"vote_{g}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("RichmanLogic_CheckOpenAct")]
[Route("RichManLoveLogic_CheckOpenAct")]
public sealed class RichManCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("richman"));
}

[Route("RichmanLogic_ReceiveVoteveReward")]
[Route("RichManLoveLogic_QuickGetReward")]
public sealed class RichManReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 150));
}

// ---------- Achievement (5) ----------

[Route("Achievement_GetReward")]
[Route("AchievementDLC_GetReward")]
public sealed class AchievementGetReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var questId = root.Find("quest_id", "id").Ulong();
        if (questId is { } q)
            player.Mark($"ach_{q}", (long)PlayerInstance.Now());
        return Task.FromResult(player.GrantJson(session.Server.Resources, 24, 120));
    }
}

[Route("Achievement_QuickGetReward")]
[Route("AchievementDLC_QuickGetReward")]
public sealed class AchievementQuick : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 300));
}

[Route("Achievement_OnAddSomeOne")]
public sealed class AchievementOnAdd : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var questId = root.Find("quest_id", "id").Ulong();
        var progress = root.Find("progress", "count").Ulong();
        if (questId is { } q && progress is { } p)
            player.Mark($"achp_{q}", (long)p);
        return Task.FromResult(new CallGsReply());
    }
}

// ---------- Misc small modules ----------

[Route("Activity_Flag")]
[Route("Activity_MarkTip")]
[Route("ActivityFace_Update")]
[Route("Activity_BirthdayMail_Gain")]
public sealed class ActivityFlags : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var id = root.Find("id", "activity_id").Ulong();
        if (id is { } i)
            player.Mark($"act_flag_{i}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("Activity_GetRoleFestivalLevelReward")]
public sealed class RoleFestivalReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 180));
}

[Route("Activity_SetRoleFestivalMessageBeRead")]
public sealed class RoleFestivalRead : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        session.Player!.Mark("role_festival_read");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("Activity_QuickQuestAward")]
public sealed class ActivityQuickQuest : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 250));
}

[Route("WorldBossLogic_CheckOpen")]
public sealed class WorldBossCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("worldboss"));
}

[Route("WorldBossLogic_UpdateProgressTask")]
public sealed class WorldBossProgress : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var taskId = root.Find("task_id", "id").Ulong();
        var progress = root.Find("progress", "count").Ulong();
        if (taskId is { } t && progress is { } p)
            player.Mark($"wb_task_{t}", (long)p);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("BossPvpLogic_GetOpenID")]
public sealed class BossPvpOpenId : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { open_id = session.Player!.Data.Pid })));
}

[Route("BossPvpLogic_LevelMopup")]
public sealed class BossPvpMopup : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 200));
}

[Route("Daily_GetActivityInfo")]
public sealed class DailyActivityInfo : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var arg = JsonSerializer.Serialize(new
        {
            activities = new[] { new { id = 1, type = 1, open = true } },
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Daily_SetSelectSuit")]
public sealed class DailySelectSuit : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var suit = root.Find("suit_id", "id").Ulong();
        if (suit is { } s)
            player.Mark($"daily_suit_{s}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("DLCLogic_CheckOpenAct")]
public sealed class DlcCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("dlc"));
}

[Route("DLC_Chapter_Mopup")]
public sealed class DlcMopup : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 220));
}

[Route("DLC_Chapter_STAR_AWARD")]
public sealed class DlcStarAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 320));
}

[Route("DreamCard_CheckOpen")]
public sealed class DreamCardCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("dreamcard"));
}

[Route("DreamCard_UpdateData")]
public sealed class DreamCardUpdate : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var key = root.Find("key", "field").Str();
        var value = root.Find("value", "val").Ulong();
        if (key != null && value is { } v)
            player.Mark($"dc_{key}", (long)v);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("GirlRush_GetAward")]
public sealed class GirlRushAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 180));
}

[Route("GirlRush_SaveData")]
public sealed class GirlRushSave : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var data = root.Find("data", "score").Ulong();
        if (data is { } d)
            player.Mark("girlrush_data", (long)d);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("NitaMonolopy_GetTreeAward")]
[Route("NitaMonolopy_GetAllTreeAward")]
public sealed class NitaTreeAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 260));
}

[Route("NitaMonolopy_DumpGame")]
public sealed class NitaDump : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        session.Player!.Mark("nita_dump", (long)PlayerInstance.Now());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("VigourSupply_CheckRefresh")]
public sealed class VigourSupplyCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var day = (long)PlayerInstance.Now() / 86400;
        var refreshed = session.Player!.GetExtra().ContainsKey($"vs_{day}");
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { refreshed, can_receive = !refreshed })));
    }
}

[Route("VigourSupply_ReceiveVigour")]
public sealed class VigourSupplyReceive : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var day = (long)PlayerInstance.Now() / 86400;
        if (player.GetExtra().ContainsKey($"vs_{day}"))
            return Task.FromResult(CallGsReply.Error("tip.already_signed"));
        player.Mark($"vs_{day}");
        player.Data.Vigor = Math.Min(player.Data.Vigor + 60, session.Server.Config.Game.VigorLimit);
        player.Save();
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { vigor = player.Data.Vigor })));
    }
}

[Route("PlayerCharge_GetAward")]
public sealed class PlayerChargeAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var id = root.Find("id", "charge_id").Ulong();
        if (id is { } i)
            player.Mark($"charge_{i}", (long)PlayerInstance.Now());
        return Task.FromResult(player.GrantJson(session.Server.Resources, 5, 300));
    }
}

[Route("PlayerCharge_Switch")]
public sealed class PlayerChargeSwitch : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        session.Player!.Mark("charge_switch", (long)PlayerInstance.Now());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("OnlineAssaultOps_ChangeFormation")]
public sealed class AssaultFormation : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var formation = root.Find("formation", "lineup", "id").Ulong();
        if (formation is { } f)
            player.Mark("assault_formation", (long)f);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("OnlineAssaultOps_Equip")]
[Route("OnlineAssaultOps_SystemEquip")]
public sealed class AssaultEquip : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var equipId = root.Find("equip_id", "id").Ulong();
        if (equipId is { } e)
            player.Mark($"assault_equip_{e}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("OnlineAssaultOps_QuestAward")]
public sealed class AssaultQuestAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 240));
}

[Route("OnlineAssaultOpsBattlePass_ClientRefresh")]
public sealed class AssaultBattlePass : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var arg = JsonSerializer.Serialize(new { level = 1, exp = 0 });
        return Task.FromResult(new CallGsReply(arg));
    }
}
