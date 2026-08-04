using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Humanizer;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace UniversalDeviceToolkit.WPF.Utils;

public static class LocalizationHelper
{
    private static string LanguagePath => Path.Combine(Folders.AppData, "lang");

    private static readonly CultureInfo DefaultLanguage = LocalizationCatalog.DefaultCulture;
    private static readonly object _eventLock = new();
    private static EventHandler? _pluginResourceCulturesChanged;

    public static event EventHandler? PluginResourceCulturesChanged
    {
        add
        {
            lock (_eventLock)
            {
                _pluginResourceCulturesChanged += value;
            }
        }
        remove
        {
            lock (_eventLock)
            {
                _pluginResourceCulturesChanged -= value;
            }
        }
    }

    public static readonly CultureInfo[] Languages = LocalizationCatalog.SupportedCultures.ToArray();

    public static FlowDirection Direction => Resource.Culture?.TextInfo.IsRightToLeft ?? false
        ? FlowDirection.RightToLeft
        : FlowDirection.LeftToRight;

    private static string? _dateFormat;

    public static string ShortDateFormat
    {
        get
        {
            if (_dateFormat is not null)
                return _dateFormat;

            _dateFormat = GetSystemShortDateFormat() ?? "dd/M/yyyy";
            return _dateFormat;
        }
    }

    public static string LanguageDisplayName(CultureInfo culture)
    {
        var name = LocalizationCatalog.GetDisplayName(culture);

        return ForceLeftToRight(name);
    }

    public static string ForceLeftToRight(string str)
    {
        if (Resource.Culture?.TextInfo.IsRightToLeft ?? false)
            return "\u200e" + str + "\u200e";
        return str;
    }

    public static string GetStringOrEnglish(ResourceManager resourceManager, string key, string fallback, CultureInfo? cultureInfo = null)
    {
        if (resourceManager is null)
            throw new ArgumentNullException(nameof(resourceManager));

        if (string.IsNullOrWhiteSpace(key))
            return fallback;

        var activeCulture = cultureInfo ?? Resource.Culture ?? CultureInfo.CurrentUICulture;

        foreach (var culture in EnumerateCultureFallbackChain(activeCulture))
        {
            var value = TryGetStringExact(resourceManager, key, culture);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        var invariant = resourceManager.GetString(key, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(invariant) ? fallback : invariant;
    }

    public static Task SetLanguageAsync(bool interactive = false) =>
        SetLanguageAsync(interactive, null, allowOfflineEnglish: false);

    public static async Task<LanguageGateOutcome> SetLanguageAsync(bool interactive, LanguagePackManager? languagePackManager, bool allowOfflineEnglish = false)
    {
        var savedCultureInfo = await GetLanguageFromFile();
        CultureInfo? cultureInfo = savedCultureInfo;
        var deviceSetupExists = IsDeviceSetupComplete();
        var showLanguageSelector = interactive && (savedCultureInfo is null || !deviceSetupExists);

        if (showLanguageSelector)
        {
            var preferred = GetPreferredStartupLanguage(savedCultureInfo);
            var manager = languagePackManager ?? new LanguagePackManager(new UniversalDeviceToolkit.Lib.ResourcesCatalog.OnlineResourceCatalogClient(new UniversalDeviceToolkit.Lib.HttpClientFactory()));
            var window = new LanguageSelectorWindow(Languages, preferred, manager, allowOfflineEnglish);
            ApplyStartupTheme(window);
            window.ShowDialog();

            var outcome = await window.GateOutcome;
            if (outcome == LanguageGateOutcome.Exit)
                return LanguageGateOutcome.Exit;

            cultureInfo = outcome == LanguageGateOutcome.ContinueEnglish
                ? DefaultLanguage
                : window.SelectedCulture ?? DefaultLanguage;

            await SaveLanguageToFileAsync(cultureInfo);
            ClearTemporaryStartupMainWindow(window);
        }

        cultureInfo ??= await GetLanguageAsync();
        SetLanguageInternal(cultureInfo);
        return LanguageGateOutcome.Continue;
    }

    private static void ClearTemporaryStartupMainWindow(Window languageWindow)
    {
        try
        {
            if (ReferenceEquals(Application.Current?.MainWindow, languageWindow))
                Application.Current.MainWindow = null;
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "loc-clear-temp-mainwindow",
                "Best-effort clear of temporary MainWindow after language selector failed.",
                ex);
        }
    }

    private static bool IsDeviceSetupComplete()
    {
        try
        {
            return File.Exists(Path.Combine(Folders.AppData, "device-setup"));
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "device-setup-exists-check",
                "Failed to check device-setup completion marker; treating as incomplete.",
                ex);
            return false;
        }
    }

