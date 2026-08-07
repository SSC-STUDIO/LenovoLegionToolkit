#if WINDOWS

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

internal enum AvaloniaOsdStyle
{
    Panel,
    Bar,
}

/// <summary>
/// A non-activating, topmost sensor overlay backed by the same OSD settings as
/// the WPF implementation. Rendering uses host-neutral sensor descriptors so it
/// remains functional when vendor-specific sensors are unavailable.
/// </summary>
internal sealed class AvaloniaOsdOverlayWindow : Window
{
    private readonly IPlatformServices _platformServices;
    private readonly OsdSettings _settings;
    private readonly StackPanel _measurements = new();
    private readonly DispatcherTimer _refreshTimer = new();
    private bool _isRefreshing;
    private bool _positionRestored;

    public AvaloniaOsdOverlayWindow(
        IPlatformServices platformServices,
        OsdSettings settings,
        AvaloniaOsdStyle style)
    {
        _platformServices = platformServices;
        _settings = settings;
        Style = style;

        ShowInTaskbar = false;
        Topmost = true;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Width = style == AvaloniaOsdStyle.Bar ? 760 : 260;
        MinWidth = style == AvaloniaOsdStyle.Bar ? 460 : 220;
        SizeToContent = style == AvaloniaOsdStyle.Bar ? SizeToContent.Height : SizeToContent.Height;
        _measurements.Orientation = style == AvaloniaOsdStyle.Bar
            ? Orientation.Horizontal
            : Orientation.Vertical;
        _measurements.Spacing = style == AvaloniaOsdStyle.Bar ? 14 : 5;

        Content = new Border
        {
            Child = _measurements,
        };

        Opened += OnOpened;
        Closed += OnClosed;
        PointerPressed += OnPointerPressed;
        PropertyChanged += OnWindowPropertyChanged;
        _refreshTimer.Tick += OnRefreshTimerTick;
        ApplySettings();
    }

    public AvaloniaOsdStyle Style { get; }

    public void ApplySettings()
    {
        var store = _settings.Store;
        if (Content is Border border)
        {
            border.Padding = new Thickness(12);
            border.CornerRadius = new CornerRadius(
                store.CornerRadiusTop,
                store.CornerRadiusTop,
                store.CornerRadiusBottom,
                store.CornerRadiusBottom);
            border.Background = CreateBrush(store.BackgroundColor, store.BackgroundOpacity);
        }

        _refreshTimer.Interval = TimeSpan.FromSeconds(Math.Clamp(store.OsdRefreshInterval, 0.1, 10));
        RestorePosition();
    }

    public void Refresh() => _ = RefreshAsync();

