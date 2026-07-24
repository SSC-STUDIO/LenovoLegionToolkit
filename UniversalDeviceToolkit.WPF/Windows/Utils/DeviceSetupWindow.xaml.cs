using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UniversalDeviceToolkit.Lib.DeviceSupport;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Windows.Utils;

public partial class DeviceSetupWindow
{
    private readonly IReadOnlyList<DevicePackOption> _packOptions;
    private readonly TaskCompletionSource<DeviceSetupResult> _taskCompletionSource = new();
    private bool _isPreparing;
    private bool _suppressPackEvents;

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public Task<DeviceSetupResult> ShouldContinue => _taskCompletionSource.Task;

    public DeviceSetupWindow(
        MachineInformation machineInformation,
        DevicePack? recommendedPack,
        bool isBasicMode,
        IReadOnlyList<DevicePack>? selectablePacks = null)
    {
        InitializeComponent();

        _titleText.Text = T("DeviceSetupWindow_Title", "Device setup");
        Title = _titleText.Text;

        _vendorText.Text = string.IsNullOrWhiteSpace(machineInformation.Vendor) ? Resource.Unnamed : machineInformation.Vendor;
        _modelText.Text = string.IsNullOrWhiteSpace(machineInformation.Model) ? Resource.Unnamed : machineInformation.Model;
        _machineTypeText.Text = string.IsNullOrWhiteSpace(machineInformation.MachineType) ? Resource.Unnamed : machineInformation.MachineType;

        _packOptions = BuildPackOptions(recommendedPack, selectablePacks, isBasicMode);
        PopulatePackCombo(recommendedPack);

        _packLabelText.Text = T("DeviceSetupWindow_SelectPackLabel", "Device profile (pack)");
        _summaryText.Text = recommendedPack is null || isBasicMode
            ? T(
                "DeviceSetupWindow_BasicModeSummary",
                "This device will start in basic mode. Hardware-specific controls stay hidden until a matching device pack is available. You can still pick a profile below if you know your model family.")
            : T(
                "DeviceSetupWindow_MatchingPackSummary",
                "Universal Device Toolkit found a matching device pack for this machine. Confirm to apply it, or choose another profile if the auto-detect is wrong.");

        _hintText.Text = T(
            "DeviceSetupWindow_MatchingPackHint",
            "Confirm saves this profile and continues. Skip for now keeps the default and may ask again next launch. This does not restart Windows.");

        _skipButton.Content = T("DeviceSetupWindow_SkipButton", "Skip for now");
        _confirmButton.Content = T("DeviceSetupWindow_ConfirmButton", "Confirm");

        UpdatePackDetail();
    }

