using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.ViveTool.Resources;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using LenovoLegionToolkit.WPF;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Controls;
using MessageBoxHelper = LenovoLegionToolkit.WPF.Utils.MessageBoxHelper;

namespace LenovoLegionToolkit.Plugins.ViveTool;

/// <summary>
/// ViVeTool Page - Windows Feature Flags Management
/// </summary>
public partial class ViveToolPage : INotifyPropertyChanged
{
    private readonly IViveToolService _viveToolService;
    private ObservableCollection<FeatureFlagInfo> _features = new();
    private List<FeatureFlagInfo> _allFeatures = new(); // Cache all features locally for fast searching
    private string _viveToolStatusDescription = string.Empty;
    private string? _viveToolVersion;
    private bool _isLoading;
    private CancellationTokenSource? _searchDebounceCts;

    public ObservableCollection<FeatureFlagInfo> Features
    {
        get => _features;
        set
        {
            _features = value;
            OnPropertyChanged();
        }
    }

    public string ViveToolStatusDescription
    {
        get => _viveToolStatusDescription;
        set
        {
            _viveToolStatusDescription = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            UpdateLoadingVisibility();
        }
    }

    private bool _isViveToolAvailable;

    public bool IsViveToolAvailable
    {
        get => _isViveToolAvailable;
        set
        {
            _isViveToolAvailable = value;
            OnPropertyChanged();
        }
    }
    
    public string? ViveToolVersion
    {
        get => _viveToolVersion;
        set
        {
            _viveToolVersion = value;
            OnPropertyChanged();
        }
    }

