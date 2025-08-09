using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnowbreakTC.Core.Configuration;
using SnowbreakTC.Core.Services;
using System.Net;
using System.Net.Sockets;

namespace SnowbreakTC.Server.Services;

/// <summary>
/// 游戏服务器服务实现
/// </summary>
public class GameServerService : IGameServerService
{
    private readonly ILogger<GameServerService> _logger;
    private readonly ServerConfiguration _serverConfig;
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly List<TcpClient> _connectedClients = new();
    private readonly object _clientsLock = new();

    public bool IsRunning { get; private set; }

    public GameServerService(
        ILogger<GameServerService> logger,
        IOptions<ServerConfiguration> serverConfig)
    {
        _logger = logger;
        _serverConfig = serverConfig.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("游戏服务器已经在运行中");
            return;
        }

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token).Token;

            // 创建 TCP 监听器
            _tcpListener = new TcpListener(IPAddress.Any, _serverConfig.GameServer.Port);
            _tcpListener.Start();

            IsRunning = true;
            _logger.LogInformation("游戏服务器已启动，监听端口: {Port}", _serverConfig.GameServer.Port);

            // 开始接受连接
            _ = Task.Run(() => AcceptClientsAsync(combinedToken), combinedToken);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动游戏服务器失败");
            IsRunning = false;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _logger.LogInformation("正在停止游戏服务器...");

            // 取消所有操作
            _cancellationTokenSource?.Cancel();

            // 关闭所有客户端连接
            lock (_clientsLock)
            {
                foreach (var client in _connectedClients.ToList())
                {
                    try
                    {
                        client.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "关闭客户端连接时出错");
                    }
                }
                _connectedClients.Clear();
            }

            // 停止监听器
            _tcpListener?.Stop();
            _tcpListener = null;

            IsRunning = false;
            _logger.LogInformation("游戏服务器已停止");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止游戏服务器时出错");
            throw;
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public int GetConnectionCount()
    {
        lock (_clientsLock)
        {
            return _connectedClients.Count;
        }
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && IsRunning)
        {
            try
            {
                if (_tcpListener == null) break;

                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                
                // 检查连接数限制
                if (GetConnectionCount() >= _serverConfig.GameServer.MaxConnections)
                {
                    _logger.LogWarning("达到最大连接数限制，拒绝新连接");
                    tcpClient.Close();
                    continue;
                }

                lock (_clientsLock)
                {
                    _connectedClients.Add(tcpClient);
                }

                _logger.LogInformation("新客户端连接: {RemoteEndPoint}", tcpClient.Client.RemoteEndPoint);

                // 为每个客户端启动处理任务
                _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // 服务器正在关闭
                break;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "接受客户端连接时出错");
                }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        // 客户端断开连接
                        break;
                    }

                    // TODO: 处理接收到的数据
                    _logger.LogDebug("从 {RemoteEndPoint} 接收到 {BytesRead} 字节数据", remoteEndPoint, bytesRead);
                    
                    // 这里应该解析协议并处理消息
                    // await ProcessMessageAsync(buffer, bytesRead, stream, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "处理客户端 {RemoteEndPoint} 时出错", remoteEndPoint);
            }
        }
        finally
        {
            // 从连接列表中移除客户端
            lock (_clientsLock)
            {
                _connectedClients.Remove(client);
            }
            
            _logger.LogInformation("客户端 {RemoteEndPoint} 已断开连接", remoteEndPoint);
        }
    }
}