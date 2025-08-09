using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class SyncBuilder
{
    public static NtfSyncPlayer Build(PlayerInstance player)
    {
        var sync = new NtfSyncPlayer
        {
            Sign = player.Data.Sign,
            Items = player.Data.Items,
            ShowItems = player.Data.ShowItems,
            ShowAttrs = player.Data.ShowAttrs,
            Pieces = player.Data.FriendPieces.Values.ToList(),
            Badges = player.Data.Badges,
            Tags = player.Data.Tags,
            Core = { [1] = player.Data.Level, [2] = (uint)Math.Min(uint.MaxValue, player.Data.Exp), [3] = player.Data.Vigor },
        };
        foreach (var (key, value) in player.Data.Money)
            sync.Money[key] = value;
        foreach (var (key, value) in player.Data.Attrs)
            sync.Custom[key] = value;
        foreach (var (key, value) in player.Data.StrAttrs)
            sync.CustomStr[key] = value;
        return sync;
    }

    public static async System.Threading.Tasks.Task PushSync(this GameSession session)
    {
        if (session.Player != null)
            await session.SendAsync(SnowbreakTC.Protocol.CmdIds.NtfScript,
                new SnowbreakTC.Protocol.Messages.NtfCallScript { Api = "sync", ExtraSync = Build(session.Player) });
    }
}
