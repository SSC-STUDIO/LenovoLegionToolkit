using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Provides plugin-based optimization categories by querying installed plugins.
/// </summary>
public class OptimizationCategoryExtender : IOptimizationCategoryExtender
{
    private static readonly string[] ManifestFileNames = ["plugin.manifest.json", "plugin.json", "Plugin.json"];

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
            var categoryPluginIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                        categoryPluginIds.Add(plugin.Id);
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to get optimization category from plugin {plugin.Id}: {ex.Message}", ex);
                }
            }

            foreach (var category in GetManifestPluginCategories(categoryPluginIds))
                list.Add(category);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get optimization categories from plugins", ex);
        }

        return list;
    }

    private IEnumerable<WindowsOptimizationCategoryDefinition> GetManifestPluginCategories(HashSet<string> alreadyAddedPluginIds)
    {
        foreach (var pluginDirectory in EnumerateInstalledPluginDirectories())
        {
            PluginManifest? manifest = null;
            try
            {
                manifest = TryReadManifest(pluginDirectory);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to read optimization manifest from {pluginDirectory}: {ex.Message}", ex);
            }

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
                continue;

            if (!IsManifestPluginInstalled(manifest.Id) || alreadyAddedPluginIds.Contains(manifest.Id))
                continue;

            var actions = manifest.Contributes?.OptimizationActions?
                .Select(action =>
                {
                    var actionId = PluginUiCapabilityResolver.GetOptimizationActionId(action);
                    if (string.IsNullOrWhiteSpace(actionId))
                        return null;

                    var description = FirstNonEmpty(
                        action.Description,
                        manifest.Store?.Description,
                        manifest.Description,
                        actionId);

                    return new WindowsOptimizationActionDefinition(
                        actionId,
                        FirstNonEmpty(action.Title, actionId),
                        description,
                        _ => Task.CompletedTask,
                        Recommended: action.Recommended ?? false,
                        IsAppliedAsync: _ => Task.FromResult(false));
                })
                .Where(action => action is not null)
                .Cast<WindowsOptimizationActionDefinition>()
                .ToArray();

            if (actions is null || actions.Length == 0)
                continue;

            alreadyAddedPluginIds.Add(manifest.Id);
            yield return new WindowsOptimizationCategoryDefinition(
                manifest.Id,
                string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name,
                manifest.Store?.Description ?? manifest.Description,
                actions ?? [],
                manifest.Id);
        }
    }

    private static IEnumerable<string> EnumerateInstalledPluginDirectories()
    {
        var pluginsDirectory = PluginPaths.GetPluginsDirectory();
        if (!Directory.Exists(pluginsDirectory))
            yield break;

        foreach (var directory in Directory.EnumerateDirectories(pluginsDirectory))
        {
            var directoryName = Path.GetFileName(directory);
            if (directoryName.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var localDirectory in Directory.EnumerateDirectories(directory))
                    yield return localDirectory;

                continue;
            }

            yield return directory;
        }
    }

    private static PluginManifest? TryReadManifest(string pluginDirectory)
    {
        foreach (var manifestFileName in ManifestFileNames)
        {
            var manifestPath = Path.Combine(pluginDirectory, manifestFileName);
            if (!File.Exists(manifestPath))
                continue;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath), options);
        }

        return null;
    }

    private bool IsManifestPluginInstalled(string pluginId) =>
        _pluginManager.GetInstalledPluginIds().Contains(pluginId, StringComparer.OrdinalIgnoreCase) ||
        _pluginManager.IsInstalled(pluginId);

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }
}
