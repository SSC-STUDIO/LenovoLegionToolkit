using System;
using Avalonia;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Dashboard
{
public partial class BalanceModeSettingsWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly PowerModeFeature _powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
    private readonly AIController _aiController = IoCContainer.Resolve<AIController>();

    public BalanceModeSettingsWindow()
    {
        InitializeComponent();

        PropertyChanged += BalanceModeSettingsWindow_PropertyChanged;
    }

    private void BalanceModeSettingsWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        if (!IsVisible)
            return;

        _aiModeCheckBox.IsChecked = _aiController.IsAIModeEnabled;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var isAiModeChecked = _aiModeCheckBox.IsChecked ?? false;

            _aiController.IsAIModeEnabled = isAiModeChecked;

            await _aiController.StopAsync();
            await _powerModeFeature.SetStateAsync(PowerModeState.Balance);
            await _aiController.StartIfNeededAsync();

            Close();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(SaveButton_Click)}.", ex);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
}
