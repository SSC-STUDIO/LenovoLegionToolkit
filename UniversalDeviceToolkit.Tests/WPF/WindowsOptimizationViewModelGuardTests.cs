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
        var initializeCore = ExtractMethod(source, "private void InitializeCore()");

        initialize.Should().Contain("RunOnDispatcher(InitializeCore);");
        initialize.Should().NotContain("_ = ScanOptimizationStatesAsync();");
        initializeCore.Should().Contain("StartOptimizationStateScan();");

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

        // The scan must acquire the semaphore to prevent concurrent runs.
        source.Should().Contain("private readonly SemaphoreSlim _optimizationStateScanLock = new(1, 1);");
        scanMethod.Should().Contain("await _optimizationStateScanLock.WaitAsync().ConfigureAwait(false);");
        scanMethod.Should().Contain("_optimizationStateScanLock.Release();");

        // GH #28: the scan must NOT overwrite user preferences with system state.
        // It should no longer call GetOptimizationActionSnapshotAsync, iterate
        // over actions to set IsSelected, or call SaveOptimizationSelection.
        scanMethod.Should().NotContain("GetOptimizationActionSnapshotAsync");
        scanMethod.Should().NotContain("SaveOptimizationSelection");
        scanMethod.Should().NotContain("action.IsSelected = isApplied");

        // It should still refresh the summary panel.
        scanMethod.Should().Contain("UpdateSelectedActions()");
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
