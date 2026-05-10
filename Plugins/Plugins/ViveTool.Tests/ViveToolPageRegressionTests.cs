using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using LenovoLegionToolkit.Plugins.ViveTool.Utils;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

public class ViveToolPageRegressionTests
{
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

        FeatureMerger.MergeImportedFeatures(visibleFeatures, allFeatures, new[] { importedFeature });

        var searchResults = FeatureFilter.FilterFeatures(allFeatures, "importedfeature");

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

        var result = FeatureFilter.FilterFeatures(allFeatures, "300");

        Assert.Single(result);
        Assert.Equal(300, result[0].Id);
    }
}
