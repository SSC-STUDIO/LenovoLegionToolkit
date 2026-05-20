using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.WPF.Utils;

public class LanguagePackManager(OnlineResourceCatalogClient resourceCatalogClient)
{
    private const string AssetPrefix = AppIdentity.CompactName;
    private static readonly string PendingUninstallPath = Path.Combine(
        Folders.AppData,
        "pending_language_uninstall.txt");
    private static readonly CultureInfo EnglishCulture = new("en");

    public bool IsEnglish(CultureInfo cultureInfo) =>
        cultureInfo.Name.Equals(EnglishCulture.Name, StringComparison.OrdinalIgnoreCase);

    public bool IsInstalled(CultureInfo cultureInfo)
    {
        if (IsEnglish(cultureInfo))
            return true;

        return GetResourceDirectoryNames(cultureInfo)
            .Select(directoryName => Path.Combine(AppContext.BaseDirectory, directoryName))
            .Any(directory => Directory.Exists(directory) &&
                              Directory.EnumerateFiles(directory, "*.resources.dll", SearchOption.TopDirectoryOnly).Any());
    }

    public string GetInstallUrl(CultureInfo cultureInfo)
    {
        var version = GetCurrentVersion();
        return $"{AppIdentity.ResourcesBaseUrl}/{version}/languages/{NormalizeAssetCultureName(cultureInfo)}.zip";
    }

    public async Task InstallAsync(CultureInfo cultureInfo, IProgress<float>? progress = null, CancellationToken token = default)
    {
        if (IsEnglish(cultureInfo))
            return;

        var tempRoot = Path.Combine(Path.GetTempPath(), $"{AssetPrefix}-lang-{cultureInfo.Name}-{Guid.NewGuid():N}");
        var tempZipPath = Path.Combine(tempRoot, "language.zip");
        var extractPath = Path.Combine(tempRoot, "extract");

        try
        {
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(extractPath);

            var languageResource = await GetLanguageResourceAsync(cultureInfo, token).ConfigureAwait(false);
            await resourceCatalogClient.DownloadAndVerifyAsync(languageResource.Url, languageResource.Sha256, tempZipPath, progress, token).ConfigureAwait(false);

            ExtractZipSafely(tempZipPath, extractPath);
            CopyLanguageDirectories(extractPath, cultureInfo);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    public void Uninstall(CultureInfo cultureInfo)
    {
        if (IsEnglish(cultureInfo))
            return;

        foreach (var directoryName in GetResourceDirectoryNames(cultureInfo))
        {
            var directory = Path.Combine(AppContext.BaseDirectory, directoryName);
            TryDeleteDirectory(directory);
        }
    }

    public void QueueUninstall(CultureInfo cultureInfo)
    {
        if (IsEnglish(cultureInfo))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(PendingUninstallPath)!);
        File.WriteAllLines(PendingUninstallPath, GetResourceDirectoryNames(cultureInfo));
    }

    public void ProcessPendingUninstall()
    {
        if (!File.Exists(PendingUninstallPath))
            return;

        try
        {
            var directoryNames = File.ReadAllLines(PendingUninstallPath)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var directoryName in directoryNames)
            {
                var directory = Path.Combine(AppContext.BaseDirectory, directoryName);
                TryDeleteDirectory(directory);
            }

            File.Delete(PendingUninstallPath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to process pending language uninstall.", ex);
        }
    }

    public static string GetLanguagePackAssetName(string version, CultureInfo cultureInfo) =>
        $"{AssetPrefix}_v{version}_lang_{NormalizeAssetCultureName(cultureInfo)}.zip";

    public static string NormalizeAssetCultureName(CultureInfo cultureInfo) =>
        cultureInfo.Name.ToLowerInvariant();

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version ?? Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null || version.Major < 0 || version.Minor < 0 || version.Build < 0)
            return "0.0.0";

        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private async Task<OnlineLanguageResource> GetLanguageResourceAsync(CultureInfo cultureInfo, CancellationToken token)
    {
        var catalog = await resourceCatalogClient.GetCatalogAsync(token).ConfigureAwait(false);
        var normalizedCulture = NormalizeAssetCultureName(cultureInfo);

        var resource = catalog.Languages.FirstOrDefault(language =>
            language.Culture.Equals(normalizedCulture, StringComparison.OrdinalIgnoreCase) ||
            language.Culture.Equals(cultureInfo.Name, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
            throw new InvalidDataException($"Language '{cultureInfo.Name}' is not available in the online resource catalog.");

        if (string.IsNullOrWhiteSpace(resource.Url))
            throw new InvalidDataException($"Language '{cultureInfo.Name}' has an empty download URL.");

        if (string.IsNullOrWhiteSpace(resource.Sha256))
            throw new InvalidDataException($"Language '{cultureInfo.Name}' is missing SHA256 metadata.");

        return resource;
    }

    private static void CopyLanguageDirectories(string extractPath, CultureInfo cultureInfo)
    {
        var expectedDirectoryNames = GetResourceDirectoryNames(cultureInfo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var copied = false;

        foreach (var sourceDirectory in Directory.EnumerateDirectories(extractPath, "*", SearchOption.TopDirectoryOnly))
        {
            var directoryName = Path.GetFileName(sourceDirectory);
            if (!expectedDirectoryNames.Contains(directoryName))
                throw new InvalidDataException($"Language pack contains unexpected directory: {directoryName}");

            var destinationDirectory = Path.Combine(AppContext.BaseDirectory, directoryName);
            CopyDirectory(sourceDirectory, destinationDirectory);
            copied = true;
        }

        var rootFiles = Directory.EnumerateFiles(extractPath, "*.resources.dll", SearchOption.TopDirectoryOnly).ToArray();
        if (rootFiles.Length > 0)
        {
            var destinationDirectory = Path.Combine(AppContext.BaseDirectory, GetPrimaryResourceDirectoryName(cultureInfo));
            Directory.CreateDirectory(destinationDirectory);

            foreach (var file in rootFiles)
                File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), true);

            copied = true;
        }

        if (!copied)
            throw new InvalidDataException("Language pack does not contain resource files.");
    }

    private static void ExtractZipSafely(string zipPath, string destinationDirectory)
    {
        var destinationRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(destinationDirectory));

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
            if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Language pack contains an unsafe path: {entry.FullName}");

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, true);
        }
    }

    private static string EnsureTrailingDirectorySeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            if (!file.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Language pack contains unexpected file: {Path.GetFileName(file)}");

            File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), true);
        }

        if (Directory.EnumerateDirectories(sourceDirectory).Any())
            throw new InvalidDataException($"Language pack contains nested directories: {sourceDirectory}");
    }

    private static string GetPrimaryResourceDirectoryName(CultureInfo cultureInfo) =>
        cultureInfo.Name switch
        {
            "zh-Hans" => "zh",
            "uz-Latn-UZ" => "uz",
            _ => cultureInfo.Name
        };

    private static string[] GetResourceDirectoryNames(CultureInfo cultureInfo)
    {
        var names = new System.Collections.Generic.List<string>();
        var current = cultureInfo;

        while (current != CultureInfo.InvariantCulture)
        {
            if (!names.Contains(current.Name, StringComparer.OrdinalIgnoreCase))
                names.Add(current.Name);

            current = current.Parent;
        }

        var primary = GetPrimaryResourceDirectoryName(cultureInfo);
        if (!names.Contains(primary, StringComparer.OrdinalIgnoreCase))
            names.Add(primary);

        return names.ToArray();
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to delete directory: {directory}", ex);
        }
    }
}
