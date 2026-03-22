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
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Settings;
using LenovoLegionToolkit.WPF.Windows.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace LenovoLegionToolkit.WPF.Utils;

public static class LocalizationHelper
{
    private static readonly string LanguagePath = Path.Combine(Folders.AppData, "lang");

    private static readonly CultureInfo DefaultLanguage = new("en");
    public static event EventHandler? PluginResourceCulturesChanged;

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

        if (culture.IetfLanguageTag.Equals("uz-latn-uz", StringComparison.InvariantCultureIgnoreCase))
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

    public static async Task SetLanguageAsync(bool interactive = false)
    {
        CultureInfo? cultureInfo = null;

        if (interactive && await GetLanguageFromFile() is null)
        {
            // Apply system theme before showing language selection window
            try
            {
                var isDarkMode = SystemTheme.IsDarkMode();
                var themeType = isDarkMode ? Wpf.Ui.Appearance.ThemeType.Dark : Wpf.Ui.Appearance.ThemeType.Light;
                var backgroundType = RenderingCompatibilityHelper.GetPreferredBackgroundType();
                Wpf.Ui.Appearance.Theme.Apply(themeType, backgroundType, false);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to apply system theme before showing language selector window.", ex);
            }

            var window = new LanguageSelectorWindow(Languages, DefaultLanguage);
            window.Show();
            cultureInfo = await window.ShouldContinue;
            if (cultureInfo is not null)
                await SaveLanguageToFileAsync(cultureInfo);
        }

        cultureInfo ??= await GetLanguageAsync();

        SetLanguageInternal(cultureInfo);
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
        Lib.Resources.Resource.Culture = cultureInfo;
        Lib.Automation.Resources.Resource.Culture = cultureInfo;
        
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
            PluginResourceCulturesChanged?.Invoke(null, EventArgs.Empty);
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
