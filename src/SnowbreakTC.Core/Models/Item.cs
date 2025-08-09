using MongoDB.Bson.Serialization.Attributes;

namespace SnowbreakTC.Core.Models;

/// <summary>
/// 物品实体
/// </summary>
[BsonCollection("items")]
public class Item : AuditableEntity
{
    /// <summary>
    /// 所属玩家ID
    /// </summary>
    [BsonElement("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 物品配置ID（对应游戏配置表）
    /// </summary>
    [BsonElement("itemId")]
    public int ItemId { get; set; }

    /// <summary>
    /// 物品数量
    /// </summary>
    [BsonElement("quantity")]
    public long Quantity { get; set; } = 1;

    /// <summary>
    /// 物品类型
    /// </summary>
    [BsonElement("type")]
    public ItemType Type { get; set; } = ItemType.Material;

    /// <summary>
    /// 物品稀有度
    /// </summary>
    [BsonElement("rarity")]
    public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    /// <summary>
    /// 物品状态
    /// </summary>
    [BsonElement("status")]
    public ItemStatus Status { get; set; } = ItemStatus.Normal;

    /// <summary>
    /// 获得时间
    /// </summary>
    [BsonElement("obtainedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 过期时间（如果有）
    /// </summary>
    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 物品属性（用于装备等）
    /// </summary>
    [BsonElement("attributes")]
    [BsonIgnoreIfNull]
    public ItemAttributes? Attributes { get; set; }

    /// <summary>
    /// 强化等级（用于装备）
    /// </summary>
    [BsonElement("enhanceLevel")]
    public int EnhanceLevel { get; set; } = 0;

    /// <summary>
    /// 物品来源
    /// </summary>
    [BsonElement("source")]
    [BsonIgnoreIfNull]
    public string? Source { get; set; }

    /// <summary>
    /// 自定义数据
    /// </summary>
    [BsonElement("customData")]
    public Dictionary<string, object> CustomData { get; set; } = new();

    /// <summary>
    /// 检查物品是否过期
    /// </summary>
    /// <returns></returns>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// 检查是否可以堆叠
    /// </summary>
    /// <returns></returns>
    public bool IsStackable()
    {
        return Type == ItemType.Material || Type == ItemType.Consumable;
    }

    /// <summary>
    /// 计算物品价值
    /// </summary>
    /// <returns></returns>
    public long CalculateValue()
    {
        var baseValue = (int)Rarity * 100;
        var enhanceMultiplier = 1 + EnhanceLevel * 0.1;
        return (long)(baseValue * enhanceMultiplier * Quantity);
    }
}

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    /// <summary>
    /// 材料
    /// </summary>
    Material = 1,

    /// <summary>
    /// 消耗品
    /// </summary>
    Consumable = 2,

    /// <summary>
    /// 武器
    /// </summary>
    Weapon = 3,

    /// <summary>
    /// 装备
    /// </summary>
    Equipment = 4,

    /// <summary>
    /// 饰品
    /// </summary>
    Accessory = 5,

    /// <summary>
    /// 礼品
    /// </summary>
    Gift = 6,

    /// <summary>
    /// 任务物品
    /// </summary>
    Quest = 7,

    /// <summary>
    /// 货币
    /// </summary>
    Currency = 8,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99
}

/// <summary>
/// 物品稀有度枚举
/// </summary>
public enum ItemRarity
{
    /// <summary>
    /// 普通
    /// </summary>
    Common = 1,

    /// <summary>
    /// 稀有
    /// </summary>
    Rare = 2,

    /// <summary>
    /// 史诗
    /// </summary>
    Epic = 3,

    /// <summary>
    /// 传说
    /// </summary>
    Legendary = 4,

    /// <summary>
    /// 神话
    /// </summary>
    Mythic = 5
}

/// <summary>
/// 物品状态枚举
/// </summary>
public enum ItemStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 1,

    /// <summary>
    /// 锁定
    /// </summary>
    Locked = 2,

    /// <summary>
    /// 已装备
    /// </summary>
    Equipped = 3,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 4
}

/// <summary>
/// 物品属性
/// </summary>
public class ItemAttributes
{
    /// <summary>
    /// 主属性
    /// </summary>
    [BsonElement("mainAttribute")]
    public AttributeType MainAttribute { get; set; } = AttributeType.None;

    /// <summary>
    /// 主属性值
    /// </summary>
    [BsonElement("mainAttributeValue")]
    public double MainAttributeValue { get; set; } = 0;

    /// <summary>
    /// 副属性列表
    /// </summary>
    [BsonElement("subAttributes")]
    public Dictionary<AttributeType, double> SubAttributes { get; set; } = new();

    /// <summary>
    /// 套装ID（如果是套装装备）
    /// </summary>
    [BsonElement("setId")]
    public int SetId { get; set; } = 0;

    /// <summary>
    /// 特殊效果
    /// </summary>
    [BsonElement("specialEffects")]
    public List<SpecialEffect> SpecialEffects { get; set; } = new();
}

/// <summary>
/// 属性类型枚举
/// </summary>
public enum AttributeType
{
    /// <summary>
    /// 无属性
    /// </summary>
    None = 0,

    /// <summary>
    /// 生命值
    /// </summary>
    Health = 1,

    /// <summary>
    /// 攻击力
    /// </summary>
    Attack = 2,

    /// <summary>
    /// 防御力
    /// </summary>
    Defense = 3,

    /// <summary>
    /// 速度
    /// </summary>
    Speed = 4,

    /// <summary>
    /// 暴击率
    /// </summary>
    CriticalRate = 5,

    /// <summary>
    /// 暴击伤害
    /// </summary>
    CriticalDamage = 6,

    /// <summary>
    /// 命中率
    /// </summary>
    Accuracy = 7,

    /// <summary>
    /// 闪避率
    /// </summary>
    Evasion = 8,

    /// <summary>
    /// 生命值百分比
    /// </summary>
    HealthPercent = 11,

    /// <summary>
    /// 攻击力百分比
    /// </summary>
    AttackPercent = 12,

    /// <summary>
    /// 防御力百分比
    /// </summary>
    DefensePercent = 13
}

/// <summary>
/// 特殊效果
/// </summary>
public class SpecialEffect
{
    /// <summary>
    /// 效果ID
    /// </summary>
    [BsonElement("effectId")]
    public int EffectId { get; set; }

    /// <summary>
    /// 效果等级
    /// </summary>
    [BsonElement("level")]
    public int Level { get; set; } = 1;

    /// <summary>
    /// 效果参数
    /// </summary>
    [BsonElement("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 仓库实体
/// </summary>
[BsonCollection("inventories")]
public class Inventory : AuditableEntity
{
    /// <summary>
    /// 所属玩家ID
    /// </summary>
    [BsonElement("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 仓库类型
    /// </summary>
    [BsonElement("type")]
    public InventoryType Type { get; set; } = InventoryType.Main;

    /// <summary>
    /// 最大槽位数
    /// </summary>
    [BsonElement("maxSlots")]
    public int MaxSlots { get; set; } = 100;

    /// <summary>
    /// 已使用槽位数
    /// </summary>
    [BsonElement("usedSlots")]
    public int UsedSlots { get; set; } = 0;

    /// <summary>
    /// 物品槽位映射
    /// </summary>
    [BsonElement("slots")]
    public Dictionary<int, InventorySlot> Slots { get; set; } = new();

    /// <summary>
    /// 仓库状态
    /// </summary>
    [BsonElement("status")]
    public InventoryStatus Status { get; set; } = InventoryStatus.Active;

    /// <summary>
    /// 添加物品到仓库
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量</param>
    /// <returns>是否成功</returns>
    public bool AddItem(string itemId, long quantity)
    {
        // 查找是否有相同物品的槽位
        var existingSlot = Slots.Values.FirstOrDefault(s => s.ItemId == itemId && s.CanStack);
        if (existingSlot != null)
        {
            existingSlot.Quantity += quantity;
            return true;
        }

        // 查找空槽位
        for (int i = 0; i < MaxSlots; i++)
        {
            if (!Slots.ContainsKey(i))
            {
                Slots[i] = new InventorySlot
                {
                    ItemId = itemId,
                    Quantity = quantity,
                    SlotIndex = i
                };
                UsedSlots++;
                return true;
            }
        }

        return false; // 仓库已满
    }

    /// <summary>
    /// 从仓库移除物品
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    /// <param name="quantity">数量</param>
    /// <returns>是否成功</returns>
    public bool RemoveItem(int slotIndex, long quantity)
    {
        if (!Slots.TryGetValue(slotIndex, out var slot))
            return false;

        if (slot.Quantity < quantity)
            return false;

        slot.Quantity -= quantity;
        if (slot.Quantity <= 0)
        {
            Slots.Remove(slotIndex);
            UsedSlots--;
        }

        return true;
    }

    /// <summary>
    /// 检查是否有足够空间
    /// </summary>
    /// <param name="requiredSlots">需要的槽位数</param>
    /// <returns></returns>
    public bool HasSpace(int requiredSlots = 1)
    {
        return UsedSlots + requiredSlots <= MaxSlots;
    }
}

/// <summary>
/// 仓库类型枚举
/// </summary>
public enum InventoryType
{
    /// <summary>
    /// 主仓库
    /// </summary>
    Main = 1,

    /// <summary>
    /// 装备仓库
    /// </summary>
    Equipment = 2,

    /// <summary>
    /// 材料仓库
    /// </summary>
    Material = 3,

    /// <summary>
    /// 临时仓库
    /// </summary>
    Temporary = 4
}

/// <summary>
/// 仓库状态枚举
/// </summary>
public enum InventoryStatus
{
    /// <summary>
    /// 激活
    /// </summary>
    Active = 1,

    /// <summary>
    /// 锁定
    /// </summary>
    Locked = 2,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 3
}

/// <summary>
/// 仓库槽位
/// </summary>
public class InventorySlot
{
    /// <summary>
    /// 物品ID
    /// </summary>
    [BsonElement("itemId")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// 物品数量
    /// </summary>
    [BsonElement("quantity")]
    public long Quantity { get; set; } = 1;

    /// <summary>
    /// 槽位索引
    /// </summary>
    [BsonElement("slotIndex")]
    public int SlotIndex { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    [BsonElement("isLocked")]
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// 是否可以堆叠
    /// </summary>
    [BsonElement("canStack")]
    public bool CanStack { get; set; } = true;

    /// <summary>
    /// 放入时间
    /// </summary>
    [BsonElement("placedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}