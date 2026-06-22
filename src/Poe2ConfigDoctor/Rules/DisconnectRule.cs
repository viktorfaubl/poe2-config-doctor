using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// Attributes "Abnormal disconnect" events: a disconnect within ~2 minutes of a D3D12 Device-Removed
/// is a GPU-crash cascade (fixable via the renderer/VRAM advice); one with no nearby crash is likely
/// network/server-side. Diagnostic only — no config change.
/// </summary>
public sealed class DisconnectRule : IRule
{
    private const double ProximitySeconds = 120;

    public string Id => "DISCONNECT";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        if (log.Scope.AbnormalDisconnect == 0)
            return null;

        var disconnects = log.DisconnectTimes
            .Where(t => log.WindowStart is null || t >= log.WindowStart)
            .ToList();
        if (disconnects.Count == 0)
            return null;

        int cascade = 0, network = 0;
        foreach (var dc in disconnects)
        {
            bool nearCrash = log.DeviceRemovedTimes.Any(dr => Math.Abs((dc - dr).TotalSeconds) <= ProximitySeconds);
            if (nearCrash) cascade++; else network++;
        }

        var parts = new List<string>();
        if (cascade > 0)
            parts.Add($"{cascade} sat within ~2 min of a GPU Device-Removed — a GPU-crash cascade; fixing the crash (renderer/VRAM advice above) fixes these.");
        if (network > 0)
            parts.Add($"{network} had no nearby crash — likely network/server-side: try a different login Gateway, set DNS to 8.8.8.8 / 8.8.4.4, and use a wired connection.");

        return new Finding
        {
            RuleId = Id,
            Title = "Disconnects — likely causes",
            Severity = Severity.Info,
            Detail = $"{disconnects.Count} abnormal disconnect(s) in the {log.ScopeName}:\n    " + string.Join("\n    ", parts),
        };
    }
}
