using SnowbreakTC.Core.Models;

namespace SnowbreakTC.GameLogic.Services;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="request">注册请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>注册结果</returns>
    Task<ServiceResult<User>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录结果</returns>
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的访问令牌</returns>
    Task<ServiceResult<string>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登出结果</returns>
    Task<ServiceResult<bool>> LogoutAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    Task<ServiceResult<User>> GetUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<ServiceResult<User>> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">修改密码请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修改结果</returns>
    Task<ServiceResult<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="request">重置密码请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重置结果</returns>
    Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证邮箱
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="verificationCode">验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<ServiceResult<bool>> VerifyEmailAsync(string userId, string verificationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<ServiceResult<bool>> SendEmailVerificationAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否可用
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    Task<ServiceResult<bool>> IsUsernameAvailableAsync(string username, string? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查邮箱是否可用
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    Task<ServiceResult<bool>> IsEmailAvailableAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 服务结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ServiceResult<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>成功结果</returns>
    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    /// <returns>失败结果</returns>
    public static ServiceResult<T> Fail(string errorMessage, string? errorCode = null)
    {
        return new ServiceResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 注册请求
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 确认密码
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// 邀请码（可选）
    /// </summary>
    public string? InviteCode { get; set; }
}

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 登录IP
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 是否记住登录
    /// </summary>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 更新用户请求
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 用户偏好设置
    /// </summary>
    public UserPreferences? Preferences { get; set; }
}

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 当前密码
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 确认新密码
    /// </summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 重置密码请求
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 重置令牌
    /// </summary>
    public string? ResetToken { get; set; }

    /// <summary>
    /// 新密码
    /// </summary>
    public string? NewPassword { get; set; }

    /// <summary>
    /// 确认新密码
    /// </summary>
    public string? ConfirmNewPassword { get; set; }
}