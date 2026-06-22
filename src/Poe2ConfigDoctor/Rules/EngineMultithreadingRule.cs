using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// When freezes/crashes appear and engine multithreading is enabled, advises testing it disabled —
/// PoE2's multithreaded path causes load/portal freezes for some setups. Advisory only (it's a
/// trade-off that can lower FPS), so nothing is auto-applied.
/// </summary>
public sealed class EngineMultithreadingRule : IRule
{
    public string Id => "ENGINE-MT";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        var c = log.Scope;
        bool freezeFamily = c.Dx12DeviceRemoved > 0 || c.PipelineGenerationFailed > 0 || c.ShaderVertexLayout > 0;
        if (!freezeFamily)
            return null;

        var mode = config.Get(Poe2Config.GeneralSection, "engine_multithreading_mode");
        if (!string.Equals(mode, "enabled", StringComparison.OrdinalIgnoreCase))
            return null;

        return new Finding
        {
            RuleId = Id,
            Title = "Freezes with engine multithreading enabled — worth A/B testing",
            Severity = Severity.Info,
            Detail = $"Freezes/crashes appear in the {log.ScopeName} and `engine_multithreading_mode=enabled`. "
                + "PoE2's multithreaded path triggers load/portal freezes on some setups. If freezes persist, "
                + "test `engine_multithreading_mode=disabled` in [GENERAL] for a session.\n    "
                + "Not auto-applied: disabling it can sharply lower FPS, so it's a trade-off to test, not a default.",
        };
    }
}
