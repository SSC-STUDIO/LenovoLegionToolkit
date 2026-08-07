using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class NetworkAccelerationPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly HashSet<string> _favoriteGroupIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LocalizedTextBlock> _groupRuntimeLabels = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<NetworkAccelerationGroupState> _groups = Array.Empty<NetworkAccelerationGroupState>();
    private NetworkAccelerationRuntimeState? _lastRuntime;
    private string _targetQuery = string.Empty;
    private bool _isApplying;
    private bool _runtimePollInFlight;
    private DispatcherTimer? _runtimeTimer;
    private bool _trafficChartInitialized;
    private bool _hasTrafficSample;
    private long _lastUploadedBytes;
    private long _lastDownloadedBytes;
    private DateTimeOffset _lastTrafficSampleUtc;

    public NetworkAccelerationPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
        ModeComboBox.ItemsSource = new[] { "Off", "SystemProxy", "DiagnosticsOnly" };
        EnabledCheckBox.IsCheckedChanged += EnabledCheckBox_IsCheckedChanged;
        ModeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshAsync();
        StartRuntimePolling();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e) => _runtimeTimer?.Stop();

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

            _groups = state.Groups;
            _favoriteGroupIds.Clear();
            foreach (var group in state.Groups)
            {
                if (group.IsFavorite)
                    _favoriteGroupIds.Add(group.Id);
            }

            FavoriteStarButton.IsEnabled = NetworkAccelerationTargets.GetRecommendedGroups(_groups).Count > 0;
            RebuildGroupsPanel();
            if (!state.IsRunning)
                ResetTrafficChart();
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void RebuildGroupsPanel()
    {
        GroupsPanel.Children.Clear();
        _groupRuntimeLabels.Clear();

        var groups = NetworkAccelerationTargets.FilterGroups(_groups, _targetQuery);
        if (groups.Count == 0)
        {
            GroupsPanel.Children.Add(new LocalizedTextBlock
            {
                Text = AvaloniaLocalization.GetString(
                    _groups.Count == 0
                        ? "NetworkAccelerationPage_DomainGroupsEmptyDescription"
                        : "NetworkAccelerationPage_NoSearchResults",
                    _groups.Count == 0
                        ? "No domain groups are available."
                        : "No targets match your search."),
                Foreground = FindBrush("TextFillColorSecondaryBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 3,
            });
            return;
        }

        foreach (var group in groups)
            GroupsPanel.Children.Add(CreateGroupCard(group));

        if (_lastRuntime is not null)
            UpdateGroupRuntimeCounts();
    }

    private void StartRuntimePolling()
    {
        _runtimeTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _runtimeTimer.Tick -= RuntimeTimer_Tick;
        _runtimeTimer.Tick += RuntimeTimer_Tick;
        _runtimeTimer.Start();
        _ = RefreshRuntimeAsync();
    }

    private async void RuntimeTimer_Tick(object? sender, EventArgs e) => await RefreshRuntimeAsync();

    private async Task RefreshRuntimeAsync()
    {
        if (_runtimePollInFlight || _isApplying || !IsLoaded)
            return;

        _runtimePollInFlight = true;
        try
        {
            var runtime = await _platformServices.GetNetworkAccelerationRuntimeAsync();
            if (!IsLoaded)
                return;

            _lastRuntime = runtime;

            // WPF exposes this card only while its proxy worker is running.
            var isRunning = runtime.IsAvailable && runtime.IsRunning;
            RuntimeCard.IsVisible = isRunning;
            if (!isRunning)
            {
                ResetTrafficChart();
                return;
            }

            UpdateTrafficChart(runtime);
            UploadValueText.Text = FormatBytes(runtime.BytesUploaded);
            DownloadValueText.Text = FormatBytes(runtime.BytesDownloaded);
            ConnectionsValueText.Text = $"{runtime.ActiveConnections} / {runtime.TotalConnections}";
            RuntimeHealthText.Text = runtime.HealthStatus;
            RuntimeStatusText.Text = runtime.Status;
            PopulateConnections(runtime.Connections);
            PopulateDestinations(runtime.Destinations);
            UpdateGroupRuntimeCounts();
        }
        finally
        {
            _runtimePollInFlight = false;
        }
    }

    private void UpdateTrafficChart(NetworkAccelerationRuntimeState runtime)
    {
        EnsureTrafficChart();

        var now = DateTimeOffset.UtcNow;
        if (_hasTrafficSample
            && (runtime.BytesUploaded < _lastUploadedBytes
                || runtime.BytesDownloaded < _lastDownloadedBytes))
        {
            TrafficChart.ClearAll();
            _hasTrafficSample = false;
        }

        var uploadRate = 0.0;
        var downloadRate = 0.0;
        if (_hasTrafficSample)
        {
            var elapsed = Math.Max(0.25, (now - _lastTrafficSampleUtc).TotalSeconds);
            uploadRate = Math.Max(0, (runtime.BytesUploaded - _lastUploadedBytes) / elapsed);
            downloadRate = Math.Max(0, (runtime.BytesDownloaded - _lastDownloadedBytes) / elapsed);
        }

        TrafficChart.AddSample("upload", uploadRate / 1024.0);
        TrafficChart.AddSample("download", downloadRate / 1024.0);
        _lastUploadedBytes = runtime.BytesUploaded;
        _lastDownloadedBytes = runtime.BytesDownloaded;
        _lastTrafficSampleUtc = now;
        _hasTrafficSample = true;

        TrafficUploadRateText.Text = FormatRate(uploadRate);
        TrafficDownloadRateText.Text = FormatRate(downloadRate);
        TrafficTotalText.Text = FormatBytes(runtime.BytesUploaded + runtime.BytesDownloaded);
    }

    private void EnsureTrafficChart()
    {
        if (_trafficChartInitialized)
            return;

        var uploadBrush = ResolveChartBrush("ChartTemperatureBrush", 238, 145, 70);
        var downloadBrush = ResolveChartBrush("ChartUtilizationBrush", 77, 166, 232);
        TrafficChart.DefineSeries("upload", uploadBrush);
        TrafficChart.DefineSeries("download", downloadBrush);
        UploadSwatch.Background = uploadBrush;
        DownloadSwatch.Background = downloadBrush;
        _trafficChartInitialized = true;
    }

    private void ResetTrafficChart()
    {
        if (_trafficChartInitialized)
            TrafficChart.ClearAll();
        _hasTrafficSample = false;
        _lastUploadedBytes = 0;
        _lastDownloadedBytes = 0;
        _lastTrafficSampleUtc = default;
        TrafficUploadRateText.Text = "\u2014";
        TrafficDownloadRateText.Text = "\u2014";
        TrafficTotalText.Text = "\u2014";
    }

    private IBrush ResolveChartBrush(string key, byte red, byte green, byte blue) =>
        this.TryFindResource(key, out var value) && value is IBrush brush
            ? brush
            : new SolidColorBrush(Color.FromRgb(red, green, blue));

    private static string FormatRate(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:0} B/s";
        if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:0.0} KB/s";
        if (bytesPerSecond < 1024L * 1024 * 1024)
            return $"{bytesPerSecond / (1024d * 1024):0.0} MB/s";
        return $"{bytesPerSecond / (1024d * 1024 * 1024):0.0} GB/s";
    }

    private void UpdateGroupRuntimeCounts()
    {
        foreach (var group in _groups)
        {
            if (!_groupRuntimeLabels.TryGetValue(group.Id, out var label))
                continue;

            var selected = NetworkAccelerationTargets.GetSelectedDomainCount(group);
            label.Text = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                AvaloniaLocalization.GetString("NetworkAccelerationPage_GroupRuntimeFormat", "{0}/{1} selected"),
                selected,
                group.DomainCount);
        }
    }

    private void PopulateConnections(IReadOnlyList<NetworkAccelerationConnectionState> connections)
    {
        ConnectionsPanel.Children.Clear();
        if (connections.Count == 0)
        {
            ConnectionsPanel.Children.Add(CreateRuntimeEmptyState("No active connections."));
            return;
        }

        foreach (var connection in connections.Take(8))
        {
            var detail = $"{connection.Protocol} / {connection.State} / {FormatBytes(connection.BytesUploaded + connection.BytesDownloaded)}";
            if (connection.ConnectLatencyMs is long latency)
                detail += $" / {latency} ms";
            if (!string.IsNullOrWhiteSpace(connection.Error))
                detail += $" / {connection.Error}";
            ConnectionsPanel.Children.Add(CreateRuntimeRow(
                FormatEndpoint(connection.Host, connection.Port),
                detail));
        }
    }

    private void PopulateDestinations(IReadOnlyList<NetworkAccelerationDestinationState> destinations)
    {
        DestinationsPanel.Children.Clear();
        if (destinations.Count == 0)
        {
            DestinationsPanel.Children.Add(CreateRuntimeEmptyState("No destination statistics."));
            return;
        }

        foreach (var destination in destinations.Take(8))
        {
            var detail = $"{destination.ActiveConnections} active / {destination.TotalConnections} total / {FormatBytes(destination.BytesUploaded + destination.BytesDownloaded)}";
            if (destination.LastConnectLatencyMs is long latency)
                detail += $" / {latency} ms";
            if (!string.IsNullOrWhiteSpace(destination.LastState))
                detail += $" / {destination.LastState}";
            DestinationsPanel.Children.Add(CreateRuntimeRow(
                FormatEndpoint(destination.Host, destination.Port),
                detail));
        }
    }

    private Control CreateRuntimeEmptyState(string text) => new LocalizedTextBlock
    {
        Text = text,
        Foreground = FindBrush("TextFillColorSecondaryBrush"),
        OverflowMode = LocalizedOverflowMode.Wrap,
        MaxLines = 2,
    };

    private Control CreateRuntimeRow(string title, string detail)
    {
        var titleBlock = new LocalizedTextBlock
        {
            Text = title,
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
            MinWidth = 0,
        };
        ToolTip.SetTip(titleBlock, title);
        var detailBlock = new LocalizedTextBlock
        {
            Text = detail,
            Foreground = FindBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
            MinWidth = 0,
        };
        ToolTip.SetTip(detailBlock, detail);
        var stack = new StackPanel { Spacing = 1 };
        stack.Children.Add(titleBlock);
        stack.Children.Add(detailBlock);
        return stack;
    }

    private static string FormatEndpoint(string host, int port) =>
        string.IsNullOrWhiteSpace(host) ? $"Port {port}" : $"{host}:{port}";

    private static string FormatBytes(long bytes)
    {
        var value = Math.Max(0, bytes);
        if (value < 1024)
            return $"{value} B";
        if (value < 1024 * 1024)
            return $"{value / 1024d:0.0} KB";
        if (value < 1024L * 1024 * 1024)
            return $"{value / (1024d * 1024):0.0} MB";
        return $"{value / (1024d * 1024 * 1024):0.0} GB";
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
        var titleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            MinWidth = 0,
        };
        titleRow.Children.Add(title);
        if (group.IsFavorite || _favoriteGroupIds.Contains(group.Id))
        {
            var star = new LocalizedTextBlock
            {
                Text = "\u2605",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = FindBrush("ChartTemperatureBrush"),
            };
            ToolTip.SetTip(star, AvaloniaLocalization.GetString("NetworkAccelerationPage_FavoriteGroup", "Favorite group"));
            titleRow.Children.Add(star);
        }

        var description = new LocalizedTextBlock
        {
            Text = string.IsNullOrWhiteSpace(group.Description)
                ? $"{group.DomainCount} domain(s)"
                : $"{group.Description} ({group.DomainCount} domain(s))",
            Foreground = FindBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var runtimeLabel = new LocalizedTextBlock
        {
            Foreground = FindBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
            MinWidth = 0,
        };
        _groupRuntimeLabels[group.Id] = runtimeLabel;
        var selected = NetworkAccelerationTargets.GetSelectedDomainCount(group);
        runtimeLabel.Text = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            AvaloniaLocalization.GetString("NetworkAccelerationPage_GroupRuntimeFormat", "{0}/{1} selected"),
            selected,
            group.DomainCount);
        var copy = new StackPanel { Spacing = 3, MinWidth = 0 };
        copy.Children.Add(titleRow);
        copy.Children.Add(description);
        copy.Children.Add(runtimeLabel);
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

    private void FavoriteStarButton_Click(object? sender, RoutedEventArgs e)
    {
        RebuildFavoriteMenu();
        FavoriteGroupsPopup.PlacementTarget = FavoriteStarButton;
        FavoriteGroupsPopup.IsOpen = true;
    }

    private void RebuildFavoriteMenu()
    {
        FavoriteGroupsPanel.Children.Clear();
        var groups = NetworkAccelerationTargets.GetRecommendedGroups(_groups);
        if (groups.Count == 0)
        {
            FavoriteGroupsPanel.Children.Add(new LocalizedTextBlock
            {
                Text = AvaloniaLocalization.GetString(
                    "NetworkAccelerationPage_FavoritesEmptyTitle",
                    "No recommended targets"),
                Foreground = FindBrush("TextFillColorSecondaryBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 2,
            });
            return;
        }

        foreach (var group in groups)
        {
            var checkBox = new CheckBox
            {
                Content = group.DisplayName,
                IsChecked = group.IsEnabled,
                Margin = new Thickness(4, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            AutomationProperties.SetAutomationId(checkBox, $"AvaloniaNetworkAccelerationRecommended_{group.Id}");
            AutomationProperties.SetName(checkBox, group.DisplayName);
            ToolTip.SetTip(checkBox, group.Description);
            var groupId = group.Id;
            checkBox.IsCheckedChanged += async (_, _) =>
            {
                if (_isApplying || checkBox.IsChecked is not bool enabled)
                    return;

                if (!await _platformServices.SetNetworkAccelerationGroupEnabledAsync(groupId, enabled))
                    await RefreshAsync();
                else
                    await RefreshAsync();
                RebuildFavoriteMenu();
            };
            FavoriteGroupsPanel.Children.Add(checkBox);
        }
    }

    private void TargetSearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _targetQuery = TargetSearchBox.Text?.Trim() ?? string.Empty;
        RebuildGroupsPanel();
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

    private async void NatDiagnosticButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        NatDiagnosticButton.IsEnabled = false;
        try
        {
            var result = await _platformServices.RunNetworkNatDiagnosticAsync(
                NatStunHostTextBox.Text?.Trim() ?? string.Empty);
            NatDiagnosticResultText.Text = result.IsAvailable
                ? $"{result.Type}; local: {result.LocalIp ?? "-"}; public: {result.PublicIp ?? "-"}; internet: {(result.InternetAvailable ? "connected" : "unreachable")}" +
                  (string.IsNullOrWhiteSpace(result.Error) ? string.Empty : $"; {result.Error}")
                : result.Error ?? "NAT diagnostics are unavailable.";
        }
        catch (Exception exception)
        {
            NatDiagnosticResultText.Text = exception.Message;
        }
        finally
        {
            NatDiagnosticButton.IsEnabled = true;
        }
    }

    private async void DnsDiagnosticButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        DnsDiagnosticButton.IsEnabled = false;
        try
        {
            var result = await _platformServices.RunNetworkDnsDiagnosticAsync(
                DnsDomainTextBox.Text?.Trim() ?? string.Empty,
                DnsServerTextBox.Text?.Trim(),
                DnsUseDohCheckBox.IsChecked == true,
                DnsDohUrlTextBox.Text?.Trim());
            DnsDiagnosticResultText.Text = result.Probes.Count > 0
                ? string.Join(Environment.NewLine, result.Probes.Select(FormatDnsProbe))
                : result.Error ?? "DNS diagnostics returned no result.";
        }
        catch (Exception exception)
        {
            DnsDiagnosticResultText.Text = exception.Message;
        }
        finally
        {
            DnsDiagnosticButton.IsEnabled = true;
        }
    }

    private async void Ipv6DiagnosticButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        Ipv6DiagnosticButton.IsEnabled = false;
        try
        {
            var result = await _platformServices.RunNetworkIpv6DiagnosticAsync();
            Ipv6DiagnosticResultText.Text = result.IsAvailable
                ? result.Supported
                    ? $"IPv6 supported: {result.Address ?? "address unavailable"}" +
                      (string.IsNullOrWhiteSpace(result.Error) ? string.Empty : $"; {result.Error}")
                    : $"IPv6 unavailable: {result.Error ?? "no routable address"}"
                : result.Error ?? "IPv6 diagnostics are unavailable.";
        }
        catch (Exception exception)
        {
            Ipv6DiagnosticResultText.Text = exception.Message;
        }
        finally
        {
            Ipv6DiagnosticButton.IsEnabled = true;
        }
    }

    private static string FormatDnsProbe(NetworkDnsProbeState probe)
    {
        var result = probe.Success && probe.Addresses.Count > 0
            ? string.Join(", ", probe.Addresses)
            : probe.Error ?? "failed";
        return $"{probe.Channel}: {result} ({probe.ElapsedMs} ms)";
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

/// <summary>
/// Pure selection/filtering logic shared by the recommended-targets star menu and
/// the target search box. Kept free of UI state so it can be unit-tested directly.
/// </summary>
public static class NetworkAccelerationTargets
{
    /// <summary>Default recommended ids (Watt Toolkit-style), favorite state aside.</summary>
    public static readonly IReadOnlyList<string> RecommendedGroupIds = new[]
    {
        "steam",
        "github",
        "public-cdn",
        "twitch",
        "roblox",
    };

    public static bool IsRecommendedGroup(string groupId, bool isFavorite) =>
        isFavorite || RecommendedGroupIds.Contains(groupId, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Recommended groups ordered favorites-first (stable order), capped to eight
    /// entries so the star menu stays compact.
    /// </summary>
    public static IReadOnlyList<NetworkAccelerationGroupState> GetRecommendedGroups(
        IReadOnlyList<NetworkAccelerationGroupState> groups) =>
        groups
            .Select((group, index) => (Group: group, Index: index))
            .Where(item => IsRecommendedGroup(item.Group.Id, item.Group.IsFavorite))
            .OrderByDescending(item => item.Group.IsFavorite)
            .ThenBy(item => item.Index)
            .Select(item => item.Group)
            .Take(8)
            .ToArray();

    /// <summary>Filters groups by a case-insensitive display-name or description match.</summary>
    public static IReadOnlyList<NetworkAccelerationGroupState> FilterGroups(
        IReadOnlyList<NetworkAccelerationGroupState> groups,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return groups;

        return groups
            .Where(group =>
                group.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || group.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>Enabled groups count all of their domains toward the selection.</summary>
    public static int GetSelectedDomainCount(NetworkAccelerationGroupState group) =>
        group.IsEnabled ? Math.Max(0, group.DomainCount) : 0;
}
