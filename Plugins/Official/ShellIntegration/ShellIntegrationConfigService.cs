using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalDeviceToolkit.Plugins.Core;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration;

public sealed record ShellManagedConfigPaths(
    string InstallDirectory,
    string ShellConfigPath,
    string ManagedDirectory,
    string SettingsPath,
    string ThemePath,
    string LanguagePath);

public sealed class ShellIntegrationConfigService
{
    private const string ManagedDirectoryName = "lenovo-legion-toolkit";
    private const string ManagedOutputDirectoryName = "managed";
    private const string ManagedBlockStart = "# region UniversalDeviceToolkit.Managed";
    private const string ManagedBlockEnd = "# endregion UniversalDeviceToolkit.Managed";
    private const string ManagedLanguageFileName = "language.nss";

    private readonly object _fileLock = new();
    private readonly Action<string> _openDirectory;
    private static readonly object _staticFileLock = new();

    private static readonly IReadOnlyDictionary<string, string[]> LanguageAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["zh-hans"] = ["zh-CN", "zh"],
        ["zh-cn"] = ["zh-CN", "zh"],
        ["zh-hant"] = ["zh-TW", "zh"],
        ["zh-tw"] = ["zh-TW", "zh"],
        ["pt-br"] = ["pt-BR", "pt"],
        ["nl-nl"] = ["nl"],
        ["uz-latn-uz"] = ["uz"]
    };

    // Pre-compiled regexes for UpsertManagedImportBlock — avoids allocating
    // new Regex objects on every ApplyProfile / EnsureManagedImportBlock call.
    // Same pattern applied to ViveToolFeatureService in VT-PERF-001.
    private static readonly Regex ManagedBlockReplaceRegex = new(
        $@"{Regex.Escape(ManagedBlockStart)}[\s\S]*?{Regex.Escape(ManagedBlockEnd)}",
        RegexOptions.Compiled);
    private static readonly Regex MenuDeclarationRegex = new(
        @"(?m)^\s*menu\(",
        RegexOptions.Compiled);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public ShellIntegrationConfigService(string? localProfileRoot = null, Action<string>? openDirectory = null)
    {
        _openDirectory = openDirectory ?? OpenDirectory;
        LocalProfileRoot = localProfileRoot ??
                           Path.Combine(
                               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                               "UniversalDeviceToolkit",
                               "Plugins",
                               "ShellIntegration");
        LegacyLocalProfileRoot = string.IsNullOrWhiteSpace(localProfileRoot)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LenovoLegionToolkit",
                "Plugins",
                "ShellIntegration")
            : null;
    }

    public string LocalProfileRoot { get; }
    public string? LegacyLocalProfileRoot { get; }
    public string LocalProfilePath => Path.Combine(LocalProfileRoot, "profile.json");

    private void EnsureLegacyProfileMigrated()
    {
        if (File.Exists(LocalProfilePath))
        {
            return;
        }

        if (LegacyLocalProfileRoot is null)
        {
            return;
        }

        var legacyProfilePath = Path.Combine(LegacyLocalProfileRoot, "profile.json");
        if (string.Equals(LocalProfilePath, legacyProfilePath, StringComparison.OrdinalIgnoreCase) ||
            IsUnderLegacyLenovoRoot(LocalProfilePath))
        {
            return;
        }

        if (!File.Exists(legacyProfilePath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(LocalProfileRoot);
            // Copy only: never write the LenovoLegionToolkit source. Temp + Move
            // so a crash cannot leave a truncated profile.json at the UDT path
            // (which would then block remigration on the next load).
            var tempPath = LocalProfilePath + ".tmp";
            try
            {
                File.Copy(legacyProfilePath, tempPath, overwrite: true);
                File.Move(tempPath, LocalProfilePath, overwrite: false);
            }
            catch
            {
                DeleteIfExists(tempPath);
                throw;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ShellIntegration: Failed to migrate legacy profile from '{legacyProfilePath}': {ex.Message}", ex);
        }
    }

    public ShellIntegrationProfile LoadProfile()
    {
        _ = TryLoadProfile(out var profile, out _);
        return profile;
    }

    public bool TryLoadProfile(out ShellIntegrationProfile profile, out string? errorMessage)
    {
        lock (_fileLock)
        {
            try
            {
                EnsureLegacyProfileMigrated();
                if (!File.Exists(LocalProfilePath))
                {
                    profile = ShellIntegrationProfile.CreateDefault();
                    errorMessage = null;
                    return true;
                }

                var json = File.ReadAllText(LocalProfilePath, Encoding.UTF8);
                var loaded = JsonSerializer.Deserialize<ShellIntegrationProfile>(json, _jsonOptions);
                profile = loaded?.Normalize() ?? ShellIntegrationProfile.CreateDefault();
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Trace($"ShellIntegration: Failed to load profile from '{LocalProfilePath}': {ex.Message}", ex);
                profile = ShellIntegrationProfile.CreateDefault();
                errorMessage = ex.Message;
                return false;
            }
        }
    }

    public bool TryLoadProfileFromFile(string filePath, out ShellIntegrationProfile profile, out string? errorMessage)
    {
        lock (_fileLock)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("Profile file path is required.", nameof(filePath));
                }

                if (!TryNormalizeUserFilePath(filePath, out var normalizedPath))
                {
                    throw new ArgumentException("Profile file path is invalid.", nameof(filePath));
                }

                if (!File.Exists(normalizedPath))
                {
                    throw new FileNotFoundException("Profile file was not found.", normalizedPath);
                }

                var json = File.ReadAllText(normalizedPath, Encoding.UTF8);
                var loaded = JsonSerializer.Deserialize<ShellIntegrationProfile>(json, _jsonOptions) ?? throw new InvalidDataException("Profile file is empty or invalid.");
                profile = loaded.Normalize();
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Trace($"ShellIntegration: Failed to load profile from '{filePath}': {ex.Message}", ex);
                profile = ShellIntegrationProfile.CreateDefault();
                errorMessage = ex.Message;
                return false;
            }
        }
    }

    public void SaveProfile(ShellIntegrationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        lock (_fileLock)
        {
            if (IsUnderLegacyLenovoRoot(LocalProfilePath))
            {
                throw new InvalidOperationException("The legacy LenovoLegionToolkit profile is read-only and must only be copied to the UniversalDeviceToolkit path.");
            }

            Directory.CreateDirectory(LocalProfileRoot);
            var json = JsonSerializer.Serialize(profile.Normalize(), _jsonOptions);
            AtomicWriteAllText(LocalProfilePath, json);
        }
    }

    public ShellIntegrationProfile ResetProfile()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        SaveProfile(profile);
        return profile;
    }

    public ShellIntegrationProfile ApplyPreset(ShellIntegrationPreset preset)
    {
        var profile = ShellIntegrationProfile.CreatePreset(preset).Normalize();
        SaveProfile(profile);
        return profile;
    }

    public bool ExportProfile(string filePath, ShellIntegrationProfile profile, out string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(profile);

        lock (_fileLock)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("Export file path is required.", nameof(filePath));
                }

                if (!TryNormalizeUserFilePath(filePath, out var normalizedPath))
                {
                    throw new ArgumentException("Export file path is invalid.", nameof(filePath));
                }

                if (IsUnderLegacyLenovoRoot(normalizedPath))
                {
                    throw new InvalidOperationException("Export must not write the legacy LenovoLegionToolkit profile.");
                }

                var directoryPath = Path.GetDirectoryName(normalizedPath);
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("Export file path must include a directory.", nameof(filePath));
                }

                Directory.CreateDirectory(directoryPath);
                var json = JsonSerializer.Serialize(profile.Normalize(), _jsonOptions);
                AtomicWriteAllText(normalizedPath, json);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Trace($"ShellIntegration: Failed to export profile to '{filePath}': {ex.Message}", ex);
                errorMessage = ex.Message;
                return false;
            }
        }
    }

    public bool ImportProfile(string filePath, out ShellIntegrationProfile profile, out string? errorMessage)
    {
        if (!TryLoadProfileFromFile(filePath, out profile, out errorMessage))
        {
            return false;
        }

        try
        {
            SaveProfile(profile);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ShellIntegration: Failed to import profile from '{filePath}': {ex.Message}", ex);
            errorMessage = ex.Message;
            return false;
        }
    }

    public ShellManagedConfigPaths? ResolveManagedPaths(string? shellInstallPath)
    {
        if (string.IsNullOrWhiteSpace(shellInstallPath))
        {
            return null;
        }

        var installDirectory = Directory.Exists(shellInstallPath)
            ? shellInstallPath
            : Path.GetDirectoryName(shellInstallPath);

        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return null;
        }

        var managedDirectory = Path.Combine(LocalProfileRoot, ManagedOutputDirectoryName, ManagedDirectoryName);
        return new ShellManagedConfigPaths(
            installDirectory,
            Path.Combine(installDirectory, "shell.nss"),
            managedDirectory,
            Path.Combine(managedDirectory, "settings.nss"),
            Path.Combine(managedDirectory, "theme.nss"),
            Path.Combine(managedDirectory, ManagedLanguageFileName));
    }

    public ShellManagedConfigPaths? ApplyProfile(string? shellInstallPath, ShellIntegrationProfile profile, CultureInfo? preferredCulture = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var paths = ResolveManagedPaths(shellInstallPath);
        if (paths is null)
        {
            return null;
        }

        var normalizedProfile = profile.Normalize();

        lock (_staticFileLock)
        {
            Directory.CreateDirectory(paths.ManagedDirectory);
            WriteFileIfChanged(paths.SettingsPath, RenderSettings(normalizedProfile));
            WriteFileIfChanged(paths.ThemePath, RenderTheme(normalizedProfile));
            WriteFileIfChanged(paths.LanguagePath, RenderLanguageOverride(paths.InstallDirectory, preferredCulture));
            EnsureManagedImportBlockUnlocked(paths);
        }

        return paths;
    }

    public void OpenManagedConfigFolder(string? shellInstallPath)
    {
        var paths = ResolveManagedPaths(shellInstallPath);
        var target = paths?.ManagedDirectory ?? LocalProfileRoot;

        try
        {
            Directory.CreateDirectory(target);
            _openDirectory(target);
        }
        catch
        {
            try
            {
                Directory.CreateDirectory(LocalProfileRoot);
                _openDirectory(LocalProfileRoot);
            }
            catch
            {
                // Ignore if the environment cannot open a folder window.
            }
        }
    }

    private static void OpenDirectory(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public static string RenderSettings(ShellIntegrationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var normalized = profile.Normalize();
        var showDelay = normalized.EnableMotionEffects ? normalized.ShowDelay : 200;
        var tipTime = normalized.TipTimeSeconds.ToString("0.0", CultureInfo.InvariantCulture);

        return
$@"# Managed by Universal Device Toolkit.
settings
{{
    showdelay = {showDelay}

    tip
    {{
        enabled = true
        normal = [default, default]
        width = 420
        opacity = 100
        radius = {normalized.BorderRadius}
        time = {tipTime}
        padding = [10, 6]
    }}
}}
";
    }

    public static string RenderTheme(ShellIntegrationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var normalized = profile.Normalize();

        return
$@"# Managed by Universal Device Toolkit.
theme
{{
    name = ""{normalized.ThemeName}""
    view = {normalized.GetViewExpression()}
    dark = {normalized.GetColorSchemeExpression()}

    background
    {{
        color = {normalized.BackgroundColor}
        opacity = {normalized.BackgroundOpacity}
        effect = {normalized.GetEffectExpression()}
    }}

    border
    {{
        enabled = true
        size = 1
        color = {normalized.AccentColor}
        opacity = 12
        radius = {normalized.BorderRadius}
        padding = [4, 4]
    }}

    shadow
    {{
        enabled = {normalized.EnableShadow.ToString().ToLowerInvariant()}
        size = {normalized.ShadowSize}
        color = {normalized.AccentColor}
        opacity = {normalized.ShadowOpacity}
        offset = {normalized.ShadowOffset}
    }}

    item
    {{
        opacity = 100
        radius = {normalized.ItemRadius}
        padding = [10, 5]
        margin = [4, 0]

        text
        {{
            normal = {normalized.TextColor}
            normal.disabled = {normalized.MutedTextColor}
            select = {normalized.SelectedTextColor}
            select.disabled = {normalized.MutedTextColor}
        }}

        back
        {{
            normal = {normalized.BackgroundColor}
            normal.disabled = {normalized.BackgroundColor}
            select = {normalized.AccentColor}
            select.disabled = {normalized.BackgroundColor}
        }}

        border
        {{
            normal = {normalized.BackgroundColor}
            normal.disabled = {normalized.BackgroundColor}
            select = {normalized.HoverColor}
            select.disabled = {normalized.BackgroundColor}
        }}
    }}

    separator
    {{
        size = 1
        color = {normalized.AccentColor}
        opacity = 16
        margin = [12, 5]
    }}

    symbol
    {{
        normal = {normalized.AccentColor}
        normal.disabled = {normalized.MutedTextColor}
        select = {normalized.SelectedTextColor}
        select.disabled = {normalized.MutedTextColor}
    }}
}}
";
    }

    public static string UpsertManagedImportBlock(string existingContent)
    {
        return UpsertManagedImportBlock(existingContent, GetRelativeManagedImportStatements());
    }

    public static string RemoveManagedImportBlock(string existingContent)
    {
        if (string.IsNullOrWhiteSpace(existingContent))
        {
            return string.Empty;
        }

        var cleaned = ManagedBlockReplaceRegex.Replace(existingContent, string.Empty).TrimEnd();
        return string.IsNullOrWhiteSpace(cleaned) ? string.Empty : cleaned + Environment.NewLine;
    }

    public bool TryRemoveManagedImportBlock(string? shellInstallPath)
    {
        var paths = ResolveManagedPaths(shellInstallPath);
        if (paths is null)
        {
            return false;
        }

        if (!File.Exists(paths.ShellConfigPath))
        {
            return true;
        }

        lock (_staticFileLock)
        {
            var existingContent = File.ReadAllText(paths.ShellConfigPath, Encoding.UTF8);
            WriteFileIfChangedUnlocked(paths.ShellConfigPath, RemoveManagedImportBlock(existingContent));
        }

        return true;
    }

    private static string UpsertManagedImportBlock(string existingContent, IEnumerable<string> importStatements)
    {
        var block = BuildManagedImportBlock(importStatements);

        var cleaned = string.IsNullOrWhiteSpace(existingContent)
            ? string.Empty
            : ManagedBlockReplaceRegex.Replace(existingContent, string.Empty).TrimEnd();

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return $"{block}{Environment.NewLine}";
        }

        var menuMatch = MenuDeclarationRegex.Match(cleaned);
        if (menuMatch.Success)
        {
            var before = cleaned[..menuMatch.Index].TrimEnd();
            var after = cleaned[menuMatch.Index..].TrimStart();

            return string.IsNullOrWhiteSpace(before)
                ? $"{block}{Environment.NewLine}{Environment.NewLine}{after}{Environment.NewLine}"
                : $"{before}{Environment.NewLine}{Environment.NewLine}{block}{Environment.NewLine}{after}{Environment.NewLine}";
        }

        return $"{cleaned}{Environment.NewLine}{Environment.NewLine}{block}{Environment.NewLine}";
    }

    private static string BuildManagedImportBlock(IEnumerable<string> importStatements)
    {
        var builder = new StringBuilder();
        builder.AppendLine(ManagedBlockStart);
        foreach (var statement in importStatements)
        {
            builder.AppendLine(statement);
        }

        builder.Append(ManagedBlockEnd);
        return builder.ToString();
    }

    private static IReadOnlyList<string> GetRelativeManagedImportStatements()
    {
        return
        [
            $"import lang 'imports/{ManagedDirectoryName}/{ManagedLanguageFileName}'",
            $"import 'imports/{ManagedDirectoryName}/settings.nss'",
            $"import 'imports/{ManagedDirectoryName}/theme.nss'"
        ];
    }

    private static IReadOnlyList<string> GetAbsoluteManagedImportStatements(ShellManagedConfigPaths paths)
    {
        return
        [
            $"import lang '{NormalizeImportPath(paths.LanguagePath)}'",
            $"import '{NormalizeImportPath(paths.SettingsPath)}'",
            $"import '{NormalizeImportPath(paths.ThemePath)}'"
        ];
    }

    private static string NormalizeImportPath(string path)
    {
        return path.Replace('\\', '/').Replace("'", "\\'");
    }

    private static void EnsureManagedImportBlock(ShellManagedConfigPaths paths)
    {
        lock (_staticFileLock)
        {
            EnsureManagedImportBlockUnlocked(paths);
        }
    }

    /// <summary>
    /// Updates the managed import block in the shell config without acquiring
    /// the static file lock. Caller must hold _staticFileLock.
    /// (SLI-026: prevents reentrant deadlock when called from ApplyProfile)
    /// </summary>
    private static void EnsureManagedImportBlockUnlocked(ShellManagedConfigPaths paths)
    {
        var existingContent = File.Exists(paths.ShellConfigPath)
            ? File.ReadAllText(paths.ShellConfigPath, Encoding.UTF8)
            : string.Empty;

        var updated = UpsertManagedImportBlock(existingContent, GetAbsoluteManagedImportStatements(paths));
        var directory = Path.GetDirectoryName(paths.ShellConfigPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        WriteFileIfChangedUnlocked(paths.ShellConfigPath, updated);
    }

    private static void WriteFileIfChanged(string path, string content)
    {
        lock (_staticFileLock)
        {
            WriteFileIfChangedUnlocked(path, content);
        }
    }

    /// <summary>
    /// Writes file if content changed without acquiring the static file lock.
    /// Caller must hold _staticFileLock.
    /// (SLI-026: prevents reentrant deadlock when called from EnsureManagedImportBlockUnlocked)
    /// (SLI-027: uses atomic temp-file + File.Move so a crash mid-write cannot
    ///  leave the shell config or managed .nss files truncated. The previous
    ///  File.WriteAllText was non-atomic: if the process or OS flushed partially,
    ///  settings.nss / theme.nss / language.nss / shell.nss would be corrupt.)
    /// </summary>
    private static void WriteFileIfChangedUnlocked(string path, string content)
    {
        var existingContent = File.Exists(path)
            ? File.ReadAllText(path, Encoding.UTF8)
            : null;

        if (string.Equals(existingContent, content, StringComparison.Ordinal))
        {
            return;
        }

        AtomicWriteAllText(path, content);
    }

    private static void AtomicWriteAllText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        try
        {
            File.WriteAllText(tempPath, content, new UTF8Encoding(false));
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            DeleteIfExists(tempPath);
            throw;
        }
    }

    private static bool TryNormalizeUserFilePath(string? filePath, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (ContainsUnsafePathSegment(filePath))
        {
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(filePath!);
            if (string.IsNullOrWhiteSpace(fullPath) || ContainsUnsafePathSegment(fullPath))
            {
                return false;
            }

            var fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains(':', StringComparison.Ordinal))
            {
                return false;
            }

            normalizedPath = fullPath;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException)
        {
            return false;
        }
    }

    private static bool ContainsUnsafePathSegment(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        if (path.Contains('\0', StringComparison.Ordinal) ||
            path.Contains("..", StringComparison.Ordinal))
        {
            return true;
        }

        return path.StartsWith(@"\\.\", StringComparison.Ordinal) ||
               path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
               path.StartsWith("//./", StringComparison.Ordinal) ||
               path.StartsWith("//?/", StringComparison.Ordinal);
    }

    private static bool IsUnderLegacyLenovoRoot(string path)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData) || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var legacyRoot = Path.GetFullPath(Path.Combine(localAppData, "LenovoLegionToolkit"));
            if (!legacyRoot.EndsWith(Path.DirectorySeparatorChar) &&
                !legacyRoot.EndsWith(Path.AltDirectorySeparatorChar))
            {
                legacyRoot += Path.DirectorySeparatorChar;
            }

            var fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(legacyRoot, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.Equals(legacyRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException)
        {
            return false;
        }
    }

    private static void DeleteIfExists(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { /* best-effort cleanup */ }
    }

    private static string RenderLanguageOverride(string installDirectory, CultureInfo? preferredCulture)
    {
        var sourcePath = ResolveLanguageSourcePath(installDirectory, preferredCulture);
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return "# Managed by Universal Device Toolkit." + Environment.NewLine;
        }

        return File.ReadAllText(sourcePath, Encoding.UTF8);
    }

    private static string? ResolveLanguageSourcePath(string installDirectory, CultureInfo? preferredCulture)
    {
        var languageDirectory = Path.Combine(installDirectory, "imports", "lang");
        if (!Directory.Exists(languageDirectory))
        {
            return null;
        }

        var languageRoot = Path.GetFullPath(languageDirectory);
        if (!languageRoot.EndsWith(Path.DirectorySeparatorChar) &&
            !languageRoot.EndsWith(Path.AltDirectorySeparatorChar))
        {
            languageRoot += Path.DirectorySeparatorChar;
        }

        foreach (var candidate in GetLanguageFileCandidates(preferredCulture))
        {
            var path = Path.GetFullPath(Path.Combine(languageDirectory, candidate));
            if (!path.StartsWith(languageRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetLanguageFileCandidates(CultureInfo? preferredCulture)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name.Contains("..", StringComparison.Ordinal) ||
                name.IndexOfAny(['/', '\\', '\0']) >= 0)
            {
                return;
            }

            var candidate = name.EndsWith(".nss", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.nss";
            if (seen.Add(candidate))
            {
                candidates.Add(candidate);
            }
        }

        if (preferredCulture is not null)
        {
            AddCandidate(preferredCulture.Name);
            AddCandidate(preferredCulture.IetfLanguageTag);

            if (LanguageAliases.TryGetValue(preferredCulture.Name, out var aliasesByName))
            {
                foreach (var alias in aliasesByName)
                {
                    AddCandidate(alias);
                }
            }

            if (LanguageAliases.TryGetValue(preferredCulture.IetfLanguageTag, out var aliasesByTag))
            {
                foreach (var alias in aliasesByTag)
                {
                    AddCandidate(alias);
                }
            }

            if (!string.IsNullOrWhiteSpace(preferredCulture.Parent?.Name))
            {
                AddCandidate(preferredCulture.Parent.Name);
            }
        }

        AddCandidate("en");
        return candidates;
    }
}
