using System.Diagnostics;
using Poe2ConfigDoctor.Cli;
using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;
using Poe2ConfigDoctor.Rules;

const int ExitOk = 0;        // ran cleanly; no issues, or changes applied
const int ExitFindings = 2;  // issues found in a dry run (nothing written)
const int ExitError = 1;     // could not run

var options = Options.Parse(args);

if (options.ShowHelp)
{
    Options.PrintHelp();
    return ExitOk;
}

if (options.Error is not null)
{
    Report.Error(options.Error);
    Console.WriteLine("Run with --help for usage.");
    return ExitError;
}

// Resolve the log.
var logPath = options.LogPath ?? Locator.FindLog();
if (logPath is null)
{
    Report.Error("Could not locate Client.txt. Pass it explicitly with --log <path>.");
    return ExitError;
}
if (!File.Exists(logPath))
{
    Report.Error($"Log file not found: {logPath}");
    return ExitError;
}

// Resolve the config.
var configPath = options.ConfigPath ?? Locator.FindConfig();
if (configPath is null)
{
    Report.Error("Could not locate poe2_production_Config.ini. Pass it explicitly with --config <path>.");
    return ExitError;
}
if (!File.Exists(configPath))
{
    Report.Error($"Config file not found: {configPath}");
    return ExitError;
}

Report.Title("PoE2 Config Doctor");

// 1. Analyze the log, then pick the scope the rules evaluate.
DateTime? windowStart = (options.AllHistory || options.SessionOnly) ? null : DateTime.Now - options.Since;
var scan = new LogAnalyzer().Analyze(logPath, windowStart, options.TailLines);

if (options.AllHistory)
{
    scan.Scope = scan.Total;
    scan.ScopeName = "whole log";
}
else if (options.SessionOnly)
{
    scan.Scope = scan.LatestSession;
    scan.ScopeName = "latest session";
}
else
{
    scan.Scope = scan.Window;
    scan.ScopeName = $"last {FormatDuration(options.Since)}";
}

Report.LogSummary(scan);

// 2. Run the rules against the log + current config.
var config = IniConfig.Load(configPath);
IRule[] rules = { new Dx12CrashRule(), new VramOomRule() };

var findings = rules
    .Select(r => r.Evaluate(scan, config))
    .Where(f => f is not null)
    .Select(f => f!)
    .ToList();

if (findings.Count == 0)
{
    Report.AllClear(scan.ScopeName);
    return ExitOk;
}

Report.Findings(findings);

// Collect the changes (last-writer-wins per key — rules here don't overlap, but be safe).
var changes = findings
    .SelectMany(f => f.Changes)
    .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
    .Select(g => g.Last())
    .ToList();

if (changes.Count == 0)
{
    Console.WriteLine();
    Report.Info("Issues detected, but no config changes apply (settings already minimized).");
    return ExitFindings;
}

// 3. Apply or report.
if (!options.Apply)
{
    Console.WriteLine();
    Report.Info($"Dry run — nothing written. Re-run with --apply to make {changes.Count} change(s).");
    return ExitFindings;
}

if (!options.Force && IsGameRunning())
{
    Console.WriteLine();
    Report.Error("Path of Exile 2 appears to be running. Close it first (it overwrites the config on exit), or pass --force.");
    return ExitError;
}

if (!options.NoBackup)
{
    var backup = config.Backup();
    Report.Info($"Backup written: {backup}");
}

int applied = 0;
foreach (var change in changes)
{
    if (config.Set(Poe2Config.DisplaySection, change.Key, change.NewValue))
        applied++;
    else
        Report.Warn($"Key '{change.Key}' not found in [{Poe2Config.DisplaySection}] — skipped.");
}

config.Save();
Console.WriteLine();
Report.Success($"Applied {applied} change(s) to {configPath}");
Report.Info("Launch the game on the new settings — do not 'Apply' in the in-game menu, or it rewrites the config.");
return ExitOk;

static string FormatDuration(TimeSpan t) =>
    t.TotalDays >= 1 && t.TotalDays % 1 == 0 ? $"{t.TotalDays:0}d"
    : t.TotalHours >= 1 && t.TotalHours % 1 == 0 ? $"{t.TotalHours:0}h"
    : t.TotalDays >= 1 ? $"{t.TotalDays:0.#}d"
    : $"{t.TotalMinutes:0}m";

static bool IsGameRunning()
{
    string[] names = { "PathOfExile", "PathOfExileSteam", "PathOfExile_x64" };
    foreach (var n in names)
    {
        try
        {
            if (Process.GetProcessesByName(n).Length > 0)
                return true;
        }
        catch
        {
            // Process enumeration can fail under odd permissions; treat as not-running.
        }
    }
    return false;
}
