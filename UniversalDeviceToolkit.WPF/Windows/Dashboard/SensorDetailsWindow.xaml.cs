using System;
using System.Windows;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Windows.Dashboard;

public partial class SensorDetailsWindow
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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Fit typical desktop work areas without leaving large empty chrome.
        var work = SystemParameters.WorkArea;
        MaxWidth = Math.Max(900, work.Width - 48);
        MaxHeight = Math.Max(520, work.Height - 64);
        if (Width > MaxWidth)
            Width = MaxWidth;
        if (Height > MaxHeight)
            Height = MaxHeight;
    }
}
