using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

// ---------- Boss damage ranking ----------

public static class BossExt
{
    public static void AddBossDamage(this PlayerInstance player, string bossKey, ulong damage)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra())
        {
            [$"boss_{bossKey}"] = player.GetExtra().GetValueOrDefault($"boss_{bossKey}", 0) + (long)Math.Min(damage, int.MaxValue),
        });
        player.Save();
    }

    public static ulong BossDamage(this PlayerInstance player, string bossKey) =>
        (ulong)Math.Max(0, player.GetExtra().GetValueOrDefault($"boss_{bossKey}", 0));

    public static (uint Tier, List<RewardItem> Rewards)? ClaimBossReward(this PlayerInstance player, ResourceManager resources, string bossKey)
    {
        var damage = player.BossDamage(bossKey);
        var tier = damage switch
        {
            >= 100000 => 4u,
            >= 50000 => 3u,
            >= 10000 => 2u,
            > 0 => 1u,
            _ => 0u,
        };
        if (tier == 0)
            return null;
        var mark = $"boss_{bossKey}_claimed_t{tier}";
        if (player.GetExtra().ContainsKey(mark))
            return null;
        var extras = player.GetExtra();
        extras[mark] = (long)PlayerInstance.Now();
        player.SetExtra(extras);
        var rewards = player.Quest(resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = tier * 200 },
            new RewardItem { G = ItemGenres.Supplies, D = 1, Count = tier * 50 },
        ]);
        player.Save();
        return (tier, rewards);
    }
}

[Route("WorldBossLogic_Record")]
[Route("SeasonBoss_Record")]
[Route("BossPvpLogic_Record")]
public sealed class BossRecord : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var damage = root.Find("damage", "score", "point").Ulong() ?? 0;
        var boss = root.Find("boss_id", "bossid", "id").Uint() ?? 0;
        var key = $"w{boss}";
        player.AddBossDamage(key, damage);
        var rank = damage >= 100000 ? 1 : damage >= 50000 ? 2 : damage >= 10000 ? 3 : 4;
        var arg = JsonSerializer.Serialize(new
        {
            damage,
            rank,
            total = player.BossDamage(key),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("WorldBossLogic_CheckReward")]
[Route("WorldBossLogic_GetReward")]
[Route("SeasonBoss_GetReward")]
[Route("BossPvpLogic_GetReward")]
public sealed class BossGetReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var boss = root.Find("boss_id", "bossid", "id").Uint() ?? 0;
        var claim = player.ClaimBossReward(session.Server.Resources, $"w{boss}");
        player.Save();
        return claim == null
            ? Task.FromResult(CallGsReply.Error("tip.reward_not_available"))
            : Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new
            {
                tier = claim.Value.Tier,
                rewards = claim.Value.Rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
            })));
    }
}

// ---------- Puzzle ----------

[Route("JigsawPuzzle_GetPiece")]
public sealed class PuzzleGetPiece : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var piece = Random.Shared.Next(1, 10);
        player.CaptureSet($"puzzle_{piece}", 1);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { piece })));
    }
}

[Route("JigsawPuzzle_PutPiece")]
public sealed class PuzzlePutPiece : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var slot = root.Find("slot", "index").Uint() ?? 0;
        var piece = root.Find("piece", "piece_id").Uint() ?? 0;
        player.CaptureSet($"puzzle_slot{slot}", piece);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("JigsawPuzzle_UseHelp")]
public sealed class PuzzleUseHelp : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        if (!player.ConsumeCurrency(AttrIds.Currency.Silver, 200))
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.Save();
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("JigsawPuzzle_GetReward")]
public sealed class PuzzleGetReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var sent = player.GetExtra().GetValueOrDefault("puzzle_sent", 0) + 1;
        var clip = sent switch
        {
            >= 3 => 9,
            >= 2 => 8,
            _ => 7,
        };
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { ["puzzle_sent"] = sent });
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = 100 },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            sent,
            clip,
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("JigsawPuzzle_SetActive")]
[Route("JigsawPuzzle_EnterLog")]
[Route("JigsawPuzzle_GetGirl")]
public sealed class PuzzleMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}

// ---------- ClimbTower progress ----------

[Route("ClimbTowerLogic_RecordProgres")]
public sealed class ClimbTowerProgress : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var level = root.Find("level", "level_id").Uint() ?? 0;
        var star = root.Find("star", "score").Uint() ?? 3;
        if (level > 0)
            player.CaptureSet($"ct_{level}", star);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("ClimbTowerLogic_SetLevelDiff")]
public sealed class ClimbTowerDiff : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var diff = root.Find("diff", "difficulty", "id").Uint() ?? 1;
        player.CaptureSet("ct_diff", diff);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("ClimbTowerLogic_GetReward")]
public sealed class ClimbTowerReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = 150 },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}
