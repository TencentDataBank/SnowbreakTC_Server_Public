using SnowbreakTC.GameLogic;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;



[Route("ShopLogic_UpdateShop")]
[Route("ShopLogic_ConvertGirlPieces")]
public sealed class ShopMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}

[Route("Item_Exchange")]
public sealed class ItemExchange : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var from = root.Find("from_id", "src", "id").Uint();
        var to = root.Find("to_id", "dst").Uint();
        var count = root.Find("num", "count").Uint() ?? 1;
        var player = session.Player!;
        if (from is not { } f || to is not { } t || !player.Inventory.Consume(f, count))
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var item = player.Inventory.Add(t, count);
        player.Save();
        var arg = System.Text.Json.JsonSerializer.Serialize(new
        {
            items = new[] { new { id = item.Id, template = item.Template, count = item.Count } },
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Item_OpenBox")]
public sealed class ItemOpenBox : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong();
        var player = session.Player!;
        var box = player.Inventory.Find(guid ?? 0);
        if (box == null || !player.Inventory.Remove(guid ?? 0))
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var item = player.Inventory.Add(box.Template, 1);
        player.Save();
        var arg = System.Text.Json.JsonSerializer.Serialize(new
        {
            items = new[] { new { id = item.Id, template = item.Template, count = item.Count } },
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Item_Recycle")]
public sealed class ItemRecycle : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong();
        var player = session.Player!;
        if (guid is not { } g || !player.Inventory.Remove(g))
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        player.Save();
        return Task.FromResult(new CallGsReply());
    }
}

[Route("TokenBox_Use")]
[Route("VigorBox_Use")]
[Route("Cash_Exchange")]
public sealed class BoxUse : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}
