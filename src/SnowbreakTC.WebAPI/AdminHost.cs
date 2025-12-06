using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnowbreakTC.Core;
using SnowbreakTC.Database;

namespace SnowbreakTC.WebAPI;

public interface IAdminGame
{
    int OnlineCount { get; }
    IEnumerable<(ulong Pid, string Name, uint Level)> OnlinePlayers();
    (bool Found, string Name, uint Level, ulong Exp, uint Vigor, int Items, bool Online)? PlayerInfo(int uid);
    int KickPlayer(ulong pid);
    int SaveAll();
    int GiveToAll(ulong template, uint count);
    int GiveToOne(ulong pid, ulong template, uint count);
}

public sealed record GmResult(bool Success, List<string> Messages);

public static class GmCatalog
{
    public static readonly (string Name, string Usage, string Description, string[] Aliases)[] Entries =
    [
        ("grant", "grant item <g> <d> <p> [count] | card <d> <p> | weapon <d> <p> | money <type> <val> | level <n> | template <id> | allcards | allweapons", "grant items, characters, weapons, currency or levels to all online players", []),
        ("player", "player list | kick <uid> | create <email>", "list, kick or create players", []),
        ("data", "data attr get|set <gid*10000+sid> [val] | progress on|off", "read or write packed attributes, toggle all-quest-progress", []),
        ("redirect", "redirect on [ip] | off | status", "manage the CN hosts redirect entries", []),
        ("mail", "mail <title>", "send a mail to all online players", []),
        ("server", "server save | stats | shutdown", "save all players, show stats, shutdown", []),
        ("handbook", "handbook", "export the GM handbook to docs/handbook.md", []),
        ("help", "help", "show the command list", []),
    ];
}

public static class AdminHost
{
    public static int ApiCount;

    public static IHost Build(AppConfig config, SnowbreakDb db, IAdminGame? game, Func<string, GmResult>? gmRun = null)
    {
        var gmRunLocal = gmRun;
        var startedAt = DateTime.UtcNow;
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(o =>
        {
            o.Listen(IPAddress.Parse(config.Admin.BindAddress), config.Admin.Port);
        });
        var app = builder.Build();
        app.Run(async ctx =>
        {
            try
            {
                await Route(ctx, config, db, game, startedAt, gmRunLocal);
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        });
        return app;
    }

    private static async Task Route(HttpContext ctx, AppConfig config, SnowbreakDb db, IAdminGame? game, DateTime startedAt, Func<string, GmResult>? gmRun)
    {
        var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? "/";
        if (ctx.Request.Method == "GET" && await TryServeAssetAsync(ctx, path))
            return;

        if (!string.IsNullOrEmpty(config.Admin.Token)
            && path.StartsWith("/api/")
            && ctx.Request.Headers["Authorization"].FirstOrDefault() != $"Bearer {config.Admin.Token}")
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"error\":\"unauthorized\"}");
            return;
        }

        if (path == "/api/console" && ctx.Request.Method == "POST")
        {
            await Console_(ctx, gmRun);
            return;
        }
        object? body = path switch
        {
            "/health" => Health(config, startedAt),
            "/api/health" => Health(config, startedAt),
            "/api/overview" => Overview(config, db, game, startedAt),
            "/api/accounts" => ctx.Request.Method == "POST" ? CreateAccount(ctx, db) : Accounts(ctx, db),
            _ when path.StartsWith("/api/accounts/") && ctx.Request.Method == "DELETE" => DeleteAccount(ctx, db),
            "/api/currencies" => Currencies(db),
            "/api/commands" => Commands(),
            "/api/setup" => Setup(ctx, config),
            "/api/setup/apply" when ctx.Request.Method == "POST" => SetupApply(ctx),
            "/api/replay" => Replay(ctx),
            "/api/online" => Online(game),
            _ when path.StartsWith("/api/players/") => Player(ctx, db, game),
            _ when path.StartsWith("/api/kick/") => Kick(ctx, game),
            _ when path.StartsWith("/api/give/") => Give(ctx, game),
            _ => null,
        };
        if (body == null)
        {
            ctx.Response.StatusCode = 404;
            body = new Dictionary<string, object?> { ["error"] = "not found", ["path"] = path };
        }
        else
        {
            ctx.Response.StatusCode = 200;
        }
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static Dictionary<string, object?> Health(AppConfig config, DateTime startedAt) => new()
    {
        ["ok"] = true,
        ["status"] = "ok",
        ["service"] = config.Game.Name,
        ["db_loaded"] = true,
        ["uptime_sec"] = (int)(DateTime.UtcNow - startedAt).TotalSeconds,
        ["version"] = "1.0.0",
    };

    private static Dictionary<string, object?> Overview(AppConfig config, SnowbreakDb db, IAdminGame? game, DateTime startedAt) => new()
    {
        ["accounts"] = db.AllUids().Count,
        ["online"] = game?.OnlineCount ?? 0,
        ["commands"] = AdminHost.ApiCount,
        ["uptime_sec"] = (int)(DateTime.UtcNow - startedAt).TotalSeconds,
        ["server_name"] = config.Game.Name,
    };

