using System.Text.Json.Serialization;
using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class HouseStateExt
{
    public const string Key = "house_state";

    public static Dictionary<string, long> House(this PlayerInstance player) => player.LoadDict(Key);

    public static void SetHouse(this PlayerInstance player, Dictionary<string, long> state)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [Key + "_n"] = 0 });
        player.SaveDict(Key, state);
        player.Save();
    }

    public static void HouseSet(this PlayerInstance player, string field, long value)
    {
        var state = player.House();
        state[field] = value;
        player.SetHouse(state);
    }

    public static RogueState Rogue(this PlayerInstance player) => RogueState.Load(player);

    public static void SaveRogue(this PlayerInstance player, RogueState state) => state.Save(player);
}

public sealed class RogueState
{
    public uint Layer { get; set; } = 1;
    public uint Node { get; set; }
    public uint Hp { get; set; } = 3;
    public bool Active { get; set; }

    public static RogueState Load(PlayerInstance player)
    {
        var raw = player.LoadDict("rogue_state");
        return new RogueState
        {
            Layer = (uint)raw.GetValueOrDefault("layer", 1),
            Node = (uint)raw.GetValueOrDefault("node", 0),
            Hp = (uint)raw.GetValueOrDefault("hp", 3),
            Active = raw.GetValueOrDefault("active", 0) != 0,
        };
    }

    public void Save(PlayerInstance player)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra())
        {
            ["layer"] = Layer,
            ["node"] = Node,
            ["hp"] = Hp,
            ["active"] = Active ? 1 : 0,
        });
        player.Save();
    }
}

// ---------- House ----------

[Route("House_Request")]
public sealed class HouseRequest : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var rooms = player.House();
        var arg = JsonSerializer.Serialize(new
        {
            level = 1,
            rooms = rooms.Select(kv => new { room = kv.Key, value = kv.Value }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("GirlRegister")]
[Route("GirlRoomChange")]
[Route("GirlLeaveRoom")]
[Route("ExchangeRoomGirl")]
[Route("SetBedroomGirlId")]
public sealed class HouseGirlOps : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var room = root.Find("area_id", "room", "room_id").Uint();
        var girlGuid = root.Find("girl_id", "id", "guid").Ulong();
        if (room is { } r && girlGuid is { } g)
            player.HouseSet($"room{r}", (long)g);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("SetGroupFurIndex")]
[Route("HouseAttr")]
[Route("HouseFuncAttribute")]
[Route("PubAttr")]
[Route("ThrowAttr")]
[Route("ThrowGameTutorialFinish")]
[Route("UnLockWaterGunGameItem")]
[Route("ChangeGirlBeachSuitId")]
[Route("ChangeNpcSuit")]
[Route("ChangeNpcSuitByAreaId")]
[Route("ReadGirlLoveStory")]
[Route("RouletteEnd")]
[Route("PubGameMulExit")]
[Route("ArcadeGameEnterMainUI")]
[Route("ThrowGameEnterMainUI")]
public sealed class HouseSetters : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var key = root.Find("key", "attr_id", "id").Str();
        var value = root.Find("value", "val", "count").Ulong();
        if (key != null && value is { } v)
            player.HouseSet(key, (long)v);
        return Task.FromResult(new CallGsReply());
    }
}

public static class HouseGameExt
{
    public static CallGsReply HouseGameEnter(this PlayerInstance player, string gameKey)
    {
        var state = player.House();
        var round = state.GetValueOrDefault(gameKey + "_round", 0) + 1;
        player.HouseSet(gameKey + "_round", round);
        return new CallGsReply(JsonSerializer.Serialize(new { round, seed = Random.Shared.Next(1, 999999) }));
    }

    public static CallGsReply HouseGameSettle(this PlayerInstance player, string gameKey, uint score)
    {
        player.HouseSet(gameKey + "_last", score);
        var best = player.House().GetValueOrDefault(gameKey + "_best");
        if (score > best)
            player.HouseSet(gameKey + "_best", score);
        return new CallGsReply(JsonSerializer.Serialize(new { score, best = Math.Max(score, best) }));
    }
}

[Route("PubGameEnter")]
[Route("ThrowGameEnter")]
[Route("ArcadeGameEnter")]
public sealed class HouseGameEnterApi : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.HouseGameEnter("pub"));
}

[Route("PubGameSettlement")]
[Route("ThrowGameSettlement")]
[Route("ArcadeGameSettlement")]
[Route("PubGameAchievementFinish")]
public sealed class HouseGameSettleApi : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var score = root.Find("score", "point").Uint() ?? 0;
        return Task.FromResult(player.HouseGameSettle("pub", score));
    }
}

[Route("PubGameGetReward")]
[Route("ThrowGameGetReward")]
[Route("ArcadeGameGetLevelReward")]
[Route("PubGameGetAchReward")]
[Route("ThrowGameGetAchReward")]
[Route("ArcadeGameGetAchReward")]
public sealed class HouseGameRewardApi : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 3, Count = 100 },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("ArcadeGameLogSettlement")]
public sealed class ArcadeLogApi : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var score = root.Find("score", "point").Uint() ?? 0;
        return Task.FromResult(player.HouseGameSettle("arcade", score));
    }
}

