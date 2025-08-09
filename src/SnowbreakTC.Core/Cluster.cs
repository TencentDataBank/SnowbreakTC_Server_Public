namespace SnowbreakTC.Core;

/// <summary>
/// Deployment identity shared by every node built from this codebase: the cluster
/// tag, schema stamp and the per-account seed constants derived from it.
/// </summary>
public static class Cluster
{
    public const string Tag = "SnowbreakTC";
    public const string NodeTag = "4E1F7A2C";
    public const string Node = Tag + "/" + NodeTag;
    public const string CertOu = "SnowbreakTC Cluster";
    public const int SchemaVersion = unchecked((int)0x53544331);
    public const uint SeedGid = 255;
    public const uint SeedSid = 1;
    public const uint SeedValue = 0x53544331;
}
