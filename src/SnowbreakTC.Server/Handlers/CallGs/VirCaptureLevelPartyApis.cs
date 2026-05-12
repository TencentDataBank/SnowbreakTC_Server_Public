using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class VirCaptureLevelExt
{
    public static void CaptureSet(this PlayerInstance player, string field, long value)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra())
        {
            [$"vc_{field}"] = value,
        });
        player.Save();
    }
}

[Route("VirCaptureLevel_SaveCapture")]
public sealed class VirCaptureSave : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var target = root.Find("target_id", "monster_id", "id").Ulong();
        if (target is { } t)
            player.CaptureSet("capture", (long)t);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("VirCaptureLevel_SavePos")]
public sealed class VirCaptureSavePos : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var x = root.Find("x", "pos_x").Uint();
        var z = root.Find("z", "pos_z").Uint();
        if (x is { } px)
            player.CaptureSet("pos_x", px);
        if (z is { } pz)
            player.CaptureSet("pos_z", pz);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("VirCaptureLevel_ChangeFlag")]
public sealed class VirCaptureChangeFlag : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var flag = root.Find("flag", "flag_id").Uint();
        if (flag is { } f)
            player.CaptureSet("flag", f);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("VirCapture_ChangeFormation")]
public sealed class VirCaptureFormation : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var formation = root.Find("formation", "formation_id", "id").Ulong();
        if (formation is { } f)
            player.CaptureSet("formation", (long)f);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("VirCapture_GetLevelAward")]
public sealed class VirCaptureLevelAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = 200 },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

// ---------- Party ----------

[Route("PartyLogic_CheckFirstSign")]
public sealed class PartyCheckFirstSign : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var signed = session.Player!.GetExtra().ContainsKey("party_first_sign");
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { first_sign = !signed })));
    }
}

[Route("PartyLogic_SetFirstSign")]
public sealed class PartySetFirstSign : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { ["party_first_sign"] = (long)PlayerInstance.Now() });
        player.Save();
        return Task.FromResult(new CallGsReply());
    }
}

[Route("PartyLogic_SetFightCard")]
public sealed class PartySetFightCard : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var cardId = root.Find("card_id", "id").Ulong();
        if (cardId is { } c && !player.Data.ShowItems.Contains(c))
            player.Data.ShowItems.Add(c);
        player.Save();
        return Task.FromResult(new CallGsReply());
    }
}

[Route("PartyLogic_SetMatchId")]
[Route("PartyLogic_SetMatchMode")]
[Route("PartyLogic_SetRandomMapId")]
[Route("PartyLogic_SetSkillID")]
[Route("PartyLogic_SetTitle")]
[Route("PartyLogic_SetTitleShow")]
public sealed class PartySetters : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var key = root.Find("key", "type", "mode").Str();
        var value = root.Find("value", "id", "val").Ulong();
        if (key != null && value is { } v)
        {
            player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [$"party_{key}"] = (long)v });
            player.Save();
        }
        return Task.FromResult(new CallGsReply());
    }
}
