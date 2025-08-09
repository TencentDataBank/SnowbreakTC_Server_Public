using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SnowbreakTC.Core;
using SnowbreakTC.Database;

namespace SnowbreakTC.Server.Sdk;

public static class JinshanRoutes
{
    public const string ChannelId = "pc_jinshan";
    public const string PlanId = "30006";
    public const string SdkVersion = "5.2.0";
    public const string ListVersion = "3.1.0";

    public static bool Match(string path)
    {
        var p = path.ToLowerInvariant();
        return p.StartsWith("/v6/")
            || p is "/loginbytoken" or "/data/report"
            || p.StartsWith("/account/qrcode/")
            || p.StartsWith("/pay/qrcode/")
            || p.StartsWith("/push/gift/")
            || p.StartsWith("/privacy/");
    }

    public static async Task Handle(HttpContext ctx, AppConfig config, SnowbreakDb db, TokenService tokens)
    {
        try
        {
            var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? "/";
            var payload = path switch
            {
                "/loginbytoken" => await LoginAsync(ctx, config, db, tokens),
                "/data/report" => new Dictionary<string, object?> { ["code"] = 0, ["msg"] = "ok" },
                _ when path.StartsWith("/account/qrcode/") => QrCode(ctx, "account"),
                _ when path.StartsWith("/pay/qrcode/") => QrCode(ctx, "pay"),
                _ when path.StartsWith("/push/gift/") => new Dictionary<string, object?>
                {
                    ["code"] = 0, ["msg"] = "兑换成功", ["responseSuccess"] = true, ["data"] = Empty,
                },
                _ when path.StartsWith("/privacy/") => null,
                _ => await V6Async(ctx, config, db, tokens, path),
            };
            if (payload == null)
            {
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync("<!doctype html><html><head><meta charset=\"utf-8\"></head><body><p>SnowbreakTC local emulation.</p></body></html>");
                return;
            }
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[jinshan] " + ctx.Request.Path + " -> " + ex);
            ctx.Response.StatusCode = 500;
        }
        finally
        {
            BodyCache.Remove(ctx);
        }
    }

    private static async Task<Dictionary<string, object?>> V6Async(HttpContext ctx, AppConfig config, SnowbreakDb db, TokenService tokens, string path)
    {
        var rest = path.Length > 4 ? path[4..] : "";
        if (rest.StartsWith("config/") || rest.Contains("config"))
            return Envelope(V6Config(config));
        if (rest.Contains("logincaptcha"))
            return new Dictionary<string, object?>
            {
                ["code"] = 1, ["msg"] = "操作成功", ["responseSuccess"] = true,
                ["fuzzyMobile"] = null, ["fuzzyMobileList"] = null,
            };
        if (rest.Contains("sendsmscaptcha"))
            return Envelope(new Dictionary<string, object?>
            {
                ["sendInterval"] = 60,
                ["expiredTime"] = 600,
            });
        if (rest.Contains("server") || rest.Contains("dispatch") || rest.Contains("zone"))
            return ServerList(config);
        if (rest.Contains("login") || rest.Contains("token") || rest.Contains("passport"))
            return await LoginAsync(ctx, config, db, tokens);
        if (rest.Contains("logout"))
            return Envelope(new Dictionary<string, object?> { ["appId"] = rest.Split('/').LastOrDefault() ?? "" });
        if (rest.Contains("player") && rest.Contains("refresh"))
        {
            var uid = await BodyValue(ctx, "uid") ?? "0";
            var nickname = int.TryParse(uid, out var parsed) ? db.GetAccount(parsed)?.Username ?? "cn_local" : "cn_local";
            return Envelope(new Dictionary<string, object?>
            {
                ["uid"] = uid, ["userId"] = uid, ["nickname"] = nickname, ["status"] = 0,
            });
        }
        return Envelope(new Dictionary<string, object?> { ["path"] = path });
    }

