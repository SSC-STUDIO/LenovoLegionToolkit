internal sealed record PerformanceEffectVerificationReport(
    string ControlId,
    string RequestedValue,
    HardwareControlSetResult ControlResult,
    CpuFrequencySummary BeforeCpuFrequency,
    CpuFrequencySummary AfterCpuFrequency,
    double? AverageFrequencyDeltaMHz,
    string[] Notes)
{
    public bool Succeeded => ControlResult.Succeeded;
}

internal sealed record CpuFrequencySummary(
    int Count,
    double? AverageMHz,
    double? MinMHz,
    double? MaxMHz,
    string Source)
{
    public static CpuFrequencySummary From(IReadOnlyCollection<CpuFrequencyReading> readings)
    {
        if (readings.Count == 0)
            return new CpuFrequencySummary(0, null, null, null, string.Empty);

        var values = readings.Select(reading => reading.MHz).ToArray();
        var source = string.Join(
            ", ",
            readings
                .Select(reading => reading.Source)
                .Where(source => !string.IsNullOrWhiteSpace(source))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(source => source, StringComparer.OrdinalIgnoreCase));

        return new CpuFrequencySummary(
            values.Length,
            Math.Round(values.Average(), 1),
            Math.Round(values.Min(), 1),
            Math.Round(values.Max(), 1),
            source);
    }
}

internal sealed class PerformanceEffectVerifier(
    Func<SystemTelemetry> readTelemetry,
    Func<string, string, HardwareControlSetResult> setControl,
    Action waitBetweenSamples)
{
    public PerformanceEffectVerificationReport Verify(string controlId, string value)
    {
        var beforeTelemetry = readTelemetry();
        var result = setControl(controlId, value);
        waitBetweenSamples();
        var afterTelemetry = readTelemetry();

        var before = CpuFrequencySummary.From(beforeTelemetry.CpuFrequencies);
        var after = CpuFrequencySummary.From(afterTelemetry.CpuFrequencies);
        var notes = BuildNotes(result, before, after);

        return new PerformanceEffectVerificationReport(
            result.ControlId,
            value,
            result,
            before,
            after,
            before.AverageMHz is null || after.AverageMHz is null
                ? null
                : Math.Round(after.AverageMHz.Value - before.AverageMHz.Value, 1),
            notes);
    }

    private static string[] BuildNotes(
        HardwareControlSetResult result,
        CpuFrequencySummary before,
        CpuFrequencySummary after)
    {
        var notes = new List<string>();
        if (!result.Succeeded)
            notes.Add($"Control write failed: {result.Detail}");

        if (before.Count == 0)
            notes.Add("No CPU frequency readings were available before the control change.");

        if (after.Count == 0)
            notes.Add("No CPU frequency readings were available after the control change.");

        if (result.Succeeded && before.AverageMHz is not null && after.AverageMHz is not null)
        {
            var delta = Math.Round(after.AverageMHz.Value - before.AverageMHz.Value, 1);
            if (Math.Abs(delta) < 1)
                notes.Add("Average CPU frequency was unchanged in the sampled window; run under a repeatable workload for stronger evidence.");
        }

        return notes.ToArray();
    }
}
