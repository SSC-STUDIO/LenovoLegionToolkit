using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Humanizer;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace UniversalDeviceToolkit.WPF.Utils;

public static class LocalizationHelper
{
    private static string LanguagePath => Path.Combine(Folders.AppData, "lang");

    private static readonly CultureInfo DefaultLanguage = new("en");
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

    public static readonly CultureInfo[] Languages = [
        DefaultLanguage,
        new("ar"),
        new("bg"),
        new("cs"),
        new("de"),
        new("el"),
        new("es"),
        new("fr"),
        new("hu"),
        new("it"),
        new("ja"),
        new("lv"),
        new("nl-nl"),
        new("pl"),
        new("pt"),
        new("pt-br"),
        new("ro"),
        new("ru"),
        new("sk"),
        new("tr"),
        new("uk"),
        new("vi"),
        new("zh-hans"),
        new("zh-hant"),
        new("uz-latn-uz"),
    ];

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
        var name = culture.NativeName.Transform(culture, To.TitleCase);

        if (culture.IetfLanguageTag.Equals("uz-latn-uz", StringComparison.OrdinalIgnoreCase))
        {
            name = "Karakalpak";
        }

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
        SetLanguageAsync(interactive, null);

    public static async Task SetLanguageAsync(bool interactive, LanguagePackManager? languagePackManager)
    {
        var savedCultureInfo = await GetLanguageFromFile();
        CultureInfo? cultureInfo = savedCultureInfo;
        var deviceSetupExists = IsDeviceSetupComplete();
        var showLanguageSelector = interactive && (savedCultureInfo is null || !deviceSetupExists);

        if (showLanguageSelector)
        {
            var preferred = GetPreferredStartupLanguage(savedCultureInfo);

            var window = languagePackManager is null
                ? new LanguageSelectorWindow(Languages, preferred)
                : new LanguageSelectorWindow(Languages, preferred, languagePackManager);
            ApplyStartupTheme(window);
            window.Show();
            cultureInfo = await window.ShouldContinue;

            if (cultureInfo is not null)
                await SaveLanguageToFileAsync(cultureInfo);
        }

        cultureInfo ??= await GetLanguageAsync();

        SetLanguageInternal(cultureInfo);
    }

    private static bool IsDeviceSetupComplete()
    {
        try
        {
            return File.Exists(Path.Combine(Folders.AppData, "device-setup"));
        }
        catch
        {
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
                var chineseCulture = isTraditionalChinese ? new CultureInfo("zh-hant") : new CultureInfo("zh-hans");
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
            var name = await File.ReadAllTextAsync(LanguagePath);
            var cultureInfo = new CultureInfo(name);
            if (!Languages.Contains(cultureInfo))
                throw new InvalidOperationException("Unknown language");
            return cultureInfo;
        }
        catch
        {
            return null;
        }
    }

    private static Task SaveLanguageToFileAsync(CultureInfo cultureInfo) => File.WriteAllTextAsync(LanguagePath, cultureInfo.Name);

    private static void SetLanguageInternal(CultureInfo cultureInfo)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en");

        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        Resource.Culture = cultureInfo;
        LenovoLegionToolkit.Lib.Resources.Resource.Culture = cultureInfo;
        UniversalDeviceToolkit.Lib.Automation.Resources.Resource.Culture = cultureInfo;
        
        // Set plugin resource cultures
        SetPluginResourceCultures(cultureInfo);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Applied culture: {cultureInfo.Name}");
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

            // Iterate through all loaded assemblies to find plugin Resource classes
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var assemblyName = assembly.GetName().Name;
                    if (assemblyName != null && assemblyName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
                    {
                        // Try both default namespace placements used by plugin resource designers.
                        var resourceType = assembly.GetType($"{assemblyName}.Resource")
                                           ?? assembly.GetType($"{assemblyName}.Resources.Resource");
                        if (resourceType != null)
                        {
                            var cultureProperty = resourceType.GetProperty("Culture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (cultureProperty != null)
                            {
                                cultureProperty.SetValue(null, defaultCultureInfo);
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Set resource culture for plugin: {assemblyName} = {defaultCultureInfo.Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Continue with other assemblies if one fails
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to set resource culture for assembly: {ex.Message}");
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

    private static CultureInfo[] EnumerateCultureFallbackChain(CultureInfo cultureInfo)
    {
        var fallbackChain = new System.Collections.Generic.List<CultureInfo>();
        var current = cultureInfo;

        while (current != CultureInfo.InvariantCulture)
        {
            if (!fallbackChain.Any(existing => existing.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase)))
                fallbackChain.Add(current);

            current = current.Parent;
        }

        if (!fallbackChain.Any(existing => existing.Name.Equals(DefaultLanguage.Name, StringComparison.OrdinalIgnoreCase)))
            fallbackChain.Add(DefaultLanguage);

        return fallbackChain.ToArray();
    }
}
