using SnowbreakTC.GameLogic;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

[Route("Weapon_Upgrade")]
public sealed class WeaponUpgrade : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid", "unique_id").Ulong();
        var amount = root.Find("materials", "num", "count").Uint() ?? 1;
        return Task.FromResult(session.Player!.Characters.AddLevel(guid ?? 0, amount)
            ? new CallGsReply("{}")
            : CallGsReply.Error("error.BadParam"));
    }
}

[Route("Weapon_Break")]
public sealed class WeaponBreak : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid", "unique_id").Ulong();
        return Task.FromResult(session.Player!.Characters.AddBreak(guid ?? 0)
            ? new CallGsReply("{}")
            : CallGsReply.Error("error.BadParam"));
    }
}

[Route("Weapon_Evolution")]
public sealed class WeaponEvolution : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid", "unique_id").Ulong();
        var item = session.Player!.Characters.FindByGuid(guid ?? 0);
        if (item?.Enhance == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        item.Enhance.Evolue++;
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("Weapon_Replace")]
public sealed class WeaponReplace : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var card = root.Find("girl_id", "card_id", "id").Ulong();
        var weapon = root.Find("weapon_id", "unique_id", "guid").Ulong();
        if (card is not { } c || weapon is not { } w)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        return Task.FromResult(session.Player!.Characters.BindWeapon(c, w)
            ? new CallGsReply("{}")
            : CallGsReply.Error("error.BadParam"));
    }
}

[Route("Weapon_ReplacePart")]
[Route("Weapon_ShowDefaultPart")]
[Route("Weapon_GetUpgradePartUnlock")]
[Route("Weapon_Exchange")]
[Route("Weapon_OneKeyToMax")]
public sealed class WeaponMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply());
}
