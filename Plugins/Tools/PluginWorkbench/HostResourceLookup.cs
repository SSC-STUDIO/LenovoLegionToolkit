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
        {
            return string.Empty;
        }

        var resourceType = ResolveResourceType();
        if (resourceType is null)
        {
            return resourceKey;
        }

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
        var hostAssemblyNames = new[] { "Universal Device Toolkit", "Lenovo Legion Toolkit" };

        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(candidate => hostAssemblyNames.Contains(candidate.GetName().Name, StringComparer.OrdinalIgnoreCase));

        if (assembly is null)
        {
            foreach (var name in hostAssemblyNames)
            {
                try { assembly = Assembly.Load(new AssemblyName(name)); }
                catch { /* try next host name */ }
                if (assembly is not null) break;
            }

            if (assembly is null)
                return null;
        }

        return assembly.GetType("LenovoLegionToolkit.WPF.Resources.Resource", throwOnError: false, ignoreCase: false);
    }
}