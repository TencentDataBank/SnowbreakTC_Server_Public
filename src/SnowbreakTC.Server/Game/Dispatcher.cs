using System.Reflection;

namespace SnowbreakTC.Server.Game;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandAttribute : Attribute
{
    public int CmdId { get; }

    public CommandAttribute(int cmdId) => CmdId = cmdId;
}

public interface IPacketHandler
{
    Task HandleAsync(GameSession session, byte[] body);
}

public static class Dispatcher
{
    private static readonly Dictionary<int, IPacketHandler> Handlers = Build();

    public static async Task<bool> DispatchAsync(GameSession session, ushort cmd, byte[] body)
    {
        if (Handlers.TryGetValue(cmd, out var handler))
        {
            return await Invoke(handler, session, body);
        }
        if (IsRequest(cmd))
        {
            await session.SendAsync((ushort)(cmd + 1));
            return true;
        }
        return false;
    }

    private static async Task<bool> Invoke(IPacketHandler handler, GameSession session, byte[] body)
    {
        try
        {
            await handler.HandleAsync(session, body);
            return true;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "handler {Type} failed (cmd from session {Session})", handler.GetType().Name, session.Id);
            return false;
        }
    }

    public static bool IsRequest(int cmd)
    {
        if (cmd is >= 1 and <= 62 && cmd % 2 == 1 && cmd != 21 && cmd != 22)
            return true;
        return cmd is >= 301 and <= 330 && cmd % 2 == 1;
    }

    private static Dictionary<int, IPacketHandler> Build()
    {
        var map = new Dictionary<int, IPacketHandler>();
        foreach (var type in typeof(Dispatcher).Assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<CommandAttribute>();
            if (attr == null || !typeof(IPacketHandler).IsAssignableFrom(type))
                continue;
            map[attr.CmdId] = (IPacketHandler)Activator.CreateInstance(type)!;
        }
        return map;
    }
}