    private void OnOpened(object? sender, EventArgs args)
    {
        ApplySettings();
        _refreshTimer.Start();
        Refresh();
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
        PropertyChanged -= OnWindowPropertyChanged;
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs args) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_isRefreshing || !IsVisible)
            return;

        _isRefreshing = true;
        try
        {
            var readings = await _platformServices.GetSensorReadingsAsync().ConfigureAwait(true);
            RenderMeasurements(readings);
        }
        catch
        {
            // The overlay is optional. Keep the last valid frame when a sensor poll fails.
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void RenderMeasurements(IReadOnlyList<SensorReadingItem> readings)
    {
        _measurements.Children.Clear();
        var selectedItems = _settings.Store.Items;
        foreach (var item in selectedItems)
        {
            var reading = readings.FirstOrDefault(candidate => Matches(item, candidate));
            if (reading is null)
                continue;

            _measurements.Children.Add(CreateMeasurement(item, reading));
        }

        if (_measurements.Children.Count == 0)
        {
            _measurements.Children.Add(new LocalizedTextBlock
            {
                Text = "No selected sensor readings are currently available.",
                Foreground = CreateBrush(_settings.Store.LabelColor),
                OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
                MaxLines = 2,
            });
        }
    }

    private Control CreateMeasurement(OsdItem item, SensorReadingItem reading)
    {
        var label = new TextBlock
        {
            Text = GetLabel(item),
            Foreground = CreateBrush(_settings.Store.LabelColor),
            FontSize = _settings.Store.FontSize,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var value = new TextBlock
        {
            Text = reading.DisplayValue,
            Foreground = GetValueBrush(item, reading),
            FontSize = _settings.Store.FontSize,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (Style == AvaloniaOsdStyle.Bar)
        {
            return new StackPanel
            {
                Spacing = 2,
                Children = { label, value },
            };
        }

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        row.Children.Add(label);
        Grid.SetColumn(value, 1);
        row.Children.Add(value);
        return row;
    }

    private IBrush GetValueBrush(OsdItem item, SensorReadingItem reading)
    {
        var value = reading.Value;
        if (value is null)
            return CreateBrush(_settings.Store.ValueColor);

        var store = _settings.Store;
        if (item is OsdItem.CpuTemperature or OsdItem.GpuTemperature or OsdItem.GpuVramTemperature
            or OsdItem.MemoryTemperature or OsdItem.Disk1Temperature or OsdItem.Disk2Temperature or OsdItem.PchTemperature)
        {
            if (value >= store.TempThresholdCritical)
                return CreateBrush(store.CriticalColor);
            if (value >= store.TempThresholdWarning)
                return CreateBrush(store.WarningColor);
        }
        else if (item is OsdItem.CpuUtilization or OsdItem.GpuUtilization or OsdItem.GpuVramUtilization or OsdItem.MemoryUtilization)
        {
            if (value >= store.UsageThresholdCritical)
                return CreateBrush(store.CriticalColor);
            if (value >= store.UsageThresholdWarning)
                return CreateBrush(store.WarningColor);
        }
        else if (item == OsdItem.Fps && value <= store.FpsThresholdCritical)
        {
            return CreateBrush(store.CriticalColor);
        }

        return CreateBrush(store.ValueColor);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (_settings.Store.IsLocked || !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        BeginMoveDrag(args);
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property.Name == nameof(Position))
            SavePosition();
    }

    private void RestorePosition()
    {
        if (_positionRestored)
            return;

        var store = _settings.Store;
        var x = Style == AvaloniaOsdStyle.Bar ? store.BarPositionX : store.PanelPositionX;
        var y = Style == AvaloniaOsdStyle.Bar ? store.BarPositionY : store.PanelPositionY;
        if (x is { } savedX && y is { } savedY)
        {
            Position = new PixelPoint((int)Math.Round(savedX), (int)Math.Round(savedY));
        }
        else
        {
            Position = Style == AvaloniaOsdStyle.Bar
                ? new PixelPoint(120, 64)
                : new PixelPoint(24, 96);
        }

        _positionRestored = true;
    }

    private void SavePosition()
    {
        if (!_positionRestored)
            return;

        if (Style == AvaloniaOsdStyle.Bar)
        {
            _settings.Store.BarPositionX = Position.X;
            _settings.Store.BarPositionY = Position.Y;
        }
        else
        {
            _settings.Store.PanelPositionX = Position.X;
            _settings.Store.PanelPositionY = Position.Y;
        }
        _settings.SynchronizeStore();
    }

    private static bool Matches(OsdItem item, SensorReadingItem reading)
    {
        var name = reading.Name;
        var category = reading.Category;
        return item switch
        {
            OsdItem.Fps => name.Contains("FPS", StringComparison.OrdinalIgnoreCase),
            OsdItem.LowFps => name.Contains("Low FPS", StringComparison.OrdinalIgnoreCase),
            OsdItem.FrameTime => name.Contains("Frame", StringComparison.OrdinalIgnoreCase),
            OsdItem.CpuFrequency or OsdItem.CpuPCoreFrequency or OsdItem.CpuECoreFrequency =>
                category.Equals("CPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Clock", StringComparison.OrdinalIgnoreCase),
            OsdItem.CpuUtilization => category.Equals("CPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Utilization", StringComparison.OrdinalIgnoreCase),
            OsdItem.CpuTemperature => category.Equals("CPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Temperature", StringComparison.OrdinalIgnoreCase),
            OsdItem.CpuPower => category.Equals("CPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Power", StringComparison.OrdinalIgnoreCase),
            OsdItem.CpuFan => category.Equals("CPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Fan", StringComparison.OrdinalIgnoreCase),
            OsdItem.GpuFrequency => category.Equals("GPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Clock", StringComparison.OrdinalIgnoreCase),
            OsdItem.GpuUtilization => category.Equals("GPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Utilization", StringComparison.OrdinalIgnoreCase),
            OsdItem.GpuTemperature => category.Equals("GPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Temperature", StringComparison.OrdinalIgnoreCase),
            OsdItem.GpuVramUtilization => name.Contains("VRAM", StringComparison.OrdinalIgnoreCase) && name.Contains("Utilization", StringComparison.OrdinalIgnoreCase),
            OsdItem.GpuVramTemperature => name.Contains("VRAM", StringComparison.OrdinalIgnoreCase) && name.Contains("Temperature", StringComparison.OrdinalIgnoreCase),
            OsdItem.GpuPower => category.Equals("GPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Power", StringComparison.OrdinalIgnoreCase),
            OsdItem.GpuFan => category.Equals("GPU", StringComparison.OrdinalIgnoreCase) && name.Contains("Fan", StringComparison.OrdinalIgnoreCase),
            OsdItem.MemoryUtilization => category.Contains("Memory", StringComparison.OrdinalIgnoreCase) && name.Contains("Utilization", StringComparison.OrdinalIgnoreCase),
            OsdItem.MemoryTemperature => category.Contains("Memory", StringComparison.OrdinalIgnoreCase) && name.Contains("Temperature", StringComparison.OrdinalIgnoreCase),
            OsdItem.Disk1Temperature => name.Contains("Disk 1", StringComparison.OrdinalIgnoreCase) && name.Contains("Temperature", StringComparison.OrdinalIgnoreCase),
            OsdItem.Disk2Temperature => name.Contains("Disk 2", StringComparison.OrdinalIgnoreCase) && name.Contains("Temperature", StringComparison.OrdinalIgnoreCase),
            OsdItem.PchTemperature => category.Contains("PCH", StringComparison.OrdinalIgnoreCase) && name.Contains("Temperature", StringComparison.OrdinalIgnoreCase),
            OsdItem.PchFan => category.Contains("PCH", StringComparison.OrdinalIgnoreCase) && name.Contains("Fan", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static string GetLabel(OsdItem item) => item switch
    {
        OsdItem.CpuFrequency => "CPU clock",
        OsdItem.CpuPCoreFrequency => "CPU P-core clock",
        OsdItem.CpuECoreFrequency => "CPU E-core clock",
        OsdItem.CpuUtilization => "CPU usage",
        OsdItem.CpuTemperature => "CPU temperature",
        OsdItem.CpuPower => "CPU power",
        OsdItem.CpuFan => "CPU fan",
        OsdItem.GpuFrequency => "GPU clock",
        OsdItem.GpuUtilization => "GPU usage",
        OsdItem.GpuTemperature => "GPU temperature",
        OsdItem.GpuVramUtilization => "VRAM usage",
        OsdItem.GpuVramTemperature => "VRAM temperature",
        OsdItem.GpuPower => "GPU power",
        OsdItem.GpuFan => "GPU fan",
        OsdItem.MemoryUtilization => "Memory usage",
        OsdItem.MemoryTemperature => "Memory temperature",
        OsdItem.Disk1Temperature => "Disk 1 temperature",
        OsdItem.Disk2Temperature => "Disk 2 temperature",
        OsdItem.PchTemperature => "PCH temperature",
        OsdItem.PchFan => "PCH fan",
        OsdItem.Fps => "FPS",
        OsdItem.LowFps => "Low FPS",
        OsdItem.FrameTime => "Frame time",
        _ => item.ToString(),
    };

    private static IBrush CreateBrush(string color, double opacity = 1)
    {
        try
        {
            var parsed = Color.Parse(color);
            return new SolidColorBrush(parsed, Math.Clamp(opacity, 0, 1));
        }
        catch
        {
            return Brushes.Transparent;
        }
    }
}

#endif
