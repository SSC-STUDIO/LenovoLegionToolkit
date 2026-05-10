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
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.Shared;
using LenovoLegionToolkit.Plugins.ViveTool.Resources;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using LenovoLegionToolkit.Plugins.ViveTool.Utils;
using LenovoLegionToolkit.WPF;
using LenovoLegionToolkit.WPF.Utils;
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
    private string _featureCountDescription = string.Empty;
    private string _viveToolPath = string.Empty;
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

    public string FeatureCountDescription
    {
        get => _featureCountDescription;
        set
        {
            _featureCountDescription = value;
            OnPropertyChanged();
            UpdateFeatureSummary();
        }
    }

    public string ViveToolPath
    {
        get => _viveToolPath;
        set
        {
            _viveToolPath = value;
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
        DataContext = this;
        var initialized = WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
        _viveToolService = new ViveToolService();
        _settings = new Services.Settings.ViveToolSettings();

        if (!initialized)
        {
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }
    }

    private void BuildFallbackUi()
    {
        var availabilityVisibilityConverter = new BooleanToVisibilityConverter();
        var inverseAvailabilityVisibilityConverter =
            new LenovoLegionToolkit.Plugins.ViveTool.Utils.InverseBooleanToVisibilityConverter();

        _searchTextBox = new Wpf.Ui.Controls.TextBox
        {
            PlaceholderText = Resource.ViveTool_SearchPlaceholder
        };
        AutomationProperties.SetAutomationId(_searchTextBox, "ViveToolSearchTextBox");
        _searchTextBox.TextChanged += SearchTextBox_TextChanged;

        _importButton = new Wpf.Ui.Controls.Button
        {
            Content = Resource.ViveTool_Import
        };
        AutomationProperties.SetAutomationId(_importButton, "ViveToolImportButton");
        _importButton.Click += ImportButton_Click;

        _refreshListButton = new Wpf.Ui.Controls.Button
        {
            Content = Resource.ViveTool_RefreshList
        };
        AutomationProperties.SetAutomationId(_refreshListButton, "ViveToolRefreshListButton");
        _refreshListButton.Click += RefreshListButton_Click;

        var settingsButton = new Wpf.Ui.Controls.Button
        {
            Content = Resource.ViveTool_GoToSettings
        };
        AutomationProperties.SetAutomationId(settingsButton, "ViveToolFeatureGoToSettingsButton");
        settingsButton.Click += GoToSettingsButton_Click;

        var missingSettingsButton = new Wpf.Ui.Controls.Button
        {
            Content = Resource.ViveTool_GoToSettings,
            Margin = new Thickness(0, 0, 8, 0)
        };
        AutomationProperties.SetAutomationId(missingSettingsButton, "ViveToolMissingGoToSettingsButton");
        missingSettingsButton.Click += GoToSettingsButton_Click;

        var missingRefreshStatusButton = new Wpf.Ui.Controls.Button
        {
            Content = Resource.ViveTool_Refresh
        };
        AutomationProperties.SetAutomationId(missingRefreshStatusButton, "ViveToolMissingRefreshStatusButton");
        missingRefreshStatusButton.Click += RefreshStatusButton_Click;

        _loadingPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Visibility = Visibility.Collapsed
        };
        AutomationProperties.SetAutomationId(_loadingPanel, "ViveToolLoadingPanel");
        _loadingPanel.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_Loading,
            Margin = new Thickness(0, 0, 0, 8)
        });

        _featuresDataGrid = new System.Windows.Controls.DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true
        };
        AutomationProperties.SetAutomationId(_featuresDataGrid, "ViveToolFeaturesDataGrid");
        _featuresDataGrid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(Features)));
        _featuresDataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Resource.ViveTool_FeatureId,
            Binding = new Binding(nameof(FeatureFlagInfo.Id))
        });
        _featuresDataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Resource.ViveTool_FeatureName,
            Binding = new Binding(nameof(FeatureFlagInfo.Name))
        });
        _featuresDataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Resource.ViveTool_Status,
            Binding = new Binding(nameof(FeatureFlagInfo.Status))
            {
                Converter = new FeatureStatusConverter()
            }
        });
        _featuresDataGrid.Columns.Add(new DataGridTemplateColumn
        {
            Header = Resource.ViveTool_Actions,
            CellTemplate = BuildFeatureActionsTemplate()
        });

        _emptyStatePanel = new StackPanel
        {
            Visibility = Visibility.Collapsed
        };
        AutomationProperties.SetAutomationId(_emptyStatePanel, "ViveToolEmptyStatePanel");
        _emptyStatePanel.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_NoFeaturesFound,
            TextWrapping = TextWrapping.Wrap
        });

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };
        buttonRow.Children.Add(_importButton);
        buttonRow.Children.Add(_refreshListButton);
        buttonRow.Children.Add(settingsButton);

        var missingToolButtonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0)
        };
        missingToolButtonRow.Children.Add(missingSettingsButton);
        missingToolButtonRow.Children.Add(missingRefreshStatusButton);

        var missingToolPanel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        missingToolPanel.SetBinding(
            UIElement.VisibilityProperty,
            new Binding(nameof(IsViveToolAvailable))
            {
                Converter = inverseAvailabilityVisibilityConverter
            });
        missingToolPanel.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_MissingToolMessage,
            TextWrapping = TextWrapping.Wrap
        });
        missingToolPanel.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_PathDescription,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });
        missingToolPanel.Children.Add(missingToolButtonRow);

        var featurePanel = new StackPanel();
        featurePanel.SetBinding(
            UIElement.VisibilityProperty,
            new Binding(nameof(IsViveToolAvailable))
            {
                Converter = availabilityVisibilityConverter
            });
        featurePanel.Children.Add(buttonRow);
        featurePanel.Children.Add(_searchTextBox);
        featurePanel.Children.Add(_loadingPanel);
        featurePanel.Children.Add(_featuresDataGrid);
        featurePanel.Children.Add(_emptyStatePanel);

        var root = new StackPanel
        {
            Margin = new Thickness(16)
        };
        AutomationProperties.SetAutomationId(this, "ViveToolPageRoot");
        AutomationProperties.SetAutomationId(root, "ViveToolPageRoot");
        root.Children.Add(new TextBlock
        {
            Text = Resource.ViveTool_PageDescription,
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        });
        root.Children.Add(missingToolPanel);
        root.Children.Add(featurePanel);

        Content = root;
        UpdateFeatureSummary();
        UpdateFeaturesVisibility();
    }

    private DataTemplate BuildFeatureActionsTemplate()
    {
        var actionsPanel = new FrameworkElementFactory(typeof(StackPanel));
        actionsPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        actionsPanel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        actionsPanel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var enableButton = BuildFeatureActionButtonFactory(
            Resource.ViveTool_Enable,
            "ViveToolEnableFeatureButton_{0}",
            EnableFeatureButton_Click);
        enableButton.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        actionsPanel.AppendChild(enableButton);

        actionsPanel.AppendChild(BuildFeatureActionButtonFactory(
            Resource.ViveTool_Disable,
            "ViveToolDisableFeatureButton_{0}",
            DisableFeatureButton_Click));

        return new DataTemplate(typeof(FeatureFlagInfo))
        {
            VisualTree = actionsPanel
        };
    }

    private FrameworkElementFactory BuildFeatureActionButtonFactory(
        string content,
        string automationIdFormat,
        RoutedEventHandler clickHandler)
    {
        var button = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
        button.SetValue(ContentControl.ContentProperty, content);
        button.SetValue(Control.FontSizeProperty, 12d);
        button.SetValue(Control.PaddingProperty, new Thickness(10, 4, 10, 4));
        button.SetBinding(FrameworkElement.TagProperty, new Binding(nameof(FeatureFlagInfo.Id)));
        button.SetBinding(
            AutomationProperties.AutomationIdProperty,
            new Binding(nameof(FeatureFlagInfo.Id))
            {
                StringFormat = automationIdFormat
            });
        button.AddHandler(
            System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
            clickHandler);

        return button;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyPluginResourceCulture();
            await _settings.LoadAsync();
            await RefreshViveToolStatusAsync();
            await LoadFeaturesAsync();
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error in Page_Loaded: {ex.Message}", ex);
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
            PluginLog.Trace($"Error applying plugin resource culture: {ex.Message}", ex);
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
                    ViveToolPath = path ?? string.Empty;
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
                    ViveToolPath = Resource.ViveTool_ViveToolNotFound;
                    ViveToolStatusDescription = Resource.ViveTool_ViveToolNotFound;
                    ViveToolVersion = null;
                }
            });
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error refreshing ViveTool status: {ex.Message}", ex);
            
            await Dispatcher.InvokeAsync(() =>
            {
                IsViveToolAvailable = false;
                ViveToolPath = Resource.ViveTool_ViveToolNotFound;
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

            await Dispatcher.InvokeAsync(() =>
            {
                IsLoading = true;
                _emptyStatePanel.Visibility = Visibility.Collapsed;
            });

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

                FeatureCountDescription = Features.Count.ToString(CultureInfo.CurrentCulture);
                UpdateFeaturesVisibility();
                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error loading features: {ex.Message}", ex);
            
            await Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                UpdateFeaturesVisibility();
            });
        }
    }

    private async void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshPageAsync(clearFeatureCache: false);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"RefreshStatusButton_Click error: {ex.Message}", ex);
        }
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
                const long estimatedTotalBytes = LenovoLegionToolkit.Plugins.Shared.Constants.EstimatedViveToolDownloadBytes;
                double percent = Math.Min(100, (bytesDownloaded * 100.0) / estimatedTotalBytes);
                
                DownloadProgress = percent;
                DownloadProgressText = string.Format(Resource.ViveTool_DownloadProgress,
                    ByteFormatter.FormatBytes(bytesDownloaded), ByteFormatter.FormatBytes(estimatedTotalBytes), (int)percent);
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
            PluginLog.Trace($"Error downloading vivetool.exe: {ex.Message}", ex);

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

    private async void RefreshListButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshPageAsync(clearFeatureCache: true);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"RefreshListButton_Click error: {ex.Message}", ex);
        }
    }

    private async void GoToSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!SDK.PluginHostContext.Current.OpenPluginSettings(PluginConstants.ViveTool))
                return;
            
            // Refresh status after settings window is closed
            await RefreshPageAsync(clearFeatureCache: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error opening plugin settings: {ex.Message}", ex);
        }
    }

    private async Task RefreshPageAsync(bool clearFeatureCache)
    {
        if (clearFeatureCache)
            _viveToolService.ClearFeatureCache();

        await RefreshViveToolStatusAsync();

        if (IsViveToolAvailable)
        {
            await LoadFeaturesAsync();
            return;
        }

        await Dispatcher.InvokeAsync(() =>
        {
            Features.Clear();
            _allFeatures.Clear();
            FeatureCountDescription = 0.ToString(CultureInfo.CurrentCulture);
            UpdateFeaturesVisibility();
        });
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
            PluginLog.Trace($"Error showing import dialog: {ex.Message}", ex);
        }
    }

    private async Task ImportFromFileAsync()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.ViveTool_ImportFromFile,
                Filter = Resource.ResourceManager.GetString("ViveTool_ImportFileDialogFilter", Resource.Culture ?? CultureInfo.CurrentUICulture)
                    ?? "All Files (*.*)|*.*|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                _emptyStatePanel.Visibility = Visibility.Collapsed;

                var importedFeatures = await _viveToolService.ImportFeaturesFromFileAsync(openFileDialog.FileName).ConfigureAwait(false);

                await Dispatcher.InvokeAsync(() =>
                {
                    FeatureMerger.MergeImportedFeatures(Features, _allFeatures, importedFeatures);

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
            PluginLog.Trace($"Error importing from file: {ex.Message}", ex);

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
                FeatureMerger.MergeImportedFeatures(Features, _allFeatures, importedFeatures);

                UpdateFeaturesVisibility();
                IsLoading = false;

                SnackbarHelper.Show(
                    Resource.ViveTool_ImportSuccess,
                    string.Format(Resource.ViveTool_ImportSuccessMessage, importedFeatures.Count));
            });
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error importing from URL: {ex.Message}", ex);

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

            await SearchFeaturesAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when debounce is cancelled - ignore
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions but don't crash the app
            PluginLog.Trace($"Error in SearchTextBox_TextChanged debounce: {ex.Message}", ex);
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
                var filteredFeatures = FeatureFilter.FilterFeatures(_allFeatures, _searchTextBox.Text);

                Features.Clear();

                foreach (var feature in filteredFeatures)
                {
                    Features.Add(feature);
                }

                FeatureCountDescription = Features.Count.ToString(CultureInfo.CurrentCulture);
                UpdateFeaturesVisibility();
            });
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error searching features: {ex.Message}", ex);
            
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
            PluginLog.Trace($"Error enabling feature {featureId}: {ex.Message}", ex);
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
            PluginLog.Trace($"Error disabling feature {featureId}: {ex.Message}", ex);
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
            PluginLog.Trace($"Error refreshing feature status {featureId}: {ex.Message}", ex);
        }
    }

    private void UpdateLoadingVisibility()
    {
        _loadingPanel.Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
        _featuresDataGrid.Visibility = IsLoading ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateFeatureSummary()
    {
        if (_featureCountTextBlock is null)
            return;

        _featureCountTextBlock.Text = string.IsNullOrWhiteSpace(FeatureCountDescription)
            ? "0"
            : FeatureCountDescription;
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
        return Binding.DoNothing;
    }
}
