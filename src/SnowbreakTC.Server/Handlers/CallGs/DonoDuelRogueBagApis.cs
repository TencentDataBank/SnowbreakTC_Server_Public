using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class DonoDuelExt
{
    public static long DuelLevel(this PlayerInstance player, string key, ulong id) =>
        player.GetExtra().GetValueOrDefault($"dd_{key}_{id}", 0);

    public static void DuelLevelUp(this PlayerInstance player, string key, ulong id, uint currency, uint cost)
    {
        if (!player.ConsumeCurrency(currency, cost))
            return;
        player.SetExtra(new Dictionary<string, long>(player.GetExtra())
        {
            [$"dd_{key}_{id}"] = player.GetExtra().GetValueOrDefault($"dd_{key}_{id}", 0) + 1,
        });
        player.Save();
    }
}

[Route("DonoDuel_ClientCheck")]
public sealed class DonoDuelCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var state = session.Player!.Rogue();
        var arg = JsonSerializer.Serialize(new { open = true, layer = state.Layer, node = state.Node });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuel_ChangeFormation")]
public sealed class DonoDuelFormation : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var formation = root.Find("formation", "lineup", "id").Ulong();
        if (formation is { } f)
            player.Mark("dd_formation", (long)f);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("DonoDuel_LockSkill")]
[Route("DonoDuel_SetLock")]
public sealed class DonoDuelLock : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var skillId = root.Find("skill_id", "id").Ulong();
        if (skillId is { } s)
            player.Mark($"dd_lock_{s}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("DonoDuel_Recycle")]
public sealed class DonoDuelRecycle : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var itemId = root.Find("item_id", "id").Ulong() ?? 0;
        var count = root.Find("count", "num").Uint() ?? 1;
        player.Inventory.Consume(itemId * 1000UL, count);
        player.SetCurrency(DonoDuelConfigTable.SeasonCurrency, player.GetCurrency(DonoDuelConfigTable.SeasonCurrency) + count * 100);
        player.Save();
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("DonoDuel_StudySkillTree")]
public sealed class DonoDuelStudyTree : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var nodeId = root.Find("node_id", "skill_id", "id").Ulong();
        if (nodeId is not { } id)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        if (player.DuelLevel("tree", id) > 0)
            return Task.FromResult(CallGsReply.Error("tip.already_signed"));
        var consume = DonoDuelConfigTable.SkillTreeConsume(session.Server.Resources, (uint)id);
        var (currency, amount) = consume ?? (DonoDuelConfigTable.SeasonCurrency, 5000);
        if (!player.ConsumeCurrency(currency, amount))
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.DuelLevelUp("tree", id, 0, 0);
        var arg = JsonSerializer.Serialize(new { node_id = id, level = player.DuelLevel("tree", id) });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuel_UpgradeAttr")]
public sealed class DonoDuelUpgradeAttr : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var position = root.Find("attr_id", "position", "id").Uint() ?? 1;
        var quality = root.Find("quality").Uint() ?? 1;
        var gain = DonoDuelConfigTable.AttrGain(session.Server.Resources, position, quality);
        if (gain == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        if (!player.ConsumeCurrency(DonoDuelConfigTable.SeasonCurrency, gain.Value.Cost))
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.DuelLevelUp("attr", position, 0, 0);
        var arg = JsonSerializer.Serialize(new { position, level = player.DuelLevel("attr", position) });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuel_UpgradeLeaderSkill")]
public sealed class DonoDuelUpgradeLeader : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var skillId = root.Find("skill_id", "id").Ulong() ?? 1;
        player.DuelLevelUp("leader", skillId, DonoDuelConfigTable.SeasonCurrency, 1200);
        var arg = JsonSerializer.Serialize(new { skill_id = skillId, level = player.DuelLevel("leader", skillId) });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuel_UpgradeSkill")]
public sealed class DonoDuelUpgradeSkill : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var skillId = root.Find("skill_id", "id").Ulong() ?? 1;
        player.DuelLevelUp("skill", skillId, DonoDuelConfigTable.SeasonCurrency, 600);
        var arg = JsonSerializer.Serialize(new { skill_id = skillId, level = player.DuelLevel("skill", skillId) });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuelTask_Accept")]
public sealed class DonoDuelTaskAccept : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var taskId = root.Find("task_id", "id").Ulong();
        if (taskId is { } t)
            player.Mark($"dd_task_{t}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("DonoDuelTask_DoSomething")]
public sealed class DonoDuelTaskDo : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var taskId = root.Find("task_id", "id").Ulong() ?? 0;
        var progress = root.Find("progress", "count").Ulong() ?? 1;
        player.Mark($"dd_taskp_{taskId}", (long)progress);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("DonoDuelTask_GetReward")]
[Route("DonoDuelTask_QuickGetReward")]
public sealed class DonoDuelTaskReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 200));
}

