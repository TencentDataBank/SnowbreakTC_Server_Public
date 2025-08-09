using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace SnowbreakTC.Protocol.Wire;

public static class Codec
{
    private static readonly ConcurrentDictionary<Type, FieldPlan[]> Plans = new();

    public static byte[] Encode(object msg)
    {
        var buf = new WireBuffer();
        EncodeTo(buf, msg);
        return buf.ToArray();
    }

    public static void EncodeTo(WireBuffer buf, object msg)
    {
        foreach (var f in Plans.GetOrAdd(msg.GetType(), static t => BuildPlan(t)))
            f.Write(buf, msg);
    }

    public static T Decode<T>(byte[] data) where T : class, IWireMessage, new()
    {
        var msg = new T();
        ReadInto(msg, typeof(T), data);
        return msg;
    }

    public static void ReadInto(object msg, Type type, ReadOnlySpan<byte> data)
    {
        var r = new WireReader(data);
        var fields = Plans.GetOrAdd(type, static t => BuildPlan(t));
        while (r.ReadTag(out var field, out var wt))
        {
            var f = fields.FirstOrDefault(x => x.Number == field);
            if (f == null)
            {
                r.Skip(wt);
                continue;
            }
            f.Read(msg, ref r, wt);
        }
    }

    private static FieldPlan[] BuildPlan(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (p, attr: p.GetCustomAttribute<FieldAttribute>()))
            .Where(x => x.attr != null)
            .Select(x => new FieldPlan(x.attr!.Number, x.p))
            .OrderBy(f => f.Number)
            .ToArray();
    }
}

internal enum Kind
{
    Varint,
    Fixed32,
    Fixed64,
    Str,
    Bytes,
    Msg,
    PackedVarint,
    PackedFixed32,
    PackedFixed64,
    StrList,
    MsgList,
    Map,
}

internal sealed class FieldPlan
{
    public readonly int Number;

    private readonly Kind _kind;
    private readonly PropertyInfo _prop;

    private readonly Func<object, object?> _get;
    private readonly Action<object, object?> _set;

    private readonly Type _elemType;
    private readonly Type _keyType;
    private readonly Type _valueType;
    private readonly Kind _keyKind;
    private readonly Kind _valueKind;

    public FieldPlan(int number, PropertyInfo prop)
    {
        Number = number;
        _prop = prop;

        var keyType = typeof(uint);
        var valueType = typeof(uint);
        var keyKind = Kind.Varint;
        var valueKind = Kind.Varint;
        var elemType = prop.PropertyType;
        _kind = Infer(prop.PropertyType, ref keyType, ref valueType, ref keyKind, ref valueKind, ref elemType);
        _keyType = keyType;
        _valueType = valueType;
        _keyKind = keyKind;
        _valueKind = valueKind;
        _elemType = elemType;

        _get = prop.GetValue!;
        _set = prop.SetValue!;
    }

