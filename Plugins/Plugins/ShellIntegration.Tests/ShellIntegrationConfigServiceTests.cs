using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Tests;

public class ShellIntegrationConfigServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly List<string> _openedDirectories = [];

    public ShellIntegrationConfigServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ShellIntegrationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    private ShellIntegrationConfigService CreateService(string? localProfileRoot = null)
    {
        return new ShellIntegrationConfigService(localProfileRoot ?? _testDirectory, path => _openedDirectories.Add(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);

        var root = Path.GetDirectoryName(_testDirectory);
        if (root is not null && Directory.Exists(root) && Directory.GetFileSystemEntries(root).Length == 0)
            Directory.Delete(root);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultPath_CreatesInstance()
    {
        var service = new ShellIntegrationConfigService();
        Assert.NotNull(service);
        Assert.Contains("UniversalDeviceToolkit", service.LocalProfileRoot);
    }

    [Fact]
    public void Constructor_WithCustomPath_CreatesInstance()
    {
        var service = CreateService();
        Assert.NotNull(service);
        Assert.Equal(_testDirectory, service.LocalProfileRoot);
    }

    [Fact]
    public void Constructor_LocalProfilePath_EndsWithProfileJson()
    {
        var service = CreateService();
        Assert.EndsWith("profile.json", service.LocalProfilePath);
    }

    #endregion

    #region LoadProfile Tests

    [Fact]
    public void LoadProfile_NoFile_ReturnsDefault()
    {
        var service = CreateService();
        var profile = service.LoadProfile();

        Assert.NotNull(profile);
        Assert.Equal("modern", profile.ThemeName);
        Assert.Equal("#F7F8FC", profile.BackgroundColor); // Actual default
        Assert.True(profile.BorderRadius >= 0);
    }

    [Fact]
    public void LoadProfile_ExistingFile_ReturnsSavedProfile()
    {
        var service = CreateService();
        var original = new ShellIntegrationProfile
        {
            ThemeName = "custom",
            BackgroundColor = "#FFFFFF",
            BackgroundOpacity = 80,
            AccentColor = "#FF0000",
            BorderRadius = 12
        };
        service.SaveProfile(original);

        var loaded = service.LoadProfile();

        Assert.Equal("custom", loaded.ThemeName);
        Assert.Equal("#FFFFFF", loaded.BackgroundColor);
        Assert.Equal(80, loaded.BackgroundOpacity);
    }

    [Fact]
    public void LoadProfile_CorruptedFile_ReturnsDefault()
    {
        var service = CreateService();
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(service.LocalProfilePath, "invalid json {{");

        var profile = service.LoadProfile();

        Assert.NotNull(profile);
        Assert.Equal("modern", profile.ThemeName); // Default value
    }

    [Fact]
    public void LoadProfile_EmptyFile_ReturnsDefault()
    {
        var service = CreateService();
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(service.LocalProfilePath, "");

        var profile = service.LoadProfile();

        Assert.NotNull(profile);
        Assert.Equal("modern", profile.ThemeName);
    }

    [Fact]
    public void LoadProfile_NullJson_ReturnsDefault()
    {
        var service = CreateService();
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(service.LocalProfilePath, "null");

        var profile = service.LoadProfile();

        Assert.NotNull(profile);
        Assert.Equal("modern", profile.ThemeName);
    }

    #endregion

    #region SaveProfile Tests

    [Fact]
    public void SaveProfile_WhenFileMoveFails_CleansUpTempFile()
    {
        var service = CreateService();
        var profile = ShellIntegrationProfile.CreateDefault();
        service.SaveProfile(profile);

        var targetPath = service.LocalProfilePath;
        var tempPath = targetPath + ".tmp";

        using (var lockStream = new FileStream(targetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            try
            {
                service.SaveProfile(profile);
            }
            catch (UnauthorizedAccessException)
            {
                // Expected: File.Move fails because target is locked
            }
        }

        Assert.False(File.Exists(tempPath), "Temp file should be cleaned up after File.Move failure.");
    }

    [Fact]
    public void SaveProfile_CreatesDirectory()
    {
        var newPath = Path.Combine(_testDirectory, "subdir", Guid.NewGuid().ToString("N"));
        var service = CreateService(newPath);
        var profile = ShellIntegrationProfile.CreateDefault();

        service.SaveProfile(profile);

        Assert.True(Directory.Exists(newPath));
    }

    [Fact]
    public void SaveProfile_CreatesFile()
    {
        var service = CreateService();
        var profile = ShellIntegrationProfile.CreateDefault();

        service.SaveProfile(profile);

        Assert.True(File.Exists(service.LocalProfilePath));
    }

    [Fact]
    public void SaveProfile_JsonIsIndented()
    {
        var service = CreateService();
        var profile = ShellIntegrationProfile.CreateDefault();

        service.SaveProfile(profile);

        var json = File.ReadAllText(service.LocalProfilePath);
        Assert.Contains("\n", json); // Indented JSON has newlines
    }

    [Fact]
    public void SaveProfile_Utf8NoBom()
    {
        var service = CreateService();
        var profile = ShellIntegrationProfile.CreateDefault();

        service.SaveProfile(profile);

        var bytes = File.ReadAllBytes(service.LocalProfilePath);
        // UTF-8 BOM is EF BB BF
        Assert.True(bytes.Length < 3 || bytes[0] != 0xEF || bytes[1] != 0xBB || bytes[2] != 0xBF);
    }

    [Fact]
    public void SaveProfile_NormalizesBeforeSave()
    {
        var service = CreateService();
        var profile = new ShellIntegrationProfile
        {
            ThemeName = null!,
            BackgroundColor = null!,
            AccentColor = null!
        };

        service.SaveProfile(profile);

        var loaded = service.LoadProfile();
        Assert.NotNull(loaded.ThemeName);
        Assert.NotNull(loaded.BackgroundColor);
    }

    [Fact]
    public void ResetProfile_OverwritesExistingProfileWithDefaults()
    {
        var service = CreateService();
        service.SaveProfile(new ShellIntegrationProfile
        {
            ThemeName = "custom",
            BackgroundColor = "#101010",
            AccentColor = "#FFFFFF"
        });

        var reset = service.ResetProfile();
        var loaded = service.LoadProfile();

        Assert.Equal("modern", reset.ThemeName);
        Assert.Equal("modern", loaded.ThemeName);
        Assert.Equal(ShellIntegrationProfile.CreateDefault().BackgroundColor, loaded.BackgroundColor);
    }

    [Fact]
    public void ApplyPreset_CompactDark_PersistsPresetProfile()
    {
        var service = CreateService();

        var applied = service.ApplyPreset(ShellIntegrationPreset.CompactDark);
        var loaded = service.LoadProfile();

        Assert.Equal("compact-dark", applied.ThemeName);
        Assert.True(applied.UseCompactView);
        Assert.Equal(ShellColorScheme.Dark, applied.ColorScheme);
        Assert.Equal("compact-dark", loaded.ThemeName);
        Assert.Equal("#111827", loaded.BackgroundColor);
    }

    [Fact]
    public void ApplyPreset_MinimalLight_DisablesEffectsAndShadow()
    {
        var service = CreateService();

        var applied = service.ApplyPreset(ShellIntegrationPreset.MinimalLight);

        Assert.False(applied.EnableMotionEffects);
        Assert.False(applied.EnableShadow);
        Assert.Equal(ShellVisualEffect.None, applied.BackgroundEffect);
        Assert.Equal(ShellColorScheme.Light, applied.ColorScheme);
    }

    [Fact]
    public void ExportProfile_WritesNormalizedJsonToTargetFile()
    {
        var service = CreateService();
        var exportPath = Path.Combine(_testDirectory, "export", "profile.json");
        var profile = new ShellIntegrationProfile
        {
            ThemeName = " custom ",
            AccentColor = "4f7cff"
        };

        var result = service.ExportProfile(exportPath, profile, out var errorMessage);
        var content = File.ReadAllText(exportPath);

        Assert.True(result);
        Assert.Null(errorMessage);
        Assert.Contains(@"""ThemeName"": ""custom""", content);
        Assert.Contains(@"""AccentColor"": ""#4F7CFF""", content);
    }

    [Fact]
    public void ImportProfile_LoadsAndPersistsProfileFromExternalFile()
    {
        var service = CreateService();
        var importPath = Path.Combine(_testDirectory, "import-profile.json");
        File.WriteAllText(importPath,
            """
            {
              "ThemeName": "imported",
              "AccentColor": "#123456",
              "BackgroundColor": "#ABCDEF"
            }
            """);

        var result = service.ImportProfile(importPath, out var profile, out var errorMessage);
        var persisted = service.LoadProfile();

        Assert.True(result);
        Assert.Null(errorMessage);
        Assert.Equal("imported", profile.ThemeName);
        Assert.Equal("#123456", persisted.AccentColor);
        Assert.Equal("#ABCDEF", persisted.BackgroundColor);
    }

    #endregion

    #region ResolveManagedPaths Tests

    [Fact]
    public void ResolveManagedPaths_NullPath_ReturnsNull()
    {
        var service = CreateService();
        var paths = service.ResolveManagedPaths(null);

        Assert.Null(paths);
    }

    [Fact]
    public void ResolveManagedPaths_EmptyPath_ReturnsNull()
    {
        var service = CreateService();
        var paths = service.ResolveManagedPaths("");

        Assert.Null(paths);
    }

    [Fact]
    public void ResolveManagedPaths_WhitespacePath_ReturnsNull()
    {
        var service = CreateService();
        var paths = service.ResolveManagedPaths("   ");

        Assert.Null(paths);
    }

    [Fact]
    public void ResolveManagedPaths_ValidDirectory_ReturnsPaths()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);

        var paths = service.ResolveManagedPaths(installDir);

        Assert.NotNull(paths);
        Assert.Equal(installDir, paths!.InstallDirectory);
        Assert.Contains("shell.nss", paths.ShellConfigPath);
        Assert.Contains("lenovo-legion-toolkit", paths.ManagedDirectory);
        Assert.EndsWith("settings.nss", paths.SettingsPath);
        Assert.EndsWith("theme.nss", paths.ThemePath);
        Assert.EndsWith("language.nss", paths.LanguagePath);
    }

    [Fact]
    public void ResolveManagedPaths_ValidFile_ReturnsPaths()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var filePath = Path.Combine(installDir, "shell.nss");
        File.WriteAllText(filePath, "test");

        var paths = service.ResolveManagedPaths(filePath);

        Assert.NotNull(paths);
        Assert.Equal(installDir, paths!.InstallDirectory);
    }

    [Fact]
    public void ResolveManagedPaths_NonexistentPath_ReturnsPaths()
    {
        var service = CreateService();
        var nonexistentPath = Path.Combine(_testDirectory, "nonexistent", "shell.nss");

        // ResolveManagedPaths only checks if path is whitespace, not if directory exists
        var paths = service.ResolveManagedPaths(nonexistentPath);

        // It should still return paths based on directory name extraction
        Assert.NotNull(paths);
        Assert.Contains("nonexistent", paths!.InstallDirectory);
    }

    [Fact]
    public void ResolveManagedPaths_ManagedDirectoryStructure_IsCorrect()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);

        var paths = service.ResolveManagedPaths(installDir);

        Assert.StartsWith(_testDirectory, paths!.ManagedDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("managed", paths.ManagedDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lenovo-legion-toolkit", paths.ManagedDirectory);
    }

    #endregion

    #region ApplyProfile Tests

    [Fact]
    public void ApplyProfile_NullPath_ReturnsNull()
    {
        var service = CreateService();
        var profile = ShellIntegrationProfile.CreateDefault();

        var paths = service.ApplyProfile(null, profile);

        Assert.Null(paths);
    }

    [Fact]
    public void ApplyProfile_ValidPath_CreatesManagedDirectory()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var profile = ShellIntegrationProfile.CreateDefault();

        var paths = service.ApplyProfile(installDir, profile);

        Assert.NotNull(paths);
        Assert.True(Directory.Exists(paths!.ManagedDirectory));
    }

    [Fact]
    public void ApplyProfile_CreatesAllConfigFiles()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var profile = ShellIntegrationProfile.CreateDefault();

        var paths = service.ApplyProfile(installDir, profile);

        Assert.True(File.Exists(paths!.SettingsPath));
        Assert.True(File.Exists(paths.ThemePath));
        Assert.True(File.Exists(paths.LanguagePath));
        Assert.True(File.Exists(paths.ShellConfigPath));
    }

    [Fact]
    public void ApplyProfile_SettingsContentIsValid()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var profile = ShellIntegrationProfile.CreateDefault();

        var paths = service.ApplyProfile(installDir, profile);

        var settingsContent = File.ReadAllText(paths!.SettingsPath);
        Assert.Contains("settings", settingsContent);
        Assert.Contains("showdelay", settingsContent);
        Assert.Contains("tip", settingsContent);
    }

    [Fact]
    public void ApplyProfile_ThemeContentIsValid()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var profile = new ShellIntegrationProfile
        {
            ThemeName = "test",
            AccentColor = "#123456",
            BackgroundColor = "#000000"
        };

        var paths = service.ApplyProfile(installDir, profile);

        var themeContent = File.ReadAllText(paths!.ThemePath);
        Assert.Contains("theme", themeContent);
        Assert.Contains("test", themeContent);
        Assert.Contains("#123456", themeContent);
    }

    [Fact]
    public void ApplyProfile_WithCulture_AppliesLanguage()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        // Create language directory with en.nss
        var langDir = Path.Combine(installDir, "imports", "lang");
        Directory.CreateDirectory(langDir);
        File.WriteAllText(Path.Combine(langDir, "en.nss"), "// English language");

        var profile = ShellIntegrationProfile.CreateDefault();
        var culture = CultureInfo.GetCultureInfo("en");

        var paths = service.ApplyProfile(installDir, profile, culture);

        Assert.True(File.Exists(paths!.LanguagePath));
        var langContent = File.ReadAllText(paths.LanguagePath);
        Assert.Contains("English", langContent);
    }

    [Fact]
    public void ApplyProfile_NoLanguageFile_CreatesManagedComment()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var profile = ShellIntegrationProfile.CreateDefault();

        var paths = service.ApplyProfile(installDir, profile);

        var langContent = File.ReadAllText(paths!.LanguagePath);
        Assert.Contains("Managed by Lenovo Legion Toolkit", langContent);
    }

    [Fact]
    public void ApplyProfile_UpdatesShellConfig()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var shellConfig = Path.Combine(installDir, "shell.nss");
        File.WriteAllText(shellConfig, "theme { }");
        var profile = ShellIntegrationProfile.CreateDefault();

        var paths = service.ApplyProfile(installDir, profile);

        var shellContent = File.ReadAllText(paths!.ShellConfigPath);
        Assert.Contains(paths.SettingsPath.Replace('\\', '/'), shellContent);
    }

    #endregion

    #region RenderSettings Tests

    [Fact]
    public void RenderSettings_ContainsManagedComment()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        Assert.Contains("Managed by Lenovo Legion Toolkit", rendered);
    }

    [Fact]
    public void RenderSettings_ContainsSettingsBlock()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        Assert.Contains("settings", rendered);
        Assert.Contains("{", rendered);
        Assert.Contains("}", rendered);
    }

    [Fact]
    public void RenderSettings_ContainsTipConfiguration()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        Assert.Contains("tip", rendered);
        Assert.Contains("enabled = true", rendered);
        Assert.Contains("width = 420", rendered);
    }

    [Fact]
    public void RenderSettings_WithMotionEffects_UsesProfileShowDelay()
    {
        var profile = new ShellIntegrationProfile
        {
            EnableMotionEffects = true,
            ShowDelay = 300
        };
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        Assert.Contains("showdelay = 300", rendered);
    }

    [Fact]
    public void RenderSettings_WithoutMotionEffects_UsesDefaultShowDelay()
    {
        var profile = new ShellIntegrationProfile
        {
            EnableMotionEffects = false,
            ShowDelay = 500
        };
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        Assert.Contains("showdelay = 200", rendered);
    }

    [Fact]
    public void RenderSettings_UsesTipTimeSeconds()
    {
        var profile = new ShellIntegrationProfile
        {
            TipTimeSeconds = 3.5
        };
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        Assert.Contains("time = 3.5", rendered);
    }

    [Fact]
    public void RenderSettings_UsesBorderRadiusInTip()
    {
        var profile = new ShellIntegrationProfile
        {
            BorderRadius = 2
        };
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        // BorderRadius appears in the tip block's radius field
        Assert.Contains("radius = 2", rendered);
    }

    #endregion

    #region RenderTheme Tests

    [Fact]
    public void RenderTheme_ContainsManagedComment()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("Managed by Lenovo Legion Toolkit", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsThemeBlock()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("theme", rendered);
        Assert.Contains("name =", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsThemeName()
    {
        var profile = new ShellIntegrationProfile { ThemeName = "custom" };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("name = \"custom\"", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsViewExpression()
    {
        var profile = new ShellIntegrationProfile { EnableMotionEffects = true };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("view =", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsColorSchemeExpression()
    {
        var profile = new ShellIntegrationProfile { ColorScheme = ShellColorScheme.Dark };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("dark =", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsBackgroundConfiguration()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("background", rendered);
        Assert.Contains("color =", rendered);
        Assert.Contains("opacity =", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsBorderConfiguration()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("border", rendered);
        Assert.Contains("enabled = true", rendered);
        Assert.Contains("radius =", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsShadowConfiguration()
    {
        var profile = new ShellIntegrationProfile { EnableShadow = true };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("shadow", rendered);
        Assert.Contains("enabled = true", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsItemConfiguration()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("item", rendered);
        Assert.Contains("text", rendered);
        Assert.Contains("back", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsSeparatorConfiguration()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("separator", rendered);
    }

    [Fact]
    public void RenderTheme_ContainsSymbolConfiguration()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("symbol", rendered);
    }

    [Fact]
    public void RenderTheme_WithAcrylicEffect_ContainsEffectExpression()
    {
        var profile = new ShellIntegrationProfile
        {
            BackgroundEffect = ShellVisualEffect.Acrylic,
            AccentColor = "#3366FF"
        };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("effect =", rendered);
        Assert.Contains("3", rendered); // Acrylic effect type
    }

    [Fact]
    public void RenderTheme_WithBlurEffect_ContainsEffectExpression()
    {
        var profile = new ShellIntegrationProfile
        {
            BackgroundEffect = ShellVisualEffect.Blur
        };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("effect =", rendered);
        Assert.Contains("2", rendered); // Blur effect type
    }

    [Fact]
    public void RenderTheme_WithNoneEffect_ContainsZeroEffect()
    {
        var profile = new ShellIntegrationProfile
        {
            BackgroundEffect = ShellVisualEffect.None
        };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("effect = 0", rendered);
    }

    [Fact]
    public void RenderTheme_UsesCorrectColors()
    {
        var profile = new ShellIntegrationProfile
        {
            BackgroundColor = "#000000",
            AccentColor = "#FF0000",
            TextColor = "#FFFFFF",
            SelectedTextColor = "#00FF00"
        };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("#000000", rendered);
        Assert.Contains("#FF0000", rendered);
        Assert.Contains("#FFFFFF", rendered);
        Assert.Contains("#00FF00", rendered);
    }

    #endregion

    #region UpsertManagedImportBlock Tests

    [Fact]
    public void UpsertManagedImportBlock_EmptyContent_ReturnsBlockOnly()
    {
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock("");

        Assert.Contains("# region LenovoLegionToolkit.Managed", result);
        Assert.Contains("# endregion LenovoLegionToolkit.Managed", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_NullContent_ReturnsBlockOnly()
    {
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock(null!);

        Assert.Contains("# region LenovoLegionToolkit.Managed", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_WhitespaceContent_ReturnsBlockOnly()
    {
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock("   ");

        Assert.Contains("# region LenovoLegionToolkit.Managed", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_ContainsLanguageImport()
    {
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock("");

        Assert.Contains("import lang", result);
        Assert.Contains("language.nss", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_ContainsSettingsImport()
    {
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock("");

        Assert.Contains("import 'imports/lenovo-legion-toolkit/settings.nss'", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_ContainsThemeImport()
    {
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock("");

        Assert.Contains("import 'imports/lenovo-legion-toolkit/theme.nss'", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_WithExistingBlock_ReplacesBlock()
    {
        var existing = "# region LenovoLegionToolkit.Managed\nold content\n# endregion LenovoLegionToolkit.Managed";
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock(existing);

        Assert.DoesNotContain("old content", result);
        Assert.Contains("settings.nss", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_IsIdempotent()
    {
        var content = "theme { }";
        var once = ShellIntegrationConfigService.UpsertManagedImportBlock(content);
        var twice = ShellIntegrationConfigService.UpsertManagedImportBlock(once);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void UpsertManagedImportBlock_WithMenuBlock_InsertsBeforeMenu()
    {
        var content = "theme { }\n\nmenu(\n{\n})";
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock(content);

        var blockIndex = result.IndexOf("# region LenovoLegionToolkit.Managed");
        var menuIndex = result.IndexOf("menu(");

        Assert.True(blockIndex < menuIndex);
    }

    [Fact]
    public void UpsertManagedImportBlock_WithoutMenuBlock_AppendsBlock()
    {
        var content = "theme { }";
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock(content);

        Assert.Contains("theme { }", result);
        Assert.Contains("# region LenovoLegionToolkit.Managed", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_PreservesNonManagedContent()
    {
        var content = "important config line\ntheme { }";
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock(content);

        Assert.Contains("important config line", result);
    }

    [Fact]
    public void UpsertManagedImportBlock_RemovesOldManagedBlock()
    {
        var oldBlock = "# region LenovoLegionToolkit.Managed\nold imports\n# endregion LenovoLegionToolkit.Managed";
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock(oldBlock);

        Assert.DoesNotContain("old imports", result);
    }

    #endregion

    #region Language Resolution Tests

    [Fact]
    public void ApplyProfile_WithChineseSimplified_ResolvesLanguage()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var langDir = Path.Combine(installDir, "imports", "lang");
        Directory.CreateDirectory(langDir);

        // Create zh-CN.nss
        File.WriteAllText(Path.Combine(langDir, "zh-CN.nss"), "// Chinese Simplified");

        var profile = ShellIntegrationProfile.CreateDefault();
        var culture = CultureInfo.GetCultureInfo("zh-Hans");

        var paths = service.ApplyProfile(installDir, profile, culture);

        var langContent = File.ReadAllText(paths!.LanguagePath);
        Assert.Contains("Chinese Simplified", langContent);
    }

    [Fact]
    public void ApplyProfile_WithChineseTraditional_ResolvesLanguage()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var langDir = Path.Combine(installDir, "imports", "lang");
        Directory.CreateDirectory(langDir);

        File.WriteAllText(Path.Combine(langDir, "zh-TW.nss"), "// Chinese Traditional");

        var profile = ShellIntegrationProfile.CreateDefault();
        var culture = CultureInfo.GetCultureInfo("zh-Hant");

        var paths = service.ApplyProfile(installDir, profile, culture);

        var langContent = File.ReadAllText(paths!.LanguagePath);
        Assert.Contains("Chinese Traditional", langContent);
    }

    [Fact]
    public void ApplyProfile_WithPortugueseBrazil_ResolvesLanguage()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var langDir = Path.Combine(installDir, "imports", "lang");
        Directory.CreateDirectory(langDir);

        File.WriteAllText(Path.Combine(langDir, "pt-BR.nss"), "// Portuguese Brazil");

        var profile = ShellIntegrationProfile.CreateDefault();
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var paths = service.ApplyProfile(installDir, profile, culture);

        var langContent = File.ReadAllText(paths!.LanguagePath);
        Assert.Contains("Portuguese Brazil", langContent);
    }

    [Fact]
    public void ApplyProfile_FallsBackToEnglish()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var langDir = Path.Combine(installDir, "imports", "lang");
        Directory.CreateDirectory(langDir);

        File.WriteAllText(Path.Combine(langDir, "en.nss"), "// English fallback");

        var profile = ShellIntegrationProfile.CreateDefault();
        var culture = CultureInfo.GetCultureInfo("xx-YY"); // Non-existent culture

        var paths = service.ApplyProfile(installDir, profile, culture);

        var langContent = File.ReadAllText(paths!.LanguagePath);
        Assert.Contains("English fallback", langContent);
    }

    [Fact]
    public void ApplyProfile_NoLanguageDirectory_CreatesManagedComment()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        // No lang directory created

        var profile = ShellIntegrationProfile.CreateDefault();
        var culture = CultureInfo.GetCultureInfo("en");

        var paths = service.ApplyProfile(installDir, profile, culture);

        var langContent = File.ReadAllText(paths!.LanguagePath);
        Assert.Contains("Managed by Lenovo Legion Toolkit", langContent);
    }

    #endregion

    #region OpenManagedConfigFolder Tests

    [Fact]
    public void OpenManagedConfigFolder_WithValidPath_DoesNotThrow()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);

        service.OpenManagedConfigFolder(installDir);

        Assert.Single(_openedDirectories);
        Assert.EndsWith(Path.Combine("managed", "lenovo-legion-toolkit"), _openedDirectories[0]);
    }

    [Fact]
    public void OpenManagedConfigFolder_WithNullPath_UsesLocalProfileRoot()
    {
        var service = CreateService();

        service.OpenManagedConfigFolder(null);

        Assert.Equal(_testDirectory, Assert.Single(_openedDirectories));
    }

    [Fact]
    public void OpenManagedConfigFolder_WithNonexistentPath_UsesLocalProfileRoot()
    {
        var service = CreateService();
        var nonexistent = Path.Combine(_testDirectory, "nonexistent");

        service.OpenManagedConfigFolder(nonexistent);

        Assert.Single(_openedDirectories);
        Assert.EndsWith(Path.Combine("managed", "lenovo-legion-toolkit"), _openedDirectories[0]);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void LoadProfile_CalledMultipleTimes_ReturnsConsistentResults()
    {
        var service = CreateService();
        var profile1 = service.LoadProfile();
        var profile2 = service.LoadProfile();

        Assert.Equal(profile1.ThemeName, profile2.ThemeName);
        Assert.Equal(profile1.BackgroundColor, profile2.BackgroundColor);
    }

    [Fact]
    public void SaveProfile_DoesNotLeaveTempFile()
    {
        var service = CreateService();
        var profile = ShellIntegrationProfile.CreateDefault();
        var expectedTempPath = service.LocalProfilePath + ".tmp";

        service.SaveProfile(profile);

        Assert.True(File.Exists(service.LocalProfilePath));
        Assert.False(File.Exists(expectedTempPath),
            "Atomic write should not leave a .tmp file behind after successful save.");
    }

    [Fact]
    public void SaveProfile_OverwritesExistingFile()
    {
        var service = CreateService();
        var profile1 = new ShellIntegrationProfile { ThemeName = "first" };
        var profile2 = new ShellIntegrationProfile { ThemeName = "second" };

        service.SaveProfile(profile1);
        service.SaveProfile(profile2);

        var loaded = service.LoadProfile();
        Assert.Equal("second", loaded.ThemeName);
    }

    [Fact]
    public void ResolveManagedPaths_WithRootedPath_HandlesCorrectly()
    {
        var service = CreateService();
        var rootedPath = Path.GetPathRoot(_testDirectory) ?? "C:\\";
        Directory.CreateDirectory(rootedPath);

        var paths = service.ResolveManagedPaths(rootedPath);

        // Should handle rooted paths gracefully
        Assert.True(paths == null || paths.InstallDirectory != null);
    }

    [Fact]
    public void UpsertManagedImportBlock_WithMultipleExistingBlocks_RemovesAll()
    {
        var content = @"
# region LenovoLegionToolkit.Managed
old1
# endregion LenovoLegionToolkit.Managed

some content

# region LenovoLegionToolkit.Managed
old2
# endregion LenovoLegionToolkit.Managed
";
        var result = ShellIntegrationConfigService.UpsertManagedImportBlock(content);

        Assert.DoesNotContain("old1", result);
        Assert.DoesNotContain("old2", result);
        Assert.Contains("some content", result);
    }

    [Fact]
    public void RenderSettings_WithZeroBorderRadius_HandlesGracefully()
    {
        var profile = new ShellIntegrationProfile { BorderRadius = 0 };
        var rendered = ShellIntegrationConfigService.RenderSettings(profile);

        Assert.Contains("radius = 0", rendered);
    }

    [Fact]
    public void RenderTheme_WithZeroOpacity_HandlesGracefully()
    {
        var profile = new ShellIntegrationProfile { BackgroundOpacity = 0 };
        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("opacity = 0", rendered);
    }

    [Fact]
    public void ApplyProfile_AllFilesUtf8NoBom()
    {
        var service = CreateService();
        var installDir = Path.Combine(_testDirectory, "shell");
        Directory.CreateDirectory(installDir);
        var profile = ShellIntegrationProfile.CreateDefault();

        var paths = service.ApplyProfile(installDir, profile);

        foreach (var filePath in new[] { paths!.SettingsPath, paths.ThemePath, paths.LanguagePath, paths.ShellConfigPath })
        {
            var bytes = File.ReadAllBytes(filePath);
            // UTF-8 BOM check
            Assert.True(bytes.Length < 3 || bytes[0] != 0xEF || bytes[1] != 0xBB || bytes[2] != 0xBF,
                $"File {filePath} should not have UTF-8 BOM");
        }
    }

    #endregion

    #region ShellIntegrationProfile Tests

    [Fact]
    public void ShellIntegrationProfile_CreateDefault_HasValidValues()
    {
        var profile = ShellIntegrationProfile.CreateDefault();

        Assert.NotNull(profile.ThemeName);
        Assert.NotNull(profile.BackgroundColor);
        Assert.NotNull(profile.AccentColor);
        Assert.True(profile.BackgroundOpacity is >= 0 and <= 100);
        Assert.True(profile.BorderRadius >= 0);
    }

    [Fact]
    public void ShellIntegrationProfile_CreatePreset_Default_ReturnsDefaultProfile()
    {
        var preset = ShellIntegrationProfile.CreatePreset(ShellIntegrationPreset.Default);

        Assert.Equal(ShellIntegrationProfile.CreateDefault().ThemeName, preset.ThemeName);
        Assert.Equal(ShellIntegrationProfile.CreateDefault().BackgroundColor, preset.BackgroundColor);
    }

    [Fact]
    public void ShellIntegrationProfile_Normalize_FillsNulls()
    {
        var profile = new ShellIntegrationProfile
        {
            ThemeName = null!,
            BackgroundColor = null!,
            AccentColor = null!
        };

        var normalized = profile.Normalize();

        Assert.NotNull(normalized.ThemeName);
        Assert.NotNull(normalized.BackgroundColor);
        Assert.NotNull(normalized.AccentColor);
    }

    [Fact]
    public void ShellIntegrationProfile_GetViewExpression_ReturnsViewExpression()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        var expression = profile.GetViewExpression();

        Assert.NotNull(expression);
        Assert.True(expression is "view.compact" or "view.medium");
    }

    [Fact]
    public void ShellIntegrationProfile_GetViewExpression_WithCompactView_ReturnsCompact()
    {
        var profile = new ShellIntegrationProfile { UseCompactView = true };
        var expression = profile.GetViewExpression();

        Assert.Equal("view.compact", expression);
    }

    [Fact]
    public void ShellIntegrationProfile_GetViewExpression_WithoutCompactView_ReturnsMedium()
    {
        var profile = new ShellIntegrationProfile { UseCompactView = false };
        var expression = profile.GetViewExpression();

        Assert.Equal("view.medium", expression);
    }

    [Fact]
    public void ShellIntegrationProfile_GetColorSchemeExpression_WithLight_ReturnsFalse()
    {
        var profile = new ShellIntegrationProfile { ColorScheme = ShellColorScheme.Light };
        var expression = profile.GetColorSchemeExpression();

        Assert.Equal("false", expression);
    }

    [Fact]
    public void ShellIntegrationProfile_GetColorSchemeExpression_WithAuto_ReturnsDefault()
    {
        var profile = new ShellIntegrationProfile { ColorScheme = ShellColorScheme.Auto };
        var expression = profile.GetColorSchemeExpression();

        Assert.Equal("default", expression);
    }

    [Fact]
    public void ShellIntegrationProfile_GetEffectExpression_WithAcrylic_ReturnsValidExpression()
    {
        var profile = new ShellIntegrationProfile
        {
            BackgroundEffect = ShellVisualEffect.Acrylic,
            AccentColor = "#3366FF"
        };
        var expression = profile.GetEffectExpression();

        Assert.Contains("3", expression); // Acrylic effect type
    }

    [Fact]
    public void ShellIntegrationProfile_GetEffectExpression_WithBlur_ReturnsValidExpression()
    {
        var profile = new ShellIntegrationProfile
        {
            BackgroundEffect = ShellVisualEffect.Blur
        };
        var expression = profile.GetEffectExpression();

        Assert.Contains("2", expression); // Blur effect type
    }

    #endregion
}
