using System.Collections.Generic;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
#if WINDOWS
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
#endif

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Stores per-plugin language overrides and re-applies them to the resource
/// classes of every loaded plugin on language changes and plugin loads.
/// </summary>
public sealed class PluginLanguageService : IPluginLanguageService
{
    private readonly PluginLanguageSettings _settings;
    private readonly Func<IReadOnlyDictionary<string, IEnumerable<Type>>> _pluginResourceTypeProvider;

    /// <summary>
    /// Process-wide singleton for UI pages that cannot resolve the service.
    /// </summary>
    public static PluginLanguageService Current { get; } = new();

    public PluginLanguageService() : this(new PluginLanguageSettings())
    {
    }

    internal PluginLanguageService(
        PluginLanguageSettings settings,
        Func<IReadOnlyDictionary<string, IEnumerable<Type>>>? pluginResourceTypeProvider = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _pluginResourceTypeProvider = pluginResourceTypeProvider ?? GetRegisteredPluginResourceTypes;
        LocalizationRuntime.CultureChanged += OnLocalizationRuntimeCultureChanged;
    }

    public string? GetLanguage(string pluginId)
    {
        ArgumentNullException.ThrowIfNull(pluginId);

        return _settings.Store.PluginLanguages.TryGetValue(pluginId, out var cultureName)
            && !string.IsNullOrWhiteSpace(cultureName)
            ? cultureName
            : null;
    }

    public void SetLanguage(string pluginId, string? cultureName)
    {
        ArgumentNullException.ThrowIfNull(pluginId);

        var store = _settings.Store;
        if (string.IsNullOrWhiteSpace(cultureName))
            store.PluginLanguages.Remove(pluginId);
        else
            store.PluginLanguages[pluginId] = cultureName.Trim();

        _settings.SynchronizeStore();
        ApplyForAllLoadedPlugins();
        LanguagesChanged?.Invoke();
    }

    public event Action? LanguagesChanged;

    /// <summary>
    /// Re-applies the resource culture of every loaded plugin: its stored
    /// override when set, otherwise the current application culture.
    /// </summary>
    public void ApplyForAllLoadedPlugins()
    {
#if WINDOWS
        EnsurePluginLoadHookAttached();
        AvaloniaPluginResourceCulture.Apply(
            LocalizationRuntime.CurrentCulture,
            _settings.Store.PluginLanguages,
            _pluginResourceTypeProvider());
#endif
    }

    internal IReadOnlyDictionary<string, IEnumerable<Type>> GetPluginResourceTypes() =>
        _pluginResourceTypeProvider();

    internal void UnsubscribeLocalization() =>
        LocalizationRuntime.CultureChanged -= OnLocalizationRuntimeCultureChanged;

    private void OnLocalizationRuntimeCultureChanged(object? sender, CultureChangedEventArgs e) =>
        ApplyForAllLoadedPlugins();

    private static IReadOnlyDictionary<string, IEnumerable<Type>> GetRegisteredPluginResourceTypes()
    {
        var result = new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase);
#if WINDOWS
        var pluginManager = IoCContainer.TryResolve<IPluginManager>();
        if (pluginManager is null)
            return result;

        foreach (var plugin in pluginManager.GetRegisteredPlugins())
        {
            if (string.IsNullOrWhiteSpace(plugin.Id) || result.ContainsKey(plugin.Id))
                continue;

            try
            {
                result[plugin.Id] = AvaloniaPluginResourceCulture.GetPluginResourceTypes(plugin.GetType().Assembly);
            }
            catch
            {
                // Plugin types that cannot be inspected expose no resource classes.
            }
        }
#endif
        return result;
    }

#if WINDOWS
    private bool _pluginLoadHookAttached;

    private void EnsurePluginLoadHookAttached()
    {
        if (_pluginLoadHookAttached)
            return;

        var pluginManager = IoCContainer.TryResolve<IPluginManager>();
        if (pluginManager is null)
            return;

        pluginManager.PluginStateChanged += OnPluginStateChanged;
        _pluginLoadHookAttached = true;
    }

    private void OnPluginStateChanged(object? sender, PluginEventArgs e) => ApplyForAllLoadedPlugins();
#endif
}
