using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class PluginCatalogContractTests
{
    [Fact]
    public async Task UnavailableHost_ReportsCatalogUnavailableWithoutEntries()
    {
        var service = new UnavailablePlatformServices();

        var state = await service.GetPluginCatalogAsync();

        state.IsAvailable.Should().BeFalse();
        state.Plugins.Should().BeEmpty();
        (await service.InstallPluginAsync("missing-plugin")).Should().BeFalse();
        (await service.UpdatePluginAsync("missing-plugin")).Should().BeFalse();
    }

    [Fact]
    public async Task InstallCoordinator_SerializesInstalls_OneAtATime()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        var active = 0;
        var maxConcurrent = 0;
        var firstStarted = new TaskCompletionSource();
        var gate = new TaskCompletionSource();

        var first = coordinator.InstallAsync(
            ["plugin-a"],
            async id =>
            {
                var current = Interlocked.Increment(ref active);
                InterlockedExchangeMax(ref maxConcurrent, current);
                firstStarted.TrySetResult();
                await gate.Task;
                Interlocked.Decrement(ref active);
            });

        await firstStarted.Task;
        var second = coordinator.InstallAsync(
            ["plugin-b"],
            async id =>
            {
                var current = Interlocked.Increment(ref active);
                InterlockedExchangeMax(ref maxConcurrent, current);
                await Task.Yield();
                Interlocked.Decrement(ref active);
            });

        maxConcurrent.Should().Be(1);
        coordinator.IsQueuedOrActive("plugin-b").Should().BeTrue();
        gate.TrySetResult();
        await Task.WhenAll(first, second);
        maxConcurrent.Should().Be(1);
        coordinator.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task InstallCoordinator_DeduplicatesPluginIds()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        var calls = new List<string>();
        var started = new TaskCompletionSource();
        var gate = new TaskCompletionSource();

        var first = coordinator.InstallAsync(
            ["plugin-x"],
            async id =>
            {
                calls.Add(id);
                started.TrySetResult();
                await gate.Task;
            });

        await started.Task;
        var second = coordinator.InstallAsync(
            ["plugin-x", "plugin-y"],
            id =>
            {
                calls.Add(id);
                return Task.CompletedTask;
            });

        gate.TrySetResult();
        await Task.WhenAll(first, second);
        calls.Should().Equal("plugin-x", "plugin-y");
    }

    [Fact]
    public async Task InstallCoordinator_UninstallAsync_UsesProvidedInstaller()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        var uninstalled = new List<string>();

        await coordinator.UninstallAsync(
            ["plugin-a", "plugin-b"],
            id =>
            {
                uninstalled.Add(id);
                return Task.CompletedTask;
            });

        uninstalled.Should().Equal("plugin-a", "plugin-b");
        coordinator.IsActive.Should().BeFalse();
        coordinator.CurrentPluginId.Should().BeNull();
    }

    [Fact]
    public async Task InstallCoordinator_ReportsQueuedOrActiveAndProgress()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        var started = new TaskCompletionSource();
        var gate = new TaskCompletionSource();

        var task = coordinator.InstallAsync(
            ["plugin-a"],
            async id =>
            {
                started.TrySetResult();
                await gate.Task;
            });

        await started.Task;
        coordinator.IsActive.Should().BeTrue();
        coordinator.CurrentPluginId.Should().Be("plugin-a");
        coordinator.IsQueuedOrActive("plugin-a").Should().BeTrue();
        coordinator.Progress.Should().BeNull();
        coordinator.StatusText.Should().Contain("plugin-a");

        var queued = coordinator.InstallAsync(["plugin-b"], _ => Task.CompletedTask);
        coordinator.IsQueuedOrActive("plugin-b").Should().BeTrue();

        gate.TrySetResult();
        await Task.WhenAll(task, queued);
        coordinator.IsActive.Should().BeFalse();
        coordinator.IsQueuedOrActive("plugin-b").Should().BeFalse();
        coordinator.CurrentPluginId.Should().BeNull();
    }

    [Fact]
    public void InstallCoordinator_EmptyRequestsCompleteImmediately()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        IEnumerable<string>? nullIds = null;

        coordinator.InstallAsync([], _ => Task.CompletedTask).IsCompleted.Should().BeTrue();
        coordinator.InstallAsync(nullIds!, _ => Task.CompletedTask).IsCompleted.Should().BeTrue();
        coordinator.IsQueuedOrActive("anything").Should().BeFalse();
    }

    [Fact]
    public async Task InstallCoordinator_RaisesChangedOnStateTransitions()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        long changes = 0;
        coordinator.Changed += () => Interlocked.Increment(ref changes);

        await coordinator.InstallAsync(["p1"], _ => Task.CompletedTask);

        Interlocked.Read(ref changes).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task InstallCoordinator_ReportsFailedOperationAndAllowsRetry()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        Func<string, Task<bool>> fail = _ => Task.FromResult(false);
        Func<string, Task<bool>> succeed = _ => Task.FromResult(true);

        var failed = await coordinator.InstallAsync(["plugin-a"], fail);

        failed.Succeeded.Should().BeFalse();
        failed.HasFailures.Should().BeTrue();
        failed.Operations.Should().ContainSingle();
        failed.Operations[0].Status.Should().Be(PluginOperationStatus.Failed);
        coordinator.IsQueuedOrActive("plugin-a").Should().BeFalse();

        var retried = await coordinator.InstallAsync(["plugin-a"], succeed);

        retried.Succeeded.Should().BeTrue();
        retried.Operations[0].Status.Should().Be(PluginOperationStatus.Succeeded);
    }

    [Fact]
    public async Task InstallCoordinator_ReportsCanceledOperationWithoutRunningInstaller()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;

        var result = await coordinator.InstallAsync(
            ["plugin-a"],
            (id, _) =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(true);
            },
            cancellation.Token);

        result.Succeeded.Should().BeFalse();
        result.HasCanceled.Should().BeTrue();
        result.Operations[0].Status.Should().Be(PluginOperationStatus.Canceled);
        calls.Should().Be(0);
        coordinator.IsQueuedOrActive("plugin-a").Should().BeFalse();
    }

    [Fact]
    public async Task InstallCoordinator_DuplicateRequestAwaitsTheOriginalResult()
    {
        var coordinator = new AvaloniaPluginInstallCoordinator();
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        Func<string, Task<bool>> installer = async _ =>
        {
            started.TrySetResult();
            await release.Task;
            return false;
        };

        var first = coordinator.UpdateAsync(["plugin-a"], installer);
        await started.Task;
        var duplicate = coordinator.UpdateAsync(["PLUGIN-A"], installer);

        duplicate.IsCompleted.Should().BeFalse();
        release.TrySetResult();
        var results = await Task.WhenAll(first, duplicate);

        results.Should().OnlyContain(result => result.HasFailures);
        results.SelectMany(result => result.Operations)
            .Should().OnlyContain(operation => operation.Status == PluginOperationStatus.Failed);
    }

    [Fact]
    public void PluginStorePage_RefreshesNavigationOnlyForSuccessfulLifecycleOperations()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "FeaturePageView.axaml.cs"));

        source.Should().Contain("Func<string, Task<bool>> installer");
        source.Should().Contain("PluginOperationBatchResult result");
        source.Should().Contain("ShowPluginOperationFailure(result)");
        source.Should().Contain(".Where(operation => operation.Succeeded)");
        source.Should().Contain("_pluginCatalogChanged?.Invoke()");
    }

    [Fact]
    public void ExecutableResolver_PickExecutable_PrefersConventionalFolderName()
    {
        var picked = AvaloniaPluginExecutableResolver.PickExecutable(
            [
                @"C:\plugins\Other.exe",
                @"C:\plugins\CoolPlugin.exe",
                @"C:\plugins\UniversalDeviceToolkit.Plugins.CoolPlugin.exe",
            ],
            "CoolPlugin");

        picked.Should().Be(@"C:\plugins\CoolPlugin.exe");
    }

    [Fact]
    public void ExecutableResolver_PickExecutable_PrefersConventionalPrefixedVariant()
    {
        var picked = AvaloniaPluginExecutableResolver.PickExecutable(
            [
                @"C:\plugins\A.exe",
                @"C:\plugins\UniversalDeviceToolkit.Plugins.CoolPlugin.exe",
            ],
            "Cool-Plugin");

        picked.Should().Be(@"C:\plugins\UniversalDeviceToolkit.Plugins.CoolPlugin.exe");
    }

    [Fact]
    public void ExecutableResolver_PickExecutable_FallsBackToFirstMatch()
    {
        IEnumerable<string>? nullFiles = null;

        AvaloniaPluginExecutableResolver.PickExecutable(
            [@"C:\plugins\Random.exe"],
            "Cool-Plugin").Should().Be(@"C:\plugins\Random.exe");
        AvaloniaPluginExecutableResolver.PickExecutable([], "Cool-Plugin").Should().BeNull();
        AvaloniaPluginExecutableResolver.PickExecutable(nullFiles!, "Cool-Plugin").Should().BeNull();
    }

    [Fact]
    public void ExecutableResolver_ResolveExecutablePath_FindsConventionalExe()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MyPlugin");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "UniversalDeviceToolkit.Plugins.MyPlugin.exe"), string.Empty);
            File.WriteAllText(Path.Combine(directory, "Other.exe"), string.Empty);

            AvaloniaPluginExecutableResolver
                .ResolveExecutablePath(directory)
                .Should()
                .Be(Path.Combine(directory, "UniversalDeviceToolkit.Plugins.MyPlugin.exe"));
            AvaloniaPluginExecutableResolver.ResolveExecutablePath(directory + "\\missing").Should().BeNull();
            AvaloniaPluginExecutableResolver.ResolveExecutablePath(null).Should().BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExecutableResolver_ResolveExecutablePath_FallsBackToAnyExe()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"udt-resolver-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var helper = Path.Combine(directory, "Helper.exe");
            var plugin = Path.Combine(directory, "PlugIn.exe");
            File.WriteAllText(helper, string.Empty);
            File.WriteAllText(plugin, string.Empty);

            var resolved = AvaloniaPluginExecutableResolver.ResolveExecutablePath(directory);
            resolved.Should().NotBeNull();
            new[] { helper, plugin }.Should().Contain(resolved);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExecutableResolver_IsSignatureValid_GuardsInvalidPaths()
    {
        AvaloniaPluginExecutableResolver.IsSignatureValid(string.Empty).Should().BeFalse();
        AvaloniaPluginExecutableResolver.IsSignatureValid(@"C:\missing\plugin.exe").Should().BeFalse();

        var path = Path.Combine(Path.GetTempPath(), $"udt-signature-{Guid.NewGuid():N}.exe");
        try
        {
            File.WriteAllText(path, string.Empty);
            AvaloniaPluginExecutableResolver.IsSignatureValid(path).Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void InterlockedExchangeMax(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (current < value)
        {
            var observed = Interlocked.CompareExchange(ref target, value, current);
            if (observed == current)
                return;
            current = observed;
        }
    }
}
