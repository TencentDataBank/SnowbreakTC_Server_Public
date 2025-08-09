using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SnowbreakTC.Core.Configuration;
using SnowbreakTC.Core.Services;
using SnowbreakTC.Database.Extensions;
using SnowbreakTC.GameLogic.Extensions;
using SnowbreakTC.Server.Services;

namespace SnowbreakTC.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/snowbreak-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("SnowbreakTC Server 正在启动...");

            var host = CreateHostBuilder(args).Build();

            // 启动服务器
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "服务器启动失败");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                    optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // 配置选项
                services.Configure<ServerConfiguration>(
                    context.Configuration.GetSection("Server"));
                services.Configure<DatabaseConfiguration>(
                    context.Configuration.GetSection("Database"));
                services.Configure<GameConfiguration>(
                    context.Configuration.GetSection("Game"));

                // 注册数据库服务
                services.AddDatabase(context.Configuration);

                // 注册游戏逻辑服务
                services.AddGameLogic();

                // 注册核心服务
                services.AddSingleton<IGameServerService, GameServerService>();
                services.AddSingleton<IWebApiService, WebApiService>();

                // 注册后台服务
                services.AddHostedService<GameServerHostedService>();
                services.AddHostedService<WebApiHostedService>();
            });
}