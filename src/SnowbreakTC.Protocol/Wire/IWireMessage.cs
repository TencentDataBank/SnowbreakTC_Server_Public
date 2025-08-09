namespace SnowbreakTC.Protocol.Wire;

public interface IWireMessage;

public static class WireExtensions
{
    public static byte[] Encode(this IWireMessage msg) => Codec.Encode(msg);

    public static void EncodeInto(this IWireMessage msg, WireBuffer buf) => Codec.EncodeTo(buf, msg);

    public static void DecodeInto<T>(this T msg, ReadOnlySpan<byte> data) where T : class, IWireMessage =>
        Codec.ReadInto(msg, typeof(T), data);
}
