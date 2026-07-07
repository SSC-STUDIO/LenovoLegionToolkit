using System.Collections.Generic;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class PackageDownloaderSettingsStoreTests
{
    [Fact]
    public void Defaults_DownloadPath_ShouldBeNull()
    {
        var store = new PackageDownloaderSettings.PackageDownloaderSettingsStore();
        store.DownloadPath.Should().BeNull();
    }

    [Fact]
    public void Defaults_OnlyShowUpdates_ShouldBeFalse()
    {
        var store = new PackageDownloaderSettings.PackageDownloaderSettingsStore();
        store.OnlyShowUpdates.Should().BeFalse();
    }

    [Fact]
    public void Defaults_HiddenPackages_ShouldBeEmpty()
    {
        var store = new PackageDownloaderSettings.PackageDownloaderSettingsStore();
        store.HiddenPackages.Should().NotBeNull();
        store.HiddenPackages.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var store = new PackageDownloaderSettings.PackageDownloaderSettingsStore();
        store.DownloadPath = @"C:\Downloads";
        store.DownloadPath.Should().Be(@"C:\Downloads");
        store.OnlyShowUpdates = true;
        store.OnlyShowUpdates.Should().BeTrue();
        store.HiddenPackages = new HashSet<string> { "PKG001", "PKG002" };
        store.HiddenPackages.Should().HaveCount(2);
    }
}

[Trait("Category", TestCategories.Unit)]
public class SpectrumKeyboardSettingsStoreTests
{
    [Fact]
    public void Defaults_KeyboardLayout_ShouldBeNull()
    {
        var store = new SpectrumKeyboardSettings.SpectrumKeyboardSettingsStore();
        store.KeyboardLayout.Should().BeNull();
    }

    [Fact]
    public void Property_ShouldBeSettable()
    {
        var store = new SpectrumKeyboardSettings.SpectrumKeyboardSettingsStore();
        store.KeyboardLayout = KeyboardLayout.Ansi;
        store.KeyboardLayout.Should().Be(KeyboardLayout.Ansi);
    }
}