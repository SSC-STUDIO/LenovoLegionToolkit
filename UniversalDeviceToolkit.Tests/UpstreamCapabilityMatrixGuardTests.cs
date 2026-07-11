using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

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
        foreach (var path in new[] { "Assets/Brand/udt-hub.svg", "Assets/Brand/udt-hub-dark.svg", "Assets/Brand/udt-hub-light.svg", "Assets/Brand/tray-dark.png", "Assets/Brand/tray-light.png", "UniversalDeviceToolkit.WPF/Assets/Icon.ico" })
            File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue(path);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}