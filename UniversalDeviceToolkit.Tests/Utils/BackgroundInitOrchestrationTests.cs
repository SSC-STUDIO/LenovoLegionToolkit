using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public sealed class BackgroundInitOrchestrationTests
{
    [Fact]
    public void AppSource_BackgroundInit_RunsHardwareStepsSerially()
    {
        var root = FindRoot();
        var appCs = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "App.xaml.cs"));

        // Regression guard: unbounded parallel hardware init caused post-start lag.
        appCs.Should().Contain("foreach (var step in initializationSteps)");
        appCs.Should().Contain("RunWithLimitedConcurrencyAsync");
        appCs.Should().NotContain("initializationTasks = initializationSteps.Select");
        appCs.Should().Contain("MarkHardwareInitInProgress");
        appCs.Should().Contain("ClearHardwareInitInProgress");
    }

    [Fact]
    public void StartupOrchestrator_SafeStart_SkipsHardwareReplaySteps()
    {
        var root = FindRoot();
        var text = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Startup", "StartupOrchestrator.cs"));

        text.Should().Contain("Safe-start: skipping hardware re-apply");
        text.Should().Contain("Extensions disabled in settings; skipping plugin directory scan");
        text.Should().Contain("IsHardwareInitInProgressMarkerPresent");
    }

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

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
