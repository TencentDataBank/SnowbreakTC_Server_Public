using SnowbreakTC.Database;

namespace SnowbreakTC.GameLogic;

public sealed class FriendSystem
{
    public const string StoreKey = "friends";
    public const ulong SystemFriendUid = 10000;

    private readonly PlayerInstance _player;
    private readonly SnowbreakDb _db;
    private List<ulong>? _friendIds;

    public FriendSystem(PlayerInstance player, SnowbreakDb db)
    {
        _player = player;
        _db = db;
    }

    private List<ulong> Ids
    {
        get
        {
            if (_friendIds != null)
                return _friendIds;
            _friendIds = _db.Load<List<ulong>>(Uid, StoreKey) ?? [];
            if (_friendIds.Count == 0 && Uid != (int)SystemFriendUid)
                _friendIds.Add(SystemFriendUid);
            return _friendIds;
        }
    }

    private int Uid => (int)_player.Data.Pid;

    public IReadOnlyList<ulong> FriendIds => Ids;

    public bool Add(ulong pid)
    {
        if (pid == 0 || pid == _player.Data.Pid || Ids.Contains(pid))
            return false;
        Ids.Add(pid);
        Persist();
        return true;
    }

    public bool Remove(ulong pid)
    {
        if (!Ids.Remove(pid))
            return false;
        Persist();
        return true;
    }

    public bool GiveVigor()
    {
        var key = $"gv{_player.Meta.LastLoginAt / 86400}";
        var extras = _player.GetExtra();
        if (extras.ContainsKey(key))
            return false;
        extras[key] = 1;
        _player.SetExtra(extras);
        return true;
    }

    private void Persist() => _db.Save(Uid, StoreKey, Ids);
}
