using System.Text.Json;
using SnowbreakTC.Protocol.Messages;
using System.Text.Json.Serialization;

namespace SnowbreakTC.GameLogic.Resources;

public sealed record GachaPoolRec
{
    [JsonPropertyName("ID")] public uint Id { get; init; }
    [JsonPropertyName("Type")] public uint Type { get; init; }
    [JsonPropertyName("Index")] public uint Index { get; init; }
    [JsonPropertyName("Coverage")] public uint Coverage { get; init; }
}

public sealed record ProbabilityRec
{
    [JsonPropertyName("ID")] public uint Id { get; init; }
    [JsonPropertyName("Rarity1")] public uint Rarity1 { get; init; }
    [JsonPropertyName("Rarity2")] public uint Rarity2 { get; init; }
    [JsonPropertyName("Rarity3")] public uint Rarity3 { get; init; }
    [JsonPropertyName("Rarity4")] public uint Rarity4 { get; init; }
    [JsonPropertyName("Rarity5")] public uint Rarity5 { get; init; }
    [JsonPropertyName("Rarity6")] public uint Rarity6 { get; init; }
}

public sealed record LevelLimitRec
{
    [JsonPropertyName("ID")] public uint Id { get; init; }
    [JsonPropertyName("Type")] public uint Type { get; init; }
    [JsonPropertyName("Limit")] public Dictionary<string, uint> Limit { get; init; } = [];
}

public static class GachaTable
{
    public const string Path = "gacha/gacha.json";

    public static List<GachaPoolRec>? Load(ResourceManager resources) =>
        resources.Table<GachaPoolRec>(Path);

    public static List<ProbabilityRec>? Probabilities(ResourceManager resources) =>
        resources.Table<ProbabilityRec>("gacha/probability.json");

    public static List<LevelLimitRec>? LevelLimits(ResourceManager resources) =>
        resources.Table<LevelLimitRec>("item/level_limit.json");

    public static uint LevelCap(ResourceManager? resources, uint breakLevel)
    {
        var table = LevelLimits(resources!);
        if (table == null || table.Count == 0)
            return 80;
        var rec = table.OrderByDescending(x => x.Limit.Count).FirstOrDefault();
        if (rec?.Limit == null || rec.Limit.Count == 0)
            return 80;
        uint cap = 20;
        foreach (var (key, value) in rec.Limit)
        {
            if (uint.TryParse(key, out var lvl) && breakLevel >= lvl && value > cap)
                cap = value;
        }
        return cap;
    }
}

public sealed record DropRec
{
    [JsonPropertyName("ID")] public uint Id { get; init; }
    [JsonPropertyName("Drop")] public List<List<uint>> Drop { get; init; } = [];
}

public sealed record DropGroupRec
{
    [JsonPropertyName("ID")] public uint Id { get; init; }
    [JsonPropertyName("Grop")] public List<JsonElement> Grop { get; init; } = [];
}

public static class DropTable
{
    public static List<DropRec>? Drops(ResourceManager resources) =>
        resources.Table<DropRec>("drop/drop.json");

    public static List<RewardItem>? Roll(ResourceManager resources, uint dropId, Random rng)
    {
        var rec = Drops(resources)?.FirstOrDefault(d => d.Id == dropId);
        if (rec == null)
            return null;
        var rewards = new List<RewardItem>();
        foreach (var entry in rec.Drop)
        {
            if (entry.Count >= 3)
                rewards.Add(new RewardItem { G = entry[0], D = entry[1], Count = entry[2] });
        }
        return rewards;
    }
}
