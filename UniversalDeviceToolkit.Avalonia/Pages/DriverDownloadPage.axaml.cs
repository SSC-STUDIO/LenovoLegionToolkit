using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DriverDownloadPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly DispatcherTimer _queueRefreshTimer;
    private IReadOnlyList<DriverPackageItem> _packages = [];
    private readonly HashSet<string> _selectedPackageIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly string[] _sources = ["Vantage", "PCSupport"];
    private readonly string[] _operatingSystems = ["Windows11", "Windows10", "Windows8", "Windows7"];
    private bool _isApplying;
    private bool _isRefreshingQueue;
    private bool _isStateUpdate;
    private DriverDownloadState? _state;

    public DriverDownloadPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
        SourceComboBox.ItemsSource = _sources;
        OsComboBox.ItemsSource = _operatingSystems;
        OsComboBox.SelectedIndex = 0;
        _queueRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _queueRefreshTimer.Tick += QueueRefreshTimer_Tick;
        Loaded += OnLoaded;
        DetachedFromVisualTree += (_, _) => _queueRefreshTimer.Stop();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        ApplyState(await _platformServices.GetDriverDownloadStateAsync(), updateInputs: true);
        _queueRefreshTimer.Start();
    }

    private async void QueueRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_isRefreshingQueue || _isApplying)
            return;

        _isRefreshingQueue = true;
        try
        {
            ApplyState(await _platformServices.GetDriverDownloadStateAsync());
        }
        finally
        {
            _isRefreshingQueue = false;
        }
    }

    private async void ScanButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            var source = SourceComboBox.SelectedItem?.ToString() ?? _sources[0];
            var os = OsComboBox.SelectedItem?.ToString() ?? _operatingSystems[0];
            StateText.Text = AvaloniaLocalization.GetString("WindowsOptimizationPage_ScanningDrivers", "Scanning driver downloads...");
            var state = await _platformServices.SearchDriverPackagesAsync(
                source,
                MachineTypeTextBox.Text ?? string.Empty,
                os,
                OnlyUpdatesCheckBox.IsChecked == true);
            ApplyState(state, updateInputs: true);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async void DownloadPathTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (_isStateUpdate)
            return;

        await PersistDownloadPathAsync();
    }

    private async void BrowseDownloadPathButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = AvaloniaLocalization.GetString("PackagesPage_DownloadTo", "Download to"),
        });
        var path = folder.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        DownloadPathTextBox.Text = path;
        await PersistDownloadPathAsync();
    }

    private void OpenDownloadPathButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = DownloadPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            StateText.Text = AvaloniaLocalization.GetString("PackagesPage_DownloadFailed_Message", "Choose an existing download folder first.");
            return;
        }

        try
        {
            using var _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StateText.Text = ex.Message;
        }
    }

    private async Task PersistDownloadPathAsync()
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            ApplyState(await _platformServices.SetDriverDownloadPathAsync(DownloadPathTextBox.Text ?? string.Empty));
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async void SelectRecommendedButton_Click(object? sender, RoutedEventArgs e) =>
        await ApplyActionAsync(() => _platformServices.SelectRecommendedDriverPackagesAsync());

    private async void StartPauseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_state?.IsQueueRunning == true)
            await ApplyActionAsync(() => _platformServices.PauseDriverDownloadsAsync());
        else
            await ApplyActionAsync(() => _platformServices.StartSelectedDriverPackagesAsync());
    }

    private async void HideSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPackageIds.Count == 0)
            return;

        await ApplyActionAsync(() => _platformServices.HideDriverPackagesAsync(_selectedPackageIds.ToArray()));
    }

    private async void RestoreHiddenButton_Click(object? sender, RoutedEventArgs e) =>
        await ApplyActionAsync(() => _platformServices.RestoreHiddenDriverPackagesAsync());

    private async Task ApplyActionAsync(Func<Task<DriverDownloadState>> action)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            ApplyState(await action());
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void ApplyState(DriverDownloadState state, bool updateInputs = false)
    {
        _state = state;
        _packages = state.Packages;
        _selectedPackageIds.Clear();
        foreach (var package in state.Packages.Where(package => package.IsSelected))
            _selectedPackageIds.Add(package.Id);

        _isStateUpdate = true;
        try
        {
            if (updateInputs)
            {
                MachineTypeTextBox.Text = state.MachineType;
                if (!string.IsNullOrWhiteSpace(state.Source))
                    SourceComboBox.SelectedItem = _sources.FirstOrDefault(source => source.Equals(state.Source, StringComparison.OrdinalIgnoreCase)) ?? _sources[0];
                if (!string.IsNullOrWhiteSpace(state.Os))
                    OsComboBox.SelectedItem = _operatingSystems.FirstOrDefault(os => os.Equals(state.Os, StringComparison.OrdinalIgnoreCase)) ?? _operatingSystems[0];
                OnlyUpdatesCheckBox.IsChecked = state.OnlyShowUpdates;
            }

            if (!string.IsNullOrWhiteSpace(state.DownloadPath))
                DownloadPathTextBox.Text = state.DownloadPath;
        }
        finally
        {
            _isStateUpdate = false;
        }

        StateText.Text = state.Error ?? GetStateText(state);
        StartPauseButton.Content = AvaloniaLocalization.GetString(
            state.IsQueueRunning ? "WindowsOptimizationPage_PauseAll_Button" : "WindowsOptimizationPage_StartAll_Button",
            state.IsQueueRunning ? "Pause selected" : "Start selected");
        RestoreHiddenButton.IsVisible = state.HiddenPackageCount > 0;
        RenderVisiblePackages();
    }

    private string GetStateText(DriverDownloadState state)
    {
        var selected = state.Packages.Count(package => package.IsSelected);
        var completed = state.Packages.Count(package => package.Status == DriverPackageStatus.Completed);
        if (state.IsQueueRunning)
            return string.Format(AvaloniaLocalization.GetString("WindowsOptimizationPage_DriverQueue_Running", "Downloading {0} selected package(s)."), selected);
        if (completed > 0)
            return string.Format(AvaloniaLocalization.GetString("WindowsOptimizationPage_DriverQueue_Completed", "{0} package(s) downloaded."), completed);
        return state.Packages.Count == 0
            ? AvaloniaLocalization.GetString("WindowsOptimizationPage_DriverEmpty_NotScanned_Message", "Choose a source and scan to list compatible driver downloads.")
            : string.Format(AvaloniaLocalization.GetString("WindowsOptimizationPage_DriverQueue_Selection", "{0} package(s) selected."), selected);
    }

    private void RenderVisiblePackages()
    {
        PackagesPanel.Children.Clear();
        var filter = FilterTextBox.Text?.Trim() ?? string.Empty;
        var packages = _packages
            .Where(package => !OnlyUpdatesCheckBox.IsChecked.GetValueOrDefault() || package.IsUpdate)
            .Where(package => string.IsNullOrWhiteSpace(filter)
                || package.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || package.Description.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || package.Category.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || package.Version.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();

        var sortKey = (SortComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Date";
        packages = sortKey switch
        {
            "Name" => packages.OrderBy(package => package.Title, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            "Category" => packages
                .OrderBy(package => package.Category, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(package => package.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToArray(),
            _ => packages.OrderByDescending(package => package.ReleaseDate).ToArray(),
        };

        if (packages.Length == 0)
        {
            PackagesPanel.Children.Add(new LocalizedTextBlock
            {
                Text = AvaloniaLocalization.GetString(
                    _packages.Count == 0
                        ? "WindowsOptimizationPage_DriverEmpty_NoResults_Message"
                        : "WindowsOptimizationPage_DriverEmpty_NoFilterResults_Message",
                    _packages.Count == 0
                        ? "No driver downloads found. Try a different source or operating system."
                        : "No driver downloads match the current filters."),
                Foreground = FindBrush("TextFillColorSecondaryBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 3,
            });
            return;
        }

        foreach (var package in packages)
            PackagesPanel.Children.Add(CreatePackageCard(package));
    }

    private void FilterTextBox_TextChanged(object? sender, TextChangedEventArgs e) => RenderVisiblePackages();

    private void OnlyUpdatesCheckBox_Click(object? sender, RoutedEventArgs e) => RenderVisiblePackages();

    private void SortComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) => RenderVisiblePackages();

    private Border CreatePackageCard(DriverPackageItem package)
    {
        var selection = new CheckBox
        {
            IsChecked = package.IsSelected,
            VerticalAlignment = VerticalAlignment.Top,
        };
        AutomationProperties.SetAutomationId(selection, $"AvaloniaDriverSelect_{package.Id}");
        AutomationProperties.SetName(selection, $"Select {package.Title}");
        selection.Click += async (_, _) =>
        {
            if (selection.IsChecked == true)
                _selectedPackageIds.Add(package.Id);
            else
                _selectedPackageIds.Remove(package.Id);
            await ApplyActionAsync(() => _platformServices.SetSelectedDriverPackagesAsync(_selectedPackageIds.ToArray()));
        };

        var title = new LocalizedTextBlock
        {
            Text = package.Title,
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var details = new LocalizedTextBlock
        {
            Text = $"{package.Category} | {package.Version} | {package.FileSize}",
            Foreground = FindBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        };
        var description = new LocalizedTextBlock
        {
            Text = package.Description,
            Foreground = FindBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var status = new LocalizedTextBlock
        {
            Text = GetPackageStatusText(package),
            Foreground = FindBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        };
        var progress = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = package.Progress,
            IsVisible = package.Status is DriverPackageStatus.Queued or DriverPackageStatus.Downloading or DriverPackageStatus.Paused,
            Height = 4,
        };
        var copy = new StackPanel { Spacing = 3, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(details);
        copy.Children.Add(description);
        copy.Children.Add(status);
        copy.Children.Add(progress);

        var queueButton = new Button
        {
            Content = AvaloniaLocalization.GetString("PackageControl_Download", "Queue"),
            MinWidth = 110,
            VerticalAlignment = VerticalAlignment.Top,
        };
        AutomationProperties.SetAutomationId(queueButton, $"AvaloniaDriverQueue_{package.Id}");
        AutomationProperties.SetName(queueButton, $"Queue {package.Title}");
        ToolTip.SetTip(queueButton, package.Description);
        queueButton.Click += async (_, _) =>
        {
            _selectedPackageIds.Add(package.Id);
            await ApplyActionAsync(async () =>
            {
                await _platformServices.SetSelectedDriverPackagesAsync(_selectedPackageIds.ToArray());
                return await _platformServices.StartSelectedDriverPackagesAsync();
            });
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 12 };
        grid.Children.Add(selection);
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);
        Grid.SetColumn(queueButton, 2);
        grid.Children.Add(queueButton);
        var card = new Border
        {
            Background = FindBrush("CardBackgroundBrush"),
            BorderBrush = FindBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = FindCornerRadius("CornerRadiusCard"),
            Padding = new Thickness(16),
            Child = grid,
            ContextMenu = CreatePackageContextMenu(package),
        };
        return card;
    }

    private ContextMenu CreatePackageContextMenu(DriverPackageItem package)
    {
        var hide = new MenuItem { Header = AvaloniaLocalization.GetString("Hide", "Hide") };
        hide.Click += async (_, _) => await ApplyActionAsync(() => _platformServices.HideDriverPackagesAsync([package.Id]));
        var hideAll = new MenuItem { Header = AvaloniaLocalization.GetString("HideAll", "Hide all") };
        hideAll.Click += async (_, _) => await ApplyActionAsync(() => _platformServices.HideDriverPackagesAsync(_packages.Select(item => item.Id).ToArray()));
        return new ContextMenu { ItemsSource = new object[] { hide, hideAll } };
    }

    private string GetPackageStatusText(DriverPackageItem package)
    {
        if (!string.IsNullOrWhiteSpace(package.Error))
            return package.Error;

        var status = package.Status switch
        {
            DriverPackageStatus.Queued => "Queued",
            DriverPackageStatus.Downloading => $"Downloading {package.Progress:0}%",
            DriverPackageStatus.Paused => "Paused",
            DriverPackageStatus.Completed => "Downloaded",
            DriverPackageStatus.Failed => "Download failed",
            _ => "Not started",
        };
        return package.IsRecommended ? $"Recommended | {status}" : status;
    }

    private IBrush FindBrush(string key) =>
        this.TryFindResource(key, out var value) && value is IBrush brush
            ? brush
            : new SolidColorBrush(Colors.Transparent);

    private CornerRadius FindCornerRadius(string key) =>
        this.TryFindResource(key, out var value) && value is CornerRadius radius
            ? radius
            : new CornerRadius(8);
}