    public void Write(WireBuffer buf, object owner)
    {
        var value = _get(owner);
        switch (_kind)
        {
            case Kind.Varint:
                var vi = ToUlong(value);
                if (vi == 0)
                    return;
                buf.WriteTag(Number, WireType.Varint);
                buf.WriteVarint(vi);
                return;

            case Kind.Fixed32:
                var f32 = (float)value!;
                if (f32 == 0f)
                    return;
                buf.WriteTag(Number, WireType.Fixed32);
                buf.WriteFixed32(BitConverter.SingleToUInt32Bits(f32));
                return;

            case Kind.Fixed64:
                var f64 = (double)value!;
                if (f64 == 0d)
                    return;
                buf.WriteTag(Number, WireType.Fixed64);
                buf.WriteFixed64(BitConverter.DoubleToUInt64Bits(f64));
                return;

            case Kind.Str:
                var s = (string)value!;
                if (s.Length == 0)
                    return;
                buf.WriteTag(Number, WireType.Bytes);
                buf.WriteString(s);
                return;

            case Kind.Bytes:
                var b = (byte[]?)value;
                if (b is not { Length: > 0 })
                    return;
                buf.WriteTag(Number, WireType.Bytes);
                buf.WriteBytesValue(b);
                return;

            case Kind.Msg:
                if (value == null)
                    return;
                buf.WriteTag(Number, WireType.Bytes);
                buf.WriteMessage(inner => Codec.EncodeTo(inner, value!));
                return;

            case Kind.PackedVarint:
            {
                if (value is not IList list || list.Count == 0)
                    return;
                var tmp = new WireBuffer();
                foreach (var item in list)
                    tmp.WriteVarint(ToUlong(item));
                buf.WriteTag(Number, WireType.Bytes);
                buf.WriteBytesValue(tmp.AsSpan());
                return;
            }

            case Kind.PackedFixed32:
            {
                if (value is not IList list || list.Count == 0)
                    return;
                var tmp = new WireBuffer();
                foreach (var item in list)
                    tmp.WriteFixed32(BitConverter.SingleToUInt32Bits((float)item!));
                buf.WriteTag(Number, WireType.Bytes);
                buf.WriteBytesValue(tmp.AsSpan());
                return;
            }

            case Kind.PackedFixed64:
            {
                if (value is not IList list || list.Count == 0)
                    return;
                var tmp = new WireBuffer();
                foreach (var item in list)
                    tmp.WriteFixed64(BitConverter.DoubleToUInt64Bits((double)item!));
                buf.WriteTag(Number, WireType.Bytes);
                buf.WriteBytesValue(tmp.AsSpan());
                return;
            }

            case Kind.StrList:
            {
                if (value is not IList list)
                    return;
                foreach (var item in list)
                {
                    buf.WriteTag(Number, WireType.Bytes);
                    buf.WriteString((string)item!);
                }
                return;
            }

            case Kind.MsgList:
            {
                if (value is not IList list)
                    return;
                foreach (var item in list)
                {
                    buf.WriteTag(Number, WireType.Bytes);
                    buf.WriteMessage(inner => Codec.EncodeTo(inner, item!));
                }
                return;
            }

            case Kind.Map:
            {
                if (value is not IDictionary dict || dict.Count == 0)
                    return;
                foreach (DictionaryEntry entry in dict)
                {
                    buf.WriteTag(Number, WireType.Bytes);
                    buf.WriteMessage(inner =>
                    {
                        inner.WriteTag(1, WireTypeOf(_keyKind));
                        WriteScalar(inner, _keyKind, entry.Key);
                        inner.WriteTag(2, WireTypeOf(_valueKind));
                        WriteScalar(inner, _valueKind, entry.Value);
                    });
                }
                return;
            }
        }
    }

    public void Read(object owner, ref WireReader r, WireType wt)
    {
        switch (_kind)
        {
            case Kind.Varint:
                _set(owner, ToScalar(r.ReadVarint(), _prop.PropertyType));
                return;

            case Kind.Fixed32:
                _set(owner, r.ReadFloat());
                return;

            case Kind.Fixed64:
                _set(owner, r.ReadDouble());
                return;

            case Kind.Str:
                _set(owner, r.ReadString());
                return;

            case Kind.Bytes:
                _set(owner, r.ReadBytes().ToArray());
                return;

            case Kind.Msg:
            {
                var instance = Activator.CreateInstance(_prop.PropertyType)!;
                Codec.ReadInto(instance, _prop.PropertyType, r.ReadBytes());
                _set(owner, instance);
                return;
            }

            case Kind.PackedVarint:
            {
                var list = EnsureList(owner);
                if (wt == WireType.Varint)
                {
                    list.Add(ToScalar(r.ReadVarint(), _elemType));
                    return;
                }
                var inner = new WireReader(r.ReadBytes());
                while (!inner.Eof)
                    list.Add(ToScalar(inner.ReadVarint(), _elemType));
                return;
            }

            case Kind.PackedFixed32:
            {
                var list = EnsureList(owner);
                var payload = r.ReadBytes();
                for (var i = 0; i + 4 <= payload.Length; i += 4)
                    list.Add(BitConverter.UInt32BitsToSingle(BitConverter.ToUInt32(payload[i..])));
                return;
            }

            case Kind.PackedFixed64:
            {
                var list = EnsureList(owner);
                var payload = r.ReadBytes();
                for (var i = 0; i + 8 <= payload.Length; i += 8)
                    list.Add(BitConverter.Int64BitsToDouble(BitConverter.ToInt64(payload[i..])));
                return;
            }

            case Kind.StrList:
                EnsureList(owner).Add(r.ReadString());
                return;

            case Kind.MsgList:
            {
                var instance = Activator.CreateInstance(_elemType)!;
                Codec.ReadInto(instance, _elemType, r.ReadBytes());
                EnsureList(owner).Add(instance);
                return;
            }

            case Kind.Map:
            {
                var dict = EnsureDict(owner);
                var inner = new WireReader(r.ReadBytes());
                object? key = null;
                object? value = null;
                while (inner.ReadTag(out var f, out var fwt))
                {
                    if (f == 1)
                        key = ReadScalar(ref inner, _keyKind, _keyType, fwt);
                    else if (f == 2)
                        value = ReadScalar(ref inner, _valueKind, _valueType, fwt);
                    else
                        inner.Skip(fwt);
                }
                if (key != null)
                    dict[key] = value;
                return;
            }
        }
    }

