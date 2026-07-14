using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Extensions;

namespace UniversalDeviceToolkit.WPF.Windows.Osd;

public abstract class OsdWindowBase : Window
{
    #region Threshold Constants

    protected int _uiUpdateThrottleMs = 0;
    protected const double MAX_FRAME_TIME_MS = 10.0;
    protected const long FRAMETIME_TIMEOUT_TICKS = 2 * 10_000_000;

    protected Brush _categoryBrush = Brushes.White;
    protected Brush _labelBrush = Brushes.White;
    protected Brush _valueBrush = Brushes.White;
    protected Brush _warningBrush = Brushes.Goldenrod;
    protected Brush _criticalBrush = Brushes.Red;
    protected Brush _separatorBrush = Brushes.Gray;

    #endregion

    #region Services

    protected readonly OsdSettings _OsdSettings = IoCContainer.Resolve<OsdSettings>();
    protected readonly SensorsController _controller = IoCContainer.Resolve<SensorsController>();
    protected readonly SensorsGroupController _sensorsGroupControllers = IoCContainer.Resolve<SensorsGroupController>();
    protected readonly FpsSensorController _fpsController = IoCContainer.Resolve<FpsSensorController>();
    protected readonly HardwareSensorSettings _hardwareSensorSettings = IoCContainer.Resolve<HardwareSensorSettings>();
    protected readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();

    #endregion

    #region State

    private static readonly string SuffixRpm = $" {Resource.RPM}";
    private static readonly string SuffixGb = $" {Resource.GB}";
    private static readonly string SuffixPercent = Resource.Percent;
    private static readonly string SuffixFahrenheit = Resource.Fahrenheit;
    private static readonly string SuffixCelsius = Resource.Celsius;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    protected readonly StringBuilder _stringBuilder = new(64);

    protected DateTime _lastUpdate = DateTime.MinValue;
    private long _lastValidFpsTick;
    private long _lastFpsUiUpdateTick;

    private CancellationTokenSource? _cts;
    protected bool _positionSet;
    private volatile bool _fpsMonitoringStarted;
    private bool _hasLenovoController;
    private bool _theRingErrorLogged;

    protected HashSet<OsdItem> _activeItems = [];
    protected Dictionary<OsdItem, FrameworkElement> _itemsMap = [];
    protected Dictionary<FrameworkElement, (List<OsdItem> Items, FrameworkElement? Separator)> _measurementGroups = [];

    #endregion

    #region Initialization

    protected void InitOsd()
    {
        _activeItems = new HashSet<OsdItem>(_OsdSettings.Store.Items);
        ShowInTaskbar = false;

        IsVisibleChanged += OnVisibilityChanged;
        SourceInitialized += OnSourceInitialized;
        Closed += OnWindowClosed;
        Loaded += OnLoaded;
        ContentRendered += OnContentRendered;
        LocationChanged += OnLocationChanged;
        MouseLeftButtonDown += OnMouseLeftButtonDown;

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        _ = InitializeComponentSpecifics();
        SubscribeEvents();
        _fpsController.FpsDataUpdated += OnFpsDataUpdated;

        ApplyAppearanceSettings();
        UpdateMeasurementControlsVisibility();
    }

    private async Task InitializeComponentSpecifics()
    {
        try
        {
            var mi = await Compatibility.GetMachineInformationAsync();
            if (mi.Properties.IsAmdDevice)
            {
                OnAmdDeviceDetected();
            }

            _hasLenovoController = await _controller.IsSupportedAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error initializing OSD component specifics: {ex.Message}");
        }
    }

    protected abstract void OnAmdDeviceDetected();

