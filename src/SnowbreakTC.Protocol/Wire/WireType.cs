namespace SnowbreakTC.Protocol.Wire;

public enum WireType : byte
{
    Varint = 0,
    Fixed64 = 1,
    Bytes = 2,
    Fixed32 = 5,
}
