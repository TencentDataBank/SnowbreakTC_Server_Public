using Microsoft.Extensions.Logging;

namespace SnowbreakTC.Core.Services;

/// <summary>
/// 游戏服务器服务接口
/// </summary>
public interface IGameServerService
{
    /// <summary>
    /// 启动游戏服务器
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止游戏服务器
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前连接数
    /// </summary>
    /// <returns>连接数</returns>
    int GetConnectionCount();

    /// <summary>
    /// 服务器是否正在运行
    /// </summary>
    bool IsRunning { get; }
}

/// <summary>
/// Web API 服务接口
/// </summary>
public interface IWebApiService
{
    /// <summary>
    /// 启动 Web API 服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止 Web API 服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 服务是否正在运行
    /// </summary>
    bool IsRunning { get; }
}