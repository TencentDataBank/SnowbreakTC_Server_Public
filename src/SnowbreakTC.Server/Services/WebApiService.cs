using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnowbreakTC.Core.Configuration;
using SnowbreakTC.Core.Services;

namespace SnowbreakTC.Server.Services;

/// <summary>
/// Web API 服务实现
/// </summary>
public class WebApiService : IWebApiService
{
    private readonly ILogger<WebApiService> _logger;
    private readonly ServerConfiguration _serverConfig;
    private readonly IServiceProvider _serviceProvider;
    private IHost? _webHost;

    public bool IsRunning { get; private set; }

    public WebApiService(
        ILogger<WebApiService> logger,
        IOptions<ServerConfiguration> serverConfig,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serverConfig = serverConfig.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Web API 服务已经在运行中");
            return;
        }

        try
        {
            _webHost = CreateWebHost();
            await _webHost.StartAsync(cancellationToken);
            
            IsRunning = true;
            _logger.LogInformation("Web API 服务已启动，监听地址: {Urls}", _serverConfig.WebAPI.Urls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 Web API 服务失败");
            IsRunning = false;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || _webHost == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("正在停止 Web API 服务...");
            
            await _webHost.StopAsync(cancellationToken);
            _webHost.Dispose();
            _webHost = null;
            
            IsRunning = false;
            _logger.LogInformation("Web API 服务已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止 Web API 服务时出错");
            throw;
        }
    }

    private IHost CreateWebHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseUrls(_serverConfig.WebAPI.Urls)
                    .ConfigureServices(services =>
                    {
                        // 复制主服务容器的服务
                        foreach (var service in _serviceProvider.GetServices<ServiceDescriptor>())
                        {
                            services.Add(service);
                        }

                        // 添加 Web API 特定服务
                        services.AddControllers();
                        services.AddEndpointsApiExplorer();
                        services.AddSwaggerGen();
                        
                        // 添加 CORS
                        services.AddCors(options =>
                        {
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.AllowAnyOrigin()
                                       .AllowAnyMethod()
                                       .AllowAnyHeader();
                            });
                        });
                    })
                    .Configure(app =>
                    {
                        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                        
                        if (env.IsDevelopment())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI();
                        }

                        app.UseRouting();
                        app.UseCors();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            
                            // 健康检查端点
                            endpoints.MapGet("/health", () => new
                            {
                                Status = "Healthy",
                                Timestamp = DateTime.UtcNow,
                                Version = "1.0.0"
                            });
                        });
                    });
            })
            .Build();
    }
}