using System.Diagnostics;
using Poe2ConfigDoctor.Cli;
using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Maintenance;
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
IRule[] rules = { new Dx12CrashRule(), new VramOomRule(), new FpsRule() };

var findings = rules
    .Select(r => r.Evaluate(scan, config))
    .Where(f => f is not null)
    .Select(f => f!)
    .ToList();

Report.Findings(findings, scan.ScopeName);

// Reactive (log/config rule) changes — always applied on --apply.
var reactiveChanges = Dedupe(findings.SelectMany(f => f.Changes));
var reactiveKeys = new HashSet<string>(reactiveChanges.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);

// Baseline changes — applied by default, minus anything a rule already covers.
var baselineChanges = Baseline.Diff(config, scan.GpuVendor)
    .Where(c => !reactiveKeys.Contains(c.Key))
    .ToList();

Report.BaselineSection(baselineChanges, willApply: !options.NoBaseline);

var changesToApply = Dedupe(options.NoBaseline ? reactiveChanges : reactiveChanges.Concat(baselineChanges));
bool willClearCache = !options.NoClearCache;

// 3. Apply or report.
if (!options.Apply)
{
    Console.WriteLine();
    var cachePart = willClearCache ? " and clear the shader cache" : "";
    if (changesToApply.Count == 0 && !willClearCache)
        Report.Success($"Nothing to do — no findings in the {scan.ScopeName} and config matches baseline.");
    else
        Report.Info($"Dry run — nothing written. Re-run with --apply to make {changesToApply.Count} config change(s){cachePart}. "
            + "Opt out with --no-baseline / --no-clear-cache.");
    return changesToApply.Count > 0 ? ExitFindings : ExitOk;
}

if (!options.Force && IsGameRunning())
{
    Console.WriteLine();
    Report.Error("Path of Exile 2 appears to be running. Close it first (it overwrites the config on exit), or pass --force.");
    return ExitError;
}

Console.WriteLine();

// 3a. Config changes.
if (changesToApply.Count > 0)
{
    if (!options.NoBackup)
        Report.Info($"Backup written: {config.Backup()}");

    int applied = 0;
    foreach (var change in changesToApply)
    {
        if (config.Set(Poe2Config.DisplaySection, change.Key, change.NewValue))
            applied++;
        else
            Report.Warn($"Key '{change.Key}' not found in [{Poe2Config.DisplaySection}] — skipped.");
    }
    config.Save();
    Report.Success($"Applied {applied} config change(s) to {configPath}"
        + (options.NoBaseline ? "" : " (incl. baseline)") + ".");
}
else
{
    Report.Info("No config changes needed.");
}

// 3b. Shader cache clear — default action on every --apply.
if (willClearCache)
    Report.CacheCleared(ShaderCacheCleaner.ClearAll());

Console.WriteLine();
Report.Info("Launch the game on the new settings — do not 'Apply' in the in-game menu, or it rewrites the config.");
return ExitOk;

static List<Poe2ConfigDoctor.Models.ConfigChange> Dedupe(IEnumerable<Poe2ConfigDoctor.Models.ConfigChange> changes) =>
    changes
        .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.Last())
        .ToList();

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
