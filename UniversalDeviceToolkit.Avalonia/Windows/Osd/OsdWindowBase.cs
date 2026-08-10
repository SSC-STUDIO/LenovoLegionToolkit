using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Win32;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Settings;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using WpfHardwareSensorSettings = UniversalDeviceToolkit.Avalonia.Settings.HardwareSensorSettings;

namespace UniversalDeviceToolkit.Avalonia.Windows.Osd;

public abstract class OsdWindowBase : Window
{
    #region Threshold Constants

    protected int _uiUpdateThrottleMs = 0;
    protected const double MAX_FRAME_TIME_MS = 10.0;
    protected const long FRAMETIME_TIMEOUT_TICKS = 2 * 10_000_000;

    protected Brush _categoryBrush = new SolidColorBrush(Colors.White);
    protected Brush _labelBrush = new SolidColorBrush(Colors.White);
    protected Brush _valueBrush = new SolidColorBrush(Colors.White);
    protected Brush _warningBrush = new SolidColorBrush(Colors.Goldenrod);
    protected Brush _criticalBrush = new SolidColorBrush(Colors.Red);
    protected Brush _separatorBrush = new SolidColorBrush(Colors.Gray);

    #endregion

    #region Services

    protected readonly OsdSettings _OsdSettings = IoCContainer.Resolve<OsdSettings>();
    protected readonly SensorsController _controller = IoCContainer.Resolve<SensorsController>();
    protected readonly SensorsGroupController _sensorsGroupControllers = IoCContainer.Resolve<SensorsGroupController>();
    protected readonly FpsSensorController _fpsController = IoCContainer.Resolve<FpsSensorController>();
    protected readonly WpfHardwareSensorSettings _hardwareSensorSettings = IoCContainer.Resolve<WpfHardwareSensorSettings>();
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
    protected Dictionary<OsdItem, Control> _itemsMap = [];
    protected Dictionary<Control, (List<OsdItem> Items, Control? Separator)> _measurementGroups = [];

    #endregion

    #region Native Window Proc Hook

    // Native wndproc subclassing replaces the WPF HwndSource.AddHook mechanism:
    // it keeps the overlay window's WS_EX_TOOLWINDOW/WS_EX_NOACTIVATE styles intact
    // and prevents activation when the window is moved.
    private const int GWLP_WNDPROC = -4;
    private const int WM_NCDESTROY = 0x0082;

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    private IntPtr _previousWndProc;
    private WndProcDelegate? _wndProcDelegate;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private void AttachWndProcHook()
    {
        if (_wndProcDelegate != null) return;
        if (TryGetPlatformHandle() is not { } handle || handle.Handle == IntPtr.Zero) return;

        _wndProcDelegate = WndProcBridge;
        var previous = SetWindowLongPtr(handle.Handle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
        if (previous == IntPtr.Zero)
        {
            _wndProcDelegate = null;
            return;
        }

        _previousWndProc = previous;
    }

    private IntPtr WndProcBridge(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        bool handled = false;
        var result = WindowExtensions.WndProcHook(hwnd, msg, wParam, lParam, ref handled);
        if (handled)
            return result;

        if (msg == WM_NCDESTROY)
        {
            _wndProcDelegate = null;
            _previousWndProc = IntPtr.Zero;
        }

        return _previousWndProc != IntPtr.Zero
            ? CallWindowProc(_previousWndProc, hwnd, (uint)msg, wParam, lParam)
            : IntPtr.Zero;
    }

    #endregion

    #region Initialization

    protected void InitOsd()
    {
        _activeItems = new HashSet<OsdItem>(_OsdSettings.Store.Items);
        ShowInTaskbar = false;

        PropertyChanged += OnVisibilityChanged;
        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
        Loaded += OnLoaded;
        PositionChanged += OnPositionChanged;
        PointerPressed += OnPointerPressed;

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
            Dispatcher.UIThread.Post(() =>
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
            Dispatcher.UIThread.Post(ApplyAppearanceSettings);
        });
    }

    #endregion

    #region Window Events

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        this.SetClickThrough(_OsdSettings.Store.IsLocked);
        AttachWndProcHook();

