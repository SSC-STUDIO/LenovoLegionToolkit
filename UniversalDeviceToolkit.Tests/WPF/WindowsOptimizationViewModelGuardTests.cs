using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class WindowsOptimizationViewModelGuardTests
{
    [Fact]
    public void Initialize_ShouldObserveStartupOptimizationScan()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "ViewModels", "WindowsOptimizationViewModel.cs");
        var initialize = ExtractMethod(source, "public void Initialize()");

        initialize.Should().Contain("StartOptimizationStateScan();");
        initialize.Should().NotContain("_ = ScanOptimizationStatesAsync();");

        var observer = ExtractMethod(source, "private async Task ObserveOptimizationStateScanAsync()");
        observer.Should().Contain("await ScanOptimizationStatesAsync().ConfigureAwait(false);");
        observer.Should().Contain("catch (Exception ex)");
        observer.Should().Contain("Log.Instance.Trace(\"Failed to scan Windows optimization states.\", ex);");
    }

    [Fact]
    public void ScanOptimizationStatesAsync_ShouldUseUiThreadActionSnapshot()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "ViewModels", "WindowsOptimizationViewModel.cs");
        var scanMethod = ExtractMethod(source, "public async Task ScanOptimizationStatesAsync()");
        var snapshotMethod = ExtractMethod(source, "private async Task<List<OptimizationActionViewModel>> GetOptimizationActionSnapshotAsync()");
        var snapshotBuilder = ExtractMethod(source, "private List<OptimizationActionViewModel> SnapshotOptimizationActions()");

        scanMethod.Should().Contain("var actions = await GetOptimizationActionSnapshotAsync().ConfigureAwait(false);");
        scanMethod.Should().Contain("foreach (var action in actions)");
        scanMethod.Should().NotContain("foreach (var category in OptimizationCategories)");

        snapshotMethod.Should().Contain("dispatcher.InvokeAsync(SnapshotOptimizationActions)");
        snapshotBuilder.Should().Contain("OptimizationCategories");
        snapshotBuilder.Should().Contain(".SelectMany(category => category.Actions)");
        snapshotBuilder.Should().Contain(".ToList();");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var braceStart = source.IndexOf('{', start);
        braceStart.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[start..(i + 1)];
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var expectedRelativePath = Path.Combine(pathParts);
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, expectedRelativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException($"Could not locate repository file '{expectedRelativePath}'.");
    }

    private static IEnumerable<string> GetRepositoryRootCandidates()
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
