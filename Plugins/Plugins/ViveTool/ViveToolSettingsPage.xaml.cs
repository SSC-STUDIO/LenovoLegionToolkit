using System;
using System.Globalization;
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
        TryInitializeComponent();
        _viveToolService = new ViveToolService();
        _settings = new Services.Settings.ViveToolSettings();
        Loaded += ViveToolSettingsPage_Loaded;
    }

    private void TryInitializeComponent()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveToolSettingsPage InitializeComponent fallback: {ex.Message}", ex);

            BuildFallbackUi();
        }
    }

    private void BuildFallbackUi()
    {
        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 12),
            TextWrapping = TextWrapping.Wrap,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 96, 96))
        };

        _viveToolPathTextBox = new Wpf.Ui.Controls.TextBox
        {
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 8, 0)
        };

        _downloadProgressBar = new ProgressBar
        {
            Height = 8,
            Margin = new Thickness(0, 0, 0, 0)
        };
        _downloadProgressText = new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        _downloadProgressGrid = new Grid
        {
            Visibility = Visibility.Collapsed
        };
        _downloadProgressGrid.Children.Add(_downloadProgressBar);
        _downloadProgressGrid.Children.Add(_downloadProgressText);

        _gitHubButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_GitHub, Margin = new Thickness(0, 0, 8, 0) };
        _gitHubButton.Click += GitHubButton_Click;
        _downloadViveToolButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_Download, Margin = new Thickness(0, 0, 8, 0) };
        _downloadViveToolButton.Click += DownloadViveToolButton_Click;
        _refreshStatusButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_Refresh };
        _refreshStatusButton.Click += RefreshStatusButton_Click;
        _browseViveToolButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_Browse, Margin = new Thickness(0, 0, 8, 0) };
        _browseViveToolButton.Click += BrowseViveToolButton_Click;
        _importConfigButton = new Wpf.Ui.Controls.Button { Content = Resource.ViveTool_ImportConfig };
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
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var statusStack = new StackPanel();
        statusStack.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_ViveToolStatus,
            FontSize = 16,
            FontWeight = FontWeights.Medium
        });
        statusStack.Children.Add(_statusTextBlock);
        statusStack.Children.Add(_downloadProgressGrid);
        statusStack.Children.Add(actionRow);
        statusCard.Child = statusStack;

        var pathCard = new Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14)
        };
        var pathStack = new StackPanel();
        pathStack.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_BinaryPathTitle,
            FontSize = 16,
            FontWeight = FontWeights.Medium,
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
                Filter = GetExecutableDialogFilter(),
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
                Filter = GetImportConfigDialogFilter(),
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

    private static string GetExecutableDialogFilter()
    {
        return GetLocalizedFilter(
            "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            "可執行檔案 (*.exe)|*.exe|所有檔案 (*.*)|*.*");
    }

    private static string GetImportConfigDialogFilter()
    {
        return GetLocalizedFilter(
            "JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            "JSON 文件 (*.json)|*.json|CSV 文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            "JSON 檔案 (*.json)|*.json|CSV 檔案 (*.csv)|*.csv|文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*");
    }

    private static string GetLocalizedFilter(string en, string zhHans, string zhHant)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        if (culture.StartsWith("zh-hans", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-cn", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-sg", StringComparison.OrdinalIgnoreCase))
        {
            return zhHans;
        }

        if (culture.StartsWith("zh-hant", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-tw", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-hk", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-mo", StringComparison.OrdinalIgnoreCase))
        {
            return zhHant;
        }

        return en;
    }
}
