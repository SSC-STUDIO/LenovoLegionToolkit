using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Implementation of plugin dependency resolver using topological sorting
/// </summary>
public class DependencyResolver : IDependencyResolver
{
    /// <inheritdoc />
    public DependencyResolutionResult ResolveDependencies(Dictionary<string, List<PluginDependency>> plugins)
    {
        var result = new DependencyResolutionResult();

        try
        {
            // First, detect circular dependencies
            var circularDeps = DetectCircularDependencies(plugins);
            if (circularDeps.Any())
            {
                result.Success = false;
                result.CircularDependencies = circularDeps;
                result.ErrorMessage = $"Circular dependencies detected: {string.Join(", ", circularDeps.Select(c => string.Join(" -> ", c)))}";
                return result;
            }

            // Build dependency graph
            var graph = BuildDependencyGraph(plugins);
            var inDegree = CalculateInDegrees(graph);

            // Topological sort using Kahn's algorithm
            var queue = new Queue<string>();
            var loadOrder = new List<string>();

            // Start with plugins that have no dependencies
            foreach (var plugin in plugins.Keys)
            {
                if (inDegree[plugin] == 0)
                {
                    queue.Enqueue(plugin);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                loadOrder.Add(current);

                // Find all plugins that depend on current
                foreach (var dependent in graph[current])
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }

            // Check if all plugins were included
            if (loadOrder.Count != plugins.Count)
            {
                // This shouldn't happen if circular detection worked, but just in case
                var unresolved = plugins.Keys.Except(loadOrder).ToList();
                result.Success = false;
                result.ErrorMessage = $"Could not resolve dependencies for: {string.Join(", ", unresolved)}";
                return result;
            }

            // Validate version constraints
            var versionConflicts = ValidateVersionConstraints(plugins);
            if (versionConflicts.Any())
            {
                result.Success = false;
                result.VersionConflicts = versionConflicts;
                result.ErrorMessage = $"Version conflicts detected for: {string.Join(", ", versionConflicts.Select(v => v.PluginId))}";
                return result;
            }

            result.Success = true;
            result.LoadOrder = loadOrder;

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"Dependency resolution successful. Load order: {string.Join(" -> ", loadOrder)}");
            }

            return result;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"Dependency resolution failed: {ex.Message}", ex);
            }

            result.Success = false;
            result.ErrorMessage = $"Dependency resolution error: {ex.Message}";
            return result;
        }
    }

    /// <inheritdoc />
    public bool ValidateInstallation(
        string pluginId,
        List<PluginDependency> dependencies,
        Dictionary<string, string> installedPlugins)
    {
        foreach (var dependency in dependencies)
        {
            if (dependency.IsOptional)
                continue;

            if (!installedPlugins.TryGetValue(dependency.PluginId, out var installedVersion))
            {
                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Validation failed: Required dependency {dependency.PluginId} not found for {pluginId}");
                }
                return false;
            }

            // Check version constraints
            if (!IsVersionCompatible(installedVersion, dependency.MinVersion, dependency.MaxVersion))
            {
                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Validation failed: Version mismatch for {dependency.PluginId}. " +
                        $"Installed: {installedVersion}, Required: {dependency.MinVersion}-{dependency.MaxVersion}");
                }
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public List<string> GetDependentPlugins(string pluginId, Dictionary<string, List<PluginDependency>> allPlugins)
    {
        var dependents = new List<string>();

        foreach (var (otherPluginId, dependencies) in allPlugins)
        {
            if (otherPluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (dependencies.Any(d => d.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase) && !d.IsOptional))
            {
                dependents.Add(otherPluginId);
            }
        }

        return dependents;
    }

    /// <inheritdoc />
    public List<List<string>> DetectCircularDependencies(Dictionary<string, List<PluginDependency>> plugins)
    {
        var circularDependencies = new List<List<string>>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var pluginId in plugins.Keys)
        {
            if (!visited.Contains(pluginId))
            {
                var path = new List<string>();
                DetectCircularDependenciesDFS(
                    pluginId,
                    plugins,
                    visited,
                    recursionStack,
                    path,
                    circularDependencies);
            }
        }

        return circularDependencies;
    }

    /// <inheritdoc />
    public DependencyGraph GetDependencyGraph(Dictionary<string, List<PluginDependency>> plugins)
    {
        var graph = new DependencyGraph();
        var nodes = new Dictionary<string, GraphNode>();

        // Create nodes
        foreach (var pluginId in plugins.Keys)
        {
            var node = new GraphNode
            {
                Id = pluginId,
                Name = pluginId, // Could be enhanced to get actual plugin name
                Version = "1.0.0" // Could be enhanced to get actual version
            };
            nodes[pluginId] = node;
            graph.Nodes.Add(node);
        }

        // Create edges
        foreach (var (pluginId, dependencies) in plugins)
        {
            foreach (var dependency in dependencies)
            {
                var edge = new GraphEdge
                {
                    From = pluginId,
                    To = dependency.PluginId,
                    IsOptional = dependency.IsOptional,
                    VersionRequirement = FormatVersionRequirement(dependency.MinVersion, dependency.MaxVersion)
                };
                graph.Edges.Add(edge);

                // Add dependency node if not exists
                if (!nodes.ContainsKey(dependency.PluginId))
                {
                    var depNode = new GraphNode
                    {
                        Id = dependency.PluginId,
                        Name = dependency.PluginId,
                        Version = "?",
                        IsInstalled = false
                    };
                    nodes[dependency.PluginId] = depNode;
                    graph.Nodes.Add(depNode);
                }
            }
        }

        return graph;
    }

    #region Private Methods

    private Dictionary<string, List<string>> BuildDependencyGraph(Dictionary<string, List<PluginDependency>> plugins)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var pluginId in plugins.Keys)
        {
            graph[pluginId] = new List<string>();
        }

        foreach (var (pluginId, dependencies) in plugins)
        {
            foreach (var dependency in dependencies)
            {
                if (!dependency.IsOptional && graph.ContainsKey(dependency.PluginId))
                {
                    // dependency.PluginId must be loaded before pluginId
                    // So pluginId depends on dependency.PluginId
                    if (!graph[dependency.PluginId].Contains(pluginId))
                    {
                        graph[dependency.PluginId].Add(pluginId);
                    }
                }
            }
        }

        return graph;
    }

    private Dictionary<string, int> CalculateInDegrees(Dictionary<string, List<string>> graph)
    {
        var inDegree = new Dictionary<string, int>();

        foreach (var node in graph.Keys)
        {
            inDegree[node] = 0;
        }

        foreach (var neighbors in graph.Values)
        {
            foreach (var neighbor in neighbors)
            {
                if (inDegree.ContainsKey(neighbor))
                {
                    inDegree[neighbor]++;
                }
            }
        }

        return inDegree;
    }

    private void DetectCircularDependenciesDFS(
        string current,
        Dictionary<string, List<PluginDependency>> plugins,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> currentPath,
        List<List<string>> circularDependencies)
    {
        visited.Add(current);
        recursionStack.Add(current);
        currentPath.Add(current);

        if (plugins.TryGetValue(current, out var dependencies))
        {
            foreach (var dependency in dependencies.Where(d => !d.IsOptional))
            {
                var depId = dependency.PluginId;

                if (!visited.Contains(depId))
                {
                    DetectCircularDependenciesDFS(depId, plugins, visited, recursionStack, currentPath, circularDependencies);
                }
                else if (recursionStack.Contains(depId))
                {
                    // Found a cycle
                    var cycleStart = currentPath.IndexOf(depId);
                    var cycle = currentPath.Skip(cycleStart).ToList();
                    cycle.Add(depId); // Close the cycle
                    circularDependencies.Add(cycle);
                }
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        recursionStack.Remove(current);
    }

    private List<VersionConflict> ValidateVersionConstraints(Dictionary<string, List<PluginDependency>> plugins)
    {
        var conflicts = new List<VersionConflict>();
        var pluginVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Collect all plugin versions (in real implementation, this would come from plugin metadata)
        foreach (var pluginId in plugins.Keys)
        {
            pluginVersions[pluginId] = "1.0.0"; // Placeholder - should get actual version
        }

        foreach (var (pluginId, dependencies) in plugins)
        {
            foreach (var dependency in dependencies)
            {
                if (pluginVersions.TryGetValue(dependency.PluginId, out var actualVersion))
                {
                    if (!IsVersionCompatible(actualVersion, dependency.MinVersion, dependency.MaxVersion))
                    {
                        conflicts.Add(new VersionConflict
                        {
                            PluginId = dependency.PluginId,
                            ActualVersion = actualVersion,
                            RequiredVersion = FormatVersionRequirement(dependency.MinVersion, dependency.MaxVersion),
                            RequiredBy = pluginId
                        });
                    }
                }
            }
        }

        return conflicts;
    }

    private bool IsVersionCompatible(string actualVersion, string? minVersion, string? maxVersion)
    {
        if (!Version.TryParse(actualVersion, out var actual))
        {
            return true; // If we can't parse, assume compatible
        }

        if (!string.IsNullOrEmpty(minVersion))
        {
            if (Version.TryParse(minVersion, out var min) && actual < min)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(maxVersion))
        {
            if (Version.TryParse(maxVersion, out var max) && actual > max)
            {
                return false;
            }
        }

        return true;
    }

    private string FormatVersionRequirement(string? minVersion, string? maxVersion)
    {
        if (!string.IsNullOrEmpty(minVersion) && !string.IsNullOrEmpty(maxVersion))
        {
            return $">={minVersion} <= {maxVersion}";
        }
        else if (!string.IsNullOrEmpty(minVersion))
        {
            return $">={minVersion}";
        }
        else if (!string.IsNullOrEmpty(maxVersion))
        {
            return $"<= {maxVersion}";
        }
        else
        {
            return "*";
        }
    }

    #endregion
}
