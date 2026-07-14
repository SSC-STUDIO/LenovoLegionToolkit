using System.Collections.Generic;

namespace UniversalDeviceToolkit.Lib.Optimization;

/// <summary>
/// Extension point for providing additional optimization categories from plugins.
/// Implemented by UniversalDeviceToolkit.Lib.Plugins to avoid circular project references.
/// </summary>
public interface IOptimizationCategoryExtender
{
    IReadOnlyList<WindowsOptimizationCategoryDefinition> GetPluginCategories();
}
