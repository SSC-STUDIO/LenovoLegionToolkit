using System;
using System.Linq;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

internal static class NetworkAccelerationPresentation
{
    public static NetworkAccelerationModePresentation GetModePresentation(NetworkAccelerationMode mode)
    {
        return mode switch
        {
            NetworkAccelerationMode.Gaming => new NetworkAccelerationModePresentation(
                NetworkAccelerationText.ModeGaming,
                NetworkAccelerationText.ModeGamingDescription,
                NetworkAccelerationText.ModeGamingTargetTitle,
                NetworkAccelerationText.ModeGamingTargetDescription,
                NetworkAccelerationText.ModeGamingRecommendedFor,
                NetworkAccelerationText.ModeGamingFocus),
            NetworkAccelerationMode.Streaming => new NetworkAccelerationModePresentation(
                NetworkAccelerationText.ModeStreaming,
                NetworkAccelerationText.ModeStreamingDescription,
                NetworkAccelerationText.ModeStreamingTargetTitle,
                NetworkAccelerationText.ModeStreamingTargetDescription,
                NetworkAccelerationText.ModeStreamingRecommendedFor,
                NetworkAccelerationText.ModeStreamingFocus),
            _ => new NetworkAccelerationModePresentation(
                NetworkAccelerationText.ModeBalanced,
                NetworkAccelerationText.ModeBalancedDescription,
                NetworkAccelerationText.ModeBalancedTargetTitle,
                NetworkAccelerationText.ModeBalancedTargetDescription,
                NetworkAccelerationText.ModeBalancedRecommendedFor,
                NetworkAccelerationText.ModeBalancedFocus)
        };
    }

    public static string GetToggleLabel(bool enabled)
    {
        return enabled
            ? NetworkAccelerationText.StateEnabled
            : NetworkAccelerationText.StateDisabled;
    }

    public static string GetPlanSummary(NetworkOptimizationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Steps.Count == 0)
        {
            return NetworkAccelerationText.NoPlannedOptimizationSteps;
        }

        return string.Join(
            Environment.NewLine,
            plan.Steps.Select((step, index) =>
                string.Format(
                    NetworkAccelerationText.Culture,
                    NetworkAccelerationText.OptimizationPlanStepFormat,
                    index + 1,
                    GetStepLabel(step),
                    $"{step.ExecutableName} {step.Arguments}".Trim(),
                    step.Required ? NetworkAccelerationText.StepSourceRequired : NetworkAccelerationText.StepSourceModeDriven)));
    }

    private static string GetStepLabel(NetworkOptimizationStep step)
    {
        return step.Key switch
        {
            "FlushDns" => NetworkAccelerationText.StepFlushDns,
            "ResetWinsock" => NetworkAccelerationText.StepResetWinsock,
            "ResetTcpIp" => NetworkAccelerationText.StepResetTcpIp,
            _ => step.Key
        };
    }
}

internal sealed record NetworkAccelerationModePresentation(
    string DisplayName,
    string Description,
    string TargetTitle,
    string TargetDescription,
    string RecommendedFor,
    string Focus);
