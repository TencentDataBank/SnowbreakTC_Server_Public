using System.Text.Json;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Handlers.CallGs;

public static class VirCaptureExt
{
    public const uint WorkerGid = 161;
    public const uint BuildStaffGid = 162;
    public const uint RikiGid = 135;
    public const uint MonsterCardGenre = 18;
    public const uint IslandCoinMoneyType = 55;

    public static void VcSet(this PlayerInstance player, string key, long value)
    {
        player.SetExtra(new Dictionary<string, long>(player.GetExtra()) { [$"vc_{key}"] = value });
        player.Save();
    }

    public static long VcGet(this PlayerInstance player, string key) =>
        player.GetExtra().GetValueOrDefault($"vc_{key}", 0);

    public static void UnlockRikiMonster(this PlayerInstance player, ResourceManager resources, uint genre, uint detail, uint particular, uint level)
    {
        var raw = resources.Raw("riki/Riki.json");
        if (raw == null)
            return;
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("Condition", out var cond) || cond.ValueKind != JsonValueKind.Array)
                continue;
            var vals = cond.EnumerateArray().Select(e => e.TryGetUInt32(out var v) ? v : 0).ToList();
            if (vals.Count >= 4 && vals[0] == genre && vals[1] == detail && vals[2] == particular && vals[3] == level)
            {
                if (row.TryGetProperty("Id", out var id) && id.TryGetUInt32(out var rikiId))
                {
                    player.SetAttr(RikiGid, rikiId, 1);
                }
            }
        }
    }

    public static (uint Genre, uint Detail, uint Particular, uint Level, uint Weight)? RollPool(
        this PlayerInstance player, ResourceManager resources, string poolFile)
    {
        var raw = resources.Raw($"pool/{poolFile}".Insert(0, "dlc/vircapture/").Replace("pool//", "pool/"));
        raw ??= resources.Raw("dlc/vircapture/pool/" + poolFile);
        if (raw == null)
            return null;
        using var doc = JsonDocument.Parse(raw);
        var total = 0;
        var rows = new List<(uint G, uint D, uint P, uint L, uint W)>();
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            uint w = 0;
            uint[] gdpl = [0, 0, 0, 0];
            if (row.TryGetProperty("Weight", out var wt) && wt.TryGetUInt32(out var wv))
                w = wv;
            if (row.TryGetProperty("GDPL", out var g) && g.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var e in g.EnumerateArray())
                {
                    if (i < 4 && e.TryGetUInt32(out var v))
                        gdpl[i] = v;
                    i++;
                }
            }
            if (w > 0)
            {
                rows.Add((gdpl[0], gdpl[1], gdpl[2], gdpl[3], w));
                total += (int)w;
            }
        }
        if (rows.Count == 0 || total == 0)
            return null;
        var roll = (uint)Random.Shared.Next(1, total + 1);
        foreach (var r in rows)
        {
            if (roll <= r.W)
                return (r.G, r.D, r.P, r.L, r.W);
            roll -= r.W;
        }
        return null;
    }

    public static ulong StaffUpgradeCost(ResourceManager resources, uint staffId, uint level)
    {
        var raw = resources.Raw("dlc/vircapture/build/vircapture_staff.json");
        if (raw == null)
            return 800 * (level + 1);
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("Id", out var id) || !id.TryGetUInt32(out var v) || v != staffId)
                continue;
            if (row.TryGetProperty("Consume", out var consume) && consume.ValueKind == JsonValueKind.Array)
            {
                var arr = consume.EnumerateArray().Select(e => e.TryGetUInt64(out var c) ? c : 0).ToList();
                return level >= 1 && level <= arr.Count ? arr[(int)level - 1] : 0;
            }
        }
        return 800 * (level + 1);
    }

    public static ulong BuildUpgradeCost(ResourceManager resources, uint buildId, uint level)
    {
        var raw = resources.Raw("dlc/vircapture/build/building.json");
        if (raw == null)
            return 1000;
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("ID", out var id) || !id.TryGetUInt32(out var v) || v != buildId)
                continue;
            if (row.TryGetProperty("BuildLevel", out var bl) && bl.ValueKind == JsonValueKind.Array)
            {
                var arr = bl.EnumerateArray().Select(e => e.TryGetUInt64(out var c) ? c : 0).ToList();
                return level + 1 < arr.Count ? arr[(int)level + 1] : 0;
            }
        }
        return 1000;
    }

    public static (uint Money, uint MapId, int CashId)? RecruitOffer(ResourceManager resources, uint staffId)
    {
        var raw = resources.Raw("dlc/vircapture/build/recruitment_staff.json");
        if (raw == null)
            return null;
        using var doc = JsonDocument.Parse(raw);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (row.TryGetProperty("StaffID", out var sid) && (!sid.TryGetUInt32(out var v) || v != staffId))
                continue;
            var money = row.TryGetProperty("Money", out var m) && m.TryGetUInt32(out var mv) ? mv : 0;
            var mapId = row.TryGetProperty("Mapid", out var mp) && mp.TryGetUInt32(out var mv2) ? mv2 : 0;
            var cashId = 55;
            var opRaw = resources.Raw("dlc/vircapture/build/operating_data.json");
            if (opRaw != null)
            {
                using var opDoc = JsonDocument.Parse(opRaw);
                foreach (var op in opDoc.RootElement.EnumerateArray())
                {
                    if (op.TryGetProperty("Mapid", out var omp) && omp.TryGetUInt32(out var omv) && omv == mapId
                        && op.TryGetProperty("Cashid", out var oc) && oc.TryGetInt32(out var ocv))
                    {
                        cashId = ocv;
                        break;
                    }
                }
            }
            return (money, mapId, cashId);
        }
        return null;
    }
}


