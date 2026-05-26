using System.Collections.Generic;

namespace LenovoLegionToolkit.Lib.Optimization;

/// <summary>
/// Extension point for providing additional optimization categories from plugins.
/// Implemented by LenovoLegionToolkit.Lib.Plugins to avoid circular project references.
/// </summary>
public interface IOptimizationCategoryExtender
{
    IReadOnlyList<WindowsOptimizationCategoryDefinition> GetPluginCategories();
}
