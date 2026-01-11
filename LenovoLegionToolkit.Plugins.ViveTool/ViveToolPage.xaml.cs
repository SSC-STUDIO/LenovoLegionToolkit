using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using LenovoLegionToolkit.WPF;
using LenovoLegionToolkit.WPF.Utils;
using Microsoft.Win32;
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
    private string _viveToolStatusDescription = string.Empty;
    private bool _isLoading;

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
        // Cleanup if needed
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

            await Dispatcher.InvokeAsync(() =>
            {
                if (isAvailable && !string.IsNullOrEmpty(path))
                {
                    ViveToolStatusDescription = string.Format(Resource.ViveTool_ViveToolFound, path);
                }
                else
                {
                    ViveToolStatusDescription = Resource.ViveTool_ViveToolNotFound;
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error refreshing ViveTool status: {ex.Message}", ex);
            
            await Dispatcher.InvokeAsync(() =>
            {
                ViveToolStatusDescription = Resource.ViveTool_ViveToolError;
            });
        }
    }

    private async Task LoadFeaturesAsync()
    {
        try
        {
            IsLoading = true;
            _emptyStatePanel.Visibility = Visibility.Collapsed;

            var features = await _viveToolService.ListFeaturesAsync().ConfigureAwait(false);

            await Dispatcher.InvokeAsync(() =>
            {
                Features.Clear();
                foreach (var feature in features)
                {
                    Features.Add(feature);
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

    private async void BrowseViveToolButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = Resource.ViveTool_SelectViveTool,
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Set initial directory if we have a current path
            var currentPath = await _viveToolService.GetViveToolPathAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
            {
                var directory = Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrEmpty(directory))
                    openFileDialog.InitialDirectory = directory;
            }

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedPath = openFileDialog.FileName;
                var fileName = Path.GetFileName(selectedPath);
                
                // Verify it's vivetool.exe
                if (!fileName.Equals("vivetool.exe", StringComparison.OrdinalIgnoreCase))
                {
                    SnackbarHelper.Show(
                        Resource.ViveTool_Error,
                        Resource.ViveTool_InvalidViveToolFile,
                        SnackbarType.Error);
                    return;
                }

                // Set the path
                var success = await _viveToolService.SetViveToolPathAsync(selectedPath).ConfigureAwait(false);
                
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (success)
                    {
                        await RefreshViveToolStatusAsync();
                        SnackbarHelper.Show(
                            Resource.ViveTool_PathSet,
                            string.Format(Resource.ViveTool_PathSetMessage, selectedPath));
                    }
                    else
                    {
                        SnackbarHelper.Show(
                            Resource.ViveTool_Error,
                            Resource.ViveTool_SetPathFailed,
                            SnackbarType.Error);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error browsing for vivetool.exe: {ex.Message}", ex);

            await Dispatcher.InvokeAsync(() =>
            {
                SnackbarHelper.Show(
                    Resource.ViveTool_Error,
                    string.Format(Resource.ViveTool_SetPathFailed, ex.Message),
                    SnackbarType.Error);
            });
        }
    }

    private async void RefreshListButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadFeaturesAsync();
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
            var openFileDialog = new OpenFileDialog
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

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await SearchFeaturesAsync();
    }

    private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Debounce search - wait a bit before searching
        await Task.Delay(500);
        if (_searchTextBox.IsFocused)
        {
            await SearchFeaturesAsync();
        }
    }

    private async Task SearchFeaturesAsync()
    {
        try
        {
            IsLoading = true;
            _emptyStatePanel.Visibility = Visibility.Collapsed;

            var searchText = _searchTextBox.Text ?? string.Empty;
            var features = await _viveToolService.SearchFeaturesAsync(searchText).ConfigureAwait(false);

            await Dispatcher.InvokeAsync(() =>
            {
                Features.Clear();
                foreach (var feature in features)
                {
                    Features.Add(feature);
                }

                UpdateFeaturesVisibility();
                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error searching features: {ex.Message}", ex);
            
            await Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
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
                FeatureFlagStatus.Enabled => "Enabled",
                FeatureFlagStatus.Disabled => "Disabled",
                FeatureFlagStatus.Default => "Default",
                _ => "Unknown"
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