[Route("VirCapture_CheckOpenAct")]
public sealed class VirCaptureCheckOpen : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var resources = session.Server.Resources;
        DateTime? pin = null;
        var pinText = session.Server.Config.Dlc.EraPin;
        if (!string.IsNullOrWhiteSpace(pinText))
            pin = EraTime.Parse(pinText);
                
        var raw = resources.Raw("dlc/vircapture/captureregion.json");
        string? start = null, end = null;
        if (raw != null)
        {
            using var doc = JsonDocument.Parse(raw);
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                if (row.TryGetProperty("StartTime", out var st)) start = st.GetString();
                if (row.TryGetProperty("EndTime", out var en)) end = en.GetString();
                break;
            }
        }
        var open = EraTime.IsOpen(start, end, pin);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { open })));
    }
}

// ---------- Staff ----------

[Route("VirCaptureStaff_Recruit")]
public sealed class VirCaptureStaffRecruit : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var staffId = root.Find("staff_id", "staffid", "id").Uint() ?? 0;
        if (staffId == 0)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        if (player.VcGet($"staff_{staffId}") > 0)
            return Task.FromResult(CallGsReply.Error("error.condition_limit"));
        var offer = VirCaptureExt.RecruitOffer(session.Server.Resources, staffId);
        if (offer == null)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var (money, mapId, cashId) = offer.Value;
        var coinSid = AttrIds.Currency.SidOf((uint)cashId);
        if (player.GetAttr(AttrIds.Currency.Group, coinSid) < money)
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.ConsumeCurrency((uint)cashId, money);
        player.SetAttr(VirCaptureExt.BuildStaffGid, 100 + staffId * 20 + 1, 1);
        player.VcSet($"staff_{staffId}", 1);
        player.VcSet($"staff_map_{staffId}", mapId);
        player.Save();
        var arg = JsonSerializer.Serialize(new { staff_id = staffId, money, cash_id = cashId });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("VirCaptureStaff_Upgrade")]
public sealed class VirCaptureStaffUpgrade : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var staffId = root.Find("staff_id", "staffid", "id").Uint() ?? 0;
        var level = player.GetAttr(VirCaptureExt.BuildStaffGid, staffId);
        if (staffId == 0 || level == 0)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        var cost = 800 * (level + 1);
        var coinSid = AttrIds.Currency.SidOf(VirCaptureExt.IslandCoinMoneyType);
        if (player.GetAttr(AttrIds.Currency.Group, coinSid) < cost)
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.ConsumeCurrency((uint)VirCaptureExt.IslandCoinMoneyType, cost);
        player.SetAttr(VirCaptureExt.BuildStaffGid, staffId, level + 1);
        player.VcSet($"staff_{staffId}", level + 1);
        player.Save();
        var arg = JsonSerializer.Serialize(new { staff_id = staffId, level = level + 1 });
        return Task.FromResult(new CallGsReply(arg));
    }
}

