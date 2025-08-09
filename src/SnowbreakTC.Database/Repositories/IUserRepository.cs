using SnowbreakTC.Core.Models;

namespace SnowbreakTC.Database.Repositories;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户对象</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户对象</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsUsernameExistsAsync(string username, string? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsEmailExistsAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新最后登录信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginIp">登录IP</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateLastLoginAsync(string userId, string loginIp, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加失败登录尝试次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前失败次数</returns>
    Task<int> IncrementFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置失败登录尝试次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> ResetFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 锁定用户账号
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="lockUntil">锁定到期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> LockUserAsync(string userId, DateTime lockUntil, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解锁用户账号
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UnlockUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证用户邮箱
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> VerifyEmailAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据角色获取用户列表
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃用户统计
    /// </summary>
    /// <param name="days">天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>活跃用户数</returns>
    Task<long> GetActiveUsersCountAsync(int days = 30, CancellationToken cancellationToken = default);
}