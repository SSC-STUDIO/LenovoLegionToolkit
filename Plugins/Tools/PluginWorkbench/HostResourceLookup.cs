using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace PluginWorkbench;

internal static class HostResourceLookup
{
    public static string Resolve(string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
            return string.Empty;

        var resourceType = ResolveResourceType();
        if (resourceType is null)
            return resourceKey;

        try
        {
            var resourceManager = resourceType
                .GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null) as ResourceManager;

            var culture = resourceType
                .GetProperty("Culture", BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null) as CultureInfo ?? CultureInfo.CurrentUICulture;

            return resourceManager?.GetString(resourceKey, culture) ?? resourceKey;
        }
        catch
        {
            return resourceKey;
        }
    }

    private static Type? ResolveResourceType()
    {
        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "Lenovo Legion Toolkit", StringComparison.OrdinalIgnoreCase));

        if (assembly is null)
        {
            try
            {
                assembly = Assembly.Load(new AssemblyName("Lenovo Legion Toolkit"));
            }
            catch
            {
                return null;
            }
        }

        return assembly.GetType("LenovoLegionToolkit.WPF.Resources.Resource", throwOnError: false, ignoreCase: false);
    }
}
