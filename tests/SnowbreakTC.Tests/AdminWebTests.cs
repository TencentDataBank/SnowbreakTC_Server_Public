using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SnowbreakTC.Core;
using SnowbreakTC.Database;
using SnowbreakTC.WebAPI;
using Xunit;

namespace SnowbreakTC.Tests;

public class AdminWebTests
{
    private static int FreePort()
    {
        using var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        return ((IPEndPoint)l.LocalEndpoint).Port;
    }

    [Fact]
    public async Task StaticAssets_And_ProtectedApi_WorkEndToEnd()
    {
        var port = FreePort();
        var dir = Path.Combine(AppContext.BaseDirectory, "tmp_web_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var dbPath = Path.Combine(dir, "test.db");
            using (var db = new SnowbreakDb(dbPath))
            {
                var cfg = new AppConfig
                {
                    Admin = new AdminConfig { BindAddress = "127.0.0.1", Port = port, Token = "secret-123" },
                };
                using var host = AdminHost.Build(cfg, db, null, cmd => new GmResult(true, [$"ok:{cmd}"]));
                await host.StartAsync();
                try
                {
                    using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

                    var index = await http.GetAsync("/");
                    Assert.Equal(HttpStatusCode.OK, index.StatusCode);
                    Assert.Contains("text/html", index.Content.Headers.ContentType?.ToString());
                    Assert.Contains("SnowbreakTC", await index.Content.ReadAsStringAsync());

                    var css = await http.GetAsync("/app.css");
                    Assert.Equal(HttpStatusCode.OK, css.StatusCode);
                    Assert.Contains("text/css", css.Content.Headers.ContentType?.ToString());

                    var js = await http.GetAsync("/app.js");
                    Assert.Equal(HttpStatusCode.OK, js.StatusCode);
                    Assert.Contains("javascript", js.Content.Headers.ContentType?.ToString());

                    var unauth = await http.GetAsync("/api/overview");
                    Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "secret-123");
                    var overview = await http.GetAsync("/api/overview");
                    Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

                    var cmdRes = await http.PostAsync("/api/console",
                        new StringContent("{\"command\":\"help\"}", Encoding.UTF8, "application/json"));
                    Assert.Equal(HttpStatusCode.OK, cmdRes.StatusCode);
                    Assert.Contains("application/json", cmdRes.Content.Headers.ContentType?.ToString());
                    using var doc = JsonDocument.Parse(await cmdRes.Content.ReadAsStringAsync());
                    Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
                    Assert.Equal("ok:help", doc.RootElement.GetProperty("messages")[0].GetString());

                    await using (var probe = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly"))
                    {
                        await probe.OpenAsync();
                        using var ver = probe.CreateCommand();
                        ver.CommandText = "PRAGMA user_version";
                        Assert.Equal(SnowbreakTC.Core.Cluster.SchemaVersion, Convert.ToInt32(await ver.ExecuteScalarAsync()));
                        using var meta = probe.CreateCommand();
                        meta.CommandText = "SELECT node FROM _cluster";
                        Assert.StartsWith("SnowbreakTC/", (string?)await meta.ExecuteScalarAsync());
                    }
                }
                finally
                {
                    await host.StopAsync();
                }
            }
            SqliteConnection.ClearAllPools();
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
