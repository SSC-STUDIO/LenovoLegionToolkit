using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.DeviceSupport;
using LenovoLegionToolkit.Lib.Utils;
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

        // Title: device setup (not hybrid-mode "restart later").
        _titleText.Text = T("DeviceSetupWindow_Title", "Device setup");
        Title = _titleText.Text;

        _vendorText.Text = string.IsNullOrWhiteSpace(machineInformation.Vendor) ? Resource.Unnamed : machineInformation.Vendor;
        _modelText.Text = string.IsNullOrWhiteSpace(machineInformation.Model) ? Resource.Unnamed : machineInformation.Model;
        _machineTypeText.Text = string.IsNullOrWhiteSpace(machineInformation.MachineType) ? Resource.Unnamed : machineInformation.MachineType;

        if (recommendedPack is null || isBasicMode)
        {
            _summaryText.Text = T(
                "DeviceSetupWindow_BasicModeSummary",
                "This device will start in basic mode. Hardware-specific controls stay hidden until a matching device pack is available.");
            _packText.Text = T("DeviceSetupWindow_BasicModePack", "Device pack: Basic mode");
            _hintText.Text = T(
                "DeviceSetupWindow_BasicModeHint",
                "Confirm saves this choice and continues. Skip for now keeps basic mode and shows this once more next launch.");
        }
        else
        {
            _summaryText.Text = T(
                "DeviceSetupWindow_MatchingPackSummary",
                "Universal Device Toolkit found a matching device pack for this machine. Confirm so hardware features can use that pack.");
            _packText.Text = string.Format(
                T("DeviceSetupWindow_DevicePackFormat", "Device pack: {0}"),
                recommendedPack.DisplayName);
            _hintText.Text = T(
                "DeviceSetupWindow_MatchingPackHint",
                "Confirm applies this pack profile. Skip for now continues without confirming (you can finish later from settings). This does not restart Windows.");
        }

        // Explicit labels — never reuse RestartLater / RestartNow (those are for OS reboot prompts).
        _skipButton.Content = T("DeviceSetupWindow_SkipButton", "Skip for now");
        _confirmButton.Content = T("DeviceSetupWindow_ConfirmButton", "Confirm");
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
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(Confirm_Click)}.", ex);
        }
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
