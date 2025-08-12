using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

[Route("Chapter_EnterLevel")]
[Route("VirCaptureTrial_EnterLevel")]
[Route("TowerLevel_EnterLevel")]
[Route("TowerEventChapter_EnterLevel")]
[Route("Daily_EnterLevel")]
[Route("Role_EnterLevel")]
[Route("DLC_Chapter_EnterLevel")]
[Route("ClimbTower_EnterLevel")]
[Route("GachaTry_EnterLevel")]
[Route("DreamCard_EnterLevel")]
[Route("DonoDuelRogue_EnterLevel")]
[Route("GirlRush_EnterLevel")]
[Route("OnlineTraining_EnterLevel")]
[Route("FunTimed_EnterLevel")]
[Route("Rogue3D_EnterLevel")]
[Route("Rogue3D_EnterSeasonLevel")]
[Route("VirCaptureLevel_EnterLevel")]
[Route("VirCaptureTower_EnterLevel")]
[Route("BossPvpLogic_EnterLevel")]
[Route("SeasonBoss_EnterLevel")]
[Route("WorldBossLogic_EnterLevel")]
[Route("FarmWarLogic_EnterLevel")]
[Route("AlienDefense_EnterLevel")]
[Route("DefendLogic_EnterLevel")]
[Route("BagRogue_EnterLevel")]
[Route("Chess_EnterLevel")]
[Route("DonoDuelArena_EnterLevel")]
public sealed class LevelEnter : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var levelId = root.Find("level_id", "levelid", "id").Uint();
        if (levelId is not { } id)
            return Task.FromResult(new CallGsReply());
        var chapter = ChapterTable.Load(session.Server.Resources)?.FirstOrDefault(c => c.Levels.Contains(id));
        if (chapter != null && !player.Quest(session.Server.Resources).IsChapterUnlocked(chapter))
            return Task.FromResult(CallGsReply.Error("tip.level_locked"));
        return Task.FromResult(new CallGsReply());
    }
}

[Route("Chapter_DealLevelSettlement")]
[Route("VirCaptureTrial_LevelSettlement")]
[Route("VirCaptureTrial_LevelFail")]
[Route("VirCaptureTower_LevelFail")]
[Route("VirCaptureTower_LevelSettlement")]
[Route("SeasonBoss_LevelSettlement")]
[Route("DonoDuelRogue_MiniGameSettlement")]
[Route("DreamCard_LevelSettlement")]
[Route("NitaMonolopy_GameSettlement")]
[Route("TowerLevel_LevelSettlement")]
[Route("TowerEventChapter_LevelSettlement")]
[Route("DLC_Chapter_LevelFail")]
[Route("GachaTry_LevelSettlement")]
[Route("GirlRush_LevelFail")]
[Route("FunTimed_LevelExit")]
[Route("BossPvpLogic_LevelFail")]
[Route("SeasonBoss_LevelFail")]
[Route("AlienDefense_LevelFail")]
[Route("FarmWarLogic_LevelFail")]
[Route("DonoDuelArena_LevelFail")]
public sealed class LevelSettlement : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var levelId = root.Find("level_id", "levelid", "id").Uint() ?? 0;
        var starMask = root.Find("star", "stars", "score").Uint() ?? 3;
        var dropId = root.Find("drop_id", "dropid").Uint();
        var result = player.Quest(session.Server.Resources).SettleLevel(levelId, starMask, dropId);
        player.Data.Exp += 100;
        player.Save();
        var arg = System.Text.Json.JsonSerializer.Serialize(new
        {
            rewards = result.Rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
            first_pass = result.FirstPass,
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Chapter_GetStarAward")]
[Route("VirCaptureTower_GetStarAward")]
public sealed class ChapterStarAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var chapterId = root.Find("chapter_id", "chapterid", "id").Uint();
        if (chapterId is not { } id)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var claim = player.Quest(session.Server.Resources).ClaimChapterStarAward(id);
        player.Save();
        return claim == null
            ? Task.FromResult(CallGsReply.Error("tip.star_award_not_available"))
            : Task.FromResult(new CallGsReply(System.Text.Json.JsonSerializer.Serialize(new
            {
                threshold = claim.Value.Threshold,
                rewards = claim.Value.Rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
            })));
    }
}

[Route("Chapter_SyncGuideLevelPassData")]
public sealed class ChapterGuideSync : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(new CallGsReply("{\"levels\":[]}"));
}

[Route("SettingChange")]
[Route("LanguageChange")]
[Route("ChangeMainScene")]
[Route("GuideLogic_WriteGuideLog")]
[Route("RecordConfession")]
[Route("ReqEntranceGreenLevel")]
[Route("FavMailLog")]
[Route("Online_ClientLog")]
[Route("Launch_OnCloseLoadingLog")]
[Route("Adjust_Record")]
[Route("ExtendFightLog")]
[Route("ExtendFightDynamicLog")]
[Route("do_server_file_check")]
public sealed class MiscPassthrough : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}

[Route("ZoneTime_ReqTime")]
public sealed class ZoneTimeReq : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var arg = System.Text.Json.JsonSerializer.Serialize(new { time = PlayerInstance.Now() });
        return Task.FromResult(new CallGsReply(arg, ReplyApi: "ZoneTime_RspTime"));
    }
}

[Route("PlayerSetting_SetProfileFace")]
[Route("PlayerSetting_ChangeShowCard")]
[Route("PlayerSetting_SetShowBadge")]
[Route("PlayerSetting_SetShowBubble")]
[Route("PlayerSetting_SetShowCover")]
[Route("PlayerSetting_SetShowNameCard")]
public sealed class PlayerSettingApis : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var value = root.Find("id", "value", "card_id").Ulong();
        var player = session.Player!;
        if (value is { } v)
            player.Data.ShowItems.Add(v);
        player.Save();
        return Task.FromResult(new CallGsReply());
    }
}
