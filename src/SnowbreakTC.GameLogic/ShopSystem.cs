using System.Text.Json.Serialization;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed record ShopGoodsRec
{
    [JsonPropertyName("ShopId")] public uint ShopId { get; init; }
    [JsonPropertyName("GoodsId")] public uint GoodsId { get; init; }
    [JsonPropertyName("GDPLN")] public List<uint> Gdpln { get; init; } = [];
    [JsonPropertyName("Price1")] public List<uint> Price1 { get; init; } = [];
    [JsonPropertyName("LimitType")] public uint LimitType { get; init; }
    [JsonPropertyName("LimitNum")] public uint LimitNum { get; init; }
    [JsonPropertyName("Class")] public string Class { get; init; } = "";
}

public static class ShopTable
{
    public static List<ShopGoodsRec>? Load(ResourceManager resources) =>
        resources.Table<ShopGoodsRec>("shop/goods.json");

    public static ShopGoodsRec? Find(ResourceManager resources, uint goodsId) =>
        Load(resources)?.FirstOrDefault(g => g.GoodsId == goodsId);
}

public sealed record ShopBuyResult(ItemInfo Item, RewardItem Price);

public static class ShopSystemExt
{
    public static ShopBuyResult? BuyGoods(this PlayerInstance player, ResourceManager resources, uint goodsId)
    {
        var goods = ShopTable.Find(resources, goodsId);
        if (goods?.Gdpln is not { Count: >= 4 } gdpl || goods.Price1 is not { Count: >= 5 } price)
            return null;
        if (goods.LimitNum > 0 && BoughtCount(player, goodsId) >= goods.LimitNum)
            return null;
        var saleGenre = gdpl[0];
        var saleDetail = gdpl[1];
        var saleParticular = gdpl[2];
        var saleLevel = gdpl[3];
        var saleCount = gdpl.Count > 4 ? gdpl[4] : 1;

        ItemInfo granted;
        if (saleGenre == ItemGenres.Card)
            granted = player.Characters.AddCharacter(saleDetail, Math.Max(saleParticular, 1), saleLevel);
        else if (saleGenre == ItemGenres.Weapon)
            granted = player.Characters.AddWeapon(saleDetail, saleParticular, saleLevel)!;
        else
            granted = player.Inventory.Add(saleGenre * 1000UL + saleDetail, saleCount);

        var cost = new RewardItem { G = price[0], D = price[1], P = price[2], L = price[3], Count = price[4] };
        var moneyType = price[1];
        var amount = price[4];
        if (price[0] is ItemGenres.Card or ItemGenres.Weapon or ItemGenres.Supplies && moneyType <= 100)
        {
            if (!player.ConsumeCurrency(moneyType, amount))
            {
                if (saleGenre is ItemGenres.Card or ItemGenres.Weapon)
                    player.Inventory.Remove(granted.Id);
                else
                    player.Inventory.Consume(saleGenre * 1000UL + saleDetail, saleCount);
                return null;
            }
        }
        else
        {
            player.Inventory.Consume(cost.G * 1000UL + cost.D, amount);
        }
        var bought = player.GetExtra();
        bought[$"shop{goodsId}"] = bought.GetValueOrDefault($"shop{goodsId}", 0) + 1;
        player.SetExtra(bought);
        return new ShopBuyResult(granted, cost);
    }

    public static uint BoughtCount(this PlayerInstance player, uint goodsId) =>
        (uint)player.GetExtra().GetValueOrDefault($"shop{goodsId}", 0);

    public static string? GoodsListJson(ResourceManager resources)
    {
        var goods = ShopTable.Load(resources);
        if (goods == null)
            return null;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            goods = goods.Select(g => new
            {
                shop_id = g.ShopId,
                goods_id = g.GoodsId,
                gdpln = g.Gdpln,
                price = g.Price1,
                limit_type = g.LimitType,
                limit_num = g.LimitNum,
            }).ToList(),
        });
    }
}
