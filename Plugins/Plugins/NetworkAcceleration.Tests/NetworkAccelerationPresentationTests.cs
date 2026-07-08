using LenovoLegionToolkit.Plugins.NetworkAcceleration;
using LenovoLegionToolkit.Plugins.TestCommon;
using System;
using Xunit;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Tests;

[Collection("NetworkAccelerationResourceCulture")]
public class NetworkAccelerationPresentationTests
{
    #region GetModePresentation

    [Fact]
    public void GetModePresentation_Gaming_ReturnsGamingPresentation()
    {
        var presentation = NetworkAccelerationPresentation.GetModePresentation(NetworkAccelerationMode.Gaming);

        Assert.Equal(NetworkAccelerationText.ModeGaming, presentation.DisplayName);
        Assert.Equal(NetworkAccelerationText.ModeGamingDescription, presentation.Description);
        Assert.Equal(NetworkAccelerationText.ModeGamingFocus, presentation.Focus);
    }

    [Fact]
    public void GetModePresentation_Streaming_ReturnsStreamingPresentation()
    {
        var presentation = NetworkAccelerationPresentation.GetModePresentation(NetworkAccelerationMode.Streaming);

        Assert.Equal(NetworkAccelerationText.ModeStreaming, presentation.DisplayName);
        Assert.Equal(NetworkAccelerationText.ModeStreamingDescription, presentation.Description);
        Assert.Equal(NetworkAccelerationText.ModeStreamingFocus, presentation.Focus);
    }

    [Theory]
    [InlineData(NetworkAccelerationMode.Balanced)]
    [InlineData((NetworkAccelerationMode)999)]
    public void GetModePresentation_UnknownOrBalanced_ReturnsBalancedPresentation(NetworkAccelerationMode mode)
    {
        var presentation = NetworkAccelerationPresentation.GetModePresentation(mode);

        Assert.Equal(NetworkAccelerationText.ModeBalanced, presentation.DisplayName);
        Assert.Equal(NetworkAccelerationText.ModeBalancedDescription, presentation.Description);
    }

    [Fact]
    public void GetModePresentation_AllModes_ReturnNonNull()
    {
        foreach (var mode in Enum.GetValues<NetworkAccelerationMode>())
        {
            var presentation = NetworkAccelerationPresentation.GetModePresentation(mode);
            Assert.NotNull(presentation);
            Assert.False(string.IsNullOrWhiteSpace(presentation.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(presentation.Description));
            Assert.False(string.IsNullOrWhiteSpace(presentation.RecommendedFor));
        }
    }

    #endregion

    #region GetToggleLabel

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetToggleLabel_ReturnsNonNullString(bool enabled)
    {
        var label = NetworkAccelerationPresentation.GetToggleLabel(enabled);
        Assert.False(string.IsNullOrWhiteSpace(label));
    }

    [Fact]
    public void GetToggleLabel_Enabled_DisabledReturnDifferentStrings()
    {
        var enabled = NetworkAccelerationPresentation.GetToggleLabel(true);
        var disabled = NetworkAccelerationPresentation.GetToggleLabel(false);
        Assert.NotEqual(enabled, disabled);
    }

    #endregion

    #region GetPlanSummary

    [Fact]
    public void GetPlanSummary_NullPlan_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NetworkAccelerationPresentation.GetPlanSummary(null!));
    }

    [Fact]
    public void GetPlanSummary_EmptySteps_ReturnsNoStepsMessage()
    {
        var plan = new NetworkOptimizationPlan(NetworkAccelerationMode.Balanced, Array.Empty<NetworkOptimizationStep>());

        var summary = NetworkAccelerationPresentation.GetPlanSummary(plan);

        Assert.Equal(NetworkAccelerationText.NoPlannedOptimizationSteps, summary);
    }

    [Fact]
    public void GetPlanSummary_WithSteps_ContainsStepCount()
    {
        var steps = new NetworkOptimizationStep[]
        {
            new("FlushDns", "ipconfig.exe", "/flushdns", true),
            new("ResetWinsock", "netsh.exe", "winsock reset", false)
        };
        var plan = new NetworkOptimizationPlan(NetworkAccelerationMode.Balanced, steps);

        var summary = NetworkAccelerationPresentation.GetPlanSummary(plan);

        Assert.Contains("1", summary);
        Assert.Contains("2", summary);
    }

    #endregion

    #region Record Equality (NetworkOptimizationStep)

    [Fact]
    public void NetworkOptimizationStep_EqualValues_AreEqual()
    {
        var step1 = new NetworkOptimizationStep("FlushDns", "ipconfig.exe", "/flushdns", true);
        var step2 = new NetworkOptimizationStep("FlushDns", "ipconfig.exe", "/flushdns", true);

        Assert.Equal(step1, step2);
        Assert.Equal(step1.GetHashCode(), step2.GetHashCode());
    }

    [Fact]
    public void NetworkOptimizationStep_DifferentValues_AreNotEqual()
    {
        var step1 = new NetworkOptimizationStep("FlushDns", "ipconfig.exe", "/flushdns", true);
        var step2 = new NetworkOptimizationStep("ResetWinsock", "netsh.exe", "winsock reset", false);

        Assert.NotEqual(step1, step2);
    }

    #endregion
}
