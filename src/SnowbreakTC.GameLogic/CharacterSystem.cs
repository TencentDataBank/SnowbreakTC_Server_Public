using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed class CharacterSystem
{
    public const uint MaxLevel = 80;
    public const uint MaxBreak = 5;

    private readonly PlayerInstance _player;

    public CharacterSystem(PlayerInstance player) => _player = player;

    public ItemInfo? FindByGuid(ulong guid) =>
        _player.Inventory.Find(guid) is { } item && ItemGenres.Unpack(item.Template).Genre is ItemGenres.Card or ItemGenres.Weapon
            ? item
            : null;

    public IEnumerable<ItemInfo> Characters =>
        _player.Inventory.Items.Values.Where(i => ItemGenres.Unpack(i.Template).Genre == ItemGenres.Card);

    public IEnumerable<ItemInfo> Weapons =>
        _player.Inventory.Items.Values.Where(i => ItemGenres.Unpack(i.Template).Genre == ItemGenres.Weapon);

    public ItemInfo AddCharacter(uint detail, uint particular, uint level = 1)
    {
        var template = ItemGenres.Template(ItemGenres.Card, detail, particular);
        var existing = Characters.FirstOrDefault(c => c.Template == template);
        if (existing != null)
            return existing;
        var card = _player.Inventory.Add(template, 1);
        card.Enhance = new EnhanceInfo { Level = level, Break = 1 };
        var weapon = AddWeapon(detail, particular, level);
        if (weapon != null)
            card.Slots[1] = weapon.Id;
        return card;
    }

    public ItemInfo? AddWeapon(uint detail, uint particular, uint level = 1)
    {
        var template = ItemGenres.Template(ItemGenres.Weapon, detail, particular);
        var weapon = _player.Inventory.Add(template, 1);
        weapon.Enhance = new EnhanceInfo { Level = level };
        return weapon;
    }
    public bool AddLevel(ulong guid, uint amount, uint cap = MaxLevel)
    {
        var item = FindByGuid(guid);
        if (item?.Enhance == null)
            return false;
        var target = Math.Min(item.Enhance.Level + amount, cap);
        if (target <= item.Enhance.Level)
            return false;
        item.Enhance.Level = target;
        return true;
    }

    public bool AddBreak(ulong guid)
    {
        var item = FindByGuid(guid);
        if (item?.Enhance == null || item.Enhance.Break >= MaxBreak)
            return false;
        item.Enhance.Break++;
        return true;
    }

    public bool AddEvolue(ulong guid, int amount)
    {
        var item = FindByGuid(guid);
        if (item?.Enhance == null)
            return false;
        var next = (int)item.Enhance.Evolue + amount;
        if (next < 0)
            return false;
        item.Enhance.Evolue = (uint)next;
        return true;
    }

    public bool BindWeapon(ulong cardGuid, ulong weaponGuid)
    {
        var card = FindByGuid(cardGuid);
        if (card == null || ItemGenres.Unpack(card.Template).Genre != ItemGenres.Card)
            return false;
        if (weaponGuid == 0)
        {
            card.Slots.Remove(1);
            return true;
        }
        if (FindByGuid(weaponGuid) is not { } weapon
            || ItemGenres.Unpack(weapon.Template).Genre != ItemGenres.Weapon)
            return false;
        card.Slots[1] = weaponGuid;
        return true;
    }

    public bool ChangeSkin(ulong cardGuid, uint skinId)
    {
        var card = FindByGuid(cardGuid);
        if (card == null)
            return false;
        card.Userdata = skinId.ToString();
        return true;
    }

    public bool AddTrust(ulong cardGuid, int amount)
    {
        var item = FindByGuid(cardGuid);
        if (item?.Enhance == null)
            return false;
        var next = (int)item.Enhance.Trust + amount;
        if (next < 0)
            return false;
        item.Enhance.Trust = (uint)next;
        return true;
    }
}
