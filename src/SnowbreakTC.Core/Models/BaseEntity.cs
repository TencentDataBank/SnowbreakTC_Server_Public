using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SnowbreakTC.Core.Models;

/// <summary>
/// 基础实体类
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 实体ID
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// 创建时间
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 版本号（用于乐观锁）
    /// </summary>
    [BsonElement("version")]
    public long Version { get; set; } = 1;

    /// <summary>
    /// 是否已删除（软删除）
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 删除时间
    /// </summary>
    [BsonElement("deletedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// 可审计实体基类
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>
    /// 创建者ID
    /// </summary>
    [BsonElement("createdBy")]
    [BsonIgnoreIfNull]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新者ID
    /// </summary>
    [BsonElement("updatedBy")]
    [BsonIgnoreIfNull]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// 删除者ID
    /// </summary>
    [BsonElement("deletedBy")]
    [BsonIgnoreIfNull]
    public string? DeletedBy { get; set; }
}