    private void SubscribeEvents()
    {
        MessagingCenter.Subscribe<OsdElementChangedMessage>(this, (message) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (App.Current.OsdWindow == null) return;

                var newItemsSet = new HashSet<OsdItem>(message.Items);
                if (_activeItems.SetEquals(newItemsSet)) return;

                _activeItems = newItemsSet;
                UpdateMeasurementControlsVisibility();
            });
        });

        MessagingCenter.Subscribe<OsdAppearanceChangedMessage>(this, _ =>
        {
            Dispatcher.BeginInvoke(ApplyAppearanceSettings);
        });
    }

    #endregion

    #region Window Events

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        this.SetClickThrough(_OsdSettings.Store.IsLocked);

        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.RemoveHook(WindowExtensions.WndProcHook);
            source.AddHook(WindowExtensions.WndProcHook);
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_OsdSettings.Store.IsLocked && e.ChangedButton == MouseButton.Left)
        {
            DragMove();

            var screen = WpfScreenHelper.Screen.FromWindow(this);
            if (screen != null)
            {
                var workArea = screen.WpfWorkingArea;

                double snapThreshold = _OsdSettings.Store.SnapThreshold;

                double left = this.Left;
                double top = this.Top;
                double width = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
                double height = this.ActualHeight > 0 ? this.ActualHeight : this.Height;

                if (Math.Abs(left - workArea.Left) < snapThreshold)
                {
                    left = workArea.Left;
                }
                else if (Math.Abs(workArea.Right - (left + width)) < snapThreshold)
                {
                    left = workArea.Right - width;
                }

                if (Math.Abs(top - workArea.Top) < snapThreshold)
                {
                    top = workArea.Top;
                }
                else if (Math.Abs(workArea.Bottom - (top + height)) < snapThreshold)
                {
                    top = workArea.Bottom - height;
                }

                if (left < workArea.Left) left = workArea.Left;
                if (left + width > workArea.Right) left = workArea.Right - width;

                if (top < workArea.Top) top = workArea.Top;
                if (top + height > workArea.Bottom) top = workArea.Bottom - height;

                this.Left = left;
                this.Top = top;

                _OsdSettings.SynchronizeStore();
            }
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => Dispatcher.BeginInvoke(new Action(SetWindowPosition), DispatcherPriority.Loaded);

    private void OnContentRendered(object? sender, EventArgs e)
    {
        if (!_positionSet)
        {
            Dispatcher.BeginInvoke(new Action(SetWindowPosition), DispatcherPriority.Render);
        }
    }

    protected virtual void SetWindowPosition()
    {
        if (SavedPositionX.HasValue && SavedPositionY.HasValue)
        {
            var savedX = SavedPositionX.Value;
            var savedY = SavedPositionY.Value;

            if (IsPositionOnScreen(savedX, savedY))
            {
                Left = savedX;
                Top = savedY;
                _positionSet = true;
                return;
            }

            SavedPositionX = null;
            SavedPositionY = null;
            _OsdSettings.SynchronizeStore();
        }

        SetDefaultWindowPosition();
    }

    protected abstract void SetDefaultWindowPosition();
    protected abstract double? SavedPositionX { get; set; }
    protected abstract double? SavedPositionY { get; set; }

    public void RecalculatePosition()
    {
        SavedPositionX = null;
        SavedPositionY = null;
        _OsdSettings.SynchronizeStore();

        SetDefaultWindowPosition();
    }

    private const int MONITOR_DEFAULTTONULL = 0;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private static bool IsPositionOnScreen(double x, double y)
    {
        var pt = new POINT { X = (int)x, Y = (int)y };
        return MonitorFromPoint(pt, MONITOR_DEFAULTTONULL) != IntPtr.Zero;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (!IsLoaded) return;

        SavedPositionX = Left;
        SavedPositionY = Top;
        _OsdSettings.SynchronizeStore();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsPositionOnScreen(Left, Top))
            {
                SetDefaultWindowPosition();
            }
        });
    }

    private async void OnVisibilityChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        try
        {
            if (IsVisible)
            {
                _sensorsGroupControllers.ShowAverageCpuFrequency = _hardwareSensorSettings.Store.ShowCpuAverageFrequency;

                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                await CheckAndUpdateFpsMonitoring();
                UpdateMeasurementControlsVisibility();

                _sensorsGroupControllers.Start(this, TimeSpan.FromSeconds(_OsdSettings.Store.OsdRefreshInterval));

                await TheRing(_cts.Token);
            }
            else
            {
                _cts?.Cancel();
                _sensorsGroupControllers.Stop(this);
                await CheckAndUpdateFpsMonitoring();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(OnVisibilityChanged)}.", ex);
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        SavedPositionX = Left;
        SavedPositionY = Top;
        _OsdSettings.SynchronizeStore();

        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        _cts?.Cancel();
        _cts?.Dispose();
        _refreshLock.Dispose();

        _fpsController.FpsDataUpdated -= OnFpsDataUpdated;
        _fpsController.Dispose();

        MessagingCenter.Unsubscribe(this);

        App.Current.OsdWindow = null;
    }

    protected virtual void ApplyAppearanceSettings()
    {
        var converter = new BrushConverter();

        _categoryBrush = (Brush)converter.ConvertFromString(_OsdSettings.Store.CategoryColor)!;
        _labelBrush = (Brush)converter.ConvertFromString(_OsdSettings.Store.LabelColor)!;
        _valueBrush = (Brush)converter.ConvertFromString(_OsdSettings.Store.ValueColor)!;
        _warningBrush = (Brush)converter.ConvertFromString(_OsdSettings.Store.WarningColor)!;
        _criticalBrush = (Brush)converter.ConvertFromString(_OsdSettings.Store.CriticalColor)!;
        _separatorBrush = (Brush)converter.ConvertFromString(_OsdSettings.Store.SeparatorColor)!;

        this.SetClickThrough(_OsdSettings.Store.IsLocked);

        if (!SavedPositionX.HasValue || !SavedPositionY.HasValue)
        {
            SetDefaultWindowPosition();
        }
    }

    protected void ApplyCornerRadius(Border border)
    {
        if (border != null)
        {
            border.CornerRadius = new CornerRadius(
                _OsdSettings.Store.CornerRadiusTop,
                _OsdSettings.Store.CornerRadiusTop,
                _OsdSettings.Store.CornerRadiusBottom,
                _OsdSettings.Store.CornerRadiusBottom);
        }
    }

    #endregion

    #region Visibility

    protected void UpdateMeasurementControlsVisibility()
    {
        bool isHybrid = _sensorsGroupControllers.IsHybrid;

        foreach (var (item, element) in _itemsMap)
        {
            bool shouldShow = _activeItems.Contains(item);

            if (isHybrid)
            {
                if (item == OsdItem.CpuFrequency) shouldShow = false;
            }
            else
            {
                if (item is OsdItem.CpuPCoreFrequency or OsdItem.CpuECoreFrequency)
                    shouldShow = false;
            }

            element.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
            OnItemVisibilityChanged(element, shouldShow);
        }

        var visibleGroups = new List<FrameworkElement>();

        foreach (var (groupPanel, (items, _)) in _measurementGroups)
        {
            bool isGroupActive = items.Any(item =>
            {
                if (!_activeItems.Contains(item)) return false;
                if (isHybrid && item == OsdItem.CpuFrequency) return false;
                if (!isHybrid && item is OsdItem.CpuPCoreFrequency or OsdItem.CpuECoreFrequency) return false;
                return true;
            });

            groupPanel.Visibility = isGroupActive ? Visibility.Visible : Visibility.Collapsed;
            if (isGroupActive) visibleGroups.Add(groupPanel);
        }

        foreach (var (groupPanel, (_, separator)) in _measurementGroups)
        {
            if (separator == null) continue;

            int index = visibleGroups.IndexOf(groupPanel);
            separator.Visibility = (index >= 0 && index < visibleGroups.Count - 1)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        _ = CheckAndUpdateFpsMonitoring();
        this.EscalateZBand();
    }

    protected virtual void OnItemVisibilityChanged(FrameworkElement element, bool visible) { }

    #endregion

    #region UI Helpers

    protected void UpdateTextBlock(TextBlock tb, double value, string format,
        double warningThreshold = double.MaxValue, double criticalThreshold = double.MaxValue)
    {
        if (tb.Visibility != Visibility.Visible) return;

        string text;
        Brush foreground = _valueBrush;

        if (double.IsNaN(value) || value < 0)
        {
            text = "-";
        }
        else
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendFormat(format, value);
            text = _stringBuilder.ToString();

            if (warningThreshold != double.MaxValue)
                foreground = SeverityBrush(value, warningThreshold, criticalThreshold);
        }

        SetTextIfChanged(tb, text);
        SetForegroundIfChanged(tb, foreground);
    }

    protected void UpdateTextBlock(TextBlock tb, int value, string? suffix = null)
    {
        if (suffix == null)
            suffix = SuffixRpm;
        if (tb.Visibility != Visibility.Visible) return;
        SetTextIfChanged(tb, value < 0 ? "-" : string.Concat(value, suffix));
        SetForegroundIfChanged(tb, _valueBrush);
    }

    protected Brush SeverityBrush(double value, double warningThreshold, double criticalThreshold)
    {
        if (value >= criticalThreshold) return _criticalBrush;
        return value >= warningThreshold ? _warningBrush : _valueBrush;
    }

    protected string GetMemoryDisplayText(double usage, double used, double total)
    {
        if (_hardwareSensorSettings.Store.DisplayMemoryInGigabytes)
        {
            if (used >= 0 && total > 0) return string.Concat(used.ToString("F1"), "/", total.ToString("F1"), SuffixGb);
            if (used >= 0) return string.Concat(used.ToString("F1"), SuffixGb);
            return "-";
        }

        return usage >= 0 ? string.Concat(usage.ToString("F0"), SuffixPercent) : "-";
    }

    protected string GetMemoryDisplayText(SensorSnapshot data) => GetMemoryDisplayText(data.MemUsage, data.MemUsed, data.MemTotal);

    protected string GetGpuVramDisplayText(SensorSnapshot data) => GetMemoryDisplayText(data.GpuVramUsage, data.GpuVramUsed, data.GpuVramTotal);

    protected string GetTemperatureFormat(double rawCelsius)
    {
        if (double.IsNaN(rawCelsius) || rawCelsius < 0) return "-";

        if (_applicationSettings.Store.TemperatureUnit == TemperatureUnit.F)
        {
            var fahrenheit = rawCelsius * 9.0 / 5.0 + 32.0;
            return string.Concat(fahrenheit.ToString("F0"), SuffixFahrenheit);
        }

        return string.Concat(rawCelsius.ToString("F0"), SuffixCelsius);
    }

    protected void UpdateTemperatureTextBlock(TextBlock tb, double rawCelsius,
        double warningThreshold = double.MaxValue, double criticalThreshold = double.MaxValue)
    {
        if (tb.Visibility != Visibility.Visible) return;

        var text = GetTemperatureFormat(rawCelsius);
        var foreground = _valueBrush;

        if (warningThreshold != double.MaxValue && !double.IsNaN(rawCelsius) && rawCelsius >= 0)
            foreground = SeverityBrush(rawCelsius, warningThreshold, criticalThreshold);

        SetTextIfChanged(tb, text);
        SetForegroundIfChanged(tb, foreground);
    }

    protected static void SetTextIfChanged(TextBlock tb, string text)
    {
        if (!string.Equals(tb.Text, text, StringComparison.Ordinal))
            tb.Text = text;
    }

    protected static void SetForegroundIfChanged(TextBlock tb, Brush brush)
    {
        if (!ReferenceEquals(tb.Foreground, brush))
            tb.Foreground = brush;
    }

    #endregion

    #region FPS Monitoring

    private async Task CheckAndUpdateFpsMonitoring()
    {
        try
        {
            bool shouldMonitor = IsVisible && ShouldMonitorFps();

            switch (shouldMonitor)
            {
                case true when !_fpsMonitoringStarted:
                    _fpsMonitoringStarted = true;
                    await StartFpsMonitoringAsync();
                    break;
                case false when _fpsMonitoringStarted:
                    _fpsMonitoringStarted = false;
                    StopFpsMonitoring();
                    break;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error checking/updating FPS monitoring: {ex.Message}");
        }
    }

    private bool ShouldMonitorFps() =>
        _activeItems.Contains(OsdItem.Fps) ||
        _activeItems.Contains(OsdItem.LowFps) ||
        _activeItems.Contains(OsdItem.FrameTime);

    private async Task StartFpsMonitoringAsync()
    {
        try { await _fpsController.StartMonitoringAsync(); }
        catch (Exception ex) { Log.Instance.Trace($"Failed to start FPS monitoring", ex); }
    }

    private void StopFpsMonitoring()
    {
        try { _fpsController.StopMonitoring(); }
        catch (Exception ex) { Log.Instance.Trace($"Failed to stop FPS monitoring", ex); }
    }

    private void OnFpsDataUpdated(object? sender, FpsSensorController.FpsData fpsData)
    {
        if (!_fpsMonitoringStarted) return;
        if (string.IsNullOrWhiteSpace(fpsData.Fps)) return;

        var valueBrush = _valueBrush;
        var criticalBrush = _criticalBrush;
        if (valueBrush is null || criticalBrush is null) return;

        long currentTick = DateTime.UtcNow.Ticks;

        int.TryParse(fpsData.Fps?.Trim(), out var fpsVal);
        int.TryParse(fpsData.LowFps?.Trim(), out var lowVal);
        double.TryParse(fpsData.FrameTime?.Trim(), out var ftVal);

        bool isSampleValid = fpsVal > 0;

        string? fpsText = null, lowText = null, ftText = null;
        Brush? fpsBrush = null, lowBrush = null, ftBrush = null;

        if (isSampleValid)
        {
            long elapsedTicks = currentTick - _lastFpsUiUpdateTick;
            var intervalTicks = TimeSpan.FromSeconds(_OsdSettings.Store.OsdRefreshInterval).Ticks;
            if (elapsedTicks < intervalTicks) return;

            _lastFpsUiUpdateTick = currentTick;
            _lastValidFpsTick = currentTick;

            const string dash = "-";

            fpsText = fpsVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            fpsBrush = (fpsVal < _OsdSettings.Store.FpsThresholdCritical) ? criticalBrush : valueBrush;

            lowText = (lowVal > 0) ? lowVal.ToString(System.Globalization.CultureInfo.InvariantCulture) : dash;
            lowBrush = (lowVal > 0 && (fpsVal - lowVal) >= _OsdSettings.Store.LowFpsDeltaThreshold) ? criticalBrush : valueBrush;

            if (ftVal > 0.1)
            {
                ftText = $"{ftVal,5:F1}ms";
                ftBrush = (ftVal > MAX_FRAME_TIME_MS) ? criticalBrush : valueBrush;
            }
            else
            {
                ftText = dash;
                ftBrush = valueBrush;
            }
        }
        else
        {
            if (currentTick - _lastValidFpsTick > FRAMETIME_TIMEOUT_TICKS)
            {
                const string dash = "-";
                fpsText = dash; fpsBrush = valueBrush;
                lowText = dash; lowBrush = valueBrush;
                ftText = dash; ftBrush = valueBrush;
                _lastFpsUiUpdateTick = currentTick;
            }
            else
            {
                return;
            }
        }

        var displayData = new FpsDisplayData
        {
            FpsText = fpsText,
            FpsBrush = fpsBrush,
            LowFpsText = lowText,
            LowFpsBrush = lowBrush,
            FrameTimeText = ftText,
            FrameTimeBrush = ftBrush
        };

        Dispatcher.BeginInvoke(() => UpdateFpsDisplay(displayData), DispatcherPriority.Normal);
    }

    protected abstract void UpdateFpsDisplay(FpsDisplayData data);

    #endregion

    #region Main Loop & Data Refresh

    private async Task TheRing(CancellationToken token)
    {
        try
        {
            await _refreshLock.WaitAsync(-1, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            while (!token.IsCancellationRequested)
            {
                var loopStart = DateTime.UtcNow;
                try
                {
                    await RefreshSensorsDataAsync(token);
                    _theRingErrorLogged = false;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled && !_theRingErrorLogged)
                    {
                        Log.Instance.Trace($"Exception occurred when executing TheRing()", ex);
                        _theRingErrorLogged = true;
                    }

                    await Task.Delay(1000, token);
                }

                var elapsed = DateTime.UtcNow - loopStart;
                var delay = TimeSpan.FromSeconds(_OsdSettings.Store.OsdRefreshInterval) - elapsed;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, token);
                }
            }
        }
        finally
        {
            try { _refreshLock.Release(); }
            catch (ObjectDisposedException ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("RefreshLock already disposed during release", ex);
            }
        }
    }

    private async Task RefreshSensorsDataAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        if (_uiUpdateThrottleMs > 0 && (DateTime.UtcNow - _lastUpdate).TotalMilliseconds < _uiUpdateThrottleMs) return;

        _lastUpdate = DateTime.UtcNow;

        var cpuUsageTask = _sensorsGroupControllers.GetCpuUsageAsync();
        var cpuFreqTask = _sensorsGroupControllers.GetCpuCoreClockAsync();
        var cpuPFreqTask = _sensorsGroupControllers.GetCpuPCoreClockAsync();
        var cpuEFreqTask = _sensorsGroupControllers.GetCpuECoreClockAsync();
        var cpuPowerTask = _sensorsGroupControllers.GetCpuPowerAsync();
        var gpuUsageTask = _sensorsGroupControllers.GetGpuUsageAsync();
        var gpuFreqTask = _sensorsGroupControllers.GetGpuCoreClockAsync();
        var gpuTempTask = _sensorsGroupControllers.GetGpuTemperatureAsync();
        var gpuVramUsageTask = _sensorsGroupControllers.GetGpuVramUtilizationAsync();
        var gpuVramUsedTask = _sensorsGroupControllers.GetGpuVramUsedAsync();
        var gpuVramTotalTask = _sensorsGroupControllers.GetGpuVramTotalAsync();
        var gpuVramTempTask = _sensorsGroupControllers.GetGpuVramTemperatureAsync();
        var gpuPowerTask = _sensorsGroupControllers.GetGpuPowerAsync();
        var memUsageTask = _sensorsGroupControllers.GetMemoryUsageAsync();
        var memUsedTask = _sensorsGroupControllers.GetMemoryUsedAsync();
        var memTotalTask = _sensorsGroupControllers.GetMemoryTotalAsync();
        var memTempTask = _sensorsGroupControllers.GetHighestMemoryTemperatureAsync();
        var ssdTempsTask = _sensorsGroupControllers.GetSsdTemperaturesAsync();

        if (_hasLenovoController)
        {
            var mainData = await _controller.GetDataAsync();

            if (token.IsCancellationRequested) return;

            await Task.WhenAll(
                cpuUsageTask, cpuFreqTask, cpuPFreqTask, cpuEFreqTask, cpuPowerTask,
                gpuUsageTask, gpuFreqTask, gpuTempTask, gpuVramUsageTask, gpuVramUsedTask,
                gpuVramTotalTask, gpuVramTempTask, gpuPowerTask, memUsageTask, memUsedTask,
                memTotalTask, memTempTask, ssdTempsTask);

            var ssdTemps = await ssdTempsTask;

            var snapshot = new SensorSnapshot
            {
                CpuUsage = mainData.CPU.Utilization,
                CpuFrequency = await cpuFreqTask,
                CpuPClock = await cpuPFreqTask,
                CpuEClock = await cpuEFreqTask,
                CpuTemp = mainData.CPU.Temperature,
                CpuPower = await cpuPowerTask,
                CpuFanSpeed = mainData.CPU.FanSpeed,

                GpuUsage = mainData.GPU.Utilization,
                GpuFrequency = mainData.GPU.CoreClock,
                GpuTemp = mainData.GPU.Temperature,
                GpuVramUsage = await gpuVramUsageTask,
                GpuVramUsed = await gpuVramUsedTask,
                GpuVramTotal = await gpuVramTotalTask,
                GpuVramTemp = await gpuVramTempTask,
                GpuPower = await gpuPowerTask,
                GpuFanSpeed = mainData.GPU.FanSpeed,

                MemUsage = await memUsageTask,
                MemUsed = await memUsedTask,
                MemTotal = await memTotalTask,
                MemTemp = (float)await memTempTask,

                PchTemp = -1,
                PchFanSpeed = -1,

                Disk1Temp = ssdTemps.Item1,
                Disk2Temp = ssdTemps.Item2
            };

            await Dispatcher.BeginInvoke(() => UpdateSensorData(snapshot), DispatcherPriority.Normal).Task;
        }
        else
        {
            var cpuTempTask = _sensorsGroupControllers.GetCpuTemperatureAsync();

            await Task.WhenAll(
                cpuUsageTask, cpuFreqTask, cpuPFreqTask, cpuEFreqTask, cpuPowerTask, cpuTempTask,
                gpuUsageTask, gpuFreqTask, gpuTempTask, gpuVramUsageTask, gpuVramUsedTask,
                gpuVramTotalTask, gpuVramTempTask, gpuPowerTask, memUsageTask, memUsedTask,
                memTotalTask, memTempTask, ssdTempsTask);

            if (token.IsCancellationRequested) return;

            var ssdTemps = await ssdTempsTask;

            var snapshot = new SensorSnapshot
            {
                CpuUsage = await cpuUsageTask,
                CpuFrequency = await cpuFreqTask,
                CpuPClock = await cpuPFreqTask,
                CpuEClock = await cpuEFreqTask,
                CpuTemp = await cpuTempTask,
                CpuPower = await cpuPowerTask,
                CpuFanSpeed = -1,

                GpuUsage = await gpuUsageTask,
                GpuFrequency = await gpuFreqTask,
                GpuTemp = await gpuTempTask,
                GpuVramUsage = await gpuVramUsageTask,
                GpuVramUsed = await gpuVramUsedTask,
                GpuVramTotal = await gpuVramTotalTask,
                GpuVramTemp = await gpuVramTempTask,
                GpuPower = await gpuPowerTask,
                GpuFanSpeed = -1,

                MemUsage = await memUsageTask,
                MemUsed = await memUsedTask,
                MemTotal = await memTotalTask,
                MemTemp = (float)await memTempTask,

                PchTemp = -1,
                PchFanSpeed = -1,

                Disk1Temp = ssdTemps.Item1,
                Disk2Temp = ssdTemps.Item2
            };

            await Dispatcher.BeginInvoke(() => UpdateSensorData(snapshot), DispatcherPriority.Normal).Task;
        }
    }

    protected abstract void UpdateSensorData(SensorSnapshot data);

    #endregion
}
