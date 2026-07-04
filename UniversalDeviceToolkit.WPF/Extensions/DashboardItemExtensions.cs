using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using UniversalDeviceToolkit.WPF.Controls;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Extensions;

public static class DashboardItemExtensions
{
    public static SymbolRegular GetIcon(this DashboardItem dashboardItem) => dashboardItem switch
    {
        DashboardItem.PowerMode => SymbolRegular.Gauge24,
        DashboardItem.BatteryMode => SymbolRegular.BatteryCharge24,
        DashboardItem.BatteryNightChargeMode => SymbolRegular.WeatherMoon24,
        DashboardItem.AlwaysOnUsb => SymbolRegular.UsbStick24,
        DashboardItem.InstantBoot => SymbolRegular.PlugDisconnected24,
        DashboardItem.HybridMode => SymbolRegular.LeafOne24,
        DashboardItem.DiscreteGpu => SymbolRegular.DeveloperBoard24,
        DashboardItem.OverclockDiscreteGpu => SymbolRegular.DeveloperBoardLightning20,
        DashboardItem.Resolution => SymbolRegular.ScaleFill24,
        DashboardItem.RefreshRate => SymbolRegular.DesktopPulse24,
        DashboardItem.DpiScale => SymbolRegular.TextFontSize24,
        DashboardItem.Hdr => SymbolRegular.Hdr24,
        DashboardItem.OverDrive => SymbolRegular.TopSpeed24,
        DashboardItem.PanelLogoBacklight => SymbolRegular.LightbulbCircle24,
        DashboardItem.PortsBacklight => SymbolRegular.UsbPlug24,
        DashboardItem.TurnOffMonitors => SymbolRegular.Desktop24,
        DashboardItem.Microphone => SymbolRegular.Mic24,
        DashboardItem.FlipToStart => SymbolRegular.Power24,
        DashboardItem.TouchpadLock => SymbolRegular.Tablet24,
        DashboardItem.FnLock => SymbolRegular.Keyboard24,
        DashboardItem.WinKeyLock => SymbolRegular.Keyboard24,
        DashboardItem.WhiteKeyboardBacklight => SymbolRegular.Keyboard24,
        DashboardItem.ItsMode => SymbolRegular.Gauge24,
        _ => throw new InvalidOperationException($"Invalid DashboardItem {dashboardItem}"),
    };

    public static string GetTitle(this DashboardItem dashboardItem) => dashboardItem switch
    {
        DashboardItem.PowerMode => Resource.PowerModeControl_Title,
        DashboardItem.BatteryMode => Resource.BatteryModeControl_Title,
        DashboardItem.BatteryNightChargeMode => Resource.BatteryNightChargeModeControl_Title,
        DashboardItem.AlwaysOnUsb => Resource.AlwaysOnUSBControl_Title,
        DashboardItem.InstantBoot => Resource.InstantBootControl_Title,
        DashboardItem.HybridMode => $"{Resource.ComboBoxHybridModeControl_Title} / {Resource.ToggleHybridModeControl_Title}",
        DashboardItem.DiscreteGpu => Resource.DiscreteGPUControl_Title,
        DashboardItem.OverclockDiscreteGpu => Resource.OverclockDiscreteGPUControl_Title,
        DashboardItem.Resolution => Resource.ResolutionControl_Title,
        DashboardItem.RefreshRate => Resource.RefreshRateControl_Title,
        DashboardItem.DpiScale => Resource.DpiScaleControl_Title,
        DashboardItem.Hdr => Resource.HDRControl_Title,
        DashboardItem.OverDrive => Resource.OverDriveControl_Title,
        DashboardItem.PanelLogoBacklight => Resource.PanelLogoBacklightControl_Title,
        DashboardItem.PortsBacklight => Resource.PortsBacklightControl_Title,
        DashboardItem.TurnOffMonitors => Resource.TurnOffMonitorsControl_Title,
        DashboardItem.Microphone => Resource.MicrophoneControl_Title,
        DashboardItem.FlipToStart => Resource.FlipToStartControl_Title,
        DashboardItem.TouchpadLock => Resource.TouchpadLockControl_Title,
        DashboardItem.FnLock => Resource.FnLockControl_Title,
        DashboardItem.WinKeyLock => Resource.WinKeyControl_Title,
        DashboardItem.WhiteKeyboardBacklight => Resource.WhiteKeyboardBacklightControl_Title,
        DashboardItem.ItsMode => "ITS Mode",
        _ => throw new InvalidOperationException($"Invalid DashboardItem {dashboardItem}"),
    };

