using System.Diagnostics;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using UniversalDeviceToolkit.Plugins.ViveTool.Resources;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings;
using UniversalDeviceToolkit.Plugins.ViveTool.Utils;
using UniversalDeviceToolkit.Plugins.SDK;

namespace UniversalDeviceToolkit.Plugins.ViveTool;

/// <summary>
/// Avalonia-native ViVeTool feature page. The WPF page remains available from
/// <see cref="ViveToolPluginPage.CreatePage"/>; this control is selected by the
/// Avalonia host through the optional CreateAvaloniaPage factory.
/// </summary>
public sealed class AvaloniaViveToolPage : UserControl
{
    private readonly IViveToolService _service;
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _statusFilter = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _summary = new();
    private readonly TextBlock _loading = new();
    private readonly ProgressBar _downloadProgress = new() { IsVisible = false, Minimum = 0, Maximum = 100, Height = 6 };
    private readonly TextBlock _downloadProgressText = new() { IsVisible = false };
    private readonly StackPanel _featureList = new();
    private readonly Border _missingBanner;
    private readonly Button _downloadButton;
    private readonly Border _emptyState;
    private Button? _goToSettingsButton;
    private Button? _missingGoToSettingsButton;
    private Button? _missingRefreshButton;
    private readonly List<FeatureFlagInfo> _allFeatures = [];
    private bool _loaded;

    public AvaloniaViveToolPage()
        : this(new ViveToolService())
    {
    }

