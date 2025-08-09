using MongoDB.Bson.Serialization.Attributes;

namespace SnowbreakTC.Core.Models;

/// <summary>
/// 用户实体
/// </summary>
[BsonCollection("users")]
public class User : AuditableEntity
{
    /// <summary>
    /// 用户名（唯一）
    /// </summary>
    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址（唯一）
    /// </summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希
    /// </summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 密码盐值
    /// </summary>
    [BsonElement("passwordSalt")]
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// 用户状态
    /// </summary>
    [BsonElement("status")]
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// 用户角色
    /// </summary>
    [BsonElement("role")]
    public UserRole Role { get; set; } = UserRole.Player;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [BsonElement("lastLoginAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 最后登录IP
    /// </summary>
    [BsonElement("lastLoginIp")]
    [BsonIgnoreIfNull]
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 登录次数
    /// </summary>
    [BsonElement("loginCount")]
    public long LoginCount { get; set; } = 0;

    /// <summary>
    /// 邮箱验证状态
    /// </summary>
    [BsonElement("emailVerified")]
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// 邮箱验证时间
    /// </summary>
    [BsonElement("emailVerifiedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? EmailVerifiedAt { get; set; }

    /// <summary>
    /// 账号锁定到期时间
    /// </summary>
    [BsonElement("lockedUntil")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// 失败登录尝试次数
    /// </summary>
    [BsonElement("failedLoginAttempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// 用户配置信息
    /// </summary>
    [BsonElement("preferences")]
    [BsonIgnoreIfNull]
    public UserPreferences? Preferences { get; set; }

    /// <summary>
    /// 检查账号是否被锁定
    /// </summary>
    /// <returns></returns>
    public bool IsLocked()
    {
        return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// 检查用户是否激活
    /// </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return Status == UserStatus.Active && !IsLocked();
    }
}

/// <summary>
/// 用户状态枚举
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// 激活
    /// </summary>
    Active = 1,

    /// <summary>
    /// 未激活
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// 已禁用
    /// </summary>
    Disabled = 3,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 4
}

/// <summary>
/// 用户角色枚举
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 普通玩家
    /// </summary>
    Player = 1,

    /// <summary>
    /// 版主
    /// </summary>
    Moderator = 2,

    /// <summary>
    /// 管理员
    /// </summary>
    Administrator = 3,

    /// <summary>
    /// 超级管理员
    /// </summary>
    SuperAdmin = 4
}

/// <summary>
/// 用户偏好设置
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// 语言设置
    /// </summary>
    [BsonElement("language")]
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 时区设置
    /// </summary>
    [BsonElement("timezone")]
    public string Timezone { get; set; } = "Asia/Shanghai";

    /// <summary>
    /// 是否接收邮件通知
    /// </summary>
    [BsonElement("emailNotifications")]
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// 是否接收推送通知
    /// </summary>
    [BsonElement("pushNotifications")]
    public bool PushNotifications { get; set; } = true;

    /// <summary>
    /// 隐私设置
    /// </summary>
    [BsonElement("privacySettings")]
    public Dictionary<string, object> PrivacySettings { get; set; } = new();
}

/// <summary>
/// MongoDB 集合名称属性
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BsonCollectionAttribute : Attribute
{
    /// <summary>
    /// 集合名称
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="collectionName">集合名称</param>
    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}