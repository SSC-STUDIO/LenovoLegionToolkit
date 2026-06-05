using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            var installedPluginIds = GetInstalledPluginIdsSnapshot();
            var installedPlugins = _pluginManager.GetRegisteredPlugins()
                .Select(plugin => TryCreateInstalledPluginContext(plugin, installedPluginIds))
                .Where(context => context is not null)
                .Cast<InstalledPluginContext>()
                .ToArray();

            foreach (var context in installedPlugins)
            {
                var plugin = context.Plugin;

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
                    else
                    {
                        category = TryGetOptimizationCategoryByConvention(plugin);
                    }

                    if (category != null)
                    {
                        if (!string.Equals(category.PluginId, context.InstalledPluginId, StringComparison.OrdinalIgnoreCase))
                        {
                            category = category with { PluginId = context.InstalledPluginId };
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
                    else if (TryCreateManifestCategory(context) is { } manifestCategory)
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

            var registeredPluginIds = installedPlugins
                .SelectMany(context => context.KnownPluginIds)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var pluginId in installedPluginIds.Where(id => !registeredPluginIds.Contains(id)))
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

    private HashSet<string> GetInstalledPluginIdsSnapshot()
    {
        try
        {
            return (_pluginManager.GetInstalledPluginIds() ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read installed plugin IDs: {ex.Message}", ex);

            return [];
        }
    }

    private InstalledPluginContext? TryCreateInstalledPluginContext(IPlugin plugin, HashSet<string> installedPluginIds)
    {
        var metadata = TryGetPluginMetadata(plugin.Id);
        var manifest = plugin is PluginManifestAdapter adapter
            ? adapter.Manifest
            : TryReadManifestNearPlugin(metadata);

        var knownPluginIds = GetKnownPluginIds(plugin, metadata, manifest).ToArray();
        var installedPluginId = knownPluginIds.FirstOrDefault(id => IsPluginIdInstalled(id, installedPluginIds));

        return installedPluginId is null
            ? null
            : new InstalledPluginContext(plugin, installedPluginId, manifest, knownPluginIds);
    }

    private bool IsPluginIdInstalled(string pluginId, HashSet<string> installedPluginIds) =>
        installedPluginIds.Contains(pluginId) || _pluginManager.IsInstalled(pluginId);

    private PluginMetadata? TryGetPluginMetadata(string pluginId)
    {
        try
        {
            return _pluginManager.GetPluginMetadata(pluginId);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read plugin metadata for {pluginId}: {ex.Message}", ex);

            return null;
        }
    }

    private static IEnumerable<string> GetKnownPluginIds(
        IPlugin plugin,
        PluginMetadata? metadata,
        PluginManifest? manifest)
    {
        var candidates = new List<string?>
        {
            plugin.Id,
            metadata?.Id,
            manifest?.Id
        };

        if (!string.IsNullOrWhiteSpace(metadata?.FilePath))
        {
            var pluginDirectory = Path.GetDirectoryName(metadata.FilePath);
            var directoryName = string.IsNullOrWhiteSpace(pluginDirectory)
                ? null
                : Path.GetFileName(pluginDirectory);

            candidates.Add(directoryName);
            candidates.Add(TrimLegacyAssemblyPrefix(directoryName));

            var parentDirectoryName = string.IsNullOrWhiteSpace(pluginDirectory)
                ? null
                : Path.GetFileName(Path.GetDirectoryName(pluginDirectory));
            if (string.Equals(parentDirectoryName, "local", StringComparison.OrdinalIgnoreCase))
                candidates.Add(directoryName);
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var value = FirstNonEmpty(candidate);
            if (!string.IsNullOrEmpty(value) && seen.Add(value))
                yield return value;
        }
    }

    private static string? TrimLegacyAssemblyPrefix(string? value)
    {
        const string prefix = "LenovoLegionToolkit.Plugins.";
        return !string.IsNullOrWhiteSpace(value) && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..]
            : null;
    }

    private static PluginManifest? TryReadManifestNearPlugin(PluginMetadata? metadata)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(metadata?.FilePath))
                return null;

            var pluginDirectory = Path.GetDirectoryName(metadata.FilePath);
            if (string.IsNullOrWhiteSpace(pluginDirectory))
                return null;

            foreach (var manifestFileName in ManifestFileNames)
            {
                var manifestPath = Path.Combine(pluginDirectory, manifestFileName);
                if (!File.Exists(manifestPath))
                    continue;

                return JsonSerializer.Deserialize<PluginManifest>(
                    File.ReadAllText(manifestPath),
                    ManifestJsonOptions);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read plugin manifest near {metadata?.FilePath}: {ex.Message}", ex);
        }

        return null;
    }

    private static WindowsOptimizationCategoryDefinition? TryCreateManifestCategory(InstalledPluginContext context) =>
        TryCreateManifestCategory(context.InstalledPluginId, context.Manifest) ??
        TryCreateManifestCategory(context.InstalledPluginId);

    private static WindowsOptimizationCategoryDefinition? TryCreateManifestCategory(string pluginId)
    {
        var manifest = PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);
        return TryCreateManifestCategory(pluginId, manifest);
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

    private static WindowsOptimizationCategoryDefinition? TryGetOptimizationCategoryByConvention(IPlugin plugin)
    {
        try
        {
            var method = plugin.GetType().GetMethod(
                "GetOptimizationCategory",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            if (method is null ||
                !typeof(WindowsOptimizationCategoryDefinition).IsAssignableFrom(method.ReturnType))
            {
                return null;
            }

            return method.Invoke(plugin, null) as WindowsOptimizationCategoryDefinition;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get convention optimization category from plugin {plugin.Id}: {ex.Message}", ex);

            return null;
        }
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
            Recommended: action.Recommended ?? false);
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

    private sealed record InstalledPluginContext(
        IPlugin Plugin,
        string InstalledPluginId,
        PluginManifest? Manifest,
        IReadOnlyList<string> KnownPluginIds);
}
