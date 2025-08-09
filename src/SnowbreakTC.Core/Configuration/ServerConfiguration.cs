namespace SnowbreakTC.Core.Configuration;

/// <summary>
/// 服务器配置
/// </summary>
public class ServerConfiguration
{
    /// <summary>
    /// 服务器名称
    /// </summary>
    public string Name { get; set; } = "SnowbreakTC Server";

    /// <summary>
    /// 服务器版本
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Web API 配置
    /// </summary>
    public WebApiConfiguration WebAPI { get; set; } = new();

    /// <summary>
    /// 游戏服务器配置
    /// </summary>
    public GameServerConfiguration GameServer { get; set; } = new();
}

/// <summary>
/// Web API 配置
/// </summary>
public class WebApiConfiguration
{
    /// <summary>
    /// 监听端口
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// 是否使用 HTTPS
    /// </summary>
    public bool UseHttps { get; set; } = false;

    /// <summary>
    /// 监听地址
    /// </summary>
    public string Urls { get; set; } = "http://localhost:8080";
}

/// <summary>
/// 游戏服务器配置
/// </summary>
public class GameServerConfiguration
{
    /// <summary>
    /// 游戏服务器端口
    /// </summary>
    public int Port { get; set; } = 22102;

    /// <summary>
    /// 最大连接数
    /// </summary>
    public int MaxConnections { get; set; } = 1000;

    /// <summary>
    /// 心跳间隔（毫秒）
    /// </summary>
    public int HeartbeatInterval { get; set; } = 30000;

    /// <summary>
    /// 会话超时时间（毫秒）
    /// </summary>
    public int SessionTimeout { get; set; } = 300000;
}