using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

/// <summary>
/// DLC_RMOL board game: the client owns the map/dice locally and only needs the
/// server to echo the accepted session/level back (see reference DLC_RMOL.lua).
/// </summary>
[Route("DLC_RMOL_EnterLevel")]
public sealed class RmolEnterLevel : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var sess = root.Find("session").Uint() ?? 0;
        var level = root.Find("level", "level_id").Uint() ?? 0;
        var girl = root.Find("girl").Ulong() ?? 0;
        if (sess == 0 || level == 0)
            return Task.FromResult(new CallGsReply("{\"Err\":\"error.BadParam\"}"));
        session.Player!.VcSet($"rmol_{sess}_{level}", (long)PlayerInstance.Now());
        var arg = JsonSerializer.Serialize(new { Session = sess, Level = level, Temp = Array.Empty<object>() });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DLC_RMOL_LevelEnd")]
public sealed class RmolLevelEnd : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var sess = root.Find("session").Uint() ?? 0;
        var level = root.Find("level", "level_id").Uint() ?? 0;
        session.Player!.VcSet($"rmol_done_{sess}_{level}", (long)PlayerInstance.Now());
        var arg = JsonSerializer.Serialize(new { Session = sess, Level = level, Temp = Array.Empty<object>() });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DLC_RMOL_LevelGiveup")]
public sealed class RmolLevelGiveup : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var sess = root.Find("session").Uint() ?? 0;
        var level = root.Find("level", "level_id").Uint() ?? 0;
        session.Player!.VcSet($"rmol_giveup_{sess}_{level}", (long)PlayerInstance.Now());
        var arg = JsonSerializer.Serialize(new { Session = sess, Level = level, Temp = Array.Empty<object>() });
        return Task.FromResult(new CallGsReply(arg));
    }
}
