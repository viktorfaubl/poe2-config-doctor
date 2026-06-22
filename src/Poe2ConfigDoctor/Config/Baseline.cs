using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Config;

/// <summary>
/// The universally-safe baseline preset — settings that broadly help and rarely regress, applied by
/// default on <c>--apply</c>. Rig-specific choices (renderer, how far to drop textures) are left to
/// the log-driven rules; the FPS levers (GI off, particle culling) live in <c>FpsRule</c>.
/// </summary>
public static class Baseline
{
    private sealed record Setting(string Key, string Value, string Reason);

    private static readonly Setting[] Settings =
    {
        new("hdr", "false",
            "HDR enlarges every framebuffer; leave off unless you have a genuinely good HDR display."),
        new("triple_buffering", "false",
            "Unneeded with VRR + vsync off, and avoids added input latency."),
        new("background_framerate_limit_enabled", "true",
            "Cap GPU/CPU use when the game is alt-tabbed to the background."),
    };

    /// <summary>
    /// Settings where the current config differs from the safe baseline. Only existing keys are
    /// proposed (the tool never invents keys). A vendor-appropriate upscaler is suggested only when
    /// upscaling is currently off — the method is never overridden if the user already chose one.
    /// </summary>
    public static List<ConfigChange> Diff(IniConfig config, GpuVendor vendor)
    {
        var changes = new List<ConfigChange>();

        foreach (var s in Settings)
        {
            var current = config.Get(Poe2Config.DisplaySection, s.Key);
            if (current is null) continue; // don't add keys that aren't there
            if (!current.Equals(s.Value, StringComparison.OrdinalIgnoreCase))
                changes.Add(new ConfigChange(s.Key, current, s.Value, s.Reason));
        }

        // Don't stack dynamic resolution on top of an upscaler.
        var upscale = config.Get(Poe2Config.DisplaySection, "upscale");
        var upscalerActive = upscale is not null
            && !upscale.Equals("Off", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(upscale);

        if (upscalerActive)
        {
            var dynRes = config.Get(Poe2Config.DisplaySection, "use_dynamic_resolution");
            if (string.Equals(dynRes, "true", StringComparison.OrdinalIgnoreCase))
                changes.Add(new ConfigChange("use_dynamic_resolution", dynRes, "false",
                    "Don't stack dynamic resolution on top of an upscaler (DLSS/FSR/XeSS) — the result looks muddy."));
        }
        else if (upscale is not null && vendor.PreferredUpscaler() is { } preferred)
        {
            // Upscaling is off and we know the vendor: suggest the right method (never DLSS on AMD, etc.).
            changes.Add(new ConfigChange("upscale", upscale, preferred,
                $"Enable {preferred} upscaling for ~20-30% more FPS on your {vendor} GPU. " +
                "The method matches your vendor; an already-chosen upscaler is never overridden."));
        }

        return changes;
    }
}
