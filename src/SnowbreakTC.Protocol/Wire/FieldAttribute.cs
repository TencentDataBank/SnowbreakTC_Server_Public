namespace SnowbreakTC.Protocol.Wire;

[AttributeUsage(AttributeTargets.Property)]
public sealed class FieldAttribute : Attribute
{
    public int Number { get; }

    public FieldAttribute(int number) => Number = number;
}