// ---------- Rogue3D (11) ----------

[Route("Rogue3D_CheckOpenAct")]
public sealed class Rogue3DCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("rogue3d"));
}

[Route("Rogue3D_SelectDiff")]
public sealed class Rogue3DDiff : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var diff = root.Find("diff", "difficulty", "id").Uint() ?? 1;
        player.Mark("r3d_diff", diff);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("Rogue3D_SelectMode")]
public sealed class Rogue3DMode : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var mode = root.Find("mode", "mode_id", "id").Uint() ?? 1;
        player.Mark("r3d_mode", mode);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("Rogue3D_SelectTalent")]
public sealed class Rogue3DTalent : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var talentId = root.Find("talent_id", "id").Ulong() ?? 1;
        player.Mark($"r3d_talent_{talentId}");
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("Rogue3D_UpgradeTech")]
public sealed class Rogue3DTech : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var techId = root.Find("tech_id", "id").Ulong() ?? 1;
        var level = player.GetExtra().GetValueOrDefault($"r3d_tech_{techId}", 0);
        if (level >= 5)
            return Task.FromResult(CallGsReply.Error("tip.girlcard_break_max"));
        if (!player.ConsumeCurrency(AttrIds.Currency.Gold, 200))
            return Task.FromResult(CallGsReply.Error("error.gold_not_enough"));
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [$"r3d_tech_{techId}"] = level + 1 });
        player.Save();
        var arg = JsonSerializer.Serialize(new { tech_id = techId, level = level + 1 });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("Rogue3D_RestrictTech")]
public sealed class Rogue3DRestrictTech : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var techId = root.Find("tech_id", "id").Ulong();
        if (techId is { } t)
            player.Mark($"r3d_restrict_{t}");
        return Task.FromResult(new CallGsReply());
    }
}

[Route("Rogue3D_SaveFightData")]
[Route("Rogue3D_SaveSeasonFightData")]
public sealed class Rogue3DSaveFight : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var damage = root.Find("damage", "score").Ulong();
        if (damage is { } d)
            player.Mark("r3d_fight", (long)d);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("Rogue3D_RoomSettlement")]
[Route("Rogue3D_SeasonRoomSettlement")]
public sealed class Rogue3DSettle : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var state = player.Rogue();
        state.Layer++;
        state.Node = 0;
        player.SaveRogue(state);
        return Task.FromResult(player.GrantJson(session.Server.Resources, 24, 100 * state.Layer));
    }
}

// ---------- BagRogue (23) ----------

public static class BagRogueExt
{
    public static void BagSet(this PlayerInstance player, string key, long value)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [$"br_{key}"] = value });
        player.Save();
    }

    public static long BagGet(this PlayerInstance player, string key) =>
        player.GetExtra().GetValueOrDefault($"br_{key}", 0);
}

[Route("BagRogue_CheckOpenAct")]
public sealed class BagRogueCheck : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.FlagReply("bagrogue"));
}

[Route("BagRogue_FirstEnter")]
public sealed class BagRogueFirstEnter : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        player.BagSet("node", 0);
        player.BagSet("gold", 500);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { node = 0, gold = 500 })));
    }
}

[Route("BagRogue_NodeWalk")]
[Route("BagRogue_NodeFinish")]
[Route("BagRogue_CheckNewMapNode")]
public sealed class BagRogueNode : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var node = player.BagGet("node") + 1;
        player.BagSet("node", node);
        var arg = JsonSerializer.Serialize(new { node });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("BagRogue_SelectAdventure")]
public sealed class BagRogueAdventure : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var advId = root.Find("adventure_id", "id").Ulong();
        if (advId is { } a)
            player.BagSet("adventure", (long)a);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("BagRogue_StoreBuyItem")]
