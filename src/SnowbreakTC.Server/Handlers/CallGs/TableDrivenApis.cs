using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class RewardTables
{
    private static List<List<uint>> CollectArray(JsonElement award)
    {
        if (award.ValueKind == JsonValueKind.Array)
            return [award.EnumerateArray().Select(e => e.TryGetUInt32(out var v) ? v : 0).ToList()];
        if (award.ValueKind == JsonValueKind.Object)
        {
            return award.EnumerateObject()
                .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                    ? p.Value.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.Array
                            ? e.EnumerateArray().Select(n => n.TryGetUInt32(out var v) ? v : 0).ToList()
                            : (List<uint>?)null)
                        .Where(x => x != null)
                        .Select(x => x!)
                    : [])
                .ToList();
        }
        return [];
    }

    private static IEnumerable<JsonElement> Rows(ResourceManager resources, string path)
    {
        var raw = resources.Raw(path);
        if (raw == null)
            yield break;
        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            yield break;
        foreach (var row in doc.RootElement.EnumerateArray())
            yield return row;
    }

    public static List<List<uint>>? BattlePassAwards(ResourceManager resources, uint level)
    {
        var awards = Rows(resources, "battlepass/levellist.json")
            .Where(x => x.TryGetProperty("Level", out var lv) && lv.TryGetUInt32(out var l) && l == level)
            .SelectMany(x => new[]
            {
                x.TryGetProperty("NormalAward", out var n) ? n : default,
                x.TryGetProperty("AdvanceAward", out var a) ? a : default,
            })
            .Where(x => x.ValueKind != JsonValueKind.Undefined && x.ValueKind != JsonValueKind.Null)
            .SelectMany(CollectArray)
            .ToList();
        return awards.Count > 0 ? awards : null;
    }

    public static List<List<uint>>? AchievementRewards(ResourceManager resources, uint questId)
    {
        var entries = Rows(resources, "achievement/achievement.json")
            .Where(x => x.TryGetProperty("ID", out var id) && id.TryGetUInt32(out var i) && i == questId)
            .SelectMany(x => x.TryGetProperty("Rewards", out var rw) ? CollectArray(rw) : [])
            .ToList();
        return entries.Count > 0 ? entries : null;
    }

    public static (uint Stars, uint Quality) AdversarySpec(ResourceManager resources, uint id)
    {
        foreach (var x in Rows(resources, "dlc/DonoDuel/donoduel_adversary.json"))
        {
            if (x.TryGetProperty("ID", out var i) && i.TryGetUInt32(out var v) && v == id)
                return (
                    x.TryGetProperty("Stars", out var s) && s.TryGetUInt32(out var st) ? st : 1,
                    x.TryGetProperty("Quality", out var q) && q.TryGetUInt32(out var qt) ? qt : 1);
        }
        return (1, 1);
    }

    public static List<List<uint>>? ArenaReward(ResourceManager resources, uint id)
    {
        var entries = Rows(resources, "dlc/DonoDuel/donoduel_arena_battle.json")
            .Where(x => x.TryGetProperty("ID", out var i) && i.TryGetUInt32(out var v) && v == id)
            .SelectMany(x => x.TryGetProperty("Reward", out var rw) ? CollectArray(rw) : [])
            .ToList();
        return entries.Count > 0 ? entries : null;
    }

    public static List<List<uint>>? NitaTreeAward(ResourceManager resources, uint nodeId)
    {
        var entries = Rows(resources, "activity/nita_monopoly/tree.json")
            .Where(x => x.TryGetProperty("ID", out var i) && i.TryGetUInt32(out var v) && v == nodeId)
            .SelectMany(x => x.TryGetProperty("Reward", out var rw) ? CollectArray(rw) : [])
            .ToList();
        return entries.Count > 0 ? entries : null;
    }
}

public static class RewardTableExt
{
    public static CallGsReply GrantEntries(this PlayerInstance player, ResourceManager resources, IEnumerable<List<uint>> entries)
    {
        var rewards = player.Quest(resources).DeliverRewards(entries.Select(e => new RewardItem
        {
            G = e.Count > 0 ? e[0] : 0,
            D = e.Count > 1 ? e[1] : 0,
            P = e.Count > 2 ? e[2] : 0,
            L = e.Count > 3 ? e[3] : 0,
            Count = e.Count > 4 ? e[4] : 1,
        }));
        player.Save();
        return new CallGsReply(JsonSerializer.Serialize(new
        {
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        }));
    }
}

