using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Plugins.ViveTool.Services;

namespace LenovoLegionToolkit.Plugins.ViveTool.Utils;

public static class FeatureFilter
{
    public static IReadOnlyList<FeatureFlagInfo> FilterFeatures(IEnumerable<FeatureFlagInfo> allFeatures, string? searchText)
    {
        var lowerKeyword = (searchText ?? string.Empty).ToLowerInvariant();

        return allFeatures.Where(feature =>
                string.IsNullOrWhiteSpace(lowerKeyword) ||
                feature.Id.ToString().Contains(lowerKeyword) ||
                (feature.Name ?? string.Empty).ToLowerInvariant().Contains(lowerKeyword) ||
                (feature.Description ?? string.Empty).ToLowerInvariant().Contains(lowerKeyword))
            .ToList();
    }
}
