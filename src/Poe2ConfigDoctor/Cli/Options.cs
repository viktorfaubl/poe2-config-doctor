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
                case "--force":
                    o.Force = true;
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
  --apply           Write the proposed changes (default: dry run, shows changes only)
  --tail <N>        Scan only the last N lines of the log (default: whole file)
  --no-backup       Do not create a .bak before writing
  --force           Apply even if the game appears to be running
  -h, --help        Show this help

By default the tool only reports what it would change. Re-run with --apply to write.
Close the game before applying: it overwrites the config on exit.
""");
    }
}
