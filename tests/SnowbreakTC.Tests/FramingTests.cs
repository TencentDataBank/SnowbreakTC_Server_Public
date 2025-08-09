using SnowbreakTC.Server.Game;
using Xunit;

namespace SnowbreakTC.Tests;

public class FramingTests
{
    [Fact]
    public async Task PlainFrame_RoundTrips()
    {
        var frame = Framing.Encode(7, [0x01, 0x02, 0x03], FrameKind.Plain);
        using var ms = new MemoryStream(frame);
        var packet = await Framing.ReadAsync(ms, CancellationToken.None);

        Assert.NotNull(packet);
        Assert.Equal(FrameKind.Plain, packet.Kind);
        Assert.Equal(7, packet.Command);
        Assert.Equal([0x01, 0x02, 0x03], packet.Body);
    }

    [Fact]
    public async Task WrappedFrame_RoundTrips()
    {
        var body = new byte[32];
        Random.Shared.NextBytes(body);
        var frame = Framing.Encode(1001, body, FrameKind.Wrapped);
        using var ms = new MemoryStream(frame);
        var packet = await Framing.ReadAsync(ms, CancellationToken.None);

        Assert.NotNull(packet);
        Assert.Equal(FrameKind.Wrapped, packet.Kind);
        Assert.Equal(1001, packet.Command);
        Assert.Equal(body, packet.Body);
    }

    [Fact]
    public async Task ControlPacket_IsDetected()
    {
        var frame = new byte[4 + 35];
        frame[0] = 0x01;
        frame[1] = 0x1F;
        using var ms = new MemoryStream(frame);
        var packet = await Framing.ReadAsync(ms, CancellationToken.None);

        Assert.NotNull(packet);
        Assert.Equal(FrameKind.Control, packet.Kind);
    }

    [Fact]
    public async Task LargeBody_IsCompressedTransparently()
    {
        var body = new byte[CompressThreshold + 100];
        Array.Fill(body, (byte)0x41);
        var frame = Framing.Encode(1234, body, FrameKind.Wrapped);
        Assert.True(frame.Length < 4 + 35 + body.Length);

        using var ms = new MemoryStream(frame);
        var packet = await Framing.ReadAsync(ms, CancellationToken.None);
        Assert.NotNull(packet);
        Assert.Equal(body, packet.Body);
    }

    private const int CompressThreshold = 60000;
}
