using SnowbreakTC.Protocol.Wire;

namespace SnowbreakTC.Protocol.Messages;

public sealed class ReqLogin : IWireMessage
{
    [Field(1)] public string Provider { get; set; } = "";
    [Field(2)] public string Token { get; set; } = "";
    [Field(3)] public ClientProfile? Profile { get; set; }
}

public sealed class RspLogin : IWireMessage
{
    [Field(1)] public string SessionId { get; set; } = "";
    [Field(2)] public PlayerSnapshot? Data { get; set; }
    [Field(3)] public bool NeedRename { get; set; }
    [Field(4)] public uint AreaId { get; set; }
    [Field(5)] public int TimeZone { get; set; }
    [Field(6)] public uint Timestamp { get; set; }
    [Field(7)] public int Certification { get; set; }
    [Field(8)] public Dictionary<string, uint> GlobalAttrs { get; set; } = [];
    [Field(9)] public uint WorldChannel { get; set; }
    [Field(10)] public Dictionary<string, string> GlobalStrAttrs { get; set; } = [];
    [Field(98)] public uint ErrorCode { get; set; }
    [Field(99)] public List<uint> ErrorInfo { get; set; } = [];
    [Field(200)] public ulong Seed { get; set; }
}

public sealed class ReqAccountInfo : IWireMessage
{
    [Field(1)] public string Provider { get; set; } = "";
    [Field(2)] public string Token { get; set; } = "";
}

public sealed class AccountInfo : IWireMessage
{
    [Field(1)] public string Account { get; set; } = "";
    [Field(2)] public ulong Pid { get; set; }
    [Field(3)] public uint NewGuide { get; set; }
    [Field(4)] public List<uint> ErrorInfo { get; set; } = [];
}

public sealed class ReqReconnect : IWireMessage
{
    [Field(1)] public ulong Pid { get; set; }
    [Field(2)] public string SessionId { get; set; } = "";
    [Field(3)] public uint WorldChannel { get; set; }
    [Field(4)] public string Language { get; set; } = "";
}

public sealed class RspReconnect : IWireMessage
{
    [Field(1)] public string SessionId { get; set; } = "";
    [Field(2)] public PlayerSnapshot? Data { get; set; }
    [Field(3)] public bool NeedRename { get; set; }
    [Field(4)] public int TimeZone { get; set; }
    [Field(5)] public uint Timestamp { get; set; }
    [Field(6)] public uint WorldChannel { get; set; }
}

public sealed class ReqRename : IWireMessage
{
    [Field(1)] public string Data { get; set; } = "";
}
