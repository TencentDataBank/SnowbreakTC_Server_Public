using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnowbreakTC.GameLogic.Services;

namespace SnowbreakTC.WebAPI.Controllers;

/// <summary>
/// 用户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="request">注册请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>注册结果</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("用户注册请求: {Username}, {Email}", request.Username, request.Email);

            var result = await _userService.RegisterAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("用户注册成功: {UserId}", result.Data?.Id);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "注册成功",
                    Data = new { UserId = result.Data?.Id }
                });
            }

            _logger.LogWarning("用户注册失败: {ErrorMessage}", result.ErrorMessage);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.ErrorMessage ?? "注册失败",
                ErrorCode = result.ErrorCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册时发生异常");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录结果</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 获取客户端IP
            request.IpAddress = GetClientIpAddress();

            _logger.LogInformation("用户登录请求: {UsernameOrEmail}, IP: {IpAddress}", 
                request.UsernameOrEmail, request.IpAddress);

            var result = await _userService.LoginAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("用户登录成功: {UserId}", result.Data?.User.Id);
                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "登录成功",
                    Data = result.Data
                });
            }

            _logger.LogWarning("用户登录失败: {ErrorMessage}", result.ErrorMessage);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.ErrorMessage ?? "登录失败",
                ErrorCode = result.ErrorCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录时发生异常");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "未授权访问"
                });
            }

            var result = await _userService.GetUserAsync(userId, cancellationToken);

            if (result.Success)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        result.Data?.Id,
                        result.Data?.Username,
                        result.Data?.Email,
                        result.Data?.Status,
                        result.Data?.Role,
                        result.Data?.EmailVerified,
                        result.Data?.CreatedAt,
                        result.Data?.LastLoginAt
                    }
                });
            }

            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "用户不存在"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户信息时发生异常");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 检查用户名是否可用
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    [HttpGet("check-username")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckUsername([FromQuery] string username, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "用户名不能为空"
                });
            }

            var result = await _userService.IsUsernameAvailableAsync(username, cancellationToken: cancellationToken);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = result.Data == true ? "用户名可用" : "用户名已被使用"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户名可用性时发生异常");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 检查邮箱是否可用
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    [HttpGet("check-email")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckEmail([FromQuery] string email, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "邮箱地址不能为空"
                });
            }

            var result = await _userService.IsEmailAvailableAsync(email, cancellationToken: cancellationToken);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = result.Data == true ? "邮箱可用" : "邮箱已被使用"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查邮箱可用性时发生异常");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    /// <returns>IP地址</returns>
    private string GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // 检查是否通过代理
        if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }
        else if (HttpContext.Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        return ipAddress ?? "unknown";
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>用户ID</returns>
    private string? GetCurrentUserId()
    {
        return HttpContext.User.FindFirst("sub")?.Value ?? 
               HttpContext.User.FindFirst("userId")?.Value;
    }
}

/// <summary>
/// API 响应模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}