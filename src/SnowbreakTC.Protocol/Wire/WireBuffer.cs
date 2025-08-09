using System.Text;

namespace SnowbreakTC.Protocol.Wire;

public sealed class WireBuffer
{
    private const int GrowthLimit = 1 << 22;

    private byte[] _buf = new byte[512];
    private int _len;

    public int Length => _len;

    public void Reserve(int extra)
    {
        if (_len + extra <= _buf.Length)
            return;
        var cap = _buf.Length;
        while (cap < _len + extra)
            cap = cap >= GrowthLimit ? cap + GrowthLimit : cap * 2;
        Array.Resize(ref _buf, cap);
    }

    public void WriteVarint(ulong value)
    {
        Reserve(10);
        while (value > 0x7FUL)
        {
            _buf[_len++] = (byte)(value | 0x80);
            value >>= 7;
        }
        _buf[_len++] = (byte)value;
    }

    public void WriteTag(int field, WireType type) => WriteVarint(((ulong)field << 3) | (byte)type);

    public void WriteFixed32(uint value)
    {
        Reserve(4);
        _buf[_len] = (byte)value;
        _buf[_len + 1] = (byte)(value >> 8);
        _buf[_len + 2] = (byte)(value >> 16);
        _buf[_len + 3] = (byte)(value >> 24);
        _len += 4;
    }

    public void WriteFixed64(ulong value)
    {
        Reserve(8);
        for (var i = 0; i < 8; i++)
            _buf[_len + i] = (byte)(value >> (i * 8));
        _len += 8;
    }

    public void WriteBytesValue(ReadOnlySpan<byte> data)
    {
        WriteVarint((ulong)data.Length);
        Reserve(data.Length);
        data.CopyTo(_buf.AsSpan(_len));
        _len += data.Length;
    }

    public void WriteString(string value)
    {
        var count = Encoding.UTF8.GetByteCount(value);
        WriteVarint((ulong)count);
        Reserve(count);
        Encoding.UTF8.GetBytes(value, _buf.AsSpan(_len));
        _len += count;
    }

    public void WriteMessage(Action<WireBuffer> body)
    {
        var tmp = new WireBuffer();
        body(tmp);
        WriteBytesValue(tmp.AsSpan());
    }

    public ReadOnlySpan<byte> AsSpan() => _buf.AsSpan(0, _len);

    public byte[] ToArray() => _buf.AsSpan(0, _len).ToArray();
}
