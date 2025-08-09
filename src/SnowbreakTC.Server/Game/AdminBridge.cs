using SnowbreakTC.GameLogic;
using SnowbreakTC.WebAPI;

namespace SnowbreakTC.Server.Game;

public sealed class AdminBridge(GameServer server) : IAdminGame
{
    public int OnlineCount => server.ActiveSessions().Count();

    public IEnumerable<(ulong Pid, string Name, uint Level)> OnlinePlayers() =>
        server.ActiveSessions().Select(s => (s.Player!.Data.Pid, s.Player.Data.Name, s.Player.Data.Level));

    public (bool Found, string Name, uint Level, ulong Exp, uint Vigor, int Items, bool Online)? PlayerInfo(int uid)
    {
        if (server.Db.GetAccount(uid) == null)
            return null;
        var player = PlayerInstance.LoadOrCreate(uid, server.Db, "");
        return (true, player.Data.Name, player.Data.Level, player.Data.Exp, player.Data.Vigor, player.Data.Items.Count,
            server.GetActive((ulong)uid) != null);
    }

    public int KickPlayer(ulong pid) => server.KickPlayer(pid);

    public int SaveAll() => server.SaveAll();

    public int GiveToAll(ulong template, uint count) => server.GiveToAll(template, count);

    public int GiveToOne(ulong pid, ulong template, uint count)
    {
        if (server.GetActive(pid)?.Player is not { } player)
            return 0;
        player.Inventory.Add(template, count);
        player.Save();
        return 1;
    }
}
