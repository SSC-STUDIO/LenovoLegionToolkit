using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
public sealed class WindowsOptimizationViewModelGuardTests
{
    [Fact]
    public void Initialize_ShouldLeaveStateScanningToThePageCoordinator()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "ViewModels", "WindowsOptimizationViewModel.cs");
        var initialize = ExtractMethod(source, "public void Initialize()");
        var initializeCore = ExtractMethod(source, "private void InitializeCore()");

        initialize.Should().Contain("RunOnDispatcher(InitializeCore);");
        initialize.Should().NotContain("_ = ScanOptimizationStatesAsync();");
        initializeCore.Should().NotContain("StartOptimizationStateScan();");
        initializeCore.Should().NotContain("ObserveOptimizationStateScanAsync");
    }

    [Fact]
    public void ScanOptimizationStatesAsync_ShouldUseUiThreadActionSnapshot()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "ViewModels", "WindowsOptimizationViewModel.cs");
        var scanMethod = ExtractMethod(source, "public async Task ScanOptimizationStatesAsync(");
        var snapshotMethod = ExtractMethod(source, "private async Task<List<OptimizationActionViewModel>> GetOptimizationActionSnapshotAsync()");
        var snapshotBuilder = ExtractMethod(source, "private List<OptimizationActionViewModel> SnapshotOptimizationActions()");

        source.Should().Contain("private readonly SemaphoreSlim _optimizationStateScanLock = new(1, 1);");
        scanMethod.Should().Contain("await _optimizationStateScanLock.WaitAsync(cancellationToken);");
        scanMethod.Should().Contain("var actions = await GetOptimizationActionSnapshotAsync();");
        scanMethod.Should().Contain("foreach (var action in actions.Where");
        scanMethod.Should().NotContain("foreach (var category in OptimizationCategories)");
        scanMethod.Should().Contain("_optimizationStateScanLock.Release();");

        snapshotMethod.Should().Contain("dispatcher.InvokeAsync(SnapshotOptimizationActions)");
        snapshotBuilder.Should().Contain("OptimizationCategories");
        snapshotBuilder.Should().Contain(".ToList()");
        snapshotBuilder.Should().Contain(".SelectMany(category => category.Actions.ToList())");
        snapshotBuilder.Should().Contain(".ToList();");
    }

    [Fact]
    public void ScanOptimizationStatesAsync_ShouldRecordMachineStateAndUnlockApply()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "ViewModels", "WindowsOptimizationViewModel.cs");
        var scanMethod = ExtractMethod(source, "public async Task ScanOptimizationStatesAsync(");

        scanMethod.Should().Contain("TryGetActionAppliedAsync");
        scanMethod.Should().Contain("action.IsApplied = isApplied;");
        scanMethod.Should().Contain("action.IsSelected = isApplied ?? false;");
        scanMethod.Should().Contain("action.IsEnabled = isApplied.HasValue;");
        scanMethod.Should().Contain("IsOptimizationStateScanned = true;");
        scanMethod.Should().NotContain("IsActionAppliedAsync(action.Key");
        source.Should().NotContain("HandleOptimizationActionChangeAsync");
    }

    [Fact]
    public void Apply_ShouldOnlyOperateInOptimizationMode()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "ViewModels", "WindowsOptimizationViewModel.cs");
        var applyMethod = ExtractMethod(source, "public async Task ApplyOptimizationChangesAsync(");

        applyMethod.Should().Contain("!IsOptimizationMode");
        source.Should().NotContain("CanCancelOptimizationChanges");
        source.Should().NotContain("CancelOptimizationChanges");
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
