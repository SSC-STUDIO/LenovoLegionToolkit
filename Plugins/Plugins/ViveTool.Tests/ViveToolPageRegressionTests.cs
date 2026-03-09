using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LenovoLegionToolkit.Plugins.ViveTool;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

public class ViveToolPageRegressionTests
{
    private static readonly MethodInfo MergeImportedFeaturesMethod = typeof(ViveToolPage)
        .GetMethod("MergeImportedFeatures", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo FilterFeaturesMethod = typeof(ViveToolPage)
        .GetMethod("FilterFeatures", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void MergeImportedFeatures_KeepsImportedFeatureSearchableAfterMerge()
    {
        var visibleFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 100, Name = "BaseFeature", Description = "Existing feature", Status = FeatureFlagStatus.Default },
        };
        var allFeatures = new List<FeatureFlagInfo>(visibleFeatures);
        var importedFeature = new FeatureFlagInfo
        {
            Id = 200,
            Name = "ImportedFeature",
            Description = "Imported after initial load",
            Status = FeatureFlagStatus.Enabled,
        };

        MergeImportedFeaturesMethod.Invoke(null, new object[] { visibleFeatures, allFeatures, new[] { importedFeature } });

        var searchResults = InvokeFilterFeatures(allFeatures, "importedfeature");

        Assert.Contains(visibleFeatures, feature => feature.Id == importedFeature.Id);
        Assert.Contains(allFeatures, feature => feature.Id == importedFeature.Id);
        Assert.Single(searchResults);
        Assert.Same(importedFeature, searchResults.Single());
    }

    [Fact]
    public void FilterFeatures_ToleratesNullNameAndDescription()
    {
        var allFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 300, Name = null!, Description = null!, Status = FeatureFlagStatus.Unknown },
        };

        var result = InvokeFilterFeatures(allFeatures, "300");

        Assert.Single(result);
        Assert.Equal(300, result[0].Id);
    }

    private static IReadOnlyList<FeatureFlagInfo> InvokeFilterFeatures(IEnumerable<FeatureFlagInfo> allFeatures, string? searchText)
    {
        return (IReadOnlyList<FeatureFlagInfo>)FilterFeaturesMethod.Invoke(null, new object?[] { allFeatures, searchText })!;
    }
}
