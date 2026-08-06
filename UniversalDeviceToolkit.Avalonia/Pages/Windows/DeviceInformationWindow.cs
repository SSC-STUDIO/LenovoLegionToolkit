using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;

#if WINDOWS
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Utils;
#endif

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Host-neutral device information dialog. Windows fills the same hardware and
/// warranty fields as the WPF dialog; portable hosts explain that the provider
/// is unavailable instead of exposing an empty or misleading surface.
/// </summary>
public sealed class DeviceInformationWindow : Window
{
    private readonly StackPanel _content = new() { Spacing = 12 };
    private readonly StackPanel _hardwareRows = new() { Spacing = 0 };
    private readonly StackPanel _warrantyRows = new() { Spacing = 0 };
    private readonly Dictionary<string, LocalizedTextBlock> _valueBlocks = new(StringComparer.Ordinal);
    private readonly Border _hardwareCard;
    private readonly Border _warrantyCard;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private bool _loaded;

    public DeviceInformationWindow()
    {
        Title = Get("DeviceInformationWindow_Title", "Device information");
        Width = 600;
        MinWidth = 460;
        MaxWidth = 720;
        MinHeight = 320;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        AutomationProperties.SetAutomationId(this, "AvaloniaDeviceInformationWindow");
        AutomationProperties.SetName(this, Title);

        var generalRows = new StackPanel { Spacing = 0 };
        generalRows.Children.Add(CreateValueRow("DeviceInformationWindow_Manufacturer_Title", "Manufacturer"));
        generalRows.Children.Add(CreateValueRow("DeviceInformationWindow_Model_Title", "Model"));
        generalRows.Children.Add(CreateValueRow("DeviceInformationWindow_MachineType_Title", "Machine type"));
        generalRows.Children.Add(CreateValueRow("DeviceInformationWindow_SerialNumber_Title", "Serial number"));
        generalRows.Children.Add(CreateValueRow("DeviceInformationWindow_BiosVersion_Title", "BIOS version"));

        _content.Children.Add(CreateSection(
            Get("DeviceInformationWindow_Device_Title", "Device"),
            generalRows));

        _hardwareCard = CreateSection(
            Get("DeviceInformationWindow_Hardware_Title", "Hardware"),
            _hardwareRows);
        _hardwareCard.IsVisible = false;
        _content.Children.Add(_hardwareCard);

        _warrantyCard = CreateWarrantySection();
        _warrantyCard.IsVisible = false;
        _content.Children.Add(_warrantyCard);
        _content.Children.Add(_status);

        Content = new ScrollViewer
        {
            Margin = new Thickness(16, 8, 16, 16),
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = _content,
        };

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;
        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task RefreshAsync()
    {
#if WINDOWS
        try
        {
            var machine = await Compatibility.GetMachineInformationAsync().ConfigureAwait(true);
            SetGeneralValues(machine);
            SetHardwareValues(machine.Hardware);
            await SetWarrantyValuesAsync(machine).ConfigureAwait(true);
            _status.Text = string.Empty;
            return;
        }
        catch (Exception ex)
        {
            _status.Text = Get("CompatibilityCheckError_Message", "Device information could not be read.");
            _status.Foreground = GetBrush("StatusWarningBrush", Colors.OrangeRed);
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Avalonia device information refresh failed.", ex);
        }
#endif

        _hardwareCard.IsVisible = false;
        _warrantyCard.IsVisible = false;
#if !WINDOWS
        _status.Text = Get(
            "Settings_Page_UnavailableReason",
            "Device information is unavailable on this platform.");
        _status.Foreground = GetBrush("TextFillColorSecondaryBrush", Colors.Gray);
#endif
    }

#if WINDOWS
    private void SetGeneralValues(MachineInformation machine)
    {
        SetValue("DeviceInformationWindow_Manufacturer_Title", machine.Vendor);
        SetValue("DeviceInformationWindow_Model_Title", machine.Model);
        SetValue("DeviceInformationWindow_MachineType_Title", machine.MachineType);
        SetValue("DeviceInformationWindow_SerialNumber_Title", machine.SerialNumber);
        SetValue("DeviceInformationWindow_BiosVersion_Title", machine.BiosVersionRaw);
    }

    private void SetHardwareValues(HardwareInventory? hardware)
    {
        _hardwareRows.Children.Clear();
        hardware ??= HardwareInventory.Empty;

        AddHardwareRow(
            "FanCurveControl_CPU",
            "CPU",
            string.Join(Environment.NewLine, hardware.Processors.Select(FormatProcessor).Where(IsPresent).Distinct()));
        AddHardwareRow(
            "FanCurveControl_GPU",
            "GPU",
            string.Join(Environment.NewLine, hardware.VideoControllers.Select(FormatVideoController).Where(IsPresent).Distinct()));
        AddHardwareRow(
            "DeviceInformationWindow_Memory_Title",
            "Memory",
            FormatMemory(hardware.Memory));
        AddHardwareRow(
            "DeviceInformationWindow_BaseBoard_Title",
            "Base board",
            FormatBaseBoard(hardware.BaseBoard));
        AddHardwareRow(
            "DeviceInformationWindow_Chassis_Title",
            "Chassis",
            FormatChassis(hardware.Chassis));

        _hardwareCard.IsVisible = _hardwareRows.Children.Count > 0;
    }

    private async Task SetWarrantyValuesAsync(MachineInformation machine)
    {
        _warrantyRows.Children.Clear();
        var checker = IoCContainer.Resolve<WarrantyChecker>();
        var warranty = await checker.GetWarrantyInfo(machine).ConfigureAwait(true);
        if (!warranty.HasValue)
            return;

        AddStaticValueRow(
            "DeviceInformationWindow_WarrantyStartDate_Title",
            "Warranty start",
            FormatDate(warranty.Value.Start));
        AddStaticValueRow(
            "DeviceInformationWindow_WarrantyEndDate_Title",
            "Warranty end",
            FormatDate(warranty.Value.End));

        var link = warranty.Value.Link ?? BuildLenovoSupportUri(machine.SerialNumber, machine.MachineType);
        var support = new Button
        {
            Content = Get("DeviceInformationWindow_LenovoSupport", "Lenovo support"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 8),
            Tag = link,
        };
        AutomationProperties.SetAutomationId(support, "AvaloniaDeviceInformationWarrantyLink");
        AutomationProperties.SetName(support, support.Content?.ToString() ?? "Lenovo support");
        ToolTip.SetTip(support, support.Content);
        support.Click += (_, _) => OpenSupportLink((Uri?)support.Tag);
        _warrantyRows.Children.Add(support);
        _warrantyCard.IsVisible = true;
    }

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

    private static string FormatVideoController(VideoControllerHardware controller)
    {
        if (string.IsNullOrWhiteSpace(controller.Name))
            return string.Empty;

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(controller.AdapterCompatibility))
            details.Add(controller.AdapterCompatibility);
        if (controller.AdapterRamBytes is > 0)
            details.Add(FormatCapacity(controller.AdapterRamBytes.Value));
        return details.Count == 0 ? controller.Name : $"{controller.Name} ({string.Join(", ", details.Distinct())})";
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

    private static string FormatBaseBoard(BaseBoardHardware baseBoard) =>
        baseBoard.HasAnySignal
            ? string.Join(" ", new[] { baseBoard.Manufacturer, baseBoard.Product, baseBoard.Version }.Where(IsPresent))
            : string.Empty;

    private static string FormatChassis(ChassisHardware chassis) =>
        chassis.HasAnySignal
            ? string.Join(", ", new[] { chassis.Manufacturer }.Where(IsPresent).Concat(chassis.ChassisTypeNames).Distinct())
            : string.Empty;

    private static string FormatCapacity(ulong bytes) => $"{bytes / (1024d * 1024d * 1024d):0.#} GiB";

    private static string FormatDate(DateTime? value) =>
        value.HasValue && value.Value != DateTime.MinValue
            ? value.Value.ToString("d", LocalizationRuntime.CurrentCulture)
            : "-";

    private void AddHardwareRow(string key, string fallback, string value)
    {
        if (!IsPresent(value))
            return;
        AddStaticValueRow(key, fallback, value);
    }

    private void OpenSupportLink(Uri? link)
    {
        if (link is null)
        {
            _status.Text = Get(
                "DeviceInformationWindow_WarrantyLinkUnavailable",
                "Warranty support link is unavailable.");
            _status.Foreground = GetBrush("StatusWarningBrush", Colors.OrangeRed);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(link.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _status.Text = Get(
                "DeviceInformationWindow_WarrantyLinkUnavailable",
                "Warranty support link is unavailable.");
            _status.Foreground = GetBrush("StatusWarningBrush", Colors.OrangeRed);
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to open warranty support link.", ex);
        }
    }

    private static Uri BuildLenovoSupportUri(string? serialNumber, string? machineType)
    {
        var query = new List<string>();
        if (IsPresent(serialNumber))
            query.Add($"serialNumber={Uri.EscapeDataString(serialNumber!.Trim())}");
        if (IsPresent(machineType))
            query.Add($"machineType={Uri.EscapeDataString(machineType!.Trim())}");
        return new Uri("https://pcsupport.lenovo.com/warrantylookup" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty));
    }
#endif

    private Border CreateWarrantySection()
    {
        var header = new LocalizedTextBlock
        {
            Text = Get("DeviceInformationWindow_Warranty_Title", "Warranty"),
            FontSize = 16,
            FontWeight = FontWeight.Medium,
            Foreground = GetBrush("TextFillColorPrimaryBrush", Colors.White),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        return new Border
        {
            Background = GetBrush("CardBackgroundBrush", Colors.Transparent),
            BorderBrush = GetBrush("CardBorderBrush", Colors.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = GetCornerRadius("CornerRadiusCard"),
            Padding = new Thickness(14),
            Child = new StackPanel { Spacing = 8, Children = { header, _warrantyRows } },
        };
    }

    private Border CreateSection(string title, Control rows)
    {
        var header = new LocalizedTextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeight.Medium,
            Foreground = GetBrush("TextFillColorPrimaryBrush", Colors.White),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        return new Border
        {
            Background = GetBrush("CardBackgroundBrush", Colors.Transparent),
            BorderBrush = GetBrush("CardBorderBrush", Colors.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = GetCornerRadius("CornerRadiusCard"),
            Padding = new Thickness(0),
            Child = new StackPanel { Spacing = 0, Children = { header, rows } },
        };
    }

    private Button CreateValueRow(string key, string fallback)
    {
        var row = new Button
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 9),
            Tag = new ValueRow(key, fallback),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,1.75*"), ColumnSpacing = 12 };
        var label = new LocalizedTextBlock
        {
            Text = Get(key, fallback),
            Foreground = GetBrush("TextFillColorSecondaryBrush", Colors.Gray),
            VerticalAlignment = VerticalAlignment.Center,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var value = new LocalizedTextBlock
        {
            Text = "-",
            TextAlignment = TextAlignment.Right,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Foreground = GetBrush("TextFillColorPrimaryBrush", Colors.White),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
            MinWidth = 0,
        };
        Grid.SetColumn(value, 1);
        grid.Children.Add(label);
        grid.Children.Add(value);
        row.Content = grid;
        _valueBlocks[key] = value;
        AutomationProperties.SetName(row, label.Text ?? fallback);
        ToolTip.SetTip(row, label.Text);
        row.Click += (_, _) => _ = CopyValueAsync(value.Text);
        return row;
    }

    private void AddStaticValueRow(string key, string fallback, string value)
    {
        var row = CreateValueRow(key, fallback);
        _valueBlocks[key].Text = value;
        _warrantyRows.Children.Add(row);
    }

    private void SetValue(string key, string? value)
    {
        if (_valueBlocks.TryGetValue(key, out var valueBlock))
            valueBlock.Text = IsPresent(value) ? value : "-";
    }

    private async Task CopyValueAsync(string? value)
    {
        if (!IsPresent(value) || value == "-")
            return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;
        await clipboard.SetTextAsync(value).ConfigureAwait(true);
        _status.Text = Get("CopiedToClipboard_Title", "Copied to clipboard");
        _status.Foreground = GetBrush("StatusSuccessBrush", Colors.SeaGreen);
    }

    private IBrush GetBrush(string key, Color fallback)
    {
        return this.TryFindResource(key, out var resource) && resource is IBrush brush
            ? brush
            : new SolidColorBrush(fallback);
    }

    private CornerRadius GetCornerRadius(string key) =>
        this.TryFindResource(key, out var resource) && resource is CornerRadius radius
            ? radius
            : new CornerRadius(8);

    private static bool IsPresent(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private sealed record ValueRow(string Key, string Fallback);
}
