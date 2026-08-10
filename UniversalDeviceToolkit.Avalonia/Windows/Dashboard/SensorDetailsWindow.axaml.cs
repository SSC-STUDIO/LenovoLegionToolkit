using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Dashboard;

public partial class SensorDetailsWindow : BaseWindow
{
    private static string TitleText =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "SensorDetailsWindow_Title", "Sensor details", Resource.Culture);

    public SensorDetailsWindow()
    {
        InitializeComponent();
        Title = TitleText;
        _titleText.Text = TitleText;
    }

    private void SensorDetailsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _sensors.UseDetailsWindowLayout();
        _sensors.RestartTrendCharts();
    }

    private void SensorDetailsWindow_Closed(object? sender, EventArgs e) => _sensors.Dispose();

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // AVALONIA: WPF OnSourceInitialized/SystemParameters.WorkArea replaced by Screens work area.
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        var work = screen.WorkingArea.ToRect(screen.Scaling);
        MaxWidth = Math.Max(900, work.Width - 48);
        MaxHeight = Math.Max(520, work.Height - 64);
        if (Width > MaxWidth)
            Width = MaxWidth;
        if (Height > MaxHeight)
            Height = MaxHeight;
    }
}
