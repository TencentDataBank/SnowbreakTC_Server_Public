using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SnowbreakTC.Core.Configuration;
using SnowbreakTC.Core.Models;

namespace SnowbreakTC.Database.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(
        IMongoDatabase database,
        ILogger<UserRepository> logger,
        IOptions<DatabaseConfiguration> dbConfig)
        : base(database, logger, dbConfig)
    {
    }

    protected override void CreateIndexes()
    {
        base.CreateIndexes();

        try
        {
            var indexKeys = Builders<User>.IndexKeys;
            var indexModels = new List<CreateIndexModel<User>>();

            // 用户名唯一索引
            indexModels.Add(new CreateIndexModel<User>(
                indexKeys.Ascending(x => x.Username),
                new CreateIndexOptions { Unique = true, Background = true }
            ));

            // 邮箱唯一索引
            indexModels.Add(new CreateIndexModel<User>(
                indexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Unique = true, Background = true }
            ));

            // 用户状态索引
            indexModels.Add(new CreateIndexModel<User>(indexKeys.Ascending(x => x.Status)));

            // 用户角色索引
            indexModels.Add(new CreateIndexModel<User>(indexKeys.Ascending(x => x.Role)));

            // 最后登录时间索引
            indexModels.Add(new CreateIndexModel<User>(indexKeys.Descending(x => x.LastLoginAt)));

            // 复合索引：状态 + 角色
            indexModels.Add(new CreateIndexModel<User>(
                indexKeys.Combine(
                    indexKeys.Ascending(x => x.Status),
                    indexKeys.Ascending(x => x.Role)
                )
            ));

            if (indexModels.Any())
            {
                _collection.Indexes.CreateMany(indexModels);
                _logger.LogDebug("为用户集合创建了 {IndexCount} 个专用索引", indexModels.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "创建用户索引时出错");
        }
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Username, username),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            
            _logger.LogDebug("根据用户名 {Username} 查询用户，结果: {Found}", username, result != null ? "找到" : "未找到");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据用户名 {Username} 获取用户时出错", username);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Email, email),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            
            _logger.LogDebug("根据邮箱 {Email} 查询用户，结果: {Found}", email, result != null ? "找到" : "未找到");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据邮箱 {Email} 获取用户时出错", email);
            throw;
        }
    }

    public async Task<bool> IsUsernameExistsAsync(string username, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(x => x.Username, username),
                filterBuilder.Eq(x => x.IsDeleted, false)
            );

            if (!string.IsNullOrEmpty(excludeUserId))
            {
                filter = filterBuilder.And(filter, filterBuilder.Ne(x => x.Id, excludeUserId));
            }

            var count = await _collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
            
            _logger.LogDebug("检查用户名 {Username} 是否存在，结果: {Exists}", username, count > 0);
            
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户名 {Username} 是否存在时出错", username);
            throw;
        }
    }

    public async Task<bool> IsEmailExistsAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(x => x.Email, email),
                filterBuilder.Eq(x => x.IsDeleted, false)
            );

            if (!string.IsNullOrEmpty(excludeUserId))
            {
                filter = filterBuilder.And(filter, filterBuilder.Ne(x => x.Id, excludeUserId));
            }

            var count = await _collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
            
            _logger.LogDebug("检查邮箱 {Email} 是否存在，结果: {Exists}", email, count > 0);
            
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查邮箱 {Email} 是否存在时出错", email);
            throw;
        }
    }

    public async Task<bool> UpdateLastLoginAsync(string userId, string loginIp, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Id, userId),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<User>.Update
                .Set(x => x.LastLoginAt, DateTime.UtcNow)
                .Set(x => x.LastLoginIp, loginIp)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.LoginCount, 1)
                .Inc(x => x.Version, 1);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            _logger.LogDebug("更新用户 {UserId} 最后登录信息，IP: {LoginIp}, 修改数: {ModifiedCount}",
                userId, loginIp, result.ModifiedCount);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户 {UserId} 最后登录信息时出错", userId);
            throw;
        }
    }

    public async Task<int> IncrementFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Id, userId),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<User>.Update
                .Inc(x => x.FailedLoginAttempts, 1)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.Version, 1);

            var options = new FindOneAndUpdateOptions<User>
            {
                ReturnDocument = ReturnDocument.After,
                Projection = Builders<User>.Projection.Include(x => x.FailedLoginAttempts)
            };

            var result = await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

            var failedAttempts = result?.FailedLoginAttempts ?? 0;
            
            _logger.LogDebug("增加用户 {UserId} 失败登录尝试次数，当前次数: {FailedAttempts}",
                userId, failedAttempts);

            return failedAttempts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增加用户 {UserId} 失败登录尝试次数时出错", userId);
            throw;
        }
    }

    public async Task<bool> ResetFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Id, userId),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<User>.Update
                .Set(x => x.FailedLoginAttempts, 0)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.Version, 1);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            _logger.LogDebug("重置用户 {UserId} 失败登录尝试次数，修改数: {ModifiedCount}",
                userId, result.ModifiedCount);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置用户 {UserId} 失败登录尝试次数时出错", userId);
            throw;
        }
    }

    public async Task<bool> LockUserAsync(string userId, DateTime lockUntil, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Id, userId),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<User>.Update
                .Set(x => x.LockedUntil, lockUntil)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.Version, 1);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            _logger.LogDebug("锁定用户 {UserId} 到 {LockUntil}，修改数: {ModifiedCount}",
                userId, lockUntil, result.ModifiedCount);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "锁定用户 {UserId} 时出错", userId);
            throw;
        }
    }

    public async Task<bool> UnlockUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Id, userId),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<User>.Update
                .Unset(x => x.LockedUntil)
                .Set(x => x.FailedLoginAttempts, 0)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.Version, 1);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            _logger.LogDebug("解锁用户 {UserId}，修改数: {ModifiedCount}", userId, result.ModifiedCount);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解锁用户 {UserId} 时出错", userId);
            throw;
        }
    }

    public async Task<bool> VerifyEmailAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Id, userId),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var update = Builders<User>.Update
                .Set(x => x.EmailVerified, true)
                .Set(x => x.EmailVerifiedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Inc(x => x.Version, 1);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            _logger.LogDebug("验证用户 {UserId} 邮箱，修改数: {ModifiedCount}", userId, result.ModifiedCount);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证用户 {UserId} 邮箱时出错", userId);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(x => x.Role, role),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var result = await _collection.Find(filter)
                .Sort(Builders<User>.Sort.Descending(x => x.CreatedAt))
                .ToListAsync(cancellationToken);

            _logger.LogDebug("根据角色 {Role} 查询用户，数量: {Count}", role, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据角色 {Role} 查询用户时出错", role);
            throw;
        }
    }

    public async Task<long> GetActiveUsersCountAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Gte(x => x.LastLoginAt, cutoffDate),
                Builders<User>.Filter.Eq(x => x.IsDeleted, false)
            );

            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            _logger.LogDebug("获取 {Days} 天内活跃用户数量: {Count}", days, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃用户数量时出错");
            throw;
        }
    }
}