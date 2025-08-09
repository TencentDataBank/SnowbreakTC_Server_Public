using SnowbreakTC.Protocol.Wire;

namespace SnowbreakTC.Protocol.Messages;

public sealed class ClientProfile : IWireMessage
{
    [Field(1)] public uint PlatId { get; set; }
    [Field(2)] public string Version { get; set; } = "";
    [Field(3)] public string OsVersion { get; set; } = "";
    [Field(4)] public string OsHardware { get; set; } = "";
    [Field(5)] public string TelecomOper { get; set; } = "";
    [Field(6)] public uint Network { get; set; }
    [Field(7)] public uint ScreenWidth { get; set; }
    [Field(8)] public uint ScreenHeight { get; set; }
    [Field(9)] public float Density { get; set; }
    [Field(10)] public string CpuProfile { get; set; } = "";
    [Field(11)] public ulong Ram { get; set; }
    [Field(12)] public string GlRender { get; set; } = "";
    [Field(13)] public string GlVersion { get; set; } = "";
    [Field(14)] public string DeviceId { get; set; } = "";
    [Field(15)] public string ResourceVersion { get; set; } = "";
    [Field(16)] public string Language { get; set; } = "";
}

public sealed class EnhanceInfo : IWireMessage
{
    [Field(1)] public uint Level { get; set; }
    [Field(2)] public uint Exp { get; set; }
    [Field(3)] public uint Break { get; set; }
    [Field(4)] public uint Evolue { get; set; }
    [Field(5)] public uint Trust { get; set; }
    [Field(6)] public uint ProLevel { get; set; }
    [Field(11)] public List<ulong> Spines { get; set; } = [];
    [Field(12)] public List<uint> Affixes { get; set; } = [];
}

public sealed class ItemInfo : IWireMessage
{
    [Field(1)] public ulong Id { get; set; }
    [Field(2)] public ulong Template { get; set; }
    [Field(3)] public uint Count { get; set; }
    [Field(4)] public uint Flag { get; set; }
    [Field(5)] public string Userdata { get; set; } = "";
    [Field(6)] public ulong Expiration { get; set; }
    [Field(7)] public EnhanceInfo? Enhance { get; set; }
    [Field(8)] public Dictionary<uint, ulong> Slots { get; set; } = [];
}

public sealed class LineupInfo : IWireMessage
{
    [Field(1)] public uint Index { get; set; }
    [Field(2)] public string Name { get; set; } = "";
    [Field(3)] public ulong Member1 { get; set; }
    [Field(4)] public ulong Member2 { get; set; }
    [Field(5)] public ulong Member3 { get; set; }
}

public sealed class RewardItem : IWireMessage
{
    [Field(1)] public uint G { get; set; }
    [Field(2)] public uint D { get; set; }
    [Field(3)] public uint P { get; set; }
    [Field(4)] public uint L { get; set; }
    [Field(5)] public uint Count { get; set; }
}

public sealed class MailEntry : IWireMessage
{
    [Field(1)] public ulong Mid { get; set; }
    [Field(2)] public uint Stat { get; set; }
    [Field(3)] public ulong Time { get; set; }
    [Field(4)] public ulong Expiration { get; set; }
}

public sealed class MailInfo : IWireMessage
{
    [Field(1)] public ulong Mid { get; set; }
    [Field(2)] public string Title { get; set; } = "";
    [Field(3)] public string Message { get; set; } = "";
    [Field(4)] public List<RewardItem> Attachments { get; set; } = [];
    [Field(5)] public string Sender { get; set; } = "";
    [Field(6)] public ulong Pid { get; set; }
    [Field(7)] public ulong Time { get; set; }
    [Field(8)] public ulong Expiration { get; set; }
    [Field(9)] public uint Life { get; set; }
    [Field(10)] public uint Stat { get; set; }
    [Field(99)] public bool IsDeleted { get; set; }
}

public sealed class FriendPieceEntry : IWireMessage
{
    [Field(1)] public uint Index { get; set; }
    [Field(2)] public ulong Pid { get; set; }
    [Field(3)] public uint Shape { get; set; }
    [Field(4)] public string Expr { get; set; } = "";
    [Field(5)] public bool Deleted { get; set; }
}

public sealed class OrderEntry : IWireMessage
{
    [Field(1)] public string TradeNo { get; set; } = "";
    [Field(2)] public string Subchannel { get; set; } = "";
    [Field(3)] public uint DoneTime { get; set; }
    [Field(4)] public uint RefundTime { get; set; }
    [Field(5)] public bool IsUnreal { get; set; }
    [Field(6)] public string Supplement { get; set; } = "";
}

public sealed class PlayerSnapshot : IWireMessage
{
    [Field(1)] public ulong Pid { get; set; }
    [Field(2)] public string Account { get; set; } = "";
    [Field(3)] public string Provider { get; set; } = "";
    [Field(4)] public uint Channel { get; set; }
    [Field(5)] public uint Subchannel { get; set; }
    [Field(11)] public string Name { get; set; } = "";
    [Field(12)] public string Sign { get; set; } = "";
    [Field(13)] public uint Sex { get; set; }
    [Field(14)] public uint Level { get; set; }
    [Field(15)] public ulong Exp { get; set; }
    [Field(16)] public uint Vigor { get; set; }
    [Field(17)] public Dictionary<string, int> Money { get; set; } = [];
    [Field(18)] public ulong Charged { get; set; }
    [Field(31)] public ulong CreateTime { get; set; }
    [Field(32)] public ulong LastLoginTime { get; set; }
    [Field(33)] public ulong LastVigorTime { get; set; }
    [Field(41)] public ulong ItemIdAlloc { get; set; }
    [Field(42)] public List<ItemInfo> Items { get; set; } = [];
    [Field(43)] public List<LineupInfo> Solutions { get; set; } = [];
    [Field(44)] public Dictionary<uint, uint> Attrs { get; set; } = [];
    [Field(45)] public Dictionary<uint, string> StrAttrs { get; set; } = [];
    [Field(46)] public List<ulong> ShowItems { get; set; } = [];
    [Field(47)] public List<uint> ShowAttrs { get; set; } = [];
    [Field(48)] public Dictionary<ulong, FriendPieceEntry> FriendPieces { get; set; } = [];
    [Field(49)] public List<ulong> LastPieces { get; set; } = [];
    [Field(50)] public Dictionary<ulong, MailEntry> MailBox { get; set; } = [];
    [Field(51)] public ulong LastGlobalMailTime { get; set; }
    [Field(52)] public ulong LastPersonMid { get; set; }
    [Field(53)] public List<ulong> Badges { get; set; } = [];
    [Field(60)] public Dictionary<string, OrderEntry> OrderBox { get; set; } = [];
    [Field(96)] public List<ulong> Tags { get; set; } = [];
    [Field(97)] public ulong Serial { get; set; }
    [Field(98)] public uint BanType { get; set; }
    [Field(99)] public string BanExpr { get; set; } = "";
}
