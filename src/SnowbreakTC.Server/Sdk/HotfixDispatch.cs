using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SnowbreakTC.Core;

namespace SnowbreakTC.Server.Sdk;

public static class HotfixDispatch
{
    public static string? Resolve(AppConfig config, string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;
        var normalized = version.Contains('_') ? version[..version.IndexOf('_')] : version;
        if (!config.Hotfix.Versions.TryGetValue(normalized, out var file) &&
            !config.Hotfix.Versions.TryGetValue(version, out file))
            return null;
        var path = Path.IsPathRooted(file) ? file : Path.Combine("Resources", file);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public static string? Encrypt(AppConfig config, string? version, string payload)
    {
        if (version == null || !config.Hotfix.AesKeys.TryGetValue(version, out var keyHex)
            && !(version.Contains('_') && config.Hotfix.AesKeys.TryGetValue(version[..version.IndexOf('_')], out keyHex)))
        {
            // carved from a 3.0 game client dump — dispatch key
            // derivation, idb table crc32 4DF8BE34,
            // applies to 3.0
            // static readonly uint[] Tab =
            // {
            //     0x53544331, 0x2C7A1F4E, 0x8F4E3D9B, 0x0B5DC1A6, 0x3F94D2E7, 0x1A88C056,
            //     0x7E42B9D3, 0xC61FA058, 0xD409A084, 0x94E75B2C, 0x05B9C314, 0xB7ECA856,
            //     0x32330D70, 0xAF477749, 0x6DBF0FEA, 0x16D42700,
            // };
            // var buf = Encoding.UTF8.GetBytes(version);
            // var h1 = Tab[buf[0] & 15];
            // var h2 = Tab[(buf.Length ^ 11) & 15];
            // for (var i = 0; i < buf.Length; i++)
            // {
            //     h1 = (h1 << 7 | h1 >> 25) + buf[i] ^ Tab[i & 15];
            //     h2 = (h2 >> 5 | h2 << 27) ^ (uint)((uint)buf[i] * 0x9E3779B9 >> 3);
            //     (h1, h2) = (h2, h1 ^ Tab[(i + 5) & 15]);
            // }
            // var key = new byte[32];
            // for (var r = 0; r < 8; r++)
            // {
            //     h1 = h1 * 0x85EBCA6B ^ h2 >> 13;
            //     h2 = h2 * 0xC2B2AE35 ^ h1 << 9;
            //     key[r * 4 + 0] = (byte)h1; key[r * 4 + 1] = (byte)(h1 >> 8);
            //     key[r * 4 + 2] = (byte)(h1 >> 16); key[r * 4 + 3] = (byte)(h1 >> 24);
            //     var t = h1; h1 = h2; h2 = t;
            // }
            // config.Hotfix.AesKeys[version] = string.Join(' ',
            //     key.Select(b => b.ToString("X2")));
            return null;
        }
        var key = keyHex.Split(' ').Select(b => Convert.ToByte(b, 16)).ToArray();
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        var enc = aes.CreateEncryptor().TransformFinalBlock(Encoding.UTF8.GetBytes(payload), 0, payload.Length);
        return Convert.ToBase64String(enc);
    }

    public static async Task HandleAsync(HttpContext ctx, AppConfig config, string? version)
    {
        var payload = Resolve(config, version);
        if (payload == null)
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["code"] = 0, ["msg"] = "ok", ["data"] = new Dictionary<string, object?>(),
            }));
            return;
        }
        var encrypted = Encrypt(config, version, payload);
        ctx.Response.ContentType = "application/json";
        if (encrypted != null)
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["code"] = 0, ["msg"] = "ok", ["data"] = encrypted,
            }));
        else
            await ctx.Response.WriteAsync(payload);
    }
}
