using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.ResourcesCatalog;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

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
            .Select(directoryName => Path.Combine(ApplicationDirectory, directoryName))
            .Any(HasAppResourceAssembly);
    }

    public string GetInstallUrl(CultureInfo cultureInfo)
    {
        var version = GetCurrentVersion();
        return $"{AppIdentity.ResourcesBaseUrl}/{version}/languages/{NormalizeAssetCultureName(cultureInfo)}.zip";
    }

    public async Task<IReadOnlyList<LanguagePackCatalogEntry>> QueryCatalogAsync(CancellationToken token = default)
    {
        try
        {
            var catalog = await resourceCatalogClient.GetCatalogAsync(token);
            return catalog.Languages
                .Select(ToCatalogEntry)
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Culture))
                .ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LanguagePackException(
                LanguagePackFailureKind.CatalogUnavailable,
                "Failed to query the online language catalog.",
                inner: ex);
        }
    }

    public async Task InstallAsync(CultureInfo cultureInfo, IProgress<float>? progress = null, CancellationToken token = default)
    {
        if (IsEnglish(cultureInfo))
            return;

        var installProgress = new InstallProgressReporter(progress);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"{AssetPrefix}-lang-{cultureInfo.Name}-{Guid.NewGuid():N}");
        var tempZipPath = Path.Combine(tempRoot, "language.zip");
        var fallbackZipPath = Path.Combine(tempRoot, "full-portable.zip");
        var extractPath = Path.Combine(tempRoot, "extract");
        var stagingPath = Path.Combine(tempRoot, "staging");
        var fallbackExtractPath = Path.Combine(tempRoot, "fallback-extract");

        try
        {
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(extractPath);
            Directory.CreateDirectory(stagingPath);

            OnlineResourceCatalog? catalog = null;
            try
            {
                installProgress.ReportStarted();
                catalog = await resourceCatalogClient.GetCatalogAsync(token);
                installProgress.ReportCatalogComplete();

                var languageResource = GetLanguageResource(catalog, cultureInfo);
                EnsureMinAppVersion(languageResource, cultureInfo);

                await resourceCatalogClient.DownloadAsync(
                    languageResource.Url,
                    tempZipPath,
                    installProgress.CreateDownloadProgress(),
                    token);
                installProgress.ReportVerifyStarted();
                try
                {
                    await resourceCatalogClient.VerifySha256Async(tempZipPath, languageResource.Sha256, token);
                }
                catch (Exception ex)
                {
                    throw new LanguagePackException(
                        LanguagePackFailureKind.HashMismatch,
                        $"SHA256 verification failed for language '{cultureInfo.Name}'.",
                        cultureInfo.Name,
                        ex);
                }

                installProgress.ReportVerifyComplete();
                installProgress.ReportApplyStarted();

                try
                {
                    ExtractZipSafely(tempZipPath, extractPath);
                }
                catch (Exception ex) when (ex is not LanguagePackException and not OperationCanceledException)
                {
                    throw new LanguagePackException(
                        LanguagePackFailureKind.CorruptPackage,
                        $"Language pack zip for '{cultureInfo.Name}' is corrupt or unsafe.",
                        cultureInfo.Name,
                        ex);
                }

                StageLanguageDirectories(extractPath, stagingPath, cultureInfo);
                ValidateStagedAssemblies(stagingPath, cultureInfo);
                AtomicApplyStagedDirectories(stagingPath, cultureInfo);

                if (!IsInstalled(cultureInfo))
                    throw new LanguagePackException(
                        LanguagePackFailureKind.ValidationFailed,
                        $"Language pack for '{cultureInfo.Name}' did not install application UI resources.",
                        cultureInfo.Name);

                installProgress.ReportComplete();
            }
            catch (OperationCanceledException)
            {
                throw new LanguagePackException(
                    LanguagePackFailureKind.Cancelled,
                    $"Language pack install for '{cultureInfo.Name}' was cancelled.",
                    cultureInfo.Name);
            }
            catch (LanguagePackException ex) when (
                ex.Kind is LanguagePackFailureKind.Cancelled
                    or LanguagePackFailureKind.AppVersionTooOld)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Language pack download failed for '{cultureInfo.Name}'. Falling back to full portable package.", ex);

                try
                {
                    await InstallFromFullPortableAsync(cultureInfo, catalog, fallbackZipPath, fallbackExtractPath, stagingPath, installProgress, token);

                    if (!IsInstalled(cultureInfo))
                        throw new LanguagePackException(
                            LanguagePackFailureKind.ValidationFailed,
                            $"Language pack for '{cultureInfo.Name}' could not be installed.",
                            cultureInfo.Name,
                            ex);
                }
                catch (LanguagePackException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw new LanguagePackException(
                        LanguagePackFailureKind.Cancelled,
                        $"Language pack install for '{cultureInfo.Name}' was cancelled.",
                        cultureInfo.Name);
                }
                catch (Exception fallbackEx)
                {
                    throw WrapInstallFailure(cultureInfo, fallbackEx, ex);
                }
            }
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
            var directory = Path.Combine(ApplicationDirectory, directoryName);
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
                var directory = Path.Combine(ApplicationDirectory, directoryName);
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

    public static string NormalizeAssetCultureName(CultureInfo cultureInfo) =>
        cultureInfo.Name;

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version ?? Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null || version.Major < 0 || version.Minor < 0 || version.Build < 0)
            return "0.0.0";

        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static OnlineLanguageResource GetLanguageResource(OnlineResourceCatalog catalog, CultureInfo cultureInfo)
    {
        foreach (var candidate in EnumerateCultureLookupNames(cultureInfo))
        {
            var resource = catalog.Languages.FirstOrDefault(language =>
                language.Culture.Equals(candidate, StringComparison.OrdinalIgnoreCase));

            if (resource is null)
            {
                resource = catalog.Languages.FirstOrDefault(language =>
                    !string.IsNullOrWhiteSpace(language.Parent) &&
                    language.Parent.Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
                    language.Culture.Equals(NormalizeAssetCultureName(cultureInfo), StringComparison.OrdinalIgnoreCase));
            }

            if (resource is null)
                continue;

            if (string.IsNullOrWhiteSpace(resource.Url))
                throw new LanguagePackException(LanguagePackFailureKind.DownloadFailed, $"Language '{cultureInfo.Name}' has an empty download URL.", cultureInfo.Name);
            if (string.IsNullOrWhiteSpace(resource.Sha256))
                throw new LanguagePackException(LanguagePackFailureKind.HashMismatch, $"Language '{cultureInfo.Name}' is missing SHA256 metadata.", cultureInfo.Name);
            return resource;
        }

        throw new LanguagePackException(
            LanguagePackFailureKind.CultureNotInCatalog,
            $"Language '{cultureInfo.Name}' is not available in the online resource catalog.",
            cultureInfo.Name);
    }

    private static IEnumerable<string> EnumerateCultureLookupNames(CultureInfo cultureInfo)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = cultureInfo;
        while (current != CultureInfo.InvariantCulture)
        {
            if (seen.Add(current.Name))
                yield return current.Name;
            if (seen.Add(NormalizeAssetCultureName(current)))
                yield return NormalizeAssetCultureName(current);
            current = current.Parent;
        }
    }

    private static void EnsureMinAppVersion(OnlineLanguageResource languageResource, CultureInfo cultureInfo)
    {
        if (string.IsNullOrWhiteSpace(languageResource.MinAppVersion))
            return;

        if (!Version.TryParse(languageResource.MinAppVersion, out var minVersion))
            return;

        if (!Version.TryParse(GetCurrentVersion(), out var appVersion))
            return;

        if (appVersion < minVersion)
        {
            throw new LanguagePackException(
                LanguagePackFailureKind.AppVersionTooOld,
                $"Language '{cultureInfo.Name}' requires app version {languageResource.MinAppVersion} or newer.",
                cultureInfo.Name);
        }
    }

    private static LanguagePackCatalogEntry ToCatalogEntry(OnlineLanguageResource resource) =>
        new(
            resource.Culture,
            resource.Parent,
            resource.Size,
            resource.Sha256,
            resource.ResourceVersion,
            resource.MinAppVersion,
            resource.Url,
            resource.DisplayName);

    private static LanguagePackException WrapInstallFailure(CultureInfo cultureInfo, Exception fallbackEx, Exception primaryEx)
    {
        var kind = fallbackEx switch
        {
            InvalidDataException => LanguagePackFailureKind.CorruptPackage,
            System.Net.Http.HttpRequestException => LanguagePackFailureKind.DownloadFailed,
            _ => LanguagePackFailureKind.Unknown
        };

        return new LanguagePackException(
            kind,
            $"Language pack for '{cultureInfo.Name}' could not be installed.",
            cultureInfo.Name,
            new AggregateException(primaryEx, fallbackEx));
    }

    private async Task InstallFromFullPortableAsync(
        CultureInfo cultureInfo,
        OnlineResourceCatalog? catalog,
        string zipPath,
        string extractPath,
        string stagingPath,
        InstallProgressReporter installProgress,
        CancellationToken token)
    {
        installProgress.ReportStarted();
        var fullPortable = await GetFullPortableResourceAsync(catalog, installProgress, token);
        installProgress.ReportCatalogComplete();

        if (string.IsNullOrWhiteSpace(fullPortable.Url))
            throw new LanguagePackException(LanguagePackFailureKind.DownloadFailed, "Full portable fallback has an empty download URL.", cultureInfo.Name);

        if (string.IsNullOrWhiteSpace(fullPortable.Sha256))
            throw new LanguagePackException(LanguagePackFailureKind.HashMismatch, "Full portable fallback is missing SHA256 metadata.", cultureInfo.Name);

        Directory.CreateDirectory(extractPath);
        TryDeleteDirectory(stagingPath);
        Directory.CreateDirectory(stagingPath);

        await resourceCatalogClient.DownloadAsync(fullPortable.Url, zipPath, installProgress.CreateDownloadProgress(), token);
        installProgress.ReportVerifyStarted();
        await resourceCatalogClient.VerifySha256Async(zipPath, fullPortable.Sha256, token);
        installProgress.ReportVerifyComplete();

        installProgress.ReportApplyStarted();
        ExtractMatchingLanguageDirectories(zipPath, extractPath, cultureInfo);
        StageLanguageDirectories(extractPath, stagingPath, cultureInfo);
        ValidateStagedAssemblies(stagingPath, cultureInfo);
        AtomicApplyStagedDirectories(stagingPath, cultureInfo);
        installProgress.ReportComplete();
    }

    private async Task<OnlineFileResource> GetFullPortableResourceAsync(OnlineResourceCatalog? catalog, InstallProgressReporter installProgress, CancellationToken token)
    {
        if (catalog?.Downloads?.Full?.Portable is { } catalogPortable &&
            !string.IsNullOrWhiteSpace(catalogPortable.Url) &&
            !string.IsNullOrWhiteSpace(catalogPortable.Sha256))
        {
            return catalogPortable;
        }

        return await CreateReleaseFullPortableResourceAsync(token);
    }

    private async Task<OnlineFileResource> CreateReleaseFullPortableResourceAsync(CancellationToken token)
    {
        var version = GetCurrentVersion();
        var assetName = $"{AssetPrefix}_v{version}_Full_win-x64.zip";
        var hashAssetName = $"{AssetPrefix}_v{version}_SHA256.txt";
        var releaseBaseUrl = $"{AppIdentity.RepositoryUrl}/releases/download/v{version}";
        var hashUrl = $"{releaseBaseUrl}/{hashAssetName}";
        var hashTempPath = Path.Combine(Path.GetTempPath(), $"{AssetPrefix}-sha256-{Guid.NewGuid():N}.txt");

        try
        {
            await resourceCatalogClient.DownloadAsync(hashUrl, hashTempPath, token: token);
            var hashText = await File.ReadAllTextAsync(hashTempPath, token);
            var sha256 = ResolveHash(hashText, assetName);
            if (string.IsNullOrWhiteSpace(sha256))
                throw new InvalidDataException($"SHA256 file does not contain an entry for '{assetName}'.");

            return new OnlineFileResource
            {
                Name = assetName,
                Url = $"{releaseBaseUrl}/{assetName}",
                Sha256 = sha256
            };
        }
        finally
        {
            try { File.Delete(hashTempPath); }
            catch (Exception ex)
            {
                Log.Instance.TraceOnce(
                    "langpack-hash-temp-cleanup",
                    $"Best-effort delete of language pack hash temp failed: {hashTempPath}",
                    ex);
            }
        }
    }

    private static string ResolveHash(string hashText, string assetName)
    {
        foreach (var line in hashText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length < 64 || !trimmed.Contains(assetName, StringComparison.OrdinalIgnoreCase))
                continue;

            var hash = trimmed[..64];
            if (hash.All(Uri.IsHexDigit))
                return hash.ToLowerInvariant();
        }

        return string.Empty;
    }

    private static void ExtractMatchingLanguageDirectories(string zipPath, string destinationDirectory, CultureInfo cultureInfo)
    {
        var destinationRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(destinationDirectory));
        var expectedDirectoryNames = GetResourceDirectoryNames(cultureInfo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var extracted = false;

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var normalizedEntryName = entry.FullName.Replace('\\', '/');
            var separatorIndex = normalizedEntryName.IndexOf('/');
            if (separatorIndex <= 0 || string.IsNullOrEmpty(entry.Name))
                continue;

            var topLevelDirectory = normalizedEntryName[..separatorIndex];
            if (!expectedDirectoryNames.Contains(topLevelDirectory))
                continue;

            var relativePath = normalizedEntryName[(separatorIndex + 1)..];
            if (string.IsNullOrWhiteSpace(relativePath))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, topLevelDirectory, relativePath));
            if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Full portable package contains an unsafe path: {entry.FullName}");

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, true);
            extracted = true;
        }

        if (!extracted)
            throw new InvalidDataException($"Full portable package does not contain language resources for '{cultureInfo.Name}'.");
    }

    private static void StageLanguageDirectories(string extractPath, string stagingRoot, CultureInfo cultureInfo)
    {
        var expectedDirectoryNames = GetResourceDirectoryNames(cultureInfo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var staged = false;

        foreach (var sourceDirectory in Directory.EnumerateDirectories(extractPath, "*", SearchOption.TopDirectoryOnly))
        {
            var directoryName = Path.GetFileName(sourceDirectory);
            if (!expectedDirectoryNames.Contains(directoryName))
            {
                if (TryStageNestedCultureDirectories(sourceDirectory, stagingRoot, expectedDirectoryNames))
                    staged = true;

                continue;
            }

            var destinationDirectory = Path.Combine(stagingRoot, directoryName);
            CopySatelliteAssemblies(sourceDirectory, destinationDirectory);
            staged = true;
        }

        var rootFiles = Directory.EnumerateFiles(extractPath, "*.resources.dll", SearchOption.TopDirectoryOnly).ToArray();
        if (rootFiles.Length > 0)
        {
            var destinationDirectory = Path.Combine(stagingRoot, GetPrimaryResourceDirectoryName(cultureInfo));
            Directory.CreateDirectory(destinationDirectory);

            foreach (var file in rootFiles)
                File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), true);

            staged = true;
        }

        if (!staged)
            throw new LanguagePackException(
                LanguagePackFailureKind.CorruptPackage,
                "Language pack does not contain resource files.",
                cultureInfo.Name);
    }

    private static bool TryStageNestedCultureDirectories(
        string sourceDirectory,
        string stagingRoot,
        System.Collections.Generic.HashSet<string> expectedDirectoryNames)
    {
        var staged = false;

        foreach (var nestedDirectory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            var directoryName = Path.GetFileName(nestedDirectory);
            if (!expectedDirectoryNames.Contains(directoryName))
                continue;

            var destinationDirectory = Path.Combine(stagingRoot, directoryName);
            CopySatelliteAssemblies(nestedDirectory, destinationDirectory);
            staged = true;
        }

        return staged;
    }

    private static void ValidateStagedAssemblies(string stagingRoot, CultureInfo cultureInfo)
    {
        var hasAssembly = GetResourceDirectoryNames(cultureInfo)
            .Select(name => Path.Combine(stagingRoot, name))
            .Any(HasAppResourceAssembly);

        if (!hasAssembly)
        {
            throw new LanguagePackException(
                LanguagePackFailureKind.ValidationFailed,
                $"Staged language pack for '{cultureInfo.Name}' is missing application satellite assemblies.",
                cultureInfo.Name);
        }
    }

    private static void AtomicApplyStagedDirectories(string stagingRoot, CultureInfo cultureInfo)
    {
        foreach (var stagedDirectory in Directory.EnumerateDirectories(stagingRoot, "*", SearchOption.TopDirectoryOnly))
        {
            var directoryName = Path.GetFileName(stagedDirectory);
            var destinationDirectory = Path.Combine(ApplicationDirectory, directoryName);
            AtomicReplaceDirectory(stagedDirectory, destinationDirectory);
        }
    }

    private static void AtomicReplaceDirectory(string sourceDirectory, string destinationDirectory)
    {
        var destinationParent = Path.GetDirectoryName(destinationDirectory);
        if (!string.IsNullOrWhiteSpace(destinationParent))
            Directory.CreateDirectory(destinationParent);

        var backupDirectory = destinationDirectory + $".bak-{Guid.NewGuid():N}";
        try
        {
            if (Directory.Exists(destinationDirectory))
                TryMoveOrCopyWithRetry(destinationDirectory, backupDirectory);

            if (Path.GetPathRoot(sourceDirectory)?.Equals(Path.GetPathRoot(destinationDirectory), StringComparison.OrdinalIgnoreCase) == true)
            {
                TryMoveOrCopyWithRetry(sourceDirectory, destinationDirectory);
            }
            else
            {
                CopyDirectoryRecursive(sourceDirectory, destinationDirectory);
                TryDeleteDirectory(sourceDirectory);
            }

            TryDeleteDirectory(backupDirectory);
        }
        catch (Exception ex)
        {
            if (Directory.Exists(backupDirectory) && !Directory.Exists(destinationDirectory))
            {
                try { Directory.Move(backupDirectory, destinationDirectory); }
                catch (Exception restoreEx)
                {
                    Log.Instance.Warning(
                        $"Language pack apply failed and backup restore also failed: {destinationDirectory}",
                        restoreEx);
                }
            }

            throw new LanguagePackException(
                LanguagePackFailureKind.ApplyFailed,
                $"Failed to atomically apply language directory '{destinationDirectory}'.",
                inner: ex);
        }
    }

    private static void CopyDirectoryRecursive(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), true);

        foreach (var nested in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
            CopyDirectoryRecursive(nested, Path.Combine(destinationDirectory, Path.GetFileName(nested)));
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

    private static void CopySatelliteAssemblies(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*.resources.dll", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), true);

        foreach (var nestedDirectory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            var directoryName = Path.GetFileName(nestedDirectory);
            var destinationNestedDirectory = Path.Combine(destinationDirectory, directoryName);
            CopySatelliteAssemblies(nestedDirectory, destinationNestedDirectory);
        }
    }

    private static string ApplicationDirectory
    {
        get
        {
            var program = Folders.Program;
            if (!string.IsNullOrWhiteSpace(program))
            {
                var normalized = program.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (Directory.Exists(normalized))
                    return normalized;
            }

            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                var directory = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    return directory;
            }

            return AppContext.BaseDirectory;
        }
    }

    private static readonly string[] AppResourceAssemblyFileNames = CreateAppResourceAssemblyFileNames();

    private static string[] CreateAppResourceAssemblyFileNames()
    {
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{AppIdentity.DisplayName}.resources.dll",
            "UniversalDeviceToolkit.WPF.resources.dll",
        };

        var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
        if (!string.IsNullOrWhiteSpace(entryAssemblyName))
            names.Add($"{entryAssemblyName}.resources.dll");

        var hostAssemblyName = typeof(LanguagePackManager).Assembly.GetName().Name;
        if (!string.IsNullOrWhiteSpace(hostAssemblyName))
            names.Add($"{hostAssemblyName}.resources.dll");

        return [.. names];
    }

    private static bool HasAppResourceAssembly(string directory)
    {
        if (!Directory.Exists(directory))
            return false;

        foreach (var fileName in AppResourceAssemblyFileNames)
        {
            if (File.Exists(Path.Combine(directory, fileName)))
                return true;
        }

        return false;
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

        var normalizedCulture = NormalizeAssetCultureName(cultureInfo);
        if (!names.Contains(normalizedCulture, StringComparer.OrdinalIgnoreCase))
            names.Add(normalizedCulture);

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

    private static void TryMoveOrCopyWithRetry(string sourceDirectory, string destinationDirectory, int maxAttempts = 3)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Move(sourceDirectory, destinationDirectory);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(100 * (attempt + 1));
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(100 * (attempt + 1));
            }
            catch (IOException) when (attempt >= maxAttempts)
            {
                // Directory.Move failed (likely locked satellite assemblies).
                // Fall back to file-by-file copy + source cleanup.
                CopyDirectoryRecursive(sourceDirectory, destinationDirectory);
                TryDeleteDirectory(sourceDirectory);
                return;
            }
        }
    }

    private sealed class InstallProgressReporter(IProgress<float>? progress)
    {
        private const float CatalogEnd = 0.05f;
        private const float DownloadStart = 0.05f;
        private const float DownloadEnd = 0.85f;
        private const float VerifyStart = 0.85f;
        private const float VerifyEnd = 0.90f;
        private const float ApplyStart = 0.90f;

        public void ReportStarted() => progress?.Report(0f);

        public void ReportCatalogComplete() => progress?.Report(CatalogEnd);

        public IProgress<float>? CreateDownloadProgress() =>
            progress is null
                ? null
                : new Progress<float>(value => progress.Report(DownloadStart + value * (DownloadEnd - DownloadStart)));

        public void ReportVerifyStarted() => progress?.Report(VerifyStart);

        public void ReportVerifyComplete() => progress?.Report(VerifyEnd);

        public void ReportApplyStarted() => progress?.Report(ApplyStart);

        public void ReportComplete() => progress?.Report(1f);
    }
}
