using SnowbreakTC.Protocol.Wire;

namespace SnowbreakTC.Protocol.Messages;

public sealed class NtfSetAttr : IWireMessage
{
    [Field(1)] public uint Gid { get; set; }
    [Field(2)] public uint Sid { get; set; }
    [Field(3)] public uint Val { get; set; }
}

public sealed class NtfSetStrAttr : IWireMessage
{
    [Field(1)] public uint Gid { get; set; }
    [Field(2)] public uint Sid { get; set; }
    [Field(3)] public string Val { get; set; } = "";
}

public sealed class NtfSyncPlayer : IWireMessage
{
    [Field(1)] public string Sign { get; set; } = "";
    [Field(2)] public Dictionary<uint, uint> Core { get; set; } = [];
    [Field(3)] public Dictionary<uint, uint> Custom { get; set; } = [];
    [Field(4)] public Dictionary<uint, string> CustomStr { get; set; } = [];
    [Field(5)] public List<ItemInfo> Items { get; set; } = [];
    [Field(6)] public List<ulong> ShowItems { get; set; } = [];
    [Field(7)] public List<uint> ShowAttrs { get; set; } = [];
    [Field(8)] public Dictionary<string, int> Money { get; set; } = [];
    [Field(9)] public List<FriendPieceEntry> Pieces { get; set; } = [];
    [Field(10)] public List<ulong> Badges { get; set; } = [];
    [Field(11)] public List<ulong> Tags { get; set; } = [];
}

public sealed class NtfSyncLineup : IWireMessage
{
    [Field(1)] public LineupInfo? Lineup { get; set; }
    [Field(2)] public bool Remove { get; set; }
}
