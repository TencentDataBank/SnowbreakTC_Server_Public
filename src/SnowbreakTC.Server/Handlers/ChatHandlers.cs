using Serilog;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers;

[Command(CmdIds.ReqWorldChat)]
public sealed class WorldChatHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var player = session.Player;
        if (player == null)
            return;
        var req = Codec.Decode<ChatMessage>(body);
        var reply = new ChatMessage
        {
            Type = ChatChannel.World,
            ChannelId = req.ChannelId,
            Sender = player.Data.Pid,
            TimeStamp = (uint)PlayerInstance.Now(),
            Text = req.Text,
            Profile = ChatProfileExt.BuildProfile(player),
        };
        var payload = Codec.Encode(reply);
        foreach (var target in session.Server.ActiveSessions())
        {
            if (target.Player != null)
                await target.SendRawAsync(CmdIds.NtfWorldChat, payload);
        }
    }
}

[Command(CmdIds.ReqFriendChat)]
public sealed class FriendChatHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var player = session.Player;
        if (player == null)
            return;
        var req = Codec.Decode<ChatMessage>(body);
        var reply = new ChatMessage
        {
            Type = ChatChannel.Friend,
            Sender = player.Data.Pid,
            Recver = req.Recver,
            TimeStamp = (uint)PlayerInstance.Now(),
            Text = req.Text,
            Profile = ChatProfileExt.BuildProfile(player),
        };
        var payload = Codec.Encode(reply);
        var delivered = false;
        if (session.Server.GetActive(req.Recver) is { Alive: true, Player: not null } target)
        {
            await target.SendRawAsync(CmdIds.NtfFriendChat, payload);
            delivered = true;
        }
        await session.SendRawAsync(CmdIds.NtfFriendChat, payload);
        Log.Debug("friend chat {Sender}->{Recver} delivered={Delivered}", player.Data.Pid, req.Recver, delivered);
    }
}

public static class ChatProfileExt
{
    public static PlayerProfileBrief BuildProfile(PlayerInstance player) => new()
    {
        Pid = player.Data.Pid,
        Account = player.Data.Account,
        CreateTime = player.Data.CreateTime,
        Name = player.Data.Name,
        Sign = player.Data.Sign,
        Sex = player.Data.Sex,
        Level = player.Data.Level,
        ShowItems = player.Data.ShowItems,
        ShowAttrs = player.Data.ShowAttrs,
        Badges = player.Data.Badges,
        Tags = player.Data.Tags,
    };
}
