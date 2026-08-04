using System.Text.Json;

namespace UniversalDeviceToolkit.Abstractions.Hardware;

/// <summary>
/// Loads the shared data-only device-pack catalog for portable clients.
/// </summary>
public static class DevicePackCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static IReadOnlyCollection<DevicePackDefinition> Load(string? catalogPath = null)
    {
        foreach (var path in CandidatePaths(catalogPath))
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                var packs = JsonSerializer.Deserialize<DevicePackDefinition[]>(File.ReadAllText(path), JsonOptions);
                if (packs is { Length: > 0 })
                    return packs;
            }
            catch
            {
                // A missing or invalid optional catalog degrades to generic basic mode.
            }
        }

        return [];
    }

    private static IEnumerable<string> CandidatePaths(string? catalogPath)
    {
        if (!string.IsNullOrWhiteSpace(catalogPath))
            yield return catalogPath;

        yield return Path.Combine(AppContext.BaseDirectory, "resources", "device-packs.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "resources", "device-packs.json");

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 8 && directory is not null; depth++, directory = directory.Parent)
            yield return Path.Combine(directory.FullName, "resources", "device-packs.json");
    }
}
