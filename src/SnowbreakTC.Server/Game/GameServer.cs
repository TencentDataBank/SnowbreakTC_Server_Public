using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Serilog;
using SnowbreakTC.Core;
using SnowbreakTC.Database;
using SnowbreakTC.GameLogic;
using SnowbreakTC.GameLogic.Resources;

namespace SnowbreakTC.Server.Game;

public sealed class GameServer
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<ulong, GameSession> _active = new();
    private TcpListener? _listener;

    public AppConfig Config { get; }
    public SnowbreakDb Db { get; }
    public TokenService Tokens { get; }
    public ResourceManager Resources { get; }

    public GameServer(AppConfig config, SnowbreakDb db, TokenService tokens, ResourceManager resources)
    {
        Config = config;
        Db = db;
        Tokens = tokens;
        Resources = resources;
    }

    public void Start(CancellationToken ct)
    {
        _listener = new TcpListener(IPAddress.Parse(Config.Game.BindAddress), Config.Game.Port);
        _listener.Start();
        Log.Information("game server listening on {Bind}:{Port}", Config.Game.BindAddress, Config.Game.Port);
        _ = AcceptLoopAsync(ct);
        _ = AutoSaveLoopAsync(ct);
    }

    private async Task AutoSaveLoopAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(Config.Database.AutoSaveSeconds, 10, 600));
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                foreach (var session in _sessions.Values)
                {
                    if (session.Player is { } player)
                    {
                        try { player.Save(); }
                        catch (Exception ex) { Log.Error(ex, "autosave failed for uid {Uid}", player.Data.Pid); }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Stop()
    {
        foreach (var session in _sessions.Values)
            session.Close();
        _sessions.Clear();
        _active.Clear();
        _listener?.Stop();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "accept failed");
                continue;
            }
            if (_sessions.Count >= Config.Game.MaxConnections)
            {
                client.Close();
                continue;
            }
            var session = new GameSession(this, client.Client);
            _sessions[session.Id] = session;
            _ = session.RunAsync();
        }
    }

    public void OnDisconnected(GameSession session)
    {
        _sessions.TryRemove(session.Id, out _);
        if (session.Player != null && _active.TryGetValue(session.Player.Data.Pid, out var current) && current == session)
            _active.TryRemove(session.Player.Data.Pid, out _);
        try { session.Player?.Save(); } catch (Exception ex) { Log.Error(ex, "save player on disconnect failed"); }
    }

    public GameSession? GetActive(ulong pid) => _active.GetValueOrDefault(pid);

    public void BindActive(ulong pid, GameSession session) => _active[pid] = session;

    public IEnumerable<GameSession> ActiveSessions() =>
        _sessions.Values.Where(s => s.Player != null && s.Alive);

    public int KickPlayer(ulong pid)
    {
        if (GetActive(pid) is { } session)
        {
            session.Close();
            return 1;
        }
        return 0;
    }

    public int SaveAll()
    {
        var saved = 0;
        foreach (var session in ActiveSessions())
        {
            try
            {
                session.Player!.Save();
                saved++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "manual save failed for uid {Uid}", session.Player!.Data.Pid);
            }
        }
        return saved;
    }

    public int GiveToAll(ulong template, uint count)
    {
        var given = 0;
        foreach (var session in ActiveSessions())
        {
            session.Player!.Inventory.Add(template, count);
            session.Player.Save();
            given++;
        }
        return given;
    }
}
