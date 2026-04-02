using System;
using System.Collections.Generic;
using System.Diagnostics;
#nullable enable

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

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
    private const string ManagedBlockStart = "# region LenovoLegionToolkit.Managed";
    private const string ManagedBlockEnd = "# endregion LenovoLegionToolkit.Managed";
    private const string ManagedLanguageFileName = "language.nss";

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

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public ShellIntegrationConfigService(string? localProfileRoot = null)
    {
        LocalProfileRoot = localProfileRoot ??
                           Path.Combine(
                               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                               "LenovoLegionToolkit",
                               "Plugins",
                               "ShellIntegration");
    }

    public string LocalProfileRoot { get; }
    public string LocalProfilePath => Path.Combine(LocalProfileRoot, "profile.json");

    public ShellIntegrationProfile LoadProfile()
    {
        try
        {
            if (!File.Exists(LocalProfilePath))
                return ShellIntegrationProfile.CreateDefault();

            var json = File.ReadAllText(LocalProfilePath, Encoding.UTF8);
            var loaded = JsonSerializer.Deserialize<ShellIntegrationProfile>(json, _jsonOptions);
            return loaded?.Normalize() ?? ShellIntegrationProfile.CreateDefault();
        }
        catch
        {
            return ShellIntegrationProfile.CreateDefault();
        }
    }

    public void SaveProfile(ShellIntegrationProfile profile)
    {
        Directory.CreateDirectory(LocalProfileRoot);
        var json = JsonSerializer.Serialize(profile.Normalize(), _jsonOptions);
        File.WriteAllText(LocalProfilePath, json, new UTF8Encoding(false));
    }

    public ShellManagedConfigPaths? ResolveManagedPaths(string? shellInstallPath)
    {
        if (string.IsNullOrWhiteSpace(shellInstallPath))
            return null;

        var installDirectory = Directory.Exists(shellInstallPath)
            ? shellInstallPath
            : Path.GetDirectoryName(shellInstallPath);

        if (string.IsNullOrWhiteSpace(installDirectory))
            return null;

        var managedDirectory = Path.Combine(installDirectory, "imports", ManagedDirectoryName);
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
        var paths = ResolveManagedPaths(shellInstallPath);
        if (paths is null)
            return null;

        Directory.CreateDirectory(paths.ManagedDirectory);
        File.WriteAllText(paths.SettingsPath, RenderSettings(profile.Normalize()), new UTF8Encoding(false));
        File.WriteAllText(paths.ThemePath, RenderTheme(profile.Normalize()), new UTF8Encoding(false));
        File.WriteAllText(paths.LanguagePath, RenderLanguageOverride(paths.InstallDirectory, preferredCulture), new UTF8Encoding(false));
        EnsureManagedImportBlock(paths.ShellConfigPath);

        return paths;
    }

    public void OpenManagedConfigFolder(string? shellInstallPath)
    {
        var paths = ResolveManagedPaths(shellInstallPath);
        var target = paths?.ManagedDirectory ?? LocalProfileRoot;

        try
        {
            Directory.CreateDirectory(target);
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                Directory.CreateDirectory(LocalProfileRoot);
                Process.Start(new ProcessStartInfo
                {
                    FileName = LocalProfileRoot,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore if the environment cannot open a folder window.
            }
        }
    }

    public static string RenderSettings(ShellIntegrationProfile profile)
    {
        var normalized = profile.Normalize();
        var showDelay = normalized.EnableMotionEffects ? normalized.ShowDelay : 200;
        var tipTime = normalized.TipTimeSeconds.ToString("0.0", CultureInfo.InvariantCulture);

        return
$@"# Managed by Lenovo Legion Toolkit.
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
        var normalized = profile.Normalize();

        return
$@"# Managed by Lenovo Legion Toolkit.
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
        var block =
$@"{ManagedBlockStart}
import lang 'imports/{ManagedDirectoryName}/{ManagedLanguageFileName}'
import 'imports/{ManagedDirectoryName}/settings.nss'
import 'imports/{ManagedDirectoryName}/theme.nss'
{ManagedBlockEnd}
".TrimEnd();

        var pattern = $"{Regex.Escape(ManagedBlockStart)}[\\s\\S]*?{Regex.Escape(ManagedBlockEnd)}";
        var cleaned = string.IsNullOrWhiteSpace(existingContent)
            ? string.Empty
            : Regex.Replace(existingContent, pattern, string.Empty).TrimEnd();

        if (string.IsNullOrWhiteSpace(cleaned))
            return $"{block}{Environment.NewLine}";

        var menuMatch = Regex.Match(cleaned, @"(?m)^\s*menu\(");
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

    private static void EnsureManagedImportBlock(string shellConfigPath)
    {
        var existingContent = File.Exists(shellConfigPath)
            ? File.ReadAllText(shellConfigPath, Encoding.UTF8)
            : string.Empty;

        var updated = UpsertManagedImportBlock(existingContent);
        var directory = Path.GetDirectoryName(shellConfigPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(shellConfigPath, updated, new UTF8Encoding(false));
    }

    private static string RenderLanguageOverride(string installDirectory, CultureInfo? preferredCulture)
    {
        var sourcePath = ResolveLanguageSourcePath(installDirectory, preferredCulture);
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return "# Managed by Lenovo Legion Toolkit." + Environment.NewLine;

        return File.ReadAllText(sourcePath, Encoding.UTF8);
    }

    private static string? ResolveLanguageSourcePath(string installDirectory, CultureInfo? preferredCulture)
    {
        var languageDirectory = Path.Combine(installDirectory, "imports", "lang");
        if (!Directory.Exists(languageDirectory))
            return null;

        foreach (var candidate in GetLanguageFileCandidates(preferredCulture))
        {
            var path = Path.Combine(languageDirectory, candidate);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static IEnumerable<string> GetLanguageFileCandidates(CultureInfo? preferredCulture)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            var candidate = name.EndsWith(".nss", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.nss";
            if (seen.Add(candidate))
                candidates.Add(candidate);
        }

        if (preferredCulture is not null)
        {
            AddCandidate(preferredCulture.Name);
            AddCandidate(preferredCulture.IetfLanguageTag);

            if (LanguageAliases.TryGetValue(preferredCulture.Name, out var aliasesByName))
            {
                foreach (var alias in aliasesByName)
                    AddCandidate(alias);
            }

            if (LanguageAliases.TryGetValue(preferredCulture.IetfLanguageTag, out var aliasesByTag))
            {
                foreach (var alias in aliasesByTag)
                    AddCandidate(alias);
            }

            if (!string.IsNullOrWhiteSpace(preferredCulture.Parent?.Name))
                AddCandidate(preferredCulture.Parent.Name);
        }

        AddCandidate("en");
        return candidates;
    }
}
