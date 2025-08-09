namespace SnowbreakTC.Core.Configuration;

/// <summary>
/// 游戏配置
/// </summary>
public class GameConfiguration
{
    /// <summary>
    /// 最大等级
    /// </summary>
    public int MaxLevel { get; set; } = 80;

    /// <summary>
    /// 初始货币
    /// </summary>
    public long StartingCurrency { get; set; } = 10000;

    /// <summary>
    /// 默认角色ID列表
    /// </summary>
    public int[] DefaultCharacters { get; set; } = { 1001, 1002 };

    /// <summary>
    /// 最大仓库槽位
    /// </summary>
    public int MaxInventorySlots { get; set; } = 500;

    /// <summary>
    /// 经验配置
    /// </summary>
    public ExperienceConfiguration Experience { get; set; } = new();

    /// <summary>
    /// 抽卡配置
    /// </summary>
    public GachaConfiguration Gacha { get; set; } = new();
}

/// <summary>
/// 经验配置
/// </summary>
public class ExperienceConfiguration
{
    /// <summary>
    /// 基础经验值
    /// </summary>
    public int BaseExp { get; set; } = 100;

    /// <summary>
    /// 经验倍率
    /// </summary>
    public double ExpMultiplier { get; set; } = 1.2;

    /// <summary>
    /// 计算升级所需经验
    /// </summary>
    /// <param name="level">当前等级</param>
    /// <returns>升级所需经验</returns>
    public long CalculateExpForLevel(int level)
    {
        return (long)(BaseExp * Math.Pow(ExpMultiplier, level - 1));
    }
}

/// <summary>
/// 抽卡配置
/// </summary>
public class GachaConfiguration
{
    /// <summary>
    /// SSR 概率
    /// </summary>
    public double RareRate { get; set; } = 0.006;

    /// <summary>
    /// SR 概率
    /// </summary>
    public double SRRate { get; set; } = 0.051;

    /// <summary>
    /// R 概率
    /// </summary>
    public double RRate { get; set; } = 0.943;

    /// <summary>
    /// 保底次数
    /// </summary>
    public int PityCount { get; set; } = 90;

    /// <summary>
    /// 软保底开始次数
    /// </summary>
    public int SoftPityStart { get; set; } = 75;
}

/// <summary>
/// 安全配置
/// </summary>
public class SecurityConfiguration
{
    /// <summary>
    /// JWT 配置
    /// </summary>
    public JwtConfiguration JWT { get; set; } = new();

    /// <summary>
    /// 密码哈希配置
    /// </summary>
    public PasswordHashConfiguration PasswordHash { get; set; } = new();
}

/// <summary>
/// JWT 配置
/// </summary>
public class JwtConfiguration
{
    /// <summary>
    /// 密钥
    /// </summary>
    public string SecretKey { get; set; } = "your-super-secret-key-change-this-in-production";

    /// <summary>
    /// 发行者
    /// </summary>
    public string Issuer { get; set; } = "SnowbreakTC";

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; } = "SnowbreakTC-Client";

    /// <summary>
    /// 过期时间（分钟）
    /// </summary>
    public int ExpiryMinutes { get; set; } = 1440;
}

/// <summary>
/// 密码哈希配置
/// </summary>
public class PasswordHashConfiguration
{
    /// <summary>
    /// BCrypt 工作因子
    /// </summary>
    public int WorkFactor { get; set; } = 12;
}