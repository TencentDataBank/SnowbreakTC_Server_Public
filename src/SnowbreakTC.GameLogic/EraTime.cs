using System.Globalization;

namespace SnowbreakTC.GameLogic;

/// <summary>
/// Time window rule for DLC era tables: rows use "[202401011000]" style timestamps.
/// A window is open while now is inside [start, end). When an era pin is configured
/// (config dlc.era_pin, same format), any window that ended inside the pinned era
/// stays open so the served content remains reachable.
/// </summary>
public static class EraTime
{
    public static DateTime? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var text = raw.Trim().Trim('[', ']');
        return DateTime.TryParseExact(text, "yyyyMMddHHmm", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var t) ? t : null;
    }

    public static bool IsOpen(string? startRaw, string? endRaw, DateTime? eraPin = null)
    {
        var start = Parse(startRaw);
        var end = Parse(endRaw);
        if (start == null || end == null)
            return false;
        var now = DateTime.UtcNow;
        if (start > now)
            return false;
        if (now < end)
            return true;
        return eraPin != null && start <= eraPin && end >= eraPin;
    }
}
