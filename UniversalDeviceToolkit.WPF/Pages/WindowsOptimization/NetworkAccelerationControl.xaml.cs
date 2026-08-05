using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Controls.Charts;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;
using CustomControls = UniversalDeviceToolkit.WPF.Controls.Custom;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

public partial class NetworkAccelerationControl : UserControl
{
    private readonly INetworkAccelerationService _acceleration;
    private readonly INetworkDiagnosticsService _diagnostics;
    private readonly INetworkStateRecoveryService _recovery;
    private bool _suppressEvents;
    private bool _isBusy;
    private CancellationTokenSource? _diagnosticsCts;
    private DispatcherTimer? _trafficTimer;
    private DispatcherTimer? _runtimeTimer;
    private NetworkProxyTrafficSnapshot? _lastTrafficSnapshot;
    private NetworkProxyRuntimeSnapshot? _lastRuntimeSnapshot;
    private DateTime _lastTrafficSampleUtc;
    private bool _hasTrafficSample;
    private bool _trafficPollInFlight;
    private bool _runtimePollInFlight;
    private bool _trafficChartInitialized;

    /// <summary>Tracks which service groups are expanded in the tree view.</summary>
    private readonly HashSet<string> _expandedGroupIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextBlock> _groupRuntimeLabels = new(StringComparer.OrdinalIgnoreCase);

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private static readonly HashSet<string> RecommendedGroupIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "steam",
        "github",
        "public-cdn",
        "twitch",
        "roblox"
    };

    /// <summary>Raised when the page toolbar needs to reflect a configuration or run-state change.</summary>
    public event EventHandler? ToolbarStateChanged;

    public bool IsAccelerationRunning => _acceleration.IsRunning;

    public int SelectedTargetCount => GetSelectedTargetCount();

    public bool CanStartAcceleration => CanStart(out _);

    public string StartAvailabilityReason
    {
        get
        {
            CanStart(out var reason);
            return reason;
        }
    }

    public IReadOnlyList<NetworkDomainGroup> GetRecommendedTargetGroups()
    {
        return (_acceleration.Config.DomainGroups ?? [])
            .Where(group => group.IsFavorite || RecommendedGroupIds.Contains(group.Id))
            .Take(8)
            .ToArray();
    }

    public bool IsTargetGroupSelected(string groupId) =>
        (_acceleration.Config.DomainGroups ?? [])
            .FirstOrDefault(group => string.Equals(group.Id, groupId, StringComparison.OrdinalIgnoreCase)) is { } group &&
        GetGroupSelectedCount(group) > 0;

    public async Task<bool> SetRecommendedTargetEnabledAsync(string groupId, bool enabled)
    {
        var group = (_acceleration.Config.DomainGroups ?? [])
            .FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
            return false;

        group.Enabled = enabled;
        if (group.SubItems is not null)
        {
            foreach (var subItem in group.SubItems)
                subItem.Enabled = enabled;
        }

        try
        {
            await _acceleration.SaveConfigAsync();
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Recommended network target save failed: {ex.Message}", ex);
        }

        BuildServiceList();
        RefreshUi();
        return true;
    }

    public IReadOnlyList<string> GetSelectedTargetSummaries() =>
        (_acceleration.Config.DomainGroups ?? [])
            .Select(group => new
            {
                group.DisplayName,
                SelectedCount = GetGroupSelectedCount(group),
                TotalCount = GetGroupTargetCount(group)
            })
            .Where(item => item.SelectedCount > 0)
            .Select(item => string.Format(
                CultureInfo.CurrentCulture,
                "{0} ({1}/{2})",
                item.DisplayName,
                item.SelectedCount,
                item.TotalCount))
            .ToArray();

    public NetworkAccelerationControl()
    {
        _acceleration = IoCContainer.Resolve<INetworkAccelerationService>();
        _diagnostics = IoCContainer.Resolve<INetworkDiagnosticsService>();
        _recovery = IoCContainer.Resolve<INetworkStateRecoveryService>();
        InitializeComponent();
        Loaded += NetworkAccelerationControl_Loaded;
        Unloaded += NetworkAccelerationControl_Unloaded;
        IsVisibleChanged += NetworkAccelerationControl_IsVisibleChanged;
    }

    private void NetworkAccelerationControl_Loaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged -= NetworkAccelerationControl_IsVisibleChanged;
        IsVisibleChanged += NetworkAccelerationControl_IsVisibleChanged;

        BuildModeCombo();
        InitDiagnosticsCombos();
        BuildServiceList();
        InitializeTrafficChart();
        InitializeRuntimeLists();
        StartTrafficPolling();
        StartRuntimePolling();
        RefreshUi();
    }

    private void NetworkAccelerationControl_Unloaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged -= NetworkAccelerationControl_IsVisibleChanged;
        CloseDiagnosticPopups();
        _diagnosticsCts?.Cancel();
        _diagnosticsCts?.Dispose();
        _diagnosticsCts = null;
        StopTrafficPolling();
        StopRuntimePolling();
    }

    private void NetworkAccelerationControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && IsLoaded)
        {
            StartTrafficPolling();
            StartRuntimePolling();
            RefreshUi();
        }
        else if (!(bool)e.NewValue)
        {
            StopTrafficPolling();
            StopRuntimePolling();
        }
    }

    private void NatStatus_Click(object sender, RoutedEventArgs e) =>
        ShowDiagnosticPopup(_natDetailsPopup, sender as UIElement);

    private void DnsStatus_Click(object sender, RoutedEventArgs e) =>
        ShowDiagnosticPopup(_dnsDetailsPopup, sender as UIElement);

    private void Ipv6Status_Click(object sender, RoutedEventArgs e) =>
        ShowDiagnosticPopup(_ipv6DetailsPopup, sender as UIElement);

    private void ShowDiagnosticPopup(System.Windows.Controls.Primitives.Popup popup, UIElement? placementTarget)
    {
        CloseDiagnosticPopups();
        popup.PlacementTarget = placementTarget;
        popup.IsOpen = true;
    }

    private void CloseDiagnosticPopups()
    {
        if (_natDetailsPopup is not null)
            _natDetailsPopup.IsOpen = false;
        if (_dnsDetailsPopup is not null)
            _dnsDetailsPopup.IsOpen = false;
        if (_ipv6DetailsPopup is not null)
            _ipv6DetailsPopup.IsOpen = false;
    }

    private void InitializeTrafficChart()
    {
        if (_trafficChart is null)
            return;

        _trafficHeadingText.Text = T("NetworkAccelerationPage_MetricsHeading", "Traffic overview");
        _trafficTotalLabel.Text = T("NetworkAccelerationPage_Metric_TotalTraffic", "Total traffic");
        if (_trafficChartInitialized)
            return;

        var uploadColor = ResolveTrafficColor(
            "PaletteOrangeBrush", Color.FromRgb(238, 145, 70));
        var downloadColor = ResolveTrafficColor(
            "PaletteLightBlueBrush", Color.FromRgb(77, 166, 232));
        _trafficChart.DefineSeries("upload", uploadColor);
        _trafficChart.DefineSeries("download", downloadColor);
        _trafficChartInitialized = true;
        ResetTrafficView();
    }

    private Color ResolveTrafficColor(string resourceKey, Color fallback)
    {
        return (TryFindResource(resourceKey) as SolidColorBrush)?.Color ?? fallback;
    }

    private void StartTrafficPolling()
    {
        if (!IsLoaded)
            return;

        _trafficTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _trafficTimer.Tick -= TrafficTimer_Tick;
        _trafficTimer.Tick += TrafficTimer_Tick;
        _trafficTimer.Start();
        _ = PollTrafficAsync();
    }

    private void StopTrafficPolling() => _trafficTimer?.Stop();

    private void InitializeRuntimeLists()
    {
        if (_connectionsHeading is not null)
            _connectionsHeading.Text = T("NetworkAccelerationPage_CurrentConnections", "Current and recent connections");
        if (_destinationsHeading is not null)
            _destinationsHeading.Text = T("NetworkAccelerationPage_DestinationStats", "Destination statistics");
        if (_runtimeHealthLabel is not null)
            _runtimeHealthLabel.Text = T("NetworkAccelerationPage_Health", "Health");
        ClearRuntimeLists();
    }

    private void StartRuntimePolling()
    {
        if (!IsLoaded)
            return;

        _runtimeTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _runtimeTimer.Tick -= RuntimeTimer_Tick;
        _runtimeTimer.Tick += RuntimeTimer_Tick;
        _runtimeTimer.Start();
        _ = PollRuntimeAsync();
    }

    private void StopRuntimePolling() => _runtimeTimer?.Stop();

    private async void RuntimeTimer_Tick(object? sender, EventArgs e) => await PollRuntimeAsync();

    private async Task PollRuntimeAsync()
    {
        if (_runtimePollInFlight || !IsLoaded || !IsVisible)
            return;

        if (!_acceleration.IsRunning)
        {
            ClearRuntimeLists();
            return;
        }

        _runtimePollInFlight = true;
        try
        {
            var snapshot = await _acceleration.GetRuntimeSnapshotAsync();
            if (!IsLoaded || !IsVisible || snapshot is null)
                return;

            _lastRuntimeSnapshot = snapshot;
            UpdateRuntimeLists(snapshot);
            if (_runtimeHealthValue is not null)
                _runtimeHealthValue.Text = FormatHealth(snapshot.HealthStatus);
            if (_runHealthText is not null)
                _runHealthText.Text = FormatHealth(snapshot.HealthStatus);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "network-accel-runtime-poll",
                "Network acceleration runtime polling failed.",
                ex);
        }
        finally
        {
            _runtimePollInFlight = false;
        }
    }

    private void UpdateRuntimeLists(NetworkProxyRuntimeSnapshot snapshot)
    {
        if (_connectionListPanel is not null)
        {
            _connectionListPanel.Items.Clear();
            foreach (var connection in snapshot.Connections.Take(8))
                _connectionListPanel.Items.Add(CreateConnectionRow(connection));
        }

        if (_destinationListPanel is not null)
        {
            _destinationListPanel.Items.Clear();
            foreach (var destination in snapshot.Destinations.Take(8))
                _destinationListPanel.Items.Add(CreateDestinationRow(destination));
        }

        UpdateGroupRuntimeCounts(snapshot);

        if (_currentConnectionsText is not null)
        {
            _currentConnectionsText.Text = string.Format(
                CultureInfo.CurrentCulture,
                T("NetworkAccelerationPage_ConnectionSummary", "{0} active / {1} total"),
                snapshot.Traffic.ActiveConnections,
                snapshot.Traffic.TotalConnections);
        }

        if (_destinationsSummaryText is not null)
        {
            _destinationsSummaryText.Text = string.Format(
                CultureInfo.CurrentCulture,
                T("NetworkAccelerationPage_DestinationSummary", "{0} destinations"),
                snapshot.Destinations.Count);
        }
    }

    private FrameworkElement CreateConnectionRow(NetworkProxyConnectionSnapshot connection)
    {
        var state = string.IsNullOrWhiteSpace(connection.State) ? "unknown" : connection.State;
        var latency = connection.ConnectLatencyMs is { } ms ? $"{ms} ms" : "-";
        var host = string.IsNullOrWhiteSpace(connection.Host) ? T("NetworkAccelerationPage_UnknownHost", "Unknown host") : connection.Host;
        var displayState = state switch
        {
            "active" => T("NetworkAccelerationPage_ConnectionActive", "Active"),
            "completed" => T("NetworkAccelerationPage_ConnectionCompleted", "Completed"),
            "blocked" => T("NetworkAccelerationPage_ConnectionBlocked", "Blocked"),
            "failed" => T("NetworkAccelerationPage_ConnectionFailed", "Failed"),
            "stopped" => T("NetworkAccelerationPage_ConnectionStopped", "Stopped"),
            _ => T("NetworkAccelerationPage_ConnectionUnknown", "Unknown")
        };
        var row = new Grid { MinHeight = 26, Margin = new Thickness(0, 0, 0, 2) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new TextBlock
        {
            Text = $"{host}:{connection.Port}",
            FontSize = (double)FindResource("FontSizeCaption"),
            Foreground = (Brush)FindResource("TextFillColorPrimaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        var detail = new TextBlock
        {
            Text = $"{displayState}  {latency}",
            FontSize = (double)FindResource("FontSizeCaption"),
            Foreground = state is "failed" or "blocked"
                ? (Brush)FindResource("StatusCriticalBrush")
                : (Brush)FindResource("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(detail, 1);
        row.Children.Add(title);
        row.Children.Add(detail);
        return row;
    }

    private FrameworkElement CreateDestinationRow(NetworkProxyDestinationSnapshot destination)
    {
        var latency = destination.LastConnectLatencyMs is { } ms ? $"{ms} ms" : "-";
        var row = new Grid { MinHeight = 26, Margin = new Thickness(0, 0, 0, 2) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new TextBlock
        {
            Text = $"{destination.Host}:{destination.Port}",
            FontSize = (double)FindResource("FontSizeCaption"),
            Foreground = (Brush)FindResource("TextFillColorPrimaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        var detail = new TextBlock
        {
            Text = string.Format(
                CultureInfo.CurrentCulture,
                T("NetworkAccelerationPage_DestinationRow", "{0} conn  {1}"),
                destination.TotalConnections,
                latency),
            FontSize = (double)FindResource("FontSizeCaption"),
            Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(detail, 1);
        row.Children.Add(title);
        row.Children.Add(detail);
        return row;
    }

    private void ClearRuntimeLists()
    {
        _lastRuntimeSnapshot = null;
        _connectionListPanel?.Items.Clear();
        _destinationListPanel?.Items.Clear();
        if (_currentConnectionsText is not null)
            _currentConnectionsText.Text = T("NetworkAccelerationPage_ConnectionsWaiting", "No active connections");
        if (_destinationsSummaryText is not null)
            _destinationsSummaryText.Text = T("NetworkAccelerationPage_DestinationsWaiting", "No destination data");
        if (_runtimeHealthValue is not null)
            _runtimeHealthValue.Text = "-";
        foreach (var label in _groupRuntimeLabels.Values)
            label.Text = string.Empty;
    }

    private void UpdateGroupRuntimeCounts(NetworkProxyRuntimeSnapshot snapshot)
    {
        foreach (var group in _acceleration.Config.DomainGroups ?? [])
        {
            if (!_groupRuntimeLabels.TryGetValue(group.Id, out var label))
                continue;

            var hosts = GetGroupDomains(group);
            var active = snapshot.Destinations
                .Where(destination => hosts.Contains(destination.Host, StringComparer.OrdinalIgnoreCase))
                .Sum(destination => destination.ActiveConnections);
            var selected = GetGroupSelectedCount(group);
            var total = GetGroupTargetCount(group);
            label.Text = string.Format(
                CultureInfo.CurrentCulture,
                T("NetworkAccelerationPage_GroupRuntimeFormat", "{0}/{1} selected  {2} active"),
                selected,
                total,
                active);
        }
    }

    private string FormatHealth(string health) => health switch
    {
        "healthy" => T("NetworkAccelerationPage_HealthHealthy", "Healthy"),
        "degraded" => T("NetworkAccelerationPage_HealthDegraded", "Degraded"),
        "stopped" => FormatStoppedHealth(),
        _ => T("NetworkAccelerationPage_HealthUnknown", "Unknown")
    };

    private string FormatStoppedHealth() => string.Format(
        CultureInfo.CurrentCulture,
        T("NetworkAccelerationPage_StatusStopped", "Stopped ({0})"),
        ModeFullLabel(GetSelectedMode()));

    private async void TrafficTimer_Tick(object? sender, EventArgs e) => await PollTrafficAsync();

    private async Task PollTrafficAsync()
    {
        if (_trafficPollInFlight || !IsLoaded || !IsVisible)
            return;

        if (!_acceleration.IsRunning)
        {
            UpdateTrafficSectionVisibility(false);
            ClearRuntimeLists();
            if (_hasTrafficSample)
                ResetTrafficView();
            return;
        }

        _trafficPollInFlight = true;
        try
        {
            var snapshot = await _acceleration.GetTrafficSnapshotAsync();
            if (!IsLoaded || !IsVisible || snapshot is null)
            {
                if (_trafficStatusText is not null)
                    _trafficStatusText.Text = T(
                        "NetworkAccelerationPage_TrafficUnavailable",
                        "Traffic data is temporarily unavailable");
                return;
            }

            var now = DateTime.UtcNow;
            if (_lastTrafficSnapshot is { } previous &&
                (snapshot.BytesUploaded < previous.BytesUploaded ||
                 snapshot.BytesDownloaded < previous.BytesDownloaded))
            {
                _trafficChart.ClearAll();
                _hasTrafficSample = false;
            }

            var uploadRate = 0.0;
            var downloadRate = 0.0;
            if (_hasTrafficSample)
            {
                var elapsed = Math.Max(0.25, (now - _lastTrafficSampleUtc).TotalSeconds);
                uploadRate = Math.Max(0, (snapshot.BytesUploaded - _lastTrafficSnapshot!.BytesUploaded) / elapsed);
                downloadRate = Math.Max(0, (snapshot.BytesDownloaded - _lastTrafficSnapshot.BytesDownloaded) / elapsed);
            }

            _trafficChart.AddSample("upload", uploadRate / 1024.0);
            _trafficChart.AddSample("download", downloadRate / 1024.0);
            _lastTrafficSnapshot = snapshot;
            _lastTrafficSampleUtc = now;
            _hasTrafficSample = true;

            _trafficUploadValue.Text = FormatRate(uploadRate);
            _trafficDownloadValue.Text = FormatRate(downloadRate);
            _trafficConnectionsValue.Text = string.Format(
                CultureInfo.CurrentCulture,
                "{0} / {1}",
                snapshot.ActiveConnections,
                snapshot.TotalConnections);
            _trafficTotalValue.Text = FormatBytes(snapshot.BytesUploaded + snapshot.BytesDownloaded);
            _trafficStatusText.Text = T(
                "NetworkAccelerationPage_TrafficLive",
                "Collecting live proxy traffic");
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "network-accel-traffic-poll",
                "Network acceleration traffic polling failed.",
                ex);
        }
        finally
        {
            _trafficPollInFlight = false;
        }
    }

    private void ResetTrafficView()
    {
        _trafficChart?.ClearAll();
        _lastTrafficSnapshot = null;
        _lastTrafficSampleUtc = default;
        _hasTrafficSample = false;
        if (_trafficUploadValue is not null) _trafficUploadValue.Text = "—";
        if (_trafficDownloadValue is not null) _trafficDownloadValue.Text = "—";
        if (_trafficConnectionsValue is not null) _trafficConnectionsValue.Text = "—";
        if (_trafficTotalValue is not null) _trafficTotalValue.Text = "—";
        if (_trafficStatusText is not null)
            _trafficStatusText.Text = T(
                "NetworkAccelerationPage_TrafficWaiting",
                "Start acceleration to collect live traffic");
    }

    private static string FormatRate(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return string.Format(CultureInfo.CurrentCulture, "{0:0} B/s", bytesPerSecond);
        if (bytesPerSecond < 1024 * 1024)
            return string.Format(CultureInfo.CurrentCulture, "{0:0.0} KB/s", bytesPerSecond / 1024);
        if (bytesPerSecond < 1024 * 1024 * 1024)
            return string.Format(CultureInfo.CurrentCulture, "{0:0.0} MB/s", bytesPerSecond / (1024 * 1024));
        return string.Format(CultureInfo.CurrentCulture, "{0:0.0} GB/s", bytesPerSecond / (1024 * 1024 * 1024));
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return string.Format(CultureInfo.CurrentCulture, "{0} B", bytes);
        if (bytes < 1024 * 1024)
            return string.Format(CultureInfo.CurrentCulture, "{0:0.0} KB", bytes / 1024.0);
        if (bytes < 1024L * 1024 * 1024)
            return string.Format(CultureInfo.CurrentCulture, "{0:0.0} MB", bytes / (1024.0 * 1024));
        return string.Format(CultureInfo.CurrentCulture, "{0:0.0} GB", bytes / (1024.0 * 1024 * 1024));
    }

    // ─────────────────────────────────────────────────────
    // Mode combo
    // ─────────────────────────────────────────────────────

    private sealed class ModeOption
    {
        public ModeOption(string label, NetworkAccelerationMode mode)
        {
            Label = label;
            Mode = mode;
        }

        public string Label { get; }
        public NetworkAccelerationMode Mode { get; }
        public override string ToString() => Label;
    }

    private void BuildModeCombo()
    {
        if (_modeComboBox is null)
            return;

        _suppressEvents = true;
        try
        {
            _modeComboBox.Items.Clear();
            _modeComboBox.Items.Add(new ModeOption(
                T("NetworkAccelerationPage_Mode_SystemProxy", "System proxy (PAC / local proxy)"),
                NetworkAccelerationMode.SystemProxy));
            _modeComboBox.Items.Add(new ModeOption(
                T("NetworkAccelerationPage_Mode_DiagnosticsOnly", "Diagnostics only (no system changes)"),
                NetworkAccelerationMode.DiagnosticsOnly));
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private static NetworkAccelerationMode ToSelectableMode(NetworkAccelerationMode mode) =>
        mode is NetworkAccelerationMode.Off or NetworkAccelerationMode.Hosts
            ? NetworkAccelerationMode.SystemProxy
            : mode;

    // ─────────────────────────────────────────────────────
    // Diagnostics combos (STUN servers, DoH URLs)
    // ─────────────────────────────────────────────────────

    private void InitDiagnosticsCombos()
    {
        if (_natStunCombo is not null)
        {
            _natStunCombo.Items.Clear();
            _natStunCombo.Items.Add("stun.miwifi.com");
            _natStunCombo.Items.Add("stun.l.google.com");
            _natStunCombo.Items.Add("stun.cloudflare.com");
            _natStunCombo.Text = "stun.miwifi.com";
        }

        if (_dnsServerCombo is not null)
        {
            _dnsServerCombo.Items.Clear();
            _dnsServerCombo.Items.Add("223.5.5.5");
            _dnsServerCombo.Items.Add("223.6.6.6");
            _dnsServerCombo.Items.Add("119.29.29.29");
            _dnsServerCombo.Items.Add("1.1.1.1");
            _dnsServerCombo.Items.Add("8.8.8.8");
            _dnsServerCombo.Text = "223.5.5.5";
        }

        if (_dnsDohUrlCombo is not null)
        {
            _dnsDohUrlCombo.Items.Clear();
            _dnsDohUrlCombo.Items.Add("https://doh.pub/dns-query");
            _dnsDohUrlCombo.Items.Add("https://dns.alidns.com/dns-query");
            _dnsDohUrlCombo.Items.Add("https://cloudflare-dns.com/dns-query");
            _dnsDohUrlCombo.Text = "https://doh.pub/dns-query";
        }

        if (_natSummaryText is not null)
            _natSummaryText.Text = T("NaDiag_Unknown", "Unknown");
        if (_dnsSummaryText is not null)
            _dnsSummaryText.Text = T("NaDiag_Unknown", "Unknown");
        if (_ipv6SummaryText is not null)
            _ipv6SummaryText.Text = T("NaDiag_Unknown", "Unknown");

    }

    // ─────────────────────────────────────────────────────
    // Collapsible diagnostic panels
    // ─────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────
    // NAT detection
    // ─────────────────────────────────────────────────────

    private async void NatDetectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_natDetectButton is null)
            return;

        _natDetectButton.IsEnabled = false;
        try
        {
            var stunHost = (_natStunCombo?.Text ?? "stun.miwifi.com").Trim();
            if (string.IsNullOrWhiteSpace(stunHost))
                stunHost = "stun.miwifi.com";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var result = await NatTypeDetector.CheckAsync(stunHost, 3478, cts.Token);

            if (_natTypeText is not null)
                _natTypeText.Text = result.Type switch
                {
                    NatType.OpenInternet => T("NaDiag_NatOpen", "Open NAT"),
                    NatType.Nat => T("NaDiag_NatRestricted", "NAT"),
                    NatType.UdpBlocked => T("NaDiag_UdpBlocked", "UDP blocked"),
                    _ => T("NaDiag_Unknown", "Unknown")
                };

            if (_natLocalIpText is not null)
                _natLocalIpText.Text = result.LocalIp ?? "—";
            if (_natPublicIpText is not null)
                _natPublicIpText.Text = result.PublicIp ?? "—";
            if (_natInternetText is not null)
                _natInternetText.Text = result.InternetAvailable
                    ? T("NaDiag_Supported", "Connected")
                    : T("NaDiag_NotSupported", "Unreachable");
            if (_natSummaryText is not null)
                _natSummaryText.Text = _natTypeText?.Text ?? T("NaDiag_Unknown", "Unknown");
        }
        catch (Exception ex)
        {
            if (_natTypeText is not null)
                _natTypeText.Text = ex.Message;
        }
        finally
        {
            _natDetectButton.IsEnabled = true;
        }
    }

    // ─────────────────────────────────────────────────────
    // DNS detection
    // ─────────────────────────────────────────────────────

    private async void DnsDetectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dnsDetectButton is null)
            return;

        _dnsDetectButton.IsEnabled = false;
        try
        {
            var domain = (_dnsDomainInput?.Text ?? "store.steampowered.com").Trim();
            var dnsServer = (_dnsServerCombo?.Text ?? string.Empty).Trim();
            var dohEnabled = _dnsDohToggle?.IsChecked == true;
            var dohUrl = (_dnsDohUrlCombo?.Text ?? string.Empty).Trim();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // System DNS resolve
            var sysResult = await DnsDiagnosticsService.ResolveSystemAsync(domain, cts.Token);

            // Custom DNS resolve (if server specified)
            DnsProbeResult? customResult = null;
            if (!string.IsNullOrWhiteSpace(dnsServer))
            {
                try
                {
                    customResult = await DnsDiagnosticsService.ResolveCustomServerAsync(domain, dnsServer, cts.Token);
                }
                catch { /* non-fatal */ }
            }

            // DoH resolve (if enabled)
            DnsProbeResult? dohResult = null;
            if (dohEnabled && !string.IsNullOrWhiteSpace(dohUrl))
            {
                try
                {
                    dohResult = await DnsDiagnosticsService.ResolveDohAsync(domain, dohUrl, cts.Token);
                }
                catch { /* non-fatal */ }
            }

            sw.Stop();

            // Fastest successful channel drives both latency and the resolved address list.
            var fastest = new[] { sysResult, customResult, dohResult }
                .Where(r => r is not null && r.Success)
                .OrderBy(r => r!.ElapsedMs)
                .FirstOrDefault();

            if (_dnsLatencyText is not null)
                _dnsLatencyText.Text = fastest is not null
                    ? string.Format(T("NaDiag_LatencyFormat", "{0} ms"), fastest.ElapsedMs)
                    : T("NaDiag_Failed", "Failed");

            if (_dnsResolvedText is not null)
                _dnsResolvedText.Text = fastest is { Addresses.Length: > 0 }
                    ? string.Join(", ", fastest.Addresses)
                    : T("NaDiag_Failed", "Failed");
            if (_dnsSummaryText is not null)
                _dnsSummaryText.Text = _dnsLatencyText?.Text ?? T("NaDiag_Failed", "Failed");
        }
        catch (Exception ex)
        {
            if (_dnsLatencyText is not null)
                _dnsLatencyText.Text = ex.Message;
        }
        finally
        {
            _dnsDetectButton.IsEnabled = true;
        }
    }

    // ─────────────────────────────────────────────────────
    // IPv6 detection
    // ─────────────────────────────────────────────────────

    private async void Ipv6DetectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_ipv6DetectButton is null)
            return;

        _ipv6DetectButton.IsEnabled = false;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await Ipv6Detector.CheckAsync(cts.Token);

            if (_ipv6SupportText is not null)
            {
                _ipv6SupportText.Text = result.Supported
                    ? T("NaDiag_Ipv6SupportedFull", "IPv6 access supported")
                    : T("NaDiag_NotSupported", "Not supported");
                _ipv6SupportText.Foreground = result.Supported
                    ? (TryFindResource("StatusSuccessBrush") as Brush ?? Brushes.Green)
                    : (TryFindResource("TextFillColorPrimaryBrush") as Brush ?? Brushes.Black);
            }

            if (_ipv6AddressText is not null)
                _ipv6AddressText.Text = result.Address ?? "—";
        }
        catch (Exception ex)
        {
            if (_ipv6SupportText is not null)
                _ipv6SupportText.Text = ex.Message;
        }
        finally
        {
            if (_ipv6SummaryText is not null && _ipv6SupportText is not null)
                _ipv6SummaryText.Text = _ipv6SupportText.Text;
            _ipv6DetectButton.IsEnabled = true;
        }
    }

    // ─────────────────────────────────────────────────────
    // Service selection list (Watt Toolkit-style tree)
    // ─────────────────────────────────────────────────────

    private void BuildServiceList()
    {
        if (_serviceListPanel is null)
            return;

        _serviceListPanel.Items.Clear();
        _groupRuntimeLabels.Clear();
        var groups = _acceleration.Config.DomainGroups;
        if (groups is null || groups.Count == 0)
        {
            if (_domainGroupsEmptyState is not null)
                _domainGroupsEmptyState.Visibility = Visibility.Visible;
            return;
        }

        if (_domainGroupsEmptyState is not null)
            _domainGroupsEmptyState.Visibility = Visibility.Collapsed;

        var query = _targetSearchBox?.Text?.Trim();
        var ordered = groups
            .Where(group => string.IsNullOrWhiteSpace(query) ||
                            group.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            group.SubItems?.Any(sub =>
                                sub.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                sub.Domain.Contains(query, StringComparison.OrdinalIgnoreCase)) == true)
            .OrderByDescending(g => g.IsFavorite)
            .ThenBy(g => groups.IndexOf(g))
            .ToList();

        if (ordered.Count == 0)
        {
            if (_domainGroupsEmptyState is not null)
                _domainGroupsEmptyState.Visibility = Visibility.Visible;
            return;
        }

        foreach (var group in ordered)
            _serviceListPanel.Items.Add(CreateServiceGroupRow(group));

        if (_lastRuntimeSnapshot is not null)
            UpdateGroupRuntimeCounts(_lastRuntimeSnapshot);
    }

    private static IReadOnlyList<string> GetGroupDomains(NetworkDomainGroup group) =>
        (group.Domains ?? [])
            .Concat((group.SubItems ?? []).Select(item => item.Domain))
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Select(domain => domain.Trim().TrimEnd('.').ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static int GetGroupTargetCount(NetworkDomainGroup group) => GetGroupDomains(group).Count;

    private static int GetGroupSelectedCount(NetworkDomainGroup group)
    {
        var direct = group.Enabled
            ? group.Domains?.Count(domain => !string.IsNullOrWhiteSpace(domain)) ?? 0
            : 0;
        var subItems = group.Enabled ? group.SubItems?.Count(item => item.Enabled) ?? 0 : 0;
        return direct + subItems;
    }

    private static readonly IReadOnlyDictionary<string, string> BrandIconGeometry =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SteamLogo"] = "M11.979,0 C5.678,0 0.511,4.86 0.022,11.037 L6.454,13.695 C6.999,13.324 7.657,13.105 8.366,13.105 C8.429,13.105 8.491,13.109 8.554,13.111 L11.415,8.969 L11.415,8.91 C11.415,6.415 13.443,4.386 15.939,4.386 C18.433,4.386 20.463,6.417 20.463,8.913 C20.463,11.408 18.433,13.438 15.939,13.438 L15.834,13.438 L11.758,16.349 C11.758,16.401 11.762,16.454 11.762,16.508 C11.762,18.383 10.247,19.904 8.372,19.904 C6.737,19.904 5.356,18.731 5.041,17.177 L0.436,15.27 C1.862,20.307 6.486,24 11.979,24 C18.606,24 23.978,18.627 23.978,12 C23.978,5.373 18.605,0 11.979,0 Z M7.54,18.21 L6.067,17.6 C6.329,18.143 6.781,18.599 7.381,18.85 C8.678,19.389 10.174,18.774 10.713,17.475 C10.976,16.845 10.977,16.156 10.718,15.526 C10.459,14.896 9.968,14.405 9.341,14.143 C8.717,13.883 8.051,13.894 7.463,14.113 L8.986,14.743 C9.942,15.143 10.395,16.243 9.995,17.198 C9.598,18.155 8.497,18.608 7.54,18.21 Z M18.955,8.907 C18.955,7.245 17.602,5.892 15.94,5.892 C14.275,5.892 12.925,7.245 12.925,8.907 C12.925,10.572 14.275,11.922 15.94,11.922 C17.603,11.922 18.955,10.572 18.955,8.907 Z M13.682,8.902 C13.682,7.65 14.695,6.636 15.947,6.636 C17.196,6.636 18.213,7.65 18.213,8.902 C18.213,10.153 17.196,11.167 15.947,11.167 C14.694,11.167 13.682,10.153 13.682,8.902 Z",
            ["GitHubLogo"] = "M12,0.297 C5.37,0.297 0,5.67 0,12.297 C0,17.6 3.438,22.097 8.205,23.682 C8.805,23.795 9.025,23.424 9.025,23.105 C9.025,22.82 9.015,22.065 9.01,21.065 C5.672,21.789 4.968,19.455 4.968,19.455 C4.422,18.07 3.633,17.7 3.633,17.7 C2.546,16.956 3.717,16.971 3.717,16.971 C4.922,17.055 5.555,18.207 5.555,18.207 C6.625,20.042 8.364,19.512 9.05,19.205 C9.158,18.429 9.467,17.9 9.81,17.6 C7.145,17.3 4.344,16.268 4.344,11.67 C4.344,10.36 4.809,9.29 5.579,8.45 C5.444,8.147 5.039,6.927 5.684,5.274 C5.684,5.274 6.689,4.952 8.984,6.504 C9.944,6.237 10.964,6.105 11.984,6.099 C13.004,6.105 14.024,6.237 14.984,6.504 C17.264,4.952 18.269,5.274 18.269,5.274 C18.914,6.927 18.509,8.147 18.389,8.45 C19.154,9.29 19.619,10.36 19.619,11.67 C19.619,16.28 16.814,17.295 14.144,17.59 C14.564,17.95 14.954,18.686 14.954,19.81 C14.954,21.416 14.939,22.706 14.939,23.096 C14.939,23.411 15.149,23.786 15.764,23.666 C20.565,22.092 24,17.592 24,12.297 C24,5.67 18.627,0.297 12,0.297 Z",
            ["TwitchLogo"] = "M11.571,4.714 L13.286,4.714 L13.286,9.857 L11.57,9.857 Z M16.286,4.714 L18,4.714 L18,9.857 L16.286,9.857 Z M6,0 L1.714,4.286 L1.714,19.714 L6.857,19.714 L6.857,24 L11.143,19.714 L14.571,19.714 L22.286,12 L22.286,0 Z M20.571,11.143 L17.143,14.571 L13.714,14.571 L10.714,17.571 L10.714,14.571 L6.857,14.571 L6.857,1.714 L20.571,1.714 Z",
            ["RobloxLogo"] = "M18.926,23.998 L0,18.892 L5.075,0.002 L24,5.108 Z M15.348,10.09 L10.066,8.637 L8.652,13.91 L13.934,15.363 Z",
            ["CdnLogo"] = "M12,2 C6.477,2 2,6.477 2,12 C2,17.523 6.477,22 12,22 C17.523,22 22,17.523 22,12 C22,6.477 17.523,2 12,2 Z M2.5,9 H21.5 M2.5,15 H21.5 M12,2 C9.5,4.8 8.3,8.1 8.3,12 C8.3,15.9 9.5,19.2 12,22 M12,2 C14.5,4.8 15.7,8.1 15.7,12 C15.7,15.9 14.5,19.2 12,22"
        };

    private CustomControls.CardExpander CreateServiceGroupRow(NetworkDomainGroup group)
    {
        var id = group.Id;
        var isExpanded = _expandedGroupIds.Contains(id);
        var enabledCount = GetGroupSelectedCount(group);
        var totalCount = GetGroupTargetCount(group);
        var allEnabled = totalCount > 0 && enabledCount == totalCount;
        var someEnabled = enabledCount > 0 && !allEnabled;

        // Group-level checkbox (three-state)
        var groupCheckBox = new CheckBox
        {
            IsChecked = allEnabled ? true : someEnabled ? null : false,
            IsThreeState = true,
            VerticalAlignment = VerticalAlignment.Center
        };
        groupCheckBox.Click += async (_, _) =>
        {
            var newState = groupCheckBox.IsChecked == true;
            if (group.SubItems is not null)
            {
                foreach (var sub in group.SubItems)
                    sub.Enabled = newState;
            }
            group.Enabled = newState;
            try { await _acceleration.SaveConfigAsync(); } catch { }
            BuildServiceList();
            RefreshUi();
        };

        var brandIcon = CreateBrandIcon(group.IconKey);
        AutomationProperties.SetName(brandIcon, $"{group.DisplayName} icon");

        // Group name + favorite star share the middle column.
        var nameText = new TextBlock
        {
            Text = group.DisplayName,
            Style = (Style)FindResource("NaServiceNameStyle")
        };
        var favStar = new TextBlock
        {
            Text = "\u2605",
            FontSize = 12,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = group.IsFavorite ? Visibility.Visible : Visibility.Collapsed,
            Foreground = (Brush)FindResource("PaletteOrangeBrush")
        };
        var nameStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        nameStack.Children.Add(nameText);
        nameStack.Children.Add(favStar);
        var runtimeText = new TextBlock
        {
            Margin = new Thickness(10, 0, 0, 0),
            FontSize = (double)FindResource("FontSizeCaption"),
            Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        runtimeText.Text = string.Format(
            CultureInfo.CurrentCulture,
            T("NetworkAccelerationPage_GroupRuntimeFormat", "{0}/{1} selected  {2} active"),
            enabledCount,
            totalCount,
            0);
        _groupRuntimeLabels[id] = runtimeText;
        nameStack.Children.Add(runtimeText);

        // Header row: [checkbox][brand icon][name+star]. CardExpander provides the chevron.
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(groupCheckBox, 0);
        Grid.SetColumn(brandIcon, 1);
        Grid.SetColumn(nameStack, 2);
        headerGrid.Children.Add(groupCheckBox);
        headerGrid.Children.Add(brandIcon);
        headerGrid.Children.Add(nameStack);

        // Sub-items content: always populated; CardExpander controls show/hide.
        var subPanel = new StackPanel { Margin = new Thickness(12, 8, 0, 0) };
        if (group.SubItems is not null)
        {
            var query = _targetSearchBox?.Text?.Trim() ?? string.Empty;
            var showAllSubItems = string.IsNullOrWhiteSpace(query) ||
                                  group.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase);
            foreach (var sub in group.SubItems)
            {
                if (showAllSubItems ||
                    sub.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    sub.Domain.Contains(query, StringComparison.OrdinalIgnoreCase))
                    subPanel.Children.Add(CreateSubItemRow(group, sub));
            }
        }

        var expander = new CustomControls.CardExpander
        {
            Style = (Style)FindResource("NaServiceExpanderStyle"),
            Header = headerGrid,
            Content = subPanel,
            IsExpanded = isExpanded
        };
        expander.Expanded += (_, _) => _expandedGroupIds.Add(id);
        expander.Collapsed += (_, _) => _expandedGroupIds.Remove(id);

        AutomationProperties.SetAutomationId(expander, $"NetworkAccelerationDomain_{id}");
        return expander;
    }

    private Grid CreateSubItemRow(NetworkDomainGroup group, NetworkDomainSubItem sub)
    {
        var checkBox = new CheckBox
        {
            IsChecked = sub.Enabled,
            VerticalAlignment = VerticalAlignment.Center
        };
        checkBox.Click += async (_, _) =>
        {
            sub.Enabled = checkBox.IsChecked == true;
            if (sub.Enabled)
                group.Enabled = true;
            else if ((group.Domains?.Count ?? 0) == 0 &&
                     !(group.SubItems?.Any(item => item.Enabled) ?? false))
                group.Enabled = false;
            try { await _acceleration.SaveConfigAsync(); } catch { }
            BuildServiceList();
            RefreshUi();
        };

        var nameText = new TextBlock
        {
            Text = sub.DisplayName,
            Style = (Style)FindResource("NaSubItemNameStyle")
        };

        // Beta tag
        UIElement? betaTag = null;
        if (sub.IsBeta)
        {
            var betaBorder = new Border
            {
                Style = (Style)FindResource("NaBetaTagStyle"),
                Child = new TextBlock
                {
                    Text = "Beta",
                    FontSize = 10,
                    Foreground = (Brush)FindResource("TextOnAccentFillColorPrimaryBrush")
                }
            };
            betaTag = betaBorder;
        }

        var domainText = new TextBlock
        {
            Text = sub.Domain,
            Style = (Style)FindResource("NaSubItemDomainStyle")
        };

        var grid = new Grid { MinHeight = 28 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(checkBox, 0);
        Grid.SetColumn(nameText, 1);
        if (betaTag is not null)
            Grid.SetColumn(betaTag, 2);
        Grid.SetColumn(domainText, 3);

        grid.Children.Add(checkBox);
        grid.Children.Add(nameText);
        if (betaTag is not null)
            grid.Children.Add(betaTag);
        grid.Children.Add(domainText);

        var wrapper = new Border
        {
            Style = (Style)FindResource("NaSubItemRowStyle"),
            Child = grid
        };

        var outerGrid = new Grid();
        outerGrid.Children.Add(wrapper);
        return outerGrid;
    }

    private Border CreateBrandIcon(string? iconKey)
    {
        var background = iconKey switch
        {
            "SteamLogo" => new SolidColorBrush(Color.FromRgb(71, 143, 232)),
            "GitHubLogo" => new SolidColorBrush(Color.FromRgb(98, 98, 98)),
            "TwitchLogo" => new SolidColorBrush(Color.FromRgb(10, 114, 197)),
            "RobloxLogo" => new SolidColorBrush(Color.FromRgb(239, 241, 243)),
            "CdnLogo" => new SolidColorBrush(Color.FromRgb(239, 241, 243)),
            _ => (Brush)(TryFindResource("ControlFillColorSecondaryBrush") ?? Brushes.Transparent)
        };

        var foreground = iconKey is "RobloxLogo" or "CdnLogo"
            ? new SolidColorBrush(Color.FromRgb(101, 106, 113))
            : Brushes.White;

        if (iconKey is not null && BrandIconGeometry.TryGetValue(iconKey, out var geometryData))
        {
            var path = new Path
            {
                Data = Geometry.Parse(geometryData),
                Fill = iconKey == "CdnLogo" ? Brushes.Transparent : foreground,
                Stroke = iconKey == "CdnLogo" ? foreground : null,
                StrokeThickness = iconKey == "CdnLogo" ? 1.4 : 0,
                Width = 22,
                Height = 22,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true
            };

            return new Border
            {
                Style = (Style)FindResource("NaBrandIconStyle"),
                Background = background,
                Child = path
            };
        }

        return new Border
        {
            Style = (Style)FindResource("NaBrandIconStyle"),
            Background = background,
            Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(iconKey) ? "?" : iconKey[..1].ToUpperInvariant(),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    // ─────────────────────────────────────────────────────
    // Auto start/stop (service list checkbox → acceleration lifecycle)
    // ─────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────
    // Refresh UI
    // ─────────────────────────────────────────────────────

    private void RefreshUi()
    {
        if (_suppressEvents)
            return;

        try
        {
            _suppressEvents = true;

            // Mode combo selection
            if (_modeComboBox is not null)
            {
                var currentMode = ToSelectableMode(_acceleration.Config.Mode);
                foreach (ModeOption item in _modeComboBox.Items)
                {
                    if (item.Mode == currentMode)
                    {
                        _modeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Advanced text
            if (_modeText is not null)
                _modeText.Text = ModeFullLabel(_acceleration.Config.Mode);
            if (_portText is not null)
                _portText.Text = string.Format(
                    T("NetworkAccelerationPage_PortFormat", "Port: {0}"),
                    _acceleration.Config.ListenPort);

            var isRunning = _acceleration.IsRunning;
            var selectedTargets = GetSelectedTargetCount();
            if (_modeComboBox is not null)
                _modeComboBox.IsEnabled = !isRunning && !_isBusy;
            if (_runStateText is not null)
                _runStateText.Text = isRunning
                    ? T("NetworkAccelerationPage_State_Connected", "Connected")
                    : T("NetworkAccelerationPage_State_Idle", "Idle");
            if (_runHealthText is not null && !isRunning)
                _runHealthText.Text = FormatStoppedHealth();
            if (_runPortText is not null)
                _runPortText.Text = _acceleration.Config.ListenPort.ToString(CultureInfo.CurrentCulture);
            if (_selectedTargetsText is not null)
                _selectedTargetsText.Text = selectedTargets.ToString(CultureInfo.CurrentCulture);
            if (_runStateDot is not null)
                _runStateDot.Fill = (Brush)FindResource(isRunning ? "StatusSuccessBrush" : "TextFillColorTertiaryBrush");
            if (_runErrorText is not null)
            {
                var canStart = CanStart(out var reason);
                _runErrorText.Text = !isRunning && !canStart
                    ? reason
                    : string.Empty;
                _runErrorText.Visibility = string.IsNullOrWhiteSpace(_runErrorText.Text)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            UpdateTrafficSectionVisibility(isRunning);
        }
        finally
        {
            _suppressEvents = false;
        }

        ToolbarStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private string ModeFullLabel(NetworkAccelerationMode mode) => mode switch
    {
        NetworkAccelerationMode.SystemProxy => T("NetworkAccelerationPage_Mode_SystemProxy", "System proxy"),
        NetworkAccelerationMode.Hosts => T("NetworkAccelerationPage_Mode_Hosts", "Hosts file"),
        NetworkAccelerationMode.DiagnosticsOnly => T("NetworkAccelerationPage_Mode_DiagnosticsOnly", "Diagnostics only"),
        NetworkAccelerationMode.Off => T("NetworkAccelerationPage_State_Idle", "Idle"),
        _ => mode.ToString()
    };

    private int GetSelectedTargetCount() =>
        (_acceleration.Config.DomainGroups ?? [])
            .Sum(GetGroupSelectedCount);

    private NetworkAccelerationMode GetSelectedMode() =>
        _modeComboBox?.SelectedItem is ModeOption option
            ? option.Mode
            : ToSelectableMode(_acceleration.Config.Mode);

    private bool CanStart(out string reason)
    {
        if (!_acceleration.IsBackendReady)
        {
            reason = T("NetworkAccelerationPage_BackendMissing_Hint", "Proxy worker is unavailable");
            return false;
        }

        if (GetSelectedTargetCount() <= 0)
        {
            reason = T("NetworkAccelerationPage_SelectGroupsFirst_Hint", "Select at least one target");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    // ─────────────────────────────────────────────────────
    // Start / Stop (called by auto-toggle)
    // ─────────────────────────────────────────────────────

    public async Task ToggleAccelerationFromToolbarAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;

        try
        {
            if (_acceleration.IsRunning)
                await StopAsync();
            else
                await StartAsync();
        }
        finally
        {
            _isBusy = false;
            RefreshUi();
        }
    }

    private async Task StartAsync()
    {
        ResetTrafficView();
        ClearRuntimeLists();
        try
        {
            var selectedMode = GetSelectedMode();
            if (selectedMode == NetworkAccelerationMode.DiagnosticsOnly)
            {
                // DiagnosticsOnly is a preview mode and never reports a real running proxy.
                // The explicit toolbar start action opts into the actual SystemProxy mode.
                _acceleration.Config.Mode = NetworkAccelerationMode.SystemProxy;
            }
            else if (_acceleration.Config.Mode == NetworkAccelerationMode.Off)
            {
                _acceleration.Config.Mode = selectedMode;
            }

            if (!CanStart(out var reason))
            {
                if (_runErrorText is not null)
                    _runErrorText.Text = reason;
                return;
            }

            _acceleration.Config.AccelerationEnabled = true;
            await _acceleration.SaveConfigAsync();

            var started = await _acceleration.StartAsync();
            UpdateTrafficSectionVisibility(started && _acceleration.IsRunning);
            if (started && _acceleration.IsRunning)
            {
                StartTrafficPolling();
                StartRuntimePolling();
            }
            else if (_runErrorText is not null)
            {
                _runErrorText.Text = T("NetworkAccelerationPage_StartFailed", "Acceleration could not be started");
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Network acceleration start failed: {ex.Message}", ex);
            UpdateTrafficSectionVisibility(false);
            ClearRuntimeLists();
            if (_runErrorText is not null)
                _runErrorText.Text = T("NetworkAccelerationPage_StartFailed", "Acceleration could not be started");
        }
    }

    private async Task StopAsync()
    {
        try
        {
            await _acceleration.StopAsync();
            ResetTrafficView();
            ClearRuntimeLists();
            UpdateTrafficSectionVisibility(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Network acceleration stop failed: {ex.Message}", ex);
            ResetTrafficView();
            ClearRuntimeLists();
            UpdateTrafficSectionVisibility(false);
        }
    }

    private void UpdateTrafficSectionVisibility(bool? isRunning = null)
    {
        if (_trafficSection is null)
            return;

        _trafficSection.Visibility = (isRunning ?? _acceleration.IsRunning)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ─────────────────────────────────────────────────────
    // Mode selection
    // ─────────────────────────────────────────────────────

    private async void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _isBusy || _modeComboBox?.SelectedItem is not ModeOption opt)
            return;

        _acceleration.Config.Mode = opt.Mode;

        try
        {
            await _acceleration.SaveConfigAsync();
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Mode change save failed: {ex.Message}", ex);
        }

        RefreshUi();
    }

    private void TargetSearchBox_TextChanged(object sender, TextChangedEventArgs e) => BuildServiceList();

    // ─────────────────────────────────────────────────────
    // Restore button (danger zone)
    // ─────────────────────────────────────────────────────

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        _isBusy = true;
        if (_restoreButton is not null) _restoreButton.IsEnabled = false;

        try
        {
            await _acceleration.StopAsync();
            _recovery.TryRestoreFromSnapshot(out _);
            _acceleration.Config.Mode = NetworkAccelerationMode.Off;
            await _acceleration.SaveConfigAsync();
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Network restore failed: {ex.Message}", ex);
        }
        finally
        {
            _isBusy = false;
            if (_restoreButton is not null) _restoreButton.IsEnabled = true;
            RefreshUi();
        }
    }
}
