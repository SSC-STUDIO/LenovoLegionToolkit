using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class AndroidSupportGuardTests
{
    private static readonly Regex AndroidProjectReference = new(
        "android|androidx|xamarin|maui|monoandroid",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public void BuildGraph_ShouldNotDeclareAndroidTargetsOrReferences()
    {
        var root = RepositoryPaths.FindRoot();
        var buildFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(IsBuildDefinition)
            .ToArray();

        buildFiles.Should().NotBeEmpty();

        var matches = buildFiles
            .Select(path => new
            {
                RelativePath = Path.GetRelativePath(root, path),
                Content = File.ReadAllText(path),
            })
            .Where(item => AndroidProjectReference.IsMatch(item.Content))
            .Select(item => item.RelativePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        matches.Should().BeEmpty(
            "Android, AndroidX, Xamarin and MAUI targets are intentionally not supported");
    }

    [Fact]
    public void Avalonia_ShouldExposeOnlyDesktopRuntimeIdentifiers()
    {
        var root = RepositoryPaths.FindRoot();
        var projectPath = Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "UniversalDeviceToolkit.Avalonia.csproj");
        var project = File.ReadAllText(projectPath);

        project.Should().Contain("<RuntimeIdentifiers>win-x64;linux-x64;osx-x64;osx-arm64</RuntimeIdentifiers>");
        project.IndexOf("android", StringComparison.OrdinalIgnoreCase).Should().Be(-1);
    }

    [Fact]
    public void PlatformDocumentation_ShouldDeclareAndroidUnsupported()
    {
        var architecture = RepositoryPaths.ReadFile("Docs", "ARCHITECTURE.md");

        architecture.Should().Contain(
            "Mobile and Android companion apps are out of scope and are not supported.");
        architecture.Should().NotContain("Mobile companion app (future consideration)");
    }

    private static bool IsBuildDefinition(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (parts.Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                              || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
                              || part.Equals("artifacts", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".props", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".targets", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".yml", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase);
    }
}
