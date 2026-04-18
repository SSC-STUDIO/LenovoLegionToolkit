using System;
using System.Threading.Tasks;
using Moq;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Features.Hybrid;
using LenovoLegionToolkit.Lib.Features.Hybrid.Notify;

namespace LenovoLegionToolkit.Tests.Features;

/// <summary>
/// Base class for Feature tests providing common test infrastructure
/// </summary>
public abstract class FeatureTestBase
{
    /// <summary>
    /// Create a mock IFeature with basic setup
    /// </summary>
    protected Mock<IFeature<TState>> CreateMockFeature<TState>() where TState : struct
    {
        return new Mock<IFeature<TState>>();
    }

    /// <summary>
    /// Setup a mock feature to return specific state
    /// </summary>
    protected void SetupMockFeatureState<TFeature, TState>(
        Mock<TFeature> mock,
        bool isSupported,
        TState[] allStates,
        TState currentState) where TFeature : class, IFeature<TState> where TState : struct
    {
        mock
            .Setup(f => f.IsSupportedAsync())
            .ReturnsAsync(isSupported);

        mock
            .Setup(f => f.GetAllStatesAsync())
            .ReturnsAsync(allStates);

        mock
            .Setup(f => f.GetStateAsync())
            .ReturnsAsync(currentState);
    }

    /// <summary>
    /// Create a mock DGPUNotify for hybrid feature tests
    /// </summary>
    protected Mock<DGPUNotify> CreateMockDGPUNotify()
    {
        var mock = new Mock<DGPUNotify>();
        mock
            .Setup(n => n.IsSupportedAsync())
            .ReturnsAsync(true);
        mock
            .Setup(n => n.IsDGPUAvailableAsync())
            .ReturnsAsync(false);
        return mock;
    }

    /// <summary>
    /// Create a mock GSyncFeature for hybrid feature tests
    /// </summary>
    protected Mock<IGSyncFeature> CreateMockGSyncFeature()
    {
        return new Mock<IGSyncFeature>();
    }

    /// <summary>
    /// Create a mock IGPUModeFeature for hybrid feature tests
    /// </summary>
    protected Mock<IIGPUModeFeature> CreateMockIGPUModeFeature()
    {
        return new Mock<IIGPUModeFeature>();
    }

    /// <summary>
    /// Verify feature state change was called
    /// </summary>
    protected void VerifyStateChange<TFeature, TState>(
        Mock<TFeature> mock,
        TState expectedState,
        Times times) where TFeature : class, IFeature<TState> where TState : struct
    {
        mock.Verify(
            f => f.SetStateAsync(expectedState),
            times);
    }
}