using System.Text;

namespace SnowbreakTC.Protocol.Wire;

public ref struct WireReader
{
    private ReadOnlySpan<byte> _data;

    public WireReader(ReadOnlySpan<byte> data) => _data = data;

    public bool Eof => _data.IsEmpty;

    public bool ReadTag(out int field, out WireType type)
    {
        if (_data.IsEmpty)
        {
            field = 0;
            type = 0;
            return false;
        }
        var tag = ReadVarint();
        field = (int)(tag >> 3);
        type = (WireType)(tag & 7);
        return field != 0;
    }

    public ulong ReadVarint()
    {
        ulong value = 0;
        var shift = 0;
        while (true)
        {
            var b = _data[0];
            _data = _data[1..];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                return value;
            shift += 7;
        }
    }

    public float ReadFloat()
    {
        var v = BitConverter.UInt32BitsToSingle(BitConverter.ToUInt32(_data));
        _data = _data[4..];
        return v;
    }

    public double ReadDouble()
    {
        var v = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(_data));
        _data = _data[8..];
        return v;
    }

    public ReadOnlySpan<byte> ReadBytes()
    {
        var len = (int)ReadVarint();
        var v = _data[..len];
        _data = _data[len..];
        return v;
    }

    public string ReadString() => Encoding.UTF8.GetString(ReadBytes());

    public void Skip(WireType type)
    {
        switch (type)
        {
            case WireType.Varint: ReadVarint(); break;
            case WireType.Fixed64: _data = _data[8..]; break;
            case WireType.Fixed32: _data = _data[4..]; break;
            case WireType.Bytes: ReadBytes(); break;
        }
    }
}
