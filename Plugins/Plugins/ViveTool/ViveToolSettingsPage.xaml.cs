using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Plugins.Shared;
using UniversalDeviceToolkit.Plugins.ViveTool.Resources;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings;
using Microsoft.Win32;

namespace UniversalDeviceToolkit.Plugins.ViveTool;

public partial class ViveToolSettingsPage
{
    private readonly IViveToolService _viveToolService;
    private readonly Services.Settings.ViveToolSettings _settings;
    private bool _isDownloading = false;
    private int _downloadProgress = 0;

    public ViveToolSettingsPage()
    {
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
        _viveToolService = new ViveToolService();
        _settings = new Services.Settings.ViveToolSettings();
        Loaded += ViveToolSettingsPage_Loaded;
    }

    private void BuildFallbackUi()
    {
        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 12),
            TextWrapping = TextWrapping.Wrap,
            Foreground = ResolveBrush("TextFillColorSecondaryBrush", SystemColors.ControlTextBrush)
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "ViveToolSettingsStatusText");

        _viveToolPathTextBox = new Wpf.Ui.Controls.TextBox
        {
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 8, 0)
        };
        AutomationProperties.SetAutomationId(_viveToolPathTextBox, "ViveToolSettingsPathTextBox");

        _downloadProgressBar = new ProgressBar
        {
            Height = 8,
            Margin = new Thickness(0, 0, 0, 0),
            Foreground = ResolveBrush("SystemAccentColorPrimaryBrush", SystemColors.HighlightBrush)
        };
        AutomationProperties.SetAutomationId(_downloadProgressBar, "ViveToolSettingsDownloadProgressBar");
        _downloadProgressText = new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0),
            Foreground = ResolveBrush("TextFillColorSecondaryBrush", SystemColors.ControlTextBrush)
        };
        AutomationProperties.SetAutomationId(_downloadProgressText, "ViveToolSettingsDownloadProgressText");
        _downloadProgressGrid = new StackPanel
        {
            Visibility = Visibility.Collapsed
        };
        AutomationProperties.SetAutomationId(_downloadProgressGrid, "ViveToolSettingsDownloadProgressPanel");
        _downloadProgressGrid.Children.Add(_downloadProgressBar);
        _downloadProgressGrid.Children.Add(_downloadProgressText);

        _gitHubButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_GitHub, Margin = new Thickness(0, 0, 8, 0) };
        AutomationProperties.SetAutomationId(_gitHubButton, "ViveToolSettingsGitHubButton");
        _gitHubButton.Click += GitHubButton_Click;
        _downloadViveToolButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_Download, Margin = new Thickness(0, 0, 8, 0) };
        AutomationProperties.SetAutomationId(_downloadViveToolButton, "ViveToolSettingsDownloadButton");
        _downloadViveToolButton.Click += DownloadViveToolButton_Click;
        _refreshStatusButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_Refresh };
        AutomationProperties.SetAutomationId(_refreshStatusButton, "ViveToolSettingsRefreshStatusButton");
        _refreshStatusButton.Click += RefreshStatusButton_Click;
        _browseViveToolButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_Browse, Margin = new Thickness(0, 0, 8, 0) };
        AutomationProperties.SetAutomationId(_browseViveToolButton, "ViveToolSettingsBrowseButton");
        _browseViveToolButton.Click += BrowseViveToolButton_Click;
        _importConfigButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_ImportConfig };
        AutomationProperties.SetAutomationId(_importConfigButton, "ViveToolSettingsImportConfigButton");
        _importConfigButton.Click += ImportConfigButton_Click;

        var actionRow = new WrapPanel();
        actionRow.Children.Add(_gitHubButton);
        actionRow.Children.Add(_downloadViveToolButton);
        actionRow.Children.Add(_refreshStatusButton);

        var pathRow = new Grid();
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_viveToolPathTextBox, 0);
        Grid.SetColumn(_browseViveToolButton, 1);
        Grid.SetColumn(_importConfigButton, 2);
        pathRow.Children.Add(_viveToolPathTextBox);
        pathRow.Children.Add(_browseViveToolButton);
        pathRow.Children.Add(_importConfigButton);

        var statusCard = new Border
        {
            BorderBrush = ResolveBrush("ControlStrokeColorDefaultBrush", SystemColors.ControlDarkBrush),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Background = ResolveBrush("ControlFillColorSecondaryBrush", SystemColors.ControlBrush),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var statusStack = new StackPanel();
        statusStack.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_ViveToolStatus,
            FontSize = 18
        });
        statusStack.Children.Add(_statusTextBlock);
        statusStack.Children.Add(_downloadProgressGrid);
        statusStack.Children.Add(actionRow);
        statusCard.Child = statusStack;

        var pathCard = new Border
        {
            BorderBrush = ResolveBrush("ControlStrokeColorDefaultBrush", SystemColors.ControlDarkBrush),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Background = ResolveBrush("ControlFillColorSecondaryBrush", SystemColors.ControlBrush),
            Padding = new Thickness(14)
        };
        var pathStack = new StackPanel();
        pathStack.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_BinaryPathTitle,
            FontSize = 18,
            Margin = new Thickness(0, 0, 0, 10)
        });
        pathStack.Children.Add(pathRow);
        pathStack.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_PathDescription,
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        pathCard.Child = pathStack;

        var root = new StackPanel { Margin = new Thickness(16) };
        AutomationProperties.SetAutomationId(this, "ViveToolSettingsRoot");
        AutomationProperties.SetAutomationId(root, "ViveToolSettingsRoot");
        root.Children.Add(statusCard);
        root.Children.Add(pathCard);

        Content = root;
    }

    private async void ViveToolSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settings.LoadAsync();
            await RefreshStatusAsync();

            _viveToolPathTextBox?.Text = _settings.ViveToolPath ?? string.Empty;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error loading ViveTool settings: {ex.Message}", ex);
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

            SetStatus(statusText, !available);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error refreshing ViveTool status: {ex.Message}", ex);

            SetStatus(Resource.ViveTool_ViveToolError, true);
        }
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is null)
        {
            return;
        }

        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
            : ResolveBrush("SystemFillColorSuccessBrush", SystemColors.ControlTextBrush);

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = isError
                ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
    }

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    private async void BrowseViveToolButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.ViveTool_SelectViveTool,
                Filter = GetExecutableDialogFilter(),
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
           {
               var selectedPath = openFileDialog.FileName;
               var fileName = Path.GetFileName(selectedPath);

               if (!fileName.Equals(ViveToolPathService.ViveToolExeName, StringComparison.OrdinalIgnoreCase))
               {
                   SetStatus(Resource.ViveTool_InvalidViveToolFile, isError: true);
                    return;
               }

                var success = await _viveToolService.SetViveToolPathAsync(selectedPath).ConfigureAwait(true);

                if (success)
                {
                    _viveToolPathTextBox?.Text = selectedPath;

                    await RefreshStatusAsync().ConfigureAwait(true);
                }
               else
               {
                   SetStatus(string.Format(Resource.ViveTool_SetPathFailed, string.Empty), isError: true);
               }
           }
       }
       catch (Exception ex)
       {
           PluginLog.Trace($"Error browsing for vivetool.exe: {ex.Message}", ex);
           SetStatus(string.Format(Resource.ViveTool_BrowseError, ex.Message), isError: true);
       }
   }

   private async void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"RefreshStatusButton_Click error: {ex.Message}", ex);
        }
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
            PluginLog.Trace($"Error opening GitHub URL: {ex.Message}", ex);
        }
    }

    private async void DownloadViveToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloading)
        {
           return;
       }

        var downloadError = false;

        try
        {
            _isDownloading = true;
            _downloadProgress = 0;

            // Update UI
            _downloadViveToolButton?.IsEnabled = false;

            _refreshStatusButton?.IsEnabled = false;

            _downloadProgressGrid?.Visibility = Visibility.Visible;

            _downloadProgressBar?.Value = 0;

            _downloadProgressText?.Text = Resource.ViveTool_Downloading;

            // Start download
            var progress = new Progress<long>(bytesDownloaded =>
            {
                const long estimatedTotalBytes = UniversalDeviceToolkit.Plugins.Shared.Constants.EstimatedViveToolDownloadBytes;
                _downloadProgress = (int)Math.Min(100, bytesDownloaded * 100 / estimatedTotalBytes);
                _downloadProgressBar?.Value = _downloadProgress;
            });

            var downloadSuccess = await _viveToolService.DownloadViveToolAsync(progress);

            // Set the path and refresh status
            if (downloadSuccess)
            {
                _downloadProgressText?.Text = Resource.ViveTool_DownloadComplete;

                _downloadProgress = 100;
                _downloadProgressBar?.Value = 100;

                // Get the downloaded path from the service
                var viveToolPath = await _viveToolService.GetViveToolPathAsync();
                if (!string.IsNullOrEmpty(viveToolPath) && _viveToolPathTextBox != null)
                {
                    _viveToolPathTextBox.Text = viveToolPath;
                }

                await RefreshStatusAsync().ConfigureAwait(true);
            }
            else
            {
                _downloadProgressText?.Text = Resource.ViveTool_DownloadFailed;
            }

            await Task.Delay(downloadSuccess ? 2000 : 1000);
        }
       catch (Exception ex)
       {
           PluginLog.Trace($"Error downloading ViveTool: {ex.Message}", ex);

            downloadError = true;
            SetStatus(string.Format(Resource.ViveTool_DownloadFailed, ex.Message), isError: true);
       }
       finally
       {
            _isDownloading = false;

            // Reset UI
            _downloadViveToolButton?.IsEnabled = true;

            _refreshStatusButton?.IsEnabled = true;

            await Task.Delay(1000); // Brief pause before hiding progress

            _downloadProgressGrid?.Visibility = Visibility.Collapsed;

           // Refresh status after download (skip on error so the inline message stays readable)
           if (!downloadError)
           {
               await RefreshStatusAsync();
           }
       }
   }

   private async void ImportConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.ViveTool_ImportConfigTitle,
                Filter = GetImportConfigDialogFilter(),
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
               var importedFeatures = await _viveToolService.ImportFeaturesFromFileAsync(openFileDialog.FileName).ConfigureAwait(true);

               SetStatus(
                   string.Format(Resource.ViveTool_ConfigImportSuccessMessage, importedFeatures.Count, openFileDialog.FileName),
                   isError: false);
           }
       }
       catch (Exception ex)
       {
           PluginLog.Trace($"Error importing configuration: {ex.Message}", ex);

           SetStatus(
               string.Format(Resource.ViveTool_ConfigImportFailedMessage, ex.Message),
               isError: true);
       }
    }

    private static string GetExecutableDialogFilter()
    {
        return T("ViveTool_ExecutableDialogFilter", "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*");
    }

    private static string GetImportConfigDialogFilter()
    {
        return T("ViveTool_ImportConfigDialogFilter", "JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*");
    }

    private static string T(string key, string fallback)
    {
        return Resource.ResourceManager.GetString(key, Resource.Culture ?? CultureInfo.CurrentUICulture) ?? fallback;
    }
}
