using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Humanizer;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Settings;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Dashboard;
using UniversalDeviceToolkit.Avalonia.Controls;
using MenuItem = UniversalDeviceToolkit.Avalonia.Controls.MenuItem;
using UniversalDeviceToolkit.Abstractions.Utils;
using WpfHardwareSensorSettings = UniversalDeviceToolkit.Avalonia.Settings.HardwareSensorSettings;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard
{
public partial class SensorsControl : global::Avalonia.Controls.UserControl, IDisposable
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
    private readonly WpfHardwareSensorSettings _hardwareSensorSettings = IoCContainer.Resolve<WpfHardwareSensorSettings>();
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
    private readonly Dictionary<string, Control?> _findNameCache = new(StringComparer.Ordinal);

    public SensorsControl()
    {
        InitializeComponent();
        _hardwareSensorSettings.SectionsChanged += HardwareSensorSettings_SectionsChanged;
        CacheTextBlockReferences();
        ApplySensorSectionConfiguration();
        InitializeContextMenu();
        InitializeTrendCharts();
        SetInitialSensorPlaceholders();
        CollapseDetailPanels();
        InitializeFromSessionCache();
        _ = FetchHardwareNamesAsync();

        PropertyChanged += SensorsControl_IsVisibleChanged;
        SizeChanged += SensorsControl_SizeChanged;
        Loaded += SensorsControl_Loaded;
        Unloaded += SensorsControl_Unloaded;
    }

    private void SensorsControl_Loaded(object sender, RoutedEventArgs e)
    {
        // First measure can be 0/narrow during page construction; re-apply once the
        // control is in the visual tree so trend charts are not stuck in Compact hide.
        var width = Bounds.Width > 1 ? Bounds.Width : 1200;
        ApplySensorSummaryLayout(width, force: true);

        // NavigationStore caches DashboardPage — Unloaded stops polling but the control
        // is reused. Restart refresh when the cached page is presented again.
        if (IsVisible)
        {
            Refresh();
            RefreshBattery();
        }
    }

    private void SensorsControl_Unloaded(object sender, RoutedEventArgs e)
    {
        // Cached navigation unloads without destroying the control — pause polling only.
        // Keep event handlers attached so Loaded / IsVisibleChanged can resume on return.
        StopSensorRefresh();
        StopBatteryRefresh();
    }

    public void Dispose()
    {
        StopSensorRefresh();
        StopBatteryRefresh();

        PropertyChanged -= SensorsControl_IsVisibleChanged;
        SizeChanged -= SensorsControl_SizeChanged;
        Loaded -= SensorsControl_Loaded;
        Unloaded -= SensorsControl_Unloaded;
        _hardwareSensorSettings.SectionsChanged -= HardwareSensorSettings_SectionsChanged;
    }

    internal enum SensorSummaryLayoutMode
    {
        Compact,      // < 900px
        Standard,     // 900px - 1499px
        Wide,         // 1500px - 1999px
        UltraWide     // ≥ 2000px (full-screen optimized)
    }

    private const string TrendUtilizationKey = "util";
    private const string TrendCoreClockKey = "clock";
    private const string TrendTemperatureKey = "temp";
    private const string TrendBatteryRateKey = "battery-rate";
    private const string TrendBatteryTemperatureKey = "battery-temp";

    private void InitializeTrendCharts()
    {
        foreach (var chart in new[] { _cpuTrendChart, _gpuTrendChart })
        {
            if (chart is null)
                continue;

            chart.DefineSeries(TrendUtilizationKey, GetChartColor("ChartUtilizationColor", global::Avalonia.Media.Colors.DodgerBlue), 100);
            chart.DefineSeries(TrendCoreClockKey, GetChartColor("ChartCoreClockColor", global::Avalonia.Media.Colors.MediumSeaGreen));
            chart.DefineSeries(TrendTemperatureKey, GetChartColor("ChartTemperatureColor", global::Avalonia.Media.Colors.Goldenrod), 110);
        }

        if (_batteryTrendChart is not null)
        {
            _batteryTrendChart.DefineSeries(TrendBatteryRateKey, GetChartColor("ChartBatteryColor", global::Avalonia.Media.Colors.MediumSeaGreen));
            _batteryTrendChart.DefineSeries(TrendBatteryTemperatureKey, GetChartColor("ChartTemperatureColor", global::Avalonia.Media.Colors.Goldenrod), 60);
        }
    }

    private global::Avalonia.Media.Color GetChartColor(string resourceKey, global::Avalonia.Media.Color fallback) =>
        this.TryFindResource(resourceKey, out var colorValue) && colorValue is global::Avalonia.Media.Color color ? color : fallback;

    private void SensorsControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width == e.PreviousSize.Width)
            return;

        // During live edge-resize content is re-rendered on every frame — skip thrashy gauge reflow.
        // AVALONIA: WPF BitmapCache/CacheMode removed (no Avalonia equivalent); live-resize guard kept.
        if (TopLevel.GetTopLevel(this) as Window is { } host && WindowResizeStabilityHelper.IsLiveResizing(host))
        {
            return;
        }

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

        if (width >= LayoutBreakpoints.SensorsUltraWide)
            return SensorSummaryLayoutMode.UltraWide;

        if (width >= LayoutBreakpoints.SensorsWide)
            return SensorSummaryLayoutMode.Wide;

        if (width >= LayoutBreakpoints.SensorsStandard)
            return SensorSummaryLayoutMode.Standard;

        return SensorSummaryLayoutMode.Compact;
    }

    internal static bool CanShowSensorDetailsForWidth(double width)
    {
        var mode = GetSensorSummaryLayoutMode(width);
        return mode == SensorSummaryLayoutMode.Wide || mode == SensorSummaryLayoutMode.UltraWide;
    }

    private void ApplySensorSummaryLayout(double width, bool force = false)
    {
        var mode = GetSensorSummaryLayoutMode(width);
        var isCompact = mode == SensorSummaryLayoutMode.Compact;
        var isWide = mode == SensorSummaryLayoutMode.Wide;
        var isUltraWide = mode == SensorSummaryLayoutMode.UltraWide;

        ApplySkeletonSummaryLayout(isCompact, isWide, isUltraWide);

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

        ApplySummaryGaugeSize(_cpuGauge, isCompact, isUltraWide);
        ApplySummaryGaugeSize(_batteryGauge, isCompact, isUltraWide);
        ApplySummaryGaugeSize(_gpuGauge, isCompact, isUltraWide);

        ApplyTrendPanelHeight(_cpuTrendPanel, isWide, isUltraWide);
        ApplyTrendPanelHeight(_batteryTrendPanel, isWide, isUltraWide);
        ApplyTrendPanelHeight(_gpuTrendPanel, isWide, isUltraWide);

        ApplyProgressBarMaxWidth(_cpuCoreClockMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_cpuTemperatureMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_cpuFanSpeedMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_batteryHealthMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_batteryTemperatureMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_batteryRateMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_gpuCoreClockMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_gpuTemperatureMetric, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_gpuFanSpeedMetric, isWide, isUltraWide);

        if (!CanShowSensorDetails)
            CollapseDetailPanels();
    }

    private void ApplySkeletonSummaryLayout(bool isCompact, bool isWide, bool isUltraWide)
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

        // Gauge sizes: GaugeSizeSM (88) compact / GaugeSizeMD (110) standard+wide / 130 ultra-wide.
        ApplySummaryGaugeSize(_skeletonCpuGauge, isCompact, isUltraWide);
        ApplySummaryGaugeSize(_skeletonBatteryGauge, isCompact, isUltraWide);
        ApplySummaryGaugeSize(_skeletonGpuGauge, isCompact, isUltraWide);

        // Trend panel heights: 120 standard / 150 wide / 180 ultra-wide.
        ApplyTrendPanelHeight(_skeletonCpuTrendPanel, isWide, isUltraWide);
        ApplyTrendPanelHeight(_skeletonBatteryTrendPanel, isWide, isUltraWide);
        ApplyTrendPanelHeight(_skeletonGpuTrendPanel, isWide, isUltraWide);

        // Metric bars MaxWidth: 260 standard / 320 wide / 400 ultra-wide (same as live ProgressBars).
        ApplyProgressBarMaxWidth(_skeletonCpuBar0, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonCpuBar1, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonCpuBar2, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonBatteryBar0, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonBatteryBar1, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonBatteryBar2, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonGpuBar0, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonGpuBar1, isWide, isUltraWide);
        ApplyProgressBarMaxWidth(_skeletonGpuBar2, isWide, isUltraWide);
    }

    private void SetLiveSensorContentVisible(bool visible)
    {
        if (_sensorsCard is not null)
            _sensorsCard.Opacity = visible ? 1 : 0;
    }

    private static void ApplySummaryGaugeSize(Control? gauge, bool isCompact, bool isUltraWide = false)
    {
        if (gauge is null)
            return;

        var size = LayoutBreakpoints.GetGaugeSize(isCompact, isUltraWide);
        gauge.Width = size;
        gauge.Height = size;
        gauge.MinWidth = size;
        gauge.MinHeight = size;
    }

    private static void ApplyTrendPanelHeight(Control? trendPanel, bool isWide, bool isUltraWide = false)
    {
        if (trendPanel is null)
            return;

        trendPanel.Height = LayoutBreakpoints.GetTrendHeight(isWide, isUltraWide);
    }

    private static void ApplyProgressBarMaxWidth(Control? progressBar, bool isWide, bool isUltraWide = false)
    {
        if (progressBar is null)
            return;

        var maxWidth = LayoutBreakpoints.GetProgressBarMaxWidth(isWide, isUltraWide);
        if (progressBar is MarqueeMetricBar metric)
            metric.BarMaxWidth = maxWidth;
        else
            progressBar.MaxWidth = maxWidth;
    }

    private bool CanShowSensorDetails => _forceShowSensorDetails ||
        _sensorSummaryLayoutMode == SensorSummaryLayoutMode.Wide ||
        _sensorSummaryLayoutMode == SensorSummaryLayoutMode.UltraWide;

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

        if (_skeletonOverlay.IsVisible != true)
        {
            SetLiveSensorContentVisible(true);
            return;
        }

        SetLiveSensorContentVisible(true);

        try
        {
            // AVALONIA: WPF Storyboard replaced by an Animation resource; collapse the overlay once it finishes.
            if (this.TryFindResource("SkeletonFadeOutAnimation", out var skeletonAnimationValue) && skeletonAnimationValue is Animation animation)
            {
                _ = animation.RunAsync(_skeletonOverlay).ContinueWith(
                    _ => Dispatcher.UIThread.Post(() => _skeletonOverlay.IsVisible = false));
            }
            else
            {
                _skeletonOverlay.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"HideSkeletonOverlay failed: {ex.Message}", ex);
            _skeletonOverlay.IsVisible = false;
        }
    }

    private void ShowSkeletonOverlay()
    {
        if (_skeletonOverlay is null)
            return;

        // Prefer real measure so first skeleton frame matches current window width
        // (compact/standard/wide), not a stale default mode from construction.
        var width = Bounds.Width > 1 ? Bounds.Width : (_sensorsCard?.Bounds.Width > 1 ? _sensorsCard.Bounds.Width : 1200);
        ApplySensorSummaryLayout(width, force: true);

        SetLiveSensorContentVisible(false);
        _skeletonOverlay.Opacity = 1;
        _skeletonOverlay.IsVisible = true;
    }

    public Task RestartInitialSensorDataLoad()
    {
        lock (_initialSensorDataLoadLock)
        {
            if (_lastRenderedSensorData is { } data && CanCompleteInitialLoadFromCachedSensorData(data))
            {
                _hasRenderedSensorData = true;
                _firstSensorDataTaskCompletionSource.TrySetResult();
                _ = Dispatcher.UIThread.InvokeAsync(HideSkeletonOverlay);
                return _firstSensorDataTaskCompletionSource.Task;
            }

            _hasRenderedSensorData = false;
            if (_firstSensorDataTaskCompletionSource.Task.IsCompleted)
                _firstSensorDataTaskCompletionSource = CreateInitialSensorDataTaskCompletionSource();

            _ = Dispatcher.UIThread.InvokeAsync(ShowSkeletonOverlay);
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
        ApplySensorSummaryLayout(Bounds.Width > 0 ? Bounds.Width : 1200);
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

        await Dispatcher.UIThread.InvokeAsync(() =>
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
                        await Dispatcher.UIThread.InvokeAsync(() =>
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
            _batteryGauge.RingBrush = (this.TryFindResource(batteryInfo.IsLowBattery ? "ChartCautionBrush" : "ChartBatteryBrush", out var batteryBrushValue)
                ? batteryBrushValue as global::Avalonia.Media.Brush
                : null)
                ?? _batteryGauge.RingBrush;
        }

        if (FindNameCached("_batteryStatusLabel") is TextBlock statusLabel)
        {
            statusLabel.Text = GetBatteryStatusText(batteryInfo);
            statusLabel.IsVisible = (batteryInfo.IsLowBattery || powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage)
                ? false
                : true;
        }

        // Warnings
        SetVisibility("_lowBatteryWarning", batteryInfo.IsLowBattery);
        SetVisibility("_lowWattageWarning", powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage);

        // Icon
        if (FindNameCached("_batteryIcon") is SymbolIcon icon)
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
        if (FindNameCached("_batteryHealthMetric") is MarqueeMetricBar healthMetric)
            UpdateValue(healthMetric, 100, info.BatteryHealth, $"{info.BatteryHealth:0.00}%");

        UpdateBatteryHealthGauge(info.BatteryHealth);

        if (FindNameCached("_batteryTemperatureMetric") is MarqueeMetricBar tempMetric)
        {
            var temperature = info.BatteryTemperatureC ?? -1;
            UpdateValue(tempMetric, 60, temperature, GetTemperatureText(info.BatteryTemperatureC));
        }

        if (FindNameCached("_batteryRateMetric") is MarqueeMetricBar rateMetric)
        {
            var rateW = Math.Abs(info.DischargeRate / 1000.0);
            // Assuming 100W is max reasonable charge/discharge rate for bar scaling
            UpdateValue(rateMetric, 100, rateW, $"{info.DischargeRate / 1000.0:+0.00;-0.00;0.00} W");
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
    /// Color: green >= 80, caution 60-80, critical/red < 60.
    /// </summary>
    private void UpdateBatteryHealthGauge(double healthPercent)
    {
        if (_batteryHealthGauge is null)
            return;

        var value = healthPercent < 0 ? 0 : Math.Clamp(healthPercent, 0, 100);
        _batteryHealthGauge.Maximum = 100;
        _batteryHealthGauge.Value = value;
        _batteryHealthGauge.ValueText = healthPercent < 0 ? "-" : string.Format("{0:0}%", value);

        var brushKey = value >= 80
            ? "ChartBatteryBrush"
            : value >= 60
                ? "ChartCautionBrush"
                : "StatusCriticalBrush";

        if (this.TryFindResource(brushKey, out var healthBrushValue) && healthBrushValue is global::Avalonia.Media.Brush brush)
            _batteryHealthGauge.RingBrush = brush;
        else if (this.TryFindResource("StatusCriticalBrush", out var fallbackBrushValue) && fallbackBrushValue is global::Avalonia.Media.Brush fallback)
            _batteryHealthGauge.RingBrush = fallback;
    }

    private void UpdateDetailText(string name, string text)
    {
        var displayText = text == "-" || !IsUsefulDetailValue(text) ? string.Empty : text;

        if (FindNameCached(name) is TextBlock tb)
        {
            tb.Text = displayText;
        }
        // AVALONIA: WPF Label removed; all detail values are TextBlocks — the Label branch was dead code.

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

        if (detailElementName is not null && FindNameCached(detailElementName) is Control detailElement)
            detailElement.IsVisible = string.IsNullOrWhiteSpace(displayText) ? false : true;
    }

    private void SetVisibility(string name, bool visible)
    {
        if (FindNameCached(name) is Control el) el.IsVisible = visible ? true : false;
    }

    private Control? FindNameCached(string name)
    {
        if (_findNameCache.TryGetValue(name, out var cached))
            return cached;

        var element = this.FindNameScope()?.Find(name) as Control;
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

        UpdateValue(_cpuCoreClockMetric, data.CPU.MaxCoreClock, data.CPU.CoreClock,
            string.Concat((data.CPU.CoreClock / 1000.0).ToString("0.0"), " ", GigahertzUnit), string.Concat((data.CPU.MaxCoreClock / 1000.0).ToString("0.0"), " ", GigahertzUnit));
        UpdateValue(_cpuTemperatureMetric, data.CPU.MaxTemperature, data.CPU.Temperature,
            GetTemperatureText(data.CPU.Temperature), GetTemperatureText(data.CPU.MaxTemperature));
        UpdateValue(_cpuFanSpeedMetric, data.CPU.MaxFanSpeed, data.CPU.FanSpeed,
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
        UpdateValue(_gpuCoreClockMetric, data.GPU.MaxCoreClock, data.GPU.CoreClock,
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

        UpdateValue(_gpuTemperatureMetric, data.GPU.MaxTemperature, data.GPU.Temperature,
            GetTemperatureText(data.GPU.Temperature), GetTemperatureText(data.GPU.MaxTemperature));
        UpdateValue(_gpuFanSpeedMetric, data.GPU.MaxFanSpeed, data.GPU.FanSpeed,
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

        // Charge % and health % are near-constant — charting them produces flat lines.
        // Chart the quantities that actually move: charge/discharge rate and temperature.
        var rateWatts = Math.Abs(info.DischargeRate / 1000.0);
        _batteryTrendChart.AddSample(TrendBatteryRateKey, rateWatts);
        if (recordTrendHistory)
            RecordTrendSample(BatteryScope, TrendBatteryRateKey, rateWatts);

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
        // Fan RPM: keep last positive reading when the new sample is unknown (-1).
        // Do not sticky-lock a false 0 forever when a later sample is -1 then positive via LHM.
        var fanSpeed = MergeFanSpeed(current.FanSpeed, previous.FanSpeed);

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

    /// <summary>
    /// Positive RPM always wins. Fresh 0 (parked) is shown. Unknown (-1) keeps last positive.
    /// </summary>
    private static int MergeFanSpeed(int current, int previous)
    {
        if (current > 0)
            return current;
        if (current == 0)
            return 0;
        return previous > 0 ? previous : current;
    }

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

    // AVALONIA: WPF PreviewMouseLeftButtonDown -> PointerPressed (PointerPressedEventArgs.ClickCount mirrors MouseButtonEventArgs.ClickCount).
    private void CardControl_PointerPressed(object? sender, PointerPressedEventArgs e)
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
            Log.Instance.Trace($"Sensor details toggled: {(_detailsExpanded ? true : false)}.");
    }

    private void ShowDetailsWindow()
    {
        if (_detailsWindow is { IsVisible: true })
        {
            _detailsWindow.Activate();
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        var window = new SensorDetailsWindow();
        window.Closed += SensorDetailsWindow_Closed;

        _detailsWindow = window;
        window.Show(owner);
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
        FindNameCached(name) is Control element && element.IsVisible;

    private async Task RefreshDetailedValuesAsync()
    {
        if (!CanShowSensorDetails || !_sensorRuntimeAvailable)
            return;

        try
        {
            var data = await _controller.GetDataAsync(true);
            await Dispatcher.UIThread.InvokeAsync(() => UpdateValues(data, completesInitialLoad: true, recordTrendHistory: true));
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

    /// <summary>
    /// Combined bar+value metric: forwards range and text to a <see cref="MarqueeMetricBar"/>.
    /// </summary>
    private static void UpdateValue(MarqueeMetricBar metric, double max, double value, string text, string? toolTipText = null)
    {
        if (value < 0)
        {
            metric.Minimum = 0;
            metric.Maximum = 1;
            metric.Value = 0;
            metric.Text = "-";
            ToolTip.SetTip(metric, null);
            return;
        }

        if (max < 0)
            max = Math.Max(value, 1);

        metric.Minimum = 0;
        metric.Maximum = max;
        metric.Value = value;
        metric.Text = text;
        ToolTip.SetTip(metric, toolTipText is null ? null : string.Format(Resource.SensorsControl_Maximum, toolTipText));
    }

    private void SetSensorSectionsVisible(bool visible)
    {
        if (visible)
        {
            // Sensor availability changes must not override the user's section selection.
            ApplySensorSectionConfiguration();
        }
        else
        {
            SetVisibility("_cpuSection", false);
            SetVisibility("_batterySectionColumn", false);
            SetVisibility("_gpuSection", false);
            SetVisibility("_skeletonCpuSection", false);
            SetVisibility("_skeletonBatterySection", false);
            SetVisibility("_skeletonGpuSection", false);
            CollapseDetailPanels();
        }

        IsVisible = true;
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
        _ = Dispatcher.UIThread.InvokeAsync(HideSkeletonOverlay);
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

            await Dispatcher.UIThread.InvokeAsync(() =>
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
                UpdateDetailText("_cpuTempRange", FormatTemperatureRangeText(_cpuTemperatureMetric?.Text, _cpuTempRange.Text));
                UpdateDetailText("_cpuVoltageRange", FormatFallbackRangeText(_cpuVoltage.Text, _cpuVoltageRange.Text));
                UpdateDetailText("_gpuTempRange", FormatTemperatureRangeText(_gpuTemperatureMetric?.Text, _gpuTempRange.Text));
                UpdateDetailText("_gpuVoltageRange", FormatFallbackRangeText(_gpuVoltage.Text, _gpuVoltageRange.Text));
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Extended sensor detail refresh failed.", ex);
        }
    }

}
}
