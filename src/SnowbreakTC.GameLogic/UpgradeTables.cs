using System.Text.Json.Serialization;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed record UpgradeExpRec
{
    [JsonPropertyName("Lv")] public uint Lv { get; init; }
    [JsonPropertyName("CardNeedExp")] public uint CardNeedExp { get; init; }
    [JsonPropertyName("SSRCardNeedExp")] public uint SsrCardNeedExp { get; init; }
    [JsonPropertyName("SusNeedExp")] public uint SusNeedExp { get; init; }
    [JsonPropertyName("SSRSusNeedExp")] public uint SsrSusNeedExp { get; init; }
    [JsonPropertyName("WeaponNeedExp")] public uint WeaponNeedExp { get; init; }
    [JsonPropertyName("SSRWeaponNeedExp")] public uint SsrWeaponNeedExp { get; init; }
}

public sealed record SuppliesRec
{
    [JsonPropertyName("Genre")] public uint Genre { get; init; }
    [JsonPropertyName("Detail")] public uint Detail { get; init; }
    [JsonPropertyName("Particular")] public uint Particular { get; init; }
    [JsonPropertyName("ProvideExp")] public uint ProvideExp { get; init; }
    [JsonPropertyName("ConsumeGold")] public uint ConsumeGold { get; init; }
    [JsonPropertyName("Level")] public uint Level { get; init; }
}

public static class UpgradeTable
{
    public static List<UpgradeExpRec>? ExpCurve(ResourceManager resources) =>
        resources.Table<UpgradeExpRec>("item/upgrade_exp.json");

    public static List<SuppliesRec>? Supplies(ResourceManager resources) =>
        resources.Table<SuppliesRec>("item/templates/suplies.json");

    public static SuppliesRec? FindSupply(ResourceManager resources, ulong template)
    {
        var (_, detail, particular, _) = ItemGenres.Unpack(template);
        return Supplies(resources)?.FirstOrDefault(s => s.Genre == ItemGenres.Supplies && s.Detail == detail && s.Particular == particular);
    }

    public static uint NeedExp(ResourceManager resources, uint level, bool ssr, byte family) =>
        ExpCurve(resources)?.FirstOrDefault(x => x.Lv == level) is { } row
            ? family switch
            {
                0 => ssr ? row.SsrCardNeedExp : row.CardNeedExp,
                1 => ssr ? row.SsrWeaponNeedExp : row.WeaponNeedExp,
                _ => ssr ? row.SsrSusNeedExp : row.SusNeedExp,
            }
            : 100;
}

public sealed record MaterialCost(List<(ulong Guid, uint Count)> Consumed, ulong Exp, ulong Silver);

public static class CharacterUpgradeExt
{
    public static MaterialCost? PlanUpgrade(this PlayerInstance player, ResourceManager resources,
        ulong targetGuid, IEnumerable<(ulong Guid, uint Count)> materials)
    {
        var target = player.Characters.FindByGuid(targetGuid);
        if (target?.Enhance == null)
            return null;
        var (genre, _, _, _) = ItemGenres.Unpack(target.Template);
        var family = genre == ItemGenres.Card ? (byte)0 : genre == ItemGenres.Weapon ? (byte)1 : (byte)2;
        var ssr = target.Enhance.Evolue >= 3 || target.Template >> 32 >= 4;
        var supplyLookup = UpgradeTable.Supplies(resources);
        var exp = 0UL;
        var silver = 0UL;
        var consumed = new List<(ulong, uint)>();
        foreach (var (guid, count) in materials)
        {
            var item = player.Inventory.Find(guid);
            if (item == null || item.Count < count || count == 0)
                return null;
            var supply = supplyLookup?.FirstOrDefault(s =>
                s.Genre == ItemGenres.Unpack(item.Template).Genre
                && s.Detail == ItemGenres.Unpack(item.Template).Detail
                && s.Particular == ItemGenres.Unpack(item.Template).Particular);
            if (supply is not { ProvideExp: > 0 })
                return null;
            exp += (ulong)supply.ProvideExp * count;
            silver += (ulong)supply.ConsumeGold * count;
            consumed.Add((guid, count));
        }
        return new MaterialCost(consumed, exp, silver);
    }

    public static uint ApplyUpgrade(this PlayerInstance player, ResourceManager resources,
        ulong targetGuid, MaterialCost plan, uint cap)
    {
        var target = player.Characters.FindByGuid(targetGuid)!;
        var enhance = target.Enhance ??= new();
        var (genre, _, _, _) = ItemGenres.Unpack(target.Template);
        var family = genre == ItemGenres.Card ? (byte)0 : genre == ItemGenres.Weapon ? (byte)1 : (byte)2;
        var ssr = enhance.Evolue >= 3 || target.Template >> 32 >= 4;
        var exp = (ulong)enhance.Exp + plan.Exp;
        while (enhance.Level < cap)
        {
            var need = UpgradeTable.NeedExp(resources, enhance.Level, ssr, family);
            if (exp < need)
                break;
            exp -= need;
            enhance.Level++;
        }
        enhance.Exp = (uint)Math.Min(uint.MaxValue, exp);
        player.SetCurrency(AttrIds.Currency.Silver, player.GetCurrency(AttrIds.Currency.Silver) - (uint)Math.Min(plan.Silver, player.GetCurrency(AttrIds.Currency.Silver)));
        foreach (var (guid, count) in plan.Consumed)
        {
            var item = player.Inventory.Find(guid);
            if (item != null)
            {
                item.Count -= count;
                if (item.Count == 0)
                    player.Inventory.Remove(guid);
            }
        }
        return target.Enhance.Level;
    }
}