    private bool _isDownloading;

    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            _isDownloading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotDownloading));
        }
    }

    public bool IsNotDownloading => !IsDownloading;

    private double _downloadProgress;

    public double DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            _downloadProgress = value;
            OnPropertyChanged();
        }
    }

    private string _downloadProgressTextValue = string.Empty;

    public string DownloadProgressText
    {
        get => _downloadProgressTextValue;
        set
        {
            _downloadProgressTextValue = value;
            OnPropertyChanged();
        }
    }

    private readonly Services.Settings.ViveToolSettings _settings;

    public ViveToolPage()
    {
        InitializeComponent();
        DataContext = this;
        _viveToolService = new ViveToolService();
        _settings = new Services.Settings.ViveToolSettings();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyPluginResourceCulture();
            await _settings.LoadAsync().ConfigureAwait(false);
            await RefreshViveToolStatusAsync();
            await LoadFeaturesAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in Page_Loaded: {ex.Message}", ex);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        // Cancel any pending search debounce
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;
    }

    private void ApplyPluginResourceCulture()
    {
        try
        {
            LocalizationHelper.SetPluginResourceCultures();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error applying plugin resource culture: {ex.Message}", ex);
        }
    }

    private async Task RefreshViveToolStatusAsync()
    {
        try
        {
            var isAvailable = await _viveToolService.IsViveToolAvailableAsync().ConfigureAwait(false);
            var path = await _viveToolService.GetViveToolPathAsync().ConfigureAwait(false);
            string? version = null;
            
            if (isAvailable && !string.IsNullOrEmpty(path))
            {
                version = await _viveToolService.GetViveToolVersionAsync().ConfigureAwait(false);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                IsViveToolAvailable = isAvailable && !string.IsNullOrEmpty(path);
                ViveToolVersion = version;
                if (IsViveToolAvailable)
                {
                    if (!string.IsNullOrEmpty(version))
                    {
                        ViveToolStatusDescription = string.Format(Resource.ViveTool_ViveToolFound, path) + $" (v{version})";
                    }
                    else
                    {
                        ViveToolStatusDescription = string.Format(Resource.ViveTool_ViveToolFound, path);
                    }
                }
                else
                {
                    ViveToolStatusDescription = Resource.ViveTool_ViveToolNotFound;
                    ViveToolVersion = null;
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error refreshing ViveTool status: {ex.Message}", ex);
            
            await Dispatcher.InvokeAsync(() =>
            {
                IsViveToolAvailable = false;
                ViveToolVersion = null;
                ViveToolStatusDescription = Resource.ViveTool_ViveToolError;
            });
        }
    }

    private async Task LoadFeaturesAsync()
    {
        try
        {
            if (!IsViveToolAvailable)
                return;

            IsLoading = true;
            _emptyStatePanel.Visibility = Visibility.Collapsed;

            var features = await _viveToolService.ListFeaturesAsync().ConfigureAwait(false);

            await Dispatcher.InvokeAsync(() =>
            {
                // Update both the visible collection and the local cache
                Features.Clear();
                _allFeatures.Clear();
                
                foreach (var feature in features)
                {
                    Features.Add(feature);
                    _allFeatures.Add(feature);
                }

                UpdateFeaturesVisibility();
                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading features: {ex.Message}", ex);
            
            await Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                UpdateFeaturesVisibility();
            });
        }
    }

    private async void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshViveToolStatusAsync();
    }

    private async void DownloadViveToolButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            IsLoading = true;
            IsDownloading = true;
            DownloadProgress = 0;
            DownloadProgressText = Resource.ViveTool_Downloading;
            _emptyStatePanel.Visibility = Visibility.Collapsed;

            // Create progress reporter
            var progress = new Progress<long>(bytesDownloaded =>
            {
                // Calculate progress percentage (we don't have total size, so we'll use a heuristic)
                // ViVeTool is around 2-3 MB, so we'll assume 3 MB for estimation
                const long estimatedTotalBytes = 3 * 1024 * 1024;
                double percent = Math.Min(100, (bytesDownloaded * 100.0) / estimatedTotalBytes);
                
                DownloadProgress = percent;
                DownloadProgressText = string.Format(Resource.ViveTool_DownloadProgress, 
                    FormatBytes(bytesDownloaded), FormatBytes(estimatedTotalBytes), (int)percent);
            });

            // Download ViVeTool
            var success = await _viveToolService.DownloadViveToolAsync(progress).ConfigureAwait(false);

            await Dispatcher.InvokeAsync(async () =>
            {
                IsLoading = false;
                IsDownloading = false;
                DownloadProgress = 0;
                DownloadProgressText = string.Empty;

                if (success)
                {
                    // Refresh status and load features
                    _ = RefreshViveToolStatusAsync();
                    _ = LoadFeaturesAsync();
                    
                    var path = await _viveToolService.GetViveToolPathAsync();
                    if (!string.IsNullOrEmpty(path))
                    {
                        SnackbarHelper.Show(Resource.ViveTool_DownloadComplete, string.Format(Resource.ViveTool_DownloadCompleteMessage, path));
                    }
                }
                else
                {
                    SnackbarHelper.Show(Resource.ViveTool_Error, Resource.ViveTool_DownloadFailed, SnackbarType.Error);
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error downloading vivetool.exe: {ex.Message}", ex);

            await Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                IsDownloading = false;
                DownloadProgress = 0;
                DownloadProgressText = string.Empty;
                
                SnackbarHelper.Show(
                    Resource.ViveTool_Error,
                    string.Format(Resource.ViveTool_DownloadFailed, ex.Message),
                    SnackbarType.Error);
            });
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB" };
        int i;
        double dblBytes = bytes;

        for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblBytes = bytes / 1024.0;
        }

        return string.Format("{0:0.##} {1}", dblBytes, suffix[i]);
    }

    private async void RefreshListButton_Click(object sender, RoutedEventArgs e)
    {
        // Clear cache to get fresh data
        _viveToolService.ClearFeatureCache();
        await LoadFeaturesAsync();
    }

    private void GoToSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new LenovoLegionToolkit.WPF.Windows.Settings.PluginSettingsWindow(LenovoLegionToolkit.Lib.Plugins.PluginConstants.ViveTool)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            
            // Refresh status after settings window is closed
            _ = RefreshViveToolStatusAsync();
            _ = LoadFeaturesAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error opening plugin settings: {ex.Message}", ex);
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show import options - first ask if user wants to import from file or URL
            var fromFile = await MessageBoxHelper.ShowAsync(
                this,
                Resource.ViveTool_Import,
                Resource.ViveTool_ImportDescription + "\n\n" + Resource.ViveTool_ImportFromFile + " / " + Resource.ViveTool_ImportFromUrl,
                Resource.ViveTool_ImportFromFile,
                Resource.ViveTool_ImportFromUrl);

            if (fromFile)
            {
                // Import from file
                await ImportFromFileAsync();
            }
            else
            {
                // Import from URL
                await ImportFromUrlAsync();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error showing import dialog: {ex.Message}", ex);
        }
    }

    private async Task ImportFromFileAsync()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.ViveTool_ImportFromFile,
                Filter = "All Files (*.*)|*.*|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                _emptyStatePanel.Visibility = Visibility.Collapsed;

                var importedFeatures = await _viveToolService.ImportFeaturesFromFileAsync(openFileDialog.FileName).ConfigureAwait(false);

                await Dispatcher.InvokeAsync(() =>
                {
                    // Merge with existing features (avoid duplicates)
                    foreach (var feature in importedFeatures)
                    {
                        if (!Features.Any(f => f.Id == feature.Id))
                        {
                            Features.Add(feature);
                        }
                    }

                    UpdateFeaturesVisibility();
                    IsLoading = false;

                    SnackbarHelper.Show(
                        Resource.ViveTool_ImportSuccess,
                        string.Format(Resource.ViveTool_ImportSuccessMessage, importedFeatures.Count));
                });
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error importing from file: {ex.Message}", ex);

            await Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                SnackbarHelper.Show(
                    Resource.ViveTool_Error,
                    string.Format(Resource.ViveTool_ImportFailed, ex.Message),
                    SnackbarType.Error);
            });
        }
    }

    private async Task ImportFromUrlAsync()
    {
        try
        {
            // Show URL input dialog
            var url = await MessageBoxHelper.ShowInputAsync(
                this,
                Resource.ViveTool_ImportFromUrl,
                "https://example.com/features.json",
                null,
                Resource.ViveTool_Import,
                Resource.ViveTool_Cancel,
                false);

            if (string.IsNullOrWhiteSpace(url))
                return;

            IsLoading = true;
            _emptyStatePanel.Visibility = Visibility.Collapsed;

            var importedFeatures = await _viveToolService.ImportFeaturesFromUrlAsync(url).ConfigureAwait(false);

            await Dispatcher.InvokeAsync(() =>
            {
                // Merge with existing features (avoid duplicates)
                foreach (var feature in importedFeatures)
                {
                    if (!Features.Any(f => f.Id == feature.Id))
                    {
                        Features.Add(feature);
                    }
                }

                UpdateFeaturesVisibility();
                IsLoading = false;

                SnackbarHelper.Show(
                    Resource.ViveTool_ImportSuccess,
                    string.Format(Resource.ViveTool_ImportSuccessMessage, importedFeatures.Count));
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error importing from URL: {ex.Message}", ex);

            await Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                SnackbarHelper.Show(
                    Resource.ViveTool_Error,
                    string.Format(Resource.ViveTool_ImportFailed, ex.Message),
                    SnackbarType.Error);
            });
        }
    }

    private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Cancel previous debounce
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();

        var cancellationToken = _searchDebounceCts.Token;

        try
        {
            // Debounce search - wait a bit before searching
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                return;

            // Check if textbox is still focused before searching
            await Dispatcher.InvokeAsync(async () =>
            {
                if (_searchTextBox.IsFocused && !cancellationToken.IsCancellationRequested)
                {
                    await SearchFeaturesAsync().ConfigureAwait(false);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when debounce is cancelled - ignore
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions but don't crash the app
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in SearchTextBox_TextChanged debounce: {ex.Message}", ex);
        }
    }

    private async Task SearchFeaturesAsync()
    {
        try
        {
            _emptyStatePanel.Visibility = Visibility.Collapsed;

            // Use local cache for fast searching instead of service calls
            await Dispatcher.InvokeAsync(() =>
            {
                var searchText = _searchTextBox.Text ?? string.Empty;
                var lowerKeyword = searchText.ToLowerInvariant();
                
                Features.Clear();
                
                // Fast in-memory filtering
                foreach (var feature in _allFeatures)
                {
                    if (string.IsNullOrWhiteSpace(lowerKeyword) ||
                        feature.Id.ToString().Contains(lowerKeyword) ||
                        feature.Name.ToLowerInvariant().Contains(lowerKeyword) ||
                        feature.Description.ToLowerInvariant().Contains(lowerKeyword))
                    {
                        Features.Add(feature);
                    }
                }

                UpdateFeaturesVisibility();
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error searching features: {ex.Message}", ex);
            
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateFeaturesVisibility();
            });
        }
    }

    private async void EnableFeatureButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not int featureId)
            return;

        try
        {
            var result = await _viveToolService.EnableFeatureAsync(featureId).ConfigureAwait(false);
            
            await Dispatcher.InvokeAsync(async () =>
            {
                if (result)
                {
                    // Refresh the feature status
                    await RefreshFeatureStatusAsync(featureId);
                    
                    SnackbarHelper.Show(
                        Resource.ViveTool_FeatureEnabled,
                        string.Format(Resource.ViveTool_FeatureEnabledMessage, featureId));
                }
                else
                {
                    SnackbarHelper.Show(
                        Resource.ViveTool_Error,
                        string.Format(Resource.ViveTool_EnableFeatureFailed, featureId),
                        SnackbarType.Error);
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error enabling feature {featureId}: {ex.Message}", ex);
        }
    }

    private async void DisableFeatureButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not int featureId)
            return;

        try
        {
            var result = await _viveToolService.DisableFeatureAsync(featureId).ConfigureAwait(false);
            
            await Dispatcher.InvokeAsync(async () =>
            {
                if (result)
                {
                    // Refresh the feature status
                    await RefreshFeatureStatusAsync(featureId);
                    
                    SnackbarHelper.Show(
                        Resource.ViveTool_FeatureDisabled,
                        string.Format(Resource.ViveTool_FeatureDisabledMessage, featureId));
                }
                else
                {
                    SnackbarHelper.Show(
                        Resource.ViveTool_Error,
                        string.Format(Resource.ViveTool_DisableFeatureFailed, featureId),
                        SnackbarType.Error);
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error disabling feature {featureId}: {ex.Message}", ex);
        }
    }

    private async Task RefreshFeatureStatusAsync(int featureId)
    {
        try
        {
            var status = await _viveToolService.GetFeatureStatusAsync(featureId).ConfigureAwait(false);
            
            await Dispatcher.InvokeAsync(() =>
            {
                var feature = Features.FirstOrDefault(f => f.Id == featureId);
                if (feature != null && status.HasValue)
                {
                    feature.Status = status.Value;
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error refreshing feature status {featureId}: {ex.Message}", ex);
        }
    }

    private void UpdateLoadingVisibility()
    {
        _loadingPanel.Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
        _featuresDataGrid.Visibility = IsLoading ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateFeaturesVisibility()
    {
        if (Features.Count == 0 && !IsLoading)
        {
            _emptyStatePanel.Visibility = Visibility.Visible;
            _featuresDataGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            _emptyStatePanel.Visibility = Visibility.Collapsed;
            _featuresDataGrid.Visibility = Visibility.Visible;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Converter for FeatureFlagStatus enum to display string
/// </summary>
public class FeatureStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FeatureFlagStatus status)
        {
            return status switch
            {
                FeatureFlagStatus.Enabled => Resource.ViveTool_StatusEnabled,
                FeatureFlagStatus.Disabled => Resource.ViveTool_StatusDisabled,
                FeatureFlagStatus.Default => Resource.ViveTool_StatusDefault,
                _ => Resource.ViveTool_StatusUnknown
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
