using SnowbreakTC.Protocol.Wire;

namespace SnowbreakTC.Protocol.Messages;

public enum ChatChannel
{
    System = 0,
    World = 1,
    Friend = 2,
    Online = 3,
}

public sealed class ChatMessage : IWireMessage
{
    [Field(1)] public ChatChannel Type { get; set; }
    [Field(2)] public uint ChannelId { get; set; }
    [Field(3)] public ulong Sender { get; set; }
    [Field(4)] public ulong Recver { get; set; }
    [Field(5)] public uint TimeStamp { get; set; }
    [Field(11)] public ulong Emoji { get; set; }
    [Field(12)] public string Text { get; set; } = "";
    [Field(21)] public PlayerProfileBrief? Profile { get; set; }
}

public sealed class PlayerProfileBrief : IWireMessage
{
    [Field(1)] public ulong Pid { get; set; }
    [Field(2)] public string Account { get; set; } = "";
    [Field(3)] public ulong CreateTime { get; set; }
    [Field(4)] public string Name { get; set; } = "";
    [Field(5)] public string Sign { get; set; } = "";
    [Field(6)] public uint Sex { get; set; }
    [Field(7)] public uint Level { get; set; }
    [Field(8)] public ulong LogoutTime { get; set; }
    [Field(9)] public uint FriendCount { get; set; }
    [Field(10)] public List<ulong> ShowItems { get; set; } = [];
    [Field(11)] public List<uint> ShowAttrs { get; set; } = [];
    [Field(12)] public List<ulong> Badges { get; set; } = [];
    [Field(13)] public List<ulong> Tags { get; set; } = [];
}

public sealed class NtfOnlineStartMsg : IWireMessage
{
    [Field(1)] public ulong RoomId { get; set; }
    [Field(2)] public string RoomAddr { get; set; } = "";
}
