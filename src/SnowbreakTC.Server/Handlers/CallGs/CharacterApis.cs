using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class JsonParam
{
    public static JsonDocument? Parse(string param)
    {
        var text = string.IsNullOrWhiteSpace(param) ? "{}" : param.Trim();
        if (text == "[]")
            text = "{}";
        try
        {
            return JsonDocument.Parse(text);
        }
        catch
        {
            return null;
        }
    }

    public static JsonElement? Find(this JsonElement root, params string[] names)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var name in names)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }
        }
        return null;
    }

    public static ulong? Ulong(this JsonElement? v) => v switch
    {
        null => null,
        { ValueKind: JsonValueKind.Number } n => n.TryGetInt64(out var i) ? (ulong)i : 0,
        { ValueKind: JsonValueKind.String } s => ulong.TryParse(s.GetString(), out var u) ? u : null,
        _ => null,
    };

    public static uint? Uint(this JsonElement? v) => Ulong(v) is { } u && u <= uint.MaxValue ? (uint)u : null;

    public static string? Str(this JsonElement? v) => v is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;
}

[Route("GirlCard_UpdateLevel")]
public sealed class GirlCardUpdateLevel : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong() ?? 0;
        var player = session.Player!;
        var target = player.Characters.FindByGuid(guid);
        if (target?.Enhance == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));

        var materials = new List<(ulong Guid, uint Count)>();
        var matNode = root.Find("materials");
        if (matNode?.ValueKind == JsonValueKind.Array)
        {
            foreach (var m in matNode.Value.EnumerateArray())
            {
                var mid = m.Find("id", "guid").Ulong();
                if (mid is { } g)
                    materials.Add((g, m.Find("count", "num").Uint() ?? 1));
            }
        }
        else
        {
            var amount = root.Find("num", "count").Uint() ?? 1;
            var filler = player.Inventory.Items.Values
                .Where(i => ItemGenres.Unpack(i.Template).Genre == ItemGenres.Supplies)
                .OrderByDescending(i => i.Count)
                .FirstOrDefault();
            if (filler != null)
                materials.Add((filler.Id, amount));
        }

        var cap = GachaTable.LevelCap(session.Server.Resources, target.Enhance.Break);
        var plan = player.PlanUpgrade(session.Server.Resources, guid, materials);
        if (plan == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        if (player.GetCurrency(AttrIds.Currency.Silver) < plan.Silver)
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        var newLevel = player.ApplyUpgrade(session.Server.Resources, guid, plan, cap);
        player.Save();
        var arg = System.Text.Json.JsonSerializer.Serialize(new { id = guid, level = newLevel });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("GirlCard_UnLockCharacter")]
public sealed class GirlCardUnLockCharacter : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var detail = root.Find("detail", "d").Uint();
        var particular = root.Find("particular", "p").Uint();
        var player = session.Player!;
        if (detail is not { } d || particular is not { } p)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        player.Characters.AddCharacter(d, p);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("GirlCard_UpByBreak")]
[Route("GirlCard_UpBySpecialBreak")]
public sealed class GirlCardUpByBreak : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong();
        return Task.FromResult(session.Player!.Characters.AddBreak(guid ?? 0)
            ? new CallGsReply("{}")
            : CallGsReply.Error("error.BadParam"));
    }
}

[Route("GirlCard_ProLevelPromote")]
public sealed class GirlCardProLevelPromote : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong();
        var item = session.Player!.Characters.FindByGuid(guid ?? 0);
        if (item?.Enhance == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        item.Enhance.ProLevel++;
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("GirlCard_ReceiveProLevelAward")]
[Route("GirlCard_SetRoleLikeFlag")]
public sealed class GirlCardMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply("{}"));
}

[Route("GirlSkin_Change")]
public sealed class GirlSkinChange : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong();
        var skin = root.Find("skin_id", "skinid", "skin").Uint();
        if (guid is not { } g)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        return Task.FromResult(session.Player!.Characters.ChangeSkin(g, skin ?? 0)
            ? new CallGsReply("{}")
            : CallGsReply.Error("error.BadParam"));
    }
}

[Route("GirlSkin_ChangeSkinType")]
[Route("GirlWeaponSkin_Change")]
public sealed class GirlSkinTypeChange : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply("{}"));
}

[Route("GirlSkinParts_Update")]
[Route("GirlSpine_UnlockNodeOneKey")]
[Route("GirlSpine_ChildUnLock")]
[Route("GirlSpine_ChildNodeReset")]
[Route("EnterGirlRoom")]
public sealed class GirlRoomMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) => Task.FromResult(new CallGsReply("{}"));
}
