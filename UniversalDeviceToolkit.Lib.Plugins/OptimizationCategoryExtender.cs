using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

                        if (category.ResourceAnchorType is null)
                        {
                            var pluginType = plugin.GetType();
                            category = category with
                            {
                                ResourceAnchorType = pluginType,
                                Actions = category.Actions
                                    .Select(action => action.ResourceAnchorType is null
                                        ? action with { ResourceAnchorType = pluginType }
                                        : action)
                                    .ToArray()
                            };
                        }

                        list.Add(category);
                    }
                    else if (TryCreateManifestCategory(plugin) is { } manifestCategory)
                    {
                        list.Add(manifestCategory);
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to get optimization category from plugin {plugin.Id}: {ex.Message}", ex);
                }
            }

            var registeredPluginIds = installedPlugins.Select(plugin => plugin.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var pluginId in _pluginManager.GetInstalledPluginIds().Where(id => !registeredPluginIds.Contains(id)))
            {
                try
                {
                    if (TryCreateManifestCategory(pluginId) is { } manifestCategory)
                        list.Add(manifestCategory);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to get manifest optimization category from plugin {pluginId}: {ex.Message}", ex);
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

    private static WindowsOptimizationCategoryDefinition? TryCreateManifestCategory(string pluginId)
    {
        var manifest = PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);
        return TryCreateManifestCategory(pluginId, manifest);
    }

    private static WindowsOptimizationCategoryDefinition? TryCreateManifestCategory(IPlugin plugin)
    {
        var manifest = plugin is PluginManifestAdapter adapter
            ? adapter.Manifest
            : PluginUiCapabilityResolver.ReadInstalledManifest(plugin.Id);

        return TryCreateManifestCategory(plugin.Id, manifest);
    }

    private static WindowsOptimizationCategoryDefinition? TryCreateManifestCategory(string pluginId, PluginManifest? manifest)
    {
        if (!PluginUiCapabilityResolver.SupportsOptimizationActions(manifest))
            return null;

        var actions = manifest!.Contributes!.OptimizationActions!
            .Select(CreateManifestAction)
            .Where(action => action is not null)
            .Cast<WindowsOptimizationActionDefinition>()
            .ToArray();

        if (actions.Length == 0)
            return null;

        var categoryKey = $"plugin.{NormalizeKey(manifest.Id, pluginId)}";
        return new WindowsOptimizationCategoryDefinition(
            categoryKey,
            FirstNonEmpty(manifest.Name, manifest.Id, pluginId),
            FirstNonEmpty(manifest.Description, manifest.Name, pluginId),
            actions,
            FirstNonEmpty(manifest.Id, pluginId));
    }

    private static WindowsOptimizationActionDefinition? CreateManifestAction(PluginManifestOptimizationContribution action)
    {
        var actionId = PluginUiCapabilityResolver.GetOptimizationActionId(action);
        var title = FirstNonEmpty(action.Title, actionId);
        if (string.IsNullOrWhiteSpace(actionId) || string.IsNullOrWhiteSpace(title))
            return null;

        return new WindowsOptimizationActionDefinition(
            actionId,
            title,
            FirstNonEmpty(action.Description, title),
            _ => Task.CompletedTask,
            Recommended: false);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static string NormalizeKey(params string?[] values)
    {
        var value = FirstNonEmpty(values);
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        var chars = value.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '-').ToArray();
        return new string(chars).Trim('-', '.', '_').ToLowerInvariant();
    }
}
