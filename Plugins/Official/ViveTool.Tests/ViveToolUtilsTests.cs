using System;
using System.Collections.Generic;
using System.Linq;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using UniversalDeviceToolkit.Plugins.ViveTool.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

public class ViveToolUtilsTests
{
    // ── ByteFormatter ────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(-1024, "-1 KB")]
    public void FormatBytes_ReturnsCorrectFormat(long bytes, string expected)
    {
        Assert.Equal(expected, ByteFormatter.FormatBytes(bytes));
    }

    [Fact]
    public void FormatBytes_LargeValue_ReturnsGB()
    {
        Assert.Contains("GB", ByteFormatter.FormatBytes(5L * 1024 * 1024 * 1024));
    }

    // ── FeatureFilter ────────────────────────────────────────────────

    private static FeatureFlagInfo F(int id, string name, FeatureFlagStatus status, string desc = "") =>
        new() { Id = id, Name = name, Status = status, Description = desc };

    [Fact]
    public void FilterFeatures_NullSearch_ReturnsAll()
    {
        var features = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled), F(2, "B", FeatureFlagStatus.Disabled) };
        Assert.Equal(2, FeatureFilter.FilterFeatures(features, null).Count);
    }

    [Fact]
    public void FilterFeatures_Keyword_FiltersByName()
    {
        var features = new List<FeatureFlagInfo> { F(1, "Alpha", FeatureFlagStatus.Enabled), F(2, "Beta", FeatureFlagStatus.Enabled) };
        var result = FeatureFilter.FilterFeatures(features, "alpha");
        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void FilterFeatures_StatusFilter_FiltersByStatus()
    {
        var features = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled), F(2, "B", FeatureFlagStatus.Disabled), F(3, "C", FeatureFlagStatus.Enabled) };
        var result = FeatureFilter.FilterFeatures(features, null, FeatureFlagStatus.Disabled);
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void FilterFeatures_SearchById()
    {
        var features = new List<FeatureFlagInfo> { F(12345, "X", FeatureFlagStatus.Default), F(67890, "Y", FeatureFlagStatus.Default) };
        Assert.Single(FeatureFilter.FilterFeatures(features, "12345"));
    }

    [Fact]
    public void FilterFeatures_DescriptionSearch()
    {
        var features = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled, "unique desc"), F(2, "B", FeatureFlagStatus.Enabled, "other") };
        Assert.Single(FeatureFilter.FilterFeatures(features, "unique"));
    }

    [Fact]
    public void FilterFeatures_CombinedKeywordAndStatus()
    {
        var features = new List<FeatureFlagInfo>
        {
            F(1, "Alpha", FeatureFlagStatus.Enabled, "desc A"),
            F(2, "Alpha", FeatureFlagStatus.Disabled, "desc B"),
            F(3, "Beta", FeatureFlagStatus.Enabled, "desc C"),
        };
        var result = FeatureFilter.FilterFeatures(features, "alpha", FeatureFlagStatus.Disabled);
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void FilterFeatures_EmptySearch_ReturnsAll()
    {
        var features = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled) };
        Assert.Single(FeatureFilter.FilterFeatures(features, ""));
    }

    [Fact]
    public void SummarizeFeatures_CountsAllStatuses()
    {
        var features = new List<FeatureFlagInfo>
        {
            F(1, "A", FeatureFlagStatus.Enabled), F(2, "B", FeatureFlagStatus.Enabled),
            F(3, "C", FeatureFlagStatus.Disabled), F(4, "D", FeatureFlagStatus.Default),
            F(5, "E", FeatureFlagStatus.Unknown),
        };
        var s = FeatureFilter.SummarizeFeatures(features);
        Assert.Equal(5, s.Total); Assert.Equal(2, s.Enabled);
        Assert.Equal(1, s.Disabled); Assert.Equal(1, s.Default); Assert.Equal(1, s.Unknown);
    }

    [Fact]
    public void SummarizeFeatures_Empty_ReturnsZeros()
    {
        var s = FeatureFilter.SummarizeFeatures(Array.Empty<FeatureFlagInfo>());
        Assert.Equal(0, s.Total);
        Assert.Equal(0, s.Enabled); Assert.Equal(0, s.Disabled);
        Assert.Equal(0, s.Default); Assert.Equal(0, s.Unknown);
    }

    [Fact]
    public void SummarizeFeatures_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FeatureFilter.SummarizeFeatures(null!));
    }

    // ── FeatureMerger ────────────────────────────────────────────────

    [Fact]
    public void MergeImportedFeatures_AddsNewFeatures()
    {
        var visible = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled) };
        var all = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled) };
        var imported = new List<FeatureFlagInfo> { F(2, "B", FeatureFlagStatus.Disabled) };
        FeatureMerger.MergeImportedFeatures(visible, all, imported);
        Assert.Equal(2, visible.Count);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void MergeImportedFeatures_DeduplicatesById()
    {
        var visible = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled) };
        var all = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled) };
        var imported = new List<FeatureFlagInfo> { F(1, "A Updated", FeatureFlagStatus.Disabled, "New description") };
        FeatureMerger.MergeImportedFeatures(visible, all, imported);
        Assert.Single(visible);
        Assert.Single(all);
        Assert.Equal("A Updated", visible[0].Name);
        Assert.Equal(FeatureFlagStatus.Disabled, visible[0].Status);
        Assert.Equal("New description", visible[0].Description);
        Assert.Equal("A Updated", all[0].Name);
        Assert.Equal(FeatureFlagStatus.Disabled, all[0].Status);
    }

    [Fact]
    public void MergeImportedFeatures_SameCollection_DoesNotDuplicate()
    {
        var features = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled) };
        var imported = new List<FeatureFlagInfo> { F(2, "B", FeatureFlagStatus.Disabled) };
        FeatureMerger.MergeImportedFeatures(features, features, imported);
        Assert.Equal(2, features.Count);
        Assert.Equal(1, features.Count(feature => feature.Id == 2));
    }

    [Fact]
    public void MergeImportedFeatures_SkipsInvalidIdsAndPreservesUnknownStatus()
    {
        var visible = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled, "Existing") };
        var all = new List<FeatureFlagInfo> { F(1, "A", FeatureFlagStatus.Enabled, "Existing") };
        var imported = new List<FeatureFlagInfo>
        {
            F(0, "Zero", FeatureFlagStatus.Disabled),
            F(-5, "Negative", FeatureFlagStatus.Disabled),
            F(1, "Feature 1", FeatureFlagStatus.Unknown),
        };
        FeatureMerger.MergeImportedFeatures(visible, all, imported);
        Assert.Single(visible);
        Assert.Equal("A", visible[0].Name);
        Assert.Equal(FeatureFlagStatus.Enabled, visible[0].Status);
        Assert.Equal("Existing", visible[0].Description);
    }

    [Fact]
    public void MergeImportedFeatures_LastWriteWinsForDuplicateImports()
    {
        var visible = new List<FeatureFlagInfo>();
        var all = new List<FeatureFlagInfo>();
        var imported = new List<FeatureFlagInfo>
        {
            F(9, "First", FeatureFlagStatus.Enabled),
            F(9, "Second", FeatureFlagStatus.Disabled, "Latest"),
        };
        FeatureMerger.MergeImportedFeatures(visible, all, imported);
        var feature = Assert.Single(visible);
        Assert.Equal("Second", feature.Name);
        Assert.Equal(FeatureFlagStatus.Disabled, feature.Status);
        Assert.Equal("Latest", feature.Description);
    }

    [Fact]
    public void MergeImportedFeatures_MultipleImports()
    {
        var visible = new List<FeatureFlagInfo>();
        var all = new List<FeatureFlagInfo>();
        var imported = new List<FeatureFlagInfo>
        {
            F(1, "A", FeatureFlagStatus.Enabled),
            F(2, "B", FeatureFlagStatus.Disabled),
            F(3, "C", FeatureFlagStatus.Default),
        };
        FeatureMerger.MergeImportedFeatures(visible, all, imported);
        Assert.Equal(3, visible.Count);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void MergeImportedFeatures_NullArguments_Throw()
    {
        var list = new List<FeatureFlagInfo>();
        var empty = Array.Empty<FeatureFlagInfo>();
        Assert.Throws<ArgumentNullException>(() => FeatureMerger.MergeImportedFeatures(null!, list, empty));
        Assert.Throws<ArgumentNullException>(() => FeatureMerger.MergeImportedFeatures(list, null!, empty));
        Assert.Throws<ArgumentNullException>(() => FeatureMerger.MergeImportedFeatures(list, list, null!));
    }
}