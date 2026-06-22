namespace Poe2ConfigDoctor.Config;

/// <summary>Section/key constants and quality-scale ordering for the PoE2 config keys this tool touches.</summary>
public static class Poe2Config
{
    /// <summary>All graphics keys this tool reads/writes live in the [DISPLAY] section.</summary>
    public const string DisplaySection = "DISPLAY";

    public const string RendererType = "renderer_type";
    public const string TextureQuality = "texture_quality";
    public const string UpscaleResolution = "upscale_resolution";
    public const string ShadowType = "shadow_type";
    public const string Hdr = "hdr";

    /// <summary>Quality scales ordered cheapest → most expensive (higher index = more VRAM / GPU cost).</summary>
    public static readonly string[] TextureQualityScale = { "TextureQualityLow", "TextureQualityMedium", "TextureQualityHigh" };

    public static readonly string[] ShadowTypeScale = { "Off", "Low", "Medium", "High" };

    /// <summary>DLSS render-resolution presets, cheapest → most expensive.</summary>
    public static readonly string[] UpscaleResolutionScale =
        { "UltraPerformance", "Performance", "Balanced", "Quality", "UltraQuality", "DLAA", "Native" };

    public static int Rank(string[] scale, string value)
    {
        for (int i = 0; i < scale.Length; i++)
            if (scale[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    /// <summary>
    /// If <paramref name="current"/> is more expensive than <paramref name="target"/> on the scale,
    /// returns <paramref name="target"/>; otherwise null (already at/below target, or unrecognized value).
    /// </summary>
    public static string? CapAt(string[] scale, string current, string target)
    {
        int c = Rank(scale, current);
        int t = Rank(scale, target);
        if (c < 0 || t < 0) return null;
        return c > t ? target : null;
    }
}
