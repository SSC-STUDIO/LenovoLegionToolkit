using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class PerformanceEffectVerifierTests
{
    [Fact]
    public void Verify_WhenControlSucceeds_ShouldSampleTelemetryBeforeAndAfter()
    {
        var events = new List<string>();
        var readCount = 0;
        var verifier = new PerformanceEffectVerifier(
            () =>
            {
                events.Add($"read-{readCount}");
                return readCount++ == 0
                    ? CreateTelemetry(
                        new CpuFrequencyReading("cpu0", 1000, "test"),
                        new CpuFrequencyReading("cpu1", 1200, "test"))
                    : CreateTelemetry(
                        new CpuFrequencyReading("cpu0", 2200, "test"),
                        new CpuFrequencyReading("cpu1", 2400, "test"));
            },
            (controlId, value) =>
            {
                events.Add($"set-{controlId}-{value}");
                return new HardwareControlSetResult(true, controlId, value, "changed");
            },
            () => events.Add("wait"));

        var report = verifier.Verify("cpu-governor", "performance");

        events.Should().Equal("read-0", "set-cpu-governor-performance", "wait", "read-1");
        report.Succeeded.Should().BeTrue();
        report.ControlId.Should().Be("cpu-governor");
        report.RequestedValue.Should().Be("performance");
        report.BeforeCpuFrequency.Should().Be(new CpuFrequencySummary(2, 1100, 1000, 1200, "test"));
        report.AfterCpuFrequency.Should().Be(new CpuFrequencySummary(2, 2300, 2200, 2400, "test"));
        report.AverageFrequencyDeltaMHz.Should().Be(1200);
        report.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Verify_WhenControlFails_ShouldStillSampleAfterAndReportNotes()
    {
        var readCount = 0;
        var verifier = new PerformanceEffectVerifier(
            () =>
            {
                readCount++;
                return CreateTelemetry();
            },
            (_, _) => new HardwareControlSetResult(false, "vendor-hardware-controls", "performance", "not writable"),
            () => { });

        var report = verifier.Verify("vendor-hardware-controls", "performance");

        readCount.Should().Be(2);
        report.Succeeded.Should().BeFalse();
        report.ControlResult.Detail.Should().Be("not writable");
        report.BeforeCpuFrequency.Count.Should().Be(0);
        report.AfterCpuFrequency.Count.Should().Be(0);
        report.AverageFrequencyDeltaMHz.Should().BeNull();
        report.Notes.Should().Contain("Control write failed: not writable");
        report.Notes.Should().Contain(note => note.Contains("before", StringComparison.OrdinalIgnoreCase));
        report.Notes.Should().Contain(note => note.Contains("after", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verify_WhenAverageFrequencyIsUnchanged_ShouldReturnEvidenceNote()
    {
        var readCount = 0;
        var verifier = new PerformanceEffectVerifier(
            () =>
            {
                readCount++;
                return CreateTelemetry(new CpuFrequencyReading("cpu0", 1800, "test"));
            },
            (controlId, value) => new HardwareControlSetResult(true, controlId, value, "changed"),
            () => { });

        var report = verifier.Verify("power-profile", "balanced");

        report.Succeeded.Should().BeTrue();
        report.AverageFrequencyDeltaMHz.Should().Be(0);
        report.Notes.Should().Contain(note => note.Contains("unchanged", StringComparison.OrdinalIgnoreCase));
    }

    private static SystemTelemetry CreateTelemetry(params CpuFrequencyReading[] cpuFrequencies) =>
        new(
            "test",
            "Test CPU",
            2,
            16,
            8,
            cpuFrequencies,
            [],
            [],
            []);
}
