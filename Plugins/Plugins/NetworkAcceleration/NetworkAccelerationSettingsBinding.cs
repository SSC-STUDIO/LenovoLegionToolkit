using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

internal static class NetworkAccelerationSettingsBinding
{
    public static bool HasToggleCheckBoxes(CheckBox? autoOptimizeOnStartupCheckBox, CheckBox? resetWinsockCheckBox, CheckBox? resetTcpIpCheckBox)
    {
        return autoOptimizeOnStartupCheckBox is not null
            && resetWinsockCheckBox is not null
            && resetTcpIpCheckBox is not null;
    }

    public static void ApplyToggleSettings(
        NetworkAccelerationSettings settings,
        CheckBox autoOptimizeOnStartupCheckBox,
        CheckBox resetWinsockCheckBox,
        CheckBox resetTcpIpCheckBox)
    {
        autoOptimizeOnStartupCheckBox.IsChecked = settings.AutoOptimizeOnStartup;
        resetWinsockCheckBox.IsChecked = settings.ResetWinsockOnOptimize;
        resetTcpIpCheckBox.IsChecked = settings.ResetTcpIpOnOptimize;
    }

    public static NetworkAccelerationSettings BuildUpdatedSettings(
        NetworkAccelerationSettings currentSettings,
        CheckBox? autoOptimizeOnStartupCheckBox,
        CheckBox? resetWinsockCheckBox,
        CheckBox? resetTcpIpCheckBox,
        NetworkAccelerationMode? preferredMode = null)
    {
        return currentSettings.With(
            preferredMode: preferredMode,
            autoOptimizeOnStartup: autoOptimizeOnStartupCheckBox?.IsChecked == true,
            resetWinsockOnOptimize: resetWinsockCheckBox?.IsChecked == true,
            resetTcpIpOnOptimize: resetTcpIpCheckBox?.IsChecked == true);
    }
}