    internal AvaloniaViveToolPage(IViveToolService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        AutomationProperties.SetAutomationId(this, "AvaloniaViveToolPageRoot");

        _searchBox.Watermark = Resource.ViveTool_SearchPlaceholder;
        _searchBox.MinWidth = 180;
        AutomationProperties.SetAutomationId(_searchBox, "AvaloniaViveToolSearchTextBox");
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        _statusFilter.ItemsSource = new object[]
        {
            new FilterOption(Resource.ViveTool_StatusFilterAll, null),
            new FilterOption(Resource.ViveTool_StatusEnabled, FeatureFlagStatus.Enabled),
            new FilterOption(Resource.ViveTool_StatusDisabled, FeatureFlagStatus.Disabled),
            new FilterOption(Resource.ViveTool_StatusDefault, FeatureFlagStatus.Default),
            new FilterOption(Resource.ViveTool_StatusUnknown, FeatureFlagStatus.Unknown),
        };
        _statusFilter.SelectedIndex = 0;
        AutomationProperties.SetAutomationId(_statusFilter, "AvaloniaViveToolStatusFilterComboBox");
        _statusFilter.SelectionChanged += (_, _) => ApplyFilter();

        var refreshButton = ActionButton(Resource.ViveTool_RefreshList, "AvaloniaViveToolRefreshListButton", () => RefreshAsync(clearFeatureCache: true));
        var importButton = ActionButton(Resource.ViveTool_Import, "AvaloniaViveToolImportButton", ImportAsync);
        var exportButton = ActionButton(Resource.ViveTool_Export, "AvaloniaViveToolExportButton", ExportAsync);
        _downloadButton = ActionButton(Resource.ViveTool_Download, "AvaloniaViveToolDownloadButton", DownloadAsync);
        _goToSettingsButton = ActionButton(Resource.ViveTool_GoToSettings, "ViveToolFeatureGoToSettingsButton", GoToSettingsAsync);
        _loading.Text = Resource.ViveTool_Loading;
        _loading.IsVisible = false;
        _loading.Foreground = Brushes.Gray;
        AutomationProperties.SetAutomationId(_loading, "AvaloniaViveToolLoadingText");
        _downloadProgress.Foreground = Brushes.DodgerBlue;
        AutomationProperties.SetAutomationId(_downloadProgress, "AvaloniaViveToolDownloadProgressBar");
        _downloadProgressText.Foreground = Brushes.Gray;
        _downloadProgressText.TextWrapping = TextWrapping.Wrap;
        AutomationProperties.SetAutomationId(_downloadProgressText, "AvaloniaViveToolDownloadProgressText");
        _summary.TextWrapping = TextWrapping.Wrap;
        _summary.Foreground = Brushes.Gray;
        AutomationProperties.SetAutomationId(_summary, "AvaloniaViveToolFeatureSummaryText");
        _status.TextWrapping = TextWrapping.Wrap;
        AutomationProperties.SetAutomationId(_status, "AvaloniaViveToolStatusText");

        var missingActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _missingGoToSettingsButton = ActionButton(Resource.ViveTool_GoToSettings, "ViveToolMissingGoToSettingsButton", GoToSettingsAsync);
        _missingRefreshButton = ActionButton(
            Resource.ViveTool_Refresh,
            "ViveToolMissingRefreshStatusButton",
            () => RefreshAsync());
        missingActions.Children.Add(_downloadButton);
        missingActions.Children.Add(_missingGoToSettingsButton);
        missingActions.Children.Add(_missingRefreshButton);

        _missingBanner = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(28, 255, 165, 0)),
            BorderBrush = Brushes.Orange,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    Heading(Resource.ViveTool_MissingToolMessage),
                    Body(Resource.ViveTool_PathDescription),
                    missingActions,
                    _downloadProgress,
                    _downloadProgressText,
                },
            },
        };

        var toolbar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 8,
            RowSpacing = 8,
        };
        Grid.SetColumn(_searchBox, 0);
        Grid.SetColumn(_statusFilter, 1);
        Grid.SetColumn(_summary, 2);
        toolbar.Children.Add(_searchBox);
        toolbar.Children.Add(_statusFilter);
        toolbar.Children.Add(_summary);
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { importButton, exportButton, refreshButton },
        };
        actions.Children.Add(_goToSettingsButton!);
        Grid.SetRow(actions, 1);
        Grid.SetColumnSpan(actions, 3);
        toolbar.Children.Add(actions);

        _featureList.Spacing = 6;
        AutomationProperties.SetAutomationId(_featureList, "AvaloniaViveToolFeatureList");

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(20, 16, 20, 20) };
        root.Children.Add(Heading(Resource.ViveTool_PageTitle, 20));
        root.Children.Add(Body(Resource.ViveTool_PageDescription));
        root.Children.Add(Card(Resource.ViveTool_ViveToolStatus, _status));
        root.Children.Add(Card(Resource.ViveTool_WarningTitle, Body(Resource.ViveTool_WarningMessage)));
        root.Children.Add(_missingBanner);
        _emptyState = new Border
        {
            IsVisible = false,
            Padding = new Thickness(16, 28),
            Child = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = "?", FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Gray },
                    new TextBlock { Text = Resource.ViveTool_NoFeaturesFound, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Foreground = Brushes.Gray },
                },
            },
        };
        AutomationProperties.SetAutomationId(_emptyState, "ViveToolEmptyStatePanel");
        root.Children.Add(Card(Resource.ViveTool_FeatureFlags, new StackPanel { Spacing = 8, Children = { toolbar, _loading, _featureList, _emptyState } }));
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = root,
        };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;
        _loaded = true;
        await RefreshAsync();
    }

    private async Task RefreshAsync(bool clearFeatureCache = false)
    {
        SetBusy(true);
        try
        {
            if (clearFeatureCache)
                _service.ClearFeatureCache();

            var available = await _service.IsViveToolAvailableAsync().ConfigureAwait(true);
            var path = await _service.GetViveToolPathAsync().ConfigureAwait(true);
            _missingBanner.IsVisible = !available || string.IsNullOrWhiteSpace(path);
            if (_missingBanner.IsVisible)
            {
                _status.Text = Resource.ViveTool_ViveToolNotFound;
                _allFeatures.Clear();
                ApplyFilter();
                return;
            }

            var version = await _service.GetViveToolVersionAsync().ConfigureAwait(true);
            _status.Text = string.Format(
                CultureInfo.CurrentCulture,
                Resource.ViveTool_ViveToolFound,
                string.IsNullOrWhiteSpace(version) ? path : $"{path} (v{version})");
            _allFeatures.Clear();
            _allFeatures.AddRange(await _service.ListFeaturesAsync().ConfigureAwait(true));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _status.Text = $"{Resource.ViveTool_ViveToolError}: {ex.Message}";
            _missingBanner.IsVisible = true;
            _allFeatures.Clear();
            ApplyFilter();
        }
        finally
        {
            SetBusy(false);
            ApplyFilter();
        }
    }

    private async Task DownloadAsync()
    {
        SetBusy(true);
        _downloadProgress.IsVisible = true;
        _downloadProgressText.IsVisible = true;
        _downloadProgress.Value = 0;
        _downloadProgressText.Text = Resource.ViveTool_Downloading;
        try
        {
            const long estimatedTotalBytes = UniversalDeviceToolkit.Plugins.Shared.Constants.EstimatedViveToolDownloadBytes;
            var progress = new Progress<long>(bytesDownloaded =>
            {
                var percent = Math.Min(100, bytesDownloaded * 100d / estimatedTotalBytes);
                _downloadProgress.Value = percent;
                _downloadProgressText.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ViveTool_DownloadProgress,
                    ByteFormatter.FormatBytes(bytesDownloaded),
                    ByteFormatter.FormatBytes(estimatedTotalBytes),
                    (int)percent);
            });
            var success = await _service.DownloadViveToolAsync(progress).ConfigureAwait(true);
            _status.Text = success ? Resource.ViveTool_DownloadComplete : Resource.ViveTool_DownloadFailed;
            if (success)
                await RefreshAsync(clearFeatureCache: true).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _status.Text = $"{Resource.ViveTool_DownloadFailed}: {ex.Message}";
        }
        finally
        {
            _downloadProgress.IsVisible = false;
            _downloadProgressText.IsVisible = false;
            SetBusy(false);
        }
    }

    private async Task ImportAsync()
    {
        var fromFile = await PickImportModeAsync().ConfigureAwait(true);
        if (fromFile is null)
            return;

        var source = fromFile.Value
            ? await PickFileAsync(Resource.ViveTool_ImportFromFile).ConfigureAwait(true)
            : await PickUrlAsync().ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(source))
            return;

        SetBusy(true);
        try
        {
            var imported = fromFile.Value
                ? await _service.ImportFeaturesFromFileAsync(source).ConfigureAwait(true)
                : await _service.ImportFeaturesFromUrlAsync(source).ConfigureAwait(true);
            var existing = _allFeatures.Select(item => item.Id).ToHashSet();
            _allFeatures.AddRange(imported.Where(item => existing.Add(item.Id)));
            _status.Text = string.Format(CultureInfo.CurrentCulture, Resource.ViveTool_ImportSuccessMessage, imported.Count);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _status.Text = $"{Resource.ViveTool_ImportFailed}: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task GoToSettingsAsync()
    {
        if (!PluginHostContextRuntime.Current.OpenPluginSettings("vive-tool"))
            return;

        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task ExportAsync()
    {
        if (_allFeatures.Count == 0)
        {
            _status.Text = Resource.ViveTool_ExportNoFeatures;
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        var file = topLevel is null
            ? null
            : await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Resource.ViveTool_Export,
                SuggestedFileName = "vivetool-features.json",
                DefaultExtension = "json",
            }).ConfigureAwait(true);
        if (file is null)
            return;

        SetBusy(true);
        try
        {
            var success = await _service.ExportFeaturesToFileAsync(file.Path.LocalPath, _allFeatures).ConfigureAwait(true);
            _status.Text = success
                ? string.Format(CultureInfo.CurrentCulture, Resource.ViveTool_ExportSuccessMessage, _allFeatures.Count, file.Path.LocalPath)
                : Resource.ViveTool_ExportFailed;
        }
        catch (Exception ex)
        {
            _status.Text = $"{Resource.ViveTool_ExportFailed}: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        var selected = _statusFilter.SelectedItem as FilterOption;
        var filtered = FeatureFilter.FilterFeatures(_allFeatures, _searchBox.Text, selected?.Status);
        _summary.Text = BuildSummary(filtered);
        _featureList.Children.Clear();
        _featureList.IsVisible = !_loading.IsVisible;
        _emptyState.IsVisible = filtered.Count == 0 && !_loading.IsVisible;
        if (_loading.IsVisible)
            return;

        foreach (var feature in filtered)
            _featureList.Children.Add(BuildFeatureRow(feature));
    }

    private Control BuildFeatureRow(FeatureFlagInfo feature)
    {
        var id = new TextBlock { Text = feature.Id.ToString(CultureInfo.CurrentCulture), MinWidth = 70 };
        var name = new TextBlock { Text = string.IsNullOrWhiteSpace(feature.Name) ? feature.Description : feature.Name, TextWrapping = TextWrapping.Wrap };
        var state = new TextBlock { Text = StatusText(feature.Status), MinWidth = 90, TextWrapping = TextWrapping.Wrap };
        var enable = ActionButton(Resource.ViveTool_Enable, $"AvaloniaViveToolEnableFeatureButton_{feature.Id}", () => ChangeFeatureAsync(feature, true));
        var disable = ActionButton(Resource.ViveTool_Disable, $"AvaloniaViveToolDisableFeatureButton_{feature.Id}", () => ChangeFeatureAsync(feature, false));
        var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { enable, disable } };
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"), ColumnSpacing = 10, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(id, 0);
        Grid.SetColumn(name, 1);
        Grid.SetColumn(state, 2);
        Grid.SetColumn(actionPanel, 3);
        row.Children.Add(id);
        row.Children.Add(name);
        row.Children.Add(state);
        row.Children.Add(actionPanel);
        return new Border
        {
            Padding = new Thickness(10, 8),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new CornerRadius(6),
            Child = row,
        };
    }

    private async Task ChangeFeatureAsync(FeatureFlagInfo feature, bool enable)
    {
        SetBusy(true);
        try
        {
            var success = enable
                ? await _service.EnableFeatureAsync(feature.Id).ConfigureAwait(true)
                : await _service.DisableFeatureAsync(feature.Id).ConfigureAwait(true);
            _status.Text = success
                ? (enable ? Resource.ViveTool_FeatureEnabled : Resource.ViveTool_FeatureDisabled)
                : (enable ? Resource.ViveTool_EnableFeatureFailed : Resource.ViveTool_DisableFeatureFailed);
            if (success)
            {
                feature.Status = enable ? FeatureFlagStatus.Enabled : FeatureFlagStatus.Disabled;
                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            _status.Text = $"{Resource.ViveTool_Error}: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _loading.IsVisible = busy;
        _downloadButton.IsEnabled = !busy;
        _searchBox.IsEnabled = !busy;
        _statusFilter.IsEnabled = !busy;
        _featureList.IsVisible = !busy;
        if (busy)
            _emptyState.IsVisible = false;
    }

    private static string BuildSummary(IReadOnlyList<FeatureFlagInfo> features)
    {
        var summary = FeatureFilter.SummarizeFeatures(features);
        return string.Format(
            CultureInfo.CurrentCulture,
            "{0} total | {1}: {2} | {3}: {4} | {5}: {6} | {7}: {8}",
            summary.Total,
            Resource.ViveTool_StatusEnabled,
            summary.Enabled,
            Resource.ViveTool_StatusDisabled,
            summary.Disabled,
            Resource.ViveTool_StatusDefault,
            summary.Default,
            Resource.ViveTool_StatusUnknown,
            summary.Unknown);
    }

    private static string StatusText(FeatureFlagStatus status) => status switch
    {
        FeatureFlagStatus.Enabled => Resource.ViveTool_StatusEnabled,
        FeatureFlagStatus.Disabled => Resource.ViveTool_StatusDisabled,
        FeatureFlagStatus.Default => Resource.ViveTool_StatusDefault,
        _ => Resource.ViveTool_StatusUnknown,
    };

    private async Task<string?> PickFileAsync(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return null;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        }).ConfigureAwait(true);
        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }

    private async Task<bool?> PickImportModeAsync()
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return null;

        bool? result = null;
        var dialog = new Window
        {
            Title = Resource.ViveTool_Import,
            Width = 480,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var fileButton = new Button { Content = Resource.ViveTool_ImportFromFile, MinWidth = 180, Padding = new Thickness(12, 7) };
        var urlButton = new Button { Content = Resource.ViveTool_ImportFromUrl, MinWidth = 180, Padding = new Thickness(12, 7) };
        var cancelButton = new Button { Content = Resource.ViveTool_Cancel, MinWidth = 100, Padding = new Thickness(12, 7) };
        AutomationProperties.SetAutomationId(fileButton, "ViveToolImportFromFileButton");
        AutomationProperties.SetAutomationId(urlButton, "ViveToolImportFromUrlButton");
        AutomationProperties.SetAutomationId(cancelButton, "ViveToolImportCancelButton");
        fileButton.Click += (_, _) => { result = true; dialog.Close(); };
        urlButton.Click += (_, _) => { result = false; dialog.Close(); };
        cancelButton.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(20),
            Children =
            {
                Body(Resource.ViveTool_ImportDescription),
                fileButton,
                urlButton,
                cancelButton,
            },
        };
        await dialog.ShowDialog(owner).ConfigureAwait(true);
        return result;
    }

    private async Task<string?> PickUrlAsync()
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return null;

        string? result = null;
        var dialog = new Window
        {
            Title = Resource.ViveTool_ImportFromUrl,
            Width = 560,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var input = new TextBox
        {
            Watermark = "https://example.com/features.json",
            MinWidth = 500,
        };
        var importButton = new Button { Content = Resource.ViveTool_Import, MinWidth = 120, Padding = new Thickness(12, 7) };
        var cancelButton = new Button { Content = Resource.ViveTool_Cancel, MinWidth = 100, Padding = new Thickness(12, 7) };
        AutomationProperties.SetAutomationId(input, "ViveToolImportUrlTextBox");
        AutomationProperties.SetAutomationId(importButton, "ViveToolImportUrlConfirmButton");
        AutomationProperties.SetAutomationId(cancelButton, "ViveToolImportUrlCancelButton");
        importButton.Click += (_, _) =>
        {
            if (!Uri.TryCreate(input.Text?.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                input.BorderBrush = Brushes.IndianRed;
                return;
            }

            result = uri.ToString();
            dialog.Close();
        };
        cancelButton.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(20),
            Children =
            {
                Body(Resource.ViveTool_ImportUrlDescription),
                input,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { importButton, cancelButton },
                },
            },
        };
        await dialog.ShowDialog(owner).ConfigureAwait(true);
        return result;
    }

    private static Button ActionButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 96 };
        AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static TextBlock Heading(string text, double size = 16) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = FontWeight.SemiBold,
        TextWrapping = TextWrapping.Wrap,
    };

    private static TextBlock Body(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.Gray,
    };

    private static Border Card(string title, Control content)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(Heading(title));
        stack.Children.Add(content);
        return new Border
        {
            Padding = new Thickness(14),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new CornerRadius(8),
            Child = stack,
        };
    }

    private sealed record FilterOption(string Label, FeatureFlagStatus? Status)
    {
        public override string ToString() => Label;
    }
}

