using System;
using System.Collections.Generic;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Utils;

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

        // Build existing-ID sets once (O(n + m)) instead of re-scanning
        // the full collection for each imported feature (O(n*m)).
        var existingVisibleIds = new HashSet<int>();
        foreach (var f in visibleFeatures)
        {
            existingVisibleIds.Add(f.Id);
        }

        var existingAllIds = new HashSet<int>();
        foreach (var f in allFeatures)
        {
            existingAllIds.Add(f.Id);
        }

        foreach (var feature in importedFeatures)
        {
            if (existingVisibleIds.Add(feature.Id))
            {
                visibleFeatures.Add(feature);
            }

            if (existingAllIds.Add(feature.Id))
            {
                allFeatures.Add(feature);
            }
        }
    }
}
