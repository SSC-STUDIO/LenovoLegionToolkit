using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils
{
public partial class DeviceInformationWindow : BaseWindow
{
    private readonly WarrantyChecker _warrantyChecker = IoCContainer.Resolve<WarrantyChecker>();
    private readonly Snackbar _snackBar;

    public static readonly StyledProperty<bool> HasHardwareInfoProperty =
        AvaloniaProperty.Register<DeviceInformationWindow, bool>(nameof(HasHardwareInfo), false);

    public static readonly StyledProperty<bool> HasWarrantyInfoProperty =
        AvaloniaProperty.Register<DeviceInformationWindow, bool>(nameof(HasWarrantyInfo), false);

    public bool HasHardwareInfo
    {
        get => GetValue(HasHardwareInfoProperty);
        set => SetValue(HasHardwareInfoProperty, value);
    }

    public bool HasWarrantyInfo
    {
        get => GetValue(HasWarrantyInfoProperty);
        set => SetValue(HasWarrantyInfoProperty, value);
    }

    public DeviceInformationWindow()
    {
        InitializeComponent();
        _snackBar = NotificationToastFactory.Create(_snackBarPresenter, HorizontalAlignment.Center);
        _snackBar.Timeout = TimeSpan.FromSeconds(1);
    }

    private async void DeviceInformationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(DeviceInformationWindow_Loaded)}.", ex);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // AVALONIA: WPF SystemParameters.WorkArea replaced by Screens work area.
        var workAreaHeight = Screens.ScreenFromWindow(this)?.WorkingArea.Height ?? Screens.Primary.WorkingArea.Height;
        _contentScrollViewer.MaxHeight = Math.Max(320, workAreaHeight - 96);
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
            HasHardwareInfo = false;

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
            HasWarrantyInfo = false;

            var warrantyInfo = await _warrantyChecker.GetWarrantyInfo(mi, forceRefresh);

            if (!warrantyInfo.HasValue)
                return;

            _warrantyStartLabel.Text = warrantyInfo.Value.Start is not null ? warrantyInfo.Value.Start?.ToString(LocalizationHelper.ShortDateFormat) : "-";
            _warrantyEndLabel.Text = warrantyInfo.Value.End is not null ? warrantyInfo.Value.End?.ToString(LocalizationHelper.ShortDateFormat) : "-";
            // Prefer product-specific page; fall back so the action card is always clickable.
            _warrantyLinkCardAction.Tag = warrantyInfo.Value.Link
                ?? BuildLenovoSupportUri(mi.SerialNumber, mi.MachineType);
            HasWarrantyInfo = true;
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

    private async void RefreshWarrantyButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshAsync(true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(RefreshWarrantyButton_OnClick)}.", ex);
        }
    }

    private void SetHardwareInformation(HardwareInventory? hardware)
    {
        hardware ??= HardwareInventory.Empty;

        var anyVisible = false;
        anyVisible |= SetRowText(_cpuRow, _cpuLabel, FormatProcessors(hardware.Processors));
        anyVisible |= SetRowText(_gpuRow, _gpuLabel, FormatVideoControllers(hardware.VideoControllers));
        anyVisible |= SetRowText(_memoryRow, _memoryLabel, FormatMemory(hardware.Memory));
        anyVisible |= SetRowText(_baseBoardRow, _baseBoardLabel, FormatBaseBoard(hardware.BaseBoard));
        anyVisible |= SetRowText(_chassisRow, _chassisLabel, FormatChassis(hardware.Chassis));

        HasHardwareInfo = anyVisible;
    }

    private static bool SetRowText(Control row, TextBlock label, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            row.IsVisible = false;
            label.Text = "-";
            return false;
        }

        row.IsVisible = true;
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

    private async void DeviceInfoRow_Click(object sender, PointerReleasedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (sender is not Border { Child: Grid grid })
            return;

        TextBlock? valueText = null;
        foreach (var child in grid.Children)
        {
            if (child is TextBlock textBlock && Grid.GetColumn(textBlock) == 1)
            {
                valueText = textBlock;
                break;
            }
        }

        if (valueText?.Text is not { } str || string.IsNullOrWhiteSpace(str) || str == "-")
            return;

        try
        {
            if (UdtAppContext.Clipboard is { } clipboard)
                await clipboard.SetTextAsync(str);
            await ShowSnackBarAsync(Resource.CopiedToClipboard_Title, string.Format(Resource.CopiedToClipboard_Message_WithParam, str));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't copy to clipboard", ex);
        }
    }

    private async void WarrantyLinkCardAction_OnClick(object sender, PointerReleasedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Handled = true;
        await OpenWarrantySupportLinkAsync();
    }

    private async Task OpenWarrantySupportLinkAsync()
    {
        try
        {
            var link = _warrantyLinkCardAction.Tag as Uri
                ?? BuildLenovoSupportUri(_serialNumberLabel.Text, _mtmLabel.Text);

            if (link is null)
            {
                await ShowSnackBarAsync(
                    Resource.DeviceInformationWindow_Warranty_Title,
                    Resource.DeviceInformationWindow_WarrantyLinkUnavailable);
                return;
            }

            link.Open();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to open Lenovo support link.", ex);

            try
            {
                await ShowSnackBarAsync(
                    Resource.DeviceInformationWindow_Warranty_Title,
                    Resource.DeviceInformationWindow_WarrantyLinkUnavailable);
            }
            catch (Exception snackEx)
            {
                Log.Instance.TraceOnce(
                    "device-info-snackbar",
                    "Failed to show warranty snackbar; keeping device information window alive.",
                    snackEx);
            }
        }
    }

    /// <summary>
    /// Builds a public Lenovo PC Support URL when the product-id deep link is unavailable.
    /// </summary>
    private static Uri? BuildLenovoSupportUri(string? serialNumber, string? machineType)
    {
        // Generic warranty lookup is always valid HTTPS; serial/MTM refine the query when present.
        var serial = string.IsNullOrWhiteSpace(serialNumber) || serialNumber == "-"
            ? null
            : serialNumber.Trim();
        var mtm = string.IsNullOrWhiteSpace(machineType) || machineType == "-"
            ? null
            : machineType.Trim();

        if (serial is not null && mtm is not null)
            return new Uri($"https://pcsupport.lenovo.com/warrantylookup?serialNumber={Uri.EscapeDataString(serial)}&machineType={Uri.EscapeDataString(mtm)}");

        if (serial is not null)
            return new Uri($"https://pcsupport.lenovo.com/warrantylookup?serialNumber={Uri.EscapeDataString(serial)}");

        return new Uri("https://pcsupport.lenovo.com/");
    }

    private async Task ShowSnackBarAsync(string title, string? message)
    {
        _snackBar.Title = title;
        _snackBar.Content = message;
        await _snackBar.ShowAsync();
    }
}
}
