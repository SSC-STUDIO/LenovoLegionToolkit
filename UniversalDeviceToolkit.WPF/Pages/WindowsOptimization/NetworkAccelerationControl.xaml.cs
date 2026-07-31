using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

public partial class NetworkAccelerationControl : UserControl
{
    private readonly INetworkAccelerationService _acceleration;
    private readonly INetworkDiagnosticsService _diagnostics;
    private readonly INetworkStateRecoveryService _recovery;
    private bool _suppressEvents;
    private bool _isBusy;
    private CancellationTokenSource? _diagnosticsCts;

    /// <summary>Tracks which service groups are expanded in the tree view.</summary>
    private readonly HashSet<string> _expandedGroupIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tracks which diagnostic panels are collapsed.</summary>
    private readonly HashSet<string> _collapsedDiagPanels = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Prevents auto-start from firing when the user manually stops acceleration.</summary>
    private bool _userStoppedManually;

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

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
        BuildModeCombo();
        InitDiagnosticsCombos();
        BuildServiceList();
        RefreshUi();
    }

    private void NetworkAccelerationControl_Unloaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged -= NetworkAccelerationControl_IsVisibleChanged;
        _diagnosticsCts?.Cancel();
        _diagnosticsCts?.Dispose();
        _diagnosticsCts = null;
    }

    private void NetworkAccelerationControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && IsLoaded)
            RefreshUi();
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

        if (_dnsDohUrlCombo is not null)
        {
            _dnsDohUrlCombo.Items.Clear();
            _dnsDohUrlCombo.Items.Add("https://doh.pub/dns-query");
            _dnsDohUrlCombo.Items.Add("https://dns.alidns.com/dns-query");
            _dnsDohUrlCombo.Items.Add("https://cloudflare-dns.com/dns-query");
            _dnsDohUrlCombo.Text = "https://doh.pub/dns-query";
        }
    }

    // ─────────────────────────────────────────────────────
    // Collapsible diagnostic panels
    // ─────────────────────────────────────────────────────

    private void ToggleDiagPanel(string panelId, FrameworkElement? contentPanel, System.Windows.Controls.Button? arrowButton)
    {
        var isCollapsed = !_collapsedDiagPanels.Add(panelId);
        if (!isCollapsed)
            _collapsedDiagPanels.Remove(panelId);

        if (contentPanel is not null)
            contentPanel.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;

        if (arrowButton is not null)
            arrowButton.Content = isCollapsed ? "▸" : "▾";
    }

    private void NatCollapseArrow_Click(object sender, RoutedEventArgs e) =>
        ToggleDiagPanel("nat", _natContentPanel, _natCollapseArrow);

    private void DnsCollapseArrow_Click(object sender, RoutedEventArgs e) =>
        ToggleDiagPanel("dns", _dnsContentPanel, _dnsCollapseArrow);

    private void Ipv6CollapseArrow_Click(object sender, RoutedEventArgs e) =>
        ToggleDiagPanel("ipv6", _ipv6ContentPanel, _ipv6CollapseArrow);

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
            var result = await NatTypeDetector.CheckAsync(stunHost, 3478, cts.Token).ConfigureAwait(true);

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
            var dnsServer = (_dnsServerInput?.Text ?? string.Empty).Trim();
            var dohEnabled = _dnsDohToggle?.IsChecked == true;
            var dohUrl = (_dnsDohUrlCombo?.Text ?? string.Empty).Trim();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // System DNS resolve
            var sysResult = await DnsDiagnosticsService.ResolveSystemAsync(domain, cts.Token).ConfigureAwait(true);

            // Custom DNS resolve (if server specified)
            DnsProbeResult? customResult = null;
            if (!string.IsNullOrWhiteSpace(dnsServer))
            {
                try
                {
                    customResult = await DnsDiagnosticsService.ResolveCustomServerAsync(domain, dnsServer, cts.Token).ConfigureAwait(true);
                }
                catch { /* non-fatal */ }
            }

            // DoH resolve (if enabled)
            DnsProbeResult? dohResult = null;
            if (dohEnabled && !string.IsNullOrWhiteSpace(dohUrl))
            {
                try
                {
                    dohResult = await DnsDiagnosticsService.ResolveDohAsync(domain, dohUrl, cts.Token).ConfigureAwait(true);
                }
                catch { /* non-fatal */ }
            }

            sw.Stop();

            // Pick the fastest successful result's latency
            var latencyMs = new[] { sysResult, customResult, dohResult }
                .Where(r => r is not null && r.Success)
                .Select(r => r!.ElapsedMs)
                .DefaultIfEmpty(-1)
                .Min();

            if (_dnsLatencyText is not null)
                _dnsLatencyText.Text = latencyMs >= 0
                    ? string.Format(T("NaDiag_LatencyFormat", "{0} ms"), latencyMs)
                    : T("NaDiag_Failed", "Failed");
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
            var result = await Ipv6Detector.CheckAsync(cts.Token).ConfigureAwait(true);

            if (_ipv6SupportText is not null)
                _ipv6SupportText.Text = result.Supported
                    ? T("NaDiag_Supported", "Supported")
                    : T("NaDiag_NotSupported", "Not supported");

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
        var groups = _acceleration.Config.DomainGroups;
        if (groups is null || groups.Count == 0)
        {
            if (_domainGroupsEmptyState is not null)
                _domainGroupsEmptyState.Visibility = Visibility.Visible;
            return;
        }

        if (_domainGroupsEmptyState is not null)
            _domainGroupsEmptyState.Visibility = Visibility.Collapsed;

        var ordered = groups
            .OrderByDescending(g => g.IsFavorite)
            .ThenBy(g => groups.IndexOf(g))
            .ToList();

        foreach (var group in ordered)
            _serviceListPanel.Items.Add(CreateServiceGroupRow(group));
    }

    private Border CreateServiceGroupRow(NetworkDomainGroup group)
    {
        var id = group.Id;
        var isExpanded = _expandedGroupIds.Contains(id);
        var enabledCount = group.SubItems?.Count(s => s.Enabled) ?? 0;
        var totalCount = group.SubItems?.Count ?? 0;
        var allEnabled = totalCount > 0 && enabledCount == totalCount;
        var someEnabled = enabledCount > 0 && !allEnabled;

        // Group-level checkbox (three-state)
        var groupCheckBox = new CheckBox
        {
            IsChecked = allEnabled ? true : someEnabled ? null : false,
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
            try { await _acceleration.SaveConfigAsync().ConfigureAwait(true); } catch { }
            BuildServiceList();

            // Auto start/stop: checkbox triggers acceleration lifecycle.
            await AutoToggleAccelerationAsync(newState);
        };

        // Brand icon placeholder
        var brandIcon = new Border
        {
            Style = (Style)FindResource("NaBrandIconStyle"),
            Background = GetBrandBrush(group.IconKey),
            Child = new TextBlock
            {
                Text = GetBrandInitial(group.DisplayName),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        // Group name
        var nameText = new TextBlock
        {
            Text = group.DisplayName,
            Style = (Style)FindResource("NaServiceNameStyle")
        };

        // Favorite star
        var favStar = new TextBlock
        {
            Text = "★",
            FontSize = 12,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = group.IsFavorite ? Visibility.Visible : Visibility.Collapsed,
            Foreground = (Brush)FindResource("PaletteOrangeBrush")
        };

        // Expand/collapse arrow
        var hasSubs = group.SubItems is { Count: > 0 };
        var arrowText = new TextBlock
        {
            Text = isExpanded ? "▾" : "▸",
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
            Visibility = hasSubs ? Visibility.Visible : Visibility.Collapsed
        };

        // Header row
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(groupCheckBox, 0);
        Grid.SetColumn(brandIcon, 1);
        Grid.SetColumn(nameText, 2);
        Grid.SetColumn(favStar, 3);
        Grid.SetColumn(arrowText, 4);
        headerGrid.Children.Add(groupCheckBox);
        headerGrid.Children.Add(brandIcon);
        headerGrid.Children.Add(nameText);
        headerGrid.Children.Add(favStar);
        headerGrid.Children.Add(arrowText);

        var headerBorder = new Border
        {
            Style = (Style)FindResource("NaServiceHeaderStyle"),
            Child = headerGrid
        };
        if (hasSubs)
        {
            headerBorder.MouseLeftButtonUp += (_, _) =>
            {
                if (!_expandedGroupIds.Add(id))
                    _expandedGroupIds.Remove(id);
                BuildServiceList();
            };
        }

        // Sub-items panel
        var subPanel = new StackPanel { Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed };
        if (isExpanded && group.SubItems is not null)
        {
            foreach (var sub in group.SubItems)
                subPanel.Children.Add(CreateSubItemRow(sub));
        }

        var outerStack = new StackPanel();
        outerStack.Children.Add(headerBorder);
        outerStack.Children.Add(subPanel);

        var result = new Border
        {
            Style = (Style)FindResource("NaServiceGroupStyle"),
            Child = outerStack
        };
        AutomationProperties.SetAutomationId(result, $"NetworkAccelerationDomain_{id}");
        return result;
    }

    private Grid CreateSubItemRow(NetworkDomainSubItem sub)
    {
        var checkBox = new CheckBox
        {
            IsChecked = sub.Enabled,
            VerticalAlignment = VerticalAlignment.Center
        };
        checkBox.Click += async (_, _) =>
        {
            sub.Enabled = checkBox.IsChecked == true;
            try { await _acceleration.SaveConfigAsync().ConfigureAwait(true); } catch { }
            BuildServiceList();
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

    private static string GetBrandInitial(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";
        return name.Substring(0, 1).ToUpperInvariant();
    }

    private Brush GetBrandBrush(string? iconKey)
    {
        var brushKey = iconKey switch
        {
            "SteamLogo" => "ChartUtilizationBrush",
            "GitHubLogo" => "TextFillColorSecondaryBrush",
            "TwitchLogo" => "AccentFillColorDefaultBrush",
            _ => "ControlFillColorSecondaryBrush"
        };
        return (Brush)(TryFindResource(brushKey) ?? FindResource("ControlFillColorSecondaryBrush"));
    }

    // ─────────────────────────────────────────────────────
    // Auto start/stop (service list checkbox → acceleration lifecycle)
    // ─────────────────────────────────────────────────────

    private async Task AutoToggleAccelerationAsync(bool shouldRun)
    {
        if (_isBusy)
            return;

        var isRunning = _acceleration.Config.Mode != NetworkAccelerationMode.Off;
        if (shouldRun == isRunning)
            return;

        // Respect manual stop: don't auto-start after user manually stopped.
        if (shouldRun && _userStoppedManually)
            return;

        _isBusy = true;
        try
        {
            if (shouldRun)
                await StartAsync();
            else
                await StopAsync();
        }
        finally
        {
            _isBusy = false;
            RefreshUi();
        }
    }

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

            RefreshMetrics();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private string ModeFullLabel(NetworkAccelerationMode mode) => mode switch
    {
        NetworkAccelerationMode.SystemProxy => T("NetworkAccelerationPage_Mode_SystemProxy", "System proxy"),
        NetworkAccelerationMode.Hosts => T("NetworkAccelerationPage_Mode_Hosts", "Hosts file"),
        NetworkAccelerationMode.DiagnosticsOnly => T("NetworkAccelerationPage_Mode_DiagnosticsOnly", "Diagnostics only"),
        NetworkAccelerationMode.Off => T("NetworkAccelerationPage_State_Idle", "Idle"),
        _ => mode.ToString()
    };

    private void RefreshMetrics()
    {
        if (_metricLatencyValue is not null) _metricLatencyValue.Text = "—";
        if (_metricUploadValue is not null) _metricUploadValue.Text = "—";
        if (_metricDownloadValue is not null) _metricDownloadValue.Text = "—";
        if (_metricConnectionsValue is not null) _metricConnectionsValue.Text = "—";
        if (_metricRulesValue is not null)
        {
            var groups = _acceleration.Config.DomainGroups;
            _metricRulesValue.Text = (groups?.Where(g => g.Enabled).SelectMany(g => g.Domains ?? []).Count() ?? 0).ToString("0");
        }
    }

    // ─────────────────────────────────────────────────────
    // Start / Stop (called by auto-toggle)
    // ─────────────────────────────────────────────────────

    private async Task StartAsync()
    {
        _userStoppedManually = false;
        try
        {
            if (_acceleration.Config.Mode == NetworkAccelerationMode.Off && _modeComboBox?.SelectedItem is ModeOption opt)
                _acceleration.Config.Mode = opt.Mode;

            await _acceleration.StartAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Network acceleration start failed: {ex.Message}", ex);
        }
    }

    private async Task StopAsync()
    {
        try
        {
            await _acceleration.StopAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Network acceleration stop failed: {ex.Message}", ex);
        }
    }

    // ─────────────────────────────────────────────────────
    // Mode selection
    // ─────────────────────────────────────────────────────

    private async void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _isBusy || _modeComboBox?.SelectedItem is not ModeOption opt)
            return;

        var wasRunning = _acceleration.Config.Mode != NetworkAccelerationMode.Off;
        _acceleration.Config.Mode = opt.Mode;

        try
        {
            await _acceleration.SaveConfigAsync().ConfigureAwait(true);
            if (wasRunning)
            {
                _isBusy = true;
                try
                {
                    await _acceleration.StopAsync().ConfigureAwait(true);
                    await _acceleration.StartAsync().ConfigureAwait(true);
                }
                finally
                {
                    _isBusy = false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Mode change save failed: {ex.Message}", ex);
        }

        RefreshUi();
    }

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
            await _acceleration.StopAsync().ConfigureAwait(true);
            _recovery.TryRestoreFromSnapshot(out _);
            _acceleration.Config.Mode = NetworkAccelerationMode.Off;
            await _acceleration.SaveConfigAsync().ConfigureAwait(true);
            _userStoppedManually = true;
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
