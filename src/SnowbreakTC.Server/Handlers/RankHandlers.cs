using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Server.Handlers;

namespace SnowbreakTC.Server.Handlers;

public sealed class RankListMsg : IWireMessage
{
    [Field(1)] public string RankName { get; set; } = "";
    [Field(2)] public List<RankListItem> List { get; set; } = [];
}

public sealed class RankListItem : IWireMessage
{
    [Field(1)] public string MemberName { get; set; } = "";
    [Field(2)] public uint Score { get; set; }
    [Field(3)] public string Info { get; set; } = "";
}

public sealed class RankInfoMsg : IWireMessage
{
    [Field(1)] public uint Score { get; set; }
    [Field(2)] public uint Rank { get; set; }
    [Field(3)] public uint Sum { get; set; }
    [Field(4)] public string Info { get; set; } = "";
}

public static class RankBoard
{
    public static List<(string Name, uint Score)> LevelBoard(GameServer server) =>
        server.Db.AllUids()
            .Select(uid => server.Db.GetAccount(uid)!)
            .Select(a => PlayerInstance.LoadOrCreate(a.Uid, server.Db, a.Username))
            .Select(p => (p.Data.Name, p.Data.Level))
            .OrderByDescending(x => x.Level)
            .Take(50)
            .ToList();

    public static List<(string Name, uint Score)> BossBoard(GameServer server) =>
        server.Db.AllUids()
            .Select(uid => server.Db.GetAccount(uid)!)
            .Select(a => PlayerInstance.LoadOrCreate(a.Uid, server.Db, a.Username))
            .Select(p => (p.Data.Name, (uint)Math.Min(uint.MaxValue, p.GetExtra().GetValueOrDefault("boss_w0", 0))))
            .OrderByDescending(x => x.Item2)
            .Take(50)
            .ToList();
}

[Command(CmdIds.ReqRankList)]
public sealed class RankListHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var server = session.Server;
        var board = RankBoard.LevelBoard(server);
        var msg = new RankListMsg
        {
            RankName = "level",
            List = board.Select((x, i) => new RankListItem
            {
                MemberName = x.Name,
                Score = x.Score,
                Info = (i + 1).ToString(),
            }).ToList(),
        };
        await session.SendRawAsync(CmdIds.RspRankList, Codec.Encode(msg));
    }
}

[Command(CmdIds.ReqRank)]
public sealed class RankHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var player = session.Player!;
        var server = session.Server;
        var board = RankBoard.LevelBoard(server);
        var myScore = player.Data.Level;
        var rank = (uint)(board.Count(b => b.Score > myScore) + 1);
        var msg = new RankInfoMsg
        {
            Score = myScore,
            Rank = rank,
            Sum = (uint)Math.Max(board.Count, 1),
            Info = player.Data.Name,
        };
        await session.SendRawAsync(CmdIds.RspRank, Codec.Encode(msg));
    }
}
