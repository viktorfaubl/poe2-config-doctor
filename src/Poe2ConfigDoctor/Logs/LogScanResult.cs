namespace Poe2ConfigDoctor.Logs;

/// <summary>Everything the analyzer learned from one pass over a client log.</summary>
public sealed class LogScanResult
{
    public required string LogPath { get; init; }
    public long FileSizeBytes { get; init; }
    public long TotalLines { get; init; }

    /// <summary>Number of "[STARTUP] Game Start" markers seen.</summary>
    public int SessionCount { get; init; }

    /// <summary>The last renderer the log shows being started (Vulkan / DirectX12 / DirectX11).</summary>
    public string? CurrentRenderer { get; init; }

    public DateTime? FirstTimestamp { get; init; }
    public DateTime? LastTimestamp { get; init; }

    /// <summary>DeviceLocal Vulkan heap size in GB, if reported (≈ usable VRAM).</summary>
    public double? DeviceLocalVramGb { get; init; }

    public string? LastDeviceRemovedReason { get; init; }
    public DateTime? LastVramOomAt { get; init; }
    public DateTime? LastDeviceRemovedAt { get; init; }

    /// <summary>Counts across the whole log.</summary>
    public IssueCounts Total { get; init; } = new();

    /// <summary>Counts since the most recent "Game Start" — i.e. the latest session only.</summary>
    public IssueCounts LatestSession { get; init; } = new();

    /// <summary>Counts within the time window the analyzer was given (e.g. the last 3 days).</summary>
    public IssueCounts Window { get; init; } = new();

    /// <summary>The earliest timestamp the window includes (null = whole log).</summary>
    public DateTime? WindowStart { get; init; }

    /// <summary>The counts the rules should evaluate, chosen by the caller (defaults to the window).</summary>
    public IssueCounts Scope { get; set; } = new();

    /// <summary>Human label for the scope, e.g. "last 3d", "latest session", "whole log".</summary>
    public string ScopeName { get; set; } = "selected scope";
}
