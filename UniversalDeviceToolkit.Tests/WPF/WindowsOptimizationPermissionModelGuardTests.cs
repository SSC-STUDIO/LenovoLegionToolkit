using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Security)]
[Trait("Category", TestCategories.Guard)]
public sealed class WindowsOptimizationPermissionModelGuardTests
{
    [Fact]
    public void WpfApplication_ShouldRunUnelevatedByDefault()
    {
        var manifest = XDocument.Load(ReadRepositoryPath("UniversalDeviceToolkit.WPF", "App.manifest"));
        var requestedExecutionLevel = manifest
            .Descendants(XNamespace.Get("urn:schemas-microsoft-com:asm.v3") + "requestedExecutionLevel")
            .Single();

        requestedExecutionLevel.Attribute("level")?.Value.Should().Be("asInvoker");
        requestedExecutionLevel.Attribute("uiAccess")?.Value.Should().Be("false");
    }

    [Fact]
    public void Autorun_ShouldUseLimitedUserRunLevel()
    {
        var source = File.ReadAllText(ReadRepositoryPath("UniversalDeviceToolkit.Lib", "System", "Autorun.cs"));

        source.Should().Contain("td.Principal.RunLevel = TaskRunLevel.LUA;");
        source.Should().Contain("currentTask.Definition.Principal.RunLevel == TaskRunLevel.LUA");
        source.Should().NotContain("TaskRunLevel.Highest");
    }

    [Fact]
    public void OptimizationMutations_ShouldBeIsolatedInAnElevatedWorker()
    {
        var elevationSource = File.ReadAllText(ReadRepositoryPath(
            "UniversalDeviceToolkit.WPF", "Utils", "WindowsOptimizationElevation.cs"));
        var pageSource = File.ReadAllText(ReadRepositoryPath(
            "UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml.cs"));

        elevationSource.Should().Contain("Verb = \"runas\"");
        elevationSource.Should().Contain("IsCurrentProcessElevated()");
        elevationSource.Should().Contain("request.Token");
        elevationSource.Should().Contain("hasOperations == hasCleanupOperations");
        elevationSource.Should().Contain("security.SetAccessRuleProtection(isProtected: true");
        elevationSource.Should().Contain("PipeAccessRule");

        pageSource.Should().Contain("ViewModel.ApplyOptimizationChangesAsync()");
        pageSource.Should().NotContain("ApplyActionAsync(");
        pageSource.Should().NotContain("RevertActionAsync(");
        pageSource.Should().NotContain("ExecuteActionsAsync(");
    }

    private static string ReadRepositoryPath(params string[] pathParts)
    {
        var relativePath = Path.Combine(pathParts);
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, relativePath);
            if (File.Exists(path))
                return path;
        }

        throw new DirectoryNotFoundException($"Could not locate repository file '{relativePath}'.");
    }

    private static System.Collections.Generic.IEnumerable<string> GetRepositoryRootCandidates()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }
}
