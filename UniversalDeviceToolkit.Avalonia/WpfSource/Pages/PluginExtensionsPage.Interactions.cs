using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class PluginExtensionsPage
{
    private async void PluginListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            PluginViewModel? clickedViewModel = null;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("PluginListBox_MouseDoubleClick triggered");

            // Ignore double-clicks that originate from action buttons inside the item template.
            if (e.OriginalSource is DependencyObject source)
            {
                var current = source;
                while (current != null)
                {
                    if (current is System.Windows.Controls.Button)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace("PluginListBox_MouseDoubleClick ignored because original source is a button");
                        return;
                    }

                    if (current is FrameworkElement element && element.DataContext is PluginViewModel viewModel)
                    {
                        clickedViewModel = viewModel;
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"PluginListBox_MouseDoubleClick data context resolved: {viewModel.PluginId}");
                        break;
                    }

                    current = VisualTreeHelper.GetParent(current);
                }
            }

            var selectedViewModel = clickedViewModel ?? _pluginsListBox.SelectedItem as PluginViewModel;
            if (selectedViewModel != null)
            {
                if (!ReferenceEquals(_pluginsListBox.SelectedItem, selectedViewModel))
                    _pluginsListBox.SelectedItem = selectedViewModel;

                var isInstalled = IsPluginInstalledForUi(selectedViewModel.PluginId);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"PluginListBox_MouseDoubleClick target={selectedViewModel.PluginId}, isInstalled={isInstalled}");

                if (isInstalled)
                {
                    await OpenPluginDefaultActionAsync(selectedViewModel.PluginId);
                }
                else
                {
                    SnackbarHelper.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage, SnackbarType.Warning);
                }
            }
            else if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace("PluginListBox_MouseDoubleClick no target plugin view model resolved");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PluginListBox_MouseDoubleClick)}: {ex.Message}", ex);
        }
    }

    private void PluginManager_PluginStateChanged(object? sender, PluginEventArgs e)
    {
        // Update UI when plugin state changes (installed/uninstalled)
        Dispatcher.BeginInvoke(() =>
        {
            UpdateSpecificPluginUI(e.PluginId);
            UpdateAllPluginsUI();
        });
    }

    private void PluginDetailsToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
            return;

        var viewModel = _pluginViewModels.FirstOrDefault(vm =>
            string.Equals(vm.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        viewModel?.ToggleDetails();
    }

    private void ContextMenu_OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string pluginId)
        {
            if (!PathSecurity.IsValidPluginId(pluginId))
                return;

            try
            {
                var pluginsDir = GetPluginsDirectory();
                var metadata = _pluginManager.GetPluginMetadata(pluginId);
                string path;

                if (metadata?.FilePath != null)
                {
                    path = Path.GetDirectoryName(metadata.FilePath) ?? string.Empty;
                }
                else
                {
                    path = Path.Combine(pluginsDir, pluginId);
                }

                if (Directory.Exists(path))
                {
                    using var process = System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    SnackbarHelper.Show(Resource.PluginExtensionsPage_FolderNotFound, Resource.PluginExtensionsPage_FolderNotFoundMessage, SnackbarType.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"Error opening plugin folder: {ex.Message}", ex);
            }
        }
    }

    private void ContextMenu_CopyId_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string pluginId)
        {
            try
            {
                System.Windows.Clipboard.SetText(pluginId);
                SnackbarHelper.Show(Resource.PluginExtensionsPage_Copied, string.Format(Resource.PluginExtensionsPage_CopiedMessage, pluginId), SnackbarType.Info);
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"Error copying plugin ID: {ex.Message}", ex);
            }
        }
    }
}
