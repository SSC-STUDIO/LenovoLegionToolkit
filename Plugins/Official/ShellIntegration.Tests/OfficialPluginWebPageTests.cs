using System;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration.Tests;

[Collection("ShellIntegrationResourceCulture")]
public class OfficialPluginWebPageTests
{
    [Fact]
    public void Manifest_DeclaresWebPageAndClearsNativeSettingsPage()
    {
        OfficialPluginWebPageAssertions.AssertManifestDeclaresWebPage("ShellIntegration");

        var manifest = OfficialPluginWebPageAssertions.ReadManifestText("ShellIntegration");
        Assert.DoesNotContain("ShellIntegrationSettingsControl", manifest, StringComparison.Ordinal);

        var html = OfficialPluginWebPageAssertions.ReadWebPageHtml("ShellIntegration");
        Assert.Contains("plugin.shell.getStatus", html, StringComparison.Ordinal);
        Assert.Contains("plugin.shell.enable", html, StringComparison.Ordinal);
        Assert.Contains("plugin.shell.disable", html, StringComparison.Ordinal);
        Assert.Contains("plugin.shell.applyPreset", html, StringComparison.Ordinal);
        Assert.Contains("plugin.shell.exportProfile", html, StringComparison.Ordinal);
        Assert.Contains("plugin.shell.importProfile", html, StringComparison.Ordinal);
        Assert.Contains("dialog:open-file", html, StringComparison.Ordinal);
        Assert.Contains("dialog:save-file", html, StringComparison.Ordinal);
        Assert.Contains("typeof bridge.invoke", html, StringComparison.Ordinal);
        Assert.Contains("function asPath", html, StringComparison.Ordinal);
    }
}
