using Serilog;
using SnowbreakTC.Core;
using SnowbreakTC.Database;
using SnowbreakTC.GameLogic.Resources;
using SnowbreakTC.Server;
using SnowbreakTC.Server.Game;
using SnowbreakTC.Server.Sdk;
using SnowbreakTC.WebAPI;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/snowbreak-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

AppConfig config;
try
{
    config = AppConfig.Load();
}
catch (Exception ex)
{
    Log.Fatal(ex, "config.json is invalid");
    return;
}

using var db = new SnowbreakDb(config.Database.File);
var tokens = new TokenService(db, Path.Combine(config.DataPath, "Keys"));
var resources = new ResourceManager(config.ResourcePath);
if (!resources.Init())
    Log.Warning("resource directory not found: {Path}, running without config tables", config.ResourcePath);

var game = new GameServer(config, db, tokens, resources);
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

game.Start(cts.Token);

using var sdk = SdkHost.Build(config, db, tokens, game);
using var admin = AdminHost.Build(config, db, new AdminBridge(game), line => GmExecutor.Run(game, line));
await sdk.StartAsync();
await admin.StartAsync();

Log.Information("SnowbreakTC ready — game :{GamePort}, sdk :{SdkPort}, admin :{AdminPort}, callgs apis :{Apis}",
    config.Game.Port, config.Sdk.Port, config.Admin.Port, SnowbreakTC.Server.Handlers.CallGs.Router.RegisteredCount);

var gmTask = GmConsole.RunAsync(game, cts.Token);

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException) { }

Log.Information("shutting down");
game.Stop();
using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await sdk.StopAsync(shutdownCts.Token);
await admin.StopAsync(shutdownCts.Token);
Log.CloseAndFlush();
