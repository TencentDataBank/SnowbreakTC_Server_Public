using System.Text.Json.Serialization;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed record RikiRec
{
    [JsonPropertyName("Id")] public uint Id { get; init; }
    [JsonPropertyName("Type")] public uint Type { get; init; }
    [JsonPropertyName("Condition")] public List<uint> Condition { get; init; } = [];
}

public sealed class RikiSystem
{
    public const uint TaskGroupId = 163;
    public const uint MirrorOffset = 500;
    public const uint ItemsPerSid = 30;

    private readonly PlayerInstance _player;
    private readonly ResourceManager _resources;

    public RikiSystem(PlayerInstance player, ResourceManager resources)
    {
        _player = player;
        _resources = resources;
    }

    public uint UnlockedCount { get; private set; }

    public void EnsureOwnedItemsUnlocked()
    {
        foreach (var item in _player.Inventory.Items.Values)
        {
            var (genre, detail, particular, level) = ItemGenres.Unpack(item.Template);
            if (genre is ItemGenres.Card or ItemGenres.Weapon or ItemGenres.CardSkin)
                UnlockItem(genre, detail, particular, level == 0 ? 1 : level);
        }
    }

    public void UnlockItem(uint genre, uint detail, uint particular, uint level)
    {
        var rows = _resources.Table<RikiRec>("riki/Riki.json");
        if (rows == null)
            return;
        foreach (var riki in rows)
        {
            if (riki.Condition.Count < 4
                || riki.Condition[0] != genre
                || riki.Condition[1] != detail
                || riki.Condition[2] != particular
                || riki.Condition[3] != level)
                continue;
            SetBit(riki.Id);
            UnlockedCount++;
        }
    }

    public void SetBit(uint rikiId)
    {
        var (sid, bit) = TaskPosition(rikiId);
        var mask = 1u << (int)bit;
        _player.SetAttr(TaskGroupId, sid, _player.GetAttr(TaskGroupId, sid) | mask);
        _player.SetAttr(TaskGroupId, sid + MirrorOffset, _player.GetAttr(TaskGroupId, sid + MirrorOffset) | mask);
    }

    public static (uint Sid, uint Bit) TaskPosition(uint rikiId)
    {
        var index = rikiId / 1000;
        var value = rikiId % 1000;
        if (index == 0 || value == 0)
            return (0, 0);
        var sid = ((index - 1) * ItemsPerSid) + ((value + ItemsPerSid - 1) / ItemsPerSid);
        return (sid, value % ItemsPerSid);
    }
}

public static class RikiPlayerExt
{
    public static RikiSystem Riki(this PlayerInstance player, ResourceManager resources) =>
        new(player, resources);
}
