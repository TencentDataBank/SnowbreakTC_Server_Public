using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public sealed record LineupEntry(string Name, int Index, ulong Member1, ulong Member2, ulong Member3);

[Route("Lineup_Update")]
[Route("Lineups_Update")]
public sealed class LineupUpdate : ICallGsApi
{
    public async Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        foreach (var entry in ReadEntries(root))
        {
            var lineup = player.Lineups.GetOrCreate((uint)Math.Max(0, entry.Index));
            lineup.Name = entry.Name;
            lineup.Member1 = entry.Member1;
            lineup.Member2 = entry.Member2;
            lineup.Member3 = entry.Member3;
        }
        player.Save();
        await session.SendAsync(CmdIds.NtfSyncLineup, new NtfSyncLineup());
        return new CallGsReply(ReplyApi: "UpdateLineup");
    }

    private static IEnumerable<LineupEntry> ReadEntries(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                if (ReadEntry(item) is { } e)
                    yield return e;
            yield break;
        }
        if (root.Find("lineups", "list") is { ValueKind: JsonValueKind.Array } batch)
        {
            foreach (var item in batch.EnumerateArray())
                if (ReadEntry(item) is { } e)
                    yield return e;
            yield break;
        }
        if (ReadEntry(root) is { } single)
            yield return single;
    }

    private static LineupEntry? ReadEntry(JsonElement element)
    {
        var index = element.Find("index").Uint();
        if (index == null)
            return null;
        return new LineupEntry(
            element.Find("name").Str() ?? "",
            (int)index,
            element.Find("member1", "member_1").Ulong() ?? 0,
            element.Find("member2", "member_2").Ulong() ?? 0,
            element.Find("member3", "member_3").Ulong() ?? 0);
    }
}

[Route("Gacha_Launch")]
public sealed class GachaLaunch : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var times = root.Find("num", "times", "count").Uint() ?? 1;
        var player = session.Player!;
        var result = new GachaSystem(player, session.Server.Resources).Launch(
            root.Find("id", "pool_id").Uint() ?? 0, times);
        if (!result.Ok)
            return Task.FromResult(CallGsReply.Error(result.Error!));
        var granted = new GachaSystem(player, session.Server.Resources).Deliver(result.Rewards!);
        player.Save();
        var items = granted.Select(i => new
        {
            id = i.Id,
            g = ItemGenres.Unpack(i.Template).Genre,
            d = ItemGenres.Unpack(i.Template).Detail,
            p = ItemGenres.Unpack(i.Template).Particular,
            count = i.Count,
        }).ToList();
        var arg = System.Text.Json.JsonSerializer.Serialize(new { items });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Gacha_UpSelect")]
public sealed class GachaUpSelect : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}
