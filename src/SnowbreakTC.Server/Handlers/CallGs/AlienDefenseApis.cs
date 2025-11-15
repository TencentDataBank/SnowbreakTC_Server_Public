using System.Text.Json;
using System.Text.Json.Serialization;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.Server.Handlers.CallGs;

public sealed record AlienUpgradeRec
{
    [JsonPropertyName("Id")] public uint Id { get; init; }
    [JsonPropertyName("SkillId")] public ulong SkillId { get; init; }
    [JsonPropertyName("UpgradeType")] public uint UpgradeType { get; init; }
    [JsonPropertyName("PreId")] public uint PreId { get; init; }
}

public sealed record AlienAwardRec
{
    [JsonPropertyName("SeasonID")] public uint SeasonId { get; init; }
    [JsonPropertyName("Index")] public uint Index { get; init; }
    [JsonPropertyName("NeedScore")] public uint NeedScore { get; init; }
    [JsonPropertyName("ScoreAward")] public List<List<uint>> ScoreAward { get; init; } = [];
}

[Route("AlienDefense_UpgradeSkill")]
public sealed class AlienUpgradeSkill : ICallGsApi
{
    public const uint PackedGroup = 143;
    public const uint BaseSid = 301;
    public const uint MaxPalIdx = 20;
    public const uint MaxSkillIdx = 8;
    public const uint MaxSkillLevel = 15;

    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var palIdx = root.Find("pal_idx", "pal", "palid").Uint() ?? 0;
        var skillIdx = root.Find("skill_idx", "skill", "skillid").Uint() ?? 0;
        if (palIdx == 0 || palIdx > MaxPalIdx || skillIdx == 0 || skillIdx > MaxSkillIdx || BaseSid + palIdx > 350)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var sid = BaseSid + palIdx;
        var shift = (int)((skillIdx - 1) * 4);
        var packed = player.GetAttr(PackedGroup, sid);
        var current = (packed >> shift) & 0xF;
        if (current >= MaxSkillLevel)
            return Task.FromResult(CallGsReply.Error("tip.girlcard_break_max"));
        var updated = current + 1;
        packed = (packed & ~(15u << shift)) | (updated << shift);
        player.SetAttr(PackedGroup, sid, packed);
        player.Save();
        var arg = JsonSerializer.Serialize(new { pal_idx = palIdx, skill_idx = skillIdx, level = updated });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("AlienDefense_CheckOpenAct")]
public sealed class AlienCheckOpen : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var dir = Path.Combine(session.Server.Resources.Root, "dlc", "AlienDefense");
        return Task.FromResult(Directory.Exists(dir)
            ? new CallGsReply("{\"open\":true}")
            : new CallGsReply("{\"open\":false}"));
    }
}

[Route("AlienDefense_GetScoreAward")]
public sealed class AlienScoreAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var index = root.Find("index", "award_index").Uint() ?? 0;
        var season = root.Find("season", "season_id").Uint() ?? 1;
        var awards = session.Server.Resources.Table<AlienAwardRec>("dlc/AlienDefense/alienaward.json");
        var rec = awards?.FirstOrDefault(a => a.SeasonId == season && a.Index == index);
        if (rec == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var score = (ulong)player.GetExtra().GetValueOrDefault($"alien_score_s{season}", 0);
        if (score < rec.NeedScore)
            return Task.FromResult(CallGsReply.Error("tip.score_not_enough"));
        var mark = $"alien_award_s{season}_{index}";
        if (player.GetExtra().ContainsKey(mark))
            return Task.FromResult(CallGsReply.Error("tip.already_signed"));
        var extras = player.GetExtra();
        extras[mark] = (long)PlayerInstance.Now();
        player.SetExtra(extras);
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(rec.ScoreAward.Select(e => new RewardItem
        {
            G = e.Count > 0 ? e[0] : 0,
            D = e.Count > 1 ? e[1] : 0,
            Count = e.Count > 4 ? e[4] : 1,
        }));
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            index,
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("AlienDefense_LevelSettlement")]
public sealed class AlienSettlement : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var season = root.Find("season", "season_id").Uint() ?? 1;
        var score = root.Find("score", "point").Uint() ?? 0;
        var key = $"alien_score_s{season}";
        var extras = player.GetExtra();
        extras[key] = extras.GetValueOrDefault(key, 0) + score;
        player.SetExtra(extras);
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = score / 10 },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            season,
            score,
            total = extras[key],
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("AlienDefense_GetQuestAward")]
[Route("AlienDefense_SaveFightGirl")]
[Route("AlienDefense_SaveFightPal")]
[Route("AlienDefense_SetPalFlag")]
[Route("AlienDefense_EnterLevel")]
public sealed class AlienMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var key = root.Find("key", "id", "pal_id").Str();
        var value = root.Find("value", "val", "level").Ulong();
        if (key != null && value is { } v)
        {
            player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [$"alien_{key}"] = (long)v });
            player.Save();
        }
        return Task.FromResult(new CallGsReply());
    }
}