public sealed class BagRogueStoreBuy : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var itemId = root.Find("item_id", "goods_id", "id").Uint() ?? 1;
        var price = root.Find("price").Uint() ?? 100;
        if (!player.ConsumeCurrency(AttrIds.Currency.Gold, price))
            return Task.FromResult(CallGsReply.Error("error.gold_not_enough"));
        var item = player.Inventory.Add(itemId * 1000, 1);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            items = new[] { new { id = item.Id, template = item.Template, count = item.Count } },
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("BagRogue_SellItem")]
public sealed class BagRogueSell : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong() ?? 0;
        var item = player.Inventory.Find(guid);
        if (item == null || !player.Inventory.Remove(guid))
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        player.SetCurrency(AttrIds.Currency.Gold, player.GetCurrency(AttrIds.Currency.Gold) + item.Count * 50);
        player.Save();
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("BagRogue_UpgradeItem")]
[Route("BagRogue_DoCraftItem")]
[Route("BagRogue_ReforgeItem")]
public sealed class BagRogueUpgrade : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong() ?? 0;
        var item = player.Inventory.Find(guid);
        if (item == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        if (!player.ConsumeCurrency(AttrIds.Currency.Gold, 150))
            return Task.FromResult(CallGsReply.Error("error.gold_not_enough"));
        if (item.Enhance == null)
            item.Enhance = new EnhanceInfo();
        item.Enhance.Level++;
        player.Save();
        var arg = JsonSerializer.Serialize(new { id = guid, level = item.Enhance.Level });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("BagRogue_EquipBag")]
[Route("BagRogue_Equip")]
public sealed class BagRogueEquip : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong();
        var slot = root.Find("slot").Uint() ?? 1;
        if (guid is { } g)
            player.BagSet($"equip{slot}", (long)g);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("BagRogue_SaveItemToExternStorage")]
public sealed class BagRogueExternSave : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong() ?? 0;
        player.BagSet($"ext_{guid}", 1);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("BagRogue_RemoveExternStorageItem")]
[Route("BagRogue_SelectExternStorageItem")]
public sealed class BagRogueExternRemove : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var guid = root.Find("id", "guid").Ulong() ?? 0;
        var extras = player.GetExtra();
        extras.Remove($"br_ext_{guid}");
        player.SetExtra(extras);
        player.Save();
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("BagRogue_SaveTrialCard")]
public sealed class BagRogueTrialCard : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var cardId = root.Find("card_id", "id").Ulong();
        if (cardId is { } c)
            player.BagSet($"trial_{c}", 1);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("BagRogue_SelectCraftItem")]
public sealed class BagRogueCraftSelect : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var itemId = root.Find("item_id", "id").Ulong();
        if (itemId is { } i)
            player.BagSet("craft", (long)i);
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("BagRogue_RefreshStore")]
public sealed class BagRogueRefreshStore : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        if (!player.ConsumeCurrency(AttrIds.Currency.Gold, 100))
            return Task.FromResult(CallGsReply.Error("error.gold_not_enough"));
        player.Save();
        return Task.FromResult(ShopSystemExt.GoodsListJson(session.Server.Resources) is { } json
            ? new CallGsReply(json)
            : new CallGsReply());
    }
}

[Route("BagRogue_GetQuestAward")]
public sealed class BagRogueQuestAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 180));
}

[Route("BagRogue_LevelMoppingUp")]
public sealed class BagRogueMopup : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 24, 200));
}

[Route("BagRogue_LevelSelectAward")]
public sealed class BagRogueSelectAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var awardId = root.Find("award_id", "id").Uint() ?? 1;
        var item = player.Inventory.Add(awardId * 1000, 1);
        player.Save();
        var arg = JsonSerializer.Serialize(new
        {
            items = new[] { new { id = item.Id, template = item.Template, count = item.Count } },
        });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("BagRogue_ExFightLog")]
public sealed class BagRogueExFightLog : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        session.Player!.Mark("br_fightlog", (long)PlayerInstance.Now());
        return Task.FromResult(new CallGsReply());
    }
}

[Route("BagRogue_AwardRefresh")]
public sealed class BagRogueAwardRefresh : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        if (!player.ConsumeCurrency(AttrIds.Currency.Gold, 120))
            return Task.FromResult(CallGsReply.Error("error.gold_not_enough"));
        player.Save();
        var arg = JsonSerializer.Serialize(new { refresh_seed = Random.Shared.Next(1, 999999) });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("DonoDuelRogue_SelectBondTag")]
public sealed class DonoDuelBondTag : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var tagId = root.Find("tag_id", "bond_id", "id").Ulong();
        if (tagId is { } t)
            player.Mark($"dd_bond_{t}");
        return Task.FromResult(new CallGsReply("{}"));
    }
}

[Route("Rogue3D_SelectSeasonTalent")]
public sealed class Rogue3DSeasonTalent : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var talentId = root.Find("talent_id", "id").Ulong() ?? 1;
        player.Mark($"r3d_season_talent_{talentId}");
        return Task.FromResult(new CallGsReply("{}"));
    }
}
