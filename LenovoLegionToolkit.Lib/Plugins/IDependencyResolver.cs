using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Represents a plugin dependency with version requirements
/// </summary>
public class PluginDependency
{
    /// <summary>
    /// The plugin ID that is required
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Minimum required version (inclusive)
    /// </summary>
    public string? MinVersion { get; set; }

    /// <summary>
    /// Maximum allowed version (inclusive)
    /// </summary>
    public string? MaxVersion { get; set; }

    /// <summary>
    /// Whether this dependency is optional
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Reason for this dependency (for error messages)
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Result of dependency resolution
/// </summary>
public class DependencyResolutionResult
{
    /// <summary>
    /// Whether resolution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Ordered list of plugins to load (dependencies first)
    /// </summary>
    public List<string> LoadOrder { get; set; } = new();

    /// <summary>
    /// Error message if resolution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Missing dependencies that couldn't be resolved
    /// </summary>
    public List<PluginDependency> MissingDependencies { get; set; } = new();

    /// <summary>
    /// Circular dependencies detected
    /// </summary>
    public List<List<string>> CircularDependencies { get; set; } = new();

    /// <summary>
    /// Version conflicts detected
    /// </summary>
    public List<VersionConflict> VersionConflicts { get; set; } = new();
}

/// <summary>
/// Represents a version conflict between plugins
/// </summary>
public class VersionConflict
{
    /// <summary>
    /// The plugin ID with conflicting version requirements
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// The required version range
    /// </summary>
    public string RequiredVersion { get; set; } = string.Empty;

    /// <summary>
    /// The actual version found
    /// </summary>
    public string ActualVersion { get; set; } = string.Empty;

    /// <summary>
    /// The plugin that requires this dependency
    /// </summary>
    public string RequiredBy { get; set; } = string.Empty;
}

/// <summary>
/// Interface for resolving plugin dependencies
/// </summary>
public interface IDependencyResolver
{
    /// <summary>
    /// Resolves dependencies for a set of plugins and returns the correct load order
    /// </summary>
    /// <param name="plugins">Dictionary of plugin ID to its dependencies</param>
    /// <returns>Resolution result with load order or errors</returns>
    DependencyResolutionResult ResolveDependencies(Dictionary<string, List<PluginDependency>> plugins);

    /// <summary>
    /// Validates if a specific plugin can be installed with current dependencies
    /// </summary>
    /// <param name="pluginId">The plugin to validate</param>
    /// <param name="dependencies">The plugin's dependencies</param>
    /// <param name="installedPlugins">Currently installed plugins with their versions</param>
    /// <returns>True if installation is valid</returns>
    bool ValidateInstallation(
        string pluginId,
        List<PluginDependency> dependencies,
        Dictionary<string, string> installedPlugins);

    /// <summary>
    /// Checks if uninstalling a plugin would break other plugins
    /// </summary>
    /// <param name="pluginId">The plugin to uninstall</param>
    /// <param name="allPlugins">All registered plugins with their dependencies</param>
    /// <returns>List of plugins that depend on the plugin being uninstalled</returns>
    List<string> GetDependentPlugins(string pluginId, Dictionary<string, List<PluginDependency>> allPlugins);

    /// <summary>
    /// Detects circular dependencies in the plugin graph
    /// </summary>
    /// <param name="plugins">Dictionary of plugin ID to its dependencies</param>
    /// <returns>List of circular dependency chains</returns>
    List<List<string>> DetectCircularDependencies(Dictionary<string, List<PluginDependency>> plugins);

    /// <summary>
    /// Gets the dependency graph for visualization
    /// </summary>
    /// <param name="plugins">Dictionary of plugin ID to its dependencies</param>
    /// <returns>Graph representation of dependencies</returns>
    DependencyGraph GetDependencyGraph(Dictionary<string, List<PluginDependency>> plugins);
}

/// <summary>
/// Represents a dependency graph for visualization
/// </summary>
public class DependencyGraph
{
    /// <summary>
    /// Nodes in the graph (plugins)
    /// </summary>
    public List<GraphNode> Nodes { get; set; } = new();

    /// <summary>
    /// Edges in the graph (dependencies)
    /// </summary>
    public List<GraphEdge> Edges { get; set; } = new();
}

/// <summary>
/// Represents a node in the dependency graph
/// </summary>
public class GraphNode
{
    /// <summary>
    /// Plugin ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Plugin name for display
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Plugin version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether the plugin is installed
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Node position for visualization (optional)
    /// </summary>
    public (double X, double Y)? Position { get; set; }
}

/// <summary>
/// Represents an edge in the dependency graph
/// </summary>
public class GraphEdge
{
    /// <summary>
    /// Source plugin ID (the dependent)
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Target plugin ID (the dependency)
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is an optional dependency
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Version requirement string
    /// </summary>
    public string? VersionRequirement { get; set; }
}
