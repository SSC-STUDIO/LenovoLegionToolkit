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

        var visibleById = IndexById(visibleFeatures);
        var sameCollection = ReferenceEquals(visibleFeatures, allFeatures);
        var allById = sameCollection ? visibleById : IndexById(allFeatures);

        foreach (var feature in importedFeatures)
        {
            if (feature is null || feature.Id <= 0)
            {
                continue;
            }

            OverlayOrAdd(visibleFeatures, visibleById, feature);
            if (!sameCollection)
            {
                OverlayOrAdd(allFeatures, allById, feature);
            }
        }
    }

    private static Dictionary<int, FeatureFlagInfo> IndexById(IEnumerable<FeatureFlagInfo> features)
    {
        var byId = new Dictionary<int, FeatureFlagInfo>();
        foreach (var feature in features)
        {
            if (feature is null || feature.Id <= 0)
            {
                continue;
            }

            byId[feature.Id] = feature;
        }

        return byId;
    }

    private static void OverlayOrAdd(
        ICollection<FeatureFlagInfo> target,
        Dictionary<int, FeatureFlagInfo> byId,
        FeatureFlagInfo incoming)
    {
        if (byId.TryGetValue(incoming.Id, out var existing))
        {
            OverlayExisting(existing, incoming);
            return;
        }

        target.Add(incoming);
        byId[incoming.Id] = incoming;
    }

    private static void OverlayExisting(FeatureFlagInfo existing, FeatureFlagInfo incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming.Name) &&
            !incoming.Name.Equals($"Feature {incoming.Id}", StringComparison.OrdinalIgnoreCase))
        {
            existing.Name = incoming.Name;
        }

        if (!string.IsNullOrWhiteSpace(incoming.Description))
        {
            existing.Description = incoming.Description;
        }

        if (incoming.Status != FeatureFlagStatus.Unknown)
        {
            existing.Status = incoming.Status;
        }
    }
}
