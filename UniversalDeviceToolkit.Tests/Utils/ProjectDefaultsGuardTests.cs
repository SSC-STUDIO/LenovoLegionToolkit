using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class ProjectDefaultsGuardTests
{
    [Fact]
    public void DirectoryBuildProps_ShouldOwnNullableAndImplicitUsingsDefaults()
    {
        var root = RepositoryPaths.FindRoot();
        var document = XDocument.Load(Path.Combine(root, "Directory.Build.props"));
        var properties = document.Descendants("PropertyGroup")
            .Elements()
            .Where(element => element.Name.LocalName is "Nullable" or "ImplicitUsings")
            .GroupBy(element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);

        properties["Nullable"].Attribute("Condition")?.Value
            .Should().Be("'$(Nullable)' == ''");
        properties["Nullable"].Value.Should().Be("enable");
        properties["ImplicitUsings"].Attribute("Condition")?.Value
            .Should().Be("'$(ImplicitUsings)' == ''");
        properties["ImplicitUsings"].Value.Should().Be("enable");
    }

    [Fact]
    public void Projects_ShouldNotRepeatCentralizedDefaults()
    {
        var root = RepositoryPaths.FindRoot();
        var projectFiles = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                             || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
                             || part.Equals("artifacts", StringComparison.OrdinalIgnoreCase)))
            .Where(path => !Path.GetFileName(path).Contains("_wpftmp", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        projectFiles.Should().NotBeEmpty();

        foreach (var projectFile in projectFiles)
        {
            var document = XDocument.Load(projectFile);
            var properties = document.Descendants("PropertyGroup").Elements().ToArray();

            properties.Should().NotContain(property =>
                (property.Name.LocalName == "Nullable" || property.Name.LocalName == "ImplicitUsings")
                && string.Equals(property.Value.Trim(), "enable", StringComparison.OrdinalIgnoreCase),
                $"{Path.GetRelativePath(root, projectFile)} should inherit repository defaults");
        }

        var explicitImplicitUsingsDisabledProjects = new[]
        {
            "UniversalDeviceToolkit.CLI.Lib/UniversalDeviceToolkit.CLI.Lib.csproj",
            "UniversalDeviceToolkit.Lib/UniversalDeviceToolkit.Lib.csproj",
            "UniversalDeviceToolkit.Lib.Macro/UniversalDeviceToolkit.Lib.Macro.csproj",
            "UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj",
        };

        foreach (var relativeProjectPath in explicitImplicitUsingsDisabledProjects)
        {
            var project = XDocument.Load(Path.Combine(root, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar)));
            project.Descendants("PropertyGroup")
                .Elements("ImplicitUsings")
                .Single()
                .Value
                .Should().Be("disable", relativeProjectPath);
        }
    }
}
