using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class DeviceInformationWindow : INotifyPropertyChanged
{
    private readonly WarrantyChecker _warrantyChecker = IoCContainer.Resolve<WarrantyChecker>();
    private readonly Snackbar _snackBar;
    private bool _isWarrantyLinkAvailable;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsWarrantyLinkAvailable
    {
        get => _isWarrantyLinkAvailable;
        private set
        {
            if (_isWarrantyLinkAvailable == value)
                return;

            _isWarrantyLinkAvailable = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWarrantyLinkAvailable)));
        }
    }

    public DeviceInformationWindow()
    {
        InitializeComponent();
        DataContext = this;
        _snackBar = new Snackbar(_snackBarPresenter)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            IsCloseButtonEnabled = false,
            Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 },
            Timeout = TimeSpan.FromSeconds(1)
        };
    }

    private async void DeviceInformationWindow_Loaded(object sender, RoutedEventArgs e) => await RefreshAsync();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _contentScrollViewer.MaxHeight = Math.Max(320, SystemParameters.WorkArea.Height - 96);
    }

    private async Task RefreshAsync(bool forceRefresh = false)
    {
        MachineInformation mi;

        try
        {
            mi = await MachineCompatibility.GetMachineInformationAsync();

            _manufacturerLabel.Text = mi.Vendor ?? "-";
            _modelLabel.Text = mi.Model ?? "-";
            _mtmLabel.Text = mi.MachineType ?? "-";
            _serialNumberLabel.Text = mi.SerialNumber ?? "-";
            _biosLabel.Text = mi.BiosVersionRaw ?? "-";
            SetHardwareInformation(mi.Hardware);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read device information: {ex.Message}", ex);

            // Display error message to user
            _manufacturerLabel.Text = Resource.CompatibilityCheckError_Message;
            _modelLabel.Text = "-";
            _mtmLabel.Text = "-";
            _serialNumberLabel.Text = "-";
            _biosLabel.Text = "-";
            _hardwareInfo.Visibility = Visibility.Collapsed;

            // Show error notification
            _snackBar.Icon = new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24 };
            _snackBar.Appearance = ControlAppearance.Danger;
            await ShowSnackBarAsync(
                Resource.CompatibilityCheckErrorWindow_Title,
                Resource.CompatibilityCheckError_Message);

            return;
        }

        try
        {
            _refreshWarrantyButton.IsEnabled = false;

            _warrantyStartLabel.Text = "-";
            _warrantyEndLabel.Text = "-";
            _warrantyLinkCardAction.Tag = null;
            IsWarrantyLinkAvailable = false;

            var warrantyInfo = await _warrantyChecker.GetWarrantyInfo(mi, forceRefresh);

            if (!warrantyInfo.HasValue)
                return;

            _warrantyStartLabel.Text = warrantyInfo.Value.Start is not null ? warrantyInfo.Value.Start?.ToString(LocalizationHelper.ShortDateFormat) : "-";
            _warrantyEndLabel.Text = warrantyInfo.Value.End is not null ? warrantyInfo.Value.End?.ToString(LocalizationHelper.ShortDateFormat) : "-";
            _warrantyLinkCardAction.Tag = warrantyInfo.Value.Link;
            IsWarrantyLinkAvailable = true;
            _warrantyInfo.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't load warranty info.", ex);
        }
        finally
        {
            _refreshWarrantyButton.IsEnabled = true;
        }
    }

    private async void RefreshWarrantyButton_OnClick(object sender, RoutedEventArgs e) => await RefreshAsync(true);

    private void SetHardwareInformation(HardwareInventory? hardware)
    {
        hardware ??= HardwareInventory.Empty;

        var anyVisible = false;
        anyVisible |= SetCardText(_cpuCard, _cpuLabel, FormatProcessors(hardware.Processors));
        anyVisible |= SetCardText(_gpuCard, _gpuLabel, FormatVideoControllers(hardware.VideoControllers));
        anyVisible |= SetCardText(_memoryCard, _memoryLabel, FormatMemory(hardware.Memory));
        anyVisible |= SetCardText(_baseBoardCard, _baseBoardLabel, FormatBaseBoard(hardware.BaseBoard));
        anyVisible |= SetCardText(_chassisCard, _chassisLabel, FormatChassis(hardware.Chassis));

        _hardwareInfo.Visibility = anyVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool SetCardText(UIElement card, TextBlock label, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            card.Visibility = Visibility.Collapsed;
            label.Text = "-";
            return false;
        }

        card.Visibility = Visibility.Visible;
        label.Text = text;
        return true;
    }

    private static string FormatProcessors(IReadOnlyCollection<ProcessorHardware> processors) =>
        string.Join(Environment.NewLine, processors.Select(FormatProcessor).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());

    private static string FormatProcessor(ProcessorHardware processor)
    {
        if (string.IsNullOrWhiteSpace(processor.Name))
            return string.Empty;

        var details = new List<string>();
        if (processor.NumberOfCores.HasValue && processor.NumberOfLogicalProcessors.HasValue)
            details.Add($"{processor.NumberOfCores}C/{processor.NumberOfLogicalProcessors}T");
        if (processor.MaxClockSpeedMHz.HasValue)
            details.Add($"{processor.MaxClockSpeedMHz} MHz");

        return details.Count == 0 ? processor.Name : $"{processor.Name} ({string.Join(", ", details)})";
    }

    private static string FormatVideoControllers(IReadOnlyCollection<VideoControllerHardware> videoControllers) =>
        string.Join(Environment.NewLine, videoControllers.Select(FormatVideoController).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());

    private static string FormatVideoController(VideoControllerHardware videoController)
    {
        if (string.IsNullOrWhiteSpace(videoController.Name))
            return string.Empty;

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(videoController.AdapterCompatibility))
            details.Add(videoController.AdapterCompatibility);
        if (videoController.AdapterRamBytes is > 0)
            details.Add(FormatCapacity(videoController.AdapterRamBytes.Value));

        return details.Count == 0 ? videoController.Name : $"{videoController.Name} ({string.Join(", ", details.Distinct())})";
    }

    private static string FormatMemory(MemoryHardware memory)
    {
        if (!memory.HasAnySignal)
            return string.Empty;

        var details = new List<string>();
        if (memory.TotalCapacityBytes > 0)
            details.Add(FormatCapacity(memory.TotalCapacityBytes));
        if (memory.ModuleCount > 0)
            details.Add($"{memory.ModuleCount} module{(memory.ModuleCount == 1 ? string.Empty : "s")}");
        if (memory.ConfiguredClockSpeedMHz.HasValue)
            details.Add($"{memory.ConfiguredClockSpeedMHz} MHz");
        else if (memory.SpeedMHz.HasValue)
            details.Add($"{memory.SpeedMHz} MHz");

        return string.Join(", ", details);
    }

    private static string FormatBaseBoard(BaseBoardHardware baseBoard)
    {
        if (!baseBoard.HasAnySignal)
            return string.Empty;

        return string.Join(" ", new[]
        {
            baseBoard.Manufacturer,
            baseBoard.Product,
            baseBoard.Version
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string FormatChassis(ChassisHardware chassis)
    {
        if (!chassis.HasAnySignal)
            return string.Empty;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(chassis.Manufacturer))
            parts.Add(chassis.Manufacturer);
        parts.AddRange(chassis.ChassisTypeNames);

        return string.Join(", ", parts.Distinct());
    }

    private static string FormatCapacity(ulong bytes)
    {
        const double gibibyte = 1024d * 1024d * 1024d;
        return $"{bytes / gibibyte:0.#} GiB";
    }

    private async void DeviceCardControl_Click(object sender, RoutedEventArgs e)
    {
        if (((sender as CardControl)?.Content as TextBlock)?.Text is not { } str)
            return;

        try
        {
            System.Windows.Clipboard.SetText(str);
            await ShowSnackBarAsync(Resource.CopiedToClipboard_Title, string.Format(Resource.CopiedToClipboard_Message_WithParam, str));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't copy to clipboard", ex);
        }
    }

    private void WarrantyLinkCardAction_OnClick(object sender, RoutedEventArgs e)
    {
        var link = _warrantyLinkCardAction.Tag as Uri;
        link?.Open();
    }

    private async Task ShowSnackBarAsync(string title, string? message)
    {
        _snackBar.Title = title;
        _snackBar.Content = message;
        await _snackBar.ShowAsync();
    }
}
}
