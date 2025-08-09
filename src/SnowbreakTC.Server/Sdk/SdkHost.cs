using System.Security.Authentication;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnowbreakTC.Core;
using SnowbreakTC.Database;
using SnowbreakTC.Server.Game;

namespace SnowbreakTC.Server.Sdk;

public static class SdkHost
{
    public static IHost Build(AppConfig config, SnowbreakDb db, TokenService tokens, GameServer game)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o =>
        {
            var bind = System.Net.IPAddress.Parse(config.Sdk.BindAddress);
            foreach (var port in new[] { config.Sdk.Port }.Concat(config.Sdk.ExtraTlsPorts))
            {
                o.Listen(bind, port, l => l.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificateSelector = (_, sni) => Certs.GetOrCreate(config.Sdk.CertDir, sni),
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                }));
            }
        });
        var app = builder.Build();
        app.Use(async (ctx, next) =>
        {
            ctx.Response.OnStarting(() =>
            {
                ctx.Response.Headers["X-Sdk-Node"] = Cluster.NodeTag;
                return Task.CompletedTask;
            });
            await next();
        });
        app.UseWebSockets();
        app.Run(async ctx =>
        {
            if (ctx.WebSockets.IsWebSocketRequest && ctx.Request.Path.Value?.ToLowerInvariant() == "/ws/gui-console")
            {
                await WsConsole.HandleAsync(ctx, game, config);
                return;
            }
            await Route(ctx, config, db, tokens);
        });
        return app;
    }

    private static readonly byte[] Favicon = Enumerable.Range(0, 64)
        .Select(i => (byte)(Cluster.SchemaVersion >> ((i & 3) * 8) ^ i * 0x9E))
        .ToArray();

    private static async Task Route(HttpContext ctx, AppConfig config, SnowbreakDb db, TokenService tokens)
    {
        var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? "/";
        if (path.Contains("hotfix") || path.Contains("resourceversion"))
        {
            await HotfixDispatch.HandleAsync(ctx, config, ctx.Request.Query["version"].FirstOrDefault());
            return;
        }
        if (JinshanRoutes.Match(path))
        {
            await JinshanRoutes.Handle(ctx, config, db, tokens);
            return;
        }
        switch (path)
        {
            case "/query":
            case "/api/serverlist":
            case var p when p.StartsWith("/query_version"):
                await Write(ctx, ServerList(config, ""));
                return;

            case "/seasun/login":
            case "/seasun/loginbytoken":
                await Write(ctx, await Login(ctx, config, db, tokens));
                return;

            case "/seasun/getaccountinfoforgame":
                await Write(ctx, await AccountInfoForGame(ctx, db));
                return;

            case var p when p.StartsWith("/account/query-uid"):
                await Write(ctx, QueryUid(ctx, db));
                return;

            case "/getgameconfig":
                await Write(ctx, GameConfig());
                return;

            case "/seasun/config":
                await Write(ctx, SeasunConfig());
                return;

            case "/health":
                await Write(ctx, new Dictionary<string, object?> { ["status"] = "ok", ["service"] = config.Game.Name });
                return;

            case "/favicon.ico":
                ctx.Response.ContentType = "image/x-icon";
                await ctx.Response.Body.WriteAsync(Favicon);
                return;

            case "/robots.txt":
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.WriteAsync($"User-agent: *\nDisallow: /\n# node {Cluster.NodeTag}\n");
                return;

            default:
                if (path.Contains("server") || path.Contains("version"))
                    await Write(ctx, ServerList(config, ""));
                else
                    await Write(ctx, new Dictionary<string, object?>
                    {
                        ["code"] = 0, ["msg"] = "ok",
                        ["path"] = path, ["service"] = config.Game.Name,
                        ["node"] = Cluster.NodeTag,
                    });
                return;
        }
    }

    private static Dictionary<string, object?> ServerList(AppConfig config, string version) => new()
    {
        ["code"] = 0,
        ["ret"] = 0,
        ["msg"] = "ok",
        ["message"] = "ok",
        ["version"] = version,
        ["node"] = Cluster.NodeTag,
        ["server_time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        ["servers"] = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1,
                ["server_id"] = 1,
                ["name"] = config.Game.Name,
                ["title"] = config.Game.Name,
                ["host"] = config.Game.PublicHost,
                ["ip"] = config.Game.PublicHost,
                ["port"] = config.Game.Port,
                ["status"] = 1,
                ["state"] = 1,
                ["is_open"] = true,
                ["open"] = true,
                ["recommend"] = true,
            },
        },
        ["game_server"] = new Dictionary<string, object?>
        {
            ["host"] = config.Game.PublicHost,
            ["ip"] = config.Game.PublicHost,
            ["port"] = config.Game.Port,
        },
        ["http_server"] = new Dictionary<string, object?>
        {
            ["host"] = config.Sdk.PublicHost,
            ["port"] = config.Sdk.Port,
        },
    };

    private static async Task<Dictionary<string, object?>> Login(HttpContext ctx, AppConfig config, SnowbreakDb db, TokenService tokens)
    {
        var form = ctx.Request.HasFormContentType ? ctx.Request.Form : null;
        Dictionary<string, JsonElement>? json = null;
        if (ctx.Request.HasJsonContentType())
        {
            try
            {
                json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await ReadBody(ctx));
            }
            catch
            {
                json = null;
            }
        }
        string? Query(string name) =>
            ctx.Request.Query[name].FirstOrDefault()
            ?? form?[name].FirstOrDefault()
            ?? (json != null && json.TryGetValue(name, out var v)
                ? v.ValueKind == JsonValueKind.Number ? v.GetRawText() : v.GetString()
                : null);

        var uid = Query("uid");
        var token = Query("token");
        var email = Query("email");

        AccountRecord? account = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            account = db.GetAccountByEmail(email.Trim());
            if (account == null && config.Sdk.AutoCreateAccount)
            {
                account = db.CreateAccount(email.Trim(), email.Split('@')[0]);
                tokens.Issue(account);
            }
        }
        else if (!string.IsNullOrWhiteSpace(token))
        {
            account = tokens.ResolveCombo(token) ?? tokens.ResolveDispatch(token);
        }
        else if (int.TryParse(uid, out var parsedUid))
        {
            account = db.GetAccount(parsedUid);
        }

        if (account == null)
            return Fail("Account not found.");

        var (_, comboToken) = tokens.Issue(account);
        return Ok(new Dictionary<string, object?>
        {
            ["uid"] = account.Uid,
            ["token"] = comboToken,
            ["nickname"] = account.Username,
            ["passportId"] = account.Uid.ToString(),
            ["ksOpenId"] = $"ks_{account.Uid}",
            ["status"] = 0,
            ["type"] = "guest",
            ["isFirstLogin"] = false,
            ["associatedAccounts"] = Array.Empty<string>(),
            ["isNeedKoreaSciAuth"] = false,
            ["thirdPartyUid"] = "",
            ["playerFillAgeUrl"] = "",
        });
    }

    private static async Task<Dictionary<string, object?>> AccountInfoForGame(HttpContext ctx, SnowbreakDb db)
    {
        var uid = ctx.Request.Query["uid"].FirstOrDefault();
        var account = int.TryParse(uid, out var parsedUid) ? db.GetAccount(parsedUid) : null;
        if (account == null)
            return Fail("Account not found.");
        return Ok(new Dictionary<string, object?>
        {
            ["bindAccountTypes"] = new[] { "google" },
            ["channelUid"] = account.Uid.ToString(),
            ["loginAccountType"] = "google",
            ["nickName"] = account.Username,
            ["passportId"] = account.Uid.ToString(),
            ["uid"] = $"seasun__{account.Uid}",
        });
    }

    private static Dictionary<string, object?> QueryUid(HttpContext ctx, SnowbreakDb db)
    {
        var auth = ctx.Request.Query["authInfo"].FirstOrDefault();
        int? uid = null;
        try
        {
            var normalized = Uri.UnescapeDataString(auth ?? "").Trim();
            var padding = normalized.Length % 4;
            if (padding > 0)
                normalized = normalized.PadRight(normalized.Length + 4 - padding, '=');
            using var doc = JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(normalized)));
            if (doc.RootElement.TryGetProperty("uid", out var v) && int.TryParse(v.GetString(), out var parsed))
                uid = parsed;
        }
        catch
        {
            // fall through to not-found
        }
        var account = uid.HasValue ? db.GetAccount(uid.Value) : null;
        if (account == null)
            return Fail("Account not found.");
        return new Dictionary<string, object?>
        {
            ["code"] = "0",
            ["msg"] = "success",
            ["data"] = new Dictionary<string, object?> { ["uid"] = $"seasun__{account.Uid}" },
        };
    }

    private static Dictionary<string, object?> GameConfig() => new()
    {
        ["code"] = "0",
        ["msg"] = "success",
        ["data"] = new Dictionary<string, object?>
        {
            ["loginType"] = new[] { "channel" },
            ["enableReportDataToDouyin"] = false,
            ["openActivationCode"] = false,
            ["appDownLoadUrl"] = "",
            ["agreementUpdateTime"] = "1728552600000",
            ["qqGroup"] = null,
        },
    };

    private static Dictionary<string, object?> SeasunConfig() => new()
    {
        ["code"] = 0,
        ["msg"] = "操作成功",
        ["data"] = new Dictionary<string, object?>
        {
            ["payType"] = new[] { "mycard" },
            ["loginType"] = new[] { "mail", "google", "twitter", "guest", "steam" },
            ["closeGeetest"] = false,
            ["payChannel"] = null,
            ["accountPublicKey"] = "",
            ["initPrivacyUpdateTime"] = 0,
        },
    };

    private static Dictionary<string, object?> Ok(Dictionary<string, object?> data) => new()
    {
        ["code"] = 0,
        ["msg"] = "操作成功",
        ["data"] = data,
    };

    private static Dictionary<string, object?> Fail(string message) => new()
    {
        ["code"] = 1001,
        ["msg"] = message,
        ["data"] = null,
    };

    private static async Task Write(HttpContext ctx, object body)
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static async Task<string> ReadBody(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        return await reader.ReadToEndAsync();
    }
}
