using SnowbreakTC.Protocol.Wire;

namespace SnowbreakTC.Protocol.Messages;

public sealed class ReqCallGs : IWireMessage
{
    [Field(1)] public string Api { get; set; } = "";
    [Field(2)] public string Param { get; set; } = "";
    [Field(3)] public uint ClickNum { get; set; }
    [Field(4)] public List<string> DependentParams { get; set; } = [];
}

public sealed class NtfCallScript : IWireMessage
{
    [Field(1)] public string Api { get; set; } = "";
    [Field(2)] public string Arg { get; set; } = "";
    [Field(3)] public NtfSyncPlayer? ExtraSync { get; set; }
    [Field(200)] public ulong Seed { get; set; }
}
