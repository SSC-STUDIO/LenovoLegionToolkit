using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class AssemblyNameCollisionGuardTests
{
    [Fact]
    public void CrossPlatformCliAndAvalonia_ShouldHaveDistinctAssemblyNames()
    {
        var root = RepositoryPaths.FindRoot();
        var crossPlatform = File.ReadAllText(Path.Combine(
            root, "UniversalDeviceToolkit.CrossPlatform", "UniversalDeviceToolkit.CrossPlatform.csproj"));
        var avalonia = File.ReadAllText(Path.Combine(
            root, "UniversalDeviceToolkit.Avalonia", "UniversalDeviceToolkit.Avalonia.csproj"));

        crossPlatform.Should().Contain("<AssemblyName>udt</AssemblyName>");
        avalonia.Should().Contain("<AssemblyName>udt-gui</AssemblyName>");
        avalonia.Should().NotContain("<AssemblyName>udt</AssemblyName>");
    }
}