// ---------- Building ----------

[Route("VirCaptureBuild_AssignWorker")]
public sealed class VirCaptureAssignWorker : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var buildId = root.Find("nId", "id", "build_id").Uint() ?? 0;
        var staffId = root.Find("nStaffId", "staff_id").Uint() ?? 0;
        var state = root.Find("nStaffState", "state").Uint() ?? 1;
        if (buildId == 0 || staffId == 0 || state is < 1 or > 3)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        if (player.GetAttr(VirCaptureExt.BuildStaffGid, staffId) <= 0)
            return Task.FromResult(CallGsReply.Error("error.BadParam"));
        if (state == 2)
        {
            player.SetAttr(VirCaptureExt.WorkerGid, staffId, 0);
        }
        else
        {
            player.SetAttr(VirCaptureExt.WorkerGid, staffId, buildId);
            player.SetAttr(VirCaptureExt.BuildStaffGid, buildId, staffId);
        }
        player.Save();
        var arg = JsonSerializer.Serialize(new { nId = buildId, nStaffId = staffId, nStaffState = state });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("VirCaptureBuild_OperateBuilding")]
public sealed class VirCaptureOperate : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var buildId = root.Find("nId", "build_id", "id").Uint() ?? 0;
        var buildLevel = player.GetAttr(VirCaptureExt.WorkerGid, 10000 + buildId * 30 + 1);
        var earnings = 500 + buildLevel * 100;
        player.SetCurrency(VirCaptureExt.IslandCoinMoneyType, player.GetCurrency(VirCaptureExt.IslandCoinMoneyType) + earnings);
        player.VcSet($"operate_{buildId}", (long)PlayerInstance.Now());
        player.Save();
        var arg = JsonSerializer.Serialize(new { nId = buildId, earnings });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("VirCaptureBuild_UpgradeBuild")]
[Route("VirCaptureBuild_UpgradeAttribute")]
public sealed class VirCaptureUpgrade : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var buildId = root.Find("nId", "build_id", "id").Uint() ?? 0;
        var cost = 1000u;
        var coinSid = AttrIds.Currency.SidOf(VirCaptureExt.IslandCoinMoneyType);
        if (player.GetAttr(AttrIds.Currency.Group, coinSid) < cost)
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.ConsumeCurrency((uint)VirCaptureExt.IslandCoinMoneyType, cost);
        var level = player.GetAttr(VirCaptureExt.BuildStaffGid, buildId) + 1;
        player.SetAttr(VirCaptureExt.BuildStaffGid, buildId, level);
        player.VcSet($"build_{buildId}", level);
        player.Save();
        var arg = JsonSerializer.Serialize(new { nId = buildId, level });
        return Task.FromResult(new CallGsReply(arg));
    }
}

// ---------- Monster gacha ----------

[Route("VirCapture_GachaMonster")]
public sealed class VirCaptureGacha : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var times = root.Find("ten").Ulong() == 1 ? 10 : root.Find("count", "num").Uint() ?? 1;
        if (times is not (1 or 10))
            times = 1;
        var cost = 300 * times;
        var coinSid = AttrIds.Currency.SidOf(VirCaptureExt.IslandCoinMoneyType);
        if (player.GetAttr(AttrIds.Currency.Group, coinSid) < cost)
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.ConsumeCurrency((uint)VirCaptureExt.IslandCoinMoneyType, cost);
        var monsters = new List<Dictionary<string, object?>>();
        for (var i = 0; i < times; i++)
        {
            var roll = player.RollPool(session.Server.Resources, "pool01.json");
            if (roll == null)
                break;
            var (g, d, p, l, _) = roll.Value;
            var item = player.Inventory.Add(ItemGenres.Template(g, d, p, l), 1);
            player.UnlockRikiMonster(session.Server.Resources, g, d, p, l);
            monsters.Add(new Dictionary<string, object?>
            {
                ["id"] = item.Id,
                ["g"] = g, ["d"] = d, ["p"] = p, ["l"] = l,
                ["template"] = item.Template,
            });
        }
        player.Save();
        var arg = JsonSerializer.Serialize(new { monsters });
        return Task.FromResult(new CallGsReply(arg));
    }
}