        if (!_positionSet)
        {
            Dispatcher.UIThread.Post(SetWindowPosition);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_OsdSettings.Store.IsLocked) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        BeginMoveDrag(e);

        var screen = this.Screens.ScreenFromWindow(this);
        if (screen != null)
        {
            var workArea = screen.WorkingArea.ToRect(screen.Scaling);

            double snapThreshold = _OsdSettings.Store.SnapThreshold;

            double left = this.Position.X;
            double top = this.Position.Y;
            double width = this.Bounds.Width > 0 ? this.Bounds.Width : this.Width;
            double height = this.Bounds.Height > 0 ? this.Bounds.Height : this.Height;

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

            this.Position = new PixelPoint((int)left, (int)top);

            _OsdSettings.SynchronizeStore();
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => Dispatcher.UIThread.Post(SetWindowPosition);

    protected virtual void SetWindowPosition()
    {
        if (SavedPositionX.HasValue && SavedPositionY.HasValue)
        {
            var savedX = SavedPositionX.Value;
            var savedY = SavedPositionY.Value;

            if (IsPositionOnScreen(savedX, savedY))
            {
                Position = new PixelPoint((int)savedX, (int)savedY);
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

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (!IsLoaded) return;

        SavedPositionX = Position.X;
        SavedPositionY = Position.Y;
        _OsdSettings.SynchronizeStore();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsPositionOnScreen(Position.X, Position.Y))
            {
                SetDefaultWindowPosition();
            }
        });
    }

    private async void OnVisibilityChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

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
        SavedPositionX = Position.X;
        SavedPositionY = Position.Y;
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
        _categoryBrush = (Brush)Brush.Parse(_OsdSettings.Store.CategoryColor);
        _labelBrush = (Brush)Brush.Parse(_OsdSettings.Store.LabelColor);
        _valueBrush = (Brush)Brush.Parse(_OsdSettings.Store.ValueColor);
        _warningBrush = (Brush)Brush.Parse(_OsdSettings.Store.WarningColor);
        _criticalBrush = (Brush)Brush.Parse(_OsdSettings.Store.CriticalColor);
        _separatorBrush = (Brush)Brush.Parse(_OsdSettings.Store.SeparatorColor);

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

            element.IsVisible = shouldShow ? true : false;
            OnItemVisibilityChanged(element, shouldShow);
        }

        var visibleGroups = new List<Control>();

        foreach (var (groupPanel, (items, _)) in _measurementGroups)
        {
            bool isGroupActive = items.Any(item =>
            {
                if (!_activeItems.Contains(item)) return false;
                if (isHybrid && item == OsdItem.CpuFrequency) return false;
                if (!isHybrid && item is OsdItem.CpuPCoreFrequency or OsdItem.CpuECoreFrequency) return false;
                return true;
            });

            groupPanel.IsVisible = isGroupActive ? true : false;
            if (isGroupActive) visibleGroups.Add(groupPanel);
        }

        foreach (var (groupPanel, (_, separator)) in _measurementGroups)
        {
            if (separator == null) continue;

            int index = visibleGroups.IndexOf(groupPanel);
            separator.IsVisible = (index >= 0 && index < visibleGroups.Count - 1)
                ? true
                : false;
        }

        _ = CheckAndUpdateFpsMonitoring();
        this.EscalateZBand();
    }

    protected virtual void OnItemVisibilityChanged(Control element, bool visible) { }

    #endregion

    #region UI Helpers

    protected void UpdateTextBlock(TextBlock tb, double value, string format,
        double warningThreshold = double.MaxValue, double criticalThreshold = double.MaxValue)
    {
        if (tb.IsVisible != true) return;

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
        if (tb.IsVisible != true) return;
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
        if (tb.IsVisible != true) return;

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

        Dispatcher.UIThread.Post(() => UpdateFpsDisplay(displayData));
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

            await Dispatcher.UIThread.InvokeAsync(() => UpdateSensorData(snapshot)).GetTask();
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

            await Dispatcher.UIThread.InvokeAsync(() => UpdateSensorData(snapshot)).GetTask();
        }
    }

    protected abstract void UpdateSensorData(SensorSnapshot data);

    #endregion
}
