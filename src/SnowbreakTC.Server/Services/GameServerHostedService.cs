using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnowbreakTC.Core.Services;

namespace SnowbreakTC.Server.Services;

/// <summary>
/// 游戏服务器后台服务
/// </summary>
public class GameServerHostedService : BackgroundService
{
    private readonly ILogger<GameServerHostedService> _logger;
    private readonly IGameServerService _gameServerService;

    public GameServerHostedService(
        ILogger<GameServerHostedService> logger,
        IGameServerService gameServerService)
    {
        _logger = logger;
        _gameServerService = gameServerService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("游戏服务器后台服务正在启动...");
            await _gameServerService.StartAsync(stoppingToken);
            
            // 保持服务运行
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                
                // 可以在这里添加定期任务，如状态检查、统计信息记录等
                if (_gameServerService.IsRunning)
                {
                    var connectionCount = _gameServerService.GetConnectionCount();
                    _logger.LogDebug("当前连接数: {ConnectionCount}", connectionCount);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常关闭
            _logger.LogInformation("游戏服务器后台服务正在关闭...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "游戏服务器后台服务运行时出错");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止游戏服务器后台服务...");
        
        try
        {
            await _gameServerService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止游戏服务器时出错");
        }
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("游戏服务器后台服务已停止");
    }
}