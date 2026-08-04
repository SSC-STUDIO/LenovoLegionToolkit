using System;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.UI;

[Trait("Category", TestCategories.Unit)]
public class ThemeAndNotificationEnumTests
{
    #region Theme Enum Tests

    [Fact]
    public void Theme_ShouldHaveThreeValues()
    {
        Enum.GetValues<Theme>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(Theme.System, 0)]
    [InlineData(Theme.Light, 1)]
    [InlineData(Theme.Dark, 2)]
    public void Theme_ShouldHaveExpectedValues(Theme theme, int expectedValue)
    {
        ((int)theme).Should().Be(expectedValue);
    }

    #endregion

    #region ThemeStylePreset Enum Tests

    [Fact]
    public void ThemeStylePreset_ShouldHaveFourValues()
    {
        Enum.GetValues<ThemeStylePreset>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(ThemeStylePreset.Default, 0)]
    [InlineData(ThemeStylePreset.Official, 1)]
    [InlineData(ThemeStylePreset.Midnight, 2)]
    [InlineData(ThemeStylePreset.Forest, 3)]
    public void ThemeStylePreset_ShouldHaveExpectedValues(ThemeStylePreset preset, int expectedValue)
    {
        ((int)preset).Should().Be(expectedValue);
    }

    [Fact]
    public void AccentPalette_ShouldChangeWithSelectedAccentColor()
    {
        var red = ThemeManager.CreateAccentPalette(new RGBColor(220, 55, 65), isDark: false);
        var teal = ThemeManager.CreateAccentPalette(new RGBColor(30, 180, 170), isDark: false);

        red.ApplicationBackground.Should().NotBe(teal.ApplicationBackground);
        red.ControlFillDefault.Should().NotBe(teal.ControlFillDefault);
        red.ControlStrokeDefault.Should().NotBe(teal.ControlStrokeDefault);
        red.TextSecondary.Should().NotBe(teal.TextSecondary);
    }

    [Fact]
    public void AccentPalette_ShouldKeepDarkSurfaceLayersOrdered()
    {
        var palette = ThemeManager.CreateAccentPalette(new RGBColor(80, 140, 230), isDark: true);

        Luminance(palette.ApplicationBackground).Should().BeLessThan(Luminance(palette.ControlFillDefault));
        Luminance(palette.ControlFillDefault).Should().BeLessThan(Luminance(palette.ControlFillSecondary));
        Luminance(palette.ControlFillSecondary).Should().BeLessThan(Luminance(palette.ControlFillTertiary));
    }

    private static double Luminance(System.Windows.Media.Color color) =>
        0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;

    #endregion

    #region AccentColorSource Enum Tests

    [Theory]
    [InlineData(AccentColorSource.System, 0)]
    [InlineData(AccentColorSource.Custom, 1)]
    public void AccentColorSource_ShouldHaveExpectedValues(AccentColorSource source, int expectedValue)
    {
        ((int)source).Should().Be(expectedValue);
    }

    #endregion

    #region WindowBackdropStyle Enum Tests

    [Theory]
    [InlineData(WindowBackdropStyle.Windows, 0)]
    [InlineData(WindowBackdropStyle.macOS, 1)]
    [InlineData(WindowBackdropStyle.Off, 2)]
    public void WindowBackdropStyle_ShouldHaveExpectedValues(WindowBackdropStyle style, int expectedValue)
    {
        ((int)style).Should().Be(expectedValue);
    }

    #endregion

    #region NotificationPosition Enum Tests

    [Fact]
    public void NotificationPosition_ShouldHaveNineValues()
    {
        Enum.GetValues<NotificationPosition>().Should().HaveCount(9);
    }

    [Theory]
    [InlineData(NotificationPosition.BottomRight, 0)]
    [InlineData(NotificationPosition.BottomCenter, 1)]
    [InlineData(NotificationPosition.BottomLeft, 2)]
    [InlineData(NotificationPosition.CenterLeft, 3)]
    [InlineData(NotificationPosition.TopLeft, 4)]
    [InlineData(NotificationPosition.TopCenter, 5)]
    [InlineData(NotificationPosition.TopRight, 6)]
    [InlineData(NotificationPosition.CenterRight, 7)]
    [InlineData(NotificationPosition.Center, 8)]
    public void NotificationPosition_ShouldHaveExpectedValues(NotificationPosition pos, int expectedValue)
    {
        ((int)pos).Should().Be(expectedValue);
    }

    #endregion

    #region NotificationDuration Enum Tests

    [Theory]
    [InlineData(NotificationDuration.Short, 0)]
    [InlineData(NotificationDuration.Normal, 1)]
    [InlineData(NotificationDuration.Long, 2)]
    public void NotificationDuration_ShouldHaveExpectedValues(NotificationDuration duration, int expectedValue)
    {
        ((int)duration).Should().Be(expectedValue);
    }

    #endregion

    #region NotificationPriority Enum Tests

    [Theory]
    [InlineData(NotificationPriority.Low, 0)]
    [InlineData(NotificationPriority.Normal, 1)]
    [InlineData(NotificationPriority.High, 2)]
    public void NotificationPriority_ShouldHaveExpectedValues(NotificationPriority priority, int expectedValue)
    {
        ((int)priority).Should().Be(expectedValue);
    }

    #endregion

    #region PowerModeMappingMode Enum Tests

