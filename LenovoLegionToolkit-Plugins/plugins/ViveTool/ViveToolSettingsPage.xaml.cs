using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.ViveTool.Resources;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using LenovoLegionToolkit.Plugins.ViveTool.Services.Settings;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.ViveTool;

public partial class ViveToolSettingsPage
{
    private readonly IViveToolService _viveToolService;
    private readonly Services.Settings.ViveToolSettings _settings;
    private bool _isDownloading = false;
    private int _downloadProgress = 0;

    public ViveToolSettingsPage()
    {
        InitializeComponent();
        _viveToolService = new ViveToolService();
        _settings = new Services.Settings.ViveToolSettings();
        Loaded += ViveToolSettingsPage_Loaded;
    }

    private async void ViveToolSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settings.LoadAsync();
            await RefreshStatusAsync();
             
            if (_viveToolPathTextBox != null)
            {
                _viveToolPathTextBox.Text = _settings.ViveToolPath ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading ViveTool settings: {ex.Message}", ex);
        }
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            var available = await _viveToolService.IsViveToolAvailableAsync();
            var path = await _viveToolService.GetViveToolPathAsync();
            
            var statusText = available ? 
                string.Format(Resource.ViveTool_ViveToolFound, path ?? Resource.ViveTool_ViveToolNotFound) :
                Resource.ViveTool_ViveToolNotFound;

            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = statusText;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error refreshing ViveTool status: {ex.Message}", ex);
            
            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = Resource.ViveTool_ViveToolError;
            }
        }
    }

    private async void BrowseViveToolButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.ViveTool_SelectViveTool,
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedPath = openFileDialog.FileName;
                var fileName = Path.GetFileName(selectedPath);
                
                if (!fileName.Equals(ViveToolService.ViveToolExeName, StringComparison.OrdinalIgnoreCase))
                {
                    System.Windows.MessageBox.Show(Resource.ViveTool_InvalidViveToolFile, Resource.ViveTool_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var success = await _viveToolService.SetViveToolPathAsync(selectedPath).ConfigureAwait(false);
                
                if (success)
                {
                    _viveToolPathTextBox.Text = selectedPath;
                }
                else
                {
                    System.Windows.MessageBox.Show(string.Format(Resource.ViveTool_SetPathFailed, string.Empty), Resource.ViveTool_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error browsing for vivetool.exe: {ex.Message}", ex);
            System.Windows.MessageBox.Show(string.Format(Resource.ViveTool_BrowseError, ex.Message), Resource.ViveTool_Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatusAsync();
    }

    private void GitHubButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = "https://github.com/thebookisclosed/ViVe/releases";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error opening GitHub URL: {ex.Message}", ex);
        }
    }

    private async void DownloadViveToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloading)
            return;

        try
        {
            _isDownloading = true;
            _downloadProgress = 0;

            // Update UI
            if (_downloadViveToolButton != null)
                _downloadViveToolButton.IsEnabled = false;
            if (_refreshStatusButton != null)
                _refreshStatusButton.IsEnabled = false;
            if (_downloadProgressGrid != null)
                _downloadProgressGrid.Visibility = Visibility.Visible;
            if (_downloadProgressBar != null)
                _downloadProgressBar.Value = 0;
            if (_downloadProgressText != null)
                _downloadProgressText.Text = Resource.ViveTool_Downloading;

            // Start download
            var progress = new Progress<long>(progress =>
            {
                _downloadProgress = (int)(progress / 10); // Convert long to int percentage
                if (_downloadProgressBar != null)
                    _downloadProgressBar.Value = _downloadProgress;
            });

            var downloadSuccess = await _viveToolService.DownloadViveToolAsync(progress);
            
            // Download completed
            if (_downloadProgressText != null)
                _downloadProgressText.Text = Resource.ViveTool_DownloadComplete;
            _downloadProgress = 100;
            if (_downloadProgressBar != null)
                _downloadProgressBar.Value = 100;

            // Set the path and refresh status
            if (downloadSuccess)
            {
                // Get the downloaded path from the service
                var viveToolPath = await _viveToolService.GetViveToolPathAsync();
                if (!string.IsNullOrEmpty(viveToolPath) && _viveToolPathTextBox != null)
                {
                    _viveToolPathTextBox.Text = viveToolPath;
                }
            }

            await Task.Delay(2000); // Show success message for 2 seconds
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error downloading ViveTool: {ex.Message}", ex);
            
            System.Windows.MessageBox.Show(string.Format(Resource.ViveTool_DownloadFailed, ex.Message), Resource.ViveTool_Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isDownloading = false;
            
            // Reset UI
            if (_downloadViveToolButton != null)
                _downloadViveToolButton.IsEnabled = true;
            if (_refreshStatusButton != null)
                _refreshStatusButton.IsEnabled = true;
            
            await Task.Delay(1000); // Brief pause before hiding progress
            
            if (_downloadProgressGrid != null)
                _downloadProgressGrid.Visibility = Visibility.Collapsed;
            
            // Refresh status after download
            await RefreshStatusAsync();
        }
    }

    private async void ImportConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.ViveTool_ImportConfigTitle,
                Filter = "JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var importedFeatures = await _viveToolService.ImportFeaturesFromFileAsync(openFileDialog.FileName).ConfigureAwait(false);
                
                System.Windows.MessageBox.Show(
                    string.Format(Resource.ViveTool_ConfigImportSuccessMessage, importedFeatures.Count, openFileDialog.FileName),
                    Resource.ViveTool_ConfigImportSuccess,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error importing configuration: {ex.Message}", ex);
            
            System.Windows.MessageBox.Show(
                string.Format(Resource.ViveTool_ConfigImportFailedMessage, ex.Message),
                Resource.ViveTool_ConfigImportFailed,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
