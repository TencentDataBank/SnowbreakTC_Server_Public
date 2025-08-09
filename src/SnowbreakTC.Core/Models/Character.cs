using MongoDB.Bson.Serialization.Attributes;

namespace SnowbreakTC.Core.Models;

/// <summary>
/// 角色实体
/// </summary>
[BsonCollection("characters")]
public class Character : AuditableEntity
{
    /// <summary>
    /// 所属玩家ID
    /// </summary>
    [BsonElement("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 角色配置ID（对应游戏配置表）
    /// </summary>
    [BsonElement("characterId")]
    public int CharacterId { get; set; }

    /// <summary>
    /// 角色等级
    /// </summary>
    [BsonElement("level")]
    public int Level { get; set; } = 1;

    /// <summary>
    /// 角色经验值
    /// </summary>
    [BsonElement("experience")]
    public long Experience { get; set; } = 0;

    /// <summary>
    /// 角色稀有度
    /// </summary>
    [BsonElement("rarity")]
    public CharacterRarity Rarity { get; set; } = CharacterRarity.R;

    /// <summary>
    /// 突破等级
    /// </summary>
    [BsonElement("breakthrough")]
    public int Breakthrough { get; set; } = 0;

    /// <summary>
    /// 角色属性
    /// </summary>
    [BsonElement("attributes")]
    public CharacterAttributes Attributes { get; set; } = new();

    /// <summary>
    /// 技能等级
    /// </summary>
    [BsonElement("skills")]
    public Dictionary<int, int> Skills { get; set; } = new();

    /// <summary>
    /// 装备的武器ID
    /// </summary>
    [BsonElement("weaponId")]
    [BsonIgnoreIfNull]
    public string? WeaponId { get; set; }

    /// <summary>
    /// 装备的装备列表
    /// </summary>
    [BsonElement("equipments")]
    public Dictionary<EquipmentSlot, string> Equipments { get; set; } = new();

    /// <summary>
    /// 角色状态
    /// </summary>
    [BsonElement("status")]
    public CharacterStatus Status { get; set; } = CharacterStatus.Active;

    /// <summary>
    /// 获得时间
    /// </summary>
    [BsonElement("obtainedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否为主角色
    /// </summary>
    [BsonElement("isMain")]
    public bool IsMain { get; set; } = false;

    /// <summary>
    /// 角色好感度
    /// </summary>
    [BsonElement("affection")]
    public int Affection { get; set; } = 0;

    /// <summary>
    /// 角色皮肤ID
    /// </summary>
    [BsonElement("skinId")]
    public int SkinId { get; set; } = 0;

    /// <summary>
    /// 角色配置数据（自定义数据）
    /// </summary>
    [BsonElement("customData")]
    public Dictionary<string, object> CustomData { get; set; } = new();

    /// <summary>
    /// 计算角色战力
    /// </summary>
    /// <returns>战力值</returns>
    public long CalculatePower()
    {
        var basePower = Attributes.Attack + Attributes.Defense + Attributes.Health / 10;
        var levelMultiplier = 1 + (Level - 1) * 0.1;
        var rarityMultiplier = (int)Rarity * 0.2 + 1;
        var breakthroughMultiplier = 1 + Breakthrough * 0.15;
        
        return (long)(basePower * levelMultiplier * rarityMultiplier * breakthroughMultiplier);
    }

    /// <summary>
    /// 检查是否可以突破
    /// </summary>
    /// <returns></returns>
    public bool CanBreakthrough()
    {
        var maxLevel = GetMaxLevelForBreakthrough();
        return Level >= maxLevel && Breakthrough < GetMaxBreakthrough();
    }

    /// <summary>
    /// 获取当前突破等级的最大等级
    /// </summary>
    /// <returns></returns>
    public int GetMaxLevelForBreakthrough()
    {
        return Breakthrough switch
        {
            0 => 20,
            1 => 40,
            2 => 50,
            3 => 60,
            4 => 70,
            5 => 80,
            _ => 80
        };
    }

    /// <summary>
    /// 获取最大突破等级
    /// </summary>
    /// <returns></returns>
    public int GetMaxBreakthrough()
    {
        return Rarity switch
        {
            CharacterRarity.R => 3,
            CharacterRarity.SR => 4,
            CharacterRarity.SSR => 5,
            _ => 3
        };
    }
}

/// <summary>
/// 角色稀有度枚举
/// </summary>
public enum CharacterRarity
{
    /// <summary>
    /// R级
    /// </summary>
    R = 1,

    /// <summary>
    /// SR级
    /// </summary>
    SR = 2,

    /// <summary>
    /// SSR级
    /// </summary>
    SSR = 3
}

/// <summary>
/// 角色状态枚举
/// </summary>
public enum CharacterStatus
{
    /// <summary>
    /// 激活状态
    /// </summary>
    Active = 1,

    /// <summary>
    /// 锁定状态
    /// </summary>
    Locked = 2,

    /// <summary>
    /// 已出售
    /// </summary>
    Sold = 3
}

/// <summary>
/// 装备槽位枚举
/// </summary>
public enum EquipmentSlot
{
    /// <summary>
    /// 头盔
    /// </summary>
    Helmet = 1,

    /// <summary>
    /// 胸甲
    /// </summary>
    Chest = 2,

    /// <summary>
    /// 腿甲
    /// </summary>
    Legs = 3,

    /// <summary>
    /// 靴子
    /// </summary>
    Boots = 4,

    /// <summary>
    /// 手套
    /// </summary>
    Gloves = 5,

    /// <summary>
    /// 饰品1
    /// </summary>
    Accessory1 = 6,

    /// <summary>
    /// 饰品2
    /// </summary>
    Accessory2 = 7
}

/// <summary>
/// 角色属性
/// </summary>
public class CharacterAttributes
{
    /// <summary>
    /// 生命值
    /// </summary>
    [BsonElement("health")]
    public int Health { get; set; } = 1000;

    /// <summary>
    /// 攻击力
    /// </summary>
    [BsonElement("attack")]
    public int Attack { get; set; } = 100;

    /// <summary>
    /// 防御力
    /// </summary>
    [BsonElement("defense")]
    public int Defense { get; set; } = 50;

    /// <summary>
    /// 速度
    /// </summary>
    [BsonElement("speed")]
    public int Speed { get; set; } = 100;

    /// <summary>
    /// 暴击率 (0-100)
    /// </summary>
    [BsonElement("criticalRate")]
    public double CriticalRate { get; set; } = 5.0;

    /// <summary>
    /// 暴击伤害 (0-300)
    /// </summary>
    [BsonElement("criticalDamage")]
    public double CriticalDamage { get; set; } = 150.0;

    /// <summary>
    /// 命中率 (0-100)
    /// </summary>
    [BsonElement("accuracy")]
    public double Accuracy { get; set; } = 95.0;

    /// <summary>
    /// 闪避率 (0-100)
    /// </summary>
    [BsonElement("evasion")]
    public double Evasion { get; set; } = 5.0;

    /// <summary>
    /// 元素抗性
    /// </summary>
    [BsonElement("elementalResistance")]
    public Dictionary<ElementType, double> ElementalResistance { get; set; } = new();

    /// <summary>
    /// 元素伤害加成
    /// </summary>
    [BsonElement("elementalDamage")]
    public Dictionary<ElementType, double> ElementalDamage { get; set; } = new();

    /// <summary>
    /// 计算总属性（包含装备加成）
    /// </summary>
    /// <param name="equipmentBonus">装备属性加成</param>
    /// <returns>总属性</returns>
    public CharacterAttributes CalculateTotalAttributes(CharacterAttributes? equipmentBonus = null)
    {
        if (equipmentBonus == null) return this;

        return new CharacterAttributes
        {
            Health = Health + equipmentBonus.Health,
            Attack = Attack + equipmentBonus.Attack,
            Defense = Defense + equipmentBonus.Defense,
            Speed = Speed + equipmentBonus.Speed,
            CriticalRate = Math.Min(100, CriticalRate + equipmentBonus.CriticalRate),
            CriticalDamage = CriticalDamage + equipmentBonus.CriticalDamage,
            Accuracy = Math.Min(100, Accuracy + equipmentBonus.Accuracy),
            Evasion = Math.Min(100, Evasion + equipmentBonus.Evasion),
            ElementalResistance = MergeElementalStats(ElementalResistance, equipmentBonus.ElementalResistance),
            ElementalDamage = MergeElementalStats(ElementalDamage, equipmentBonus.ElementalDamage)
        };
    }

    /// <summary>
    /// 合并元素属性
    /// </summary>
    private Dictionary<ElementType, double> MergeElementalStats(
        Dictionary<ElementType, double> base1, 
        Dictionary<ElementType, double> base2)
    {
        var result = new Dictionary<ElementType, double>(base1);
        foreach (var kvp in base2)
        {
            result[kvp.Key] = result.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
        }
        return result;
    }
}

/// <summary>
/// 元素类型枚举
/// </summary>
public enum ElementType
{
    /// <summary>
    /// 无元素
    /// </summary>
    None = 0,

    /// <summary>
    /// 火元素
    /// </summary>
    Fire = 1,

    /// <summary>
    /// 水元素
    /// </summary>
    Water = 2,

    /// <summary>
    /// 雷元素
    /// </summary>
    Electric = 3,

    /// <summary>
    /// 冰元素
    /// </summary>
    Ice = 4,

    /// <summary>
    /// 风元素
    /// </summary>
    Wind = 5,

    /// <summary>
    /// 土元素
    /// </summary>
    Earth = 6,

    /// <summary>
    /// 光元素
    /// </summary>
    Light = 7,

    /// <summary>
    /// 暗元素
    /// </summary>
    Dark = 8
}