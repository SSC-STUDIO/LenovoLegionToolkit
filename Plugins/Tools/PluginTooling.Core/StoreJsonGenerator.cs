using System.IO.Compression;
using System.Security.Cryptography;

namespace PluginTooling.Core;

public sealed class StoreJsonGenerator
{
    private readonly PluginRepository _repository = new();

    public StoreDocument Generate(StoreGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var selectedPluginIds = request.PluginIds.Count == 0
            ? repository.Plugins.Values.Where(HasStoreMetadata).Select(plugin => plugin.Manifest.Id).ToArray()
            : _repository.ResolveTargetPluginIds(repository, request.PluginIds);

        var assetRoot = request.AssetRoot is null
            ? Path.Combine(repository.RootPath, "Build", "release-assets")
            : Path.GetFullPath(request.AssetRoot);

        var releaseDate = request.ReleaseDate ?? DateTimeOffset.UtcNow;
        var existingEntries = repository.StoreDocument?.Plugins.ToDictionary(
            entry => entry.Id,
            entry => entry,
            StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, StorePluginEntry>(StringComparer.OrdinalIgnoreCase);

        var store = new StoreDocument
        {
            LastUpdated = releaseDate.ToString("O"),
            StoreVersion = repository.StoreDocument?.StoreVersion ?? "1.0.0",
            Plugins = [],
        };

        if (request.MergeExisting && repository.StoreDocument is not null)
        {
            store.LastUpdated = repository.StoreDocument.LastUpdated;
            store.StoreVersion = string.IsNullOrWhiteSpace(repository.StoreDocument.StoreVersion)
                ? store.StoreVersion
                : repository.StoreDocument.StoreVersion;
            store.Plugins.AddRange(repository.StoreDocument.Plugins.Select(Clone));
        }

        var storeContentChanged = false;

        foreach (var pluginId in selectedPluginIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            var plugin = repository.Plugins[pluginId];
            if (!HasStoreMetadata(plugin))
            {
                continue;
            }

            var storeMetadata = plugin.UnifiedManifest.Store;
            var tagName = $"{plugin.Manifest.Id}-v{plugin.Manifest.Version}";
            var assetName = $"{tagName}.zip";
            var assetPath = Path.Combine(assetRoot, assetName);
            var assetExists = File.Exists(assetPath);
            if (request.RequireAssets && !assetExists)
            {
                throw new FileNotFoundException($"Release asset is required for store generation but was not found: {assetPath}", assetPath);
            }

            var generatedEntry = new StorePluginEntry
            {
                Id = plugin.Manifest.Id,
                Name = plugin.Manifest.Name,
                Description = storeMetadata.Description,
                Author = plugin.Manifest.Author,
                Version = plugin.Manifest.Version,
                MinLltVersion = plugin.Manifest.MinLltVersion,
                IsSystemPlugin = plugin.Manifest.IsSystemPlugin,
                DownloadUrl = $"{request.ReleaseRepositoryUrl}/download/{tagName}/{assetName}",
                Changelog = $"{request.ReleaseRepositoryUrl}/tag/{tagName}",
                FileSize = assetExists ? new FileInfo(assetPath).Length : 0,
                ReleaseDate = releaseDate.ToString("O"),
                RepositoryUrl = storeMetadata.RepositoryUrl ?? plugin.Manifest.Repository,
                SupportedLanguages = storeMetadata.SupportedLanguages.ToList(),
                Icon = storeMetadata.Icon,
                IconBackground = storeMetadata.IconBackground,
                Dependencies = storeMetadata.Dependencies.ToList(),
                Tags = storeMetadata.Tags.ToList(),
                Status = ResolveLifecycleStatus(plugin),
            };

            var hasExistingEntry = existingEntries.TryGetValue(plugin.Manifest.Id, out var existingEntry);
            ApplyLocalizationFields(generatedEntry, plugin.UnifiedManifest, existingEntry);
            if (request.MergeExisting &&
                hasExistingEntry &&
                existingEntry is not null &&
                string.Equals(existingEntry.Version, plugin.Manifest.Version, StringComparison.OrdinalIgnoreCase) &&
                existingEntry.FileSize > 0 &&
                existingEntry.FileSize == generatedEntry.FileSize)
            {
                generatedEntry.ReleaseDate = existingEntry.ReleaseDate;
                generatedEntry.DownloadUrl = existingEntry.DownloadUrl;
                generatedEntry.Changelog = existingEntry.Changelog;
            }

            // Prefer hashes computed from the release ZIP; fall back to existing store values
            // only when the asset is absent (e.g. regenerate metadata without re-download).
            if (assetExists)
            {
                ApplyIntegrityHashesFromAsset(generatedEntry, assetPath, plugin);
            }
            else if (request.MergeExisting && existingEntry is not null)
            {
                PreserveIntegrityFields(generatedEntry, existingEntry);
            }

            if (request.RequireAssets &&
                (string.IsNullOrWhiteSpace(generatedEntry.ZipHash) || string.IsNullOrWhiteSpace(generatedEntry.FileHash)))
            {
                throw new InvalidOperationException(
                    $"Release asset for '{plugin.Manifest.Id}' is present but integrity hashes could not be computed " +
                    $"(zipHash='{generatedEntry.ZipHash}', fileHash='{generatedEntry.FileHash}'). " +
                    "Ensure the package ZIP contains the main plugin DLL.");
            }

            var priorEntry = store.Plugins.FirstOrDefault(entry =>
                string.Equals(entry.Id, generatedEntry.Id, StringComparison.OrdinalIgnoreCase));
            ReplaceOrAdd(store.Plugins, generatedEntry);

            if (!request.MergeExisting || !hasExistingEntry || existingEntry is null || !EntriesEqual(existingEntry, generatedEntry))
            {
                store.LastUpdated = releaseDate.ToString("O");
                storeContentChanged = true;
            }
            else if (priorEntry is not null && !EntriesEqual(priorEntry, generatedEntry))
            {
                storeContentChanged = true;
            }
        }

        store.Plugins = store.Plugins
            .OrderBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (storeContentChanged)
        {
            store.StoreVersion = BumpStoreVersion(store.StoreVersion);
        }

        return store;
    }

    public string Write(StoreGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var outputPath = request.OutputPath is null
            ? Path.Combine(repository.RootPath, "store.json")
            : Path.GetFullPath(request.OutputPath);

        var store = Generate(request);
        PluginRepository.WriteJsonFile(outputPath, store);
        return outputPath;
    }

    public StoreCheckResult Check(StoreGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var storePath = request.OutputPath is null
            ? Path.Combine(repository.RootPath, "store.json")
            : Path.GetFullPath(request.OutputPath);

        if (!File.Exists(storePath))
        {
            return new StoreCheckResult(storePath, false, $"Store file not found: {storePath}");
        }

        var generated = Generate(request);
        var expected = PluginRepository.ToJson(generated);
        var current = File.ReadAllText(storePath);

        var matches = NormalizeForComparison(current) == NormalizeForComparison(expected);
        return matches
            ? new StoreCheckResult(storePath, true, "store.json matches generator output.")
            : new StoreCheckResult(storePath, false, "store.json differs from generator output. Re-run generate-store with the same arguments to update it.");
    }

    private static string NormalizeForComparison(string value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] == '\uFEFF')
        {
            value = value[1..];
        }

