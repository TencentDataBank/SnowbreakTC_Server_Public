using System.Net;

namespace SnowbreakTC.Server.Tools;

public static class HostsSetup
{
    public const string Marker = "# SNOWBREAKTC-REDIRECT";

    public static readonly string[] CnDomains =
    [
        "sh-jxsj.xgsdk.com", "xgdata.xgsdk.com", "js2sdk.xoyo.com", "sh-qrcode.xgsdk.com",
        "activity.xgsdk.com", "gpm-conf.testplus.cn", "cbjq-serverlist.xoyocdn.com",
    ];

    public static string HostsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "drivers", "etc", "hosts");

    public sealed record HostsState(bool Readable, string Path, bool Managed, List<string> ManagedLines);

    public static HostsState Read()
    {
        try
        {
            if (!File.Exists(HostsPath))
                return new HostsState(false, HostsPath, false, []);
            var lines = File.ReadAllLines(HostsPath)
                .Where(l => l.Contains(Marker, StringComparison.Ordinal))
                .ToList();
            return new HostsState(true, HostsPath, lines.Count > 0, lines);
        }
        catch
        {
            return new HostsState(false, HostsPath, false, []);
        }
    }

    public static string Write(bool enable, string targetIp)
    {
        targetIp = string.IsNullOrWhiteSpace(targetIp) ? "127.0.0.1" : targetIp.Trim();
        if (!IPAddress.TryParse(targetIp, out _))
            return "invalid address: " + targetIp;
        try
        {
            return WriteDirect(enable, targetIp);
        }
        catch (UnauthorizedAccessException)
        {
            return "hosts write denied: run the server as administrator to manage hosts";
        }
        catch (IOException ex)
        {
            return "hosts write failed: " + ex.Message;
        }
    }

    private static string WriteDirect(bool enable, string targetIp)
    {
        var lines = File.Exists(HostsPath) ? File.ReadAllLines(HostsPath).ToList() : [];
        lines.RemoveAll(l => l.Contains(Marker, StringComparison.Ordinal));
        if (enable)
        {
            lines.Add(Marker);
            foreach (var domain in CnDomains)
                lines.Add($"{targetIp} {domain} {Marker}");
        }
        File.WriteAllLines(HostsPath, lines);
        return enable
            ? $"hosts updated: {CnDomains.Length} domains -> {targetIp}"
            : "hosts updated: SNOWBREAKTC redirect entries removed";
    }

    public static List<(string Check, bool Ok)> ProbeClient(string? gamePath)
    {
        var checks = new List<(string, bool)>();
        if (string.IsNullOrWhiteSpace(gamePath))
        {
            checks.Add(("path provided", false));
            return checks;
        }
        checks.Add(("path exists", Directory.Exists(gamePath)));
        var exe = Directory.EnumerateFiles(gamePath, "*.exe", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Contains("Client", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(f).Contains("Game", StringComparison.OrdinalIgnoreCase));
        checks.Add(("client exe", exe != null));
        checks.Add(("Paks dir", Directory.Exists(Path.Combine(gamePath, "Paks"))));
        checks.Add(("UserDir", Directory.EnumerateDirectories(
            Path.GetDirectoryName(gamePath) ?? ".", "*", SearchOption.TopDirectoryOnly).Any()));
        return checks;
    }
}
