using System.Net.Sockets;
using Serilog;
using SnowbreakTC.GameLogic;
using SnowbreakTC.Protocol.Wire;

namespace SnowbreakTC.Server.Game;

public sealed class GameSession
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _cts = new();

    public string Id { get; } = Guid.NewGuid().ToString("N")[..12];
    public FrameKind FrameMode { get; private set; } = FrameKind.Plain;
    public PlayerInstance? Player { get; set; }
    public bool Alive { get; private set; } = true;
    public GameServer Server { get; }

    public GameSession(GameServer server, Socket socket)
    {
        Server = server;
        _socket = socket;
        _stream = new NetworkStream(socket, ownsSocket: true);
    }

    public async Task RunAsync()
    {
        Log.Information("session {Id} connected from {Remote}", Id, _socket.RemoteEndPoint);
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                using var idle = new CancellationTokenSource(TimeSpan.FromSeconds(IdleTimeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, idle.Token);
                var packet = await Framing.ReadAsync(_stream, linked.Token);
                if (packet == null)
                    break;
                if (packet.Kind == FrameKind.Control)
                    continue;
                FrameMode = packet.Kind;
                await Dispatcher.DispatchAsync(this, packet.Command, packet.Body);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log.Debug(ex, "session {Id} read loop ended", Id);
        }
        finally
        {
            Close();
        }
    }

    public const int IdleTimeoutSeconds = 60;

    public Task SendAsync(ushort cmd, object? msg = null) =>
        SendRawAsync(cmd, msg == null ? [] : Codec.Encode(msg));

    public async Task SendRawAsync(ushort cmd, byte[] body)
    {
        if (!Alive)
            return;
        try
        {
            var frame = Framing.Encode(cmd, body, FrameMode);
            await _stream.WriteAsync(frame, _cts.Token);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "session {Id} send failed", Id);
            Close();
        }
    }

    public void Close()
    {
        if (!Alive)
            return;
        Alive = false;
        _cts.Cancel();
        Server.OnDisconnected(this);
        try { _socket.Close(); } catch { /* ignore */ }
    }
}
