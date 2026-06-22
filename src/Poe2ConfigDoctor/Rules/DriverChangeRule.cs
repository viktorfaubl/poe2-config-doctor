using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// When crashes/freezes appear and the GPU driver changed during the logged period, advises rolling
/// back — driver regressions are a documented crash cause. Driver action — advisory only.
/// </summary>
public sealed class DriverChangeRule : IRule
{
    public string Id => "DRIVER";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        var c = log.Scope;
        bool crashFamily = c.Dx12DeviceRemoved > 0 || c.PipelineGenerationFailed > 0 || c.ShaderVertexLayout > 0;
        if (!crashFamily || log.DistinctDriverVersions.Count < 2)
            return null;

        var chain = string.Join(" -> ", log.DistinctDriverVersions);
        return new Finding
        {
            RuleId = Id,
            Title = "GPU driver changed during the logged period — consider rolling back",
            Severity = Severity.Info,
            Detail = $"Crashes/freezes appear in the {log.ScopeName} and the GPU driver version changed ({chain}). "
                + "Driver regressions are a known PoE2 crash cause.\n    "
                + "If the crashes started right after a driver update, roll back to the previous version "
                + "(NVIDIA: clean reinstall of the prior driver; AMD: Adrenalin rollback). Driver action — the tool can't apply this.",
        };
    }
}
