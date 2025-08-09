using System.Text.Json.Serialization;

namespace SnowbreakTC.Protocol.Messages;

/// <summary>
/// 游戏消息基类
/// </summary>
public abstract class GameMessage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 消息类型
    /// </summary>
    [JsonPropertyName("messageType")]
    public abstract MessageType MessageType { get; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 玩家ID（可选）
    /// </summary>
    [JsonPropertyName("playerId")]
    public string? PlayerId { get; set; }
}

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    // 连接相关
    Handshake = 1001,
    Heartbeat = 1002,
    Disconnect = 1003,

    // 认证相关
    Login = 2001,
    LoginResponse = 2002,
    Logout = 2003,

    // 玩家相关
    GetPlayerInfo = 3001,
    PlayerInfoResponse = 3002,
    UpdatePlayerInfo = 3003,

    // 角色相关
    GetCharacterList = 4001,
    CharacterListResponse = 4002,
    SelectCharacter = 4003,

    // 战斗相关
    EnterBattle = 5001,
    BattleStart = 5002,
    BattleAction = 5003,
    BattleResult = 5004,
    ExitBattle = 5005,

    // 物品相关
    GetInventory = 6001,
    InventoryResponse = 6002,
    UseItem = 6003,

    // 抽卡相关
    Gacha = 7001,
    GachaResult = 7002,

    // 错误消息
    Error = 9999
}

/// <summary>
/// 握手消息
/// </summary>
public class HandshakeMessage : GameMessage
{
    public override MessageType MessageType => MessageType.Handshake;

    /// <summary>
    /// 客户端版本
    /// </summary>
    [JsonPropertyName("clientVersion")]
    public string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// 设备信息
    /// </summary>
    [JsonPropertyName("deviceInfo")]
    public DeviceInfo? DeviceInfo { get; set; }
}

/// <summary>
/// 心跳消息
/// </summary>
public class HeartbeatMessage : GameMessage
{
    public override MessageType MessageType => MessageType.Heartbeat;

    /// <summary>
    /// 序列号
    /// </summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }
}

/// <summary>
/// 登录消息
/// </summary>
public class LoginMessage : GameMessage
{
    public override MessageType MessageType => MessageType.Login;

    /// <summary>
    /// 访问令牌
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 设备ID
    /// </summary>
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应消息
/// </summary>
public class LoginResponseMessage : GameMessage
{
    public override MessageType MessageType => MessageType.LoginResponse;

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 玩家基本信息
    /// </summary>
    [JsonPropertyName("playerInfo")]
    public PlayerBasicInfo? PlayerInfo { get; set; }
}

/// <summary>
/// 获取玩家信息消息
/// </summary>
public class GetPlayerInfoMessage : GameMessage
{
    public override MessageType MessageType => MessageType.GetPlayerInfo;
}

/// <summary>
/// 玩家信息响应消息
/// </summary>
public class PlayerInfoResponseMessage : GameMessage
{
    public override MessageType MessageType => MessageType.PlayerInfoResponse;

    /// <summary>
    /// 玩家详细信息
    /// </summary>
    [JsonPropertyName("playerInfo")]
    public PlayerDetailInfo? PlayerInfo { get; set; }
}

/// <summary>
/// 错误消息
/// </summary>
public class ErrorMessage : GameMessage
{
    public override MessageType MessageType => MessageType.Error;

    /// <summary>
    /// 错误代码
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 原始消息ID
    /// </summary>
    [JsonPropertyName("originalMessageId")]
    public string? OriginalMessageId { get; set; }
}

/// <summary>
/// 设备信息
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// 设备类型
    /// </summary>
    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// 操作系统
    /// </summary>
    [JsonPropertyName("os")]
    public string OS { get; set; } = string.Empty;

    /// <summary>
    /// 操作系统版本
    /// </summary>
    [JsonPropertyName("osVersion")]
    public string OSVersion { get; set; } = string.Empty;

    /// <summary>
    /// 设备型号
    /// </summary>
    [JsonPropertyName("deviceModel")]
    public string DeviceModel { get; set; } = string.Empty;
}

/// <summary>
/// 玩家基本信息
/// </summary>
public class PlayerBasicInfo
{
    /// <summary>
    /// 玩家ID
    /// </summary>
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 等级
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// 头像ID
    /// </summary>
    [JsonPropertyName("avatarId")]
    public int AvatarId { get; set; }
}

/// <summary>
/// 玩家详细信息
/// </summary>
public class PlayerDetailInfo : PlayerBasicInfo
{
    /// <summary>
    /// 经验值
    /// </summary>
    [JsonPropertyName("experience")]
    public long Experience { get; set; }

    /// <summary>
    /// 货币信息
    /// </summary>
    [JsonPropertyName("currencies")]
    public Dictionary<string, long> Currencies { get; set; } = new();

    /// <summary>
    /// VIP等级
    /// </summary>
    [JsonPropertyName("vipLevel")]
    public int VipLevel { get; set; }

    /// <summary>
    /// 最后活跃时间
    /// </summary>
    [JsonPropertyName("lastActiveAt")]
    public long LastActiveAt { get; set; }
}