    private static List<Dictionary<string, object?>> Accounts(HttpContext ctx, SnowbreakDb db)
    {
        var q = ctx.Request.Query["q"].FirstOrDefault()?.ToLowerInvariant();
        var page = int.TryParse(ctx.Request.Query["page"], out var p) ? Math.Max(1, p) : 1;
        var size = int.TryParse(ctx.Request.Query["size"], out var s) ? Math.Clamp(s, 1, 200) : 50;
        var list = db.AllUids()
            .Select(uid => db.GetAccount(uid)!)
            .Where(a => q == null || a.Username.ToLowerInvariant().Contains(q) || a.Email.ToLowerInvariant().Contains(q))
            .OrderBy(a => a.Uid)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(a => (Dictionary<string, object?>)new Dictionary<string, object?>
            {
                ["uid"] = a.Uid,
                ["username"] = a.Username,
                ["email"] = a.Email,
                ["created_at"] = a.CreatedAt,
            })
            .ToList();
        return list;
    }

    private static Dictionary<string, object?> CreateAccount(HttpContext ctx, SnowbreakDb db)
    {
        Dictionary<string, JsonElement>? body = null;
        if (ctx.Request.HasJsonContentType())
        {
            try { body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ReadJson(ctx)); }
            catch { body = null; }
        }
        string? email = null;
        if (body != null && body.TryGetValue("email", out var e) && e.ValueKind == JsonValueKind.String)
            email = e.GetString();
        email ??= ctx.Request.Query["email"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(email))
            return new Dictionary<string, object?> { ["error"] = "email required" };
        var account = db.GetAccountByEmail(email) ?? db.CreateAccount(email, email.Split('@')[0]);
        return new Dictionary<string, object?> { ["uid"] = account.Uid, ["email"] = account.Email };
    }

    private static Dictionary<string, object?> DeleteAccount(HttpContext ctx, SnowbreakDb db)
    {
        var uidText = ctx.Request.Path.Value!.Split('/').Last();
        if (!int.TryParse(uidText, out var uid))
            return new Dictionary<string, object?> { ["error"] = "bad uid" };
        var account = db.GetAccount(uid);
        if (account == null)
            return new Dictionary<string, object?> { ["deleted"] = 0 };
        db.DeletePlayerDocuments(uid);
        return new Dictionary<string, object?> { ["deleted"] = 1, ["uid"] = uid };
    }

    private static object Currencies(SnowbreakDb db)
    {
        var rows = db.AllUids().Select(uid =>
        {
            var player = db.Load<SnowbreakTC.Protocol.Messages.PlayerSnapshot>(uid, "player");
            return new Dictionary<string, object?>
            {
                ["uid"] = uid,
                ["level"] = player?.Level ?? 0,
                ["vigor"] = player?.Vigor ?? 0,
                ["money"] = player?.Money ?? new Dictionary<string, int>(),
            };
        }).ToList();
        return rows;
    }

    private static string ReadJson(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        return reader.ReadToEndAsync().GetAwaiter().GetResult();
    }

    private static object Online(IAdminGame? game) => new
    {
        count = game?.OnlineCount ?? 0,
        players = game?.OnlinePlayers().Select(p => new { uid = p.Pid, name = p.Name, level = p.Level }).ToList() ?? [],
    };

    private static object? Player(HttpContext ctx, SnowbreakDb db, IAdminGame? game)
    {
        var uidText = ctx.Request.Path.Value!.Split('/').Last();
        if (!int.TryParse(uidText, out var uid))
            return null;
        var info = game?.PlayerInfo(uid);
        if (info == null || !info.Value.Found)
            return null;
        var v = info.Value;
        return new Dictionary<string, object?>
        {
            ["uid"] = uid,
            ["name"] = v.Name,
            ["level"] = v.Level,
            ["exp"] = v.Exp,
            ["vigor"] = v.Vigor,
            ["items"] = v.Items,
            ["online"] = v.Online,
        };
    }

    private static object? Kick(HttpContext ctx, IAdminGame? game)
    {
        if (!ulong.TryParse(ctx.Request.Path.Value!.Split('/').Last(), out var uid) || game == null)
            return null;
        return new Dictionary<string, object?> { ["kicked"] = game.KickPlayer(uid) };
    }

    private static object? Give(HttpContext ctx, IAdminGame? game)
    {
        if (game == null)
            return null;
        var seg = ctx.Request.Path.Value!.Split('/').Skip(3).ToArray();
        if (seg.Length < 3 || !uint.TryParse(seg[0], out var genre) || !uint.TryParse(seg[1], out var detail) || !uint.TryParse(seg[2], out var particular))
            return null;
        var count = seg.Length > 3 && uint.TryParse(seg[3], out var c) ? c : 1;
        var level = uint.TryParse(ctx.Request.Query["level"].FirstOrDefault(), out var l) ? l : 1;
        var template = ((ulong)level << 48) | ((ulong)particular << 32) | ((ulong)detail << 16) | genre;
        var targetUid = ctx.Request.Query["uid"].FirstOrDefault();
        var given = targetUid != null && ulong.TryParse(targetUid, out var tu)
            ? game.GiveToOne(tu, template, count)
            : game.GiveToAll(template, count);
        return new Dictionary<string, object?> { ["template"] = template, ["count"] = count, ["given_to"] = given };
    }

    private static readonly string[] CnDomains =
    [
        "sh-jxsj.xgsdk.com", "xgdata.xgsdk.com", "js2sdk.xoyo.com", "sh-qrcode.xgsdk.com",
        "activity.xgsdk.com", "gpm-conf.testplus.cn", "cbjq-serverlist.xoyocdn.com",
    ];

    private static object Setup(HttpContext ctx, AppConfig config)
    {
        var hosts = new List<string>();
        var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "drivers", "etc", "hosts");
        if (File.Exists(hostsPath))
        {
            foreach (var line in File.ReadAllLines(hostsPath))
            {
                if (line.Contains("SNOWBREAKTC") || line.Contains("snowbreaktc"))
                    hosts.Add(line);
            }
        }
        var addresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(a => a.ToString())
            .ToList();
        return new Dictionary<string, object?>
        {
            ["region"] = ctx.Request.Query["region"].FirstOrDefault() ?? "cn",
            ["regions"] = new[]
            {
                new Dictionary<string, object?> { ["id"] = "cn", ["loader"] = "hosts", ["note"] = "CN: client starts normally, 7 SDK domains are pointed to this server via hosts" },
                new Dictionary<string, object?> { ["id"] = "global", ["loader"] = "proxy", ["note"] = "Global: redirect oversea domains to this server" },
            },
            ["public_address"] = config.Sdk.PublicHost,
            ["game_public_address"] = config.Game.PublicHost,
            ["ports"] = new Dictionary<string, object?>
            {
                ["http"] = config.Sdk.Port,
                ["game"] = config.Game.Port,
                ["admin"] = config.Admin.Port,
            },
            ["addresses"] = addresses,
            ["hosts"] = hosts,
            ["cn_domains"] = CnDomains,
            ["global_domains"] = new[]
            {
                "amazingseasuncdn.com", "amazingseasun.com", "seasungames.com", "snowbreak-game.com",
                "xgsdk.com", "xoyo.games", "qcloud.com", "tencentcs.com",
            },
        };
    }

    private static object SetupApply(HttpContext ctx) => new Dictionary<string, object?>
    {
        ["accepted"] = true,
        ["note"] = "hosts redirection must be applied manually with administrator rights; see README client guide",
    };

    private static object Replay(HttpContext ctx)
    {
        var lang = ctx.Request.Query["lang"].FirstOrDefault();
        return new Dictionary<string, object?>
        {
            ["lang"] = lang ?? "zh-CN",
            ["eras"] = Array.Empty<object>(),
            ["current"] = null,
        };
    }

    private static async Task Console_(HttpContext ctx, Func<string, GmResult>? gmRun)
    {
        Dictionary<string, JsonElement>? body = null;
        if (ctx.Request.HasJsonContentType())
        {
            try { body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ReadJson(ctx)); }
            catch { body = null; }
        }
        string? command = null;
        JsonElement c1 = default, c2 = default;
        if (body != null && (body.TryGetValue("Command", out c1) || body.TryGetValue("command", out c2)))
            command = c1.ValueKind == JsonValueKind.String ? c1.GetString()
                : c2.ValueKind == JsonValueKind.String ? c2.GetString()
                : null;
        command ??= ctx.Request.Query["cmd"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(command))
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                code = "invalid_arguments",
                message = "command is required",
            }));
            return;
        }
        if (gmRun == null)
        {
            ctx.Response.StatusCode = 503;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                code = "console_not_ready",
                message = "the command layer is not initialised yet",
            }));
            return;
        }
        var result = gmRun(command);
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            accepted = true,
            success = result.Success,
            messages = result.Messages,
        }));
    }

    private static async Task<bool> TryServeAssetAsync(HttpContext ctx, string path)
    {
        var (resource, contentType) = path switch
        {
            "/" or "/index.html" => ("SnowbreakTC.WebAPI.wwwroot.index.html", "text/html; charset=utf-8"),
            "/app.css" => ("SnowbreakTC.WebAPI.wwwroot.app.css", "text/css; charset=utf-8"),
            "/app.js" => ("SnowbreakTC.WebAPI.wwwroot.app.js", "application/javascript; charset=utf-8"),
            _ => (null, null),
        };
        if (resource == null || contentType == null)
            return false;
        using var stream = typeof(AdminHost).Assembly.GetManifestResourceStream(resource);
        if (stream == null)
            return false;
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = contentType;
        await stream.CopyToAsync(ctx.Response.Body);
        return true;
    }

    private static object Commands() => GmCatalog.Entries
        .Select(c => new
        {
            name = c.Name,
            usage = c.Usage,
            description = c.Description,
            aliases = c.Aliases,
            permissions = Array.Empty<string>(),
        })
        .ToList();
}