    private static IReadOnlyList<DevicePackOption> BuildPackOptions(
        DevicePack? recommendedPack,
        IReadOnlyList<DevicePack>? selectablePacks,
        bool isBasicMode)
    {
        var options = new List<DevicePackOption>();

        // Always offer basic mode first as a safe escape hatch.
        options.Add(new DevicePackOption(
            CatalogDeviceSupportProvider.GenericBasicPackId,
            T("DeviceSetupWindow_BasicModePackName", "Basic mode (plugins & optimization only)"),
            isHardware: false,
            isRecommended: recommendedPack is null || isBasicMode));

        var packs = (selectablePacks ?? [])
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Id))
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        // Hardware packs first (Lenovo gaming etc.), then brand basic packs.
        var ordered = packs
            .OrderByDescending(IsHardwarePack)
            .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var pack in ordered)
        {
            if (pack.Id.Equals(CatalogDeviceSupportProvider.GenericBasicPackId, StringComparison.OrdinalIgnoreCase))
                continue;

            var isRecommended = recommendedPack is not null &&
                                pack.Id.Equals(recommendedPack.Id, StringComparison.OrdinalIgnoreCase);
            var hardware = IsHardwarePack(pack);
            var label = isRecommended
                ? string.Format(
                    T("DeviceSetupWindow_RecommendedPackFormat", "{0} (recommended)"),
                    pack.DisplayName)
                : pack.DisplayName;
            if (hardware)
                label = string.Format(T("DeviceSetupWindow_HardwarePackFormat", "{0} — full hardware"), label);
            else
                label = string.Format(T("DeviceSetupWindow_BasicPackFormat", "{0} — basic"), label);

            options.Add(new DevicePackOption(pack.Id, label, hardware, isRecommended));
        }

        // Ensure recommended pack is present even if catalog list was empty/partial.
        if (recommendedPack is not null &&
            options.All(o => !o.Id.Equals(recommendedPack.Id, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(1, new DevicePackOption(
                recommendedPack.Id,
                string.Format(
                    T("DeviceSetupWindow_RecommendedPackFormat", "{0} (recommended)"),
                    recommendedPack.DisplayName),
                IsHardwarePack(recommendedPack),
                isRecommended: true));
        }

        return options;
    }

    private static bool IsHardwarePack(DevicePack pack) =>
        pack.EnabledFeatures.Any(f =>
            f.Equals("lenovo-hardware-controls", StringComparison.OrdinalIgnoreCase));

    private void PopulatePackCombo(DevicePack? recommendedPack)
    {
        _suppressPackEvents = true;
        try
        {
            _packComboBox.Items.Clear();
            foreach (var option in _packOptions)
                _packComboBox.Items.Add(option);

            var preferred = _packOptions.FirstOrDefault(o => o.IsRecommended)
                            ?? _packOptions.FirstOrDefault();
            if (preferred is not null)
                _packComboBox.SelectedItem = preferred;
            else if (_packComboBox.Items.Count > 0)
                _packComboBox.SelectedIndex = 0;
        }
        finally
        {
            _suppressPackEvents = false;
        }
    }

    private void PackComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressPackEvents)
            return;
        UpdatePackDetail();
    }

    private void UpdatePackDetail()
    {
        if (_packComboBox.SelectedItem is not DevicePackOption option)
        {
            _packDetailText.Text = string.Empty;
            return;
        }

        _packDetailText.Text = option.IsHardware
            ? T(
                "DeviceSetupWindow_HardwarePackDetail",
                "Full hardware: power modes, sensors, fans, and Lenovo controls when the firmware exposes them.")
            : T(
                "DeviceSetupWindow_BasicPackDetail",
                "Basic profile: plugins, system optimization, language, and theme. Hardware controls stay hidden.");
    }

    private DevicePackOption? GetSelectedOption() =>
        _packComboBox.SelectedItem as DevicePackOption ?? _packOptions.FirstOrDefault();

    private void DeviceSetupWindow_OnClosed(object? sender, EventArgs e) =>
        _taskCompletionSource.TrySetResult(DeviceSetupResult.Deferred);

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isPreparing || _taskCompletionSource.Task.IsCompleted)
                return;

            _isPreparing = true;
            _confirmButton.IsEnabled = false;
            _skipButton.IsEnabled = false;
            _packComboBox.IsEnabled = false;
            _statusText.Text = T("DeviceSetupWindow_Preparing", "Preparing device setup...");
            _statusText.Visibility = Visibility.Visible;

            await Task.Yield();

            var selected = GetSelectedOption();
            var packId = selected?.Id;
            var isBasic = selected is null || !selected.IsHardware;
            _taskCompletionSource.TrySetResult(new DeviceSetupResult(true, packId, isBasic, this));
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
        _packComboBox.IsEnabled = true;
        _statusText.Text = message;
        _statusText.Visibility = Visibility.Visible;
        Show();
    }

    private sealed class DevicePackOption(string id, string label, bool isHardware, bool isRecommended)
    {
        public string Id { get; } = id;
        public string Label { get; } = label;
        public bool IsHardware { get; } = isHardware;
        public bool IsRecommended { get; } = isRecommended;
        public override string ToString() => Label;
    }
}

public readonly record struct DeviceSetupResult(
    bool Confirmed,
    string? DevicePackId,
    bool IsBasicMode,
    DeviceSetupWindow? Window)
{
    public static DeviceSetupResult Deferred => new(false, null, true, null);
}
