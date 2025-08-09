using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Server.Handlers;

namespace SnowbreakTC.Server.Handlers.Friend;

public static class FriendSupport
{
    public static FriendSystem Friends(this GameSession session) =>
        new(session.Player!, session.Server.Db);

    public static List<PlayerProfileBrief> Profiles(GameServer server, IEnumerable<ulong> pids) =>
        pids.Select(pid =>
        {
            var account = server.Db.GetAccount((int)pid);
            if (account == null)
                return null;
            var player = PlayerInstance.LoadOrCreate((int)pid, server.Db, account.Username);
            return ChatProfileExt.BuildProfile(player);
        })
        .Where(p => p != null)
        .Select(p => p!)
        .ToList();

    public static async Task PushFriendListAsync(GameSession session)
    {
        var profiles = Profiles(session.Server, session.Friends().FriendIds);
        var payload = Codec.Encode(new PlayerProfileArray { List = profiles });
        await session.SendRawAsync(CmdIds.NtfUpdateFriend, payload);
    }

    public static ulong? BodyPid(byte[] body)
    {
        var r = new WireReader(body);
        while (r.ReadTag(out var field, out var wt))
        {
            if (field == 1 && wt == WireType.Varint)
                return r.ReadVarint();
            r.Skip(wt);
        }
        return null;
    }
}

public sealed class PlayerProfileArray : IWireMessage
{
    [Field(1)] public List<PlayerProfileBrief> List { get; set; } = [];
}

[Command(CmdIds.ReqAddFriendReq)]
public sealed class AddFriendHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var pid = FriendSupport.BodyPid(body);
        if (pid is { } target && session.Friends().Add(target))
        {
            session.Player!.Save();
            await session.SendAsync(CmdIds.RspAddFriendReq);
            await FriendSupport.PushFriendListAsync(session);
            return;
        }
        await session.SendAsync(CmdIds.RspAddFriendReq);
    }
}

[Command(CmdIds.ReqAgreeFriendReq)]
public sealed class AgreeFriendHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var pid = FriendSupport.BodyPid(body);
        if (pid is { } target)
            session.Friends().Add(target);
        await session.SendAsync(CmdIds.RspAgreeFriendReq);
        await FriendSupport.PushFriendListAsync(session);
    }
}

[Command(CmdIds.ReqRefuseFriendReq)]
public sealed class RefuseFriendHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body) =>
        await session.SendAsync(CmdIds.RspRefuseFriendReq);
}

[Command(CmdIds.ReqRemoveFriend)]
public sealed class RemoveFriendHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var pid = FriendSupport.BodyPid(body);
        if (pid is { } target)
            session.Friends().Remove(target);
        await session.SendAsync(CmdIds.RspRemoveFriend);
        await FriendSupport.PushFriendListAsync(session);
    }
}

[Command(CmdIds.ReqGiveFriendVigor)]
public sealed class GiveFriendVigorHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var granted = session.Friends().GiveVigor();
        if (granted && session.Player != null)
        {
            session.Player.Data.Vigor = Math.Min(session.Player.Data.Vigor + 20, session.Server.Config.Game.VigorLimit);
            session.Player.Save();
        }
        await session.SendAsync(CmdIds.RspGiveFriendVigor);
    }
}

[Command(CmdIds.ReqRecvFriendVigor)]
public sealed class RecvFriendVigorHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        session.Friends().GiveVigor();
        session.Player!.Data.Vigor = Math.Min(session.Player.Data.Vigor + 20, session.Server.Config.Game.VigorLimit);
        session.Player.Save();
        await session.SendAsync(CmdIds.RspRecvFriendVigor);
    }
}

[Command(CmdIds.ReqFindPlayer)]
public sealed class FindPlayerHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var keyword = FriendBodyReader.FindKeyword(body);
        var server = session.Server;
        var accounts = server.Db.AllUids().Select(uid => server.Db.GetAccount(uid)!).ToList();
        var match = accounts.FirstOrDefault(a =>
            keyword != null && (a.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase) || a.Uid.ToString() == keyword));
        if (match != null)
        {
            var payload = Codec.Encode(new PlayerProfileArray { List = FriendSupport.Profiles(server, [(ulong)match.Uid]) });
            await session.SendRawAsync(CmdIds.NtfUpdateFriend, payload);
        }
        await session.SendAsync(CmdIds.RspFindPlayer);
    }
}

[Command(CmdIds.ReqPlayerProfile)]
public sealed class PlayerProfileHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var pid = FriendSupport.BodyPid(body);
        if (pid is not { } target)
        {
            await session.SendAsync(CmdIds.RspPlayerProfile);
            return;
        }
        var profiles = FriendSupport.Profiles(session.Server, [target]);
        await session.SendRawAsync(CmdIds.NtfUpdateFriend, Codec.Encode(new PlayerProfileArray { List = profiles }));
        await session.SendAsync(CmdIds.RspPlayerProfile);
    }
}

public static class FriendBodyReader
{
    public static string? FindKeyword(byte[] body)
    {
        var r = new WireReader(body);
        while (r.ReadTag(out var field, out var wt))
        {
            if (field == 1 && wt == WireType.Bytes)
                return r.ReadString();
            r.Skip(wt);
        }
        return null;
    }
}
