#if WINDOWS

using System.Globalization;
using System.Reflection;
using System.Resources;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Shared.Logging;

namespace UniversalDeviceToolkit.Avalonia.Localization;

/// <summary>
/// Applies the host culture to dynamically loaded plugin resource classes.
/// Plugin DLLs are loaded after the app culture is established, so generated
/// Resource classes need this explicit synchronization just as they do in WPF.
/// </summary>
internal static class AvaloniaPluginResourceCulture
{
    public static void Apply(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!IsPluginAssembly(assembly))
                continue;

            foreach (var type in GetPluginResourceTypes(assembly))
            {
                try
                {
                    type.GetProperty("Culture", BindingFlags.Public | BindingFlags.Static)!
                        .SetValue(null, culture);
                }
                catch (Exception exception)
                {
                    SharedLog.Trace($"Failed to apply culture to plugin resource {type.FullName}.", exception);
                }
            }
        }
    }

    private static IEnumerable<Type> GetPluginResourceTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes().Where(IsPluginResourceType).ToArray();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)
                .Cast<Type>()
                .Where(IsPluginResourceType)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static bool IsPluginAssembly(Assembly assembly)
    {
        try
        {
            if (assembly.IsDynamic)
                return false;

            var name = assembly.GetName().Name ?? string.Empty;
            if (name.Equals("UniversalDeviceToolkit.Lib.Plugins", StringComparison.OrdinalIgnoreCase)
                || name.Equals("UniversalDeviceToolkit.Tests", StringComparison.OrdinalIgnoreCase))
                return false;

            if (name.Contains("Plugin", StringComparison.OrdinalIgnoreCase))
                return true;

            var location = assembly.Location;
            var pluginsDirectory = PluginPaths.GetPluginsDirectory();
            return !string.IsNullOrWhiteSpace(location)
                && !string.IsNullOrWhiteSpace(pluginsDirectory)
                && location.StartsWith(pluginsDirectory, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPluginResourceType(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.Name is not ("Resource" or "Resources"))
            return false;

        var cultureProperty = type.GetProperty("Culture", BindingFlags.Public | BindingFlags.Static);
        var resourceManagerProperty = type.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.Static);
        return cultureProperty?.PropertyType == typeof(CultureInfo)
            && resourceManagerProperty?.PropertyType == typeof(ResourceManager)
            && (type.Namespace?.EndsWith(".Resources", StringComparison.Ordinal) == true
                || type.FullName?.Contains(".Resources.", StringComparison.Ordinal) == true
                || type.FullName?.EndsWith(".Resource", StringComparison.Ordinal) == true);
    }
}

#endif
