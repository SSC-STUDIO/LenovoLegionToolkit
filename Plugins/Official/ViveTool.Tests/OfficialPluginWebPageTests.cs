using System;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

public class OfficialPluginWebPageTests
{
    [Fact]
    public void Manifest_DeclaresWebPageAndFeatureTableEntry()
    {
        OfficialPluginWebPageAssertions.AssertManifestDeclaresWebPage("ViveTool");

        var html = OfficialPluginWebPageAssertions.ReadWebPageHtml("ViveTool");
        Assert.Contains("plugin.vive.getStatus", html, StringComparison.Ordinal);
        Assert.Contains("plugin.vive.listFeatures", html, StringComparison.Ordinal);
        Assert.Contains("plugin.vive.searchFeatures", html, StringComparison.Ordinal);
        Assert.Contains("plugin.vive.enableFeature", html, StringComparison.Ordinal);
        Assert.Contains("plugin.vive.download", html, StringComparison.Ordinal);
        Assert.Contains("plugin.vive.downloadProgress", html, StringComparison.Ordinal);
        Assert.Contains("up-table", html, StringComparison.Ordinal);
        Assert.Contains("up-banner", html, StringComparison.Ordinal);
    }
}
