using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class SolutionProjectInventoryGuardTests
{
    private static readonly Regex SolutionProject = new(
        "\\\"(?<path>[^\\\"]+\\.csproj)\\\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, string> ExternalProjects =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["UniversalDeviceToolkit.Plugins.Abstractions/UniversalDeviceToolkit.Plugins.Abstractions.csproj"] =
                "Built directly by crossplatform-libs.yml as the independent plugin SDK surface."
        };

    [Fact]
    public void EveryRepositoryProject_ShouldBeInSolutionOrExplicitlyExternal()
    {
        var root = RepositoryPaths.FindRoot();
        var solutionPath = Path.Combine(root, "UniversalDeviceToolkit.sln");
        var solutionProjects = SolutionProject.Matches(File.ReadAllText(solutionPath))
            .Select(match => Normalize(match.Groups["path"].Value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var repositoryProjects = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !IsBuildOutput(path))
            .Select(path => Normalize(Path.GetRelativePath(root, path)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = repositoryProjects
            .Except(solutionProjects, StringComparer.OrdinalIgnoreCase)
            .Except(ExternalProjects.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        missing.Should().BeEmpty(
            "every repository project must be represented by the solution or added to the explicit external SDK allowlist");
    }

    [Fact]
    public void ExternalProjectAllowlist_ShouldDocumentExistingProjects()
    {
        var root = RepositoryPaths.FindRoot();

        foreach (var (relativePath, reason) in ExternalProjects)
        {
            File.Exists(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)))
                .Should().BeTrue($"external project '{relativePath}' must still exist: {reason}");
        }
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('.', '/');

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                         || part.Equals("obj", StringComparison.OrdinalIgnoreCase));
}
