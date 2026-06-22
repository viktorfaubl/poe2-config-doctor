using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// On Windows 11 24H2 (build 26100+), warns about the documented CPU-scheduling regression that causes
/// loading-screen freezes/100% CPU lockups in PoE2 (absent on 23H2). OS-level — advisory only.
/// </summary>
public sealed class Windows24H2Rule : IRule
{
    private const int Build24H2 = 26100;

    public string Id => "WIN-24H2";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        if (log.WindowsBuild is not { } build || build < Build24H2)
            return null;

        var c = log.Scope;
        bool crashFamily = c.Dx12DeviceRemoved > 0 || c.PipelineGenerationFailed > 0 || c.ShaderVertexLayout > 0;
        if (!crashFamily)
            return null;

        return new Finding
        {
            RuleId = Id,
            Title = $"Windows 11 24H2 (build {build}) — known loading-screen freeze regression",
            Severity = Severity.Warning,
            Detail = $"You're on Windows 11 build {build} (24H2), which has a documented CPU-scheduling regression "
                + "causing loading-screen freezes / 100% CPU lockups in PoE2 — absent on 23H2.\n    "
                + "If you get hard lockups on zone transitions: revert to 23H2, or restrict PoE2's CPU affinity off "
                + "CPU0/1, disable Engine Multithreading, and use Windowed Fullscreen. OS-level — the tool can't apply this.",
        };
    }
}
