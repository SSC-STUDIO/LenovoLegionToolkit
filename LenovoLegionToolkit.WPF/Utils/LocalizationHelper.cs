using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
                Wpf.Ui.Appearance.Theme.Apply(themeType, Wpf.Ui.Appearance.BackgroundType.Mica, false);
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
    /// Set resource cultures for all plugins, using plugin-specific language settings if available
    /// </summary>
    public static void SetPluginResourceCultures(CultureInfo? defaultCultureInfo = null)
    {
        try
        {
            var pluginSettings = new Settings.PluginSettings();
            defaultCultureInfo ??= Resource.Culture ?? CultureInfo.CurrentUICulture;

            // Iterate through all loaded assemblies to find plugin Resource classes
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var assemblyName = assembly.GetName().Name;
                    if (assemblyName != null && assemblyName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find the plugin ID by looking for IPlugin implementation
                        string? pluginId = null;
                        try
                        {
                            var pluginTypes = assembly.GetTypes()
                                .Where(t => typeof(Lib.Plugins.IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                            foreach (var pluginType in pluginTypes)
                            {
                                try
                                {
                                    var pluginInstance = Activator.CreateInstance(pluginType) as Lib.Plugins.IPlugin;
                                    pluginId = pluginInstance?.Id;
                                    if (!string.IsNullOrWhiteSpace(pluginId))
                                        break;
                                }
                                catch
                                {
                                    // Continue searching
                                }
                            }
                        }
                        catch
                        {
                            // Continue without plugin ID
                        }

                        // Get plugin-specific culture or use default
                        var pluginCulture = !string.IsNullOrWhiteSpace(pluginId)
                            ? pluginSettings.GetPluginCulture(pluginId) ?? defaultCultureInfo
                            : defaultCultureInfo;

                        // Try to find Resource class in the plugin namespace
                        var resourceType = assembly.GetType($"{assemblyName}.Resource");
                        if (resourceType != null)
                        {
                            var cultureProperty = resourceType.GetProperty("Culture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (cultureProperty != null)
                            {
                                cultureProperty.SetValue(null, pluginCulture);
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Set resource culture for plugin: {assemblyName} (ID: {pluginId ?? "unknown"}) = {pluginCulture.Name}");
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
}
