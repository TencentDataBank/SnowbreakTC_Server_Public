using System.Text.Json;
using SnowbreakTC.Protocol.Messages;

namespace SnowbreakTC.GameLogic;

public sealed class MailSystem
{
    public const string StoreKey = "mail";

    private readonly PlayerInstance _player;
    private readonly SnowbreakTC.Database.SnowbreakDb _db;
    private List<MailInfo>? _bodies;

    public MailSystem(PlayerInstance player, SnowbreakTC.Database.SnowbreakDb db)
    {
        _player = player;
        _db = db;
    }

    private List<MailInfo> Bodies => _bodies ??= _db.Load<List<MailInfo>>((int)_player.Data.Pid, StoreKey) ?? [];

    public IReadOnlyList<MailInfo> All => Bodies;

    public MailInfo Send(string title, string message, string sender, IEnumerable<RewardItem>? attachments = null, ulong lifeDays = 30)
    {
        var mid = ++_player.Data.LastPersonMid;
        var now = PlayerInstance.Now();
        var mail = new MailInfo
        {
            Mid = mid,
            Title = title,
            Message = message,
            Sender = sender,
            Pid = _player.Data.Pid,
            Time = now,
            Expiration = now + lifeDays * 86400,
            Life = (uint)lifeDays,
            Stat = 0,
        };
        if (attachments != null)
            mail.Attachments.AddRange(attachments);
        Bodies.Add(mail);
        _player.Data.MailBox[mid] = new MailEntry
        {
            Mid = mid,
            Stat = 0,
            Time = now,
            Expiration = mail.Expiration,
        };
        Persist();
        return mail;
    }

    public MailInfo? Read(ulong mid)
    {
        var mail = Bodies.FirstOrDefault(m => m.Mid == mid);
        if (mail == null)
            return null;
        mail.Stat = 1;
        if (_player.Data.MailBox.TryGetValue(mid, out var entry))
            entry.Stat = 1;
        Persist();
        return mail;
    }

    public List<RewardItem>? TakeAttachments(ulong mid)
    {
        var mail = Bodies.FirstOrDefault(m => m.Mid == mid);
        if (mail == null || mail.Stat >= 2)
            return null;
        var taken = new List<RewardItem>(mail.Attachments);
        mail.Attachments.Clear();
        mail.Stat = 2;
        if (_player.Data.MailBox.TryGetValue(mid, out var entry))
            entry.Stat = 2;
        Persist();
        return taken;
    }

    public bool Delete(ulong mid)
    {
        var mail = Bodies.FirstOrDefault(m => m.Mid == mid);
        if (mail == null)
            return false;
        Bodies.Remove(mail);
        _player.Data.MailBox.Remove(mid);
        Persist();
        return true;
    }

    private void Persist()
    {
        _db.Save((int)_player.Data.Pid, StoreKey, Bodies);
        _player.Save();
    }
}

public static class MailSystemExtensions
{
    public static MailSystem Mail(this PlayerInstance player, SnowbreakTC.Database.SnowbreakDb db) => new(player, db);
}
