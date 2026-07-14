using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal sealed record PluginDiscoveryReport(
    string Source,
    string[] SearchRoots,
    PluginDescriptor[] Plugins,
    string[] Notes)
{
    public static PluginDiscoveryReport Unknown(string source, params string[] notes) =>
        new(source, [], [], notes);
}

internal sealed record PluginDescriptor(
    string Id,
    string Name,
    string Version,
    string ManifestPath,
    bool IsCrossPlatformCandidate,
    bool HasRuntimeContribution,
    int OptimizationActionCount,
    string[] TargetPlatforms,
    string Reason);

internal sealed class PluginDiscoveryReader(
    IFileSystem fileSystem,
    string? explicitPluginsRoot = null)
{
    private static readonly string[] ManifestFileNames = ["plugin.manifest.json", "plugin.json", "Plugin.json"];

    public PluginDiscoveryReport Read()
    {
        var roots = ResolveSearchRoots(explicitPluginsRoot)
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var plugins = new List<PluginDescriptor>();
        foreach (var root in roots)
        {
            if (!fileSystem.DirectoryExists(root))
                continue;

            foreach (var pluginDirectory in fileSystem.EnumerateDirectories(root).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var descriptor = TryReadPlugin(pluginDirectory);
                if (descriptor is not null)
                    plugins.Add(descriptor);
            }
        }

        string[] notes = plugins.Count == 0
            ? new[] { "No plugin manifests were found. The cross-platform CLI only inspects manifests and does not load WPF or Windows-only plugin assemblies." }
            : ["Plugin assemblies are not loaded by this CLI; manifest data is used to identify cross-platform candidates safely."];

        return new PluginDiscoveryReport(
            "cross-platform-plugin-manifest",
            roots,
            plugins
                .GroupBy(plugin => plugin.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(plugin => plugin.IsCrossPlatformCandidate).First())
                .OrderBy(plugin => plugin.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            notes);
    }

    private PluginDescriptor? TryReadPlugin(string pluginDirectory)
    {
        foreach (var manifestPath in ManifestFileNames.Select(fileName => CombinePath(pluginDirectory, fileName)))
        {
            var text = fileSystem.ReadAllText(manifestPath);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            try
            {
                var manifest = JsonSerializer.Deserialize<CrossPlatformPluginManifest>(text, JsonOptions);
                if (manifest is null)
                    continue;

                var id = FirstPresent(manifest.Id, Path.GetFileName(pluginDirectory));
                if (string.IsNullOrWhiteSpace(id) || !IsValidPluginId(id))
                    return new PluginDescriptor(id, manifest.Name, manifest.Version, manifestPath, false, false, 0, [], "Manifest id is missing or invalid.");

                var targets = NormalizePlatforms(manifest.TargetPlatforms, manifest.SupportedPlatforms, manifest.Platforms);
                var hasRuntime = !string.IsNullOrWhiteSpace(manifest.Contributes?.Runtime?.Class);
                var optimizationCount = manifest.Contributes?.OptimizationActions?.Count ?? 0;
                var crossPlatform = IsCrossPlatformCandidate(targets, hasRuntime, optimizationCount);
                var reason = crossPlatform
                    ? BuildCandidateReason(targets, hasRuntime, optimizationCount)
                    : "Manifest has no non-Windows target platform, runtime contribution, or manifest-defined optimization action.";

                return new PluginDescriptor(
                    id,
                    FirstPresent(manifest.Name, id),
                    FirstPresent(manifest.Version, "unknown"),
                    manifestPath,
                    crossPlatform,
                    hasRuntime,
                    optimizationCount,
                    targets,
                    reason);
            }
            catch (JsonException ex)
            {
                return new PluginDescriptor(
                    Path.GetFileName(pluginDirectory),
                    Path.GetFileName(pluginDirectory),
                    "unknown",
                    manifestPath,
                    false,
                    false,
                    0,
                    [],
                    $"Manifest could not be parsed: {ex.Message}");
            }
        }

        return null;
    }

    private static bool IsCrossPlatformCandidate(string[] targetPlatforms, bool hasRuntime, int optimizationActionCount)
    {
        if (targetPlatforms.Any(platform =>
                platform.Equals("linux", StringComparison.OrdinalIgnoreCase) ||
                platform.Equals("macos", StringComparison.OrdinalIgnoreCase) ||
                platform.Equals("osx", StringComparison.OrdinalIgnoreCase) ||
                platform.Equals("unix", StringComparison.OrdinalIgnoreCase) ||
                platform.Equals("any", StringComparison.OrdinalIgnoreCase) ||
                platform.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                platform.Equals("cross-platform", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return hasRuntime || optimizationActionCount > 0;
    }

    private static string BuildCandidateReason(string[] targetPlatforms, bool hasRuntime, int optimizationActionCount)
    {
        var reasons = new List<string>();
        if (targetPlatforms.Length > 0)
            reasons.Add($"targets {string.Join(", ", targetPlatforms)}");
        if (hasRuntime)
            reasons.Add("declares runtime contribution");
        if (optimizationActionCount > 0)
            reasons.Add($"declares {optimizationActionCount} optimization actions");

        return string.Join("; ", reasons);
    }

    private static string[] NormalizePlatforms(params string[]?[] values) =>
        values
            .Where(value => value is not null)
            .SelectMany(value => value!)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Regex.Replace(value.ToLowerInvariant(), @"[\s_]+", "-"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string[] ResolveSearchRoots(string? explicitRoot)
    {
        var roots = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitRoot))
            return [Path.GetFullPath(explicitRoot)];

        var envRoot = Environment.GetEnvironmentVariable("UDT_PLUGINS_DIR");
        if (!string.IsNullOrWhiteSpace(envRoot))
            roots.Add(envRoot);

        roots.Add(Path.Combine(AppContext.BaseDirectory, "plugins"));
        roots.Add(Path.Combine(AppContext.BaseDirectory, "Build", "plugins"));
        roots.Add(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Build", "plugins"));
        roots.Add(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Build", "plugins"));

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
            roots.Add(Path.Combine(appData, "UniversalDeviceToolkit", "plugins"));

        return roots.Select(path => Path.GetFullPath(path)).ToArray();
    }

    private static string CombinePath(string directory, string fileName) =>
        $"{directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}{Path.DirectorySeparatorChar}{fileName}";

    private static string FirstPresent(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static bool IsValidPluginId(string value) =>
        Regex.IsMatch(value, @"^[a-zA-Z0-9][a-zA-Z0-9._-]{0,127}$");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}

internal sealed class CrossPlatformPluginManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("targetPlatforms")]
    public string[]? TargetPlatforms { get; set; }

    [JsonPropertyName("supportedPlatforms")]
    public string[]? SupportedPlatforms { get; set; }

    [JsonPropertyName("platforms")]
    public string[]? Platforms { get; set; }

    [JsonPropertyName("contributes")]
    public CrossPlatformPluginContributions? Contributes { get; set; }
}

internal sealed class CrossPlatformPluginContributions
{
    [JsonPropertyName("runtime")]
    public CrossPlatformPluginRuntimeContribution? Runtime { get; set; }

    [JsonPropertyName("optimizationActions")]
    public List<CrossPlatformPluginOptimizationContribution>? OptimizationActions { get; set; }
}

internal sealed class CrossPlatformPluginRuntimeContribution
{
    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;
}

// Deserialization target for contributes.optimizationActions; only list length is used.
internal sealed class CrossPlatformPluginOptimizationContribution
{
}