[Route("BattlePassLogic_ClientRefresh")]
public sealed class BattlePassTableRefresh : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var level = Math.Max(1, player.GetExtra().GetValueOrDefault("bp_level", 1));
        var awards = RewardTables.BattlePassAwards(session.Server.Resources, (uint)level) ?? [];
        var arg = JsonSerializer.Serialize(new
        {
            level,
            normal = awards.Count > 0 ? awards[0] : new List<uint>(),
            advance = awards.Count > 1 ? awards[1] : new List<uint>(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Achievement_GetReward")]
public sealed class AchievementTableReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var questId = root.Find("quest_id", "id").Ulong() ?? 0;
        var entries = RewardTables.AchievementRewards(session.Server.Resources, (uint)questId);
        if (entries is not { Count: > 0 })
            return Task.FromResult(player.GrantJson(session.Server.Resources, 24, 120));
        player.Mark($"ach_{questId}", (long)PlayerInstance.Now());
        return Task.FromResult(player.GrantEntries(session.Server.Resources, entries));
    }
}

[Route("DonoDuel_UpgradeStar")]
public sealed class DonoDuelStarTable : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var id = root.Find("id").Ulong() ?? 0;
        var (stars, _) = RewardTables.AdversarySpec(session.Server.Resources, (uint)id);
        var current = player.GetExtra().GetValueOrDefault($"dd_star_{id}", 0);
        if (current >= stars)
            return Task.FromResult(CallGsReply.Error("tip.girlcard_break_max"));
        var cost = (uint)(800 * (current + 1));
        if (!player.ConsumeCurrency(AttrIds.Currency.Silver, cost))
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [$"dd_star_{id}"] = current + 1 });
        player.Save();
        var arg = JsonSerializer.Serialize(new { id, star = current + 1, max = stars });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuel_UpgradeQuality")]
public sealed class DonoDuelQualityTable : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var id = root.Find("id").Ulong() ?? 0;
        var (_, quality) = RewardTables.AdversarySpec(session.Server.Resources, (uint)id);
        var current = player.GetExtra().GetValueOrDefault($"dd_quality_{id}", 0);
        if (current >= quality)
            return Task.FromResult(CallGsReply.Error("tip.girlcard_break_max"));
        var cost = (uint)(2000 * (current + 1));
        if (!player.ConsumeCurrency(AttrIds.Currency.Silver, cost))
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [$"dd_quality_{id}"] = current + 1 });
        player.Save();
        var arg = JsonSerializer.Serialize(new { id, quality = current + 1, max = quality });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuelTask_GetReward")]
public sealed class DonoDuelTaskTableReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var battleId = root.Find("battle_id", "id").Uint() ?? 0;
        var entries = RewardTables.ArenaReward(session.Server.Resources, battleId);
        return entries is { Count: > 0 }
            ? Task.FromResult(player.GrantEntries(session.Server.Resources, entries))
            : Task.FromResult(player.GrantJson(session.Server.Resources, 24, 200));
    }
}

[Route("NitaMonolopy_GetTreeAward")]
public sealed class NitaTreeTableAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var nodeId = root.Find("node_id", "tree_id", "id").Uint() ?? 0;
        var entries = RewardTables.NitaTreeAward(session.Server.Resources, nodeId);
        return entries is { Count: > 0 }
            ? Task.FromResult(player.GrantEntries(session.Server.Resources, entries))
            : Task.FromResult(player.GrantJson(session.Server.Resources, 24, 260));
    }
}

public static class DonoDuelConfigTable
{
    public const uint SeasonCurrency = 63;

    public static (uint Cost, uint GainMin, uint GainMax)? AttrGain(ResourceManager resources, uint position, uint quality)
    {
        var configId = position switch
        {
            1 => 1u,
            2 => 23u,
            3 => 24u,
            _ => 0u,
        };
        var raw = resources.Raw("dlc/DonoDuel/donoduel_config.json");
        if (raw == null || configId == 0)
            return null;
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("ConfigID", out var id) || !id.TryGetUInt32(out var cid) || cid != configId)
                continue;
            if (!row.TryGetProperty("Parameter", out var param) || param.ValueKind != JsonValueKind.Array)
                return null;
            foreach (var entry in param.EnumerateArray())
            {
                var vals = entry.EnumerateArray().Select(e => e.TryGetUInt32(out var v) ? v : 0).ToList();
                if (vals.Count >= 4 && vals[0] == quality)
                    return (vals[1], vals[2], vals[3]);
            }
        }
        return null;
    }

    public static (uint Currency, uint Amount)? SkillTreeConsume(ResourceManager resources, uint nodeId)
    {
        var raw = resources.Raw("dlc/DonoDuel/donoduel_skilltree.json");
        if (raw == null)
            return null;
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("ID", out var id) || !id.TryGetUInt32(out var v) || v != nodeId)
                continue;
            if (row.TryGetProperty("Consume", out var consume) && consume.ValueKind == JsonValueKind.Array)
            {
                var vals = consume.EnumerateArray().Select(e => e.TryGetUInt32(out var c) ? c : 0).ToList();
                if (vals.Count >= 2)
                    return (vals[0], vals[1]);
            }
        }
        return null;
    }

    public static List<uint>? SkillTreePrerequisite(ResourceManager resources, uint nodeId)
    {
        var raw = resources.Raw("dlc/DonoDuel/donoduel_skilltree.json");
        if (raw == null)
            return null;
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("ID", out var id) || !id.TryGetUInt32(out var v) || v != nodeId)
                continue;
            if (row.TryGetProperty("Prerequisite", out var pre))
                return pre.ValueKind == JsonValueKind.Array
                    ? pre.EnumerateArray().Select(e => e.TryGetUInt32(out var v) ? v : 0).ToList()
                    : [];
        }
        return null;
    }
}
