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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        MaxHeight = Math.Max(560, SystemParameters.WorkArea.Height - 72);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