    public static async Task<IEnumerable<AbstractRefreshingControl>> GetControlAsync(this DashboardItem dashboardItem) => dashboardItem switch
    {
        DashboardItem.PowerMode => [new PowerModeControl()],
        DashboardItem.BatteryMode => [new BatteryModeControl()],
        DashboardItem.BatteryNightChargeMode => [new BatteryNightChargeModeControl()],
        DashboardItem.AlwaysOnUsb => [new AlwaysOnUSBControl()],
        DashboardItem.InstantBoot => [new InstantBootControl()],
        DashboardItem.HybridMode => [await HybridModeControlFactory.GetControlAsync()],
        DashboardItem.DiscreteGpu => [new DiscreteGPUControl()],
        DashboardItem.OverclockDiscreteGpu => [new OverclockDiscreteGPUControl()],
        DashboardItem.Resolution => [new ResolutionControl()],
        DashboardItem.RefreshRate => [new RefreshRateControl()],
        DashboardItem.DpiScale => [new DpiScaleControl()],
        DashboardItem.Hdr => [new HDRControl()],
        DashboardItem.OverDrive => [new OverDriveControl()],
        DashboardItem.PanelLogoBacklight => [new PanelLogoBacklightControl()],
        DashboardItem.PortsBacklight => [new PortsBacklightControl()],
        DashboardItem.TurnOffMonitors => [new TurnOffMonitorsControl()],
        DashboardItem.Microphone => [new MicrophoneControl()],
        DashboardItem.FlipToStart => [new FlipToStartControl()],
        DashboardItem.TouchpadLock => [new TouchpadLockControl()],
        DashboardItem.FnLock => [new FnLockControl()],
        DashboardItem.WinKeyLock => [new WinKeyControl()],
        DashboardItem.WhiteKeyboardBacklight => [new WhiteKeyboardBacklightControl(), new OneLevelWhiteKeyboardBacklightControl()],
        DashboardItem.ItsMode => [new DashboardITSModeControl()],
        _ => throw new InvalidOperationException($"Invalid DashboardItem {dashboardItem}"),
    };
}

file sealed class DashboardITSModeControl : AbstractComboBoxFeatureCardControl<ITSMode>
{
    private readonly ITSModeFeature _itsModeFeature = IoCContainer.Resolve<ITSModeFeature>();

    public DashboardITSModeControl()
    {
        Icon = SymbolRegular.Gauge24;
        Title = "ITS Mode";
        Subtitle = "Intelligent Thermal Solution";
    }

    protected override string ComboBoxItemDisplayName(ITSMode value) => value.GetDisplayName();

    protected override async Task OnStateChangeAsync(ComboBox comboBox, IFeature<ITSMode> feature, ITSMode? newValue, ITSMode? oldValue)
    {
        if (newValue is null || oldValue is null)
            return;

        if (newValue.Value != oldValue.Value)
        {
            try
            {
                await _itsModeFeature.SetStateAsync(newValue.Value);
                _itsModeFeature.LastItsMode = newValue.Value;
            }
            catch (DllNotFoundException)
            {
                await MessageBoxHelper.ShowAsync(this, "ITS Mode", "ITS runtime is unavailable on this system.", Resource.OK);
            }
        }

        await base.OnStateChangeAsync(comboBox, feature, newValue, oldValue);
    }

    protected override void OnStateChangeException(Exception exception)
    {
        if (exception is PowerModeUnavailableWithoutACException ex1)
        {
            SnackbarHelper.Show(Resource.PowerModeUnavailableWithoutACException_Title,
                string.Format(Resource.PowerModeUnavailableWithoutACException_Message, ex1.PowerMode.GetDisplayName()),
                SnackbarType.Warning);
        }
    }
}
