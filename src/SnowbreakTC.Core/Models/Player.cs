using MongoDB.Bson.Serialization.Attributes;

namespace SnowbreakTC.Core.Models;

/// <summary>
/// 玩家实体
/// </summary>
[BsonCollection("players")]
public class Player : AuditableEntity
{
    /// <summary>
    /// 关联的用户ID
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 玩家昵称
    /// </summary>
    [BsonElement("nickname")]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 玩家等级
    /// </summary>
    [BsonElement("level")]
    public int Level { get; set; } = 1;

    /// <summary>
    /// 当前经验值
    /// </summary>
    [BsonElement("experience")]
    public long Experience { get; set; } = 0;

    /// <summary>
    /// 玩家状态
    /// </summary>
    [BsonElement("status")]
    public PlayerStatus Status { get; set; } = PlayerStatus.Active;

    /// <summary>
    /// 货币信息
    /// </summary>
    [BsonElement("currencies")]
    public PlayerCurrencies Currencies { get; set; } = new();

    /// <summary>
    /// 玩家统计信息
    /// </summary>
    [BsonElement("statistics")]
    public PlayerStatistics Statistics { get; set; } = new();

    /// <summary>
    /// 玩家设置
    /// </summary>
    [BsonElement("settings")]
    public PlayerSettings Settings { get; set; } = new();

    /// <summary>
    /// 最后活跃时间
    /// </summary>
    [BsonElement("lastActiveAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后登录IP
    /// </summary>
    [BsonElement("lastLoginIp")]
    [BsonIgnoreIfNull]
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 玩家头像ID
    /// </summary>
    [BsonElement("avatarId")]
    public int AvatarId { get; set; } = 1001;

    /// <summary>
    /// 玩家签名
    /// </summary>
    [BsonElement("signature")]
    [BsonIgnoreIfNull]
    public string? Signature { get; set; }

    /// <summary>
    /// VIP等级
    /// </summary>
    [BsonElement("vipLevel")]
    public int VipLevel { get; set; } = 0;

    /// <summary>
    /// VIP经验
    /// </summary>
    [BsonElement("vipExp")]
    public long VipExp { get; set; } = 0;

    /// <summary>
    /// 检查玩家是否在线
    /// </summary>
    /// <returns></returns>
    public bool IsOnline()
    {
        return Status == PlayerStatus.Online;
    }

    /// <summary>
    /// 检查玩家是否可用
    /// </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return Status != PlayerStatus.Banned && Status != PlayerStatus.Deleted;
    }

    /// <summary>
    /// 计算升级所需经验
    /// </summary>
    /// <param name="targetLevel">目标等级</param>
    /// <returns>所需经验</returns>
    public long GetExpRequiredForLevel(int targetLevel)
    {
        if (targetLevel <= Level) return 0;
        
        long totalExp = 0;
        for (int i = Level; i < targetLevel; i++)
        {
            totalExp += (long)(100 * Math.Pow(1.2, i - 1));
        }
        return totalExp - Experience;
    }
}

/// <summary>
/// 玩家状态枚举
/// </summary>
public enum PlayerStatus
{
    /// <summary>
    /// 激活状态
    /// </summary>
    Active = 1,

    /// <summary>
    /// 在线状态
    /// </summary>
    Online = 2,

    /// <summary>
    /// 离线状态
    /// </summary>
    Offline = 3,

    /// <summary>
    /// 被封禁
    /// </summary>
    Banned = 4,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 5
}

/// <summary>
/// 玩家货币信息
/// </summary>
public class PlayerCurrencies
{
    /// <summary>
    /// 金币
    /// </summary>
    [BsonElement("gold")]
    public long Gold { get; set; } = 10000;

    /// <summary>
    /// 钻石
    /// </summary>
    [BsonElement("diamond")]
    public long Diamond { get; set; } = 0;

    /// <summary>
    /// 体力
    /// </summary>
    [BsonElement("stamina")]
    public int Stamina { get; set; } = 100;

    /// <summary>
    /// 最大体力
    /// </summary>
    [BsonElement("maxStamina")]
    public int MaxStamina { get; set; } = 100;