/// <summary>
/// Avalonia-native ViVeTool settings page. File operations use Avalonia's
/// storage provider and therefore work in the desktop host without WPF dialogs.
/// </summary>
public sealed class AvaloniaViveToolSettingsPage : UserControl
{
    private readonly IViveToolService _service;
    private readonly ViveToolSettings _settings = new();
    private readonly TextBlock _status = new();
    private readonly TextBox _path = new();
    private readonly ProgressBar _progress = new() { IsVisible = false, Minimum = 0, Maximum = 100, Height = 6 };
    private readonly TextBlock _progressText = new() { IsVisible = false };
    private bool _loaded;

    public AvaloniaViveToolSettingsPage()
        : this(new ViveToolService())
    {
    }

    internal AvaloniaViveToolSettingsPage(IViveToolService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        AutomationProperties.SetAutomationId(this, "AvaloniaViveToolSettingsRoot");
        _status.TextWrapping = TextWrapping.Wrap;
        AutomationProperties.SetAutomationId(_status, "AvaloniaViveToolSettingsStatusText");
        _path.IsReadOnly = true;
        _path.Watermark = Resource.ViveTool_PathPlaceholder;
        AutomationProperties.SetAutomationId(_path, "AvaloniaViveToolSettingsPathTextBox");
        _progress.Foreground = Brushes.DodgerBlue;
        AutomationProperties.SetAutomationId(_progress, "AvaloniaViveToolSettingsDownloadProgressBar");
        _progressText.Foreground = Brushes.Gray;
        AutomationProperties.SetAutomationId(_progressText, "AvaloniaViveToolSettingsDownloadProgressText");

        var refresh = ActionButton(Resource.ViveTool_Refresh, "AvaloniaViveToolSettingsRefreshStatusButton", RefreshStatusAsync);
        var download = ActionButton(Resource.ViveTool_Download, "AvaloniaViveToolSettingsDownloadButton", DownloadAsync);
        var github = ActionButton(Resource.ViveTool_GitHub, "AvaloniaViveToolSettingsGitHubButton", OpenGitHubAsync);
        var browse = ActionButton(Resource.ViveTool_Browse, "AvaloniaViveToolSettingsBrowseButton", BrowseAsync);
        var import = ActionButton(Resource.ViveTool_ImportConfig, "AvaloniaViveToolSettingsImportConfigButton", ImportConfigAsync);

        var pathGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8 };
        Grid.SetColumn(_path, 0);
        Grid.SetColumn(browse, 1);
        Grid.SetColumn(import, 2);
        pathGrid.Children.Add(_path);
        pathGrid.Children.Add(browse);
        pathGrid.Children.Add(import);

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(20, 16, 20, 20) };
        root.Children.Add(Heading(Resource.ViveTool_ViveToolStatus, 18));
        root.Children.Add(Card(new StackPanel
        {
            Spacing = 8,
            Children =
            {
                _status,
                _progress,
                _progressText,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { download, github, refresh } },
            },
        }));
        root.Children.Add(Heading(Resource.ViveTool_BinaryPathTitle));
        root.Children.Add(Body(Resource.ViveTool_PathDescription));
        root.Children.Add(pathGrid);
        root.Children.Add(new Border
        {
            Padding = new Thickness(10, 8),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Orange,
            Background = new SolidColorBrush(Color.FromArgb(28, 255, 165, 0)),
            CornerRadius = new CornerRadius(6),
            Child = Body(Resource.ViveTool_WarningMessage),
        });
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = root,
        };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;
        _loaded = true;
        await _settings.LoadAsync().ConfigureAwait(true);
        _path.Text = _settings.ViveToolPath ?? string.Empty;
        await RefreshStatusAsync().ConfigureAwait(true);
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            var available = await _service.IsViveToolAvailableAsync().ConfigureAwait(true);
            var path = await _service.GetViveToolPathAsync().ConfigureAwait(true);
            _path.Text = path ?? _settings.ViveToolPath ?? string.Empty;
            _status.Text = available && !string.IsNullOrWhiteSpace(path)
                ? string.Format(CultureInfo.CurrentCulture, Resource.ViveTool_ViveToolFound, path)
                : Resource.ViveTool_ViveToolNotFound;
            _status.Foreground = available ? Brushes.SeaGreen : Brushes.OrangeRed;
        }
        catch (Exception ex)
        {
            _status.Text = $"{Resource.ViveTool_ViveToolError}: {ex.Message}";
            _status.Foreground = Brushes.OrangeRed;
        }
    }

    private async Task BrowseAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Resource.ViveTool_SelectViveTool,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Executable files (*.exe)") { Patterns = ["*.exe"] }],
        }).ConfigureAwait(true);
        if (files.Count == 0)
            return;

        var path = files[0].Path.LocalPath;
        if (!string.Equals(Path.GetFileName(path), ViveToolPathService.ViveToolExeName, StringComparison.OrdinalIgnoreCase))
        {
            _status.Text = Resource.ViveTool_InvalidViveToolFile;
            _status.Foreground = Brushes.OrangeRed;
            return;
        }

        if (await _service.SetViveToolPathAsync(path).ConfigureAwait(true))
        {
            _settings.ViveToolPath = path;
            _path.Text = path;
            await RefreshStatusAsync().ConfigureAwait(true);
        }
        else
        {
            _status.Text = Resource.ViveTool_SetPathFailed;
            _status.Foreground = Brushes.OrangeRed;
        }
    }

    private async Task DownloadAsync()
    {
        _progress.IsVisible = true;
        _progressText.IsVisible = true;
        _progressText.Text = Resource.ViveTool_Downloading;
        try
        {
            var progress = new Progress<long>(bytes =>
            {
                const long total = UniversalDeviceToolkit.Plugins.Shared.Constants.EstimatedViveToolDownloadBytes;
                _progress.Value = Math.Min(100, bytes * 100d / total);
                _progressText.Text = string.Format(CultureInfo.CurrentCulture, Resource.ViveTool_DownloadProgress, ByteFormatter.FormatBytes(bytes), ByteFormatter.FormatBytes(total), (int)_progress.Value);
            });
            var success = await _service.DownloadViveToolAsync(progress).ConfigureAwait(true);
            _progressText.Text = success ? Resource.ViveTool_DownloadComplete : Resource.ViveTool_DownloadFailed;
            await RefreshStatusAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _progressText.Text = $"{Resource.ViveTool_DownloadFailed}: {ex.Message}";
        }
        finally
        {
            _progress.IsVisible = false;
            _progressText.IsVisible = false;
        }
    }

    private async Task ImportConfigAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Resource.ViveTool_ImportConfigTitle,
            AllowMultiple = false,
        }).ConfigureAwait(true);
        if (files.Count == 0)
            return;
        try
        {
            var imported = await _service.ImportFeaturesFromFileAsync(files[0].Path.LocalPath).ConfigureAwait(true);
            _status.Text = string.Format(CultureInfo.CurrentCulture, Resource.ViveTool_ConfigImportSuccessMessage, imported.Count, files[0].Path.LocalPath);
            _status.Foreground = Brushes.SeaGreen;
        }
        catch (Exception ex)
        {
            _status.Text = $"{Resource.ViveTool_ConfigImportFailedMessage}: {ex.Message}";
            _status.Foreground = Brushes.OrangeRed;
        }
    }

    private static Task OpenGitHubAsync()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/thebookisclosed/ViVe/releases") { UseShellExecute = true });
        }
        catch
        {
            // Opening a browser is best effort and should not break settings.
        }
        return Task.CompletedTask;
    }

    private static Button ActionButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 96 };
        AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static TextBlock Heading(string text, double size = 16) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = FontWeight.SemiBold,
        TextWrapping = TextWrapping.Wrap,
    };

    private static TextBlock Body(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.Gray,
    };

    private static Border Card(Control content) => new()
    {
        Padding = new Thickness(14),
        BorderThickness = new Thickness(1),
        BorderBrush = Brushes.Gray,
        CornerRadius = new CornerRadius(8),
        Child = content,
    };
}
