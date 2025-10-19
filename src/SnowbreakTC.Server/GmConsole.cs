using Serilog;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Server.Handlers.CallGs;
using SnowbreakTC.WebAPI;

namespace SnowbreakTC.Server;

public static class GmExecutor
{
    public static GmResult Run(GameServer server, string line)

    {
        var messages = new List<string>();
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return new GmResult(false, ["empty command"]);
        try
        {
            switch (parts[0])
            {
                case "grant" when parts.Length >= 6 && parts[1] == "item"
                    && uint.TryParse(parts[2], out var g) && uint.TryParse(parts[3], out var d) && uint.TryParse(parts[4], out var p2):
                {
                    var count = parts.Length > 5 && uint.TryParse(parts[5], out var c) ? c : 1;
                    var template = ItemGenres.Template(g, d, p2);
                    var given = server.GiveToAll(template, count);
                    messages.Add($"gave {template} x{count} to {given} players");
                    break;
                }
                case "grant" when parts.Length == 4 && parts[1] == "money"
                    && uint.TryParse(parts[2], out var mt) && uint.TryParse(parts[3], out var mv):
                {
                    foreach (var session in server.ActiveSessions())
                    {
                        session.Player!.SetCurrency(mt, mv);
                        session.Player.Save();
                    }
                    messages.Add($"money {mt} -> {mv}");
                    break;
                }
                case "grant" when parts.Length == 3 && parts[1] == "level"
                    && uint.TryParse(parts[2], out var lvl):
                {
                    foreach (var session in server.ActiveSessions())
                    {
                        session.Player!.Data.Level = Math.Clamp(lvl, 1, 80);
                        session.Player.Save();
                    }
                    messages.Add($"level -> {lvl}");
                    break;
                }
                case "grant" when parts.Length == 4 && parts[1] == "card"
                    && uint.TryParse(parts[2], out var gd) && uint.TryParse(parts[3], out var gp):
                {
                    var added = 0;
                    foreach (var session in server.ActiveSessions())
                    {
                        session.Player!.Characters.AddCharacter(gd, gp);
                        session.Player.Save();
                        added++;
                    }
                    messages.Add($"card detail={gd} particular={gp} added to {added} players");
                    break;
                }
                case "grant" when parts.Length == 4 && parts[1] == "weapon"
                    && uint.TryParse(parts[2], out var wd) && uint.TryParse(parts[3], out var wp):
                {
                    var added = 0;
                    foreach (var session in server.ActiveSessions())
                    {
                        session.Player!.Characters.AddWeapon(wd, wp);
                        session.Player.Save();
                        added++;
                    }
                    messages.Add($"weapon detail={wd} particular={wp} added to {added} players");
                    break;
                }
                case "grant" when parts.Length == 2 && parts[1] == "allcards":
                {
                    var count = 0;
                    foreach (var session in server.ActiveSessions())
                    {
                        for (var d = 1u; d <= 20u; d++)
                            session.Player!.Characters.AddCharacter(d, d % 5 + 1);
                        session.Player!.Save();
                        count++;
                    }
                    messages.Add($"all cards granted to {count} players");
                    break;
                }
                case "grant" when parts.Length == 2 && parts[1] == "allweapons":
                {
                    var count = 0;
                    foreach (var session in server.ActiveSessions())
                    {
                        for (var d = 1u; d <= 10u; d++)
                            session.Player!.Characters.AddWeapon(d, 1);
                        session.Player!.Save();
                        count++;
                    }
                    messages.Add($"all weapons granted to {count} players");
                    break;
                }
                case "grant" when parts.Length == 3 && parts[1] == "template"
                    && ulong.TryParse(parts[2], out var tid):
                {
                    var given = server.GiveToAll(tid, 1);
                    messages.Add($"template {tid} -> {given} players");
                    break;
                }
                case "player" when parts.Length == 2 && parts[1] == "list":
                    messages.Add(string.Join(", ", server.ActiveSessions()
                        .Select(s2 => $"{s2.Player!.Data.Pid}:{s2.Player.Data.Name}")));
                    break;
                case "player" when parts.Length == 3 && parts[1] == "kick"
                    && ulong.TryParse(parts[2], out var k):
                    messages.Add($"kicked {server.KickPlayer(k)}");
                    break;
                case "player" when parts.Length == 3 && parts[1] == "create":
                {
                    var email = parts[2].Contains('@') ? parts[2] : $"{parts[2]}@local";
                    var account = server.Db.GetAccountByEmail(email) ?? server.Db.CreateAccount(email, email.Split('@')[0]);
                    messages.Add($"account uid={account.Uid} email={account.Email}");
                    break;
                }
                case "data" when parts.Length == 5 && parts[1] == "attr" && parts[2] == "get"
                    && uint.TryParse(parts[3], out var packed) :
                {
                    var gid = packed / 10000;
                    var sid = packed % 10000;
                    foreach (var session in server.ActiveSessions())
                        messages.Add($"{session.Player!.Data.Name}: attr({gid},{sid}) = {session.Player.GetAttr(gid, sid)}");
                    break;
                }
                case "data" when parts.Length == 6 && parts[1] == "attr" && parts[2] == "set"
                    && uint.TryParse(parts[3], out var packed2) && uint.TryParse(parts[4], out var sv):
                {
                    var gid2 = packed2 / 10000;
                    var sid2 = packed2 % 10000;
                    foreach (var session in server.ActiveSessions())
                    {
                        session.Player!.SetAttr(gid2, sid2, sv);
                        session.Player.Save();
                    }
                    messages.Add($"attr({gid2},{sid2}) -> {sv}");
                    break;
                }
                case "data" when parts.Length == 3 && parts[1] == "progress":
                {
                    var on = parts[2] is "1" or "on" or "true";
                    foreach (var session in server.ActiveSessions())
                        session.Player!.Mark("quest_complete_all", on ? 1 : 0);
                    messages.Add($"progress complete_all -> {on}");
                    break;
                }
                case "redirect" when parts.Length == 2 && parts[1] == "status":
                {
                    var state = Tools.HostsSetup.Read();
                    messages.Add($"hosts readable={state.Readable} managed={state.Managed} entries={state.ManagedLines.Count}");
                    break;
                }
                case "redirect" when parts.Length >= 2 && (parts[1] == "on" || parts[1] == "off"):
                {
                    var ip = parts.Length > 2 ? parts[2] : "127.0.0.1";
                    messages.Add(Tools.HostsSetup.Write(parts[1] == "on", ip));
                    break;
                }
                case "mail" when parts.Length >= 2:
                {
                    var title = string.Join(' ', parts[1..]);
                    var sent = 0;
                    foreach (var session in server.ActiveSessions())
                    {
                        var player = session.Player!;
                        player.Mail(server.Db).Send(title, title, "GM");
                        player.Save();
                        sent++;
                    }
                    messages.Add($"mail '{title}' sent to {sent} players");
                    break;
                }
                case "server" when parts.Length == 2 && parts[1] == "save":
                    messages.Add($"saved {server.SaveAll()} players");
                    break;
                case "server" when parts.Length == 2 && parts[1] == "stats":
                    messages.Add($"registered callgs apis: {Handlers.CallGs.Router.RegisteredCount}; online: {server.ActiveSessions().Count()}");
                    break;
                case "server" when parts.Length == 2 && parts[1] == "shutdown":
                    messages.Add("shutdown requested");
                    Log.Information("gm shutdown requested");
                    Environment.Exit(0);
                    break;
                case "handbook" when parts.Length == 1:
                {
                    var docs = Path.Combine(Directory.GetCurrentDirectory(), "docs");
                    Directory.CreateDirectory(docs);
                    var nl = Environment.NewLine;
                    var md = "# GM Handbook" + nl + nl + "| command | usage | description |" + nl + "|---|---|---|" + nl;
                    foreach (var (name, usage, description, _) in WebAPI.GmCatalog.Entries)
                        md += $"| {name} | {usage} | {description} |" + nl;
                    File.WriteAllText(Path.Combine(docs, "handbook.md"), md);
                    messages.Add("handbook written to docs/handbook.md");
                    break;
                }
                case "help" when parts.Length == 1:
                    messages.Add("commands: " + string.Join(", ", WebAPI.GmCatalog.Entries.Select(c => c.Name)));
                    break;
                default:
                    return new GmResult(false, [$"unknown command: {parts[0]}. try help"]);
            }
            return new GmResult(true, messages);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "gm command failed: {Line}", line);
            return new GmResult(false, [ex.Message]);
        }
    }
}

public static class GmConsole
{
    public static async Task RunAsync(GameServer server, CancellationToken ct)
    {
        Log.Information("gm console ready — groups: grant | player | data | redirect | mail | server | handbook | help");
        while (!ct.IsCancellationRequested)
        {
            string line;
            try
            {
                line = await Console.In.ReadLineAsync(ct) ?? "";
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (line.Length == 0)
                continue;
            var result = GmExecutor.Run(server, line);
            foreach (var msg in result.Messages)
                Log.Information("gm: {Msg}", msg);
        }
    }
}