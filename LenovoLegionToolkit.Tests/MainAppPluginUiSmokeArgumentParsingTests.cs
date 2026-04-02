using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class MainAppPluginUiSmokeArgumentParsingTests
{
    [Fact]
    public void ResolveRepositoryRoot_WithNamedArguments_ReturnsExplicitRepoRoot()
    {
        var repoRoot = FindRepositoryRoot();
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveRepositoryRoot = programType.GetMethod("ResolveRepositoryRoot", BindingFlags.NonPublic | BindingFlags.Static);

        resolveRepositoryRoot.Should().NotBeNull();

        var result = (string?)resolveRepositoryRoot!.Invoke(
            null,
            new object[]
            {
                new[]
                {
                    "--repo-root",
                    repoRoot,
                    "--plugin",
                    "shell-integration",
                    "--culture",
                    "zh-Hans"
                }
            });

        result.Should().Be(repoRoot);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "LenovoLegionToolkit.sln");
            if (File.Exists(solutionPath))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for MainAppPluginUi.Smoke argument parsing tests.");
    }
}
