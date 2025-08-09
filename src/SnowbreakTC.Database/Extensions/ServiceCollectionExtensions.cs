using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SnowbreakTC.Core.Configuration;
using SnowbreakTC.Database.Repositories;
using StackExchange.Redis;

namespace SnowbreakTC.Database.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加数据库服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns></returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // 配置数据库选项
        services.Configure<DatabaseConfiguration>(
            configuration.GetSection("Database"));

        // 注册 MongoDB 客户端
        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var dbConfig = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
            return new MongoClient(dbConfig.MongoDB.ConnectionString);
        });

        // 注册 MongoDB 数据库
        services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            var dbConfig = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
            return client.GetDatabase(dbConfig.MongoDB.DatabaseName);
        });

        // 注册 Redis 连接
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var dbConfig = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
            return ConnectionMultiplexer.Connect(dbConfig.Redis.ConnectionString);
        });

        // 注册 Redis 数据库
        services.AddSingleton<IDatabase>(serviceProvider =>
        {
            var connection = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var dbConfig = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
            return connection.GetDatabase(dbConfig.Redis.Database);
        });

        // 注册仓储服务
        services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IPlayerRepository, PlayerRepository>();
        // services.AddScoped<ICharacterRepository, CharacterRepository>();
        // services.AddScoped<IItemRepository, ItemRepository>();
        // services.AddScoped<IInventoryRepository, InventoryRepository>();

        return services;
    }
}