    private static Dictionary<string, object?> V6Config(AppConfig config)
    {
        var sdkBase = $"https://{config.Sdk.PublicHost}:{config.Sdk.Port}";
        var jsBase = sdkBase;
        return new Dictionary<string, object?>
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["appId"] = "200001086",
                ["channelId"] = ChannelId,
                ["planId"] = PlanId,
                ["sdkVersion"] = SdkVersion,
                ["captchaType"] = "none",
                ["closeGeetest"] = true,
                ["enableSms"] = true,
                ["enableAccountLogin"] = true,
                ["enableQrCodeLogin"] = true,
                ["enableRealName"] = false,
                ["loginUrl"] = jsBase,
                ["serverListUrl"] = jsBase,
                ["dispatchUrl"] = sdkBase,
                ["userInfoUrl"] = sdkBase,
                ["payUrl"] = sdkBase,
                ["qrCodeUrl"] = sdkBase,
                ["giftUrl"] = sdkBase,
                ["reportUrl"] = sdkBase + "/data/report",
                ["noticeUrl"] = jsBase,
                ["versionUrl"] = jsBase,
                ["webUrl"] = jsBase,
            },
            ["appId"] = "200001086",
            ["closeGeetest"] = true,
            ["captchaType"] = "none",
            ["enableSms"] = true,
            ["loginType"] = new[] { "channel" },
            ["accountLoginType"] = new[] { "phone", "email" },
            ["qrCodeLogin"] = true,
            ["protocolVersion"] = "1.0",
            ["privacyVersion"] = 1,
            ["userAgreementVersion"] = 1,
        };
    }

    private static async Task<Dictionary<string, object?>> LoginAsync(HttpContext ctx, AppConfig config, SnowbreakDb db, TokenService tokens)
    {
        var passportId = await BodyValue(ctx, "passportId");
        var mobile = await BodyValue(ctx, "mobile")
            ?? await BodyValue(ctx, "phone")
            ?? await BodyValue(ctx, "account")
            ?? await BodyValue(ctx, "userName")
            ?? await BodyValue(ctx, "username")
            ?? await BodyValue(ctx, "email");
        var password = await BodyValue(ctx, "password");

        var identity = (string.IsNullOrWhiteSpace(passportId) ? mobile : passportId)?.Trim();
        if (string.IsNullOrWhiteSpace(identity))
            identity = BodyFind(ctx, "uid");
        var account = Resolve(db, identity);
        if (account == null)
            return new Dictionary<string, object?> { ["code"] = 255, ["msg"] = "登录失败", ["responseSuccess"] = false };

        var (_, token) = tokens.Issue(account);
        var uid = account.Uid.ToString();
        var fields = new Dictionary<string, object?>
        {
            ["uid"] = uid,
            ["userId"] = uid,
            ["userIdStr"] = uid,
            ["token"] = token,
            ["accessToken"] = token,
            ["sessionId"] = token,
            ["ksOpenId"] = $"ks_{uid}",
            ["openId"] = uid,
            ["passportId"] = passportId ?? uid,
            ["nickname"] = account.Username,
            ["type"] = "guest",
            ["status"] = 0,
            ["accountType"] = 3,
            ["accountStatus"] = 0,
            ["mobile"] = mobile ?? "",
            ["fuzzyMobile"] = mobile ?? "",
            ["email"] = passportId ?? "",
            ["canTouristPay"] = false,
            ["isFirstLogin"] = false,
            ["isNeedKoreaSciAuth"] = false,
            ["expireTime"] = 0,
            ["expiresIn"] = 0,
            ["associatedAccounts"] = Array.Empty<string>(),
            ["thirdPartyUid"] = "",
            ["playerFillAgeUrl"] = "",
        };
        var envelope = Envelope(fields);
        envelope["uid"] = uid;
        envelope["userId"] = uid;
        envelope["token"] = token;
        envelope["accessToken"] = token;
        envelope["ksOpenId"] = $"ks_{uid}";
        envelope["nickname"] = account.Username;
        envelope["passportId"] = passportId ?? uid;
        envelope["mobile"] = mobile ?? "";
        envelope["fuzzyMobile"] = mobile ?? "";
        envelope["email"] = passportId ?? "";
        envelope["type"] = "guest";
        envelope["accountType"] = 3;
        envelope["status"] = 0;
        envelope["data"] = fields;
        return envelope;
    }

    private static AccountRecord? Resolve(SnowbreakDb db, string? identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
            return null;
        var id = identity.Trim();
        AccountRecord? account = null;
        if (int.TryParse(id, out var uid))
            account = db.GetAccount(uid);
        var derivedEmail = id.Contains('@') ? id : $"cn{id}@local";
        account ??= db.GetAccountByEmail(id) ?? db.GetAccountByEmail(derivedEmail);
        if (account == null && (id.Contains('@') || id.All(char.IsDigit)))
            account = db.CreateAccount(derivedEmail, id.Contains('@') ? id.Split('@')[0] : id);
        return account;
    }

    private static Dictionary<string, object?> QrCode(HttpContext ctx, string kind)
    {
        var action = ctx.Request.Path.Value?.Split('/').LastOrDefault() ?? "";
        return Envelope(new Dictionary<string, object?>
        {
            ["status"] = 1,
            ["qrCode"] = "",
            ["qrCodeUrl"] = "",
            ["token"] = "",
            ["expireTime"] = 600,
            ["action"] = action,
            ["kind"] = kind,
        });
    }

    public static Dictionary<string, object?> ServerList(AppConfig config)
    {
        var gamePort = config.Game.Port;
        return new Dictionary<string, object?>
        {
            ["code"] = 1,
            ["msg"] = "操作成功",
            ["responseSuccess"] = true,
            ["version"] = ListVersion,
            ["server_time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["servers"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1, ["server_id"] = 1,
                    ["name"] = config.Game.Name, ["title"] = config.Game.Name,
                    ["host"] = config.Game.PublicHost, ["ip"] = config.Game.PublicHost,
                    ["port"] = gamePort, ["status"] = 1, ["state"] = 1,
                    ["is_open"] = true, ["open"] = true, ["recommend"] = true,
                },
            },
            ["game_server"] = new Dictionary<string, object?>
            {
                ["host"] = config.Game.PublicHost, ["ip"] = config.Game.PublicHost, ["port"] = gamePort,
            },
            ["http_server"] = new Dictionary<string, object?>
            {
                ["host"] = config.Sdk.PublicHost, ["port"] = config.Sdk.Port,
            },
        };
    }

    private static Dictionary<string, object?> Envelope(Dictionary<string, object?> data) => new()
    {
        ["code"] = 1,
        ["msg"] = "操作成功",
        ["responseSuccess"] = true,
        ["data"] = data,
    };

    private static async Task<string?> BodyValue(HttpContext ctx, string name)
    {
        var raw = await BodyAsync(ctx);
        if (raw == null)
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return Find(doc.RootElement, name)?.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static readonly Dictionary<HttpContext, string> BodyCache = new();

    private static async Task<string?> BodyAsync(HttpContext ctx)
    {
        if (BodyCache.TryGetValue(ctx, out var cached))
            return cached;
        if (!ctx.Request.HasJsonContentType())
            return null;
        using var reader = new StreamReader(ctx.Request.Body);
        var raw = await reader.ReadToEndAsync();
        BodyCache[ctx] = raw;
        return raw;
    }

    private static string? BodyFind(HttpContext ctx, string name)
    {
        var raw = BodyAsync(ctx).GetAwaiter().GetResult();
        if (raw == null)
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var v = Find(doc.RootElement, name);
            if (v == null)
                return null;
            return v.Value.ValueKind == JsonValueKind.Number ? v.Value.GetRawText() : v.Value.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static JsonElement? Find(JsonElement node, string name)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var prop in node.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(prop.Value.GetString()))
                return prop.Value;
        }
        foreach (var prop in node.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Object && Find(prop.Value, name) is { } nested)
                return nested;
        }
        return null;
    }

    private static readonly Dictionary<string, object?> Empty = [];
}