        return PluginRepository.NormalizeLineEndings(value).TrimEnd();
    }

    private static bool HasStoreMetadata(PluginContext plugin)
    {
        var store = plugin.UnifiedManifest.Store;
        return !string.IsNullOrWhiteSpace(store.Description) &&
               !string.IsNullOrWhiteSpace(store.Icon) &&
               !string.IsNullOrWhiteSpace(store.IconBackground) &&
               store.Tags.Count > 0 &&
               store.SupportedLanguages.Count > 0;
    }

    private static void ReplaceOrAdd(List<StorePluginEntry> entries, StorePluginEntry entry)
    {
        var index = entries.FindIndex(existing => string.Equals(existing.Id, entry.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            entries[index] = entry;
        }
        else
        {
            entries.Add(entry);
        }
    }

    private static StorePluginEntry Clone(StorePluginEntry entry)
    {
        return new StorePluginEntry
        {
            Id = entry.Id,
            Name = entry.Name,
            Description = entry.Description,
            LocalizedNames = CloneStringDictionary(entry.LocalizedNames),
            LocalizedDescriptions = CloneStringDictionary(entry.LocalizedDescriptions),
            LocalizedTags = CloneTagDictionary(entry.LocalizedTags),
            Author = entry.Author,
            Version = entry.Version,
            MinLltVersion = entry.MinLltVersion,
            IsSystemPlugin = entry.IsSystemPlugin,
            DownloadUrl = entry.DownloadUrl,
            Changelog = entry.Changelog,
            FileSize = entry.FileSize,
            FileHash = entry.FileHash,
            ZipHash = entry.ZipHash,
            ReleaseDate = entry.ReleaseDate,
            RepositoryUrl = entry.RepositoryUrl,
            SupportedLanguages = (entry.SupportedLanguages ?? []).ToList(),
            Icon = entry.Icon,
            IconBackground = entry.IconBackground,
            Dependencies = (entry.Dependencies ?? []).ToList(),
            Tags = (entry.Tags ?? []).ToList(),
            Status = entry.Status,
        };
    }

    private static bool EntriesEqual(StorePluginEntry left, StorePluginEntry right)
    {
        return string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
               string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
               string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
               StringDictionariesEqual(left.LocalizedNames, right.LocalizedNames) &&
               StringDictionariesEqual(left.LocalizedDescriptions, right.LocalizedDescriptions) &&
               TagDictionariesEqual(left.LocalizedTags, right.LocalizedTags) &&
               string.Equals(left.Author, right.Author, StringComparison.Ordinal) &&
               string.Equals(left.Version, right.Version, StringComparison.Ordinal) &&
               string.Equals(left.MinLltVersion, right.MinLltVersion, StringComparison.Ordinal) &&
               left.IsSystemPlugin == right.IsSystemPlugin &&
               string.Equals(left.DownloadUrl, right.DownloadUrl, StringComparison.Ordinal) &&
               string.Equals(left.Changelog, right.Changelog, StringComparison.Ordinal) &&
               left.FileSize == right.FileSize &&
               string.Equals(left.FileHash, right.FileHash, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.ZipHash, right.ZipHash, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.ReleaseDate, right.ReleaseDate, StringComparison.Ordinal) &&
               string.Equals(left.RepositoryUrl, right.RepositoryUrl, StringComparison.Ordinal) &&
               (left.SupportedLanguages ?? []).SequenceEqual(right.SupportedLanguages ?? [], StringComparer.Ordinal) &&
               string.Equals(left.Icon, right.Icon, StringComparison.Ordinal) &&
               string.Equals(left.IconBackground, right.IconBackground, StringComparison.Ordinal) &&
               (left.Dependencies ?? []).SequenceEqual(right.Dependencies ?? [], StringComparer.Ordinal) &&
               (left.Tags ?? []).SequenceEqual(right.Tags ?? [], StringComparer.Ordinal) &&
               string.Equals(left.Status, right.Status, StringComparison.Ordinal);
    }

    private static void ApplyLocalizationFields(
        StorePluginEntry entry,
        UnifiedPluginManifest manifest,
        StorePluginEntry? existingEntry)
    {
        entry.LocalizedNames = ResolveLocalizedStrings(
            manifest.LocalizedNames ?? [],
            existingEntry?.LocalizedNames,
            entry.Name);

        entry.LocalizedDescriptions = ResolveLocalizedStrings(
            manifest.LocalizedDescriptions ?? [],
            existingEntry?.LocalizedDescriptions,
            entry.Description);

        entry.LocalizedTags = ResolveLocalizedTags(
            manifest.LocalizedTags ?? [],
            existingEntry?.LocalizedTags,
            entry.Tags);
    }

    private static void PreserveIntegrityFields(StorePluginEntry generated, StorePluginEntry existing)
    {
        if (string.IsNullOrWhiteSpace(generated.FileHash) && !string.IsNullOrWhiteSpace(existing.FileHash))
            generated.FileHash = existing.FileHash;

        if (string.IsNullOrWhiteSpace(generated.ZipHash) && !string.IsNullOrWhiteSpace(existing.ZipHash))
            generated.ZipHash = existing.ZipHash;
    }

    /// <summary>
    /// Fills <see cref="StorePluginEntry.ZipHash"/> (ZIP SHA-256) and
    /// <see cref="StorePluginEntry.FileHash"/> (main plugin DLL SHA-256 inside the ZIP).
    /// Host verifies ZIP before extract and DLL before load.
    /// </summary>
    internal static void ApplyIntegrityHashesFromAsset(
        StorePluginEntry entry,
        string assetPath,
        PluginContext plugin)
    {
        entry.ZipHash = ComputeSha256Hex(assetPath);
        entry.FileHash = TryComputeMainDllHashFromZip(assetPath, plugin) ?? string.Empty;
    }

    internal static string ComputeSha256Hex(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static string? TryComputeMainDllHashFromZip(string zipPath, PluginContext plugin)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{plugin.ExpectedAssemblyName}.dll",
            $"UniversalDeviceToolkit.Plugins.{plugin.FolderName}.dll",
            $"LenovoLegionToolkit.Plugins.{plugin.FolderName}.dll",
        };

        // Hyphenated id → Pascal folder variants sometimes differ; also try plugin id forms.
        var noHyphen = plugin.Manifest.Id.Replace("-", "", StringComparison.Ordinal);
        candidates.Add($"UniversalDeviceToolkit.Plugins.{noHyphen}.dll");
        candidates.Add($"LenovoLegionToolkit.Plugins.{noHyphen}.dll");

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var fileName = Path.GetFileName(entry.FullName);
                if (!candidates.Contains(fileName))
                    continue;

                using var entryStream = entry.Open();
                using var memory = new MemoryStream();
                entryStream.CopyTo(memory);
                var hash = SHA256.HashData(memory.ToArray());
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }

        return null;
    }

    private static Dictionary<string, string> ResolveLocalizedStrings(
        Dictionary<string, string> manifestValues,
        Dictionary<string, string>? existingValues,
        string fallback)
    {
        if (manifestValues.Count > 0)
            return CloneStringDictionary(manifestValues);

        if (existingValues is { Count: > 0 })
            return CloneStringDictionary(existingValues);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = fallback,
        };
    }

    private static Dictionary<string, List<string>> ResolveLocalizedTags(
        Dictionary<string, List<string>> manifestValues,
        Dictionary<string, List<string>>? existingValues,
        IReadOnlyList<string> fallback)
    {
        if (manifestValues.Count > 0)
            return CloneTagDictionary(manifestValues);

        if (existingValues is { Count: > 0 })
            return CloneTagDictionary(existingValues);

        return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = fallback.ToList(),
        };
    }

    private static Dictionary<string, string> CloneStringDictionary(Dictionary<string, string>? source) =>
        (source ?? []).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, List<string>> CloneTagDictionary(Dictionary<string, List<string>>? source) =>
        (source ?? []).ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToList(),
            StringComparer.OrdinalIgnoreCase);

    private static bool StringDictionariesEqual(
        Dictionary<string, string>? left,
        Dictionary<string, string>? right)
    {
        left ??= [];
        right ??= [];
        if (left.Count != right.Count)
            return false;

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var rightValue) ||
                !string.Equals(pair.Value, rightValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TagDictionariesEqual(
        Dictionary<string, List<string>>? left,
        Dictionary<string, List<string>>? right)
    {
        left ??= [];
        right ??= [];
        if (left.Count != right.Count)
            return false;

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var rightValue) ||
                !(pair.Value ?? []).SequenceEqual(rightValue ?? [], StringComparer.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string BumpStoreVersion(string current)
    {
        if (Version.TryParse(NormalizeStoreVersion(current), out var version))
        {
            var build = Math.Max(version.Build, 0);
            var bumped = new Version(version.Major, version.Minor, build + 1);
            return bumped.ToString(3);
        }

        return "1.0.0";
    }

    private static string NormalizeStoreVersion(string value)
    {
        var normalized = value.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
            normalized = normalized[1..];

        return normalized;
    }

    private static string ResolveLifecycleStatus(PluginContext plugin)
    {
        var explicitLifecycle = plugin.UnifiedManifest.Lifecycle;
        if (!string.IsNullOrWhiteSpace(explicitLifecycle) &&
            !string.Equals(explicitLifecycle, PluginLifecycleStatus.Active, StringComparison.OrdinalIgnoreCase))
        {
            return explicitLifecycle;
        }

        var manifestName = plugin.Manifest.Name ?? string.Empty;
        var description = plugin.UnifiedManifest.Store?.Description ?? string.Empty;

        if (manifestName.Contains("(Migrated)", StringComparison.OrdinalIgnoreCase) ||
            description.StartsWith("Deprecated:", StringComparison.OrdinalIgnoreCase))
        {
            return PluginLifecycleStatus.Migrated;
        }

        return PluginLifecycleStatus.Active;
    }
}
