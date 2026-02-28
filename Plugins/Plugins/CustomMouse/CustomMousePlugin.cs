using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Plugins.SDK;
using Microsoft.Win32;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

[Plugin(
    id: "custom-mouse",
    name: "Custom Mouse",
    version: "1.0.6",
    description: "Customize mouse cursor style behavior and mouse settings",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "3.6.1",
    Icon = "Mouse24"
)]
public class CustomMousePlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    private enum CursorTheme
    {
        Light,
        Dark
    }

    private const uint SpiSetMouseButtonSwap = 0x0021;
    private const uint SpiSetMouseSpeed = 0x0071;
    private const uint SpiSetCursors = 0x0057;
    private const uint SpifUpdateIniFile = 0x0001;
    private const uint SpifSendChange = 0x0002;

    private const string CursorRegistryPath = @"Control Panel\\Cursors";
    private const string CursorSchemesRegistryPath = @"Control Panel\\Cursors\\Schemes";
    private const string CursorBackupSavedFlag = "CursorBackupSaved";

    private static readonly (string Key, string FileName)[] CursorSchemeOrder =
    [
        ("Arrow", "Pointer.cur"),
        ("Help", "Help.cur"),
        ("AppStarting", "Working.ani"),
        ("Wait", "Busy.ani"),
        ("crosshair", "Precision.cur"),
        ("IBeam", "Beam.cur"),
        ("NWPen", "Handwriting.cur"),
        ("No", "Unavailable.cur"),
        ("SizeNS", "Vert.cur"),
        ("SizeWE", "Horz.cur"),
        ("SizeNWSE", "Dgn1.cur"),
        ("SizeNESW", "Dgn2.cur"),
        ("SizeAll", "Move.cur"),
        ("UpArrow", "Alternate.cur"),
        ("Hand", "Link.cur"),
        ("Person", "Person.cur"),
        ("Pin", "Pin.cur")
    ];

    private static readonly string[] AdditionalCursorKeys =
    [
        "precisionhair"
    ];

    public override string Id => "custom-mouse";
    public override string Name => CustomMouseText.PluginName;
    public override string Description => CustomMouseText.PluginDescription;
    public override string Icon => "Mouse24";
    public override bool IsSystemPlugin => false;

    private MouseSettings _settings;

    public MouseSettings Settings => _settings;

    public CustomMousePlugin()
    {
        _settings = LoadSettings();
    }

    public override object? GetFeatureExtension()
    {
        return null;
    }

    public override object? GetSettingsPage()
    {
        return new CustomMouseSettingsPluginPage(this);
    }

    public override WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
    {
        return new WindowsOptimizationCategoryDefinition(
            "custom.mouse",
            "WindowsOptimization_Category_CustomMouse_Title",
            "WindowsOptimization_Category_CustomMouse_Description",
            new[]
            {
                new WindowsOptimizationActionDefinition(
                    "custom.mouse.cursor.auto-theme.enable",
                    "WindowsOptimization_Action_CustomMouse_AutoTheme_Enable_Title",
                    "WindowsOptimization_Action_CustomMouse_AutoTheme_Enable_Description",
                    EnableAutoThemeCursorStyleAsync,
                    Recommended: true,
                    IsAppliedAsync: IsAutoThemeCursorStyleEnabledAsync),
                new WindowsOptimizationActionDefinition(
                    "custom.mouse.cursor.auto-theme.disable",
                    "WindowsOptimization_Action_CustomMouse_AutoTheme_Disable_Title",
                    "WindowsOptimization_Action_CustomMouse_AutoTheme_Disable_Description",
                    DisableAutoThemeCursorStyleAsync,
                    Recommended: false,
                    IsAppliedAsync: async ct => !await IsAutoThemeCursorStyleEnabledAsync(ct).ConfigureAwait(false))
            },
            Id);
    }

    public override void OnInstalled()
    {
        _settings = MouseSettings.CreateDefault();
        _ = SaveSettingsAsync();
    }

    public override void OnUninstalled()
    {
        _settings.AutoThemeCursorStyle = false;
        _ = SaveSettingsAsync();
        _ = RestoreBackedUpCursorSchemeAsync(CancellationToken.None);
    }

    public bool SetDpi(int dpi)
    {
        if (dpi < 100 || dpi > 16000)
            return false;

        _settings.Dpi = dpi;
        return true;
    }

    public bool SetPollingRate(int rate)
    {
        var validRates = new[] { 125, 250, 500, 1000 };
        if (Array.IndexOf(validRates, rate) < 0)
            return false;

        _settings.PollingRate = rate;
        return true;
    }

    public bool SetWindowsPointerSpeed(int speed)
    {
        if (speed < 1 || speed > 20)
            return false;

        if (!SystemParametersInfo(SpiSetMouseSpeed, 0, new IntPtr(speed), SpifUpdateIniFile | SpifSendChange))
            return false;

        _settings.WindowsPointerSpeed = speed;
        return true;
    }

    public bool SetSwapButtons(bool swapButtons)
    {
        if (!SystemParametersInfo(SpiSetMouseButtonSwap, swapButtons ? 1u : 0u, IntPtr.Zero, SpifSendChange))
            return false;

        _settings.SwapButtons = swapButtons;
        return true;
    }

    public bool SetAutoThemeCursorStyle(bool enabled)
    {
        _settings.AutoThemeCursorStyle = enabled;
        return true;
    }

    public async Task<bool> ApplyCursorStyleForCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var theme = IsSystemLightTheme() ? CursorTheme.Light : CursorTheme.Dark;

            if (!TryApplyCursorThemeWithInf(theme, cancellationToken))
                ApplyCursorThemeFromResources(theme);

            _settings.LastAppliedTheme = theme == CursorTheme.Light ? "light" : "dark";
            await SaveSettingsAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task SaveSettingsAsync()
    {
        Configuration.SetValue(nameof(MouseSettings.Dpi), _settings.Dpi);
        Configuration.SetValue(nameof(MouseSettings.PollingRate), _settings.PollingRate);
        Configuration.SetValue(nameof(MouseSettings.WindowsPointerSpeed), _settings.WindowsPointerSpeed);
        Configuration.SetValue(nameof(MouseSettings.SwapButtons), _settings.SwapButtons);
        Configuration.SetValue(nameof(MouseSettings.AutoThemeCursorStyle), _settings.AutoThemeCursorStyle);
        Configuration.SetValue(nameof(MouseSettings.LastAppliedTheme), _settings.LastAppliedTheme ?? string.Empty);
        await Configuration.SaveAsync().ConfigureAwait(false);
    }

    private MouseSettings LoadSettings()
    {
        return new MouseSettings
        {
            Dpi = Configuration.GetValue(nameof(MouseSettings.Dpi), 1600),
            PollingRate = Configuration.GetValue(nameof(MouseSettings.PollingRate), 1000),
            WindowsPointerSpeed = Configuration.GetValue(nameof(MouseSettings.WindowsPointerSpeed), 10),
            SwapButtons = Configuration.GetValue(nameof(MouseSettings.SwapButtons), false),
            AutoThemeCursorStyle = Configuration.GetValue(nameof(MouseSettings.AutoThemeCursorStyle), true),
            LastAppliedTheme = Configuration.GetValue(nameof(MouseSettings.LastAppliedTheme), string.Empty),
            ButtonMappings = new Dictionary<int, int>()
        };
    }

    private async Task EnableAutoThemeCursorStyleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _settings.AutoThemeCursorStyle = true;
        await SaveSettingsAsync().ConfigureAwait(false);

        if (!await ApplyCursorStyleForCurrentThemeAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Failed to apply custom mouse cursor style.");
    }

    private async Task DisableAutoThemeCursorStyleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _settings.AutoThemeCursorStyle = false;
        await SaveSettingsAsync().ConfigureAwait(false);

        await RestoreBackedUpCursorSchemeAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task<bool> IsAutoThemeCursorStyleEnabledAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_settings.AutoThemeCursorStyle);
    }

    private bool TryApplyCursorThemeWithInf(CursorTheme theme, CancellationToken cancellationToken)
    {
        try
        {
            var infPath = GetInstallInfPath(theme);
            if (!File.Exists(infPath))
                return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"setupapi.dll,InstallHinfSection DefaultInstall 132 \"{infPath}\"",
                WorkingDirectory = Path.GetDirectoryName(infPath) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            process.WaitForExit(15000);
            cancellationToken.ThrowIfCancellationRequested();

            if (!process.HasExited || process.ExitCode != 0)
                return false;

            ApplySystemCursorRefresh();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ApplyCursorThemeFromResources(CursorTheme theme)
    {
        BackupCurrentCursorSchemeIfNeeded();

        var basePath = GetBaseCursorPath(theme);
        var animationPath = GetAnimationCursorPath(theme);
        var schemeName = theme == CursorTheme.Light ? "LLT Custom Mouse Light" : "LLT Custom Mouse Dark";

        EnsureCursorResourcesExist(basePath, animationPath);

        using var cursorKey = Registry.CurrentUser.CreateSubKey(CursorRegistryPath, true)
                             ?? throw new InvalidOperationException("Failed to open cursor registry path.");
        using var schemesKey = Registry.CurrentUser.CreateSubKey(CursorSchemesRegistryPath, true)
                              ?? throw new InvalidOperationException("Failed to open cursor schemes registry path.");

        var schemeEntries = new List<string>(CursorSchemeOrder.Length);
        foreach (var (key, fileName) in CursorSchemeOrder)
        {
            var path = ResolveCursorFilePath(basePath, animationPath, fileName);
            cursorKey.SetValue(key, path, RegistryValueKind.ExpandString);
            schemeEntries.Add(path);
        }

        var precisionPath = Path.Combine(basePath, "Precision.cur");
        foreach (var additionalKey in AdditionalCursorKeys)
            cursorKey.SetValue(additionalKey, precisionPath, RegistryValueKind.ExpandString);

        cursorKey.SetValue(string.Empty, schemeName, RegistryValueKind.String);
        schemesKey.SetValue(schemeName, string.Join(",", schemeEntries), RegistryValueKind.ExpandString);

        ApplySystemCursorRefresh();
    }

    private async Task RestoreBackedUpCursorSchemeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Configuration.GetValue(CursorBackupSavedFlag, false))
            return;

        using var cursorKey = Registry.CurrentUser.CreateSubKey(CursorRegistryPath, true)
                             ?? throw new InvalidOperationException("Failed to open cursor registry path.");

        var defaultValue = Configuration.GetValue(GetBackupConfigKey(string.Empty), string.Empty);
        cursorKey.SetValue(string.Empty, defaultValue, RegistryValueKind.String);

        foreach (var (key, _) in CursorSchemeOrder)
        {
            var backupValue = Configuration.GetValue(GetBackupConfigKey(key), string.Empty);
            cursorKey.SetValue(key, backupValue, RegistryValueKind.ExpandString);
        }

        foreach (var additionalKey in AdditionalCursorKeys)
        {
            var backupValue = Configuration.GetValue(GetBackupConfigKey(additionalKey), string.Empty);
            cursorKey.SetValue(additionalKey, backupValue, RegistryValueKind.ExpandString);
        }

        ApplySystemCursorRefresh();
        await Task.CompletedTask;
    }

    private void BackupCurrentCursorSchemeIfNeeded()
    {
        if (Configuration.GetValue(CursorBackupSavedFlag, false))
            return;

        using var cursorKey = Registry.CurrentUser.OpenSubKey(CursorRegistryPath, false);
        if (cursorKey == null)
            return;

        Configuration.SetValue(GetBackupConfigKey(string.Empty), Convert.ToString(cursorKey.GetValue(string.Empty)) ?? string.Empty);
        foreach (var (key, _) in CursorSchemeOrder)
        {
            Configuration.SetValue(GetBackupConfigKey(key), Convert.ToString(cursorKey.GetValue(key)) ?? string.Empty);
        }

        foreach (var additionalKey in AdditionalCursorKeys)
        {
            Configuration.SetValue(GetBackupConfigKey(additionalKey), Convert.ToString(cursorKey.GetValue(additionalKey)) ?? string.Empty);
        }

        Configuration.SetValue(CursorBackupSavedFlag, true);
        _ = Configuration.SaveAsync();
    }

    private static string GetBackupConfigKey(string registryValueName)
    {
        if (string.IsNullOrEmpty(registryValueName))
            return "CursorBackup_Default";

        return $"CursorBackup_{registryValueName}";
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var personalizeKey = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", false);
            var value = personalizeKey?.GetValue("AppsUseLightTheme");
            return value is int intValue ? intValue != 0 : true;
        }
        catch
        {
            return true;
        }
    }

    private string GetInstallInfPath(CursorTheme theme)
    {
        var themeName = theme == CursorTheme.Light ? "Light" : "Dark";
        return Path.Combine(
            GetResourceRoot(),
            "W11-CC-V2.2-HDPI",
            themeName,
            "Regular",
            "02. classic",
            "Install.inf");
    }

    private string GetBaseCursorPath(CursorTheme theme)
    {
        var themeName = theme == CursorTheme.Light ? "Light" : "Dark";
        return Path.Combine(
            GetResourceRoot(),
            "W11-CC-V2.2-HDPI",
            themeName,
            "Regular",
            "Base");
    }

    private string GetAnimationCursorPath(CursorTheme theme)
    {
        var themeName = theme == CursorTheme.Light ? "Light" : "Dark";
        var candidate = Path.Combine(
            GetResourceRoot(),
            "W11-CC-V2.2-HDPI",
            themeName,
            "Regular",
            "02. classic");

        if (Directory.Exists(candidate))
            return candidate;

        return Path.Combine(
            GetResourceRoot(),
            "W11-CC-V2.2-HDPI",
            "Dark",
            "Regular",
            "02. classic");
    }

    private string GetResourceRoot()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(CustomMousePlugin).Assembly.Location) ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDirectory, "Resources");
    }

    private static string ResolveCursorFilePath(string basePath, string animationPath, string fileName)
    {
        if (fileName.EndsWith(".ani", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(animationPath, fileName);

        return Path.Combine(basePath, fileName);
    }

    private static void EnsureCursorResourcesExist(string basePath, string animationPath)
    {
        foreach (var (_, fileName) in CursorSchemeOrder)
        {
            var fullPath = ResolveCursorFilePath(basePath, animationPath, fileName);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Cursor resource file not found: {fullPath}");
        }
    }

    private static void ApplySystemCursorRefresh()
    {
        SystemParametersInfo(SpiSetCursors, 0, IntPtr.Zero, SpifSendChange);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
}

public class CustomMousePluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    private readonly CustomMousePlugin _plugin;

    public CustomMousePluginPage(CustomMousePlugin plugin)
    {
        _plugin = plugin;
    }

    public string PageTitle => CustomMouseText.PageTitle;
    public string? PageIcon => "Mouse24";

    public object CreatePage()
    {
        return new CustomMouseControl(_plugin);
    }
}

public class CustomMouseSettingsPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    private readonly CustomMousePlugin _plugin;

    public CustomMouseSettingsPluginPage(CustomMousePlugin plugin)
    {
        _plugin = plugin;
    }

    public string PageTitle => CustomMouseText.SettingsPageTitle;
    public string? PageIcon => "Settings24";

    public object CreatePage()
    {
        return new CustomMouseSettingsControl(_plugin);
    }
}

public class MouseSettings
{
    public int Dpi { get; set; } = 1600;
    public int PollingRate { get; set; } = 1000;
    public int WindowsPointerSpeed { get; set; } = 10;
    public bool SwapButtons { get; set; }
    public bool AutoThemeCursorStyle { get; set; } = true;
    public string? LastAppliedTheme { get; set; }
    public Dictionary<int, int> ButtonMappings { get; set; } = new();

    public static MouseSettings CreateDefault()
    {
        return new MouseSettings
        {
            Dpi = 1600,
            PollingRate = 1000,
            WindowsPointerSpeed = 10,
            SwapButtons = false,
            AutoThemeCursorStyle = true,
            LastAppliedTheme = string.Empty,
            ButtonMappings = new Dictionary<int, int>()
        };
    }
}
