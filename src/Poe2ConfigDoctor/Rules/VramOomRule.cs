using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// Detects VRAM exhaustion (<c>eWarnOutOfVRAM</c>) and trims the largest VRAM consumers —
/// textures, DLSS internal resolution, HDR framebuffers, shadows — back under budget.
/// Only proposes a change when the current setting is actually above the safe target.
/// </summary>
public sealed class VramOomRule : IRule
{
    public string Id => "VRAM-OOM";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        int oom = log.LatestSession.VramOom;
        if (oom == 0)
            return null;

        var budget = log.DeviceLocalVramGb is { } gb ? $" on a {gb:0.0} GB VRAM budget" : "";
        var finding = new Finding
        {
            RuleId = Id,
            Title = "Out of VRAM (eWarnOutOfVRAM) — graphics memory budget exceeded",
            Severity = Severity.Critical,
            Detail = $"{oom}x eWarnOutOfVRAM in the latest session{budget}. "
                + "VRAM spill to system memory is what causes the stutter and freezes; "
                + "trimming the biggest consumers brings the game back under budget.",
        };

        AddCap(finding, config, Poe2Config.TextureQuality, Poe2Config.TextureQualityScale,
            "TextureQualityMedium", "Textures are the single largest VRAM consumer.");

        AddCap(finding, config, Poe2Config.UpscaleResolution, Poe2Config.UpscaleResolutionScale,
            "Quality", "Lowering the DLSS internal render resolution shrinks render targets and buffers.");

        AddCap(finding, config, Poe2Config.ShadowType, Poe2Config.ShadowTypeScale,
            "Medium", "Shadow maps use meaningful VRAM and add PSO-compilation hitches.");

        var hdr = config.Get(Poe2Config.DisplaySection, Poe2Config.Hdr);
        if (string.Equals(hdr, "true", StringComparison.OrdinalIgnoreCase))
            finding.Changes.Add(new ConfigChange(Poe2Config.Hdr, hdr, "false",
                "HDR enlarges every framebuffer by roughly 25%."));

        if (finding.Changes.Count == 0)
            finding = finding.WithAllMinimizedNote();

        return finding;
    }

    private static void AddCap(Finding finding, IniConfig config, string key, string[] scale, string target, string reason)
    {
        var current = config.Get(Poe2Config.DisplaySection, key);
        if (current is not null && Poe2Config.CapAt(scale, current, target) is { } newValue)
            finding.Changes.Add(new ConfigChange(key, current, newValue, reason));
    }
}

file static class FindingExtensions
{
    /// <summary>Returns a copy of the finding with a note that everything tunable is already minimized.</summary>
    public static Finding WithAllMinimizedNote(this Finding f) => new()
    {
        RuleId = f.RuleId,
        Title = f.Title,
        Severity = f.Severity,
        Detail = f.Detail
            + "\n    All targeted settings are already at or below safe levels — the 8 GB card may simply be "
            + "maxed for this content. Next levers: lower the display resolution, or reduce screenspace effects.",
    };
}
