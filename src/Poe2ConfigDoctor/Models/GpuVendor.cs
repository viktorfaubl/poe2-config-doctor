namespace Poe2ConfigDoctor.Models;

/// <summary>GPU vendor, derived from the adapter name in the log.</summary>
public enum GpuVendor
{
    Unknown,
    Nvidia,
    Amd,
    Intel,
}

public static class GpuVendorExtensions
{
    public static GpuVendor FromAdapterName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return GpuVendor.Unknown;
        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Nvidia;
        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Amd;
        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) || name.Contains("Arc", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Intel;
        return GpuVendor.Unknown;
    }

    /// <summary>The upscaler method to recommend for this vendor, or null if unknown.</summary>
    public static string? PreferredUpscaler(this GpuVendor vendor) => vendor switch
    {
        GpuVendor.Nvidia => "DLSS",
        GpuVendor.Amd => "FSR",
        GpuVendor.Intel => "XeSS",
        _ => null,
    };
}
