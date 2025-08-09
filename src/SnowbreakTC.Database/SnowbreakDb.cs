using Microsoft.Data.Sqlite;

namespace SnowbreakTC.Database;

public sealed class SnowbreakDb : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SnowbreakDb(string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        _conn = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = file,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString());
        _conn.Open();
        Exec("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");
        Exec("""
            CREATE TABLE IF NOT EXISTS accounts(
                uid INTEGER PRIMARY KEY,
                username TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                combo_token TEXT NOT NULL DEFAULT '',
                dispatch_token TEXT NOT NULL DEFAULT '',
                created_at INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS documents(
                uid INTEGER NOT NULL,
                key TEXT NOT NULL,
                json TEXT NOT NULL,
                PRIMARY KEY(uid, key)
            );
            """);
        Exec($"PRAGMA user_version={Core.Cluster.SchemaVersion};");
        Exec("CREATE TABLE IF NOT EXISTS _cluster(node TEXT NOT NULL, joined_at INTEGER NOT NULL);");
        Exec($"INSERT OR IGNORE INTO _cluster VALUES('{Core.Cluster.Node}', strftime('%s','now'));");
    }

    public int NextUid()
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(uid), 10000) + 1 FROM accounts";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        finally { _lock.Release(); }
    }

    public AccountRecord? GetAccount(int uid)
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT uid, username, email, combo_token, dispatch_token, created_at FROM accounts WHERE uid=$uid";
            cmd.Parameters.AddWithValue("$uid", uid);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadAccount(r) : null;
        }
        finally { _lock.Release(); }
    }

    public AccountRecord? GetAccountByEmail(string email)
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT uid, username, email, combo_token, dispatch_token, created_at FROM accounts WHERE email=$email";
            cmd.Parameters.AddWithValue("$email", email);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadAccount(r) : null;
        }
        finally { _lock.Release(); }
    }

    public AccountRecord? GetAccountByToken(string token, bool combo)
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = combo
                ? "SELECT uid, username, email, combo_token, dispatch_token, created_at FROM accounts WHERE combo_token=$tok AND combo_token != ''"
                : "SELECT uid, username, email, combo_token, dispatch_token, created_at FROM accounts WHERE dispatch_token=$tok AND dispatch_token != ''";
            cmd.Parameters.AddWithValue("$tok", token);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadAccount(r) : null;
        }
        finally { _lock.Release(); }
    }

    public AccountRecord CreateAccount(string email, string username)
    {
        var uid = NextUid();
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO accounts(uid, username, email, created_at) VALUES($uid, $name, $email, $time)";
            cmd.Parameters.AddWithValue("$uid", uid);
            cmd.Parameters.AddWithValue("$name", username);
            cmd.Parameters.AddWithValue("$email", email);
            cmd.Parameters.AddWithValue("$time", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }
        finally { _lock.Release(); }
        return GetAccount(uid)!;
    }

    public void SetTokens(int uid, string comboToken, string dispatchToken)
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE accounts SET combo_token=$c, dispatch_token=$d WHERE uid=$uid";
            cmd.Parameters.AddWithValue("$c", comboToken);
            cmd.Parameters.AddWithValue("$d", dispatchToken);
            cmd.Parameters.AddWithValue("$uid", uid);
            cmd.ExecuteNonQuery();
        }
        finally { _lock.Release(); }
    }

    public T? Load<T>(int uid, string key)
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT json FROM documents WHERE uid=$uid AND key=$key";
            cmd.Parameters.AddWithValue("$uid", uid);
            cmd.Parameters.AddWithValue("$key", key);
            var json = cmd.ExecuteScalar() as string;
            return json == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        finally { _lock.Release(); }
    }

    public void Save<T>(int uid, string key, T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO documents(uid, key, json) VALUES($uid, $key, $json)
                ON CONFLICT(uid, key) DO UPDATE SET json=excluded.json
                """;
            cmd.Parameters.AddWithValue("$uid", uid);
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$json", json);
            cmd.ExecuteNonQuery();
        }
        finally { _lock.Release(); }
    }

    public void Delete(int uid, string key)
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM documents WHERE uid=$uid AND key=$key";
            cmd.Parameters.AddWithValue("$uid", uid);
            cmd.Parameters.AddWithValue("$key", key);
            cmd.ExecuteNonQuery();
        }
        finally { _lock.Release(); }
    }

public void DeletePlayerDocuments(int uid)
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM documents WHERE uid=$uid";
            cmd.Parameters.AddWithValue("$uid", uid);
            cmd.ExecuteNonQuery();
        }
        finally { _lock.Release(); }
    }

    public List<int> AllUids()
    {
        _lock.Wait();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT uid FROM accounts ORDER BY uid";
            using var r = cmd.ExecuteReader();
            var list = new List<int>();
            while (r.Read())
                list.Add(r.GetInt32(0));
            return list;
        }
        finally { _lock.Release(); }
    }

    private static AccountRecord ReadAccount(SqliteDataReader r) => new()
    {
        Uid = r.GetInt32(0),
        Username = r.GetString(1),
        Email = r.GetString(2),
        ComboToken = r.IsDBNull(3) ? "" : r.GetString(3),
        DispatchToken = r.IsDBNull(4) ? "" : r.GetString(4),
        CreatedAt = r.GetInt64(5),
    };

    private void Exec(string sql)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _conn.Dispose();
}

public sealed record AccountRecord
{
    public int Uid { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string ComboToken { get; set; } = "";
    public string DispatchToken { get; set; } = "";
    public long CreatedAt { get; init; }
}
