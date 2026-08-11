using System.Collections.Generic;
using System.Linq;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using UniversalDeviceToolkit.Plugins.ViveTool.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

public class ViveToolFeatureMergeRegressionTests
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

    [Fact]
    public void FilterFeatures_WithStatusFilter_ReturnsOnlyMatchingStatuses()
    {
        var allFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 100, Name = "Alpha", Description = "Enabled feature", Status = FeatureFlagStatus.Enabled },
            new() { Id = 200, Name = "Beta", Description = "Disabled feature", Status = FeatureFlagStatus.Disabled },
            new() { Id = 300, Name = "Gamma", Description = "Default feature", Status = FeatureFlagStatus.Default },
        };

        var result = FeatureFilter.FilterFeatures(allFeatures, string.Empty, FeatureFlagStatus.Disabled);

        var feature = Assert.Single(result);
        Assert.Equal(200, feature.Id);
    }

    [Fact]
    public void SummarizeFeatures_ReturnsCountsPerStatus()
    {
        var allFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 100, Name = "Alpha", Status = FeatureFlagStatus.Enabled },
            new() { Id = 200, Name = "Beta", Status = FeatureFlagStatus.Disabled },
            new() { Id = 300, Name = "Gamma", Status = FeatureFlagStatus.Default },
            new() { Id = 400, Name = "Delta", Status = FeatureFlagStatus.Unknown },
            new() { Id = 500, Name = "Epsilon", Status = FeatureFlagStatus.Enabled },
        };

        var summary = FeatureFilter.SummarizeFeatures(allFeatures);

        Assert.Equal(5, summary.Total);
        Assert.Equal(2, summary.Enabled);
        Assert.Equal(1, summary.Disabled);
        Assert.Equal(1, summary.Default);
        Assert.Equal(1, summary.Unknown);
    }
}
