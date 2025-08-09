using Microsoft.Extensions.DependencyInjection;

namespace SnowbreakTC.GameLogic.Extensions;

/// <summary>
/// 游戏逻辑服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加游戏逻辑服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddGameLogic(this IServiceCollection services)
    {
        // 注册游戏逻辑服务
        // services.AddScoped<IPlayerService, PlayerService>();
        // services.AddScoped<ICharacterService, CharacterService>();
        // services.AddScoped<IInventoryService, InventoryService>();
        // services.AddScoped<IBattleService, BattleService>();
        // services.AddScoped<IGachaService, GachaService>();

        return services;
    }
}