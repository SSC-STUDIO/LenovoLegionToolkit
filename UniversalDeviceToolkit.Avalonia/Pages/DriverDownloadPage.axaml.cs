using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DriverDownloadPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private bool _isApplying;
    private readonly string[] _sources = ["Vantage", "PCSupport"];
    private readonly string[] _operatingSystems = ["Windows11", "Windows10", "Windows8", "Windows7"];

    public DriverDownloadPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
        SourceComboBox.ItemsSource = _sources;
        OsComboBox.ItemsSource = _operatingSystems;
        OsComboBox.SelectedIndex = 0;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var state = await _platformServices.GetDriverDownloadStateAsync();
        MachineTypeTextBox.Text = state.MachineType;
        StateText.Text = state.Error ?? string.Empty;
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
            var state = await _platformServices.SearchDriverPackagesAsync(
                source,
                MachineTypeTextBox.Text ?? string.Empty,
                os,
                OnlyUpdatesCheckBox.IsChecked == true);
            MachineTypeTextBox.Text = state.MachineType;
            StateText.Text = state.Error ?? $"{state.Packages.Count} package(s) found.";
            RenderPackages(state.Packages);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void RenderPackages(IReadOnlyList<DriverPackageItem> packages)
    {
        PackagesPanel.Children.Clear();
        if (packages.Count == 0)
        {
            PackagesPanel.Children.Add(new LocalizedTextBlock
            {
                Text = AvaloniaLocalization.GetString(
                    "WindowsOptimizationPage_DriverEmpty_NoResults_Message",
                    "No driver downloads found. Try a different source or operating system."),
                Foreground = FindBrush("TextFillColorSecondaryBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 3,
            });
            return;
        }

        foreach (var package in packages)
            PackagesPanel.Children.Add(CreatePackageCard(package));
    }

    private Border CreatePackageCard(DriverPackageItem package)
    {
        var downloadButton = new Button
        {
            Content = AvaloniaLocalization.GetString("PackageControl_Download", "Download"),
            MinWidth = 110,
            VerticalAlignment = VerticalAlignment.Top,
        };
        AutomationProperties.SetAutomationId(downloadButton, $"AvaloniaDriverDownload_{package.Id}");
        AutomationProperties.SetName(downloadButton, $"Download {package.Title}");
        ToolTip.SetTip(downloadButton, package.Description);
        downloadButton.Click += async (_, _) => await DownloadPackageAsync(package, downloadButton);

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
        var copy = new StackPanel { Spacing = 3, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(details);
        copy.Children.Add(description);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(copy);
        Grid.SetColumn(downloadButton, 1);
        grid.Children.Add(downloadButton);
        return new Border
        {
            Background = FindBrush("CardBackgroundBrush"),
            BorderBrush = FindBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = FindCornerRadius("CornerRadiusCard"),
            Padding = new Thickness(16),
            Child = grid,
        };
    }

    private async Task DownloadPackageAsync(DriverPackageItem package, Button button)
    {
        if (_isApplying)
            return;

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

        _isApplying = true;
        button.IsEnabled = false;
        try
        {
            var success = await _platformServices.DownloadDriverPackageAsync(package.Id, path);
            ToolTip.SetTip(button, success
                ? AvaloniaLocalization.GetString("PackageControl_DownloadComplete_Title", "Download complete")
                : AvaloniaLocalization.GetString("PackagesPage_DownloadFailed_Title", "Download failed"));
        }
        finally
        {
            button.IsEnabled = true;
            _isApplying = false;
        }
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
