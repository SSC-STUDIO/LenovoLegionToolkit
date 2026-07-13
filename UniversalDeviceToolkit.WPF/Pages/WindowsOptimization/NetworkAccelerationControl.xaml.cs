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
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Network;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

public partial class NetworkAccelerationControl : UserControl
{
    private readonly INetworkAccelerationService _acceleration;
    private readonly INetworkDiagnosticsService _diagnostics;
    private readonly INetworkStateRecoveryService _recovery;
    private readonly ApplicationSettings _settings;
    private bool _suppressEvents;
    private bool _isBusy;
    private bool _startFailed;
    private ConnectionUiState _uiState = ConnectionUiState.Idle;
    private CancellationTokenSource? _diagnosticsCts;

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public NetworkAccelerationControl()
    {
        _acceleration = IoCContainer.Resolve<INetworkAccelerationService>();
        _diagnostics = IoCContainer.Resolve<INetworkDiagnosticsService>();
        _recovery = IoCContainer.Resolve<INetworkStateRecoveryService>();
        _settings = IoCContainer.Resolve<ApplicationSettings>();
        InitializeComponent();
        Loaded += NetworkAccelerationControl_Loaded;
        Unloaded += NetworkAccelerationControl_Unloaded;
        IsVisibleChanged += NetworkAccelerationControl_IsVisibleChanged;
    }

