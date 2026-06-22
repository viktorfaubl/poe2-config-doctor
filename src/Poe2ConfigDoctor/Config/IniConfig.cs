namespace Poe2ConfigDoctor.Config;

/// <summary>
/// A minimal, section-aware, line-preserving INI editor for the PoE2 config.
/// Reads every line verbatim and only rewrites the single line whose value changes,
/// so comments, ordering and unrelated keys are untouched. PoE2 reuses key names across
/// sections (e.g. keybind blocks), hence all access is scoped to a section.
/// </summary>
public sealed class IniConfig
{
    private readonly List<string> _lines;

    public string Path { get; }

    private IniConfig(string path, IEnumerable<string> lines)
    {
        Path = path;
        _lines = lines.ToList();
    }

    public static IniConfig Load(string path) => new(path, File.ReadAllLines(path));

    /// <summary>Returns the trimmed value for <paramref name="key"/> within <paramref name="section"/>, or null.</summary>
    public string? Get(string section, string key)
    {
        string? current = null;
        foreach (var line in _lines)
        {
            if (TryGetSection(line, out var s)) { current = s; continue; }
            if (!SectionMatches(current, section)) continue;
            if (TrySplit(line, out var k, out var v) && k.Equals(key, StringComparison.OrdinalIgnoreCase))
                return v;
        }
        return null;
    }

    /// <summary>Sets the value for an existing key within a section. Returns false if the key was not found.</summary>
    public bool Set(string section, string key, string value)
    {
        string? current = null;
        for (int i = 0; i < _lines.Count; i++)
        {
            if (TryGetSection(_lines[i], out var s)) { current = s; continue; }
            if (!SectionMatches(current, section)) continue;
            if (TrySplit(_lines[i], out var k, out _) && k.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                _lines[i] = $"{key}={value}";
                return true;
            }
        }
        return false;
    }

    public void Save() => File.WriteAllLines(Path, _lines);

    private static bool TryGetSection(string line, out string section)
    {
        var t = line.Trim();
        if (t.Length >= 2 && t[0] == '[' && t[^1] == ']')
        {
            section = t[1..^1];
            return true;
        }
        section = string.Empty;
        return false;
    }

    private static bool SectionMatches(string? current, string wanted) =>
        current is not null && current.Equals(wanted, StringComparison.OrdinalIgnoreCase);

    private static bool TrySplit(string line, out string key, out string value)
    {
        int eq = line.IndexOf('=');
        if (eq > 0)
        {
            key = line[..eq].Trim();
            value = line[(eq + 1)..].Trim();
            return true;
        }
        key = value = string.Empty;
        return false;
    }
}
