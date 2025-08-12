using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

[Route("ShopLogic_GetGoodsList")]
[Route("ShopLogic_GetOpenTime")]
public sealed class ShopGetGoodsV2 : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var tabs = ReadTable(session, "shop/shop_tab.json");
        var arg = JsonSerializer.Serialize(new
        {
            goods = JsonSerializer.Deserialize<JsonElement>(ShopSystemExt.GoodsListJson(session.Server.Resources) ?? "{}").GetProperty("goods"),
            shop_tab = tabs,
        });
        return Task.FromResult(new CallGsReply(arg));
    }

    private static JsonElement ReadTable(GameSession session, string path)
    {
        var file = Path.Combine(session.Server.Resources.Root, path.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(file)
            ? JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(file))
            : JsonSerializer.Deserialize<JsonElement>("[]");
    }
}

[Route("ShopLogic_BuyGoods")]
[Route("IBLogic_BuyGoods")]
public sealed class ShopBuyGoodsV2 : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var goodsId = root.Find("goods_id", "goodsid", "id").Uint();
        if (goodsId is not { } id)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var result = player.BuyGoods(session.Server.Resources, id);
        if (result == null)
            return Task.FromResult(CallGsReply.Error("tip.money_not_enough"));
        player.Save();
        var unpacked = ItemGenres.Unpack(result.Item.Template);
        var arg = JsonSerializer.Serialize(new
        {
            goods_id = id,
            item = new { id = result.Item.Id, g = unpacked.Genre, d = unpacked.Detail, p = unpacked.Particular, count = result.Item.Count },
            price = new { g = result.Price.G, d = result.Price.D, count = result.Price.Count },
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("ShopLogic_UpdateShop")]
[Route("ShopLogic_ConvertGirlPieces")]
public sealed class ShopMiscV2 : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}

public sealed class SupportCardState
{
    public uint AffixSeed { get; set; } = 1;
}

public static class AffixCostExt
{
    public static CallGsReply? SpendAffixCost(this PlayerInstance player, ResourceManager resources)
    {
        var raw = resources.Raw("item/templates/support_card.json");
        if (raw == null)
            return null;
        List<uint>? cost = null;
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (row.TryGetProperty("AffixCost", out var c) && c.ValueKind == JsonValueKind.Array)
            {
                cost = c.EnumerateArray().Select(e => e.TryGetUInt32(out var v) ? v : 0).ToList();
                break;
            }
        }
        if (cost is not { Count: >= 5 } price)
            return null;
        var (g, d, count) = (price[0], price[1], price[4]);
        if (player.ConsumeCurrency(d, count))
        {
            player.Save();
            return null;
        }
        if (player.Inventory.Consume(g * 1000UL + d, count))
        {
            player.Save();
            return null;
        }
        return CallGsReply.Error("tip.material_not_enough");
    }
}

public static class SupportCardExt
{
    public const string StateKey = "support_state";

    public static uint NextAffixSeed(this PlayerInstance player)
    {
        var state = player.LoadDict(StateKey);
        var seed = state.GetValueOrDefault("affix_seed", 1) + 1;
        player.SetExtra(new Dictionary<string, long>(state) { ["affix_seed"] = seed });
        return (uint)seed;
    }
}

[Route("SupporterCard_Equip")]
public sealed class SupporterEquip : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var cardGuid = root.Find("girl_id", "card_id", "id").Ulong();
        var supportGuid = root.Find("support_id", "supporter_id", "guid").Ulong();
        if (cardGuid is not { } c || supportGuid is not { } s)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var card = player.Characters.FindByGuid(c);
        var support = player.Inventory.Find(s);
        if (card?.Enhance == null || support == null || ItemGenres.Unpack(support.Template).Genre != 3)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        card.Slots[2] = s;
        player.Save();
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("SupporterCard_Upgrade")]
public sealed class SupporterUpgrade : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong() ?? 0;
        var support = player.Inventory.Find(guid);
        if (support?.Enhance == null || ItemGenres.Unpack(support.Template).Genre != 3)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var cost = 1000 * (support.Enhance.Level + 1);
        if (!player.ConsumeCurrency(AttrIds.Currency.Silver, (uint)cost))
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        support.Enhance.Level++;
        player.Save();
        var arg = JsonSerializer.Serialize(new { id = guid, level = support.Enhance.Level });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("SupporterCard_SelectAffix")]
[Route("SupporterCard_ResetAffix")]
[Route("SupporterCard_SelectInitialAffix")]
[Route("SupporterCard_ResetInitialAffix")]
public sealed class SupporterAffix : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong() ?? 0;
        var support = player.Inventory.Find(guid);
        if (support?.Enhance == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var costFail = player.SpendAffixCost(session.Server.Resources);
        if (costFail != null)
            return Task.FromResult(costFail);
        var seed = player.NextAffixSeed();
        var affix = 1000 + seed % 900;
        support.Enhance.Affixes.Add(affix);
        player.Save();
        var arg = JsonSerializer.Serialize(new { id = guid, affix });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("SupporterCard_FixedResetInitialAffix")]
[Route("SupporterCard_ReceiveFixedItem")]
public sealed class SupporterMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}
