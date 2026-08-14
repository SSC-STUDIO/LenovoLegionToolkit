using System;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.CustomMouse.Tests;

[Collection("CustomMouseResourceCulture")]
public class OfficialPluginWebPageTests
{
    [Fact]
    public void Manifest_DeclaresWebPageAndRequiredFiles()
    {
        OfficialPluginWebPageAssertions.AssertManifestDeclaresWebPage("CustomMouse");

        var html = OfficialPluginWebPageAssertions.ReadWebPageHtml("CustomMouse");
        Assert.Contains("plugin.customMouse.getState", html, StringComparison.Ordinal);
        Assert.Contains("plugin.customMouse.applyWindows", html, StringComparison.Ordinal);
        Assert.Contains("plugin.customMouse.setCursorThemeMode", html, StringComparison.Ordinal);
        Assert.Contains("plugin.customMouse.applyCursorThemeNow", html, StringComparison.Ordinal);
        Assert.Contains("plugin.customMouse.syncFromWindows", html, StringComparison.Ordinal);
        Assert.Contains("plugin.customMouse.restoreWindowsDefault", html, StringComparison.Ordinal);
        Assert.DoesNotContain("DPI", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("polling", html, StringComparison.OrdinalIgnoreCase);
    }
}
