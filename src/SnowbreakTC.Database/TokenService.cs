using System.Security.Cryptography;
using System.Text;

namespace SnowbreakTC.Database;

public sealed class TokenService
{
    private const long TokenLifetimeDays = 30;

    private readonly SnowbreakDb _db;
    private readonly byte[] _key;

    public TokenService(SnowbreakDb db, string keyDir)
    {
        _db = db;
        Directory.CreateDirectory(keyDir);
        var keyFile = Path.Combine(keyDir, "token.key");
        if (File.Exists(keyFile))
            _key = File.ReadAllBytes(keyFile);
        else
        {
            _key = RandomNumberGenerator.GetBytes(32);
            File.WriteAllBytes(keyFile, _key);
        }
    }

    public (string Combo, string Dispatch) Issue(AccountRecord account)
    {
        var combo = Sign($"c.{account.Uid}");
        var dispatch = Sign($"d.{account.Uid}");
        account.ComboToken = combo;
        account.DispatchToken = dispatch;
        _db.SetTokens(account.Uid, combo, dispatch);
        return (combo, dispatch);
    }

    public AccountRecord? ResolveCombo(string token) => Resolve(token, 'c', _db.GetAccountByToken(token, combo: true));

    public AccountRecord? ResolveDispatch(string token) => Resolve(token, 'd', _db.GetAccountByToken(token, combo: false));

    private AccountRecord? Resolve(string token, char kind, AccountRecord? stored)
    {
        if (stored != null)
            return stored;
        var parts = token.Split('.');
        if (parts.Length != 3 || parts[0].Length != 1 || parts[0][0] != kind)
            return null;
        if (!int.TryParse(parts[1], out var uid))
            return null;
        return Verify(token) ? _db.GetAccount(uid) : null;
    }

    private string Sign(string payload)
    {
        var expiry = DateTimeOffset.UtcNow.AddDays(TokenLifetimeDays).ToUnixTimeSeconds();
        var body = $"{payload}.{expiry}";
        var mac = HMACSHA1.HashData(_key, Encoding.UTF8.GetBytes(body));
        return $"{body}.{Convert.ToHexString(mac)[..8].ToLowerInvariant()}";
    }

    private bool Verify(string token)
    {
        var idx = token.LastIndexOf('.');
        if (idx < 0)
            return false;
        var body = token[..idx];
        var mac = token[(idx + 1)..];
        var expect = Convert.ToHexString(HMACSHA1.HashData(_key, Encoding.UTF8.GetBytes(body)))[..8].ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(mac), Encoding.ASCII.GetBytes(expect));
    }
}