    private void NetworkAccelerationControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_metricsHeadingText is not null)
            _metricsHeadingText.Text = T("NetworkAccelerationPage_MetricsHeading", "Overview");
        BuildModeCombo();
        BuildDomainGroupTiles();
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
        // No continuous sampling loop; refresh only when visible.
        if ((bool)e.NewValue && IsLoaded)
            RefreshUi();
    }

    /// <summary>
    /// Plain data item for the mode combo. Using ComboBoxItem instances as Items causes the
    /// closed-field ContentPresenter to re-host the item visual and double-paint / overlap text.
    /// </summary>
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
            // Data items (not ComboBoxItem): SelectionBoxItem is a string-like object, single paint.
            _modeComboBox.Items.Add(new ModeOption(
                T("NetworkAccelerationPage_Mode_SystemProxy", "System proxy (PAC / local proxy)"),
                NetworkAccelerationMode.SystemProxy));
            _modeComboBox.Items.Add(new ModeOption(
                T("NetworkAccelerationPage_Mode_Hosts", "Hosts rewrite (UDT-marked block)"),
                NetworkAccelerationMode.Hosts));
            _modeComboBox.Items.Add(new ModeOption(
                T("NetworkAccelerationPage_Mode_DiagnosticsOnly", "Diagnostics only (no system changes)"),
                NetworkAccelerationMode.DiagnosticsOnly));
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void BuildDomainGroupTiles()
    {
        if (_domainGroupsPanel is null)
            return;

        _domainGroupsPanel.Items.Clear();
        var groups = _acceleration.Config.DomainGroups;
        if (groups is null || groups.Count == 0)
        {
            _acceleration.Config.DomainGroups = BuiltinDomainGroups.CreateDefaults();
            groups = _acceleration.Config.DomainGroups;
        }

        foreach (var group in groups)
        {
            var domainCount = group.Domains?.Count ?? 0;
            var tile = CreateDomainTile(group.Id, group.DisplayName, domainCount, group.Enabled);
            _domainGroupsPanel.Items.Add(tile);
        }
    }

    private Border CreateDomainTile(string id, string displayName, int domainCount, bool enabled)
    {
        var title = new TextBlock
        {
            Text = displayName,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = (Brush)FindResource("TextFillColorPrimaryBrush")
        };
        var subtitle = new TextBlock
        {
            Text = string.Format(
                T("NetworkAccelerationPage_DomainCountFormat", "{0} domains"),
                domainCount),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = (Brush)FindResource("TextFillColorSecondaryBrush")
        };

        // Plain status text only — no pill/ellipse chrome around "Enabled".
        var stateLabel = new TextBlock
        {
            Name = "StateLabel",
            Text = enabled
                ? T("NetworkAccelerationPage_DomainEnabled", "Enabled")
                : T("NetworkAccelerationPage_DomainDisabled", "Off"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = (Brush)FindResource(enabled ? "PaletteGreenBrush" : "TextFillColorTertiaryBrush")
        };

        var stack = new StackPanel();
        stack.Children.Add(title);
        stack.Children.Add(subtitle);
        stack.Children.Add(stateLabel);

        var tile = new Border
        {
            Tag = id,
            Child = stack,
            Width = 140,
            MinHeight = 78,
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(10, 8, 10, 8),
            CornerRadius = TryCornerRadius("CornerRadiusControl", 10),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Focusable = true,
            SnapsToDevicePixels = true,
            Background = (Brush)FindResource(enabled ? "ControlFillColorSecondaryBrush" : "ControlFillColorDefaultBrush"),
            BorderBrush = (Brush)FindResource(enabled ? "AccentFillColorDefaultBrush" : "ControlStrokeColorDefaultBrush")
        };

        AutomationProperties.SetAutomationId(tile, $"NetworkAccelerationDomain_{id}");
        AutomationProperties.SetName(tile, displayName);

        tile.MouseLeftButtonUp += DomainTile_MouseLeftButtonUp;
        tile.KeyDown += DomainTile_KeyDown;
        tile.MouseEnter += (_, _) =>
        {
            if (tile.Tag is string)
                tile.Opacity = 0.94;
        };
        tile.MouseLeave += (_, _) => tile.Opacity = 1;

        return tile;
    }

    private static CornerRadius TryCornerRadius(string key, double fallback)
    {
        if (Application.Current?.TryFindResource(key) is CornerRadius cr)
            return cr;
        return new CornerRadius(fallback);
    }

    private async void DomainTile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border tile)
            await ToggleDomainTileAsync(tile).ConfigureAwait(true);
    }

    private async void DomainTile_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not Border tile)
            return;
        if (e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            await ToggleDomainTileAsync(tile).ConfigureAwait(true);
        }
    }

    private async Task ToggleDomainTileAsync(Border tile)
    {
        if (_suppressEvents || _isBusy || tile.Tag is not string id)
            return;

        try
        {
            var group = _acceleration.Config.DomainGroups?.FirstOrDefault(g =>
                string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));
            if (group is null)
                return;

            group.Enabled = !group.Enabled;
            await _acceleration.SaveConfigAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("Failed to toggle network acceleration domain tile; UI kept alive.", ex);
        }

        RefreshUi();
    }

    private enum ConnectionUiState
    {
        Idle,
        Starting,
        Connected,
        Stopping,
        Restoring,
        Failed,
        DiagnosticsOnly,
        WorkerMissing
    }

    private enum StatusVisual
    {
        Neutral,
        Running,
        Caution,
        Danger,
        Info
    }

    private void ApplyStatusVisual(StatusVisual visual)
    {
        // Soft-tint chip: low-alpha fill + mid-alpha border + solid accent dot/label.
        // Always content-hugging (HorizontalAlignment=Left) — never Stretch.
        var accentKey = visual switch
        {
            StatusVisual.Running => "PaletteGreenBrush",
            StatusVisual.Caution => "PaletteOrangeBrush",
            StatusVisual.Danger => "PaletteRedBrush",
            StatusVisual.Info => "PaletteLightBlueBrush",
            _ => null
        };

        if (accentKey is null || ResolveSolidColor(accentKey) is not { } accent)
        {
            ApplyNeutralStatusChrome();
            return;
        }

        if (_statusPill is not null)
        {
            _statusPill.Background = SoftBrush(accent, alpha: 0x22);
            _statusPill.BorderBrush = SoftBrush(accent, alpha: 0x55);
        }

        if (_statusDot is not null)
            _statusDot.Fill = new SolidColorBrush(accent);

        if (_statusText is not null)
            _statusText.Foreground = SoftBrush(accent, alpha: 0xEE);
    }

    private void ApplyNeutralStatusChrome()
    {
        if (_statusPill is not null)
        {
            _statusPill.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
            _statusPill.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        }

        if (_statusDot is not null)
            _statusDot.SetResourceReference(Shape.FillProperty, "TextFillColorSecondaryBrush");

        if (_statusText is not null)
            _statusText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
    }

    private Color? ResolveSolidColor(string brushKey)
    {
        try
        {
            if (TryFindResource(brushKey) is SolidColorBrush solid)
                return solid.Color;
            if (TryFindResource(brushKey) is Color color)
                return color;
        }
        catch
        {
            // Resource missing in design-time / tests — fall back to neutral chrome.
        }

        return null;
    }

    private static SolidColorBrush SoftBrush(Color baseColor, byte alpha) =>
        new(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));

    private static void TrySetResourceBrush(FrameworkElement? element, DependencyProperty property, string key, string fallbackKey)
    {
        if (element is null)
            return;
        try
        {
            element.SetResourceReference(property, key);
        }
        catch
        {
            element.SetResourceReference(property, fallbackKey);
        }
    }

    private ConnectionUiState ResolveUiState()
    {
        if (_isBusy && _uiState is ConnectionUiState.Starting or ConnectionUiState.Stopping or ConnectionUiState.Restoring)
            return _uiState;

        var config = _acceleration.Config;
        if (_startFailed)
            return ConnectionUiState.Failed;
        if (!config.AccelerationEnabled || config.Mode is NetworkAccelerationMode.Off)
            return ConnectionUiState.Idle;
        if (!_acceleration.IsBackendReady)
            return ConnectionUiState.WorkerMissing;
        if (config.Mode is NetworkAccelerationMode.DiagnosticsOnly)
            return ConnectionUiState.DiagnosticsOnly;
        if (_acceleration.IsRunning)
            return ConnectionUiState.Connected;
        return ConnectionUiState.Idle;
    }

    private string StatusLabelFor(ConnectionUiState state) => state switch
    {
        ConnectionUiState.Starting => T("NetworkAccelerationPage_State_Starting", "Starting…"),
        ConnectionUiState.Connected => T("NetworkAccelerationPage_State_Connected", "Connected"),
        ConnectionUiState.Stopping => T("NetworkAccelerationPage_State_Stopping", "Stopping…"),
        ConnectionUiState.Restoring => T("NetworkAccelerationPage_State_Restoring", "Restoring…"),
        ConnectionUiState.Failed => T("NetworkAccelerationPage_State_Failed", "Start failed"),
        // Short chip label; full safety copy is tooltip + mode summary line.
        ConnectionUiState.DiagnosticsOnly => T("NetworkAccelerationPage_ModeShort_DiagnosticsOnly", "Diagnostics only"),
        ConnectionUiState.WorkerMissing => T("NetworkAccelerationPage_StatusWorkerMissing", "Worker binary not found — build/install UniversalDeviceToolkit.NetworkProxy.exe"),
        _ => T("NetworkAccelerationPage_State_Idle", "Not started")
    };

    private string StatusDetailFor(ConnectionUiState state) => state switch
    {
        ConnectionUiState.DiagnosticsOnly => T(
            "NetworkAccelerationPage_StatusDiagnosticsOnly",
            "Diagnostics only (no system network changes)"),
        ConnectionUiState.WorkerMissing => T(
            "NetworkAccelerationPage_StatusWorkerMissing",
            "Worker binary not found — build/install UniversalDeviceToolkit.NetworkProxy.exe"),
        ConnectionUiState.Connected => ModeFullLabel(_acceleration.Config.Mode),
        ConnectionUiState.Failed => T("NetworkAccelerationPage_State_Failed", "Start failed"),
        _ => StatusLabelFor(state)
    };

    private string ModeFullLabel(NetworkAccelerationMode mode) => mode switch
    {
        NetworkAccelerationMode.Hosts => T("NetworkAccelerationPage_Mode_Hosts", "Hosts rewrite (UDT-marked block)"),
        NetworkAccelerationMode.DiagnosticsOnly => T("NetworkAccelerationPage_Mode_DiagnosticsOnly", "Diagnostics only (no system changes)"),
        NetworkAccelerationMode.SystemProxy => T("NetworkAccelerationPage_Mode_SystemProxy", "System proxy (PAC / local proxy)"),
        _ => T("NetworkAccelerationPage_Mode_SystemProxy", "System proxy (PAC / local proxy)")
    };

    private string ModeShortLabel(NetworkAccelerationMode mode) => mode switch
    {
        NetworkAccelerationMode.Hosts => T("NetworkAccelerationPage_ModeShort_Hosts", "Hosts"),
        NetworkAccelerationMode.DiagnosticsOnly => T("NetworkAccelerationPage_ModeShort_DiagnosticsOnly", "Diagnostics only"),
        NetworkAccelerationMode.SystemProxy => T("NetworkAccelerationPage_ModeShort_SystemProxy", "System proxy"),
        NetworkAccelerationMode.Off => T("NetworkAccelerationPage_State_Idle", "Not started"),
        _ => T("NetworkAccelerationPage_ModeShort_SystemProxy", "System proxy")
    };

    private StatusVisual VisualFor(ConnectionUiState state) => state switch
    {
        ConnectionUiState.Connected => StatusVisual.Running,
        ConnectionUiState.Starting or ConnectionUiState.Stopping or ConnectionUiState.Restoring => StatusVisual.Caution,
        ConnectionUiState.Failed or ConnectionUiState.WorkerMissing => StatusVisual.Danger,
        ConnectionUiState.DiagnosticsOnly => StatusVisual.Info,
        _ => StatusVisual.Neutral
    };

    private void RefreshUi()
    {
        _suppressEvents = true;
        try
        {
            var config = _acceleration.Config;
            _uiState = ResolveUiState();

            var statusLabel = StatusLabelFor(_uiState);
            var statusDetail = StatusDetailFor(_uiState);
            if (_statusText is not null)
            {
                _statusText.Text = statusLabel;
                // Long states (worker missing) may need wrap; short chips stay single-line.
                _statusText.TextWrapping = _uiState is ConnectionUiState.WorkerMissing
                    ? TextWrapping.Wrap
                    : TextWrapping.NoWrap;
            }

            ApplyStatusVisual(VisualFor(_uiState));

            var modeFull = ModeFullLabel(config.Mode);
            var modeShort = ModeShortLabel(config.Mode);
            if (_modeSummaryText is not null)
            {
                // Diagnostics: put the safety note under the short chip (chip stays "仅诊断").
                _modeSummaryText.Text = _uiState is ConnectionUiState.DiagnosticsOnly
                    ? statusDetail
                    : $"{T("NetworkAccelerationPage_ModeLabel", "Mode")} · {modeShort}";
                _modeSummaryText.ToolTip = _uiState is ConnectionUiState.DiagnosticsOnly
                    ? statusDetail
                    : modeFull;
            }

            if (_statusPill is not null)
                _statusPill.ToolTip = statusDetail;
            if (_statusText is not null)
                _statusText.ToolTip = statusDetail;

            if (_modeText is not null)
                _modeText.Text = $"{T("NetworkAccelerationPage_ModeLabel", "Mode")}: {config.Mode}";
            if (_portText is not null)
                _portText.Text = $"{T("NetworkAccelerationPage_PortLabel", "Listen port")}: {config.ListenPort}";

            if (_modeComboBox is not null)
            {
                var displayMode = config.Mode is NetworkAccelerationMode.Off
                    ? NetworkAccelerationMode.SystemProxy
                    : config.Mode;
                for (var i = 0; i < _modeComboBox.Items.Count; i++)
                {
                    if (_modeComboBox.Items[i] is ModeOption option && option.Mode == displayMode)
                    {
                        _modeComboBox.SelectedIndex = i;
                        break;
                    }
                }

                _modeComboBox.IsEnabled = !_isBusy;
            }

            RefreshDomainTiles();
            RefreshMetrics();
            RefreshPrimaryAction();
            if (_restoreButton is not null)
                _restoreButton.IsEnabled = !_isBusy;
            if (_diagnosticsButton is not null)
                _diagnosticsButton.IsEnabled = !_isBusy;
            if (_domainGroupsPanel is not null)
                _domainGroupsPanel.IsEnabled = !_isBusy;
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void RefreshDomainTiles()
    {
        var groups = _acceleration.Config.DomainGroups ?? [];
        if (_domainGroupsPanel is not null)
        {
            foreach (var tile in _domainGroupsPanel.Items.OfType<Border>())
            {
                if (tile.Tag is not string id)
                    continue;
                var group = groups.FirstOrDefault(g =>
                    string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));
                var enabled = group?.Enabled == true;
                tile.Background = (Brush)FindResource(enabled ? "ControlFillColorSecondaryBrush" : "ControlFillColorDefaultBrush");
                tile.BorderBrush = (Brush)FindResource(enabled ? "AccentFillColorDefaultBrush" : "ControlStrokeColorDefaultBrush");
                if (tile.Child is not StackPanel sp)
                    continue;

                foreach (var child in sp.Children)
                {
                    if (child is TextBlock { Name: "StateLabel" } label)
                    {
                        label.Text = enabled
                            ? T("NetworkAccelerationPage_DomainEnabled", "Enabled")
                            : T("NetworkAccelerationPage_DomainDisabled", "Off");
                        label.SetResourceReference(TextBlock.ForegroundProperty,
                            enabled ? "PaletteGreenBrush" : "TextFillColorTertiaryBrush");
                    }
                }
            }
        }

        var enabledCount = groups.Count(g => g.Enabled);
        var domainCount = groups.Where(g => g.Enabled).SelectMany(g => g.Domains ?? []).Count();
        if (_domainGroupsText is not null)
        {
            _domainGroupsText.Text = FormatDomainGroupsSummary(
                Resource.NetworkAccelerationPage_DomainGroupsSummary,
                Resource.NetworkAccelerationPage_DomainGroupsLabel,
                enabledCount,
                groups.Count,
                domainCount);
        }
    }

    internal static string FormatDomainGroupsSummary(
        string format,
        string label,
        int enabledCount,
        int totalCount,
        int domainCount)
    {
        return format.Contains("{3", StringComparison.Ordinal)
            ? string.Format(format, label, enabledCount, totalCount, domainCount)
            : string.Format(format, enabledCount, totalCount, domainCount);
    }

    private void RefreshMetrics()
    {
        // No continuous traffic sampler in the service — show real rule counts and placeholders.
        var na = T("NetworkAccelerationPage_Metric_Unavailable", "—");
        var running = _acceleration.IsRunning;

        if (_metricLatencyValue is not null)
            _metricLatencyValue.Text = na;
        if (_metricUploadValue is not null)
            _metricUploadValue.Text = na;
        if (_metricDownloadValue is not null)
            _metricDownloadValue.Text = na;
        if (_metricConnectionsValue is not null)
            _metricConnectionsValue.Text = running ? "1" : na;

        var groups = _acceleration.Config.DomainGroups ?? [];
        var enabledDomains = groups.Where(g => g.Enabled).SelectMany(g => g.Domains ?? []).Count();
        if (_metricRulesValue is not null)
            _metricRulesValue.Text = enabledDomains.ToString();
    }

    private void RefreshPrimaryAction()
    {
        if (_primaryActionButton is null)
            return;

        var state = _uiState;
        var busy = _isBusy;

        switch (state)
        {
            case ConnectionUiState.Connected:
                _primaryActionButton.Content = T("NetworkAccelerationPage_Stop", "Stop");
                _primaryActionButton.Appearance = ControlAppearance.Secondary;
                _primaryActionButton.IsEnabled = !busy;
                AutomationProperties.SetName(_primaryActionButton, T("NetworkAccelerationPage_Stop", "Stop"));
                break;
            case ConnectionUiState.Failed:
                _primaryActionButton.Content = T("NetworkAccelerationPage_Retry", "Retry");
                _primaryActionButton.Appearance = ControlAppearance.Primary;
                _primaryActionButton.IsEnabled = !busy && _acceleration.IsBackendReady;
                AutomationProperties.SetName(_primaryActionButton, T("NetworkAccelerationPage_Retry", "Retry"));
                break;
            case ConnectionUiState.Starting:
            case ConnectionUiState.Stopping:
            case ConnectionUiState.Restoring:
                _primaryActionButton.Content = StatusLabelFor(state);
                _primaryActionButton.Appearance = ControlAppearance.Primary;
                _primaryActionButton.IsEnabled = false;
                break;
            case ConnectionUiState.WorkerMissing:
                _primaryActionButton.Content = T("NetworkAccelerationPage_Start", "Start");
                _primaryActionButton.Appearance = ControlAppearance.Primary;
                _primaryActionButton.IsEnabled = false;
                break;
            case ConnectionUiState.DiagnosticsOnly:
                _primaryActionButton.Content = T("NetworkAccelerationPage_Start", "Start");
                _primaryActionButton.Appearance = ControlAppearance.Primary;
                _primaryActionButton.IsEnabled = false;
                break;
            default:
                _primaryActionButton.Content = T("NetworkAccelerationPage_Start", "Start");
                _primaryActionButton.Appearance = ControlAppearance.Primary;
                _primaryActionButton.IsEnabled = !busy && _acceleration.IsBackendReady;
                AutomationProperties.SetName(_primaryActionButton, T("NetworkAccelerationPage_Start", "Start"));
                break;
        }
    }

    private async void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        if (_uiState is ConnectionUiState.Connected)
            await StopAsync().ConfigureAwait(true);
        else
            await StartAsync().ConfigureAwait(true);
    }

    private async Task StartAsync()
    {
        _isBusy = true;
        _startFailed = false;
        _uiState = ConnectionUiState.Starting;
        RefreshUi();

        try
        {
            if (!_acceleration.Config.AccelerationEnabled)
            {
                _acceleration.Config.AccelerationEnabled = true;
                if (_acceleration.Config.Mode is NetworkAccelerationMode.Off)
                    _acceleration.Config.Mode = NetworkAccelerationMode.SystemProxy;
                await _acceleration.SaveConfigAsync().ConfigureAwait(true);
            }

            var ok = await _acceleration.StartAsync().ConfigureAwait(true);
            _startFailed = !ok;
            if (!ok)
                SetDiagnosticsMessage(Resource.NetworkAccelerationPage_StartFailed);
        }
        catch (Exception ex)
        {
            _startFailed = true;
            SetDiagnosticsMessage($"{Resource.NetworkAccelerationPage_DiagnosticsFailed}: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
            RefreshUi();
        }
    }

    private async Task StopAsync()
    {
        _isBusy = true;
        _uiState = ConnectionUiState.Stopping;
        RefreshUi();

        try
        {
            await _acceleration.StopAsync().ConfigureAwait(true);
            _startFailed = false;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("Network acceleration StopAsync failed from UI.", ex);
        }
        finally
        {
            _isBusy = false;
            RefreshUi();
        }
    }

    private async void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _isBusy ||
            _modeComboBox?.SelectedItem is not ModeOption option)
            return;

        var mode = option.Mode;
        try
        {
            _acceleration.Config.AccelerationEnabled = true;
            _acceleration.Config.Mode = mode;
            await _acceleration.SaveConfigAsync().ConfigureAwait(true);

            if (_acceleration.IsRunning)
                await _acceleration.StopAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to apply network acceleration mode '{mode}'.", ex);
        }

        RefreshUi();
    }

    private async void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        _diagnosticsCts?.Cancel();
        _diagnosticsCts?.Dispose();
        _diagnosticsCts = new CancellationTokenSource();
        var token = _diagnosticsCts.Token;

        _isBusy = true;
        if (_diagnosticsButton is not null)
        {
            _diagnosticsButton.IsEnabled = false;
            _diagnosticsButton.Content = T("NetworkAccelerationPage_RunningDiagnostics", "Running diagnostics…");
        }

        try
        {
            var report = await _diagnostics.RunQuickCheckAsync(token).ConfigureAwait(true);
            if (_diagnosticsText is not null)
                _diagnosticsText.Text = report.Summary;
            RenderDiagnosticsItems(report);
        }
        catch (OperationCanceledException)
        {
            SetDiagnosticsMessage(T("NetworkAccelerationPage_DiagnosticsCancelled", "Diagnostics cancelled."));
        }
        catch (Exception ex)
        {
            SetDiagnosticsMessage($"{Resource.NetworkAccelerationPage_DiagnosticsFailed}: {ex.Message}");
            RenderDiagnosticsFailure(ex.Message);
        }
        finally
        {
            _isBusy = false;
            if (_diagnosticsButton is not null)
            {
                _diagnosticsButton.Content = Resource.NetworkAccelerationPage_RunDiagnostics;
                _diagnosticsButton.IsEnabled = true;
            }
            RefreshUi();
        }
    }

    private void SetDiagnosticsMessage(string message)
    {
        if (_diagnosticsText is not null)
        {
            _diagnosticsText.Text = message;
            _diagnosticsText.Visibility = Visibility.Visible;
        }
    }

    private void RenderDiagnosticsFailure(string message)
    {
        if (_diagnosticsItemsPanel is null)
            return;

        _diagnosticsItemsPanel.Items.Clear();
        _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
            T("NetworkAccelerationPage_Diag_Overall", "Diagnostics"),
            message,
            StatusVisual.Danger));
    }

    private void RenderDiagnosticsItems(NetworkDiagnosticsReport report)
    {
        if (_diagnosticsItemsPanel is null)
            return;

        _diagnosticsItemsPanel.Items.Clear();

        // Map only real fields from the report / summary — never invent probe results.
        _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
            T("NetworkAccelerationPage_Diag_Backend", "Proxy worker"),
            _acceleration.IsBackendReady
                ? T("NetworkAccelerationPage_Diag_BackendOk", "Ready")
                : T("NetworkAccelerationPage_Diag_BackendMissing", "Not found"),
            _acceleration.IsBackendReady ? StatusVisual.Running : StatusVisual.Danger));

        _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
            T("NetworkAccelerationPage_Diag_Running", "Acceleration"),
            report.AccelerationEnabled
                ? (report.Mode.ToString())
                : T("NetworkAccelerationPage_State_Idle", "Not started"),
            report.AccelerationEnabled ? StatusVisual.Info : StatusVisual.Neutral));

        _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
            T("NetworkAccelerationPage_Diag_Loopback", "Loopback proxy port"),
            report.LoopbackReachable
                ? T("NetworkAccelerationPage_Diag_Ok", "OK")
                : T("NetworkAccelerationPage_Diag_Warn", "Not reachable"),
            report.LoopbackReachable ? StatusVisual.Running : StatusVisual.Caution));

        // Parse gateway / DNS lines from summary when present (UI mapping only).
        foreach (var line in (report.Summary ?? string.Empty).Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Gateway", StringComparison.OrdinalIgnoreCase))
            {
                var ok = !trimmed.Contains("none", StringComparison.OrdinalIgnoreCase)
                         && !trimmed.Contains("failed", StringComparison.OrdinalIgnoreCase);
                _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
                    T("NetworkAccelerationPage_Diag_Gateway", "Gateway"),
                    trimmed,
                    ok ? StatusVisual.Running : StatusVisual.Caution));
            }
            else if (trimmed.StartsWith("Configured DNS", StringComparison.OrdinalIgnoreCase))
            {
                _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
                    T("NetworkAccelerationPage_Diag_Dns", "DNS"),
                    trimmed,
                    StatusVisual.Info));
            }
            else if (trimmed.StartsWith("Configured DoH", StringComparison.OrdinalIgnoreCase))
            {
                _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
                    T("NetworkAccelerationPage_Diag_Doh", "DoH"),
                    trimmed,
                    StatusVisual.Info));
            }
        }

        if (_diagnosticsText is not null)
            _diagnosticsText.Visibility = Visibility.Collapsed;
    }

    private Border CreateDiagRow(string name, string result, StatusVisual severity)
    {
        var dot = new Ellipse
        {
            Width = 7,
            Height = 7,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0)
        };
        TrySetResourceBrush(dot, Shape.FillProperty, severity switch
        {
            StatusVisual.Running => "PaletteGreenBrush",
            StatusVisual.Caution => "PaletteOrangeBrush",
            StatusVisual.Danger => "PaletteRedBrush",
            StatusVisual.Info => "PaletteLightBlueBrush",
            _ => "TextFillColorSecondaryBrush"
        }, "TextFillColorSecondaryBrush");

        var title = new TextBlock
        {
            Text = name,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = (Brush)FindResource("TextFillColorPrimaryBrush")
        };
        var detail = new TextBlock
        {
            Text = result,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 240,
            Foreground = (Brush)FindResource("TextFillColorSecondaryBrush")
        };

        var grid = new Grid { MinHeight = 22 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var dotHost = new Border
        {
            Child = dot,
            Width = 16,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(dotHost, 0);
        Grid.SetColumn(title, 1);
        Grid.SetColumn(detail, 2);
        title.Margin = new Thickness(0, 0, 8, 0);
        grid.Children.Add(dotHost);
        grid.Children.Add(title);
        grid.Children.Add(detail);

        return new Border
        {
            Child = grid,
            Margin = new Thickness(0, 0, 0, 4),
            Padding = new Thickness(10, 7, 10, 7),
            MinHeight = 34,
            CornerRadius = TryCornerRadius("CornerRadiusControl", 8),
            Background = (Brush)FindResource("ControlFillColorSecondaryBrush"),
            BorderBrush = (Brush)FindResource("ControlStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true
        };
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        _isBusy = true;
        _uiState = ConnectionUiState.Restoring;
        RefreshUi();

        try
        {
            await _acceleration.StopAsync().ConfigureAwait(true);
            var ok = _recovery.TryRestoreFromSnapshot(out var report);
            var message = ok
                ? report
                : $"{Resource.NetworkAccelerationPage_RestorePartial}\n{report}";
            SetDiagnosticsMessage(message);
            if (_diagnosticsItemsPanel is not null)
            {
                _diagnosticsItemsPanel.Items.Clear();
                _diagnosticsItemsPanel.Items.Add(CreateDiagRow(
                    T("NetworkAccelerationPage_RestoreNetwork", "Force restore network state"),
                    message,
                    ok ? StatusVisual.Running : StatusVisual.Caution));
            }
        }
        catch (Exception ex)
        {
            SetDiagnosticsMessage($"{Resource.NetworkAccelerationPage_DiagnosticsFailed}: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
            _startFailed = false;
            RefreshUi();
        }
    }
}
