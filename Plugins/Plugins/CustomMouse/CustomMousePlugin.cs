using System;
using System.Collections.Generic;
using System.Linq;
#nullable enable

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.Shared;
using Microsoft.Win32;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

[Plugin(
    id: "custom-mouse",
    name: "Custom Mouse",
    version: "1.0.16",
    description: "Customize mouse cursor style behavior and mouse settings",
    author: "SSC-STUDIO",
    MinimumHostVersion = "3.6.1",
    Icon = "Pen24"
)]
public class CustomMousePlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase, IAppStartupPlugin
{
    internal const string WindowsDefaultCursorSchemeName = "Windows Aero";

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

    private const string CursorRegistryPath = @"Control Panel\Cursors";
    private const string CursorSchemesRegistryPath = @"Control Panel\Cursors\Schemes";
    private const string CursorBackupSavedFlag = "CursorBackupSaved";
    private const string PluginsDirectoryOverrideEnvironmentVariable = "LLT_PLUGIN_DIRECTORY_OVERRIDE";
    private const string ResourcesDirectoryName = "Resources";
    private const string CursorThemeResourceDirectoryName = "W11-CC-V2.2-HDPI";

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
    public override string Icon => "Pen24";
    public override bool IsSystemPlugin => false;

    private MouseSettings _settings;
    private readonly ThemeWatcherRuntime _themeWatcher = new();

    public MouseSettings Settings => _settings;

    public CustomMousePlugin()
    {
        PluginLog.Configure(
            isTraceEnabled: () => Log.Instance.IsTraceEnabled,
            trace: (message, exception) => Log.Instance.Trace(message, exception));

        _settings = LoadSettings();
        NormalizeCursorThemeSettings();
    }

    public void OnAppStarted()
    {
        if (_settings.CursorThemeMode != CursorThemeMode.Auto)
        {
            return;
        }

        StartThemeWatcher();
        if (!IsCurrentThemeAlreadyApplied())
        {
            RunBackgroundTask(nameof(OnAppStarted), () => ApplyCursorStyleForCurrentThemeAsync(GetRuntimeCancellationToken()));
        }
    }