    [Theory]
    [InlineData(PowerModeMappingMode.Disabled, 0)]
    [InlineData(PowerModeMappingMode.WindowsPowerMode, 1)]
    [InlineData(PowerModeMappingMode.WindowsPowerPlan, 2)]
    public void PowerModeMappingMode_ShouldHaveExpectedValues(PowerModeMappingMode mode, int expectedValue)
    {
        ((int)mode).Should().Be(expectedValue);
    }

    #endregion

    #region NotificationType Enum Coverage

    [Theory]
    [InlineData(NotificationType.ACAdapterConnected, 0)]
    [InlineData(NotificationType.ACAdapterConnectedLowWattage, 1)]
    [InlineData(NotificationType.ACAdapterDisconnected, 2)]
    [InlineData(NotificationType.AutomationNotification, 3)]
    [InlineData(NotificationType.CameraOn, 4)]
    [InlineData(NotificationType.CameraOff, 5)]
    [InlineData(NotificationType.CapsLockOn, 6)]
    [InlineData(NotificationType.CapsLockOff, 7)]
    [InlineData(NotificationType.FnLockOn, 8)]
    [InlineData(NotificationType.FnLockOff, 9)]
    [InlineData(NotificationType.MicrophoneOff, 10)]
    [InlineData(NotificationType.MicrophoneOn, 11)]
    [InlineData(NotificationType.NumLockOn, 12)]
    [InlineData(NotificationType.NumLockOff, 13)]
    public void NotificationType_ShouldHaveExpectedValues(NotificationType type, int expectedValue)
    {
        ((int)type).Should().Be(expectedValue);
    }

    [Fact]
    public void NotificationType_ShouldHaveFortyValues()
    {
        Enum.GetValues<NotificationType>().Should().HaveCount(40);
    }

    #endregion

    #region UpdateCheckSettings Default Store Tests

    [Fact]
    public void UpdateCheckSettingsStore_Default_ShouldHaveExpectedValues()
    {
        var store = new UpdateCheckSettings.UpdateCheckSettingsStore();
        store.LastUpdateCheckDateTime.Should().BeNull();
        store.UpdateCheckFrequency.Should().Be(UpdateCheckFrequency.PerHour);
        store.UpdateRepositoryOwner.Should().BeNull();
        store.UpdateRepositoryName.Should().BeNull();
    }

    #endregion

    #region FeatureStateMessage Additional Tests

    [Fact]
    public void FeatureStateMessage_Double_ShouldRetainValue()
    {
        var msg = new FeatureStateMessage<double>(3.14);
        msg.State.Should().Be(3.14);
    }

    [Fact]
    public void FeatureStateMessage_Enum_ShouldRetainValue()
    {
        var msg = new FeatureStateMessage<Theme>(Theme.Dark);
        msg.State.Should().Be(Theme.Dark);
    }

    [Fact]
    public void FeatureStateMessage_Bool_False_ShouldRetainFalse()
    {
        var msg = new FeatureStateMessage<bool>(false);
        msg.State.Should().BeFalse();
    }

    #endregion

    #region Messaging Marker Classes

    [Fact]
    public void SpectrumBacklightChangedMessage_ShouldImplementIMessage()
    {
        var msg = new SpectrumBacklightChangedMessage();
        msg.Should().BeAssignableTo<IMessage>();
    }

    [Fact]
    public void RGBKeyboardBacklightChangedMessage_ShouldImplementIMessage()
    {
        var msg = new RGBKeyboardBacklightChangedMessage();
        msg.Should().BeAssignableTo<IMessage>();
    }

    #endregion

    #region OsdAppearanceChangedMessage Tests

    [Fact]
    public void OsdAppearanceChangedMessage_ShouldImplementIMessage()
    {
        var msg = new OsdAppearanceChangedMessage();
        msg.Should().BeAssignableTo<IMessage>();
    }

    #endregion

    #region Settings Store Model Tests

    [Fact]
    public void SpectrumKeyboardSettingsStore_Default_ShouldHaveNullLayout()
    {
        var store = new SpectrumKeyboardSettings.SpectrumKeyboardSettingsStore();
        store.KeyboardLayout.Should().BeNull();
    }

    [Fact]
    public void SpectrumKeyboardSettingsStore_SetLayout_ShouldRetainValue()
    {
        var store = new SpectrumKeyboardSettings.SpectrumKeyboardSettingsStore
        {
            KeyboardLayout = KeyboardLayout.Iso
        };
        store.KeyboardLayout.Should().Be(KeyboardLayout.Iso);
    }

    [Fact]
    public void PackageDownloaderSettingsStore_Defaults_ShouldHaveExpectedValues()
    {
        var store = new PackageDownloaderSettings.PackageDownloaderSettingsStore();
        store.DownloadPath.Should().BeNull();
        store.OnlyShowUpdates.Should().BeFalse();
        store.HiddenPackages.Should().NotBeNull();
        store.HiddenPackages.Should().BeEmpty();
    }

    [Fact]
    public void SunriseSunsetSettingsStore_Defaults_ShouldBeNull()
    {
        var store = new SunriseSunsetSettings.SunriseSunsetSettingsStore();
        store.LastCheckDateTime.Should().BeNull();
        store.Sunrise.Should().BeNull();
        store.Sunset.Should().BeNull();
    }

    #endregion
}
