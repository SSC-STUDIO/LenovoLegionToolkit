using UniversalDeviceToolkit.Plugins.NetworkAcceleration;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.NetworkAcceleration.Tests;

[Collection("NetworkAccelerationResourceCulture")]
public class NetworkAccelerationSettingsEdgeCaseTests
{
    [Fact]
    public void CreateDefault_ContainsExpectedDefaults()
    {
        var defaults = NetworkAccelerationSettings.CreateDefault();

        Assert.Equal(NetworkAccelerationMode.Balanced, defaults.PreferredMode);
        Assert.False(defaults.AutoOptimizeOnStartup);
        Assert.True(defaults.ResetWinsockOnOptimize);
        Assert.False(defaults.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var original = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Gaming,
            AutoOptimizeOnStartup = true,
            ResetWinsockOnOptimize = false,
            ResetTcpIpOnOptimize = true
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.PreferredMode, clone.PreferredMode);
        Assert.Equal(original.AutoOptimizeOnStartup, clone.AutoOptimizeOnStartup);
        Assert.Equal(original.ResetWinsockOnOptimize, clone.ResetWinsockOnOptimize);
        Assert.Equal(original.ResetTcpIpOnOptimize, clone.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        var original = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Streaming,
            AutoOptimizeOnStartup = true,
            ResetWinsockOnOptimize = true,
            ResetTcpIpOnOptimize = false
        };

        var clone = original.Clone();
        clone.PreferredMode = NetworkAccelerationMode.Gaming;
        clone.AutoOptimizeOnStartup = false;

        Assert.Equal(NetworkAccelerationMode.Streaming, original.PreferredMode);
        Assert.True(original.AutoOptimizeOnStartup);
    }

    [Fact]
    public void With_NullParameters_ReturnsEqualClone()
    {
        var original = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Gaming,
            AutoOptimizeOnStartup = true,
            ResetWinsockOnOptimize = false,
            ResetTcpIpOnOptimize = true
        };

        var result = original.With();

        Assert.NotSame(original, result);
        Assert.Equal(original.PreferredMode, result.PreferredMode);
        Assert.Equal(original.AutoOptimizeOnStartup, result.AutoOptimizeOnStartup);
        Assert.Equal(original.ResetWinsockOnOptimize, result.ResetWinsockOnOptimize);
        Assert.Equal(original.ResetTcpIpOnOptimize, result.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void With_SingleParameter_OnlyChangesThatOne()
    {
        var original = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Balanced,
            AutoOptimizeOnStartup = false,
            ResetWinsockOnOptimize = true,
            ResetTcpIpOnOptimize = false
        };

        var result = original.With(preferredMode: NetworkAccelerationMode.Streaming);

        Assert.Equal(NetworkAccelerationMode.Streaming, result.PreferredMode);
        Assert.False(result.AutoOptimizeOnStartup);
        Assert.True(result.ResetWinsockOnOptimize);
        Assert.False(result.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void With_MultipleParameters_UpdatesAllSpecified()
    {
        var original = NetworkAccelerationSettings.CreateDefault();

        var result = original.With(
            preferredMode: NetworkAccelerationMode.Gaming,
            autoOptimizeOnStartup: true,
            resetTcpIpOnOptimize: true);

        Assert.Equal(NetworkAccelerationMode.Gaming, result.PreferredMode);
        Assert.True(result.AutoOptimizeOnStartup);
        Assert.True(original.ResetWinsockOnOptimize); // unchanged from default
        Assert.True(result.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void With_NeverModifiesOriginal()
    {
        var original = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Gaming,
            AutoOptimizeOnStartup = true,
            ResetWinsockOnOptimize = false,
            ResetTcpIpOnOptimize = true
        };

        var _ = original.With(
            preferredMode: NetworkAccelerationMode.Streaming,
            autoOptimizeOnStartup: false);

        Assert.Equal(NetworkAccelerationMode.Gaming, original.PreferredMode);
        Assert.True(original.AutoOptimizeOnStartup);
        Assert.False(original.ResetWinsockOnOptimize);
        Assert.True(original.ResetTcpIpOnOptimize);
    }
}
