using System.Text;
using Serilog;
using SnowbreakTC.Core;
using SnowbreakTC.Database;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Server.Handlers.Friend;

namespace SnowbreakTC.Server.Handlers;

public static class AccountResolver
{
    public static AccountRecord? Resolve(GameServer server, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var account = server.Tokens.ResolveCombo(token) ?? server.Tokens.ResolveDispatch(token);
        if (account != null)
            return account;

        var embedded = ExtractEmbedded(token);
        if (embedded == null)
            return null;

        account = server.Tokens.ResolveCombo(embedded.Value.Token) ?? server.Tokens.ResolveDispatch(embedded.Value.Token);
        if (account != null)
            return account;

        if (embedded.Value.Uid > 0)
            account = server.Db.GetAccount(embedded.Value.Uid);
        return account;
    }

    private static (string Token, int Uid)? ExtractEmbedded(string token)
    {
        try
        {
            var normalized = Uri.UnescapeDataString(token).Trim();
            var padding = normalized.Length % 4;
            if (padding > 0)
                normalized = normalized.PadRight(normalized.Length + 4 - padding, '=');
            using var doc = System.Text.Json.JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(normalized)));
            var auth = doc.RootElement.TryGetProperty("authToken", out var t) ? t.GetString() : null;
            var uid = doc.RootElement.TryGetProperty("uid", out var u)
                ? int.TryParse(u.ValueKind == System.Text.Json.JsonValueKind.Number ? u.GetRawText() : u.GetString(), out var v) ? v : 0
                : 0;
            return auth == null && uid == 0 ? null : (auth ?? "", uid);
        }
        catch
        {
            return null;
        }
    }
}

[Command(CmdIds.ReqLogin)]
public sealed class LoginHandler : IPacketHandler
{
    public const uint CurrencySeedAmount = 1000000;

    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var req = Codec.Decode<ReqLogin>(body);
        var server = session.Server;
        var account = AccountResolver.Resolve(server, req.Token);
        if (account == null)
        {
            Log.Warning("login rejected: token prefix {Prefix}", req.Token.Length > 24 ? req.Token[..24] : req.Token);
            await session.SendAsync(CmdIds.NtfLogout);
            session.Close();
            return;
        }

        if (server.GetActive((ulong)account.Uid) is { Alive: true } previous && previous != session)
        {
            Log.Information("kicking duplicate session for uid {Uid}", account.Uid);
            previous.Close();
        }

        var player = PlayerInstance.LoadOrCreate(account.Uid, server.Db, account.Username);
        var needRename = string.IsNullOrEmpty(player.Data.Name);
        if (needRename)
            player.Data.Name = $"Snow{account.Uid % 10000:D4}";

        if (player.SeedCurrencies(CurrencySeedAmount))
            Log.Information("seeded currency attrs for uid {Uid}", account.Uid);
        if (!player.Characters.Characters.Any())
        {
            player.Characters.AddCharacter(1, 1);
            player.Characters.AddCharacter(1, 2);
            Log.Information("granted starter loadout for uid {Uid}", account.Uid);
            player.Mail(server.Db).Send("Welcome", "Welcome to SnowbreakTC.", "SnowbreakTC");
        }
        var riki = player.Riki(server.Resources);
        riki.EnsureOwnedItemsUnlocked();
        if (riki.UnlockedCount > 0)
            Log.Information("riki unlocked {Count} entries for uid {Uid}", riki.UnlockedCount, account.Uid);
        player.RegenVigor(server.Config.Game.VigorLimit, server.Config.Game.VigorRegenMinutes);
        player.Meta.LastLoginAt = (long)PlayerInstance.Now();
        player.Meta.LoginCount++;
        player.Save();

        session.Player = player;
        server.BindActive((ulong)account.Uid, session);

        var rsp = new RspLogin
        {
            SessionId = session.Id,
            Data = player.Data,
            NeedRename = needRename,
            AreaId = server.Config.Game.AreaId,
            TimeZone = server.Config.Game.TimeZone,
            Timestamp = (uint)PlayerInstance.Now(),
            WorldChannel = server.Config.Game.WorldChannel,
            Seed = Cluster.SeedValue,
        };
        await session.SendAsync(CmdIds.RspLogin, rsp);
        await session.SendAsync(CmdIds.NtfScript, new NtfCallScript());
        await FriendSupport.PushFriendListAsync(session);
        Log.Information("uid {Uid} ({Name}) entered game (session {Session})", account.Uid, player.Data.Name, session.Id);
    }
}

[Command(CmdIds.ReqReconnect)]
public sealed class ReconnectHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var req = Codec.Decode<ReqReconnect>(body);
        var server = session.Server;
        var player = server.Db.GetAccount((int)req.Pid) == null
            ? null
            : PlayerInstance.LoadOrCreate((int)req.Pid, server.Db, "");
        if (player == null)
        {
            await session.SendAsync(CmdIds.NtfLogout);
            session.Close();
            return;
        }
        session.Player = player;
        server.BindActive(req.Pid, session);
        await session.SendAsync(CmdIds.RspReconnect, new RspReconnect
        {
            SessionId = session.Id,
            Data = player.Data,
            TimeZone = server.Config.Game.TimeZone,
            Timestamp = (uint)PlayerInstance.Now(),
            WorldChannel = server.Config.Game.WorldChannel,
        });
    }
}

[Command(CmdIds.ReqRename)]
public sealed class RenameHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var req = Codec.Decode<ReqRename>(body);
        var player = session.Player;
        if (player == null || string.IsNullOrWhiteSpace(req.Data))
        {
            await session.SendAsync(CmdIds.RspRename);
            return;
        }
        player.Data.Name = req.Data.Trim();
        player.Save();
        await session.SendAsync(CmdIds.RspRename);
        await session.SendAsync(CmdIds.NtfSetStrAttr, new NtfSetStrAttr { Gid = 0, Sid = 0, Val = player.Data.Name });
    }
}

[Command(CmdIds.ReqAccountInfo)]
public sealed class AccountInfoHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var player = session.Player;
        if (player == null)
            return;
        await session.SendAsync(CmdIds.RspAccountInfo, new AccountInfo
        {
            Account = player.Data.Account,
            Pid = player.Data.Pid,
        });
    }
}

[Command(CmdIds.ReqCallGS)]
public sealed class CallGsHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var req = Codec.Decode<ReqCallGs>(body);
        Serilog.Log.Debug("callgs {Api} param={Param}", req.Api, req.Param);
        await CallGs.Router.DispatchAsync(session, req.Api, req.Param);
    }
}

[Command(CmdIds.NtfSetAttr)]
public sealed class SyncAttrHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var attr = Codec.Decode<NtfSetAttr>(body);
        session.Player?.SetAttr(attr.Gid, attr.Sid, attr.Val);
        session.Player?.Save();
    }
}

[Command(CmdIds.NtfSetStrAttr)]
public sealed class SyncStrAttrHandler : IPacketHandler
{
    public async Task HandleAsync(GameSession session, byte[] body)
    {
        var attr = Codec.Decode<NtfSetStrAttr>(body);
        session.Player?.SetStrAttr(attr.Gid, attr.Sid, attr.Val);
        session.Player?.Save();
    }
}
