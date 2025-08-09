namespace SnowbreakTC.Core.Configuration;

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// MongoDB 配置
    /// </summary>
    public MongoDbConfiguration MongoDB { get; set; } = new();

    /// <summary>
    /// Redis 配置
    /// </summary>
    public RedisConfiguration Redis { get; set; } = new();
}

/// <summary>
/// MongoDB 配置
/// </summary>
public class MongoDbConfiguration
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DatabaseName { get; set; } = "SnowbreakTC";

    /// <summary>
    /// 集合名称配置
    /// </summary>
    public CollectionConfiguration Collections { get; set; } = new();
}

/// <summary>
/// 集合名称配置
/// </summary>
public class CollectionConfiguration
{
    /// <summary>
    /// 用户集合
    /// </summary>
    public string Users { get; set; } = "users";

    /// <summary>
    /// 玩家集合
    /// </summary>
    public string Players { get; set; } = "players";

    /// <summary>
    /// 角色集合
    /// </summary>
    public string Characters { get; set; } = "characters";

    /// <summary>
    /// 物品集合
    /// </summary>
    public string Items { get; set; } = "items";

    /// <summary>
    /// 仓库集合
    /// </summary>
    public string Inventories { get; set; } = "inventories";
}

/// <summary>
/// Redis 配置
/// </summary>
public class RedisConfiguration
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 数据库索引
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// 键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "snowbreak:";

    /// <summary>
    /// 默认过期时间（秒）
    /// </summary>
    public int DefaultExpiry { get; set; } = 3600;
}