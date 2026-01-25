using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Plugins.SDK;

/// <summary>
/// Interface for plugins that can provide optimization categories
/// This is a forwarder interface that inherits from the main IOptimizationCategoryProvider in Lib
/// </summary>
public interface IOptimizationCategoryProvider : LenovoLegionToolkit.Lib.Plugins.IOptimizationCategoryProvider
{
    // All interface members are inherited from the base interface in Lib
}
