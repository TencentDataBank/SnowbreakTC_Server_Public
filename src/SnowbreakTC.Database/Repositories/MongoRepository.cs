using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SnowbreakTC.Core.Configuration;
using SnowbreakTC.Core.Models;

namespace SnowbreakTC.Database.Repositories;

/// <summary>
/// MongoDB 仓储基类实现
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;
    protected readonly ILogger<MongoRepository<T>> _logger;
    protected readonly DatabaseConfiguration _dbConfig;

    public MongoRepository(
        IMongoDatabase database,
        ILogger<MongoRepository<T>> logger,
        IOptions<DatabaseConfiguration> dbConfig)
    {
        _logger = logger;
        _dbConfig = dbConfig.Value;
        
        var collectionName = GetCollectionName();
        _collection = database.GetCollection<T>(collectionName);
        
        // 创建索引
        CreateIndexes();
    }

    /// <summary>
    /// 获取集合名称
    /// </summary>
    /// <returns></returns>
    protected virtual string GetCollectionName()
    {
        var attribute = typeof(T).GetCustomAttribute<BsonCollectionAttribute>();
        return attribute?.CollectionName ?? typeof(T).Name.ToLowerInvariant() + "s";
    }

    /// <summary>
    /// 创建索引
    /// </summary>
    protected virtual void CreateIndexes()
    {
        try
        {
            var indexKeys = Builders<T>.IndexKeys;
            var indexModels = new List<CreateIndexModel<T>>();

            // 创建基础索引
            indexModels.Add(new CreateIndexModel<T>(indexKeys.Ascending(x => x.CreatedAt)));
            indexModels.Add(new CreateIndexModel<T>(indexKeys.Ascending(x => x.UpdatedAt)));
            indexModels.Add(new CreateIndexModel<T>(indexKeys.Ascending(x => x.IsDeleted)));

            // 创建复合索引
            indexModels.Add(new CreateIndexModel<T>(
                indexKeys.Combine(
                    indexKeys.Ascending(x => x.IsDeleted),
                    indexKeys.Descending(x => x.CreatedAt)
                )
            ));

            if (indexModels.Any())
            {
                _collection.Indexes.CreateMany(indexModels);
                _logger.LogDebug("为集合 {CollectionName} 创建了 {IndexCount} 个索引", 
                    GetCollectionName(), indexModels.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "创建索引时出错");
        }
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );

            var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            
            _logger.LogDebug("根据ID {Id} 查询实体，结果: {Found}", id, result != null ? "找到" : "未找到");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID {Id} 获取实体时出错", id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
            var result = await _collection.Find(filter).ToListAsync(cancellationToken);
            
            _logger.LogDebug("获取所有实体，数量: {Count}", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有实体时出错");
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Where(predicate),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );

            var result = await _collection.Find(filter).ToListAsync(cancellationToken);
            
            _logger.LogDebug("根据条件查询实体，数量: {Count}", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据条件查询实体时出错");
            throw;
        }
    }

    public virtual async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Where(predicate),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );

            var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            
            _logger.LogDebug("根据条件查询单个实体，结果: {Found}", result != null ? "找到" : "未找到");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据条件查询单个实体时出错");
            throw;
        }
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? predicate = null,
        int pageIndex = 0,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<T>.Filter;
            var filter = filterBuilder.Eq(x => x.IsDeleted, false);

            if (predicate != null)
            {
                filter = filterBuilder.And(filter, filterBuilder.Where(predicate));
            }

            var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            
            var items = await _collection
                .Find(filter)
                .Sort(Builders<T>.Sort.Descending(x => x.CreatedAt))
                .Skip(pageIndex * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            _logger.LogDebug("分页查询实体，页索引: {PageIndex}, 页大小: {PageSize}, 总数: {TotalCount}, 当前页数量: {CurrentCount}",
                pageIndex, pageSize, totalCount, items.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询实体时出错");
            throw;
        }
    }

    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Version = 1;

            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            
            _logger.LogDebug("创建实体成功，ID: {Id}", entity.Id);
            
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建实体时出错");
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> CreateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            var now = DateTime.UtcNow;

            foreach (var entity in entityList)
            {
                entity.CreatedAt = now;
                entity.UpdatedAt = now;
                entity.Version = 1;
            }

            await _collection.InsertManyAsync(entityList, cancellationToken: cancellationToken);
            
            _logger.LogDebug("批量创建实体成功，数量: {Count}", entityList.Count);
            
            return entityList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建实体时出错");
            throw;
        }
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Version++;

            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, entity.Id),
                Builders<T>.Filter.Eq(x => x.Version, entity.Version - 1) // 乐观锁
            );

            var result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException($"实体 {entity.Id} 不存在或版本冲突");
            }

            _logger.LogDebug("更新实体成功，ID: {Id}, 版本: {Version}", entity.Id, entity.Version);
            
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新实体时出错，ID: {Id}", entity.Id);
            throw;
        }
    }

    public virtual async Task<bool> UpdatePartialAsync(string id, object updateDefinition, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<T>.Update
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.Version, 1);

            // 这里需要根据 updateDefinition 构建更新操作
            // 简化实现，实际项目中可能需要更复杂的逻辑

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            _logger.LogDebug("部分更新实体，ID: {Id}, 匹配数: {MatchedCount}, 修改数: {ModifiedCount}",
                id, result.MatchedCount, result.ModifiedCount);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "部分更新实体时出错，ID: {Id}", id);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);

            _logger.LogDebug("物理删除实体，ID: {Id}, 删除数: {DeletedCount}", id, result.DeletedCount);

            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "物理删除实体时出错，ID: {Id}", id);
            throw;
        }
    }

    public virtual async Task<bool> SoftDeleteAsync(string id, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<T>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.Version, 1);

            if (!string.IsNullOrEmpty(deletedBy) && typeof(T).IsSubclassOf(typeof(AuditableEntity)))
            {
                update = update.Set("deletedBy", deletedBy);
            }

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            _logger.LogDebug("软删除实体，ID: {Id}, 删除者: {DeletedBy}, 修改数: {ModifiedCount}",
                id, deletedBy, result.ModifiedCount);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "软删除实体时出错，ID: {Id}", id);
            throw;
        }
    }

    public virtual async Task<long> DeleteManyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.Where(predicate);
            var result = await _collection.DeleteManyAsync(filter, cancellationToken);

            _logger.LogDebug("批量删除实体，删除数: {DeletedCount}", result.DeletedCount);

            return result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除实体时出错");
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Where(predicate),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );

            var count = await _collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
            
            _logger.LogDebug("检查实体是否存在，结果: {Exists}", count > 0);
            
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查实体是否存在时出错");
            throw;
        }
    }

    public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<T>.Filter;
            var filter = filterBuilder.Eq(x => x.IsDeleted, false);

            if (predicate != null)
            {
                filter = filterBuilder.And(filter, filterBuilder.Where(predicate));
            }

            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            
            _logger.LogDebug("统计实体数量，结果: {Count}", count);
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "统计实体数量时出错");
            throw;
        }
    }

    public virtual async Task<IEnumerable<TResult>> AggregateAsync<TResult>(
        object[] pipeline,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 简化实现，实际项目中需要更复杂的聚合管道处理
            var result = await _collection.Aggregate()
                .As<TResult>()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("聚合查询完成，结果数量: {Count}", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚合查询时出错");
            throw;
        }
    }
}