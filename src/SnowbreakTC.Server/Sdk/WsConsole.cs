using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SnowbreakTC.Core;
using SnowbreakTC.Server.Game;
using SnowbreakTC.WebAPI;

namespace SnowbreakTC.Server.Sdk;

public static class WsConsole
{
    public static async Task HandleAsync(HttpContext ctx, GameServer? game, AppConfig config)
    {
        using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
        var hello = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { cmd = "", node = Cluster.NodeTag }));
        await ws.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, ctx.RequestAborted);
        var buffer = new byte[4096];
        while (ws.State == WebSocketState.Open)
        {
            var received = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ctx.RequestAborted);
            if (received.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ctx.RequestAborted);
                return;
            }
            var text = System.Text.Encoding.UTF8.GetString(buffer, 0, received.Count);
            var result = Execute(game, config, text);
            var payload = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { cmd = text, result }));
            await ws.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, ctx.RequestAborted);
        }
    }

    private static object Execute(GameServer? game, AppConfig config, string text)
    {
        if (game == null)
            return new { error = "game server unavailable" };
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts[0].ToLowerInvariant() switch
        {
            "overview" => new { online = game.ActiveSessions().Count(), apis = Handlers.CallGs.Router.RegisteredCount },
            "online" => game.ActiveSessions().Select(s => new { uid = s.Player!.Data.Pid, name = s.Player.Data.Name }).ToList(),
            "kick" when parts.Length == 2 && ulong.TryParse(parts[1], out var k) => new { kicked = game.KickPlayer(k) },
            "save" => new { saved = game.SaveAll() },
            "give" when parts.Length >= 4
                && uint.TryParse(parts[1], out var g) && uint.TryParse(parts[2], out var d) && uint.TryParse(parts[3], out var p) =>
                new { given_to = game.GiveToAll(((ulong)g << 48) | ((ulong)p << 32) | ((ulong)d << 16) | g, parts.Length > 4 && uint.TryParse(parts[4], out var c) ? c : 1) },
            "stop" => new { stopping = true },
            _ => (object)new { error = "unknown command", supported = new[] { "overview", "online", "kick <uid>", "save", "give <g> <d> <p> [count]", "stop" } },
        };
    }
}
