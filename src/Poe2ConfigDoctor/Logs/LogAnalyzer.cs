using System.Globalization;
using System.Text.RegularExpressions;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Logs;

/// <summary>
/// Streams a Path of Exile 2 <c>Client.txt</c> and tallies the known failure signatures.
/// Tracks both whole-log totals and the latest session (everything after the last "Game Start").
/// </summary>
public sealed partial class LogAnalyzer
{
    private const string GameStartMarker = "[STARTUP] Game Start";
    private const string RendererMarker = "[RENDER] Starting device:";

    /// <param name="windowStart">Earliest timestamp to include in the windowed counts; null = whole log.</param>
    public LogScanResult Analyze(string path, DateTime? windowStart = null, int tailLines = 0)
    {
        var total = new IssueCounts();
        var latest = new IssueCounts();
        var window = new IssueCounts();

        long lineCount = 0;
        int sessions = 0;
        string? currentRenderer = null;
        string? gpuName = null;
        double? vramGb = null;
        bool? hagsEnabled = null;
        string? lastDeviceRemovedReason = null;
        DateTime? firstTs = null, lastTs = null, lastOom = null, lastDeviceRemoved = null;

        var deviceRemovedTimes = new List<DateTime>();
        var disconnectTimes = new List<DateTime>();
        var sessionSpans = new List<(DateTime Start, DateTime End)>();
        DateTime? curSessionStart = null;

        foreach (var line in ReadLines(path, tailLines))
        {
            lineCount++;

            var ts = ParseTimestamp(line);
            var prevTs = lastTs; // last activity before this line — used to close a session span
            if (ts is not null)
            {
                firstTs ??= ts;
                lastTs = ts;
            }

            // Effective time for this line (carry the last seen timestamp for lines without one).
            bool inWindow = windowStart is null || (lastTs is { } e && e >= windowStart);

            // A new session resets the "latest session" counters and closes the previous session span.
            if (line.Contains(GameStartMarker, StringComparison.Ordinal))
            {
                sessions++;
                latest = new IssueCounts();
                if (curSessionStart is not null && prevTs is not null)
                    sessionSpans.Add((curSessionStart.Value, prevTs.Value));
                curSessionStart = ts ?? prevTs;
            }

            if (hagsEnabled is null && line.Contains("Hardware-accelerated GPU scheduling:", StringComparison.Ordinal))
                hagsEnabled = line.Contains("Enabled", StringComparison.OrdinalIgnoreCase);

            int rendererIdx = line.IndexOf(RendererMarker, StringComparison.Ordinal);
            if (rendererIdx >= 0)
                currentRenderer = line[(rendererIdx + RendererMarker.Length)..].Trim();

            // GPU adapter: the "Found matching" line names the *selected* device (authoritative); fall
            // back to the first non-software "Enumerated adapter:" line otherwise.
            if (line.Contains("Found matching physical device", StringComparison.Ordinal)
                || line.Contains("Found matching adapter", StringComparison.Ordinal))
            {
                var m = AdapterParenRegex().Match(line);
                if (m.Success)
                {
                    var raw = m.Groups["name"].Value;
                    int dash = raw.IndexOf(" - ", StringComparison.Ordinal); // strip " - <monitor>"
                    gpuName = (dash > 0 ? raw[..dash] : raw).Trim();
                }
            }
            else if (gpuName is null
                && line.Contains("Enumerated adapter:", StringComparison.Ordinal)
                && !line.Contains("Microsoft Basic Render Driver", StringComparison.Ordinal))
            {
                int idx = line.IndexOf("Enumerated adapter:", StringComparison.Ordinal);
                gpuName = line[(idx + "Enumerated adapter:".Length)..].Trim();
            }

            if (vramGb is null && line.Contains("Memory heap", StringComparison.Ordinal)
                && line.Contains("DeviceLocal", StringComparison.Ordinal))
            {
                var m = HeapSizeRegex().Match(line);
                if (m.Success && double.TryParse(m.Groups["gb"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gb))
                    vramGb = gb;
            }

            if (line.Contains("eWarnOutOfVRAM", StringComparison.Ordinal))
            {
                total.VramOom++; latest.VramOom++;
                if (inWindow) window.VramOom++;
                lastOom = ts ?? lastOom;
            }

            if (line.Contains("[D3D12] Device Removed", StringComparison.Ordinal))
            {
                total.Dx12DeviceRemoved++; latest.Dx12DeviceRemoved++;
                if (inWindow) window.Dx12DeviceRemoved++;
                lastDeviceRemoved = ts ?? lastDeviceRemoved;
                if (lastTs is { } drt) deviceRemovedTimes.Add(drt);
                var m = ReasonRegex().Match(line);
                if (m.Success) lastDeviceRemovedReason = m.Groups["reason"].Value;
            }

            if (line.Contains("Pipeline generation failed", StringComparison.Ordinal))
            {
                total.PipelineGenerationFailed++; latest.PipelineGenerationFailed++;
                if (inWindow) window.PipelineGenerationFailed++;
            }

            if (line.Contains("Shader uses incorrect vertex layout", StringComparison.Ordinal))
            {
                total.ShaderVertexLayout++; latest.ShaderVertexLayout++;
                if (inWindow) window.ShaderVertexLayout++;
            }

            if (line.Contains("Abnormal disconnect", StringComparison.Ordinal))
            {
                total.AbnormalDisconnect++; latest.AbnormalDisconnect++;
                if (inWindow) window.AbnormalDisconnect++;
                if (lastTs is { } dct) disconnectTimes.Add(dct);
            }
        }

        // Close the final (still-open) session, or treat the whole log as one session if no markers.
        if (curSessionStart is not null && lastTs is not null)
            sessionSpans.Add((curSessionStart.Value, lastTs.Value));
        else if (sessionSpans.Count == 0 && firstTs is not null && lastTs is not null)
            sessionSpans.Add((firstTs.Value, lastTs.Value));

        TimeSpan? longestSessionInScope = null;
        foreach (var (start, end) in sessionSpans)
        {
            if (windowStart is not null && end < windowStart) continue;
            var duration = end - start;
            if (longestSessionInScope is null || duration > longestSessionInScope)
                longestSessionInScope = duration;
        }

        // If the log had no "Game Start" marker, the latest session is effectively the whole file.
        if (sessions == 0)
            latest = total;

        // No window restriction means the window is the whole log.
        if (windowStart is null)
            window = total;

        return new LogScanResult
        {
            LogPath = path,
            FileSizeBytes = new FileInfo(path).Length,
            TotalLines = lineCount,
            SessionCount = sessions,
            CurrentRenderer = currentRenderer,
            GpuName = gpuName,
            GpuVendor = GpuVendorExtensions.FromAdapterName(gpuName),
            FirstTimestamp = firstTs,
            LastTimestamp = lastTs,
            DeviceLocalVramGb = vramGb,
            LastDeviceRemovedReason = lastDeviceRemovedReason,
            LastVramOomAt = lastOom,
            LastDeviceRemovedAt = lastDeviceRemoved,
            HagsEnabled = hagsEnabled,
            DeviceRemovedTimes = deviceRemovedTimes,
            DisconnectTimes = disconnectTimes,
            LongestSessionInScope = longestSessionInScope,
            Total = total,
            LatestSession = latest,
            Window = window,
            WindowStart = windowStart,
        };
    }

    /// <summary>Streams lines, or only the last <paramref name="tailLines"/> if &gt; 0 (kept in a ring buffer).</summary>
    private static IEnumerable<string> ReadLines(string path, int tailLines)
    {
        if (tailLines <= 0)
            return File.ReadLines(path);

        var ring = new string[tailLines];
        int count = 0, head = 0;
        foreach (var line in File.ReadLines(path))
        {
            ring[head] = line;
            head = (head + 1) % tailLines;
            count++;
        }

        int take = Math.Min(count, tailLines);
        var result = new List<string>(take);
        int start = count <= tailLines ? 0 : head;
        for (int i = 0; i < take; i++)
            result.Add(ring[(start + i) % tailLines]);
        return result;
    }

    /// <summary>Lines start with "yyyy/MM/dd HH:mm:ss"; parse that prefix if present.</summary>
    private static DateTime? ParseTimestamp(string line)
    {
        if (line.Length < 19) return null;
        return DateTime.TryParseExact(
            line.AsSpan(0, 19), "yyyy/MM/dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : null;
    }

    [GeneratedRegex(@"size\s*=\s*(?<gb>[0-9]+(?:\.[0-9]+)?)\s*GB", RegexOptions.IgnoreCase)]
    private static partial Regex HeapSizeRegex();

    [GeneratedRegex(@"Reason:\s*(?<reason>0x[0-9a-fA-F]+)")]
    private static partial Regex ReasonRegex();

    [GeneratedRegex(@"\((?<name>[^)]+)\)")]
    private static partial Regex AdapterParenRegex();
}
