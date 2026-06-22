namespace Poe2ConfigDoctor.Maintenance;

/// <summary>Result of clearing one shader-cache directory.</summary>
public sealed record CacheClearResult(string Path, int FilesDeleted, long BytesFreed);

/// <summary>
/// Clears PoE2's on-disk shader caches under %APPDATA%\Path of Exile 2. Safe: the game rebuilds them
/// on next launch. Recommended after patches/driver updates (stale-cache stutter) and run by default
/// on --apply. Only deletes cache files; never touches the game process.
/// </summary>
public static class ShaderCacheCleaner
{
    private static readonly string[] CacheFolderNames =
        { "ShaderCacheVulkan", "ShaderCacheD3D12", "ShaderCacheD3D11" };

    public static IReadOnlyList<string> CacheDirs()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(appData, "Path of Exile 2");
        return CacheFolderNames
            .Select(name => Path.Combine(baseDir, name))
            .Where(Directory.Exists)
            .ToList();
    }

    public static List<CacheClearResult> ClearAll()
    {
        var results = new List<CacheClearResult>();
        foreach (var dir in CacheDirs())
        {
            int files = 0;
            long bytes = 0;
            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    bytes += new FileInfo(file).Length;
                    File.Delete(file);
                    files++;
                }
                catch
                {
                    // Skip files locked by another process; the rest still clear.
                }
            }
            results.Add(new CacheClearResult(dir, files, bytes));
        }
        return results;
    }
}
