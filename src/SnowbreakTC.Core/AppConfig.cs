using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnowbreakTC.Core;

public sealed class AppConfig
{
    [JsonPropertyName("game")] public GameConfig Game { get; set; } = new();
    [JsonPropertyName("sdk")] public SdkConfig Sdk { get; set; } = new();
    [JsonPropertyName("database")] public DbConfig Database { get; set; } = new();
    [JsonPropertyName("admin")] public AdminConfig Admin { get; set; } = new();
    [JsonPropertyName("hotfix")] public HotfixConfig Hotfix { get; set; } = new();
    [JsonPropertyName("dlc")] public DlcConfig Dlc { get; set; } = new();
    [JsonPropertyName("resource_path")] public string ResourcePath { get; set; } = "Resources";
    [JsonPropertyName("data_path")] public string DataPath { get; set; } = "Data";

    public static AppConfig Load(string path = "config.json")
    {
        if (!File.Exists(path))
            return new AppConfig();
        return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path),
            new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip }) ?? new AppConfig();
    }
}

public sealed class GameConfig
{
    [JsonPropertyName("name")] public string Name { get; set; } = "SnowbreakTC";
    [JsonPropertyName("bind")] public string BindAddress { get; set; } = "0.0.0.0";
    [JsonPropertyName("port")] public int Port { get; set; } = 20000;
    [JsonPropertyName("public_host")] public string PublicHost { get; set; } = "127.0.0.1";
    [JsonPropertyName("world_channel")] public uint WorldChannel { get; set; } = 1;
    [JsonPropertyName("area_id")] public uint AreaId { get; set; } = 1;
    [JsonPropertyName("timezone")] public int TimeZone { get; set; } = 8;
    [JsonPropertyName("max_connections")] public int MaxConnections { get; set; } = 4096;
    [JsonPropertyName("vigor_limit")] public uint VigorLimit { get; set; } = 240;
    [JsonPropertyName("vigor_regen_minutes")] public uint VigorRegenMinutes { get; set; } = 6;
}

public sealed class SdkConfig
{
    [JsonPropertyName("bind")] public string BindAddress { get; set; } = "0.0.0.0";
    [JsonPropertyName("port")] public int Port { get; set; } = 443;
    [JsonPropertyName("extra_tls_ports")] public int[] ExtraTlsPorts { get; set; } = [13443, 18443, 31443];
    [JsonPropertyName("public_host")] public string PublicHost { get; set; } = "127.0.0.1";
    [JsonPropertyName("auto_create_account")] public bool AutoCreateAccount { get; set; } = true;
    [JsonPropertyName("cert_dir")] public string CertDir { get; set; } = "Data/Cert";
}

public sealed class DbConfig
{
    [JsonPropertyName("file")] public string File { get; set; } = "Data/Database/snowbreak.db";
    [JsonPropertyName("autosave_seconds")] public int AutoSaveSeconds { get; set; } = 60;
}

public sealed class DlcConfig
{
    [JsonPropertyName("era_pin")] public string EraPin { get; set; } = "";
}

public sealed class AdminConfig
{
    [JsonPropertyName("bind")] public string BindAddress { get; set; } = "127.0.0.1";
    [JsonPropertyName("port")] public int Port { get; set; } = 18080;
    [JsonPropertyName("token")] public string Token { get; set; } = "";
}

public sealed class HotfixConfig
{
    [JsonPropertyName("versions")] public Dictionary<string, string> Versions { get; set; } = [];
    [JsonPropertyName("aes_keys")] public Dictionary<string, string> AesKeys { get; set; } = [];
}
