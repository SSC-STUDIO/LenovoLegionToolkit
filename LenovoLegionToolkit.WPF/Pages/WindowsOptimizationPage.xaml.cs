using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage
{
    private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
    private bool _isRunning;

    public WindowsOptimizationPage()
    {
        InitializeComponent();
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e) =>
        await ExecuteAsync(
            _windowsOptimizationService.ApplyPerformanceOptimizationsAsync,
            Resource.SettingsPage_WindowsOptimization_Performance_Success,
            Resource.SettingsPage_WindowsOptimization_Performance_Error);

    private async void CleanupButton_Click(object sender, RoutedEventArgs e) =>
        await ExecuteAsync(
            _windowsOptimizationService.RunCleanupAsync,
            Resource.SettingsPage_WindowsOptimization_Cleanup_Success,
            Resource.SettingsPage_WindowsOptimization_Cleanup_Error);

    private async Task ExecuteAsync(Func<CancellationToken, Task> operation, string successMessage, string errorMessage)
    {
        if (_isRunning)
            return;

        _isRunning = true;
        ToggleButtons(false);

        try
        {
            await operation(CancellationToken.None).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, successMessage, SnackbarType.Success));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Windows optimization action failed. Exception: {ex.Message}", ex);

            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, errorMessage, SnackbarType.Error));
        }
        finally
        {
            _isRunning = false;
            Dispatcher.Invoke(() => ToggleButtons(true));
        }
    }

    private void ToggleButtons(bool isEnabled)
    {
        _applyButton.IsEnabled = isEnabled;
        _cleanupButton.IsEnabled = isEnabled;
    }
}

