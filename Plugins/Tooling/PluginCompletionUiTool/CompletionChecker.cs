using System.Runtime.Versioning;
using PluginTooling.Core;

namespace PluginCompletionUiTool;

[SupportedOSPlatform("windows")]
internal sealed class CompletionChecker
{
    private readonly Action<string> _log;
    private readonly Action<StepReportItem>? _stepSink;

    public CompletionChecker(Action<string> log, Action<StepReportItem>? stepSink = null)
    {
        _log = log;
        _stepSink = stepSink;
    }

    public async Task<CompletionReport> RunAsync(CompletionCheckRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var service = new PluginValidationService(_log, step =>
        {
            _stepSink?.Invoke(new StepReportItem
            {
                Timestamp = step.Timestamp,
                PluginId = step.PluginId,
                Status = step.Status,
                Message = step.Message,
            });
        });
        var report = await service.RunAsync(new ValidationRequest
        {
            RepositoryRoot = request.RepositoryRoot,
            Configuration = request.Configuration,
            SkipBuild = request.SkipBuild,
            SkipTests = request.SkipTests,
            Profile = PluginValidationProfile.OfficialCandidate,
            PluginIds = request.PluginIds,
        }, cancellationToken);

        return new CompletionReport
        {
            Totals = new CompletionTotals
            {
                PluginCount = report.Totals.PluginCount,
                Failures = report.Totals.Failures,
                Warnings = report.Totals.Warnings,
            },
            Plugins = report.Plugins.Select(static plugin => new PluginReportItem
            {
                PluginId = plugin.PluginId,
                Status = plugin.Status,
                Failures = plugin.Failures,
                Warnings = plugin.Warnings,
            }).ToList(),
            Steps = report.Steps.Select(static step => new StepReportItem
            {
                Timestamp = step.Timestamp,
                PluginId = step.PluginId,
                Status = step.Status,
                Message = step.Message,
            }).ToList(),
        };
    }
}
