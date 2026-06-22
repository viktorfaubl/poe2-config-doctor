using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// Recommends the highest-value FPS settings on a CPU-bound engine: Global Illumination off (the
/// single biggest lever, benchmarked at ~+40 FPS) and dynamic particle culling on. Config-state
/// based — fires whenever these aren't already optimal, regardless of the log.
/// </summary>
public sealed class FpsRule : IRule
{
    public string Id => "FPS-LEVERS";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        var changes = new List<ConfigChange>();

        var gi = config.Get(Poe2Config.DisplaySection, "global_illumination_detail");
        if (gi is not null && gi.Trim() != "0")
            changes.Add(new ConfigChange("global_illumination_detail", gi, "0",
                "Global Illumination off is the single biggest FPS lever (~+40 FPS / ~32% in benchmarks); it re-lights surfaces per spell cast."));

        var culling = config.Get(Poe2Config.DisplaySection, "use_dynamic_particle_culling2");
        if (culling is not null && !culling.Equals("true", StringComparison.OrdinalIgnoreCase))
            changes.Add(new ConfigChange("use_dynamic_particle_culling2", culling, "true",
                "Dynamic particle culling cuts the CPU cost of dense, effect-heavy content (Breach/Ritual/bosses)."));

        if (changes.Count == 0)
            return null;

        var finding = new Finding
        {
            RuleId = Id,
            Title = "FPS settings are not optimal",
            Severity = Severity.Warning,
            Detail = "PoE2 is CPU-bound (per GGG's own 0.4.0 patch notes); these settings recover the most FPS in dense content.",
        };
        finding.Changes.AddRange(changes);
        return finding;
    }
}
