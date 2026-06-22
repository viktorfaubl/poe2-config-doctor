namespace Poe2ConfigDoctor.Cli;

/// <summary>Best-effort auto-discovery of the PoE2 config file and client log.</summary>
public static class Locator
{
    public static string? FindConfig()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var candidate = Path.Combine(docs, "My Games", "Path of Exile 2", "poe2_production_Config.ini");
        return File.Exists(candidate) ? candidate : null;
    }

    public static string? FindLog()
    {
        var candidates = new List<string>
        {
            @"E:\Games\PoE2\logs\Client.txt",
            @"C:\Program Files (x86)\Grinding Gear Games\Path of Exile 2\logs\Client.txt",
        };

        // Common per-drive layouts: <drive>\Games\PoE2 and Steam libraries.
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            candidates.Add(Path.Combine(drive.Name, "Games", "PoE2", "logs", "Client.txt"));
            candidates.Add(Path.Combine(drive.Name, "Games", "Path of Exile 2", "logs", "Client.txt"));
            candidates.Add(Path.Combine(drive.Name, "SteamLibrary", "steamapps", "common", "Path of Exile 2", "logs", "Client.txt"));
            candidates.Add(Path.Combine(drive.Name, "Program Files (x86)", "Steam", "steamapps", "common", "Path of Exile 2", "logs", "Client.txt"));
        }

        return candidates.FirstOrDefault(File.Exists);
    }
}
