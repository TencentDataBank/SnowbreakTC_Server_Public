using SnowbreakTC.Core;
using SnowbreakTC.Database;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;

namespace SnowbreakTC.GameLogic;

public sealed class PlayerInstance
{
    public const string StoreKey = "player";
    public const string MetaKey = "meta";

    private readonly SnowbreakDb _db;

    public PlayerSnapshot Data { get; }
    public PlayerMeta Meta { get; private set; }
    public InventoryManager Inventory { get; }
    public LineupManager Lineups { get; }

    public int Uid => (int)Data.Pid;

    private PlayerInstance(PlayerSnapshot data, PlayerMeta meta, SnowbreakDb db)
    {
        Data = data;
        Meta = meta;
        _db = db;
        Inventory = new InventoryManager(data);
        Lineups = new LineupManager(data);
    }

    public static PlayerInstance LoadOrCreate(int uid, SnowbreakDb db, string username)
    {
        var data = db.Load<PlayerSnapshot>(uid, StoreKey);
        var meta = db.Load<PlayerMeta>(uid, MetaKey) ?? new PlayerMeta();
        if (data == null)
        {
            data = new PlayerSnapshot
            {
                Pid = (ulong)uid,
                Account = username,
                Level = 1,
                CreateTime = Now(),
                Serial = 1,
                Attrs =
                {
                    [Cluster.SeedGid * 10000 + Cluster.SeedSid] = Cluster.SeedValue,
                    [Cluster.SeedGid << 16 | Cluster.SeedSid] = Cluster.SeedValue,
                },
            };
        }
        return new PlayerInstance(data, meta, db);
    }

    public uint GetAttr(uint gid, uint sid)
    {
        if (Data.Attrs.TryGetValue(gid * 10000 + sid, out var v) && v != 0)
            return v;
        return Data.Attrs.TryGetValue(gid << 16 | sid, out v) ? v : 0;
    }

    public void SetAttr(uint gid, uint sid, uint val)
    {
        Data.Attrs[gid * 10000 + sid] = val;
        Data.Attrs[gid << 16 | sid] = val;
    }

    public void SetStrAttr(uint gid, uint sid, string val)
    {
        Data.StrAttrs[gid << 16 | sid] = val;
    }

    public uint GetCurrency(uint moneyType) => GetAttr(AttrIds.Currency.Group, AttrIds.Currency.SidOf(moneyType));

    public void SetCurrency(uint moneyType, uint amount) => SetAttr(AttrIds.Currency.Group, AttrIds.Currency.SidOf(moneyType), amount);

    public bool SeedCurrencies(uint amount, uint maxType = 200)
    {
        if (Meta.CurrencySeeded)
            return false;
        for (var t = 1u; t <= maxType; t++)
            SetCurrency(t, amount);
        Meta.CurrencySeeded = true;
        return true;
    }

    public uint RegenVigor(uint limit, uint perMinute)
    {
        if (Data.Vigor >= limit || Data.LastVigorTime == 0)
        {
            Data.LastVigorTime = Now();
            return 0;
        }
        var now = Now();
        var elapsed = (ulong)Math.Max(0, now - Data.LastVigorTime);
        var gained = (uint)Math.Min(elapsed / (perMinute * 60), limit - Data.Vigor);
        if (gained > 0)
        {
            Data.Vigor += gained;
            Data.LastVigorTime = now;
        }
        return gained;
    }

    public string SnapshotJson() => System.Text.Json.JsonSerializer.Serialize(Data);

    public CharacterSystem Characters => new(this);

    public QuestSystem Quest(ResourceManager resources) => new(this, resources);

    public Dictionary<string, long> LoadDict(string key) =>
        _db.Load<Dictionary<string, long>>(Uid, key) ?? [];

    public void SaveDict(string key, Dictionary<string, long> value) => _db.Save(Uid, key, value);

    public void Mark(string key, long value = 1)
    {
        var extras = LoadDict("extras");
        extras[key] = value;
        SaveDict("extras", extras);
        Save();
    }

    public bool ConsumeCurrency(uint moneyType, uint amount)
    {
        var current = GetCurrency(moneyType);
        if (current < amount)
            return false;
        SetCurrency(moneyType, current - amount);
        return true;
    }

    public void Save()
    {
        _db.Save(Uid, StoreKey, Data);
        _db.Save(Uid, MetaKey, Meta);
    }

    public static ulong Now() => (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

public sealed class PlayerMeta
{
    public bool CurrencySeeded { get; set; }
    public long LastLoginAt { get; set; }
    public uint LoginCount { get; set; }
}
