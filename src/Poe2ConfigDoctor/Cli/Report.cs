using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Maintenance;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Cli;

/// <summary>Console output helpers — colored, human-readable reporting.</summary>
public static class Report
{
    public static void Title(string text)
    {
        Console.WriteLine();
        WriteLine($"== {text} ==", ConsoleColor.Cyan);
        Console.WriteLine();
    }

    public static void LogSummary(LogScanResult s)
    {
        WriteLine("Log", ConsoleColor.White);
        Console.WriteLine($"  Path      : {s.LogPath}");
        Console.WriteLine($"  Size      : {s.FileSizeBytes / 1024.0 / 1024.0:0.0} MB, {s.TotalLines:n0} lines");
        Console.WriteLine($"  Sessions  : {s.SessionCount}");
        if (s.FirstTimestamp is { } a && s.LastTimestamp is { } b)
            Console.WriteLine($"  Span      : {a:yyyy-MM-dd HH:mm} -> {b:yyyy-MM-dd HH:mm}");
        if (s.CurrentRenderer is { } r)
            Console.WriteLine($"  Renderer  : {r} (last started)");
        if (s.GpuName is { } g)
            Console.WriteLine($"  GPU       : {g} [{s.GpuVendor}]");
        if (s.DeviceLocalVramGb is { } gb)
            Console.WriteLine($"  VRAM      : {gb:0.0} GB (DeviceLocal heap)");
        if (s.DriverVersion is { } dv)
            Console.WriteLine($"  Driver    : {dv}" + (s.DistinctDriverVersions.Count > 1 ? $"  (changed: {string.Join(" -> ", s.DistinctDriverVersions)})" : ""));
        if (s.WindowsBuild is { } wb)
            Console.WriteLine($"  Windows   : build {wb}{(wb >= 26100 ? " (24H2)" : "")}");
        if (s.HagsEnabled is { } hags)
            Console.WriteLine($"  HAGS      : {(hags ? "Enabled" : "Disabled")}");

        WriteIssueLine("  Issues    ", s.Total, "whole log");
        if (!ReferenceEquals(s.Scope, s.Total))
            WriteIssueLine("  In scope  ", s.Scope, s.ScopeName);
        Console.WriteLine();
    }

    private static void WriteIssueLine(string label, IssueCounts c, string scope)
    {
        Console.WriteLine($"{label}: OOM={c.VramOom}  DeviceRemoved={c.Dx12DeviceRemoved}  " +
                          $"PipelineFail={c.PipelineGenerationFailed}  VertexLayout={c.ShaderVertexLayout}  " +
                          $"Disconnect={c.AbnormalDisconnect}  ({scope})");
    }

    public static void Findings(IReadOnlyList<Finding> findings, string scopeName)
    {
        Console.WriteLine();
        if (findings.Count == 0)
        {
            Success($"No crash / VRAM / FPS findings in the {scopeName}.");
            return;
        }

        WriteLine($"Findings ({findings.Count})", ConsoleColor.White);
        Console.WriteLine();
        foreach (var f in findings)
        {
            var (label, color) = f.Severity switch
            {
                Severity.Critical => ("CRITICAL", ConsoleColor.Red),
                Severity.Warning => ("WARNING", ConsoleColor.Yellow),
                _ => ("INFO", ConsoleColor.Gray),
            };
            Write($"  [{label}] ", color);
            var tag = f.Changes.Count == 0 ? "  (advisory — no automatic change)" : "";
            WriteLine($"{f.Title}  ({f.RuleId}){tag}", ConsoleColor.White);
            Console.WriteLine($"    {f.Detail}");
            foreach (var c in f.Changes)
            {
                Write("    -> ", ConsoleColor.Green);
                Console.WriteLine($"{c.Key}: {c.OldValue ?? "(unset)"} => {c.NewValue}");
                Console.WriteLine($"       {c.Reason}");
            }
            Console.WriteLine();
        }
    }

    public static void BaselineSection(IReadOnlyList<ConfigChange> changes, bool willApply)
    {
        Console.WriteLine();
        WriteLine("Baseline (safe defaults)", ConsoleColor.White);
        if (changes.Count == 0)
        {
            Info("  Config already matches the safe baseline.");
            return;
        }

        var status = willApply ? "applied by default on --apply" : "SKIPPED (--no-baseline given)";
        Console.WriteLine($"  {changes.Count} safe default(s) — {status}; opt out with --no-baseline:");
        foreach (var c in changes)
        {
            Write("    -> ", ConsoleColor.Green);
            Console.WriteLine($"{c.Key}: {c.OldValue ?? "(unset)"} => {c.NewValue}");
            Console.WriteLine($"       {c.Reason}");
        }
    }

    public static void CacheCleared(IReadOnlyList<CacheClearResult> results)
    {
        if (results.Count == 0)
        {
            Info("Shader cache: no cache folders found to clear.");
            return;
        }

        var totalFiles = results.Sum(r => r.FilesDeleted);
        var totalMb = results.Sum(r => r.BytesFreed) / 1024.0 / 1024.0;
        Success($"Shader cache cleared: {totalFiles:n0} files, {totalMb:0.0} MB across {results.Count} folder(s) — rebuilds on next launch.");
        foreach (var r in results)
            Console.WriteLine($"    {r.Path}  ({r.FilesDeleted:n0} files)");
    }

    public static void BackupList(string configPath, IReadOnlyList<BackupInfo> backups)
    {
        Console.WriteLine();
        var dir = BackupManager.BackupDir(configPath);
        if (backups.Count == 0)
        {
            Info($"No backups found in {dir}.");
            return;
        }

        WriteLine($"Backups ({backups.Count}) in {dir}", ConsoleColor.White);
        for (int i = 0; i < backups.Count; i++)
        {
            var b = backups[i];
            var tag = i == 0 ? "  (most recent)" : "";
            Console.WriteLine($"  {b.Timestamp:yyyy-MM-dd HH:mm:ss}  {Path.GetFileName(b.Path)}{tag}");
        }
        Console.WriteLine();
        Info("Restore the most recent with --restore, or a specific one with --restore <filename>.");
    }

    public static void Success(string text) => WriteLine(text, ConsoleColor.Green);
    public static void Info(string text) => WriteLine(text, ConsoleColor.Gray);
    public static void Warn(string text) => WriteLine($"WARN: {text}", ConsoleColor.Yellow);
    public static void Error(string text) => WriteLine($"ERROR: {text}", ConsoleColor.Red);

    private static void Write(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    private static void WriteLine(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }
}
