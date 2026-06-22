using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// When crashes/freezes appear and the log shows Hardware-accelerated GPU scheduling (HAGS) enabled,
/// advises disabling it — HAGS conflicts with PoE2's memory management on some Win11 systems. HAGS is
/// a Windows setting, not a game config key, so this is advisory only.
/// </summary>
public sealed class HagsRule : IRule
{
    public string Id => "HAGS";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        var c = log.Scope;
        bool crashFamily = c.Dx12DeviceRemoved > 0 || c.PipelineGenerationFailed > 0 || c.ShaderVertexLayout > 0;
        if (!crashFamily || log.HagsEnabled != true)
            return null;

        return new Finding
        {
            RuleId = Id,
            Title = "Hardware-accelerated GPU scheduling (HAGS) is enabled — try disabling it",
            Severity = Severity.Warning,
            Detail = $"Crashes/freezes appear in the {log.ScopeName} and the log shows HAGS Enabled, which "
                + "conflicts with PoE2's GPU memory management on some Windows 11 systems.\n    "
                + "Disable it: Settings > System > Display > Graphics > 'Change default graphics settings' > "
                + "turn off Hardware-accelerated GPU scheduling, then restart. (Windows setting — the tool can't apply this.)",
        };
    }
}
