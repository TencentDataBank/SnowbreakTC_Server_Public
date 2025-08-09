using System.Reflection;
using SnowbreakTC.Core;
using SnowbreakTC.Protocol;
using SnowbreakTC.Protocol.Messages;
using SnowbreakTC.Protocol.Wire;
using SnowbreakTC.Server.Game;
using SnowbreakTC.WebAPI;

namespace SnowbreakTC.Server.Handlers.CallGs;

public sealed record CallGsReply(string Arg = "{}", NtfSyncPlayer? Sync = null, string? ReplyApi = null)
{
    public static CallGsReply Error(string key) => new($"{{\"sErr\":\"{key}\"}}");
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RouteAttribute : Attribute
{
    public string Path { get; }

    public RouteAttribute(string path) => Path = path;
}

public interface ICallGsApi
{
    Task<CallGsReply> HandleAsync(GameSession session, string param);
}

public interface IStubApi;

public static class Router
{
    private static readonly Dictionary<string, ICallGsApi> Apis = Build();

    static Router() => AdminHost.ApiCount = Apis.Count;

    public static int RegisteredCount => Apis.Count;

    private static Dictionary<string, ICallGsApi> Build()
    {
        var candidates = typeof(Router).Assembly.GetTypes()
            .Where(t => typeof(ICallGsApi).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .OrderBy(t => typeof(IStubApi).IsAssignableFrom(t))
            .SelectMany(t => t.GetCustomAttributes<RouteAttribute>()
                .Select(r => (Route: r.Path, Api: (ICallGsApi)Activator.CreateInstance(t)!)));
        var map = new Dictionary<string, ICallGsApi>(StringComparer.OrdinalIgnoreCase);
        foreach (var (route, api) in candidates)
        {
            if (!map.ContainsKey(route))
                map[route] = api;
        }
        return map;
    }

    public static async Task DispatchAsync(GameSession session, string api, string param)
    {
        var reply = Apis.TryGetValue(api, out var handler)
            ? await InvokeAsync(handler, session, param)
            : new CallGsReply();
        if (session.Player != null && reply.Sync == null)
            reply = reply with { Sync = SyncBuilder.Build(session.Player) };
        await session.SendAsync(CmdIds.NtfScript, new NtfCallScript
        {
            Api = reply.ReplyApi ?? api,
            Arg = reply.Arg,
            ExtraSync = reply.Sync,
            Seed = Cluster.SeedValue,
        });
    }

    private static async Task<CallGsReply> InvokeAsync(ICallGsApi handler, GameSession session, string param)
    {
        try
        {
            return await handler.HandleAsync(session, param);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "callgs {Api} failed", param.Length > 0 ? handler.GetType().Name : handler.GetType().Name);
            return CallGsReply.Error("error.ServerError");
        }
    }
}
