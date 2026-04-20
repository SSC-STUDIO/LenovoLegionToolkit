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
}

internal sealed record NetworkAccelerationModePresentation(
    string DisplayName,
    string Description,
    string TargetTitle,
    string TargetDescription,
    string RecommendedFor,
    string Focus);