    private IList EnsureList(object owner)
    {
        if (_get(owner) is IList list)
            return list;
        list = (IList)Activator.CreateInstance(_prop.PropertyType)!;
        _set(owner, list);
        return list;
    }

    private IDictionary EnsureDict(object owner)
    {
        if (_get(owner) is IDictionary dict)
            return dict;
        dict = (IDictionary)Activator.CreateInstance(_prop.PropertyType)!;
        _set(owner, dict);
        return dict;
    }

    private static void WriteScalar(WireBuffer buf, Kind kind, object? value)
    {
        switch (kind)
        {
            case Kind.Str:
                buf.WriteString((string)value!);
                break;
            case Kind.Msg:
                buf.WriteMessage(inner => Codec.EncodeTo(inner, value!));
                break;
            case Kind.Fixed32:
                buf.WriteFixed32(BitConverter.SingleToUInt32Bits((float)value!));
                break;
            case Kind.Fixed64:
                buf.WriteFixed64(BitConverter.DoubleToUInt64Bits((double)value!));
                break;
            default:
                buf.WriteVarint(ToUlong(value));
                break;
        }
    }

    private static object? ReadScalar(ref WireReader r, Kind kind, Type type, WireType wt)
    {
        switch (kind)
        {
            case Kind.Str:
                return r.ReadString();
            case Kind.Fixed32:
                return r.ReadFloat();
            case Kind.Fixed64:
                return r.ReadDouble();
            case Kind.Msg:
                var instance = Activator.CreateInstance(type)!;
                Codec.ReadInto(instance, type, r.ReadBytes());
                return instance;
            default:
                return ToScalar(r.ReadVarint(), type);
        }
    }

    private static ulong ToUlong(object? value) => value switch
    {
        null => 0,
        ulong u => u,
        long l => unchecked((ulong)l),
        uint u32 => u32,
        int i32 => unchecked((ulong)i32),
        bool b => b ? 1UL : 0UL,
        Enum e => Convert.ToUInt64(e),
        ushort us => us,
        short s => unchecked((ulong)s),
        byte by => by,
        sbyte sb => unchecked((ulong)sb),
        _ => Convert.ToUInt64(value),
    };

    private static object ToScalar(ulong value, Type type)
    {
        if (type == typeof(ulong)) return value;
        if (type == typeof(long)) return unchecked((long)value);
        if (type == typeof(uint)) return (uint)value;
        if (type == typeof(int)) return unchecked((int)value);
        if (type == typeof(bool)) return value != 0;
        if (type.IsEnum) return Enum.ToObject(type, value);
        return Convert.ChangeType(value, type);
    }

    private static WireType WireTypeOf(Kind kind) => kind switch
    {
        Kind.Fixed32 => WireType.Fixed32,
        Kind.Fixed64 => WireType.Fixed64,
        Kind.Str or Kind.Bytes or Kind.Msg => WireType.Bytes,
        _ => WireType.Varint,
    };

    private static Kind Infer(Type type, ref Type keyType, ref Type valueType, ref Kind keyKind, ref Kind valueKind, ref Type elemType)
    {
        if (type == typeof(string)) return Kind.Str;
        if (type == typeof(byte[])) return Kind.Bytes;
        if (type == typeof(float)) return Kind.Fixed32;
        if (type == typeof(double)) return Kind.Fixed64;
        if (type == typeof(bool) || type.IsEnum || (!type.IsClass && !type.IsInterface)) return Kind.Varint;

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();

            if (def == typeof(List<>))
            {
                elemType = args[0];
                if (elemType == typeof(string)) return Kind.StrList;
                if (typeof(IWireMessage).IsAssignableFrom(elemType)) return Kind.MsgList;
                if (elemType == typeof(float)) return Kind.PackedFixed32;
                if (elemType == typeof(double)) return Kind.PackedFixed64;
                return Kind.PackedVarint;
            }

            if (def == typeof(Dictionary<,>))
            {
                keyType = args[0];
                valueType = args[1];
                keyKind = Infer(args[0], ref keyType, ref valueType, ref keyKind, ref valueKind, ref elemType!);
                valueKind = Infer(args[1], ref keyType, ref valueType, ref keyKind, ref valueKind, ref elemType!);
                return Kind.Map;
            }
        }

        return Kind.Msg;
    }
}
