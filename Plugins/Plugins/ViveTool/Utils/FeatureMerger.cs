using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Plugins.ViveTool.Services;

namespace LenovoLegionToolkit.Plugins.ViveTool.Utils;

public static class FeatureMerger
{
    public static void MergeImportedFeatures(
        ICollection<FeatureFlagInfo> visibleFeatures,
        ICollection<FeatureFlagInfo> allFeatures,
        IEnumerable<FeatureFlagInfo> importedFeatures)
    {
        ArgumentNullException.ThrowIfNull(visibleFeatures);
        ArgumentNullException.ThrowIfNull(allFeatures);
        ArgumentNullException.ThrowIfNull(importedFeatures);

        foreach (var feature in importedFeatures)
        {
            if (!visibleFeatures.Any(f => f.Id == feature.Id))
            {
                visibleFeatures.Add(feature);
            }

            if (!allFeatures.Any(f => f.Id == feature.Id))
            {
                allFeatures.Add(feature);
            }
        }
    }
}