    public override object? GetFeatureExtension() => null;

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
        RunLifecycleTask(nameof(OnInstalled), SaveSettingsAsync);
    }

    public override void OnUninstalled()
    {
        StopThemeWatcher();
        _settings.AutoThemeCursorStyle = false;
        _settings.CursorThemeMode = CursorThemeMode.Auto;
        RunLifecycleTask(nameof(OnUninstalled), SaveSettingsAsync);
        RunLifecycleTask(nameof(OnUninstalled), () => RestoreBackedUpCursorSchemeAsync(CancellationToken.None));
    }

    public override void OnShutdown()
    {
        StopThemeWatcher();
    }

    public override void Stop()
    {
        StopThemeWatcher();
    }

    protected override CancellationToken GetRuntimeCancellationToken()
    {
        return _themeWatcher.GetCancellationToken();
    }

    public bool SetDpi(int dpi)
    {
        if (dpi is < 100 or > 16000)
        {
            return false;
        }

        _settings.Dpi = dpi;
        return true;
    }

    public bool SetPollingRate(int rate)
    {
        var validRates = new[] { 125, 250, 500, 1000 };
        if (Array.IndexOf(validRates, rate) < 0)
        {
            return false;
        }

        _settings.PollingRate = rate;
        return true;
    }

    public bool SetWindowsPointerSpeed(int speed)
    {
        if (speed is < 1 or > 20)
        {
            return false;
        }

        if (!SystemParametersInfo(SpiSetMouseSpeed, 0, new IntPtr(speed), SpifUpdateIniFile | SpifSendChange))
        {
            return false;
        }

        _settings.WindowsPointerSpeed = speed;
        return true;
    }

    public bool SetSwapButtons(bool swapButtons)
    {
        if (!SystemParametersInfo(SpiSetMouseButtonSwap, swapButtons ? 1u : 0u, IntPtr.Zero, SpifSendChange))
        {
            return false;
        }

        _settings.SwapButtons = swapButtons;
        return true;
    }

    public bool SetAutoThemeCursorStyle(bool enabled)
    {
        _settings.AutoThemeCursorStyle = enabled;
        _settings.CursorThemeMode = enabled ? CursorThemeMode.Auto : GetExplicitCursorThemeMode();

        if (enabled)
        {
            StartThemeWatcher();
        }
        else
        {
            StopThemeWatcher();
        }

        return true;
    }

    public async Task<bool> SetCursorThemeModeAsync(CursorThemeMode mode)
    {
        var previousMode = _settings.CursorThemeMode;
        var previousAutoThemeCursorStyle = _settings.AutoThemeCursorStyle;
        var previousLastAppliedTheme = _settings.LastAppliedTheme;

        _settings.CursorThemeMode = mode;
        _settings.AutoThemeCursorStyle = mode == CursorThemeMode.Auto;

        bool applied;
        if (mode == CursorThemeMode.Auto)
        {
            StartThemeWatcher();
            applied = await ApplyCursorStyleForCurrentThemeAsync().ConfigureAwait(false);
        }
        else
        {
            StopThemeWatcher();
            applied = await ApplySpecificCursorThemeAsync(mode == CursorThemeMode.Light ? CursorTheme.Light : CursorTheme.Dark).ConfigureAwait(false);
        }

        if (applied)
        {
            return true;
        }

        _settings.CursorThemeMode = previousMode;
        _settings.AutoThemeCursorStyle = previousAutoThemeCursorStyle;
        _settings.LastAppliedTheme = previousLastAppliedTheme;

        if (previousAutoThemeCursorStyle)
        {
            StartThemeWatcher();
        }
        else
        {
            StopThemeWatcher();
        }

        return false;
    }

    public async Task<bool> RestoreWindowsDefaultCursorThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var restored = RestoreCursorScheme(WindowsDefaultCursorSchemeName);
            if (!restored)
            {
                return false;
            }

            _settings.AutoThemeCursorStyle = false;
            _settings.CursorThemeMode = CursorThemeMode.WindowsDefault;
            _settings.LastAppliedTheme = string.Empty;
            StopThemeWatcher();
            await SaveSettingsAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void ReloadSettingsFromSystem()
    {
        try
        {
            _settings.WindowsPointerSpeed = ReadCurrentWindowsPointerSpeed();
            _settings.SwapButtons = ReadCurrentSwapButtons();

            var detectedMode = DetectCurrentCursorThemeMode();
            _settings.CursorThemeMode = detectedMode;
            _settings.AutoThemeCursorStyle = detectedMode == CursorThemeMode.Auto;

            _settings.LastAppliedTheme = detectedMode switch
            {
                CursorThemeMode.Light => "light",
                CursorThemeMode.Dark => "dark",
                _ => string.Empty
            };
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"CustomMouse: Failed to reload system settings: {ex.Message}", ex);
        }
    }

    private async Task<bool> ApplySpecificCursorThemeAsync(CursorTheme theme, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await TryApplyCursorThemeWithInfAsync(theme, cancellationToken).ConfigureAwait(false))
            {
                ApplyCursorThemeFromResources(theme);
            }

            _settings.LastAppliedTheme = theme == CursorTheme.Light ? "light" : "dark";
            await SaveSettingsAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"CustomMouse: Failed to apply cursor theme '{theme}': {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> ApplyCursorStyleForCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var theme = IsSystemLightTheme() ? CursorTheme.Light : CursorTheme.Dark;

            if (!await TryApplyCursorThemeWithInfAsync(theme, cancellationToken).ConfigureAwait(false))
            {
                ApplyCursorThemeFromResources(theme);
            }

            _settings.LastAppliedTheme = theme == CursorTheme.Light ? "light" : "dark";
            await SaveSettingsAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"CustomMouse: Failed to apply current cursor theme: {ex.Message}", ex);
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
        Configuration.SetValue(nameof(MouseSettings.CursorThemeMode), (int)_settings.CursorThemeMode);
        Configuration.SetValue(nameof(MouseSettings.LastAppliedTheme), _settings.LastAppliedTheme ?? string.Empty);
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await Configuration.SaveAsync().ConfigureAwait(false);
                return;
            }
            catch (System.IO.IOException) when (attempt < maxRetries - 1)
            {
                await System.Threading.Tasks.Task.Delay(50 * (attempt + 1)).ConfigureAwait(false);
            }
        }
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
            CursorThemeMode = (CursorThemeMode)Configuration.GetValue(nameof(MouseSettings.CursorThemeMode), (int)CursorThemeMode.Auto),
            LastAppliedTheme = Configuration.GetValue(nameof(MouseSettings.LastAppliedTheme), string.Empty),
            ButtonMappings = new Dictionary<int, int>()
        };
    }

    private void StartThemeWatcher()
    {
        _themeWatcher.ThemeChanged -= OnThemeChangedAsync;
        _themeWatcher.ThemeChanged += OnThemeChangedAsync;
        _themeWatcher.Start(_settings.LastAppliedTheme);
    }

    private void StopThemeWatcher()
    {
        _themeWatcher.Stop();
        _themeWatcher.ThemeChanged -= OnThemeChangedAsync;
    }

    private async Task OnThemeChangedAsync(string newTheme, CancellationToken cancellationToken)
    {
        if (_settings.CursorThemeMode != CursorThemeMode.Auto)
        {
            return;
        }

        await ApplyCursorStyleForCurrentThemeAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnableAutoThemeCursorStyleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _settings.AutoThemeCursorStyle = true;
        _settings.CursorThemeMode = CursorThemeMode.Auto;
        StartThemeWatcher();
        await SaveSettingsAsync().ConfigureAwait(false);

        if (!await ApplyCursorStyleForCurrentThemeAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(CustomMouseText.StatusCursorApplyFailed);
        }
    }

    private async Task DisableAutoThemeCursorStyleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _settings.AutoThemeCursorStyle = false;
        _settings.CursorThemeMode = GetExplicitCursorThemeMode();
        StopThemeWatcher();
        await SaveSettingsAsync().ConfigureAwait(false);

        await RestoreBackedUpCursorSchemeAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task<bool> IsAutoThemeCursorStyleEnabledAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_settings.AutoThemeCursorStyle);
    }

    private async Task<bool> TryApplyCursorThemeWithInfAsync(CursorTheme theme, CancellationToken cancellationToken)
    {
        try
        {
            var infPath = GetInstallInfPath(theme);
            if (!File.Exists(infPath))
            {
                return false;
            }

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
            {
                return false;
            }

            var waitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            var completedTask = await Task.WhenAny(waitTask, timeoutTask).ConfigureAwait(false);
            if (completedTask != waitTask)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore cleanup failures and report timeout below.
                }

                return false;
            }

            await waitTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (!process.HasExited || process.ExitCode != 0)
            {
                return false;
            }

            ApplySystemCursorRefresh();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            PluginLog.Trace($"CustomMouse: INF installation failed: {ex.Message}", ex);
            return false;
        }
    }

    private void ApplyCursorThemeFromResources(CursorTheme theme)
    {
        BackupCurrentCursorSchemeIfNeeded();

        var basePath = GetBaseCursorPath(theme);
        var animationPath = GetAnimationCursorPath(theme);
        var schemeName = theme == CursorTheme.Light ? "UDT Custom Mouse Light" : "UDT Custom Mouse Dark";

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
        {
            cursorKey.SetValue(additionalKey, precisionPath, RegistryValueKind.ExpandString);
        }

        cursorKey.SetValue(string.Empty, schemeName, RegistryValueKind.String);
        schemesKey.SetValue(schemeName, string.Join(",", schemeEntries), RegistryValueKind.ExpandString);

        ApplySystemCursorRefresh();
    }

    private Task RestoreBackedUpCursorSchemeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Configuration.GetValue(CursorBackupSavedFlag, false))
        {
            return Task.CompletedTask;
        }

        RestoreBackedUpCursorSchemeInternal();
        return Task.CompletedTask;
    }

    internal bool RestoreCursorScheme(string schemeName)
    {
        if (string.IsNullOrWhiteSpace(schemeName))
        {
            return false;
        }

        using var cursorKey = Registry.CurrentUser.CreateSubKey(CursorRegistryPath, true)
                             ?? throw new InvalidOperationException("Failed to open cursor registry path.");
        using var schemesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cursors\Schemes", false);
        var rawScheme = Convert.ToString(schemesKey?.GetValue(schemeName));
        if (string.IsNullOrWhiteSpace(rawScheme))
        {
            return false;
        }

        var parts = rawScheme.Split(',');
        if (parts.Length < CursorSchemeOrder.Length)
        {
            return false;
        }

        cursorKey.SetValue(string.Empty, schemeName, RegistryValueKind.String);
        for (var i = 0; i < CursorSchemeOrder.Length; i++)
        {
            cursorKey.SetValue(CursorSchemeOrder[i].Key, parts[i], RegistryValueKind.ExpandString);
        }

        var precisionPath = GetPrecisionCursorPath(parts);
        foreach (var additionalKey in AdditionalCursorKeys)
        {
            cursorKey.SetValue(additionalKey, precisionPath, RegistryValueKind.ExpandString);
        }

        cursorKey.SetValue("Scheme Source", 1, RegistryValueKind.DWord);
        ApplySystemCursorRefresh();
        return true;
    }

    private void RestoreBackedUpCursorSchemeInternal()
    {
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
    }

    private void BackupCurrentCursorSchemeIfNeeded()
    {
        if (Configuration.GetValue(CursorBackupSavedFlag, false))
        {
            return;
        }

        using var cursorKey = Registry.CurrentUser.OpenSubKey(CursorRegistryPath, false);
        if (cursorKey == null)
        {
            return;
        }

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
        RunLifecycleTask(nameof(BackupCurrentCursorSchemeIfNeeded), Configuration.SaveAsync);
    }

    /// <summary>
    /// Executes a lifecycle task without synchronously blocking the caller's thread.
    /// Uses fire-and-forget with logging instead of <c>GetAwaiter().GetResult()</c>
    /// to avoid deadlock when the host calls lifecycle methods on the UI thread.
    /// </summary>
    private static void RunLifecycleTask(string operationName, Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PluginLog.Trace($"CustomMouse lifecycle operation '{operationName}' failed: {ex.Message}", ex);
            }
        });
    }

    private static void RunBackgroundTask(string operationName, Func<Task<bool>> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore expected cancellation during shutdown/theme watcher disposal.
            }
            catch (Exception ex)
            {
                PluginLog.Trace($"CustomMouse background operation '{operationName}' failed: {ex.Message}", ex);
            }
        });
    }

    private void NormalizeCursorThemeSettings()
    {
        var mode = _settings.CursorThemeMode;
        if (!Enum.IsDefined(typeof(CursorThemeMode), mode))
        {
            mode = CursorThemeMode.Auto;
        }

        _settings.CursorThemeMode = mode;
        _settings.AutoThemeCursorStyle = mode == CursorThemeMode.Auto;
    }

    private bool IsCurrentThemeAlreadyApplied()
    {
        var currentTheme = IsSystemLightTheme() ? "light" : "dark";
        return string.Equals(_settings.LastAppliedTheme, currentTheme, StringComparison.OrdinalIgnoreCase);
    }

    private CursorThemeMode GetExplicitCursorThemeMode()
    {
        if (string.Equals(_settings.LastAppliedTheme, "light", StringComparison.OrdinalIgnoreCase))
        {
            return CursorThemeMode.Light;
        }

        if (string.Equals(_settings.LastAppliedTheme, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return CursorThemeMode.Dark;
        }

        return IsSystemLightTheme() ? CursorThemeMode.Light : CursorThemeMode.Dark;
    }

    private CursorThemeMode DetectCurrentCursorThemeMode()
    {
        var currentSchemeName = ReadCurrentCursorSchemeName();
        if (string.Equals(currentSchemeName, WindowsDefaultCursorSchemeName, StringComparison.OrdinalIgnoreCase))
        {
            return CursorThemeMode.WindowsDefault;
        }

        var arrowPath = ReadCurrentCursorValue("Arrow");
        if (!string.IsNullOrWhiteSpace(arrowPath))
        {
            if (arrowPath.IndexOf(@"\Light\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CursorThemeMode.Light;
            }

            if (arrowPath.IndexOf(@"\Dark\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CursorThemeMode.Dark;
            }
        }

        return _settings.AutoThemeCursorStyle ? CursorThemeMode.Auto : GetExplicitCursorThemeMode();
    }

    private static string GetBackupConfigKey(string registryValueName)
    {
        if (string.IsNullOrEmpty(registryValueName))
        {
            return "CursorBackup_Default";
        }

        return $"CursorBackup_{registryValueName}";
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var personalizeKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            var value = personalizeKey?.GetValue("AppsUseLightTheme");
            return value is int intValue ? intValue != 0 : true;
        }
        catch
        {
            return true;
        }
    }

    private static int ReadCurrentWindowsPointerSpeed()
    {
        using var mouseKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", false);
        var raw = Convert.ToString(mouseKey?.GetValue("MouseSensitivity"));
        return int.TryParse(raw, out var speed) && speed >= 1 && speed <= 20 ? speed : 10;
    }

    private static bool ReadCurrentSwapButtons()
    {
        using var mouseKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", false);
        var raw = Convert.ToString(mouseKey?.GetValue("SwapMouseButtons"));
        return string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadCurrentCursorSchemeName()
    {
        using var cursorKey = Registry.CurrentUser.OpenSubKey(CursorRegistryPath, false);
        return Convert.ToString(cursorKey?.GetValue(string.Empty)) ?? string.Empty;
    }

    private static string ReadCurrentCursorValue(string valueName)
    {
        using var cursorKey = Registry.CurrentUser.OpenSubKey(CursorRegistryPath, false);
        return Convert.ToString(cursorKey?.GetValue(valueName)) ?? string.Empty;
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
        {
            return candidate;
        }

        return Path.Combine(
            GetResourceRoot(),
            "W11-CC-V2.2-HDPI",
            "Dark",
            "Regular",
            "02. classic");
    }

    internal string GetResourceRoot()
    {
        return EnumerateResourceRootCandidates()
            .FirstOrDefault(IsCursorResourceRoot)
            ?? EnumerateResourceRootCandidates().First();
    }

    private static IEnumerable<string> EnumerateResourceRootCandidates()
    {
        var overridePluginsDirectory = GetPluginsDirectoryOverride();
        var directories = new List<string?>();

        if (!string.IsNullOrWhiteSpace(overridePluginsDirectory))
        {
            directories.Add(Path.Combine(overridePluginsDirectory, "local", "custom-mouse"));
            directories.Add(Path.Combine(overridePluginsDirectory, "custom-mouse"));
            directories.Add(Path.Combine(overridePluginsDirectory, "LenovoLegionToolkit.Plugins.CustomMouse"));
        }

        directories.Add(Path.GetDirectoryName(typeof(CustomMousePlugin).Assembly.Location));
        directories.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        if (string.IsNullOrWhiteSpace(overridePluginsDirectory))
        {
            var pluginsDirectory = GetDefaultPluginsDirectory();
            directories.Add(Path.Combine(pluginsDirectory, "local", "custom-mouse"));
            directories.Add(Path.Combine(pluginsDirectory, "custom-mouse"));
            directories.Add(Path.Combine(pluginsDirectory, "LenovoLegionToolkit.Plugins.CustomMouse"));
        }

        directories.Add(AppContext.BaseDirectory);

        var distinctDirectories = directories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in distinctDirectories)
        {
            yield return Path.Combine(directory, ResourcesDirectoryName);
        }
    }

    private static string? GetPluginsDirectoryOverride()
    {
        var overridePath = Environment.GetEnvironmentVariable(PluginsDirectoryOverrideEnvironmentVariable);
        return string.IsNullOrWhiteSpace(overridePath)
            ? null
            : Path.GetFullPath(overridePath);
    }

    private static string GetDefaultPluginsDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "UniversalDeviceToolkit", "plugins");
    }

    private static bool IsCursorResourceRoot(string resourceRoot)
    {
        return !string.IsNullOrWhiteSpace(resourceRoot)
               && Directory.Exists(Path.Combine(resourceRoot, CursorThemeResourceDirectoryName));
    }

    private static string ResolveCursorFilePath(string basePath, string animationPath, string fileName)
    {
        if (fileName.EndsWith(".ani", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(animationPath, fileName);
        }

        return Path.Combine(basePath, fileName);
    }

    private static void EnsureCursorResourcesExist(string basePath, string animationPath)
    {
        foreach (var (_, fileName) in CursorSchemeOrder)
        {
            var fullPath = ResolveCursorFilePath(basePath, animationPath, fileName);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Cursor resource file not found: {fullPath}");
            }
        }
    }

    private static void ApplySystemCursorRefresh()
    {
        SystemParametersInfo(SpiSetCursors, 0, IntPtr.Zero, SpifSendChange);
    }

    private static string GetPrecisionCursorPath(string[] schemeParts)
    {
        if (schemeParts.Length > 4 && !string.IsNullOrWhiteSpace(schemeParts[4]))
        {
            return schemeParts[4];
        }

        return schemeParts.Length > 0 ? schemeParts[0] : string.Empty;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
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

public enum CursorThemeMode
{
    Auto = 0,
    Light = 1,
    Dark = 2,
    WindowsDefault = 3
}

public class MouseSettings
{
    public int Dpi { get; set; } = 1600;
    public int PollingRate { get; set; } = 1000;
    public int WindowsPointerSpeed { get; set; } = 10;
    public bool SwapButtons { get; set; }
    public bool AutoThemeCursorStyle { get; set; } = true;
    public CursorThemeMode CursorThemeMode { get; set; } = CursorThemeMode.Auto;
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
            CursorThemeMode = CursorThemeMode.Auto,
            LastAppliedTheme = string.Empty,
            ButtonMappings = new Dictionary<int, int>()
        };
    }
}
