using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.DeviceSupport;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class UpstreamCapabilityMatrixGuardTests
{
    [Fact]
    public void CapabilityMatrix_ShouldTrackHighValueUpstreamAreas()
    {
        var root = FindRoot();
        var text = File.ReadAllText(Path.Combine(root, "Docs", "UpstreamCapabilityMatrix.md"));
        foreach (var capability in new[] { "OR composite automation trigger", "Hardware sensor automation conditions", "Settings export/import", "24-zone" })
            text.Should().Contain(capability);
    }

    [Fact]
    public void BrandAssets_ShouldContainRequiredVariants()
    {
        var root = FindRoot();
        foreach (var path in new[]
                 {
                     "Assets/Brand/udt-symbol.svg",
                     "Assets/Brand/udt-symbol-dark.svg",
                     "Assets/Brand/udt-symbol-light.svg",
                     "Assets/Brand/tray-dark.png",
                     "Assets/Brand/tray-light.png",
                     "Assets/Logo.png",
                     "Assets/Icon.ico",
                     "Assets/Logo.png",
                     "Assets/Screenshot_main.png"
                 })
            File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue(path);
    }

    [Fact]
    public void BrandAssets_InstallerAndSite_ReferenceCanonicalIcons()
    {
        var root = FindRoot();
        var installerProject = File.ReadAllText(Path.Combine(root, "Tools", "Installer", "UniversalDeviceToolkit.Installer.csproj"));
        installerProject.Should().Contain(@"ApplicationIcon>..\..\Assets\Icon.ico");

        var engine = File.ReadAllText(Path.Combine(root, "Tools", "Installer", "InstallerEngine.cs"));
        engine.Should().Contain("DisplayIcon");
        engine.Should().Contain("InstallerConstants.MainExeName");

        var site = File.ReadAllText(Path.Combine(root, "site", "index.html"));
        site.Should().Contain("Screenshot_main.png");

        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        readme.Should().Contain("Assets/Logo.png");
    }

    [Fact]
    public void BrandAssets_ShouldNotRetainAlternateConcepts()
    {
        var root = FindRoot();
        var brandDirectory = Path.Combine(root, "Assets", "Brand");
        Directory.GetDirectories(brandDirectory).Should().BeEmpty();
        Directory.GetFiles(brandDirectory, "*.svg")
            .Select(Path.GetFileName)
            .Should().BeEquivalentTo("udt-symbol.svg", "udt-symbol-dark.svg", "udt-symbol-light.svg");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
