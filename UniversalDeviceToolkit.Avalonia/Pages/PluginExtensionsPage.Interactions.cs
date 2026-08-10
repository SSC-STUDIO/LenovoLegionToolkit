using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class PluginExtensionsPage
{
    // AVALONIA: WPF MouseDoubleClick â†?Avalonia DoubleTapped (TappedEventArgs).
    private async void PluginListBox_MouseDoubleClick(object? sender, TappedEventArgs e)
    {
        try
        {
            PluginViewModel? clickedViewModel = null;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("PluginListBox_MouseDoubleClick triggered");

            // Ignore double-clicks that originate from action buttons inside the item template.
            if (e.Source is Visual source)
            {
                var current = source;
                while (current != null)
                {
                    if (current is Button)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace("PluginListBox_MouseDoubleClick ignored because original source is a button");
                        return;
                    }

                    if (current is Control element && element.DataContext is PluginViewModel viewModel)
                    {
                        clickedViewModel = viewModel;
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"PluginListBox_MouseDoubleClick data context resolved: {viewModel.PluginId}");
                        break;
                    }

                    current = current.GetVisualParent();
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
        Dispatcher.UIThread.Post(() =>
        {
            UpdateSpecificPluginUI(e.PluginId);
            UpdateAllPluginsUI();
        });
    }

    private void PluginDetailsToggleButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not UniversalDeviceToolkit.Avalonia.Controls.Button button || button.Tag is not string pluginId)
            return;

        var viewModel = _pluginViewModels.FirstOrDefault(vm =>
            string.Equals(vm.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        viewModel?.ToggleDetails();
    }

    private void ContextMenu_OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string pluginId)
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

    private async void ContextMenu_CopyId_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string pluginId)
        {
            try
            {
                // AVALONIA: WPF Clipboard replaced by TopLevel.Clipboard (async).
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard is not null)
                    await clipboard.SetTextAsync(pluginId);

                SnackbarHelper.Show(Resource.PluginExtensionsPage_Copied, string.Format(Resource.PluginExtensionsPage_CopiedMessage, pluginId), SnackbarType.Info);
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"Error copying plugin ID: {ex.Message}", ex);
            }
        }
    }
}
