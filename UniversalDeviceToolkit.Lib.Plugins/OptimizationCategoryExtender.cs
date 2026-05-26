using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Provides plugin-based optimization categories by querying installed plugins.
/// </summary>
public class OptimizationCategoryExtender : IOptimizationCategoryExtender
{
    private readonly IPluginManager _pluginManager;

    public OptimizationCategoryExtender(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public IReadOnlyList<WindowsOptimizationCategoryDefinition> GetPluginCategories()
    {
        var list = new List<WindowsOptimizationCategoryDefinition>();

        try
        {
            var installedPlugins = _pluginManager.GetRegisteredPlugins()
                .Where(p => _pluginManager.IsInstalled(p.Id));

            foreach (var plugin in installedPlugins)
            {
                try
                {
                    WindowsOptimizationCategoryDefinition? category = null;

                    if (plugin is IOptimizationCategoryProvider provider)
                    {
                        category = provider.GetOptimizationCategory();
                    }
                    else if (plugin is PluginBase pluginBase)
                    {
                        category = pluginBase.GetOptimizationCategory();
                    }

                    if (category != null)
                    {
                        if (string.IsNullOrEmpty(category.PluginId))
                        {
                            category = category with { PluginId = plugin.Id };
                        }
                        list.Add(category);
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to get optimization category from plugin {plugin.Id}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get optimization categories from plugins", ex);
        }

        return list;
    }
}
