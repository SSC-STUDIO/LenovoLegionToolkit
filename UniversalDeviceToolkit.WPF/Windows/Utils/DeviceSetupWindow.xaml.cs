using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.DeviceSupport;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Windows.Utils;

public partial class DeviceSetupWindow
{
    private readonly DevicePack? _recommendedPack;
    private readonly TaskCompletionSource<DeviceSetupResult> _taskCompletionSource = new();
    private bool _isPreparing;

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public Task<DeviceSetupResult> ShouldContinue => _taskCompletionSource.Task;

    public DeviceSetupWindow(MachineInformation machineInformation, DevicePack? recommendedPack, bool isBasicMode)
    {
        _recommendedPack = recommendedPack;

        InitializeComponent();

        _vendorText.Text = string.IsNullOrWhiteSpace(machineInformation.Vendor) ? Resource.Unnamed : machineInformation.Vendor;
        _modelText.Text = string.IsNullOrWhiteSpace(machineInformation.Model) ? Resource.Unnamed : machineInformation.Model;
        _machineTypeText.Text = string.IsNullOrWhiteSpace(machineInformation.MachineType) ? Resource.Unnamed : machineInformation.MachineType;

        if (recommendedPack is null || isBasicMode)
        {
            _summaryText.Text = T(
                "DeviceSetupWindow_BasicModeSummary",
                "This device will start in basic mode. Hardware-specific controls are hidden until a compatible device pack is available.");
            _packText.Text = T("DeviceSetupWindow_BasicModePack", "Device pack: Basic mode");
        }
        else
        {
            _summaryText.Text = T(
                "DeviceSetupWindow_MatchingPackSummary",
                "Universal Device Toolkit detected a matching device pack. Confirm it now so hardware-specific features can be prepared by the app.");
            _packText.Text = string.Format(
                T("DeviceSetupWindow_DevicePackFormat", "Device pack: {0}"),
                recommendedPack.DisplayName);
        }
    }

    private void DeviceSetupWindow_OnClosed(object? sender, EventArgs e) =>
        _taskCompletionSource.TrySetResult(DeviceSetupResult.Deferred);

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isPreparing)
                return;

            _isPreparing = true;
            _confirmButton.IsEnabled = false;
            _skipButton.IsEnabled = false;
            _statusText.Text = T("DeviceSetupWindow_Preparing", "Preparing device setup...");
            _statusText.Visibility = Visibility.Visible;

            await Task.Yield();

            _taskCompletionSource.TrySetResult(new DeviceSetupResult(true, _recommendedPack?.Id, this));
        }
        catch (Exception) { /* Logging excluded — no Log access in this scope */ }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        if (_isPreparing)
            return;

        _taskCompletionSource.TrySetResult(DeviceSetupResult.Deferred);
        Close();
    }

    public void SetInstalling(string message)
    {
        _statusText.Text = message;
        _statusText.Visibility = Visibility.Visible;
    }

    public void CompleteAndClose()
    {
        Close();
    }

    public void SetFailed(string message)
    {
        _isPreparing = false;
        _confirmButton.IsEnabled = true;
        _skipButton.IsEnabled = true;
        _statusText.Text = message;
        _statusText.Visibility = Visibility.Visible;
        Show();
    }
}

public readonly record struct DeviceSetupResult(bool Confirmed, string? DevicePackId, DeviceSetupWindow? Window)
{
    public static DeviceSetupResult Deferred => new(false, null, null);
}
