using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace LenovoLegionToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class MachineInformationTests
{
    #region FeatureData

    [Fact]
    public void FeatureData_Unknown_ShouldHaveUnknownSource()
    {
        var fd = MachineInformation.FeatureData.Unknown;
        fd.Source.Should().Be(MachineInformation.FeatureData.SourceType.Unknown);
    }

    [Fact]
    public void FeatureData_Unknown_All_ShouldBeEmpty()
    {
        var fd = MachineInformation.FeatureData.Unknown;
        fd.All.Should().BeEmpty();
    }

    [Fact]
    public void FeatureData_WithCapabilities_All_ShouldReturnOrdered()
    {
        var capabilities = new[] { CapabilityID.OverDrive, CapabilityID.IGPUMode, CapabilityID.FlipToStart };
        var fd = new MachineInformation.FeatureData(MachineInformation.FeatureData.SourceType.CapabilityData, capabilities);

        fd.All.Should().ContainInOrder(
            CapabilityID.IGPUMode,
            CapabilityID.FlipToStart,
            CapabilityID.OverDrive);
    }

    [Fact]
    public void FeatureData_IndexerGet_ContainsCapability_ShouldReturnTrue()
    {
        var capabilities = new[] { CapabilityID.IGPUMode, CapabilityID.OverDrive };
        var fd = new MachineInformation.FeatureData(MachineInformation.FeatureData.SourceType.Flags, capabilities);

        fd[CapabilityID.IGPUMode].Should().BeTrue();
        fd[CapabilityID.OverDrive].Should().BeTrue();
    }

    [Fact]
    public void FeatureData_IndexerGet_DoesNotContainCapability_ShouldReturnFalse()
    {
        var capabilities = new[] { CapabilityID.IGPUMode };
        var fd = new MachineInformation.FeatureData(MachineInformation.FeatureData.SourceType.Flags, capabilities);

        fd[CapabilityID.OverDrive].Should().BeFalse();
    }

    [Fact]
    public void FeatureData_Source_ShouldReflectConstructorValue()
    {
        var fd = new MachineInformation.FeatureData(MachineInformation.FeatureData.SourceType.CapabilityData);
        fd.Source.Should().Be(MachineInformation.FeatureData.SourceType.CapabilityData);
    }

    #endregion

    #region PropertyData

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void PropertyData_SupportsGodMode_ShouldBeV1OrV2(bool v1, bool v2, bool expected)
    {
        var pd = new MachineInformation.PropertyData
        {
            SupportsGodModeV1 = v1,
            SupportsGodModeV2 = v2
        };
        pd.SupportsGodMode.Should().Be(expected);
    }

    [Fact]
    public void PropertyData_AllProperties_ShouldDefaultToFalse()
    {
        var pd = new MachineInformation.PropertyData();
        pd.SupportsGodMode.Should().BeFalse();
        pd.SupportsGSync.Should().BeFalse();
        pd.SupportsIGPUMode.Should().BeFalse();
        pd.SupportsAIMode.Should().BeFalse();
        pd.SupportBootLogoChange.Should().BeFalse();
        pd.HasQuietToPerformanceModeSwitchingBug.Should().BeFalse();
        pd.HasGodModeToOtherModeSwitchingBug.Should().BeFalse();
        pd.IsExcludedFromLenovoLighting.Should().BeFalse();
        pd.IsExcludedFromPanelLogoLenovoLighting.Should().BeFalse();
        pd.HasAlternativeFullSpectrumLayout.Should().BeFalse();
    }

    [Fact]
    public void PropertyData_SupportsAlwaysOnAc_ShouldDefaultToFalseTuple()
    {
        var pd = new MachineInformation.PropertyData();
        pd.SupportsAlwaysOnAc.status.Should().BeFalse();
        pd.SupportsAlwaysOnAc.connectivity.Should().BeFalse();
    }

    [Fact]
    public void PropertyData_SetProperties_ShouldRetainValues()
    {
        var pd = new MachineInformation.PropertyData
        {
            SupportsGSync = true,
            SupportsIGPUMode = true,
            HasQuietToPerformanceModeSwitchingBug = true,
            SupportsAlwaysOnAc = (true, false)
        };
        pd.SupportsGSync.Should().BeTrue();
        pd.SupportsIGPUMode.Should().BeTrue();
        pd.HasQuietToPerformanceModeSwitchingBug.Should().BeTrue();
        pd.SupportsAlwaysOnAc.status.Should().BeTrue();
        pd.SupportsAlwaysOnAc.connectivity.Should().BeFalse();
    }

    #endregion
}
