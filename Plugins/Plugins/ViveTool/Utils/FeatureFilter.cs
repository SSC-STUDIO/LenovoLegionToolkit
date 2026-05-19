using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Plugins.ViveTool.Services;

namespace LenovoLegionToolkit.Plugins.ViveTool.Utils;

public readonly record struct FeatureStatusSummary(
    int Total,
    int Enabled,
    int Disabled,
    int Default,
    int Unknown);

public static class FeatureFilter
{
    public static IReadOnlyList<FeatureFlagInfo> FilterFeatures(
        IEnumerable<FeatureFlagInfo> allFeatures,
        string? searchText,
        FeatureFlagStatus? statusFilter = null)
    {
        var lowerKeyword = (searchText ?? string.Empty).ToLowerInvariant();

        return allFeatures.Where(feature =>
                (statusFilter is null || feature.Status == statusFilter.Value) &&
                (
                    string.IsNullOrWhiteSpace(lowerKeyword) ||
                    feature.Id.ToString().Contains(lowerKeyword) ||
                    (feature.Name ?? string.Empty).ToLowerInvariant().Contains(lowerKeyword) ||
                    (feature.Description ?? string.Empty).ToLowerInvariant().Contains(lowerKeyword)
                ))
            .ToList();
    }

    public static FeatureStatusSummary SummarizeFeatures(IEnumerable<FeatureFlagInfo> features)
    {
        ArgumentNullException.ThrowIfNull(features);

        var total = 0;
        var enabled = 0;
        var disabled = 0;
        var @default = 0;
        var unknown = 0;

        foreach (var feature in features)
        {
            total++;
            switch (feature.Status)
            {
                case FeatureFlagStatus.Enabled:
                    enabled++;
                    break;
                case FeatureFlagStatus.Disabled:
                    disabled++;
                    break;
                case FeatureFlagStatus.Default:
                    @default++;
                    break;
                default:
                    unknown++;
                    break;
            }
        }

        return new FeatureStatusSummary(total, enabled, disabled, @default, unknown);
    }
}
