using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed class InventoryManager
{
    private readonly PlayerSnapshot _data;
    private readonly Dictionary<ulong, ItemInfo> _items;

    public InventoryManager(PlayerSnapshot data)
    {
        _data = data;
        _items = data.Items.ToDictionary(i => i.Id);
    }

    public IReadOnlyDictionary<ulong, ItemInfo> Items => _items;

    public ItemInfo? Find(ulong id) => _items.GetValueOrDefault(id);

    public uint Count(ulong template) =>
        (uint)_items.Values.Where(i => i.Template == template).Sum(i => i.Count);

    public ItemInfo Add(ulong template, uint count, uint flag = 0, string userdata = "")
    {
        var id = ++_data.ItemIdAlloc;
        var item = new ItemInfo
        {
            Id = id,
            Template = template,
            Count = count,
            Flag = flag,
            Userdata = userdata,
        };
        _data.Items.Add(item);
        _items[id] = item;
        return item;
    }

    public bool Remove(ulong id)
    {
        if (!_items.Remove(id, out var item))
            return false;
        _data.Items.Remove(item);
        return true;
    }

    public bool Consume(ulong template, uint count)
    {
        if (Count(template) < count)
            return false;
        foreach (var item in _items.Values.Where(i => i.Template == template).ToList())
        {
            var take = Math.Min(item.Count, count);
            item.Count -= take;
            count -= take;
            if (item.Count == 0)
                Remove(item.Id);
            if (count == 0)
                break;
        }
        return true;
    }
}

public sealed class LineupManager
{
    private readonly PlayerSnapshot _data;

    public LineupManager(PlayerSnapshot data) => _data = data;

    public LineupInfo? Get(uint index) => _data.Solutions.FirstOrDefault(l => l.Index == index);

    public LineupInfo GetOrCreate(uint index)
    {
        var lineup = Get(index);
        if (lineup != null)
            return lineup;
        lineup = new LineupInfo { Index = index };
        _data.Solutions.Add(lineup);
        return lineup;
    }
}
