using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Humanizer;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Dashboard;
using Wpf.Ui.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard
{
public partial class SensorsControl : IDisposable
{
    private const string CelsiusUnit = "\u00B0C";
    private const string FahrenheitUnit = "\u00B0F";
    private const string GigahertzUnit = "GHz";
    private const string MegahertzUnit = "MHz";
    private const string RpmUnit = "RPM";
    private static readonly TimeSpan DoubleClickThreshold = TimeSpan.FromMilliseconds(500);
    private const int TrendHistoryCapacity = 60;
    private static readonly object SessionSensorDataLock = new();
    private static SensorsData? _sessionSensorData;

    // Session-persistent trend history: survives page navigation so charts
    // do NOT restart from scratch when the user navigates away and back.
    // Keyed "scope:seriesKey", capped at DefaultCapacity (60) per series.
    private static readonly object TrendHistoryLock = new();
    private static readonly Dictionary<string, List<double>> _sessionTrendHistory = new(StringComparer.Ordinal);

    private const string CpuScope = "cpu";
    private const string GpuScope = "gpu";
    private const string BatteryScope = "battery";

    private readonly ISensorsController _controller = IoCContainer.Resolve<ISensorsController>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly DashboardSettings _dashboardSettings = IoCContainer.Resolve<DashboardSettings>();
    private readonly HardwareSensorSettings _hardwareSensorSettings = IoCContainer.Resolve<HardwareSensorSettings>();
    private readonly SensorsGroupController? _sensorsGroupController = IoCContainer.TryResolve<SensorsGroupController>();
    private readonly IDelayProvider _delayProvider = IoCContainer.Resolve<IDelayProvider>();
    private bool _sensorRuntimeAvailable = true;
    private bool _forceShowSensorDetails;
    private volatile bool _forceDetailedRefresh;
    private bool _detailsExpanded;
    private Window? _detailsWindow;
    private DateTime _lastDetailsToggleClick = DateTime.MinValue;

    private CancellationTokenSource? _cts;
    private Task? _refreshTask;
    private bool _sensorsRefreshFailureLogged;
    // Guards start/stop of sensor refresh so Dispose / IsVisible / Refresh cannot race on _cts.
    private readonly object _sensorLifecycleLock = new();

    private CancellationTokenSource? _batteryCts;
    private Task? _batteryRefreshTask;
    // Guards start/stop of battery refresh so Dispose / IsVisible / Refresh cannot race on _batteryCts.
    private readonly object _batteryLifecycleLock = new();
    private readonly object _initialSensorDataLoadLock = new();
    private TaskCompletionSource _firstSensorDataTaskCompletionSource = CreateInitialSensorDataTaskCompletionSource();
    private bool _hasRenderedSensorData;
    private SensorsData? _lastRenderedSensorData;
    private int _extendedDetailsRefreshVersion;
    private Task? _extendedDetailsRefreshTask;

    private string _cpuName = string.Empty;
    private string _gpuName = string.Empty;
    private SensorSummaryLayoutMode _sensorSummaryLayoutMode = SensorSummaryLayoutMode.Standard;

    private TextBlock? _cpuWattageText;
    private TextBlock? _cpuTempRangeText;
    private TextBlock? _cpuVoltageText;
    private TextBlock? _cpuVoltageRangeText;
    private TextBlock? _gpuWattageText;
    private TextBlock? _gpuTempRangeText;
    private TextBlock? _gpuVoltageText;
    private TextBlock? _gpuVoltageRangeText;
    private bool _textBlockReferencesCached;
    private readonly Dictionary<string, FrameworkElement?> _findNameCache = new(StringComparer.Ordinal);

    public SensorsControl()
    {
        InitializeComponent();
        CacheTextBlockReferences();
        ApplySensorSectionConfiguration();
        InitializeContextMenu();
        InitializeTrendCharts();
        SetInitialSensorPlaceholders();
        CollapseDetailPanels();
        InitializeFromSessionCache();
        _ = FetchHardwareNamesAsync();

        IsVisibleChanged += SensorsControl_IsVisibleChanged;
        SizeChanged += SensorsControl_SizeChanged;
        Loaded += SensorsControl_Loaded;
        Unloaded += SensorsControl_Unloaded;
    }

    private void SensorsControl_Loaded(object sender, RoutedEventArgs e)
    {
        // First measure can be 0/narrow during page construction; re-apply once the
        // control is in the visual tree so trend charts are not stuck in Compact hide.
        var width = ActualWidth > 1 ? ActualWidth : 1200;
        ApplySensorSummaryLayout(width, force: true);
    }

    private void ApplySensorSectionConfiguration()
    {
        var store = _hardwareSensorSettings.Store;
        var visible = new HashSet<string>(store.VisibleSections ?? [], StringComparer.OrdinalIgnoreCase);
        var sectionMap = new Dictionary<string, FrameworkElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["CPU"] = _cpuSection,
            ["Battery"] = _batterySectionColumn,
            ["GPU"] = _gpuSection
        };

        foreach (var (name, element) in sectionMap)
            element.Visibility = visible.Contains(name) ? Visibility.Visible : Visibility.Collapsed;

        var order = (store.SectionOrder is { Length: > 0 } ? store.SectionOrder : ["CPU", "Battery", "GPU"])
            .Where(name => sectionMap.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var orderedVisible = new List<UIElement>();
        foreach (var name in order)
        {
            if (sectionMap.TryGetValue(name, out var element) && element.Visibility == Visibility.Visible)
                orderedVisible.Add(element);
        }

        foreach (var (name, element) in sectionMap)
        {
            if (element.Visibility == Visibility.Visible && !orderedVisible.Contains(element))
                orderedVisible.Add(element);
        }

        _sensorsGrid.Children.Clear();
        foreach (var child in orderedVisible)
            _sensorsGrid.Children.Add(child);

        _sensorsGrid.Columns = Math.Max(1, orderedVisible.Count);
    }

    private void SensorsControl_Unloaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged -= SensorsControl_IsVisibleChanged;
        SizeChanged -= SensorsControl_SizeChanged;
        Loaded -= SensorsControl_Loaded;
        Unloaded -= SensorsControl_Unloaded;
        Dispose();
    }

    public void Dispose()
    {
        StopSensorRefresh();
        StopBatteryRefresh();
    }

    internal enum SensorSummaryLayoutMode
    {
        Compact,
        Standard,
        Wide
    }

    private const string TrendUtilizationKey = "util";
    private const string TrendCoreClockKey = "clock";
    private const string TrendTemperatureKey = "temp";
    private const string TrendBatteryChargeKey = "battery-charge";
    private const string TrendBatteryHealthKey = "battery-health";
    private const string TrendBatteryTemperatureKey = "battery-temp";

    private void InitializeTrendCharts()
    {
        foreach (var chart in new[] { _cpuTrendChart, _gpuTrendChart })
        {
            if (chart is null)
                continue;

            chart.DefineSeries(TrendUtilizationKey, GetChartColor("ChartUtilizationColor", System.Windows.Media.Colors.DodgerBlue), 100);
            chart.DefineSeries(TrendCoreClockKey, GetChartColor("ChartCoreClockColor", System.Windows.Media.Colors.MediumSeaGreen));
            chart.DefineSeries(TrendTemperatureKey, GetChartColor("ChartTemperatureColor", System.Windows.Media.Colors.Goldenrod), 110);
        }

        if (_batteryTrendChart is not null)
        {
            _batteryTrendChart.DefineSeries(TrendBatteryChargeKey, GetChartColor("ChartBatteryColor", System.Windows.Media.Colors.MediumSeaGreen), 100);
            _batteryTrendChart.DefineSeries(TrendBatteryHealthKey, GetChartColor("ChartCoreClockColor", System.Windows.Media.Colors.SeaGreen), 100);
            _batteryTrendChart.DefineSeries(TrendBatteryTemperatureKey, GetChartColor("ChartTemperatureColor", System.Windows.Media.Colors.Goldenrod), 60);
        }
    }

    private System.Windows.Media.Color GetChartColor(string resourceKey, System.Windows.Media.Color fallback) =>
        TryFindResource(resourceKey) is System.Windows.Media.Color color ? color : fallback;

    private void SensorsControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;

        // Ignore transient zero/near-zero measures that would falsely enter Compact.
        if (e.NewSize.Width <= 1)
            return;

        ApplySensorSummaryLayout(e.NewSize.Width);
    }

    internal static int GetSensorColumnCountForWidth(double width)
    {
        _ = width;
        return 3;
    }

    internal static SensorSummaryLayoutMode GetSensorSummaryLayoutMode(double width)
    {
        // Unmeasured / invalid widths must not collapse to Compact (hides charts).
        if (width <= 1)
            return SensorSummaryLayoutMode.Standard;

        if (width >= 1500)
            return SensorSummaryLayoutMode.Wide;

        if (width >= 900)
            return SensorSummaryLayoutMode.Standard;

        return SensorSummaryLayoutMode.Compact;
    }

    internal static bool CanShowSensorDetailsForWidth(double width) =>
        GetSensorSummaryLayoutMode(width) == SensorSummaryLayoutMode.Wide;

    private void ApplySensorSummaryLayout(double width, bool force = false)
    {
        if (_sensorsGrid is not null && _sensorsGrid.Columns != 3)
            _sensorsGrid.Columns = 3;

        if (_skeletonGrid is not null && _skeletonGrid.Columns != 3)
            _skeletonGrid.Columns = 3;

        var mode = GetSensorSummaryLayoutMode(width);
        var isCompact = mode == SensorSummaryLayoutMode.Compact;
        var isWide = mode == SensorSummaryLayoutMode.Wide;

        ApplySkeletonSummaryLayout(isCompact, isWide);

        if (!force && mode == _sensorSummaryLayoutMode)
            return;

        _sensorSummaryLayoutMode = mode;

        // Model names can stay compact-only to save space.
        SetVisibility("_cpuModelName", !isCompact);
        SetVisibility("_batteryModelName", !isCompact);
        SetVisibility("_gpuModelName", !isCompact);

        // Trend charts + legends always stay visible. Hiding them in Compact caused
        // "missing on first open, appear after page switches" when the first measure
        // was narrow and later measures recovered to Standard/Wide.
        SetVisibility("_cpuLegend", true);
        SetVisibility("_batteryLegend", true);
        SetVisibility("_gpuLegend", true);
        SetVisibility("_cpuTrendPanel", true);
        SetVisibility("_batteryTrendPanel", true);
        SetVisibility("_gpuTrendPanel", true);

        ApplySummaryGaugeSize(_cpuGauge, isCompact);
        ApplySummaryGaugeSize(_batteryGauge, isCompact);
        ApplySummaryGaugeSize(_gpuGauge, isCompact);

        ApplyTrendPanelHeight(_cpuTrendPanel, isWide);
        ApplyTrendPanelHeight(_batteryTrendPanel, isWide);
        ApplyTrendPanelHeight(_gpuTrendPanel, isWide);

        ApplyProgressBarMaxWidth(_cpuCoreClockBar, isWide);
        ApplyProgressBarMaxWidth(_cpuTemperatureBar, isWide);
        ApplyProgressBarMaxWidth(_cpuFanSpeedBar, isWide);
        ApplyProgressBarMaxWidth(_batteryHealthBar, isWide);
        ApplyProgressBarMaxWidth(_batteryTemperatureBar, isWide);
        ApplyProgressBarMaxWidth(_batteryRateBar, isWide);
        ApplyProgressBarMaxWidth(_gpuCoreClockBar, isWide);
        ApplyProgressBarMaxWidth(_gpuTemperatureBar, isWide);
        ApplyProgressBarMaxWidth(_gpuFanSpeedBar, isWide);

        if (!CanShowSensorDetails)
            CollapseDetailPanels();
    }

    private void ApplySkeletonSummaryLayout(bool isCompact, bool isWide)
    {
        // Match live subtitle visibility (hidden only in compact).
        SetVisibility("_skeletonCpuSubtitle", !isCompact);
        SetVisibility("_skeletonBatterySubtitle", !isCompact);
        SetVisibility("_skeletonGpuSubtitle", !isCompact);

        // Trends + legends always visible (same as live summary).
        SetVisibility("_skeletonCpuLegend", true);
        SetVisibility("_skeletonBatteryLegend", true);
        SetVisibility("_skeletonGpuLegend", true);
        SetVisibility("_skeletonCpuTrendPanel", true);
        SetVisibility("_skeletonBatteryTrendPanel", true);
        SetVisibility("_skeletonGpuTrendPanel", true);

        // Gauge sizes: GaugeSizeSM (88) compact / GaugeSizeMD (110) standard+wide.
        ApplySummaryGaugeSize(_skeletonCpuGauge, isCompact);
        ApplySummaryGaugeSize(_skeletonBatteryGauge, isCompact);
        ApplySummaryGaugeSize(_skeletonGpuGauge, isCompact);

        // Trend panel heights: 76 standard / 96 wide.
        ApplyTrendPanelHeight(_skeletonCpuTrendPanel, isWide);
        ApplyTrendPanelHeight(_skeletonBatteryTrendPanel, isWide);
        ApplyTrendPanelHeight(_skeletonGpuTrendPanel, isWide);

        // Metric bars MaxWidth: 260 standard / 320 wide (same as live ProgressBars).
        ApplyProgressBarMaxWidth(_skeletonCpuBar0, isWide);
        ApplyProgressBarMaxWidth(_skeletonCpuBar1, isWide);
        ApplyProgressBarMaxWidth(_skeletonCpuBar2, isWide);
        ApplyProgressBarMaxWidth(_skeletonBatteryBar0, isWide);
        ApplyProgressBarMaxWidth(_skeletonBatteryBar1, isWide);
        ApplyProgressBarMaxWidth(_skeletonBatteryBar2, isWide);
        ApplyProgressBarMaxWidth(_skeletonGpuBar0, isWide);
        ApplyProgressBarMaxWidth(_skeletonGpuBar1, isWide);
        ApplyProgressBarMaxWidth(_skeletonGpuBar2, isWide);
    }

    private void SetLiveSensorContentVisible(bool visible)
    {
        if (_sensorsCard is not null)
            _sensorsCard.Opacity = visible ? 1 : 0;
    }

    private static void ApplySummaryGaugeSize(FrameworkElement? gauge, bool isCompact)
    {
        if (gauge is null)
            return;

        // Keep in sync with DesignTokens: GaugeSizeSM=88, GaugeSizeMD=110.
        var size = isCompact ? 88 : 110;
        gauge.Width = size;
        gauge.Height = size;
        gauge.MinWidth = size;
        gauge.MinHeight = size;
    }

    private static void ApplyTrendPanelHeight(FrameworkElement? trendPanel, bool isWide)
    {
        if (trendPanel is null)
            return;

        trendPanel.Height = isWide ? 96 : 76;
    }

    private static void ApplyProgressBarMaxWidth(FrameworkElement? progressBar, bool isWide)
    {
        if (progressBar is null)
            return;

        progressBar.MaxWidth = isWide ? 320 : 260;
    }

    private bool CanShowSensorDetails => _forceShowSensorDetails || _sensorSummaryLayoutMode == SensorSummaryLayoutMode.Wide;

    public Task FirstSensorDataReadyTask
    {
        get
        {
            lock (_initialSensorDataLoadLock)
                return _firstSensorDataTaskCompletionSource.Task;
        }
    }

    private void HideSkeletonOverlay()
    {
        if (_skeletonOverlay is null)
            return;

        if (_skeletonOverlay.Visibility != Visibility.Visible)
        {
            SetLiveSensorContentVisible(true);
            return;
        }

        SetLiveSensorContentVisible(true);

        try
        {
            if (TryFindResource("SkeletonFadeOutStoryboard") is Storyboard storyboard)
            {
                storyboard.Begin(this);
            }
            else
            {
                _skeletonOverlay.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"HideSkeletonOverlay failed: {ex.Message}", ex);
            _skeletonOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowSkeletonOverlay()
    {
        if (_skeletonOverlay is null)
            return;

        // Prefer real measure so first skeleton frame matches current window width
        // (compact/standard/wide), not a stale default mode from construction.
        var width = ActualWidth > 1 ? ActualWidth : (_sensorsCard?.ActualWidth > 1 ? _sensorsCard.ActualWidth : 1200);
        ApplySensorSummaryLayout(width, force: true);

        SetLiveSensorContentVisible(false);
        _skeletonOverlay.Opacity = 1;
        _skeletonOverlay.Visibility = Visibility.Visible;
    }

    public Task RestartInitialSensorDataLoad()
    {
        lock (_initialSensorDataLoadLock)
        {
            if (_lastRenderedSensorData is { } data && CanCompleteInitialLoadFromCachedSensorData(data))
            {
                _hasRenderedSensorData = true;
                _firstSensorDataTaskCompletionSource.TrySetResult();
                _ = Dispatcher.InvokeAsync(HideSkeletonOverlay);
                return _firstSensorDataTaskCompletionSource.Task;
            }

            _hasRenderedSensorData = false;
            if (_firstSensorDataTaskCompletionSource.Task.IsCompleted)
                _firstSensorDataTaskCompletionSource = CreateInitialSensorDataTaskCompletionSource();

            _ = Dispatcher.InvokeAsync(ShowSkeletonOverlay);
            return _firstSensorDataTaskCompletionSource.Task;
        }
    }

    public void RestartTrendCharts()
    {
        ClearTrendCharts();
        InitializeTrendChartsFromSessionCache();
    }

    public void UseDetailsWindowLayout()
    {
        _forceShowSensorDetails = true;
        ApplySensorSummaryLayout(ActualWidth > 0 ? ActualWidth : 1200);
        SetVisibility("_cpuModelName", true);
        SetVisibility("_batteryModelName", true);
        SetVisibility("_gpuModelName", true);
        SetVisibility("_cpuLegend", true);
        SetVisibility("_batteryLegend", true);
        SetVisibility("_gpuLegend", true);
        SetVisibility("_cpuTrendPanel", true);
        SetVisibility("_batteryTrendPanel", true);
        SetVisibility("_gpuTrendPanel", true);
        ShowDetailPanels();
    }

    internal static bool HasInitialSummarySensorData(SensorsData data) =>
        HasInitialSummarySensorData(data.CPU) && HasInitialSummarySensorData(data.GPU);

    internal static bool HasAnySummarySensorData(SensorsData data) =>
        HasAnySummarySensorData(data.CPU) || HasAnySummarySensorData(data.GPU);

    internal static bool CanCompleteInitialLoadFromCachedSensorData(SensorsData data) =>
        HasInitialSummarySensorData(data);

    private async Task FetchHardwareNamesAsync()
    {
        try
        {
            if (_sensorsGroupController is not null
                && await _sensorsGroupController.IsSupportedAsync() is LibreHardwareMonitorInitialState.Initialized or LibreHardwareMonitorInitialState.Success)
            {
                _cpuName = await _sensorsGroupController.GetCpuNameAsync();
                _gpuName = await _sensorsGroupController.GetGpuNameAsync();
            }

            _cpuName = NormalizeHardwareNameOrFallback(_cpuName, T("SensorsControl_UnknownCpu", "Unknown CPU"));
            _gpuName = NormalizeHardwareNameOrFallback(_gpuName, T("SensorsControl_UnknownGpu", "Unknown GPU"));
        }
        catch
        {
            _cpuName = T("SensorsControl_UnknownCpu", "Unknown CPU");
            _gpuName = T("SensorsControl_UnknownGpu", "Unknown GPU");
        }

        await Dispatcher.InvokeAsync(() =>
        {
            UpdateModelNameText("_cpuModelName", _cpuName);
            UpdateModelNameText("_gpuModelName", _gpuName);
        });
    }

    private void InitializeContextMenu()
    {
        ContextMenu = new ContextMenu();
        ContextMenu.Items.Add(new MenuItem { Header = Resource.SensorsControl_RefreshInterval, IsEnabled = false });

        foreach (var interval in new[] { 1, 2, 3, 5 })
        {
            var item = new MenuItem
            {
                Header = TimeSpan.FromSeconds(interval).Humanize(culture: Resource.Culture)
            };
            if (_dashboardSettings.Store.SensorsRefreshIntervalSeconds == interval)
                item.Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 };

            item.Click += (_, _) =>
            {
                _dashboardSettings.Store.SensorsRefreshIntervalSeconds = interval;
                _dashboardSettings.SynchronizeStore();
                InitializeContextMenu();
            };
            ContextMenu.Items.Add(item);
        }
    }

    private async void SensorsControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        try
        {
            if (IsVisible)
            {
                var width = ActualWidth > 1 ? ActualWidth : 1200;
                ApplySensorSummaryLayout(width, force: true);
                Refresh();
                RefreshBattery();
                return;
            }

            // Always operate on locals after clearing fields — never touch _cts/_batteryCts after await.
            await StopSensorRefreshAsync().ConfigureAwait(true);
            await StopBatteryRefreshAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(SensorsControl_IsVisibleChanged)}.", ex);
        }
    }

    private static void SafeCancelAndDispose(CancellationTokenSource? cts)
    {
        if (cts is null)
            return;

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task SafeCancelAndDisposeAsync(CancellationTokenSource? cts)
    {
        if (cts is null)
            return;

        try
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task SafeAwaitRefreshTaskAsync(Task? task)
    {
        if (task is null)
            return;

        try
        {
            await task.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            // Visibility/dispose teardown must not surface refresh loop failures.
            Log.Instance.TraceOnce(
                "sensors-safe-await-refresh",
                "Sensor refresh task faulted during safe await (teardown-safe).",
                ex);
        }
    }

    private void StopSensorRefresh()
    {
        CancellationTokenSource? cts;
        lock (_sensorLifecycleLock)
        {
            cts = _cts;
            _cts = null;
            _refreshTask = null;
        }

        SafeCancelAndDispose(cts);
    }

    private async Task StopSensorRefreshAsync()
    {
        CancellationTokenSource? cts;
        Task? task;
        lock (_sensorLifecycleLock)
        {
            cts = _cts;
            _cts = null;
            task = _refreshTask;
            _refreshTask = null;
        }

        await SafeCancelAndDisposeAsync(cts).ConfigureAwait(true);
        await SafeAwaitRefreshTaskAsync(task).ConfigureAwait(true);
    }

    private void StopBatteryRefresh()
    {
        CancellationTokenSource? cts;
        lock (_batteryLifecycleLock)
        {
            cts = _batteryCts;
            _batteryCts = null;
            _batteryRefreshTask = null;
        }

        SafeCancelAndDispose(cts);
    }

    private async Task StopBatteryRefreshAsync()
    {
        CancellationTokenSource? cts;
        Task? task;
        lock (_batteryLifecycleLock)
        {
            cts = _batteryCts;
            _batteryCts = null;
            task = _batteryRefreshTask;
            _batteryRefreshTask = null;
        }

        await SafeCancelAndDisposeAsync(cts).ConfigureAwait(true);
        await SafeAwaitRefreshTaskAsync(task).ConfigureAwait(true);
    }

    private void RefreshBattery()
    {
        // Capture CTS in a local so concurrent Dispose / hide cannot NRE on field access after await.
        lock (_batteryLifecycleLock)
        {
            var previous = _batteryCts;
            _batteryCts = null;
            SafeCancelAndDispose(previous);

            var cts = new CancellationTokenSource();
            _batteryCts = cts;
            var token = cts.Token;

            _batteryRefreshTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var batteryInfo = Battery.GetBatteryInformation();
                        var powerAdapterStatus = await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false);
                        var onBatterySince = Battery.GetOnBatterySince();
                        await Dispatcher.InvokeAsync(() =>
                            SetBattery(batteryInfo, powerAdapterStatus, onBatterySince, recordTrendHistory: true));

                        await _delayProvider.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when battery refresh is cancelled.
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Battery information refresh failed.", ex);
                    }
                }
            }, token);
        }
    }

    private void SetBattery(BatteryInformation batteryInfo, PowerAdapterStatus powerAdapterStatus, DateTime? onBatterySince,
        bool recordTrendHistory = false)
    {
        if (_batteryGauge is not null)
        {
            _batteryGauge.Maximum = 100;
            _batteryGauge.Value = batteryInfo.BatteryPercentage;
            _batteryGauge.ValueText = $"{batteryInfo.BatteryPercentage:N0}%";
            _batteryGauge.RingBrush = (batteryInfo.IsLowBattery
                ? TryFindResource("ChartCautionBrush")
                : TryFindResource("ChartBatteryBrush")) as System.Windows.Media.Brush
                ?? _batteryGauge.RingBrush;
        }

        if (FindNameCached("_batteryStatusLabel") is TextBlock statusLabel)
        {
            statusLabel.Text = GetBatteryStatusText(batteryInfo);
            statusLabel.Visibility = (batteryInfo.IsLowBattery || powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // Warnings
        SetVisibility("_lowBatteryWarning", batteryInfo.IsLowBattery);
        SetVisibility("_lowWattageWarning", powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage);

        // Icon
        if (FindNameCached("_batteryIcon") is Wpf.Ui.Controls.SymbolIcon icon)
        {
            icon.Symbol = batteryInfo.IsCharging
                ? SymbolRegular.BatteryCharge24
                : GetBatteryIconSymbol(batteryInfo.BatteryPercentage);
        }

        // Details
        UpdateBatteryDetails(batteryInfo, onBatterySince);
        PushBatteryTrendSamples(batteryInfo, recordTrendHistory);
    }

    private void UpdateBatteryDetails(BatteryInformation info, DateTime? onBatterySince)
    {
        // Implement logic to update details UI (ProgressBar/Text)
        // This relies on the UI elements being present in the XAML
        // I will implement this assuming the UI structure I will create
        UpdateDetailText("_batteryHealthText", $"{info.BatteryHealth:0.00}%");
        if (FindNameCached("_batteryHealthBar") is System.Windows.Controls.Primitives.RangeBase healthBar)
            healthBar.Value = info.BatteryHealth;

        UpdateBatteryHealthGauge(info.BatteryHealth);

        if (FindNameCached("_batteryTemperatureBar") is System.Windows.Controls.Primitives.RangeBase tempBar &&
            FindNameCached("_batteryTempText") is ContentControl tempLabel)
        {
            var temperature = info.BatteryTemperatureC ?? -1;
            UpdateValue(tempBar, tempLabel, 60, temperature, GetTemperatureText(info.BatteryTemperatureC));
        }

        if (FindNameCached("_batteryRateBar") is System.Windows.Controls.Primitives.RangeBase rateBar &&
            FindNameCached("_batteryRateText") is ContentControl rateLabel)
        {
            var rateW = Math.Abs(info.DischargeRate / 1000.0);
            // Assuming 100W is max reasonable charge/discharge rate for bar scaling
            UpdateValue(rateBar, rateLabel, 100, rateW, $"{info.DischargeRate / 1000.0:+0.00;-0.00;0.00} W");
        }

        UpdateModelNameText("_batteryModelName", info.ModelName ?? T("SensorsControl_UnknownBattery", "Unknown battery"));

        if (!CanShowSensorDetails)
            return;

        // Advanced Details
        UpdateDetailText("_batteryRateRange", $"{info.MinDischargeRate / 1000.0:+0.0;-0.0;0.0} W ~ {info.MaxDischargeRate / 1000.0:+0.0;-0.0;0.0} W");
        
        if (info.DesignCapacity > 0)
        {
             UpdateDetailText("_batteryCap", $"{info.EstimateChargeRemaining / 1000.0:0.00} Wh");
             UpdateDetailText("_batteryFullCap", $"{info.FullChargeCapacity / 1000.0:0.00} Wh");
             // Keep design capacity as supporting text under the health ring (tooltip still labels it).
             UpdateDetailText("_batteryDesignCap",
                 $"{T("SensorsControl_DesignCapacity", "Design capacity")}: {info.DesignCapacity / 1000.0:0.00} Wh");

             if (_batteryCapGauge is not null)
                _batteryCapGauge.Value = (info.EstimateChargeRemaining / (double)info.DesignCapacity) * 100.0;
             if (_batteryFullCapGauge is not null)
                _batteryFullCapGauge.Value = (info.FullChargeCapacity / (double)info.DesignCapacity) * 100.0;
        }
        else
        {
            UpdateDetailText("_batteryDesignCap", string.Empty);
        }

        UpdateDetailText("_batteryCycles", $"{info.CycleCount:N0}");
        UpdateDetailText("_batteryDate", info.ManufactureDate?.ToString(LocalizationHelper.ShortDateFormat) ?? string.Empty);
        UpdateDetailText("_batteryTemperature", FormatNullableTemperature(info.BatteryTemperatureC, _applicationSettings.Store.TemperatureUnit));

    }

    /// <summary>
    /// Third detail ring shows battery health (not a static "design capacity" track).
    /// Color: green ≥80, caution 60–80, critical/red &lt;60.
    /// </summary>
    private void UpdateBatteryHealthGauge(double healthPercent)
    {
        if (_batteryHealthGauge is null)
            return;

        var value = healthPercent < 0 ? 0 : Math.Clamp(healthPercent, 0, 100);
        _batteryHealthGauge.Maximum = 100;
        _batteryHealthGauge.Value = value;
        _batteryHealthGauge.ValueText = healthPercent < 0 ? "—" : $"{value:0}%";

        var brushKey = value >= 80
            ? "ChartBatteryBrush"
            : value >= 60
                ? "ChartCautionBrush"
                : "StatusCriticalBrush";

        if (TryFindResource(brushKey) is System.Windows.Media.Brush brush)
            _batteryHealthGauge.RingBrush = brush;
        else if (TryFindResource("StatusCriticalBrush") is System.Windows.Media.Brush fallback)
            _batteryHealthGauge.RingBrush = fallback;
    }

    private void UpdateDetailText(string name, string text)
    {
        var displayText = text == "-" || !IsUsefulDetailValue(text) ? string.Empty : text;

        if (FindNameCached(name) is TextBlock tb) 
        {
            tb.Text = displayText;
        }
        else if (FindNameCached(name) is Label lbl) lbl.Content = displayText;

        UpdateDetailContainerVisibility(name, displayText);
    }

    private void UpdateDetailContainerVisibility(string valueElementName, string displayText)
    {
        var detailElementName = valueElementName switch
        {
            "_cpuWattage" => "_cpuWattageDetail",
            "_cpuVoltage" => "_cpuVoltageDetail",
            "_cpuPCoreClock" => "_cpuPCoreClockDetail",
            "_cpuECoreClock" => "_cpuECoreClockDetail",
            "_cpuMemoryUsage" => "_cpuMemoryUsageDetail",
            "_cpuTempRange" => "_cpuTempRangeDetail",
            "_cpuVoltageRange" => "_cpuVoltageRangeDetail",
            "_cpuMemoryTemperature" => "_cpuMemoryTemperatureDetail",
            "_cpuSsdTemperature" => "_cpuSsdTemperatureDetail",
            "_batteryRateRange" => "_batteryRateRangeDetail",
            "_batteryCycles" => "_batteryCyclesDetail",
            "_batteryDate" => "_batteryDateDetail",
            "_batteryTemperature" => "_batteryTemperatureDetail",
            "_gpuVramUsage" => "_gpuVramUsageDetail",
            "_gpuWattage" => "_gpuWattageDetail",
            "_gpuVoltage" => "_gpuVoltageDetail",
            "_gpuPcieThroughput" => "_gpuPcieThroughputDetail",
            "_gpuVramTemperature" => "_gpuVramTemperatureDetail",
            "_gpuHotSpotTemperature" => "_gpuHotSpotTemperatureDetail",
            "_gpuTempRange" => "_gpuTempRangeDetail",
            "_gpuVoltageRange" => "_gpuVoltageRangeDetail",
            _ => null
        };

        if (detailElementName is not null && FindNameCached(detailElementName) is FrameworkElement detailElement)
            detailElement.Visibility = string.IsNullOrWhiteSpace(displayText) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SetVisibility(string name, bool visible)
    {
        if (FindNameCached(name) is UIElement el) el.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private FrameworkElement? FindNameCached(string name)
    {
        if (_findNameCache.TryGetValue(name, out var cached))
            return cached;

        var element = FindName(name) as FrameworkElement;
        _findNameCache[name] = element;
        return element;
    }

    private static string GetBatteryStatusText(BatteryInformation batteryInfo)
    {
        if (batteryInfo.IsCharging)
        {
            return batteryInfo.DischargeRate > 0 
                ? Resource.BatteryPage_ACAdapterConnectedAndCharging 
                : Resource.BatteryPage_ACAdapterConnectedNotCharging;
        }

        if (batteryInfo.BatteryLifeRemaining < 0)
            return Resource.BatteryPage_EstimatingBatteryLife;

        var time = TimeSpan.FromSeconds(batteryInfo.BatteryLifeRemaining).Humanize(2, Resource.Culture);
        return string.Format(Resource.BatteryPage_EstimatedBatteryLifeRemaining, time);
    }

    private static SymbolRegular GetBatteryIconSymbol(double percentage)
    {
        var number = (int)Math.Round(percentage / 10.0);
        return number switch
        {
            10 => SymbolRegular.Battery1024,
            9 => SymbolRegular.Battery924,
            8 => SymbolRegular.Battery824,
            7 => SymbolRegular.Battery724,
            6 => SymbolRegular.Battery624,
            5 => SymbolRegular.Battery524,
            4 => SymbolRegular.Battery424,
            3 => SymbolRegular.Battery324,
            2 => SymbolRegular.Battery224,
            1 => SymbolRegular.Battery124,
            _ => SymbolRegular.Battery024,
        };
    }

    private void Refresh()
    {
        lock (_sensorLifecycleLock)
        {
            var previous = _cts;
            _cts = null;
            SafeCancelAndDispose(previous);

            var cts = new CancellationTokenSource();
            _cts = cts;
            var token = cts.Token;

            _refreshTask = Task.Run(async () =>
            {
                if (!await _controller.IsSupportedAsync().ConfigureAwait(false))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _sensorRuntimeAvailable = false;
                        SetSensorSectionsVisible(true);
                        ResetSensorValues();
                        CompleteInitialSensorDataLoad();
                    });
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    _sensorRuntimeAvailable = true;
                    SetSensorSectionsVisible(true);
                });

                await _controller.PrepareAsync().ConfigureAwait(false);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Always request the detailed snapshot while detail panels are open so
                        // wattage/voltage/memory-clock stay on one source (NvAPI/WMI path) and
                        // do not alternate with the LibreHardwareMonitor extended overlay.
                        var detailed = await Dispatcher.InvokeAsync(() =>
                            CanShowSensorDetails
                            && (_detailsExpanded
                                || _forceDetailedRefresh
                                || IsElementVisible("_cpuDetailsPanel")
                                || IsElementVisible("_gpuDetailsPanel"))).Task.ConfigureAwait(false);

                        var data = await _controller.GetDataAsync(detailed).ConfigureAwait(false);
                        if (detailed)
                            _forceDetailedRefresh = false;
                        await Dispatcher.InvokeAsync(() => UpdateValues(data, completesInitialLoad: true, recordTrendHistory: true));
                        _sensorsRefreshFailureLogged = false;
                        await _delayProvider.Delay(TimeSpan.FromSeconds(_dashboardSettings.Store.SensorsRefreshIntervalSeconds), token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when sensors refresh is cancelled, no action needed
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled && !_sensorsRefreshFailureLogged)
                        {
                            Log.Instance.Trace($"Sensors refresh failed.", ex);
                            _sensorsRefreshFailureLogged = true;
                        }

                        var cached = TryGetSessionSensorDataForDisplay();
                        if (cached.HasValue)
                            await Dispatcher.InvokeAsync(() => UpdateValues(cached.Value));
                    }
                }
            }, token);
        }
    }

    private void CacheTextBlockReferences()
    {
        if (_textBlockReferencesCached)
            return;

        _cpuWattageText = FindNameCached("_cpuWattage") as TextBlock;
        _cpuTempRangeText = FindNameCached("_cpuTempRange") as TextBlock;
        _cpuVoltageText = FindNameCached("_cpuVoltage") as TextBlock;
        _cpuVoltageRangeText = FindNameCached("_cpuVoltageRange") as TextBlock;
        _gpuWattageText = FindNameCached("_gpuWattage") as TextBlock;
        _gpuTempRangeText = FindNameCached("_gpuTempRange") as TextBlock;
        _gpuVoltageText = FindNameCached("_gpuVoltage") as TextBlock;
        _gpuVoltageRangeText = FindNameCached("_gpuVoltageRange") as TextBlock;
        _textBlockReferencesCached = true;
    }

    private void UpdateValues(SensorsData data, bool completesInitialLoad = false, bool recordTrendHistory = false)
    {
        data = MergeSensorDataForDisplay(data, _lastRenderedSensorData);
        var shouldCompleteInitialLoad = completesInitialLoad && HasInitialSummarySensorData(data);
        _lastRenderedSensorData = data;
        CacheSessionSensorDataForDisplay(data);

        if (!_hasRenderedSensorData && shouldCompleteInitialLoad)
            CompleteInitialSensorDataLoad();

        UpdateValue(_cpuCoreClockBar, _cpuCoreClockLabel, data.CPU.MaxCoreClock, data.CPU.CoreClock,
            string.Concat((data.CPU.CoreClock / 1000.0).ToString("0.0"), " ", GigahertzUnit), string.Concat((data.CPU.MaxCoreClock / 1000.0).ToString("0.0"), " ", GigahertzUnit));
        UpdateValue(_cpuTemperatureBar, _cpuTemperatureLabel, data.CPU.MaxTemperature, data.CPU.Temperature,
            GetTemperatureText(data.CPU.Temperature), GetTemperatureText(data.CPU.MaxTemperature));
        UpdateValue(_cpuFanSpeedBar, _cpuFanSpeedLabel, data.CPU.MaxFanSpeed, data.CPU.FanSpeed,
            string.Concat(data.CPU.FanSpeed.ToString("0.0"), " ", RpmUnit), string.Concat(data.CPU.MaxFanSpeed.ToString("0.0"), " ", RpmUnit));

        // When detail panels are open, CPU wattage is owned by the LHM extended path
        // (includes cores/memory/platform breakdown). Writing a plain wattage here first
        // caused a one-second flicker with that richer string.
        if (_cpuWattageText is not null && !_detailsExpanded)
        {
            _cpuWattageText.Text = data.CPU.Wattage >= 0 ? $"{data.CPU.Wattage} W" : NotAvailableText();
        }

        if (_cpuTempRangeText is not null)
        {
             if (IsTemperatureRangeAvailable(data.CPU.MinTemperature, data.CPU.MaxTemperatureRecord))
                 _cpuTempRangeText.Text = $"{data.CPU.MinTemperature}{CelsiusUnit} ~ {data.CPU.MaxTemperatureRecord}{CelsiusUnit}";
             else
                 _cpuTempRangeText.Text = NotAvailableText();
        }

        if (_cpuVoltageText is not null)
        {
            _cpuVoltageText.Text = data.CPU.Voltage > 0 ? $"{data.CPU.Voltage:0.000} V" : NotAvailableText();
        }

        if (_cpuVoltageRangeText is not null)
        {
             if (IsVoltageRangeAvailable(data.CPU.MinVoltage, data.CPU.MaxVoltage))
                 _cpuVoltageRangeText.Text = $"{data.CPU.MinVoltage:0.000} V ~ {data.CPU.MaxVoltage:0.000} V";
             else
                 _cpuVoltageRangeText.Text = NotAvailableText();
        }

        // GPU Core Clock (Main view)
        UpdateValue(_gpuCoreClockBar, _gpuCoreClockLabel, data.GPU.MaxCoreClock, data.GPU.CoreClock,
            string.Concat((data.GPU.CoreClock / 1000.0).ToString("0.0"), " ", GigahertzUnit), string.Concat((data.GPU.MaxCoreClock / 1000.0).ToString("0.0"), " ", GigahertzUnit));

        // GPU Memory Clock (Details view)
        if (_gpuMemoryClockBar is not null && _gpuMemoryClockText is not null)
        {
            if (data.GPU.MaxMemoryClock < 0 || data.GPU.MemoryClock < 0)
            {
                _gpuMemoryClockBar.Value = 0;
                _gpuMemoryClockText.Text = "-";
            }
            else
            {
                _gpuMemoryClockBar.Maximum = data.GPU.MaxMemoryClock;
                _gpuMemoryClockBar.Value = data.GPU.MemoryClock;
                _gpuMemoryClockText.Text = string.Concat(data.GPU.MemoryClock.ToString("0.0"), " ", MegahertzUnit);
            }
        }

        UpdateValue(_gpuTemperatureBar, _gpuTemperatureLabel, data.GPU.MaxTemperature, data.GPU.Temperature,
            GetTemperatureText(data.GPU.Temperature), GetTemperatureText(data.GPU.MaxTemperature));
        UpdateValue(_gpuFanSpeedBar, _gpuFanSpeedLabel, data.GPU.MaxFanSpeed, data.GPU.FanSpeed,
            string.Concat(data.GPU.FanSpeed.ToString("0.0"), " ", RpmUnit), string.Concat(data.GPU.MaxFanSpeed.ToString("0.0"), " ", RpmUnit));

        if (_gpuWattageText is not null)
        {
            _gpuWattageText.Text = FormatPower(data.GPU.Wattage);
        }
        
        if (_gpuTempRangeText is not null)
        {
             if (IsTemperatureRangeAvailable(data.GPU.MinTemperature, data.GPU.MaxTemperatureRecord))
                 _gpuTempRangeText.Text = $"{data.GPU.MinTemperature}{CelsiusUnit} ~ {data.GPU.MaxTemperatureRecord}{CelsiusUnit}";
             else
                 _gpuTempRangeText.Text = NotAvailableText();
        }

        if (_gpuVoltageText is not null)
        {
            _gpuVoltageText.Text = data.GPU.Voltage > 0 ? $"{data.GPU.Voltage:0.000} V" : NotAvailableText();
        }
        
        if (_gpuVoltageRangeText is not null)
        {
             if (IsVoltageRangeAvailable(data.GPU.MinVoltage, data.GPU.MaxVoltage))
                 _gpuVoltageRangeText.Text = $"{data.GPU.MinVoltage:0.000} V ~ {data.GPU.MaxVoltage:0.000} V";
             else
                 _gpuVoltageRangeText.Text = NotAvailableText();
        }

        UpdateGaugesAndTrends(data, recordTrendHistory);

        QueueExtendedDetailValuesRefresh();
    }

    private void UpdateGaugesAndTrends(SensorsData data, bool recordTrendHistory = false)
    {
        // CPU / GPU utilization gauges (center ring of each section).
        if (_cpuGauge is not null)
        {
            var util = data.CPU.Utilization >= 0 ? data.CPU.Utilization : 0;
            _cpuGauge.Maximum = data.CPU.MaxUtilization > 0 ? data.CPU.MaxUtilization : 100;
            _cpuGauge.Value = util;
            _cpuGauge.ValueText = data.CPU.Utilization >= 0 ? string.Concat(data.CPU.Utilization.ToString(), "%") : "-";
        }

        if (_gpuGauge is not null)
        {
            var util = data.GPU.Utilization >= 0 ? data.GPU.Utilization : 0;
            _gpuGauge.Maximum = data.GPU.MaxUtilization > 0 ? data.GPU.MaxUtilization : 100;
            _gpuGauge.Value = util;
            _gpuGauge.ValueText = data.GPU.Utilization >= 0 ? string.Concat(data.GPU.Utilization.ToString(), "%") : "-";
        }

        // Trend charts: push the latest summary samples (utilization %, core clock GHz, temperature).
        PushTrendSamples(_cpuTrendChart, data.CPU, CpuScope, recordTrendHistory);
        PushTrendSamples(_gpuTrendChart, data.GPU, GpuScope, recordTrendHistory);
    }

    private static void PushTrendSamples(Charts.TrendChartControl? chart, SensorData data,
        string? scope = null, bool recordTrendHistory = false)
    {
        if (chart is null)
            return;

        if (data.Utilization >= 0)
        {
            chart.AddSample(TrendUtilizationKey, data.Utilization);
            if (recordTrendHistory && scope is not null)
                RecordTrendSample(scope, TrendUtilizationKey, data.Utilization);
        }

        if (data.CoreClock >= 0)
        {
            chart.AddSample(TrendCoreClockKey, data.CoreClock / 1000.0);
            if (recordTrendHistory && scope is not null)
                RecordTrendSample(scope, TrendCoreClockKey, data.CoreClock / 1000.0);
        }

        if (data.Temperature >= 0)
        {
            chart.AddSample(TrendTemperatureKey, data.Temperature);
            if (recordTrendHistory && scope is not null)
                RecordTrendSample(scope, TrendTemperatureKey, data.Temperature);
        }
    }

    private void PushBatteryTrendSamples(BatteryInformation info, bool recordTrendHistory = false)
    {
        if (_batteryTrendChart is null)
            return;

        if (info.BatteryPercentage >= 0)
        {
            _batteryTrendChart.AddSample(TrendBatteryChargeKey, info.BatteryPercentage);
            if (recordTrendHistory)
                RecordTrendSample(BatteryScope, TrendBatteryChargeKey, info.BatteryPercentage);
        }

        if (info.BatteryHealth >= 0)
        {
            _batteryTrendChart.AddSample(TrendBatteryHealthKey, info.BatteryHealth);
            if (recordTrendHistory)
                RecordTrendSample(BatteryScope, TrendBatteryHealthKey, info.BatteryHealth);
        }

        if (info.BatteryTemperatureC is { } temperature)
        {
            _batteryTrendChart.AddSample(TrendBatteryTemperatureKey, temperature);
            if (recordTrendHistory)
                RecordTrendSample(BatteryScope, TrendBatteryTemperatureKey, temperature);
        }
    }

    /// <summary>
    /// Records a single historical sample into the session-static trend history store,
    /// keyed by "scope:seriesKey". Caps each series at DefaultCapacity (60).
    /// </summary>
    private static void RecordTrendSample(string scope, string seriesKey, double value)
    {
        var key = string.Concat(scope, ":", seriesKey);
        lock (TrendHistoryLock)
        {
            if (!_sessionTrendHistory.TryGetValue(key, out var list))
            {
                list = new List<double>(TrendHistoryCapacity);
                _sessionTrendHistory[key] = list;
            }

            list.Add(value);

            // Trim excess — keep only the last TrendHistoryCapacity samples.
            if (list.Count > TrendHistoryCapacity)
                list.RemoveRange(0, list.Count - TrendHistoryCapacity);
        }
    }

    private void ClearTrendCharts()
    {
        _cpuTrendChart?.ClearAll();
        _batteryTrendChart?.ClearAll();
        _gpuTrendChart?.ClearAll();
    }

    private void InitializeTrendChartsFromSessionCache()
    {
        // Replay the full recorded history per series into each chart so the
        // trend lines are restored intact (not just a single latest point).
        if (_cpuTrendChart is not null)
            ReplayHistoryIntoChart(_cpuTrendChart, CpuScope);
        if (_gpuTrendChart is not null)
            ReplayHistoryIntoChart(_gpuTrendChart, GpuScope);
        if (_batteryTrendChart is not null)
            ReplayHistoryIntoChart(_batteryTrendChart, BatteryScope);
    }

    /// <summary>
    /// Replays all recorded samples for the given scope into the chart.
    /// DrawSeries requires at least 2 points or the line is invisible.
    /// </summary>
    private static void ReplayHistoryIntoChart(Charts.TrendChartControl chart, string scope)
    {
        List<KeyValuePair<string, List<double>>> captured;
        lock (TrendHistoryLock)
        {
            captured = _sessionTrendHistory
                .Where(kvp => kvp.Key.StartsWith(scope + ":", StringComparison.Ordinal))
                .Select(kvp => kvp)
                .ToList();
        }

        foreach (var entry in captured)
        {
            // entry.Key = "scope:seriesKey"  → extract seriesKey
            var seriesKey = entry.Key[(scope.Length + 1)..];
            foreach (var value in entry.Value)
                chart.AddSample(seriesKey, value);
        }
    }

    private void ResetSensorValues()
    {
        _lastRenderedSensorData = null;
        ClearSessionSensorData();
        ClearTrendHistory();
        ClearTrendCharts();
        UpdateValues(SensorsData.Empty);
    }

    private void InitializeFromSessionCache()
    {
        var cached = TryGetSessionSensorDataForDisplay();
        if (cached is null)
            return;

        UpdateValues(cached.Value, completesInitialLoad: true);
    }

    internal static SensorsData MergeSensorDataForDisplay(SensorsData current, SensorsData? previous) =>
        previous is { } previousData
            ? new SensorsData(
                MergeSensorDataForDisplay(current.CPU, previousData.CPU),
                MergeSensorDataForDisplay(current.GPU, previousData.GPU))
            : current;

    private static SensorData MergeSensorDataForDisplay(SensorData current, SensorData previous)
    {
        var utilization = KeepCurrentOrPrevious(current.Utilization, previous.Utilization, IsNonNegative);
        var coreClock = KeepCurrentOrPrevious(current.CoreClock, previous.CoreClock, IsNonNegative);
        var memoryClock = KeepCurrentOrPrevious(current.MemoryClock, previous.MemoryClock, IsNonNegative);
        var temperature = KeepCurrentOrPrevious(current.Temperature, previous.Temperature, IsNonNegative);
        var wattage = KeepCurrentOrPrevious(current.Wattage, previous.Wattage, IsNonNegative);
        var voltage = KeepCurrentOrPrevious(current.Voltage, previous.Voltage, value => value > 0);
        var fanSpeed = KeepCurrentOrPrevious(current.FanSpeed, previous.FanSpeed, IsNonNegative);

        var merged = new SensorData(
            utilization,
            ResolveMaximum(current.MaxUtilization, current.Utilization, previous.MaxUtilization),
            coreClock,
            ResolveMaximum(current.MaxCoreClock, current.CoreClock, previous.MaxCoreClock),
            memoryClock,
            ResolveMaximum(current.MaxMemoryClock, current.MemoryClock, previous.MaxMemoryClock),
            temperature,
            ResolveMaximum(current.MaxTemperature, current.Temperature, previous.MaxTemperature),
            wattage,
            voltage,
            fanSpeed,
            ResolveMaximum(current.MaxFanSpeed, current.FanSpeed, previous.MaxFanSpeed));

        var (minVoltage, maxVoltage) = IsVoltageRangeAvailable(current.MinVoltage, current.MaxVoltage)
            ? (current.MinVoltage, current.MaxVoltage)
            : IsVoltageRangeAvailable(previous.MinVoltage, previous.MaxVoltage)
                ? (previous.MinVoltage, previous.MaxVoltage)
                : (current.MinVoltage, current.MaxVoltage);

        var (minTemperature, maxTemperatureRecord) = IsTemperatureRangeAvailable(current.MinTemperature, current.MaxTemperatureRecord)
            ? (current.MinTemperature, current.MaxTemperatureRecord)
            : IsTemperatureRangeAvailable(previous.MinTemperature, previous.MaxTemperatureRecord)
                ? (previous.MinTemperature, previous.MaxTemperatureRecord)
                : (current.MinTemperature, current.MaxTemperatureRecord);

        return merged.WithMinMax(minVoltage, maxVoltage, minTemperature, maxTemperatureRecord);
    }

    private static int ResolveMaximum(int currentMaximum, int currentValue, int previousMaximum)
    {
        if (IsNonNegative(currentMaximum))
            return currentMaximum;

        if (IsNonNegative(previousMaximum))
            return previousMaximum;

        return IsNonNegative(currentValue) ? Math.Max(currentValue, 1) : currentMaximum;
    }

    private static int KeepCurrentOrPrevious(int current, int previous, Func<int, bool> isValid) =>
        isValid(current) ? current : previous;

    private static double KeepCurrentOrPrevious(double current, double previous, Func<double, bool> isValid) =>
        isValid(current) ? current : previous;

    internal static void CacheSessionSensorDataForDisplay(SensorsData data)
    {
        if (!HasAnySummarySensorData(data))
            return;

        lock (SessionSensorDataLock)
            _sessionSensorData = data;
    }

    private static void ClearTrendHistory()
    {
        lock (TrendHistoryLock)
            _sessionTrendHistory.Clear();
    }

    internal static SensorsData? TryGetSessionSensorDataForDisplay()
    {
        lock (SessionSensorDataLock)
            return _sessionSensorData;
    }

    private static void ClearSessionSensorData()
    {
        lock (SessionSensorDataLock)
            _sessionSensorData = null;
    }

    private void QueueExtendedDetailValuesRefresh()
    {
        if (!CanShowSensorDetails || !_detailsExpanded || !_sensorRuntimeAvailable || _sensorsGroupController is null)
            return;

        if (_extendedDetailsRefreshTask is { IsCompleted: false })
            return;

        _extendedDetailsRefreshTask = UpdateExtendedDetailValuesAsync();
    }

    private void CardControl_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var now = DateTime.UtcNow;
        if (e.ClickCount >= 2 || now - _lastDetailsToggleClick <= DoubleClickThreshold)
        {
            _lastDetailsToggleClick = DateTime.MinValue;
            ToggleDetails();
            e.Handled = true;
            return;
        }

        _lastDetailsToggleClick = now;
    }

    private void ToggleDetails()
    {
        if (_forceShowSensorDetails)
            return;

        if (!CanShowSensorDetails)
        {
            ShowDetailsWindow();
            return;
        }

        _detailsExpanded = !AreDetailsVisible();

        if (_detailsExpanded)
            ShowDetailPanels();
        else
            CollapseDetailPanels();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Sensor details toggled: {(_detailsExpanded ? Visibility.Visible : Visibility.Collapsed)}.");
    }

    private void ShowDetailsWindow()
    {
        if (_detailsWindow is { IsVisible: true })
        {
            _detailsWindow.Activate();
            return;
        }

        var window = new SensorDetailsWindow
        {
            Owner = Window.GetWindow(this)
        };
        window.Closed += SensorDetailsWindow_Closed;

        _detailsWindow = window;
        window.Show();
    }

    private void SensorDetailsWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is Window window && ReferenceEquals(_detailsWindow, window))
            _detailsWindow = null;
    }

    private void ShowDetailPanels()
    {
        _detailsExpanded = true;

        if (_sensorRuntimeAvailable)
        {
            SetVisibility("_cpuDetailsPanel", true);
            SetVisibility("_gpuDetailsPanel", true);
        }

        SetVisibility("_batteryDetailsPanel", true);

        _forceDetailedRefresh = true;
        _ = RefreshDetailedValuesAsync();
    }

    private bool AreDetailsVisible() =>
        CanShowSensorDetails
        && (IsElementVisible("_batteryDetailsPanel")
            || (_sensorRuntimeAvailable && (IsElementVisible("_cpuDetailsPanel") || IsElementVisible("_gpuDetailsPanel"))));

    private bool IsElementVisible(string name) =>
        FindNameCached(name) is FrameworkElement element && element.Visibility == Visibility.Visible;

    private async Task RefreshDetailedValuesAsync()
    {
        if (!CanShowSensorDetails || !_sensorRuntimeAvailable)
            return;

        try
        {
            var data = await _controller.GetDataAsync(true);
            await Dispatcher.InvokeAsync(() => UpdateValues(data, completesInitialLoad: true, recordTrendHistory: true));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Immediate detailed sensors refresh failed.", ex);
        }
    }

    private string GetTemperatureText(double? temperature)
    {
        if (temperature is null)
            return "-";

        return FormatTemperature(temperature.Value, _applicationSettings.Store.TemperatureUnit);
    }

    private static void UpdateValue(RangeBase bar, ContentControl label, double max, double value, string text, string? toolTipText = null)
    {
        if (max < 0 || value < 0)
        {
            bar.Minimum = 0;
            bar.Maximum = 1;
            bar.Value = 0;
            label.Content = "-";
            label.ToolTip = null;
            label.Tag = 0;
        }
        else
        {
            bar.Minimum = 0;
            bar.Maximum = max;
            bar.Value = value;
            label.Content = text;
            label.ToolTip = toolTipText is null ? null : string.Format(Resource.SensorsControl_Maximum, toolTipText);
            label.Tag = value;
        }
    }

    private void SetSensorSectionsVisible(bool visible)
    {
        SetVisibility("_cpuSection", visible);
        SetVisibility("_gpuSection", visible);

        if (!visible)
        {
            CollapseDetailPanels();
        }

        if (FindNameCached("_batterySectionColumn") is FrameworkElement batterySection)
            batterySection.Visibility = Visibility.Visible;

        Visibility = Visibility.Visible;
    }

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private static string NotAvailableText() => T("SensorsControl_NotAvailable", "N/A");

    private void CollapseDetailPanels()
    {
        _detailsExpanded = false;
        _forceDetailedRefresh = false;
        SetVisibility("_cpuDetailsPanel", false);
        SetVisibility("_batteryDetailsPanel", false);
        SetVisibility("_gpuDetailsPanel", false);
    }

    private static TaskCompletionSource CreateInitialSensorDataTaskCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private void CompleteInitialSensorDataLoad()
    {
        lock (_initialSensorDataLoadLock)
        {
            _hasRenderedSensorData = true;
            _firstSensorDataTaskCompletionSource.TrySetResult();
        }
        _ = Dispatcher.InvokeAsync(HideSkeletonOverlay);
    }

    private static bool HasInitialSummarySensorData(SensorData data) =>
        HasRenderableProgressMetric(data.Utilization, data.MaxUtilization)
        || HasRenderableProgressMetric(data.CoreClock, data.MaxCoreClock)
        || HasRenderableProgressMetric(data.Temperature, data.MaxTemperature)
        || HasRenderableProgressMetric(data.FanSpeed, data.MaxFanSpeed);

    private static bool HasAnySummarySensorData(SensorData data) =>
        data.Utilization >= 0
        || data.CoreClock >= 0
        || data.Temperature >= 0
        || data.FanSpeed >= 0;

    private static bool HasRenderableProgressMetric(int value, int max) =>
        value >= 0 && max >= 0;

    private static bool IsNonNegative(int value) => value >= 0;

    private static bool IsTemperatureRangeAvailable(int minTemperature, int maxTemperature) =>
        minTemperature > 0 && maxTemperature > 0;

    private static bool IsVoltageRangeAvailable(double minVoltage, double maxVoltage) =>
        minVoltage > 0 && maxVoltage > 0;

    private void SetInitialSensorPlaceholders()
    {
        UpdateDetailText("_cpuWattage", NotAvailableText());
        UpdateDetailText("_cpuVoltage", NotAvailableText());
        UpdateDetailText("_cpuPCoreClockTitle", T("SensorsControl_PCoreClock_Title", "P Core Clock"));
        UpdateDetailText("_cpuPCoreClock", NotAvailableText());
        UpdateDetailText("_cpuECoreClockTitle", T("SensorsControl_ECoreClock_Title", "E Core Clock"));
        UpdateDetailText("_cpuECoreClock", NotAvailableText());
        UpdateDetailText("_cpuTempRange", NotAvailableText());
        UpdateDetailText("_cpuVoltageRange", NotAvailableText());
        UpdateDetailText("_cpuMemoryUsageTitle", T("SensorsControl_MemoryUsage_Title", "Memory Usage"));
        UpdateDetailText("_cpuMemoryUsage", NotAvailableText());
        UpdateDetailText("_cpuMemoryTemperatureTitle", T("SensorsControl_MemoryTemperature_Title", "Memory Temperature"));
        UpdateDetailText("_cpuMemoryTemperature", NotAvailableText());
        UpdateDetailText("_cpuSsdTemperatureTitle", T("SensorsControl_SsdTemperature_Title", "SSD Temperature"));
        UpdateDetailText("_cpuSsdTemperature", NotAvailableText());
        UpdateDetailText("_gpuWattage", NotAvailableText());
        UpdateDetailText("_gpuVoltage", NotAvailableText());
        UpdateDetailText("_gpuTempRange", NotAvailableText());
        UpdateDetailText("_gpuVoltageRange", NotAvailableText());
        UpdateDetailText("_gpuVramUsageTitle", T("SensorsControl_VramUsage_Title", "VRAM Usage"));
        UpdateDetailText("_gpuVramUsage", NotAvailableText());
        UpdateDetailText("_gpuVramTemperatureTitle", T("SensorsControl_VramTemperature_Title", "VRAM Temperature"));
        UpdateDetailText("_gpuVramTemperature", NotAvailableText());
        UpdateDetailText("_gpuHotSpotTemperatureTitle", T("SensorsControl_GpuHotSpotTemperature_Title", "GPU Hot Spot"));
        UpdateDetailText("_gpuHotSpotTemperature", NotAvailableText());
        UpdateDetailText("_gpuPcieThroughputTitle", T("SensorsControl_GpuPcieThroughput_Title", "PCIe Throughput"));
        UpdateDetailText("_gpuPcieThroughput", NotAvailableText());

        UpdateModelNameText("_cpuModelName", _cpuName);
        UpdateModelNameText("_gpuModelName", _gpuName);
        UpdateModelNameText("_batteryModelName", null);
    }

    private async Task UpdateExtendedDetailValuesAsync()
    {
        if (_sensorsGroupController is null)
            return;

        try
        {
            if (!CanShowSensorDetails || !_detailsExpanded || !_sensorRuntimeAvailable)
                return;

            var refreshVersion = Interlocked.Increment(ref _extendedDetailsRefreshVersion);

            if (!_sensorsGroupController.IsLibreHardwareMonitorInitialized())
                _ = await _sensorsGroupController.IsSupportedAsync();

            if (!_sensorsGroupController.IsLibreHardwareMonitorInitialized())
                return;

            await _sensorsGroupController.UpdateAsync();

            // Only fields that the main GetDataAsync path does NOT already render.
            // Writing GPU wattage / voltage / memory-clock here as well caused a 1 Hz flicker
            // (NvAPI snapshot vs LibreHardwareMonitor values alternating each refresh).
            var gpuVramUsedTask = _sensorsGroupController.GetGpuVramUsedAsync();
            var gpuVramTotalTask = _sensorsGroupController.GetGpuVramTotalAsync();
            var gpuVramUtilizationTask = _sensorsGroupController.GetGpuVramUtilizationAsync();
            var gpuVramTemperatureTask = _sensorsGroupController.GetGpuVramTemperatureAsync();
            var gpuHotSpotTemperatureTask = _sensorsGroupController.GetGpuHotSpotTemperatureAsync();
            var gpuPcieRxThroughputTask = _sensorsGroupController.GetGpuPcieRxThroughputAsync();
            var gpuPcieTxThroughputTask = _sensorsGroupController.GetGpuPcieTxThroughputAsync();
            var gpuIsIntegratedTask = _sensorsGroupController.IsCurrentGpuIntegratedAsync();
            var cpuPowerTask = _sensorsGroupController.GetCpuPowerAsync();
            var cpuPCoreClockTask = _sensorsGroupController.GetCpuPCoreClockAsync();
            var cpuECoreClockTask = _sensorsGroupController.GetCpuECoreClockAsync();
            var memoryUsageTask = _sensorsGroupController.GetMemoryUsageAsync();
            var memoryUsedTask = _sensorsGroupController.GetMemoryUsedAsync();
            var memoryTotalTask = _sensorsGroupController.GetMemoryTotalAsync();
            var memoryTemperatureTask = _sensorsGroupController.GetHighestMemoryTemperatureAsync();
            var ssdTemperaturesTask = _sensorsGroupController.GetSsdTemperaturesAsync();
            var cpuComponentPowersTask = _sensorsGroupController.GetCpuComponentPowersAsync();

            await Task.WhenAll(
                gpuVramUsedTask,
                gpuVramTotalTask,
                gpuVramUtilizationTask,
                gpuVramTemperatureTask,
                gpuHotSpotTemperatureTask,
                gpuPcieRxThroughputTask,
                gpuPcieTxThroughputTask,
                gpuIsIntegratedTask,
                cpuPowerTask,
                cpuPCoreClockTask,
                cpuECoreClockTask,
                memoryUsageTask,
                memoryUsedTask,
                memoryTotalTask,
                memoryTemperatureTask,
                ssdTemperaturesTask,
                cpuComponentPowersTask);

            var gpuIsIntegrated = await gpuIsIntegratedTask;
            var gpuVramUsed = await gpuVramUsedTask;
            var gpuVramTotal = await gpuVramTotalTask;
            var gpuVramUtilization = await gpuVramUtilizationTask;
            var gpuVramTemperature = await gpuVramTemperatureTask;
            var gpuHotSpotTemperature = await gpuHotSpotTemperatureTask;
            var gpuPcieRxThroughput = await gpuPcieRxThroughputTask;
            var gpuPcieTxThroughput = await gpuPcieTxThroughputTask;
            var cpuPower = await cpuPowerTask;
            var cpuPCoreClock = await cpuPCoreClockTask;
            var cpuECoreClock = await cpuECoreClockTask;
            var memoryUsage = await memoryUsageTask;
            var memoryUsed = await memoryUsedTask;
            var memoryTotal = await memoryTotalTask;
            var memoryTemperature = await memoryTemperatureTask;
            var ssdTemperatures = await ssdTemperaturesTask;
            var cpuComponentPowers = await cpuComponentPowersTask;

            await Dispatcher.InvokeAsync(() =>
            {
                if (refreshVersion != _extendedDetailsRefreshVersion)
                    return;

                UpdateDetailText("_gpuVramUsageTitle", GetGpuMemoryUsageTitle(gpuIsIntegrated));
                UpdateDetailText("_gpuVramUsage", FormatUsageInGigabytes(gpuVramUsed, gpuVramTotal, gpuVramUtilization));
                UpdateDetailText("_gpuVramTemperature", GetTemperatureText(gpuVramTemperature >= 0 ? (double?)gpuVramTemperature : null));
                UpdateDetailText("_gpuHotSpotTemperature", GetTemperatureText(gpuHotSpotTemperature >= 0 ? (double?)gpuHotSpotTemperature : null));
                UpdateDetailText("_gpuPcieThroughput", FormatThroughputPair(gpuPcieRxThroughput, gpuPcieTxThroughput));
                // Prefer LHM CPU power breakdown (richer than summary wattage).
                UpdateDetailText("_cpuWattage", FormatCpuPowerBreakdown(cpuPower, cpuComponentPowers));
                UpdateDetailText("_cpuPCoreClock", FormatFrequency(cpuPCoreClock));
                UpdateDetailText("_cpuECoreClock", FormatFrequency(cpuECoreClock));
                UpdateDetailText("_cpuMemoryUsage", FormatUsageInGigabytes(memoryUsed, memoryTotal, memoryUsage));
                UpdateDetailText("_cpuMemoryTemperature", GetTemperatureText(memoryTemperature > 0 ? memoryTemperature : null));
                UpdateDetailText("_cpuSsdTemperature", FormatTemperaturePair(ssdTemperatures, _applicationSettings.Store.TemperatureUnit));
                // Ranges: keep min/max from the primary SensorsData path; only seed when empty.
                UpdateDetailText("_cpuTempRange", FormatTemperatureRangeText(_cpuTemperatureLabel?.Content as string ?? _cpuTemperatureLabel?.Content?.ToString(), _cpuTempRange.Text));
                UpdateDetailText("_cpuVoltageRange", FormatFallbackRangeText(_cpuVoltage.Text, _cpuVoltageRange.Text));
                UpdateDetailText("_gpuTempRange", FormatTemperatureRangeText(_gpuTemperatureLabel?.Content as string ?? _gpuTemperatureLabel?.Content?.ToString(), _gpuTempRange.Text));
                UpdateDetailText("_gpuVoltageRange", FormatFallbackRangeText(_gpuVoltage.Text, _gpuVoltageRange.Text));
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Extended sensor detail refresh failed.", ex);
        }
    }

    internal static string GetGpuMemoryUsageTitle(bool isIntegratedGpu) =>
        isIntegratedGpu
            ? T("SensorsControl_SharedMemoryUsage_Title", "Shared Memory Usage")
            : T("SensorsControl_VramUsage_Title", "VRAM Usage");

    internal static string FormatUsageInGigabytes(float usedGb, float totalGb, float percentage = -1f)
    {
        if (usedGb < 0)
        {
            return percentage >= 0
                ? $"{percentage:0}%"
                : "-";
        }

        if (totalGb <= 0)
        {
            return percentage >= 0
                ? $"{usedGb:0.0} GB ({percentage:0}%)"
                : $"{usedGb:0.0} GB";
        }

        if (percentage < 0)
            percentage = totalGb > 0 ? (usedGb / totalGb) * 100f : -1f;

        return percentage >= 0
            ? $"{usedGb:0.0} / {totalGb:0.0} GB ({percentage:0}%)"
            : $"{usedGb:0.0} / {totalGb:0.0} GB";
    }

    internal static string FormatTemperaturePair((float first, float second) temperatures, TemperatureUnit temperatureUnit)
    {
        var first = temperatures.first >= 0 ? FormatTemperature(temperatures.first, temperatureUnit) : null;
        var second = temperatures.second >= 0 ? FormatTemperature(temperatures.second, temperatureUnit) : null;

        return (first, second) switch
        {
            ({ } a, { } b) => $"{a} / {b}",
            ({ } a, null) => a,
            (null, { } b) => b,
            _ => "-"
        };
    }

    internal static string FormatThroughputPair(float rxBytesPerSecond, float txBytesPerSecond)
    {
        var rx = FormatThroughput(rxBytesPerSecond);
        var tx = FormatThroughput(txBytesPerSecond);

        // Prefer a line break so long "Rx … / Tx …" strings wrap cleanly inside the
        // detail column instead of overflowing into the temperature column.
        return (rx, tx) switch
        {
            ({ } a, { } b) => $"Rx {a}\nTx {b}",
            ({ } a, null) => $"Rx {a}",
            (null, { } b) => $"Tx {b}",
            _ => "-"
        };
    }

    private static readonly System.Collections.Generic.List<string> _cpuPowerParts = new(4);

    internal static string FormatCpuPowerBreakdown(float totalWatts, (float cores, float memory, float platform) components)
    {
        _cpuPowerParts.Clear();
        var coresLabel = T("SensorsControl_CpuCoresPower_Label", "Cores");
        var memoryLabel = T("SensorsControl_CpuMemoryPower_Label", "Memory");
        var platformLabel = T("SensorsControl_CpuPlatformPower_Label", "Platform");

        if (totalWatts >= 0)
            _cpuPowerParts.Add($"{totalWatts} W");

        if (components.cores > 0)
            _cpuPowerParts.Add($"{coresLabel} {components.cores:0.#} W");

        if (components.memory > 0)
            _cpuPowerParts.Add($"{memoryLabel} {components.memory:0.#} W");

        if (components.platform > 0)
            _cpuPowerParts.Add($"{platformLabel} {components.platform:0.#} W");

        return _cpuPowerParts.Count > 0 ? string.Join(" | ", _cpuPowerParts) : NotAvailableText();
    }

    internal static string FormatVoltage(float voltage) =>
        voltage > 0 ? $"{voltage:0.000} V" : NotAvailableText();

    internal static string FormatPower(float wattage) =>
        wattage >= 0 ? $"{wattage:0.#} W" : NotAvailableText();

    internal static string FormatPowerKeepingPrevious(float wattage, string? previousText) =>
        wattage >= 0
            ? FormatPower(wattage)
            : !string.IsNullOrWhiteSpace(previousText) && previousText != NotAvailableText()
                ? previousText
                : NotAvailableText();

    internal static string FormatNullableTemperature(double? temperature, TemperatureUnit temperatureUnit) =>
        temperature is { } value ? FormatTemperature(value, temperatureUnit) : NotAvailableText();

    internal static string FormatFrequency(float frequencyMHz) =>
        frequencyMHz > 0 ? $"{frequencyMHz / 1000.0:0.0} {GigahertzUnit}" : NotAvailableText();

    internal static string FormatFallbackRangeText(string? primaryValue, string? existingRangeText)
    {
        if (!string.IsNullOrWhiteSpace(existingRangeText) && existingRangeText != NotAvailableText())
            return existingRangeText;

        return !string.IsNullOrWhiteSpace(primaryValue) && primaryValue != NotAvailableText()
            ? primaryValue
            : NotAvailableText();
    }

    internal static string FormatTemperatureRangeText(string? primaryTemperatureText, string? existingRangeText) =>
        FormatFallbackRangeText(primaryTemperatureText, existingRangeText);

    internal static bool IsUsefulDetailValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        return !string.Equals(trimmed, "-", StringComparison.Ordinal)
            && !string.Equals(trimmed, NotAvailableText(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? FormatThroughput(float bytesPerSecond)
    {
        if (bytesPerSecond < 0)
            return null;

        const float kb = 1024f;
        const float mb = kb * 1024f;
        const float gb = mb * 1024f;

        return bytesPerSecond switch
        {
            >= gb => $"{bytesPerSecond / gb:0.00} GB/s",
            >= mb => $"{bytesPerSecond / mb:0.00} MB/s",
            >= kb => $"{bytesPerSecond / kb:0.00} KB/s",
            _ => $"{bytesPerSecond:0} B/s"
        };
    }

    internal static string FormatTemperature(double temperature, TemperatureUnit temperatureUnit)
    {
        if (temperatureUnit == TemperatureUnit.F)
        {
            temperature *= 9.0 / 5.0;
            temperature += 32;
            return $"{temperature:0} {FahrenheitUnit}";
        }

        return $"{temperature:0} {CelsiusUnit}";
    }

    internal static string? NormalizeModelName(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return null;

        return modelName.Trim();
    }

    internal static string NormalizeHardwareNameOrFallback(string? hardwareName, string fallback)
    {
        var normalized = NormalizeModelName(hardwareName);
        return normalized is null || string.Equals(normalized, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
            ? fallback
            : normalized;
    }

    private void UpdateModelNameText(string elementName, string? modelName)
    {
        if (FindNameCached(elementName) is not TextBlock textBlock)
            return;

        var normalizedModelName = NormalizeModelName(modelName);
        textBlock.Text = normalizedModelName ?? string.Empty;
        textBlock.Visibility = normalizedModelName is null || _sensorSummaryLayoutMode == SensorSummaryLayoutMode.Compact
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
}