    private static CultureInfo GetPreferredStartupLanguage(CultureInfo? savedCultureInfo)
    {
        if (savedCultureInfo is not null)
            return savedCultureInfo;

        try
        {
            var systemCulture = CultureInfo.CurrentUICulture;

            if (systemCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                var traditionalChineseRegions = new[] { "TW", "HK", "MO" };
                var isTraditionalChinese = traditionalChineseRegions.Any(region =>
                    systemCulture.Name.Contains(region, StringComparison.OrdinalIgnoreCase));
                var chineseCulture = isTraditionalChinese ? new CultureInfo("zh-Hant") : new CultureInfo("zh-Hans");
                if (Languages.Contains(chineseCulture))
                    return chineseCulture;
            }

            var exactMatch = Languages.FirstOrDefault(language =>
                language.Name.Equals(systemCulture.Name, StringComparison.OrdinalIgnoreCase));
            if (exactMatch is not null)
                return exactMatch;

            var parentMatch = Languages.FirstOrDefault(language =>
                language.Name.Equals(systemCulture.Parent.Name, StringComparison.OrdinalIgnoreCase));
            if (parentMatch is not null)
                return parentMatch;

            var neutralMatch = Languages.FirstOrDefault(language =>
                language.TwoLetterISOLanguageName.Equals(systemCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
            if (neutralMatch is not null)
                return neutralMatch;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to resolve startup language from system culture.", ex);
        }

        return DefaultLanguage;
    }

    public static void ApplyStartupTheme(Window window)
    {
        try
        {
            var settings = new ApplicationSettings();
            var isDarkMode = ResolveStartupDarkMode(settings.Store.Theme);

            var themeType = isDarkMode ? Wpf.Ui.Appearance.ApplicationTheme.Dark : Wpf.Ui.Appearance.ApplicationTheme.Light;
            var backgroundType = RenderingCompatibilityHelper.GetPreferredBackgroundType(settings);

            if (Application.Current.MainWindow is null)
                Application.Current.MainWindow = window;

            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(themeType, backgroundType, false);
            window.SetResourceReference(Window.BackgroundProperty, "ApplicationBackgroundBrush");
            Application.Current.Resources["SnackbarShadowColor"] = isDarkMode
                ? System.Windows.Media.Colors.Black
                : System.Windows.Media.Color.FromArgb(64, 0, 0, 0);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to apply configured startup theme before showing language selector window.", ex);
        }
    }

    private static bool ResolveStartupDarkMode(Theme configuredTheme)
    {
        switch (configuredTheme)
        {
            case Theme.Dark:
                return true;
            case Theme.Light:
                return false;
            case Theme.System:
            default:
                try
                {
                    return SystemTheme.IsDarkMode();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Failed to resolve startup theme from system theme; using dark theme.", ex);

                    return true;
                }
        }
    }

    public static async Task SetLanguageAsync(CultureInfo cultureInfo)
    {
        await SaveLanguageToFileAsync(cultureInfo);
        SetLanguageInternal(cultureInfo);
    }

    public static async Task<CultureInfo> GetLanguageAsync()
    {
        var cultureInfo = await GetLanguageFromFile();
        if (cultureInfo is null)
        {
            cultureInfo = DefaultLanguage;
            await SaveLanguageToFileAsync(cultureInfo);
        }
        return cultureInfo;
    }

    private static async Task<CultureInfo?> GetLanguageFromFile()
    {
        try
        {
            var name = (await File.ReadAllTextAsync(LanguagePath)).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var cultureInfo = new CultureInfo(name);
            // Prefer exact / case-insensitive match against supported list (Contains uses object equality).
            var matched = ResolveSupportedLanguage(cultureInfo);
            if (matched is null)
                throw new InvalidOperationException($"Unknown language '{name}'");
            return matched;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Maps free-form culture names (e.g. en-US, zh-CN) onto the Languages table.
    /// </summary>
    internal static CultureInfo? ResolveSupportedLanguage(CultureInfo cultureInfo)
    {
        if (cultureInfo is null)
            return null;

        return LocalizationCatalog.NormalizeCulture(cultureInfo);
    }

    private static Task SaveLanguageToFileAsync(CultureInfo cultureInfo)
    {
        var normalized = ResolveSupportedLanguage(cultureInfo) ?? DefaultLanguage;
        Directory.CreateDirectory(Path.GetDirectoryName(LanguagePath)!);
        return File.WriteAllTextAsync(LanguagePath, normalized.Name);
    }

    private static void SetLanguageInternal(CultureInfo cultureInfo)
    {
        cultureInfo = ResolveSupportedLanguage(cultureInfo) ?? DefaultLanguage;

        // Keep the WPF resource lifecycle and the shared host runtime in sync. The WPF
        // language gate still owns its online-pack and first-run policy, while the shared
        // runtime provides one canonical culture state for plugins and other hosts.
        LocalizationRuntime.SetCultureAsync(cultureInfo, persist: false)
            .GetAwaiter()
            .GetResult();

        // Format numbers/dates in invariant-friendly English culture; UI strings use UI culture.
        var english = new CultureInfo("en");
        Thread.CurrentThread.CurrentCulture = english;
        CultureInfo.DefaultThreadCurrentCulture = english;

        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        ApplyCoreResourceCultures(cultureInfo);
        SetPluginResourceCultures(cultureInfo);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Applied culture: {cultureInfo.Name} (UI={CultureInfo.CurrentUICulture.Name})");
    }

    /// <summary>
    /// Propagates UI culture to main app + core libraries. Safe to call after delayed plugin load.
    /// </summary>
    public static void ApplyCoreResourceCultures(CultureInfo cultureInfo)
    {
        Resource.Culture = cultureInfo;
        UniversalDeviceToolkit.Lib.Resources.Resource.Culture = cultureInfo;
        UniversalDeviceToolkit.Lib.Automation.Resources.Resource.Culture = cultureInfo;
        UniversalDeviceToolkit.Lib.Macro.Resources.Resource.Culture = cultureInfo;
        // Host-side plugin error strings (signature/sandbox/dependency) live here.
        UniversalDeviceToolkit.Lib.Plugins.Resources.Resource.Culture = cultureInfo;
    }

    /// <summary>
    /// Set resource cultures for all loaded plugins to the current application language.
    /// Per-plugin language overrides were removed because the runtime no longer exposes
    /// a user-facing way to manage them and stale hidden values caused plugins to drift
    /// away from the global language selection.
    /// </summary>
    public static void SetPluginResourceCultures(CultureInfo? defaultCultureInfo = null)
    {
        try
        {
            defaultCultureInfo ??= Resource.Culture ?? CultureInfo.CurrentUICulture;

            // Only touch plugin assemblies. Scanning every loaded assembly with GetTypes()
            // freezes the UI when opening the plugin page (large AppDomain after store load).
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsLikelyPluginAssembly(assembly))
                    continue;

                foreach (var resourceType in GetPluginResourceTypes(assembly))
                {
                    try
                    {
                        var cultureProperty = resourceType.GetProperty("Culture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var resourceManagerProperty = resourceType.GetProperty("ResourceManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (cultureProperty?.PropertyType != typeof(CultureInfo) || resourceManagerProperty?.PropertyType != typeof(ResourceManager))
                            continue;

                        cultureProperty.SetValue(null, defaultCultureInfo);
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Set resource culture for plugin resource: {resourceType.FullName} = {defaultCultureInfo.Name}");
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to set plugin resource culture for {resourceType.FullName}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to set plugin resource cultures: {ex.Message}");
        }
        finally
        {
            EventHandler? handler;
            lock (_eventLock)
            {
                handler = _pluginResourceCulturesChanged;
            }
            handler?.Invoke(null, EventArgs.Empty);
        }
    }

    private static Type[] GetPluginResourceTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return FilterPluginResourceTypes(assembly.GetTypes());
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            return FilterPluginResourceTypes(ex.Types.Where(type => type is not null).Cast<Type>());
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static Type[] FilterPluginResourceTypes(IEnumerable<Type> types) =>
        types.Where(IsPluginResourceType)
            .ToArray();

    private static bool IsLikelyPluginAssembly(System.Reflection.Assembly assembly)
    {
        try
        {
            if (assembly.IsDynamic)
                return false;

            var name = assembly.GetName().Name ?? string.Empty;
            // Host assemblies that merely contain "Plugin" in the name are not plugins.
            if (name.Equals("UniversalDeviceToolkit.Lib.Plugins", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("UniversalDeviceToolkit.Tests", StringComparison.OrdinalIgnoreCase))
                return false;

            if (name.Contains("Plugin", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("UniversalDeviceToolkit.Plugins", StringComparison.OrdinalIgnoreCase))
                return true;

            // Loaded from the plugins folder (file-based load contexts).
            var location = assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
                return false;

            var pluginsDir = UniversalDeviceToolkit.Lib.Plugins.PluginPaths.GetPluginsDirectory();
            return !string.IsNullOrWhiteSpace(pluginsDir) &&
                   location.StartsWith(pluginsDir, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Recognizes generated satellite resource types in plugin assemblies.
    /// Supports both <c>*.Resources.Resource</c> and <c>*.Resource</c> naming.
    /// </summary>
    internal static bool IsPluginResourceType(Type type)
    {
        if (type is null || type.IsAbstract || type.IsInterface)
            return false;

        // Host libraries (not third-party plugins). Lib.Plugins hosts signature/sandbox
        // error strings and is applied via ApplyCoreResourceCultures, not plugin scanning.
        if (type.Assembly == typeof(Resource).Assembly ||
            type.Assembly == typeof(UniversalDeviceToolkit.Lib.Resources.Resource).Assembly ||
            type.Assembly == typeof(UniversalDeviceToolkit.Lib.Automation.Resources.Resource).Assembly ||
            type.Assembly == typeof(UniversalDeviceToolkit.Lib.Macro.Resources.Resource).Assembly ||
            type.Assembly == typeof(UniversalDeviceToolkit.Lib.Plugins.Resources.Resource).Assembly)
            return false;

        var cultureProperty = type.GetProperty("Culture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var resourceManagerProperty = type.GetProperty("ResourceManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (cultureProperty?.PropertyType != typeof(CultureInfo) ||
            resourceManagerProperty?.PropertyType != typeof(ResourceManager))
            return false;

        // Classic Designer class name "Resource" under a *.Resources namespace.
        if ((type.Name is "Resource" or "Resources") &&
            (type.Namespace?.EndsWith(".Resources", StringComparison.Ordinal) == true ||
             type.FullName?.Contains(".Resources.", StringComparison.Ordinal) == true ||
             type.FullName?.EndsWith(".Resource", StringComparison.Ordinal) == true))
            return true;

        // Plugin assemblies may live under UniversalDeviceToolkit.Plugins.* or UniversalDeviceToolkit.Plugins.*
        var assemblyName = type.Assembly.GetName().Name ?? string.Empty;
        if (assemblyName.Contains("Plugins", StringComparison.OrdinalIgnoreCase) &&
            type.Name is "Resource" or "Resources")
            return true;

        return false;
    }

    /// <summary>
    /// Fallback chain for UI strings: exact culture → parents → English.
    /// Never falls through to Chinese cultures unless the requested culture is Chinese.
    /// </summary>
    internal static IEnumerable<CultureInfo> EnumerateCultureFallbackChainPublic(CultureInfo cultureInfo) =>
        EnumerateCultureFallbackChain(cultureInfo);

    private static unsafe string? GetSystemShortDateFormat()
    {
        var ptr = IntPtr.Zero;
        try
        {
            var length = PInvoke.GetLocaleInfoEx(null, PInvoke.LOCALE_SSHORTDATE, null, 0);
            if (length == 0)
                return null;

            ptr = Marshal.AllocHGlobal(sizeof(char) * length);
            var charPtr = new PWSTR((char*)ptr.ToPointer());

            length = PInvoke.GetLocaleInfoEx(null, PInvoke.LOCALE_SSHORTDATE, charPtr, length);
            return length == 0 ? null : charPtr.ToString();
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private static string? TryGetStringExact(ResourceManager resourceManager, string key, CultureInfo culture)
    {
        try
        {
            var resourceSet = resourceManager.GetResourceSet(culture, true, false);
            return resourceSet?.GetString(key, false);
        }
        catch
        {
            return null;
        }
    }

    private static CultureInfo[] EnumerateCultureFallbackChain(CultureInfo cultureInfo) =>
        LocalizationCatalog.GetFallbackChain(cultureInfo).ToArray();

    internal static bool IsChineseCulture(CultureInfo cultureInfo)
    {
        if (cultureInfo is null || cultureInfo == CultureInfo.InvariantCulture)
            return false;

        return LocalizationCatalog.IsChinese(cultureInfo);
    }
}