// ---------- Rogue ----------

[Route("RogueLogic_EnterRogue")]
public sealed class RogueEnter : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var state = new RogueState { Layer = 1, Node = 0, Hp = 3, Active = true };
        player.SaveRogue(state);
        var arg = JsonSerializer.Serialize(new { layer = state.Layer, node = state.Node, hp = state.Hp });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("RogueLogic_FinishNode")]
public sealed class RogueFinishNode : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var state = player.Rogue();
        if (!state.Active)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        state.Node++;
        if (state.Node >= 6)
        {
            state.Node = 0;
            state.Layer++;
        }
        player.SaveRogue(state);
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = 50 * state.Layer },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            layer = state.Layer,
            node = state.Node,
            hp = state.Hp,
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("RogueLogic_ClientMoveNext")]
public sealed class RogueMoveNext : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var state = player.Rogue();
        state.Node++;
        if (state.Node >= 6)
        {
            state.Node = 0;
            state.Layer++;
        }
        player.SaveRogue(state);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { layer = state.Layer, node = state.Node })));
    }
}

[Route("RogueLogic_ResetRogue")]
public sealed class RogueReset : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        player.SaveRogue(new RogueState());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("RogueLogic_ReviveRole")]
[Route("RogueLogic_ChangeFormation")]
[Route("RogueLogic_ShowOpenStory")]
[Route("RogueLogic_FetchDailyBuff")]
public sealed class RogueMisc : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var state = player.Rogue();
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { layer = state.Layer, node = state.Node, hp = state.Hp })));
    }
}

[Route("RogueLogic_QuickGetReward")]
public sealed class RogueQuickReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var state = player.Rogue();
        var rewards = player.Quest(session.Server.Resources).DeliverRewards(
        [
            new RewardItem { G = ItemGenres.Supplies, D = 24, Count = 100 * state.Layer },
        ]);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("RogueLogic_VisitShop")]
[Route("RogueLogic_RefreshShopGoods")]
public sealed class RogueShop : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(ShopSystemExt.GoodsListJson(session.Server.Resources) is { } json
            ? new CallGsReply(json)
            : new CallGsReply());
}

// ---------- Calendar activity sign-in ----------

public sealed record CalendarCycleRec
{
    [JsonPropertyName("id")] public uint Id { get; init; }
    [JsonPropertyName("content_id")] public List<uint> ContentIds { get; init; } = [];
}

public sealed record CalendarContentRec
{
    [JsonPropertyName("content_id")] public uint ContentId { get; init; }
    [JsonPropertyName("preview")] public List<List<uint>> Preview { get; init; } = [];
}

public static class CalendarExt
{
    public static List<CalendarCycleRec>? Cycles(this ResourceManager r) =>
        r.Table<CalendarCycleRec>("activity/sign/calendar_cycle.json");

    public static CalendarContentRec? Content(this ResourceManager r, uint contentId) =>
        r.Table<CalendarContentRec>("activity/sign/calendar_content.json")?.FirstOrDefault(c => c.ContentId == contentId);

    public static CallGsReply Award(this PlayerInstance player, ResourceManager resources, uint contentId)
    {
        var key = $"cal{contentId}";
        if (player.GetExtra().ContainsKey(key))
            return CallGsReply.Error("tip.already_signed");
        var content = resources.Content(contentId);
        if (content == null)
            return CallGsReply.Error("error.BadParam");
        var extras = player.GetExtra();
        extras[key] = (long)PlayerInstance.Now();
        player.SetExtra(extras);
        var rewards = player.Quest(resources).DeliverRewards(content.Preview.Select(e => new RewardItem
        {
            G = e.Count > 0 ? e[0] : 0,
            D = e.Count > 1 ? e[1] : 0,
            Count = e.Count > 4 ? e[4] : 1,
        }));
        player.Save();
        return new CallGsReply(JsonSerializer.Serialize(new
        {
            content_id = contentId,
            rewards = rewards.Select(r => new { g = r.G, d = r.D, count = r.Count }).ToList(),
        }));
    }
}

[Route("Activity_GetAward")]
public sealed class ActivityGetAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var contentId = root.Find("content_id", "activity_id", "id").Uint();
        if (contentId is not { } id)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        return Task.FromResult(player.Award(session.Server.Resources, id));
    }
}

[Route("Activity_ReSign")]
public sealed class ActivityReSign : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var contentId = root.Find("content_id", "activity_id", "id").Uint();
        if (contentId is not { } id)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var extras = player.GetExtra();
        extras.Remove($"cal{id}");
        player.SetExtra(extras);
        return Task.FromResult(player.Award(session.Server.Resources, id));
    }
}

[Route("Activity_Refresh")]
public sealed class ActivityRefresh : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var cycles = session.Server.Resources.Cycles();
        var arg = JsonSerializer.Serialize(new
        {
            activities = cycles?.Select(c => new { id = c.Id, contents = c.ContentIds }).ToList() ?? [],
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}
