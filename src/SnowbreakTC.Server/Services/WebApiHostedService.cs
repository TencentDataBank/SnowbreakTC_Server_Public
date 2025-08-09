using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnowbreakTC.Core.Services;

namespace SnowbreakTC.Server.Services;

/// <summary>
/// Web API 后台服务
/// </summary>
public class WebApiHostedService : BackgroundService
{
    private readonly ILogger<WebApiHostedService> _logger;
    private readonly IWebApiService _webApiService;

    public WebApiHostedService(
        ILogger<WebApiHostedService> logger,
        IWebApiService webApiService)
    {
        _logger = logger;
        _webApiService = webApiService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Web API 后台服务正在启动...");
            await _webApiService.StartAsync(stoppingToken);
            
            // 保持服务运行
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                
                // 可以在这里添加定期任务，如健康检查、性能监控等
                if (_webApiService.IsRunning)
                {
                    _logger.LogDebug("Web API 服务运行正常");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常关闭
            _logger.LogInformation("Web API 后台服务正在关闭...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web API 后台服务运行时出错");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止 Web API 后台服务...");
        
        try
        {
            await _webApiService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止 Web API 服务时出错");
        }
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Web API 后台服务已停止");
    }
}