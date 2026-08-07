using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class NetworkAccelerationPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private bool _isApplying;

    public NetworkAccelerationPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
        ModeComboBox.ItemsSource = new[] { "Off", "SystemProxy", "DiagnosticsOnly" };
        EnabledCheckBox.IsCheckedChanged += EnabledCheckBox_IsCheckedChanged;
        ModeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _isApplying = true;
        try
        {
            var state = await _platformServices.GetNetworkAccelerationStateAsync();
            EnabledCheckBox.IsEnabled = state.IsAvailable;
            EnabledCheckBox.IsChecked = state.IsEnabled;
            ModeComboBox.IsEnabled = state.IsAvailable;
            ModeComboBox.SelectedItem = state.Mode;
            StatusText.Text = state.IsAvailable
                ? $"{state.Status} {(state.IsBackendReady ? string.Empty : "Worker unavailable.")}".Trim()
                : state.Status;
            StartStopButton.IsEnabled = state.IsAvailable && state.IsEnabled;
            StartStopButton.Content = state.IsRunning
                ? AvaloniaLocalization.GetString("NetworkAccelerationPage_Stop", "Stop")
                : AvaloniaLocalization.GetString("NetworkAccelerationPage_Start", "Start");

            GroupsPanel.Children.Clear();
            if (state.Groups.Count == 0)
            {
                GroupsPanel.Children.Add(new LocalizedTextBlock
                {
                    Text = AvaloniaLocalization.GetString(
                        "NetworkAccelerationPage_DomainGroupsEmptyDescription",
                        "No domain groups are available."),
                    Foreground = FindBrush("TextFillColorSecondaryBrush"),
                    OverflowMode = LocalizedOverflowMode.Wrap,
                    MaxLines = 3,
                });
            }
            else
            {
                foreach (var group in state.Groups)
                    GroupsPanel.Children.Add(CreateGroupCard(group));
            }
        }
        finally
        {
            _isApplying = false;
        }
    }

    private Border CreateGroupCard(NetworkAccelerationGroupState group)
    {
        var checkBox = new CheckBox
        {
            IsChecked = group.IsEnabled,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsEnabled = true,
        };
        AutomationProperties.SetAutomationId(checkBox, $"AvaloniaNetworkAccelerationGroup_{group.Id}");
        AutomationProperties.SetName(checkBox, group.DisplayName);
        ToolTip.SetTip(checkBox, group.Description);
        checkBox.IsCheckedChanged += async (_, _) =>
        {
            if (_isApplying || checkBox.IsChecked is not bool enabled)
                return;

            if (!await _platformServices.SetNetworkAccelerationGroupEnabledAsync(group.Id, enabled))
                await RefreshAsync();
        };

        var title = new LocalizedTextBlock
        {
            Text = group.DisplayName,
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = string.IsNullOrWhiteSpace(group.Description)
                ? $"{group.DomainCount} domain(s)"
                : $"{group.Description} ({group.DomainCount} domain(s))",
            Foreground = FindBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var copy = new StackPanel { Spacing = 3, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(description);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(copy);
        Grid.SetColumn(checkBox, 1);
        grid.Children.Add(checkBox);
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

    private async void EnabledCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_isApplying || EnabledCheckBox.IsChecked is not bool enabled)
            return;

        if (!await _platformServices.SetNetworkAccelerationEnabledAsync(enabled))
            await RefreshAsync();
        else
            await RefreshAsync();
    }

    private async void ModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isApplying || ModeComboBox.SelectedItem is not string mode)
            return;

        if (!await _platformServices.SetNetworkAccelerationModeAsync(mode))
            await RefreshAsync();
    }

    private async void StartStopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            if (!await _platformServices.ToggleNetworkAccelerationAsync())
                ToolTip.SetTip(StartStopButton, AvaloniaLocalization.GetString("NetworkAccelerationPage_StartFailed", "The network worker could not be started."));
        }
        finally
        {
            _isApplying = false;
        }

        await RefreshAsync();
    }

    private async void DiagnosticsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            DiagnosticsText.Text = await _platformServices.RunNetworkDiagnosticsAsync();
            DiagnosticsText.IsVisible = true;
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async void RestoreButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            DiagnosticsText.Text = await _platformServices.RestoreNetworkAccelerationAsync();
            DiagnosticsText.IsVisible = true;
        }
        finally
        {
            _isApplying = false;
        }

        await RefreshAsync();
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
