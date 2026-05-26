using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

/// <summary>
/// Unit tests for the SoftwareDisabler module: AbstractSoftwareDisabler, VantageDisabler,
/// FnKeysDisabler, and LegionZoneDisabler.
/// </summary>
public class SoftwareDisablerTests
{
    #region SoftwareStatus Enum Tests

    [Fact]
    public void SoftwareStatus_HasThreeValues()
    {
        var values = Enum.GetValues<SoftwareStatus>();

        values.Should().HaveCount(3);
        values.Should().Contain(SoftwareStatus.Enabled);
        values.Should().Contain(SoftwareStatus.Disabled);
        values.Should().Contain(SoftwareStatus.NotFound);
    }

    [Fact]
    public void SoftwareStatus_Enabled_HasExpectedValue()
    {
        ((int)SoftwareStatus.Enabled).Should().Be(0);
    }

    [Fact]
    public void SoftwareStatus_Disabled_HasExpectedValue()
    {
        ((int)SoftwareStatus.Disabled).Should().Be(1);
    }

    [Fact]
    public void SoftwareStatus_NotFound_HasExpectedValue()
    {
        ((int)SoftwareStatus.NotFound).Should().Be(2);
    }

    #endregion

    #region SoftwareDisablerException Tests

    [Fact]
    public void SoftwareDisablerException_WithMessageAndInner_SetsProperties()
    {
        // Arrange
        var inner = new InvalidOperationException("inner");

        // Act
        var ex = new SoftwareDisablerException("test message", inner);

        // Assert
        ex.Message.Should().Contain("test message");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void SoftwareDisablerException_IsException()
    {
        // Act
        var ex = new SoftwareDisablerException("msg", new Exception());

        // Assert
        ex.Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region VantageDisabler Protected Properties

    [Fact]
    public void VantageDisabler_HasScheduledTasksPaths()
    {
        // Arrange
        var disabler = new VantageDisabler();
        var property = typeof(VantageDisabler).GetProperty("ScheduledTasksPaths", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var paths = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        paths.Should().NotBeEmpty();
        paths.Should().Contain("Lenovo\\Vantage");
        paths.Should().Contain("Lenovo\\ImController");
    }

    [Fact]
    public void VantageDisabler_HasServiceNames()
    {
        // Arrange
        var disabler = new VantageDisabler();
        var property = typeof(VantageDisabler).GetProperty("ServiceNames", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var names = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        names.Should().NotBeEmpty();
        names.Should().Contain("ImControllerService");
        names.Should().Contain("LenovoVantageService");
    }

    [Fact]
    public void VantageDisabler_HasProcessNames()
    {
        // Arrange
        var disabler = new VantageDisabler();
        var property = typeof(VantageDisabler).GetProperty("ProcessNames", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var names = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        names.Should().NotBeEmpty();
        names.Should().Contain("LenovoVantage");
    }

    [Fact]
    public void VantageDisabler_ScheduledTasksPaths_ContainsExpectedCount()
    {
        // Arrange
        var disabler = new VantageDisabler();
        var property = typeof(VantageDisabler).GetProperty("ScheduledTasksPaths", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var paths = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert - VantageDisabler has 7 scheduled task paths
        paths.Count().Should().Be(7);
    }

    #endregion

    #region FnKeysDisabler Protected Properties

    [Fact]
    public void FnKeysDisabler_HasEmptyScheduledTasksPaths()
    {
        // Arrange
        var disabler = new FnKeysDisabler();
        var property = typeof(FnKeysDisabler).GetProperty("ScheduledTasksPaths", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var paths = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        paths.Should().BeEmpty();
    }

    [Fact]
    public void FnKeysDisabler_HasServiceNames()
    {
        // Arrange
        var disabler = new FnKeysDisabler();
        var property = typeof(FnKeysDisabler).GetProperty("ServiceNames", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var names = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        names.Should().ContainSingle("LenovoFnAndFunctionKeys");
    }

    [Fact]
    public void FnKeysDisabler_HasProcessNames()
    {
        // Arrange
        var disabler = new FnKeysDisabler();
        var property = typeof(FnKeysDisabler).GetProperty("ProcessNames", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var names = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        names.Should().Contain("LenovoUtilityUI");
        names.Should().Contain("LenovoUtilityService");
        names.Should().Contain("LenovoSmartKey");
        names.Should().HaveCount(3);
    }

    #endregion

    #region LegionZoneDisabler Protected Properties

    [Fact]
    public void LegionZoneDisabler_HasEmptyScheduledTasksPaths()
    {
        // Arrange
        var disabler = new LegionZoneDisabler();
        var property = typeof(LegionZoneDisabler).GetProperty("ScheduledTasksPaths", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var paths = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        paths.Should().BeEmpty();
    }

    [Fact]
    public void LegionZoneDisabler_HasServiceNames()
    {
        // Arrange
        var disabler = new LegionZoneDisabler();
        var property = typeof(LegionZoneDisabler).GetProperty("ServiceNames", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var names = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        names.Should().ContainSingle("LZService");
    }

    [Fact]
    public void LegionZoneDisabler_HasProcessNames()
    {
        // Arrange
        var disabler = new LegionZoneDisabler();
        var property = typeof(LegionZoneDisabler).GetProperty("ProcessNames", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var names = (IEnumerable<string>)property.GetValue(disabler)!;

        // Assert
        names.Should().Contain("LegionZone");
        names.Should().Contain("LZTray");
        names.Should().HaveCount(2);
    }

    #endregion

    #region OnRefreshed Event Tests

    [Fact]
    public void VantageDisabler_OnRefreshed_Event_CanSubscribe()
    {
        // Arrange
        var disabler = new VantageDisabler();
        var callCount = 0;

        // Act
        disabler.OnRefreshed += (_, _) => callCount++;

        // Assert - no exception thrown
        callCount.Should().Be(0);
    }

    [Fact]
    public void FnKeysDisabler_OnRefreshed_Event_CanSubscribe()
    {
        // Arrange
        var disabler = new FnKeysDisabler();
        var callCount = 0;

        // Act
        disabler.OnRefreshed += (_, _) => callCount++;

        // Assert
        callCount.Should().Be(0);
    }

    [Fact]
    public void LegionZoneDisabler_OnRefreshed_Event_CanSubscribe()
    {
        // Arrange
        var disabler = new LegionZoneDisabler();
        var callCount = 0;

        // Act
        disabler.OnRefreshed += (_, _) => callCount++;

        // Assert
        callCount.Should().Be(0);
    }

    #endregion

    #region AbstractSoftwareDisablerEventArgs Tests

    [Fact]
    public void AbstractSoftwareDisablerEventArgs_SetsStatus()
    {
        // Arrange & Act
        var args = new AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs { Status = SoftwareStatus.Enabled };

        // Assert
        args.Status.Should().Be(SoftwareStatus.Enabled);
    }

    [Fact]
    public void AbstractSoftwareDisablerEventArgs_CanSetAllStatusValues()
    {
        foreach (var status in Enum.GetValues<SoftwareStatus>())
        {
            // Act
            var args = new AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs { Status = status };

            // Assert
            args.Status.Should().Be(status);
        }
    }

    #endregion

    #region GetStatusAsync Tests

    [Fact]
    public async Task VantageDisabler_GetStatusAsync_ReturnsValidStatus()
    {
        // Arrange
        var disabler = new VantageDisabler();

        // Act
        var status = await disabler.GetStatusAsync();

        // Assert - any valid enum value is acceptable
        Enum.GetValues<SoftwareStatus>().Should().Contain(status);
    }

    [Fact]
    public async Task FnKeysDisabler_GetStatusAsync_ReturnsValidStatus()
    {
        // Arrange
        var disabler = new FnKeysDisabler();

        // Act
        var status = await disabler.GetStatusAsync();

        // Assert
        Enum.GetValues<SoftwareStatus>().Should().Contain(status);
    }

    [Fact]
    public async Task LegionZoneDisabler_GetStatusAsync_ReturnsValidStatus()
    {
        // Arrange
        var disabler = new LegionZoneDisabler();

        // Act
        var status = await disabler.GetStatusAsync();

        // Assert
        Enum.GetValues<SoftwareStatus>().Should().Contain(status);
    }

    #endregion
}
