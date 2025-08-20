using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class ChessStateExt
{
    public static void SetFormation(this PlayerInstance player, string activity, long formation)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra())
        {
            [$"chess{activity}_formation"] = formation,
        });
        player.Save();
    }

    public static long GetFormation(this PlayerInstance player, string activity) =>
        player.GetExtra().GetValueOrDefault($"chess{activity}_formation", 0);
}

public static class ChessFiles
{
    public static string? ReadChessFile(GameSession session, string activity, string file)
    {
        var path = Path.Combine(session.Server.Resources.Root, "chess", activity, file);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}

[Route("ChessLogic_EnterMap")]
public sealed class ChessEnterMap : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var activity = root.Find("activity", "activity_id", "id").Str() ?? "demo";
        var map = ChessFiles.ReadChessFile(session, activity, "map1.json")
            ?? ChessFiles.ReadChessFile(session, activity, "grids.json");
        var player = session.Player!;
        var arg = map != null
            ? JsonSerializer.Serialize(new
            {
                activity,
                formation = player.GetFormation(activity),
                map = JsonSerializer.Deserialize<JsonElement>(map),
            })
            : "{}";
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("ChessLogic_ChangeFormation")]
public sealed class ChessChangeFormation : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var activity = root.Find("activity", "activity_id", "id").Str() ?? "demo";
        var formation = root.Find("formation", "formation_id", "id").Ulong();
        if (formation is { } f)
            player.SetFormation(activity, (long)f);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("ChessLogic_CheckOpenAct")]
public sealed class ChessCheckOpen : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var activity = root.Find("activity", "activity_id", "id").Str() ?? "demo";
        var dir = Path.Combine(session.Server.Resources.Root, "chess", activity);
        return Task.FromResult(Directory.Exists(dir)
            ? new CallGsReply("{\"open\":true}")
            : new CallGsReply("{\"open\":false}"));
    }
}

[Route("Chess_EnterLevel")]
public sealed class ChessEnterLevel : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var activity = root.Find("activity", "activity_id", "id").Str() ?? "demo";
        var rewards = ChessFiles.ReadChessFile(session, activity, "rewards.json");
        var arg = rewards != null
            ? JsonSerializer.Serialize(new { activity, rewards = JsonSerializer.Deserialize<JsonElement>(rewards) })
            : "{}";
        return Task.FromResult(new CallGsReply(arg));
    }
}
