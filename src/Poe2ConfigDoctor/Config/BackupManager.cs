using System.Globalization;
using System.Text.RegularExpressions;

namespace Poe2ConfigDoctor.Config;

/// <summary>A config backup on disk.</summary>
public sealed record BackupInfo(string Path, DateTime Timestamp);

/// <summary>
/// Timestamped config backups under a <c>poe2doctor-backups</c> folder next to the config.
/// Backups are never overwritten, so the original survives any number of applies. Restore copies the
/// most recent backup back over the config (snapshotting the current file first, so restore is undoable).
/// </summary>
public static class BackupManager
{
    private const string DefaultSuffix = ".bak";
    private const string PreRestoreSuffix = ".prerestore";

    public static string BackupDir(string configPath) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configPath))!, "poe2doctor-backups");

    /// <summary>Copies the config to a timestamped backup and returns it. <paramref name="now"/> stamps the name.</summary>
    public static BackupInfo Create(string configPath, DateTime now, bool preRestore = false)
    {
        var dir = BackupDir(configPath);
        Directory.CreateDirectory(dir);

        var name = Path.GetFileNameWithoutExtension(configPath);
        var ext = Path.GetExtension(configPath);
        var suffix = preRestore ? PreRestoreSuffix : DefaultSuffix;
        var stamp = now.ToString("yyyyMMdd-HHmmss");

        var dest = Path.Combine(dir, $"{name}.{stamp}{ext}{suffix}");
        for (int i = 1; File.Exists(dest); i++) // avoid same-second collisions
            dest = Path.Combine(dir, $"{name}.{stamp}-{i}{ext}{suffix}");

        File.Copy(configPath, dest);
        return new BackupInfo(dest, now);
    }

    /// <summary>All restorable backups for this config, newest first. Pre-restore snapshots are excluded.</summary>
    public static IReadOnlyList<BackupInfo> List(string configPath)
    {
        var dir = BackupDir(configPath);
        if (!Directory.Exists(dir))
            return Array.Empty<BackupInfo>();

        var name = Path.GetFileNameWithoutExtension(configPath);
        var ext = Path.GetExtension(configPath);
        return Directory.EnumerateFiles(dir, $"{name}.*{ext}{DefaultSuffix}")
            .Select(f => new BackupInfo(f, StampOf(f)))
            .OrderByDescending(b => b.Timestamp)
            .ThenByDescending(b => b.Path, StringComparer.Ordinal)
            .ToList();
    }

    public static BackupInfo? Latest(string configPath) => List(configPath).FirstOrDefault();

    /// <summary>
    /// Resolves a restore target given as a full path, a bare filename in the backup folder, or a
    /// filename match among the listed backups. Returns null if nothing matches.
    /// </summary>
    public static BackupInfo? Resolve(string configPath, string target)
    {
        if (File.Exists(target))
            return new BackupInfo(Path.GetFullPath(target), StampOf(target));

        var inDir = Path.Combine(BackupDir(configPath), target);
        if (File.Exists(inDir))
            return new BackupInfo(inDir, StampOf(inDir));

        return List(configPath)
            .FirstOrDefault(b => Path.GetFileName(b.Path).Equals(target, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The creation time encoded in the backup's filename, falling back to its last-write time.</summary>
    private static DateTime StampOf(string path)
    {
        var m = StampRegex.Match(Path.GetFileName(path));
        if (m.Success && DateTime.TryParseExact(m.Value, "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        return File.GetLastWriteTime(path);
    }

    private static readonly Regex StampRegex = new(@"\d{8}-\d{6}", RegexOptions.Compiled);
}
