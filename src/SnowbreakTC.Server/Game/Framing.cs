using System.IO.Compression;
using System.Text;

namespace SnowbreakTC.Server.Game;

public enum FrameKind
{
    Plain,
    Wrapped,
    Control,
}

public sealed record RawPacket(ushort Command, byte[] Body, FrameKind Kind);

public static class Framing
{
    private const ushort Magic = 0x011F;
    private const int WrapperSize = 35;
    private const int MaxBody = 1 << 20;
    private const int CompressThreshold = 60000;
    private const byte ZlibFlag = 2;

    public static async Task<RawPacket?> ReadAsync(Stream stream, CancellationToken ct)
    {
        var head = new byte[4];
        if (!await ReadExact(stream, head, ct))
            return null;

        var magic = (ushort)((head[0] << 8) | head[1]);
        if (magic == Magic)
        {
            var cmd = BitConverter.ToUInt16(head, 2);
            var wrapper = new byte[WrapperSize];
            if (cmd == 0)
            {
                if (!await ReadExact(stream, wrapper, ct))
                    return null;
                return new RawPacket(0, [], FrameKind.Control);
            }
            if (!await ReadExact(stream, wrapper, ct))
                return null;
            var len = BitConverter.ToUInt16(wrapper, 6);
            var body = new byte[len];
            if (len > 0 && !await ReadExact(stream, body, ct))
                return null;
            if (wrapper[10] == ZlibFlag && len >= 2 && body[0] == 0x78)
                body = Decompress(body);
            return new RawPacket(cmd, body, FrameKind.Wrapped);
        }

        var total = BitConverter.ToUInt32(head, 0);
        if (total is < 2 or > MaxBody + 2)
            return null;
        var frame = new byte[total];
        if (!await ReadExact(stream, frame, ct))
            return null;
        return new RawPacket(BitConverter.ToUInt16(frame, 0), frame[2..], FrameKind.Plain);
    }

    public static byte[] Encode(ushort cmd, byte[] body, FrameKind kind) =>
        kind == FrameKind.Wrapped ? EncodeWrapped(cmd, body) : EncodePlain(cmd, body);

    public static byte[] EncodePlain(ushort cmd, byte[] body)
    {
        var frame = new byte[6 + body.Length];
        BitConverter.GetBytes((uint)(2 + body.Length)).CopyTo(frame, 0);
        BitConverter.GetBytes(cmd).CopyTo(frame, 4);
        body.CopyTo(frame, 6);
        return frame;
    }

    public static byte[] EncodeWrapped(ushort cmd, byte[] body)
    {
        var payload = body.Length > CompressThreshold ? Compress(body) : body;
        var frame = new byte[4 + WrapperSize + payload.Length];
        BitConverter.GetBytes(Magic).Reverse().ToArray().CopyTo(frame, 0);
        BitConverter.GetBytes(cmd).CopyTo(frame, 2);
        BitConverter.GetBytes((ushort)payload.Length).CopyTo(frame, 4 + 6);
        if (payload.Length >= 2 && payload[0] == 0x78 && payload[1] is 0x01 or 0x5E or 0x9C or 0xDA)
            frame[4 + 10] = ZlibFlag;
        payload.CopyTo(frame, 4 + WrapperSize);
        return frame;
    }

    private static byte[] Compress(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(data);
        return ms.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var src = new MemoryStream(data);
        using var zlib = new ZLibStream(src, CompressionMode.Decompress);
        using var ms = new MemoryStream();
        zlib.CopyTo(ms);
        return ms.ToArray();
    }

    private static async Task<bool> ReadExact(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(offset), ct);
            if (n == 0)
                return false;
            offset += n;
        }
        return true;
    }
}
