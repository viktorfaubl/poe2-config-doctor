namespace Poe2ConfigDoctor.Cli;

/// <summary>Parsed command-line options.</summary>
public sealed class Options
{
    public string? LogPath { get; private set; }
    public string? ConfigPath { get; private set; }

    /// <summary>When true, write changes to the config. Default is a safe dry run.</summary>
    public bool Apply { get; private set; }

    /// <summary>Scan only the last N lines of the log (0 = whole file).</summary>
    public int TailLines { get; private set; }

    /// <summary>Skip backing up the config before writing.</summary>
    public bool NoBackup { get; private set; }

    /// <summary>Apply even if the game appears to be running (it will clobber the file on exit).</summary>
    public bool Force { get; private set; }

    /// <summary>Skip applying the safe baseline preset (baseline is applied by default on --apply).</summary>
    public bool NoBaseline { get; private set; }

    /// <summary>Skip clearing the shader cache (it is cleared by default on --apply).</summary>
    public bool NoClearCache { get; private set; }

    /// <summary>Restore the config from a backup, then exit.</summary>
    public bool Restore { get; private set; }

    /// <summary>A specific backup to restore (filename or path); null = most recent.</summary>
    public string? RestoreTarget { get; private set; }

    /// <summary>List available config backups, then exit.</summary>
    public bool ListBackups { get; private set; }

    /// <summary>How far back to consider issues. Default: 3 days.</summary>
    public TimeSpan Since { get; private set; } = TimeSpan.FromDays(3);

    /// <summary>Consider the entire log, ignoring the time window.</summary>
    public bool AllHistory { get; private set; }

    /// <summary>Consider only the latest session (everything after the last Game Start).</summary>
    public bool SessionOnly { get; private set; }

    public bool ShowHelp { get; private set; }
    public string? Error { get; private set; }

    public static Options Parse(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help":
                    o.ShowHelp = true;
                    break;
                case "--apply":
                    o.Apply = true;
                    break;
                case "--no-backup":
                    o.NoBackup = true;
                    break;
                case "--no-baseline":
                    o.NoBaseline = true;
                    break;
                case "--no-clear-cache":
                    o.NoClearCache = true;
                    break;
                case "--restore":
                    o.Restore = true;
                    // Optional value: the next token, unless it's another flag.
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        o.RestoreTarget = args[++i];
                    break;
                case "--list-backups":
                    o.ListBackups = true;
                    break;
                case "--force":
                    o.Force = true;
                    break;
                case "--all":
                    o.AllHistory = true;
                    break;
                case "--session":
                    o.SessionOnly = true;
                    break;
                case "--since":
                    if (!TryNext(args, ref i, out var d) || !TryParseDuration(d, out var span))
                    { o.Error = "--since requires a duration like 3d, 72h, or 90m."; return o; }
                    o.Since = span;
                    break;
                case "--log":
                    if (!TryNext(args, ref i, out var lp)) { o.Error = "--log requires a path."; return o; }
                    o.LogPath = lp;
                    break;
                case "--config":
                    if (!TryNext(args, ref i, out var cp)) { o.Error = "--config requires a path."; return o; }
                    o.ConfigPath = cp;
                    break;
                case "--tail":
                    if (!TryNext(args, ref i, out var t) || !int.TryParse(t, out var n) || n < 0)
                    { o.Error = "--tail requires a non-negative integer."; return o; }
                    o.TailLines = n;
                    break;
                default:
                    o.Error = $"Unknown argument: {a}";
                    return o;
            }
        }
        return o;
    }

    private static bool TryNext(string[] args, ref int i, out string value)
    {
        if (i + 1 < args.Length)
        {
            value = args[++i];
            return true;
        }
        value = string.Empty;
        return false;
    }

    /// <summary>Parses "3d" / "72h" / "90m", or a bare number treated as days.</summary>
    public static bool TryParseDuration(string text, out TimeSpan span)
    {
        span = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        char unit = char.ToLowerInvariant(text[^1]);
        var hasUnit = unit is 'd' or 'h' or 'm';
        var number = hasUnit ? text[..^1] : text;

        if (!double.TryParse(number, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value) || value <= 0)
            return false;

        span = unit switch
        {
            'h' => TimeSpan.FromHours(value),
            'm' => TimeSpan.FromMinutes(value),
            _ => TimeSpan.FromDays(value), // 'd' or no unit
        };
        return true;
    }

    public static void PrintHelp()
    {
        Console.WriteLine(
"""
poe2doctor - analyzes Path of Exile 2 client logs and applies safe, explained config fixes.

USAGE:
  poe2doctor [options]

OPTIONS:
  --log <path>      Path to Client.txt        (auto-detected if omitted)
  --config <path>   Path to poe2_production_Config.ini (auto-detected if omitted)
  --since <dur>     Consider issues within this window: 3d, 72h, 90m (default: 3d)
  --all             Consider the entire log, ignoring the time window
  --session         Consider only the latest game session
  --apply           Write the proposed changes (default: dry run, shows changes only)
  --tail <N>        Scan only the last N lines of the log (default: whole file)
  --no-backup       Do not create a .bak before writing
  --no-baseline     Do not apply the safe baseline preset (it is applied by default)
  --no-clear-cache  Do not clear the shader cache (it is cleared by default)
  --list-backups    List available config backups, then exit
  --restore [file]  Restore the config from a backup (most recent, or the named
                    one), then exit
  --force           Apply even if the game appears to be running
  -h, --help        Show this help

By default the tool looks at issues from the last 3 days. It only reports what it
would change; re-run with --apply to write. On --apply it also applies the safe
baseline preset and clears the shader cache unless you pass --no-baseline /
--no-clear-cache. Close the game before applying: it overwrites the config on exit.
""");
    }
}
