using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.ViewModels;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Pages.WindowsOptimization;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage
{
    private async void ScanCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanCleanupAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async void RunCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedActions = ViewModel.CleanupCategories
            .SelectMany(c => c.Actions)
            .Where(a => a.IsEnabled && a.IsSelected)
            .ToList();

        if (selectedActions.Count == 0)
        {
            await SnackbarHelper.ShowAsync(
                Resource.SettingsPage_WindowsOptimization_Title,
                Resource.ResourceManager.GetString("WindowsOptimizationPage_Cleanup_NoSelection_Warning") ?? "Please select at least one cleanup option.",
                SnackbarType.Warning);
            return;
        }

        // Logic for running cleanup with progress reporting
        try
        {
            ViewModel.IsBusy = true;
            ViewModel.IsCleaning = true;
            var swOverall = Stopwatch.StartNew();
            long totalFreedBytes = 0;

            for (int i = 0; i < selectedActions.Count; i++)
            {
                var action = selectedActions[i];
                var progress = (int)((i + 1.0) / selectedActions.Count * 100);
                
                // Update UI on UI thread
                await Dispatcher.BeginInvoke(() =>
                {
                    ViewModel.CurrentOperationText = string.Format(Resource.ResourceManager.GetString("WindowsOptimizationPage_RunningStep") ?? "Running {0}...", action.Title);
                    ViewModel.RunCleanupButtonText = string.Format(Resource.WindowsOptimizationPage_RunCleanupButtonText_Format, progress);
                });

                long sizeBefore = 0;
                try
                {
                    sizeBefore = await _windowsOptimizationService.EstimateActionSizeAsync(action.Key, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to estimate size before cleanup for {action.Key}", ex);
                }

                await _windowsOptimizationService.ExecuteActionsAsync([action.Key], CancellationToken.None).ConfigureAwait(false);

                long sizeAfter = 0;
                try
                {
                    sizeAfter = await _windowsOptimizationService.EstimateActionSizeAsync(action.Key, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to estimate size after cleanup for {action.Key}", ex);
                }
                totalFreedBytes += Math.Max(0, sizeBefore - sizeAfter);
            }

            swOverall.Stop();
            var summary = string.Format(Resource.ResourceManager.GetString("WindowsOptimizationPage_CleanupSummary") ?? "Freed {0} in {1}s", 
                ViewModel.EstimatedCleanupSizeText, swOverall.Elapsed.TotalSeconds.ToString("0.0"));
            
            // Show snackbar on UI thread
            await Dispatcher.BeginInvoke(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, summary, SnackbarType.Success));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Cleanup failed.", ex);
            
            await Dispatcher.BeginInvoke(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, Resource.SettingsPage_WindowsOptimization_Cleanup_Error, SnackbarType.Error));
        }
        finally
        {
            // Update UI on UI thread
            await Dispatcher.BeginInvoke(() =>
            {
                ViewModel.IsBusy = false;
                ViewModel.IsCleaning = false;
                ViewModel.CurrentOperationText = string.Empty;
                ViewModel.RunCleanupButtonText = string.Empty;
            });
            
            await ViewModel.UpdateEstimatedCleanupSizeAsync().ConfigureAwait(false);
        }
    }

    private void AddCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var rule = new CustomCleanupRuleViewModel(dialog.SelectedPath, [], false);
            ViewModel.CustomCleanupRules.Add(rule);
            SaveCustomCleanupRules();
        }
    }

    private void ClearCustomCleanupRulesButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CustomCleanupRules.Clear();
        SaveCustomCleanupRules();
    }

    private void EditCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is CustomCleanupRuleViewModel rule)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = rule.DirectoryPath;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                rule.DirectoryPath = dialog.SelectedPath;
                SaveCustomCleanupRules();
            }
        }
    }

    private void RemoveCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is CustomCleanupRuleViewModel rule)
        {
            ViewModel.CustomCleanupRules.Remove(rule);
            SaveCustomCleanupRules();
        }
    }

    private void SaveCustomCleanupRules()
    {
        _applicationSettings.Store.CustomCleanupRules = ViewModel.CustomCleanupRules.Select(r => r.ToModel()).ToList();
        _applicationSettings.SynchronizeStore();
    }
}