    /// <summary>
    /// 体力恢复时间
    /// </summary>
    [BsonElement("staminaRecoverAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StaminaRecoverAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 友情点
    /// </summary>
    [BsonElement("friendshipPoints")]
    public long FriendshipPoints { get; set; } = 0;

    /// <summary>
    /// 荣誉点
    /// </summary>
    [BsonElement("honorPoints")]
    public long HonorPoints { get; set; } = 0;
}

/// <summary>
/// 玩家统计信息
/// </summary>
public class PlayerStatistics
{
    /// <summary>
    /// 总游戏时间（分钟）
    /// </summary>
    [BsonElement("totalPlayTime")]
    public long TotalPlayTime { get; set; } = 0;

    /// <summary>
    /// 登录天数
    /// </summary>
    [BsonElement("loginDays")]
    public int LoginDays { get; set; } = 0;

    /// <summary>
    /// 连续登录天数
    /// </summary>
    [BsonElement("consecutiveLoginDays")]
    public int ConsecutiveLoginDays { get; set; } = 0;

    /// <summary>
    /// 最后签到时间
    /// </summary>
    [BsonElement("lastCheckInAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? LastCheckInAt { get; set; }

    /// <summary>
    /// 战斗次数
    /// </summary>
    [BsonElement("battleCount")]
    public long BattleCount { get; set; } = 0;

    /// <summary>
    /// 胜利次数
    /// </summary>
    [BsonElement("victoryCount")]
    public long VictoryCount { get; set; } = 0;

    /// <summary>
    /// 失败次数
    /// </summary>
    [BsonElement("defeatCount")]
    public long DefeatCount { get; set; } = 0;

    /// <summary>
    /// 抽卡次数
    /// </summary>
    [BsonElement("gachaCount")]
    public long GachaCount { get; set; } = 0;

    /// <summary>
    /// 获得SSR次数
    /// </summary>
    [BsonElement("ssrCount")]
    public long SsrCount { get; set; } = 0;

    /// <summary>
    /// 计算胜率
    /// </summary>
    /// <returns>胜率百分比</returns>
    public double GetWinRate()
    {
        if (BattleCount == 0) return 0;
        return (double)VictoryCount / BattleCount * 100;
    }

    /// <summary>
    /// 计算SSR概率
    /// </summary>
    /// <returns>SSR概率百分比</returns>
    public double GetSsrRate()
    {
        if (GachaCount == 0) return 0;
        return (double)SsrCount / GachaCount * 100;
    }
}

/// <summary>
/// 玩家设置
/// </summary>
public class PlayerSettings
{
    /// <summary>
    /// 音效音量 (0-100)
    /// </summary>
    [BsonElement("soundVolume")]
    public int SoundVolume { get; set; } = 80;

    /// <summary>
    /// 音乐音量 (0-100)
    /// </summary>
    [BsonElement("musicVolume")]
    public int MusicVolume { get; set; } = 60;

    /// <summary>
    /// 语音音量 (0-100)
    /// </summary>
    [BsonElement("voiceVolume")]
    public int VoiceVolume { get; set; } = 80;

    /// <summary>
    /// 画质设置
    /// </summary>
    [BsonElement("graphicsQuality")]
    public GraphicsQuality GraphicsQuality { get; set; } = GraphicsQuality.Medium;

    /// <summary>
    /// 帧率设置
    /// </summary>
    [BsonElement("frameRate")]
    public FrameRate FrameRate { get; set; } = FrameRate.FPS60;

    /// <summary>
    /// 是否允许好友申请
    /// </summary>
    [BsonElement("allowFriendRequests")]
    public bool AllowFriendRequests { get; set; } = true;

    /// <summary>
    /// 是否显示在线状态
    /// </summary>
    [BsonElement("showOnlineStatus")]
    public bool ShowOnlineStatus { get; set; } = true;

    /// <summary>
    /// 自动战斗设置
    /// </summary>
    [BsonElement("autoBattleSettings")]
    public Dictionary<string, object> AutoBattleSettings { get; set; } = new();
}

/// <summary>
/// 画质设置枚举
/// </summary>
public enum GraphicsQuality
{
    /// <summary>
    /// 低画质
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中画质
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高画质
    /// </summary>
    High = 3,

    /// <summary>
    /// 超高画质
    /// </summary>
    Ultra = 4
}

/// <summary>
/// 帧率设置枚举
/// </summary>
public enum FrameRate
{
    /// <summary>
    /// 30 FPS
    /// </summary>
    FPS30 = 30,

    /// <summary>
    /// 60 FPS
    /// </summary>
    FPS60 = 60,

    /// <summary>
    /// 120 FPS
    /// </summary>
    FPS120 = 120
}