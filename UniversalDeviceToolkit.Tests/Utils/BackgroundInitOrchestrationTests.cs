using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

/// <summary>
/// Behavioral tests for startup orchestration. Source-string greps are intentionally
/// minimal — prefer runtime assertions for non-overlap and safe-start skips.
/// </summary>
[Collection(TestCollections.ProcessState)]
[Trait("Category", TestCategories.Unit)]
public sealed class BackgroundInitOrchestrationTests
{
    [Fact]
    public void HardwareInitMarker_WriteReadClear_IsIdempotent()
    {
        var dir = Path.Combine(Path.GetTempPath(), "udt-hw-init-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var previous = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, dir);

            StartupHealthGuard.IsHardwareInitInProgressMarkerPresent().Should().BeFalse();
            StartupHealthGuard.MarkHardwareInitInProgress();
            StartupHealthGuard.IsHardwareInitInProgressMarkerPresent().Should().BeTrue();
            StartupHealthGuard.ClearHardwareInitInProgress();
            StartupHealthGuard.IsHardwareInitInProgressMarkerPresent().Should().BeFalse();
            StartupHealthGuard.ClearHardwareInitInProgress(); // idempotent
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, previous);
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task StartupInitializationRunner_HardwareSteps_DoNotOverlap()
    {
        var guard = new StartupHealthGuard();
        var runner = new StartupInitializationRunner(guard, safeStart: false);

        var overlapDetected = 0;
        var inFlight = 0;
        var observedOrder = new List<string>();
        var gate = new object();

        for (var i = 0; i < 5; i++)
        {
            var name = $"hw-{i}";
            runner.RegisterStep(name, TimeSpan.FromSeconds(5), async () =>
            {
                lock (gate)
                {
                    inFlight++;
                    if (inFlight > 1)
                        Interlocked.Increment(ref overlapDetected);
                    observedOrder.Add($"start:{name}");
                }

                await Task.Delay(20).ConfigureAwait(false);

                lock (gate)
                {
                    observedOrder.Add($"end:{name}");
                    inFlight--;
                }
            }, isCritical: false);
        }

        var result = await runner.RunAsync();

        result.Success.Should().BeTrue();
        overlapDetected.Should().Be(0, "hardware steps must never run concurrently");
        observedOrder.Should().Equal(
            "start:hw-0", "end:hw-0",
            "start:hw-1", "end:hw-1",
            "start:hw-2", "end:hw-2",
            "start:hw-3", "end:hw-3",
            "start:hw-4", "end:hw-4");
    }

    [Fact]
    public async Task StartupInitializationRunner_SafeStart_SkipsNonCriticalEnsureLikeSteps()
    {
        var guard = new StartupHealthGuard();
        var runner = new StartupInitializationRunner(guard, safeStart: true);

        var ensureGodModeCalled = false;
        var ensureOverclockCalled = false;
        var readOnlyProbeCalled = false;

        runner.RegisterStep("log-status", TimeSpan.FromSeconds(1),
            () => readOnlyProbeCalled = true,
            isCritical: true);

        runner.RegisterStep("EnsureGodMode", TimeSpan.FromSeconds(1),
            () => ensureGodModeCalled = true,
            isCritical: false);
        runner.RegisterStep("EnsureOverclock", TimeSpan.FromSeconds(1),
            () => ensureOverclockCalled = true,
            isCritical: false);

        var result = await runner.RunAsync();

        result.Success.Should().BeTrue();
        result.EnteredSafeMode.Should().BeTrue();
        readOnlyProbeCalled.Should().BeTrue();
        ensureGodModeCalled.Should().BeFalse();
        ensureOverclockCalled.Should().BeFalse();
        result.SkippedSteps.Should().Contain("EnsureGodMode");
        result.SkippedSteps.Should().Contain("EnsureOverclock");
    }

    [Fact]
    public void HardwareStateRecovery_ReportIncludesGodModeToBalanceStep()
    {
        var console = new StringWriter();
        var impl = new HardwareStateRecoveryImplementation(_ => null, s => console.Write(s));
        var service = new HardwareStateRecoveryService(impl);

        var ok = service.TryResetHardware(out var report, restoreProcessorMinState: false);

        ok.Should().BeTrue();
        report.Should().Contain("god-mode-to-balance");
        report.Should().Contain("processor-min-state");
        report.Should().Contain("skipped (optional");
    }

    [Fact]
    public void AppSource_UsesRunnerAndLimitedServiceConcurrency()
    {
        // Single lightweight wiring guard (not a laundry list of string greps).
        var root = FindRoot();
        var appCs = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "App.xaml.cs"));
        appCs.Should().Contain("StartupInitializationRunner");
        appCs.Should().Contain("maxServiceConcurrency = 2");
        appCs.Should().Contain("MarkHardwareInitInProgress");
        appCs.Should().Contain("ClearHardwareInitInProgress");
    }

    [Fact]
    public void DeviceSetupWindow_DoesNotReuseRestartLaterLabel()
    {
        var root = FindRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "Utils", "DeviceSetupWindow.xaml"));
        var cs = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "Utils", "DeviceSetupWindow.xaml.cs"));

        // Regression: skip button previously used Resource.RestartLater ("I will restart later").
        xaml.Should().NotContain("Resource.RestartLater");
        cs.Should().Contain("DeviceSetupWindow_SkipButton");
        cs.Should().Contain("Skip for now");
        cs.Should().Contain("DeviceSetupWindow_ConfirmButton");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
