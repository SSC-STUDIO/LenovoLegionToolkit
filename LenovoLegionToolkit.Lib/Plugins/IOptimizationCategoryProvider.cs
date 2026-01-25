using LenovoLegionToolkit.Lib.Optimization;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Interface for plugins that can provide optimization categories
/// </summary>
public interface IOptimizationCategoryProvider
{
    /// <summary>
    /// Get optimization category definition provided by this plugin
    /// </summary>
    /// <returns>Optimization category definition, or null if not provided</returns>
    WindowsOptimizationCategoryDefinition? GetOptimizationCategory();
}