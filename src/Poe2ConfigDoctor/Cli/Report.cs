using Poe2ConfigDoctor.Logs;
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
        if (s.DeviceLocalVramGb is { } gb)
            Console.WriteLine($"  VRAM      : {gb:0.0} GB (DeviceLocal heap)");

        var t = s.Total;
        Console.WriteLine($"  Issues    : OOM={t.VramOom}  DeviceRemoved={t.Dx12DeviceRemoved}  " +
                          $"PipelineFail={t.PipelineGenerationFailed}  VertexLayout={t.ShaderVertexLayout}  " +
                          $"Disconnect={t.AbnormalDisconnect}  (whole log)");
        Console.WriteLine();
    }

    public static void AllClear()
    {
        Success("No known issues found in the latest session. Nothing to change.");
    }

    public static void Findings(IReadOnlyList<Finding> findings)
    {
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
            WriteLine($"{f.Title}  ({f.RuleId})", ConsoleColor.White);
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
