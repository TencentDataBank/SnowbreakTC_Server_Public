using System.Text.Json;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Server.Handlers.Friend;

namespace SnowbreakTC.Server.Handlers;

public sealed class OnlineRoom
{
    public uint RoomId { get; set; }
    public ulong Owner { get; set; }
    public uint OnlineId { get; set; }
    public List<ulong> Members { get; set; } = [];
}

public static class RoomManager
{
    private static readonly object Gate = new();
    private static readonly Dictionary<uint, OnlineRoom> Rooms = [];
    private static uint _nextId = 1;

    public static OnlineRoom? FindByMember(ulong pid)
    {
        lock (Gate)
            return Rooms.Values.FirstOrDefault(r => r.Members.Contains(pid));
    }

    public static OnlineRoom Create(GameSession session, uint onlineId)
    {
        lock (Gate)
        {
            Leave(session, quiet: true);
            var room = new OnlineRoom
            {
                RoomId = _nextId++,
                Owner = session.Player!.Data.Pid,
                OnlineId = onlineId,
                Members = [session.Player.Data.Pid],
            };
            Rooms[room.RoomId] = room;
            return room;
        }
    }

    public static OnlineRoom? Join(GameSession session, uint roomId)
    {
        lock (Gate)
        {
            if (!Rooms.TryGetValue(roomId, out var room))
                return null;
            Leave(session, quiet: true);
            if (!room.Members.Contains(session.Player!.Data.Pid))
                room.Members.Add(session.Player.Data.Pid);
            return room;
        }
    }

    public static void Leave(GameSession session, bool quiet = false)
    {
        var pid = session.Player?.Data.Pid ?? 0;
        if (pid == 0)
            return;
        lock (Gate)
        {
            var room = Rooms.Values.FirstOrDefault(r => r.Members.Contains(pid));
            if (room == null)
                return;
            room.Members.Remove(pid);
            if (room.Members.Count == 0 || room.Owner == pid)
                Rooms.Remove(room.RoomId);
        }
    }

    public static async Task BroadcastAsync(OnlineRoom room, ushort cmd, object payload, GameServer server)
    {
        var data = Codec.Encode(payload);
        foreach (var pid in room.Members)
        {
            if (server.GetActive(pid) is { Alive: true } member)
                await member.SendRawAsync(cmd, data);
        }
    }
}

public static class OnlineRoomSupport
{
    public static object RoomJson(OnlineRoom room, GameServer server) => new
    {
        room_id = room.RoomId,
        owner = room.Owner,
        online_id = room.OnlineId,
        members = room.Members.Select(pid =>
        {
            var session = server.GetActive(pid);
            return new { pid, name = session?.Player?.Data.Name ?? "", level = session?.Player?.Data.Level ?? 0, ready = true };
        }).ToList(),
    };
}

[Command(CmdIds.ReqOnlineRoom)]
public sealed class OnlineRoomHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var player = session.Player!;
        var room = RoomManager.FindByMember(player.Data.Pid);
        if (room == null)
        {
            room = RoomManager.Create(session, 1);
        }
        await RoomManager.BroadcastAsync(room, CmdIds.NtfOnlineRoomInfo, OnlineRoomSupport.RoomJson(room, session.Server), session.Server);
    }
}

[Command(CmdIds.ReqOnlineRoomStart)]
public sealed class OnlineRoomStartHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var room = RoomManager.FindByMember(session.Player!.Data.Pid);
        if (room == null)
        {
            await session.SendAsync(CmdIds.RspOnlineRoomStart);
            return;
        }
        await RoomManager.BroadcastAsync(room, CmdIds.NtfOnlineStart, new NtfOnlineStartMsg(), session.Server);
        await session.SendAsync(CmdIds.RspOnlineRoomStart);
    }
}

[Command(CmdIds.ReqOnlineRoomExit)]
public sealed class OnlineRoomExitHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        RoomManager.Leave(session);
        await session.SendAsync(CmdIds.RspOnlineRoomExit);
        session.Player?.Save();
    }
}

[Command(CmdIds.ReqChangeWorldChannel)]
public sealed class ChangeWorldChannelHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var channel = FriendSupport.BodyPid(body) ?? 1;
        await session.SendAsync(CmdIds.RspChangeWorldChannel);
        await session.SendAsync(CmdIds.NtfGlobalAttrs, new NtfSetAttr { Gid = 40, Sid = 1, Val = (uint)channel });
    }
}
