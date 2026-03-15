
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public partial class NetworkAccelerationControl : UserControl
{
    private const int MaxTelemetrySamples = 24;

    private readonly NetworkAccelerationPlugin _plugin;
    private readonly NetworkAccelerationTelemetryService _telemetryService = new();
    private readonly DispatcherTimer _telemetryTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };
    private readonly List<NetworkAccelerationTelemetrySnapshot> _telemetrySamples = [];

    public NetworkAccelerationControl(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
        _telemetryTimer.Tick += TelemetryTimer_Tick;

        TryInitializeComponent();
        LoadCurrentMode();
        UpdateModeDescription();
        var initialSnapshot = new NetworkAccelerationTelemetrySnapshot(DateTimeOffset.Now, NetworkAccelerationText.NoActiveAdapter, 0, 0, 0, 0);
        UpdateTelemetrySummary(initialSnapshot);
        UpdateAnalytics(initialSnapshot);
        SetStatus(NetworkAccelerationText.MonitoringStatus, false);
    }

    private void TryInitializeComponent()
    {
        try
        {
            InitializeComponent();
        }
        catch
        {
            BuildFallbackUi();
        }
    }

    private void BuildFallbackUi()
    {
        _modeComboBox = new ComboBox();
        AutomationProperties.SetAutomationId(_modeComboBox, "NetworkAcceleration_ModeComboBox");
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeBalanced, Tag = "Balanced" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeGaming, Tag = "Gaming" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeStreaming, Tag = "Streaming" });
        _modeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;

        _statusTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "NetworkAcceleration_StatusText");

        var root = new StackPanel { Margin = new Thickness(16) };
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_FeatureRoot");
        root.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.PageTitle,
            FontSize = 24,
            FontWeight = FontWeights.SemiBold
        });
        root.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.LiveTelemetryDescription,
            Margin = new Thickness(0, 8, 0, 16),
            TextWrapping = TextWrapping.Wrap
        });
        root.Children.Add(_modeComboBox);

        var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        var optimizeButton = new Button { Content = NetworkAccelerationText.RunQuickOptimizationButton, Width = 160 };
        AutomationProperties.SetAutomationId(optimizeButton, "NetworkAcceleration_QuickOptimizeButton");
        optimizeButton.Click += QuickOptimizeButton_Click;
        var resetButton = new Button { Content = NetworkAccelerationText.ResetNetworkStackButton, Width = 150, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(resetButton, "NetworkAcceleration_ResetStackButton");
        resetButton.Click += ResetStackButton_Click;
        var saveButton = new Button { Content = NetworkAccelerationText.SaveModeButton, Width = 120, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(saveButton, "NetworkAcceleration_SaveModeButton");
        saveButton.Click += SaveModeButton_Click;
        actions.Children.Add(optimizeButton);
        actions.Children.Add(resetButton);
        actions.Children.Add(saveButton);

        root.Children.Add(actions);
        root.Children.Add(new Border
        {
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.LightGray,
            Child = _statusTextBlock
        });

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshTelemetry();
        _telemetryTimer.Start();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _telemetryTimer.Stop();
        _telemetryService.Dispose();
    }

    private void TelemetryTimer_Tick(object? sender, EventArgs e)
    {
        RefreshTelemetry();
    }

    private void LoadCurrentMode()
    {
        if (_modeComboBox is null)
            return;

        var modeValue = _plugin.Settings.PreferredMode.ToString();
        foreach (var item in _modeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag as string == modeValue)
            {
                _modeComboBox.SelectedItem = item;
                break;
            }
        }

        if (_modeComboBox.SelectedItem == null && _modeComboBox.Items.Count > 0)
            _modeComboBox.SelectedIndex = 0;
    }

    private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateModeDescription();
    }

    private void UpdateModeDescription()
    {
        if (_modeDescriptionTextBlock is null)
            return;

        var mode = ParseSelectedMode();
        _modeDescriptionTextBlock.Text = mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGamingDescription,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreamingDescription,
            _ => NetworkAccelerationText.ModeBalancedDescription
        };
    }

    private void RefreshTelemetry()
    {
        var snapshot = _telemetryService.Capture();
        _telemetrySamples.Add(snapshot);
        if (_telemetrySamples.Count > MaxTelemetrySamples)
            _telemetrySamples.RemoveAt(0);

        UpdateTelemetrySummary(snapshot);
        UpdateAnalytics(snapshot);
        UpdateChart();
        UpdateBurstChart();
    }

    private void UpdateTelemetrySummary(NetworkAccelerationTelemetrySnapshot snapshot)
    {
        if (_downloadValueTextBlock != null)
            _downloadValueTextBlock.Text = FormatRate(snapshot.DownloadMbps);

        if (_uploadValueTextBlock != null)
            _uploadValueTextBlock.Text = FormatRate(snapshot.UploadMbps);

        if (_peakValueTextBlock != null)
            _peakValueTextBlock.Text = FormatRate(_telemetrySamples.Count == 0
                ? 0
                : _telemetrySamples.Max(sample => Math.Max(sample.DownloadMbps, sample.UploadMbps)));

        if (_adapterValueTextBlock != null)
            _adapterValueTextBlock.Text = snapshot.InterfaceName;

        if (_downloadTotalTextBlock != null)
            _downloadTotalTextBlock.Text = FormatDataSize(snapshot.TotalReceivedBytes);

        if (_uploadTotalTextBlock != null)
            _uploadTotalTextBlock.Text = FormatDataSize(snapshot.TotalSentBytes);

        if (_updatedValueTextBlock != null)
            _updatedValueTextBlock.Text = snapshot.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentUICulture);
    }

    private void UpdateAnalytics(NetworkAccelerationTelemetrySnapshot snapshot)
    {
        var combinedNow = Math.Max(0, snapshot.DownloadMbps) + Math.Max(0, snapshot.UploadMbps);
        var downloadShare = combinedNow <= 0 ? 0 : (snapshot.DownloadMbps / combinedNow) * 100d;
        var uploadShare = combinedNow <= 0 ? 0 : (snapshot.UploadMbps / combinedNow) * 100d;
        var averageBurst = _telemetrySamples.Count == 0 ? 0 : _telemetrySamples.Average(sample => sample.DownloadMbps + sample.UploadMbps);
        var peakBurst = _telemetrySamples.Count == 0 ? 0 : _telemetrySamples.Max(sample => sample.DownloadMbps + sample.UploadMbps);

        if (_downloadShareValueTextBlock != null)
            _downloadShareValueTextBlock.Text = FormatPercent(downloadShare);

        if (_uploadShareValueTextBlock != null)
            _uploadShareValueTextBlock.Text = FormatPercent(uploadShare);

        if (_downloadShareBar != null)
            _downloadShareBar.Value = downloadShare;

        if (_uploadShareBar != null)
            _uploadShareBar.Value = uploadShare;

        if (_rollingAverageTextBlock != null)
            _rollingAverageTextBlock.Text = FormatRate(averageBurst);

        if (_burstPeakTextBlock != null)
            _burstPeakTextBlock.Text = FormatRate(peakBurst);
    }

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateChart();
    }

    private void BurstCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateBurstChart();
    }

    private void UpdateChart()
    {
        if (_chartCanvas is null ||
            _downloadLine is null ||
            _uploadLine is null ||
            _downloadFillPolygon is null ||
            _uploadFillPolygon is null ||
            _chartEmptyTextBlock is null ||
            _chartCeilingTextBlock is null ||
            _chartFloorTextBlock is null)
        {
            return;
        }

        var width = _chartCanvas.ActualWidth;
        var height = _chartCanvas.ActualHeight;
        if (width <= 1 || height <= 1)
            return;

        if (_telemetrySamples.Count < 2)
        {
            _downloadLine.Points = [];
            _uploadLine.Points = [];
            _downloadFillPolygon.Points = [];
            _uploadFillPolygon.Points = [];
            _chartEmptyTextBlock.Visibility = Visibility.Visible;
            _chartCeilingTextBlock.Text = FormatRate(0);
            _chartFloorTextBlock.Text = FormatRate(0);
            return;
        }

        _chartEmptyTextBlock.Visibility = Visibility.Collapsed;

        var ceiling = Math.Max(1d, _telemetrySamples.Max(sample => Math.Max(sample.DownloadMbps, sample.UploadMbps)));
        _chartCeilingTextBlock.Text = FormatRate(ceiling);
        _chartFloorTextBlock.Text = FormatRate(0);

        _downloadLine.Points = BuildLinePoints(width, height, ceiling, sample => sample.DownloadMbps);
        _uploadLine.Points = BuildLinePoints(width, height, ceiling, sample => sample.UploadMbps);
        _downloadFillPolygon.Points = BuildFillPoints(_downloadLine.Points, width, height);
        _uploadFillPolygon.Points = BuildFillPoints(_uploadLine.Points, width, height);
    }

    private void UpdateBurstChart()
    {
        if (_burstCanvas is null ||
            _burstLine is null ||
            _burstFillPolygon is null ||
            _burstEmptyTextBlock is null)
        {
            return;
        }

        var width = _burstCanvas.ActualWidth;
        var height = _burstCanvas.ActualHeight;
        if (width <= 1 || height <= 1)
            return;

        if (_telemetrySamples.Count < 2)
        {
            _burstLine.Points = [];
            _burstFillPolygon.Points = [];
            _burstEmptyTextBlock.Visibility = Visibility.Visible;
            return;
        }

        _burstEmptyTextBlock.Visibility = Visibility.Collapsed;
        var ceiling = Math.Max(1d, _telemetrySamples.Max(sample => sample.DownloadMbps + sample.UploadMbps));
        _burstLine.Points = BuildLinePoints(width, height, ceiling, sample => sample.DownloadMbps + sample.UploadMbps);
        _burstFillPolygon.Points = BuildFillPoints(_burstLine.Points, width, height);
    }

    private PointCollection BuildLinePoints(double width, double height, double ceiling, Func<NetworkAccelerationTelemetrySnapshot, double> selector)
    {
        var points = new PointCollection();
        var step = _telemetrySamples.Count <= 1 ? width : width / (_telemetrySamples.Count - 1d);

        for (var index = 0; index < _telemetrySamples.Count; index++)
        {
            var value = selector(_telemetrySamples[index]);
            var x = step * index;
            var y = height - ((Math.Clamp(value, 0, ceiling) / ceiling) * height);
            points.Add(new Point(x, y));
        }

        return points;
    }

    private static PointCollection BuildFillPoints(PointCollection linePoints, double width, double height)
    {
        if (linePoints.Count == 0)
            return [];

        var points = new PointCollection(linePoints);
        points.Add(new Point(width, height));
        points.Add(new Point(0, height));
        return points;
    }

    private async void QuickOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _plugin.RunQuickOptimizationAsync().ConfigureAwait(true);
        SetStatus(
            success ? NetworkAccelerationText.StatusQuickOptimizationCompleted : NetworkAccelerationText.StatusQuickOptimizationFailed,
            !success);
        RefreshTelemetry();
    }

    private async void ResetStackButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _plugin.ResetNetworkStackAsync().ConfigureAwait(true);
        SetStatus(
            success ? NetworkAccelerationText.StatusResetCompleted : NetworkAccelerationText.StatusResetFailed,
            !success);
        RefreshTelemetry();
    }

    private async void SaveModeButton_Click(object sender, RoutedEventArgs e)
    {
        var mode = ParseSelectedMode();
        if (mode is null)
        {
            SetStatus(NetworkAccelerationText.StatusSelectValidMode, true);
            return;
        }

        _plugin.SetPreferredMode(mode.Value);
        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        UpdateModeDescription();
        SetStatus(NetworkAccelerationText.StatusModeSaved, false);
    }

    private NetworkAccelerationMode? ParseSelectedMode()
    {
        if (_modeComboBox?.SelectedItem is ComboBoxItem combo &&
            combo.Tag is string modeText &&
            Enum.TryParse(modeText, true, out NetworkAccelerationMode parsed))
        {
            return parsed;
        }

        return null;
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is null)
            return;

        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC42B1C"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0F7B5A"));
    }

    private static string FormatRate(double mbps)
    {
        return string.Format(CultureInfo.CurrentUICulture, NetworkAccelerationText.MbpsValueFormat, mbps);
    }

    private static string FormatPercent(double value)
    {
        return string.Format(CultureInfo.CurrentUICulture, "{0:0}%", value);
    }

    private static string FormatDataSize(long bytes)
    {
        double value = bytes;
        var suffixes = new[] { "B", "KB", "MB", "GB", "TB" };
        var suffixIndex = 0;

        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return string.Format(CultureInfo.CurrentUICulture, "{0:0.#} {1}", value, suffixes[suffixIndex]);
    }
}
