using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.ViewModels;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class WindowsOptimizationPage
{
    private async void ScanCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.ScanCleanupAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error scanning cleanup.", ex);
        }
    }

    private async void RunCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        var cleanupToastId = Guid.Empty;
        try
        {
            var selectedActions = ViewModel.CleanupCategories
                .SelectMany(c => c.Actions)
                .Where(a => a.IsEnabled && a.IsSelected)
                .ToList();

            if (selectedActions.Count == 0)
            {
                await SnackbarHelper.ShowAsync(
                    Resource.SettingsPage_WindowsOptimization_Title,
                    LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "WindowsOptimizationPage_Cleanup_NoSelection_Warning", "Please select at least one cleanup option.", Resource.Culture),
                    SnackbarType.Warning);
                return;
            }

            // Logic for running cleanup with progress reporting
            ViewModel.IsBusy = true;
            ViewModel.IsCleaning = true;
            cleanupToastId = ProgressToastHelper.Start(Resource.SettingsPage_WindowsOptimization_Title);
            var swOverall = Stopwatch.StartNew();
            long totalFreedBytes = 0;
            var successCount = 0;
            var failCount = 0;

            for (int i = 0; i < selectedActions.Count; i++)
            {
                var action = selectedActions[i];
                var progress = (int)((i + 1.0) / selectedActions.Count * 100);
                
                // Update UI on UI thread
                await Dispatcher.BeginInvoke(() =>
                {
                    ViewModel.CurrentOperationText = string.Format(LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "WindowsOptimizationPage_RunningStep", "Running {0}...", Resource.Culture), action.Title);
                    ViewModel.RunCleanupButtonText = string.Format(Resource.WindowsOptimizationPage_RunCleanupButtonText_Format, progress);
                });

                ProgressToastHelper.Update(
                    cleanupToastId,
                    progress,
                    string.Format(LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "WindowsOptimizationPage_RunningStep", "Running {0}...", Resource.Culture), action.Title));

                long sizeBefore = 0;
                try
                {
                    sizeBefore = await _windowsOptimizationService.EstimateActionSizeAsync(action.Key, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to estimate size before cleanup for {action.Key}", ex);
                }

                try
                {
                    await _windowsOptimizationService.ExecuteActionsAsync([action.Key], CancellationToken.None);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Cleanup step failed for {action.Key}", ex);
                    continue;
                }

                long sizeAfter = 0;
                try
                {
                    sizeAfter = await _windowsOptimizationService.EstimateActionSizeAsync(action.Key, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to estimate size after cleanup for {action.Key}", ex);
                }
                totalFreedBytes += Math.Max(0, sizeBefore - sizeAfter);
            }

            swOverall.Stop();
            var freedText = FormatFreedBytes(totalFreedBytes);
            string summary;
            SnackbarType severity;
            if (failCount == 0)
            {
                // Even when every step "succeeded", zero freed bytes is still a valid result.
                summary = string.Format(
                    LocalizationHelper.GetStringOrEnglish(
                        Resource.ResourceManager,
                        "WindowsOptimizationPage_CleanupSummary",
                        "Freed {0} in {1}s ({2} items).",
                        Resource.Culture),
                    freedText,
                    swOverall.Elapsed.TotalSeconds.ToString("0.0"),
                    successCount);
                severity = SnackbarType.Success;
            }
            else if (successCount == 0)
            {
                summary = LocalizationHelper.GetStringOrEnglish(
                    Resource.ResourceManager,
                    "SettingsPage_WindowsOptimization_Cleanup_Error",
                    "Cleanup failed.",
                    Resource.Culture);
                severity = SnackbarType.Error;
            }
            else
            {
                summary = string.Format(
                    LocalizationHelper.GetStringOrEnglish(
                        Resource.ResourceManager,
                        "WindowsOptimizationPage_CleanupPartialSummary",
                        "Freed {0}. {1} succeeded, {2} failed.",
                        Resource.Culture),
                    freedText,
                    successCount,
                    failCount);
                severity = SnackbarType.Warning;
            }

            await Dispatcher.BeginInvoke(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, summary, severity));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Cleanup failed.", ex);
            
            await Dispatcher.BeginInvoke(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, Resource.SettingsPage_WindowsOptimization_Cleanup_Error, SnackbarType.Error));
        }
        finally
        {
            ProgressToastHelper.Complete(cleanupToastId);

            try
            {
                // Update UI on UI thread
                await Dispatcher.BeginInvoke(() =>
                {
                    ViewModel.IsBusy = false;
                    ViewModel.IsCleaning = false;
                    ViewModel.CurrentOperationText = string.Empty;
                    ViewModel.ResetRunCleanupButtonText();
                });

                await ViewModel.UpdateEstimatedCleanupSizeAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Cleanup finally block failed.", ex);
            }
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

    private static string FormatFreedBytes(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} {units[unit]}"
            : $"{value:0.##} {units[unit]}";
    }
}

