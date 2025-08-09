using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using Xunit;

namespace SnowbreakTC.Tests;

public class CodecTests
{
    [Fact]
    public void SetAttr_EncodesExpectedWireBytes()
    {
        var msg = new NtfSetAttr { Gid = 1, Sid = 2, Val = 3 };
        var data = Codec.Encode(msg);
        Assert.Equal([0x08, 0x01, 0x10, 0x02, 0x18, 0x03], data);
    }

    [Fact]
    public void Snapshot_RoundTrips()
    {
        var snapshot = new PlayerSnapshot
        {
            Pid = 10001,
            Name = "Test",
            Level = 42,
            Exp = 123456,
            Sex = 1,
            Attrs = new Dictionary<uint, uint> { [10001] = 77, [65537] = 5 },
            StrAttrs = new Dictionary<uint, string> { [65537] = "hello" },
            Money = new Dictionary<string, int> { ["gold_box"] = 999 },
            Items =
            {
                new ItemInfo { Id = 1, Template = 1001, Count = 5, Enhance = new EnhanceInfo { Level = 3, Spines = [1, 2, 3] } },
                new ItemInfo { Id = 2, Template = 2002, Count = 1 },
            },
            Solutions = { new LineupInfo { Index = 0, Name = "main", Member1 = 1 } },
            ShowAttrs = [7, 8, 9],
        };

        var decoded = Codec.Decode<PlayerSnapshot>(Codec.Encode(snapshot));

        Assert.Equal(10001ul, decoded.Pid);
        Assert.Equal("Test", decoded.Name);
        Assert.Equal(42u, decoded.Level);
        Assert.Equal(123456ul, decoded.Exp);
        Assert.Equal(77u, decoded.Attrs[10001]);
        Assert.Equal(5u, decoded.Attrs[65537]);
        Assert.Equal("hello", decoded.StrAttrs[65537]);
        Assert.Equal(999, decoded.Money["gold_box"]);
        Assert.Equal(2, decoded.Items.Count);
        Assert.Equal(3u, decoded.Items[0].Enhance!.Level);
        Assert.Equal([1ul, 2, 3], decoded.Items[0].Enhance!.Spines);
        Assert.Equal("main", decoded.Solutions[0].Name);
        Assert.Equal([7u, 8, 9], decoded.ShowAttrs);
    }

    [Fact]
    public void ReqLogin_DecodesUnknownFieldsGracefully()
    {
        var payload = new byte[]
        {
            0x0A, 0x07, (byte)'c', (byte)'h', (byte)'a', (byte)'n', (byte)'n', (byte)'e', (byte)'l',
            0x12, 0x04, (byte)'a', (byte)'b', (byte)'c', (byte)'d',
            0x98, 0x06, 0x01,
        };
        var req = Codec.Decode<ReqLogin>(payload);
        Assert.Equal("channel", req.Provider);
        Assert.Equal("abcd", req.Token);
    }

    [Fact]
    public void Defaults_AreNotSerialized()
    {
        var msg = new NtfSetAttr();
        Assert.Empty(Codec.Encode(msg));
    }
}
