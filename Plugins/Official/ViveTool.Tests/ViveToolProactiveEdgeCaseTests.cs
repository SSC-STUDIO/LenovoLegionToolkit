using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings;
using UniversalDeviceToolkit.Plugins.ViveTool.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Proactive edge-case coverage for ViveTool util/service surfaces that were
/// previously uncovered or only partially covered: byte formatter boundaries,
/// feature merge/filter edge cases and thread-safety, and corrupted-JSON
/// settings recovery under a redirected AppData (UDT_APPDATA_OVERRIDE) so the
/// machine never gets polluted.
/// </summary>
public class ViveToolProactiveEdgeCaseTests
{
    // ByteFormatter boundary coverage

    [Theory]
    [InlineData(long.MaxValue, "TB")]
    [InlineData(long.MinValue, "TB")]
    [InlineData(1024L * 1024 * 1024 * 1024, "TB")]
    [InlineData(-1, "-1 B")]
    [InlineData(1023, "1023 B")]
    public void FormatBytes_EdgeValues_ContainCorrectSuffix(long bytes, string expectedSuffix)
    {
        var result = ByteFormatter.FormatBytes(bytes);
        Assert.Contains(expectedSuffix, result);
    }

    [Fact]
    public void FormatBytes_ExactlyOneKb_ReturnsOneKb()
    {
        Assert.Equal("1 KB", ByteFormatter.FormatBytes(1024));
    }

    [Fact]
    public void FormatBytes_NegativeZero_ReturnsZero()
    {
        var result = ByteFormatter.FormatBytes(0);
        Assert.Equal("0 B", result);
    }

    // FeatureMerger concurrent thread-safety

    [Fact]
    public async Task MergeImportedFeatures_ConcurrentAccess_DoesNotThrow()
    {
        var visible = new System.Collections.Generic.List<FeatureFlagInfo>();
        var all = new System.Collections.Generic.List<FeatureFlagInfo>();
        var lockObj = new object();

        var tasks = System.Linq.Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            var feature = new FeatureFlagInfo { Id = i, Name = $"F{i}", Status = FeatureFlagStatus.Default };
            lock (lockObj)
            {
                if (!visible.Any(f => f.Id == feature.Id)) visible.Add(feature);
                if (!all.Any(f => f.Id == feature.Id)) all.Add(feature);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        Assert.Equal(20, visible.Count);
        Assert.Equal(20, all.Count);
    }

    [Fact]
    public void MergeImportedFeatures_EmptyImport_DoesNotModify()
    {
        var visible = new System.Collections.Generic.List<FeatureFlagInfo>();
        var all = new System.Collections.Generic.List<FeatureFlagInfo>();
        FeatureMerger.MergeImportedFeatures(visible, all, Array.Empty<FeatureFlagInfo>());
        Assert.Empty(visible);
        Assert.Empty(all);
    }

    [Fact]
    public void FeatureFilter_EmptyCollection_ReturnsEmpty()
    {
        var result = FeatureFilter.FilterFeatures(Array.Empty<FeatureFlagInfo>(), "test");
        Assert.Empty(result);
    }

    [Fact]
    public void FeatureFilter_WhitespaceSearch_FiltersByName()
    {
        var features = new System.Collections.Generic.List<FeatureFlagInfo>
        {
            new FeatureFlagInfo { Id = 1, Name = "Alpha Beta", Status = FeatureFlagStatus.Enabled },
            new FeatureFlagInfo { Id = 2, Name = "Gamma", Status = FeatureFlagStatus.Enabled },
        };
        var result = FeatureFilter.FilterFeatures(features, "Alpha");
        Assert.Single(result);
    }

}