[Route("VirCapture_GetRikiAward")]
public sealed class VirCaptureRikiAward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var rikiId = root.Find("riki_id", "id").Uint() ?? 0;
        var mark = $"vc_riki_{rikiId}";
        var player = session.Player!;
        if (rikiId == 0 || player.GetExtra().ContainsKey(mark))
            return Task.FromResult(CallGsReply.Error("tip.already_signed"));
        player.Mark(mark);
        return Task.FromResult(player.GrantJson(session.Server.Resources, 6, 50));
    }
}

// ---------- Task / Trial / misc ----------

[Route("VirCaptureTask_DoSomething")]
public sealed class VirCaptureTaskDo : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var taskId = root.Find("task_id", "id").Ulong() ?? 0;
        var progress = root.Find("progress", "count").Ulong() ?? 1;
        if (taskId > 0)
            player.VcSet($"task_{taskId}", (long)progress);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("VirCaptureTask_DoCheckCostTask")]
public sealed class VirCaptureTaskCheckCost : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var taskId = root.Find("task_id", "id").Ulong() ?? 0;
        var cost = root.Find("cost").Uint() ?? 0;
        if (cost > 0 && !player.ConsumeCurrency(VirCaptureExt.IslandCoinMoneyType, cost))
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.VcSet($"task_{taskId}", (long)PlayerInstance.Now());
        player.Save();
        return Task.FromResult(new CallGsReply());
    }
}

[Route("VirCaptureTask_DoGiveUp")]
public sealed class VirCaptureTaskGiveUp : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var taskId = root.Find("task_id", "id").Ulong() ?? 0;
        var extras = player.GetExtra();
        extras.Remove($"vc_task_{taskId}");
        player.SetExtra(extras);
        player.Save();
        return Task.FromResult(new CallGsReply());
    }
}

[Route("VirCaptureTask_DoRandom")]
public sealed class VirCaptureTaskRandom : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var taskId = (long)Random.Shared.Next(9000, 9010);
        session.Player!.VcSet("random_task", taskId);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { task_id = taskId })));
    }
}

[Route("VirCaptureTask_GetReward")]
public sealed class VirCaptureTaskReward : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param) =>
        Task.FromResult(session.Player!.GrantJson(session.Server.Resources, 6, 100));
}

[Route("VirCapture_SwitchDayNight")]
public sealed class VirCaptureDayNight : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var cur = player.VcGet("daynight");
        player.VcSet("daynight", cur == 0 ? 1 : 0);
        return Task.FromResult(new CallGsReply(JsonSerializer.Serialize(new { night = cur == 0 })));
    }
}

[Route("VirCapture_SetLock")]
[Route("VirCapture_ChangeBagRedFlag")]
public sealed class VirCaptureFlags : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var id = root.Find("id", "monster_id").Ulong();
        if (id is { } i)
            player.VcSet($"flag_{i}", 1);
        return Task.FromResult(new CallGsReply());
    }
}

[Route("VirCapture_UpgradeLevel")]
public sealed class VirCaptureUpgradeLevel : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var player = session.Player!;
        var cost = 500u;
        var coinSid = AttrIds.Currency.SidOf(VirCaptureExt.IslandCoinMoneyType);
        if (player.GetAttr(AttrIds.Currency.Group, coinSid) < cost)
            return Task.FromResult(CallGsReply.Error("tip.material_not_enough"));
        player.ConsumeCurrency((uint)VirCaptureExt.IslandCoinMoneyType, cost);
        var level = player.VcGet("player_level") + 1;
        player.VcSet("player_level", level);
        player.Save();
        var arg = JsonSerializer.Serialize(new { level });
        return Task.FromResult(new CallGsReply(arg));
    }
}
