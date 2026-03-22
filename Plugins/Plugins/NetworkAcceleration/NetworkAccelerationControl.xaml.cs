
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
    private static readonly string[] DataSizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    private readonly NetworkAccelerationPlugin _plugin;
    private readonly NetworkAccelerationTelemetryService _telemetryService = new();
    private readonly DispatcherTimer _telemetryTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };
    private readonly List<NetworkAccelerationTelemetrySnapshot> _telemetrySamples = [];
    private static CultureInfo Culture => NetworkAccelerationText.Culture;

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

        _downloadValueTextBlock = CreateMetricValueTextBlock(24);
        _uploadValueTextBlock = CreateMetricValueTextBlock(24);
        _peakValueTextBlock = CreateMetricValueTextBlock(24);
        _adapterValueTextBlock = CreateMetricValueTextBlock(18);
        _downloadTotalTextBlock = CreateMetricValueTextBlock(18);
        _uploadTotalTextBlock = CreateMetricValueTextBlock(18);
        _updatedValueTextBlock = CreateMetricValueTextBlock(18);
        _modeDescriptionTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("TextFillColorPrimaryBrush", "#FFF6F3EE")
        };

        var root = new StackPanel { Margin = new Thickness(20) };
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_FeatureRoot");

        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var heroCopy = new StackPanel { Margin = new Thickness(0, 0, 20, 0) };
        heroCopy.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.FeatureOverviewTitle,
            FontSize = 28,
            Foreground = FindBrush("TextFillColorPrimaryBrush", "#FFF6F3EE")
        });
        heroCopy.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            Text = NetworkAccelerationText.FeatureOverviewDescription,
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("TextFillColorSecondaryBrush", "#FFBDB8B0")
        });
        Grid.SetColumn(heroCopy, 0);
        heroGrid.Children.Add(heroCopy);

        var heroVisual = CreateTrafficHeroPanel();
        Grid.SetColumn(heroVisual, 1);
        heroGrid.Children.Add(heroVisual);

        root.Children.Add(new Border
        {
            Padding = new Thickness(24),
            CornerRadius = new CornerRadius(24),
            Background = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#FF2F3028"),
                (Color)ColorConverter.ConvertFromString("#FF383A31"),
                0),
            BorderBrush = FindBrush("ControlStrokeColorDefaultBrush", "#443C3B35"),
            BorderThickness = new Thickness(1),
            Child = heroGrid
        });

        var summaryGrid = new System.Windows.Controls.Primitives.UniformGrid
        {
            Margin = new Thickness(0, 16, 0, 0),
            Columns = 4
        };
        summaryGrid.Children.Add(CreateMetricCard("DL", NetworkAccelerationText.CurrentDownloadLabel, "#FF4F9CFF", _downloadValueTextBlock));
        summaryGrid.Children.Add(CreateMetricCard("UL", NetworkAccelerationText.CurrentUploadLabel, "#FF27C7A8", _uploadValueTextBlock));
        summaryGrid.Children.Add(CreateMetricCard("PK", NetworkAccelerationText.PeakTrafficLabel, "#FFFFA24C", _peakValueTextBlock));
        summaryGrid.Children.Add(CreateMetricCard("NET", NetworkAccelerationText.ActiveAdapterLabel, "#FFB78CFF", _adapterValueTextBlock));
        root.Children.Add(summaryGrid);

        var optimizeButton = new Button { Content = NetworkAccelerationText.RunQuickOptimizationButton, Width = 176, Height = 36 };
        AutomationProperties.SetAutomationId(optimizeButton, "NetworkAcceleration_QuickOptimizeButton");
        optimizeButton.Click += QuickOptimizeButton_Click;
        var resetButton = new Button { Content = NetworkAccelerationText.ResetNetworkStackButton, Width = 168, Height = 36, Margin = new Thickness(10, 0, 0, 0) };
        AutomationProperties.SetAutomationId(resetButton, "NetworkAcceleration_ResetStackButton");
        resetButton.Click += ResetStackButton_Click;
        var saveButton = new Button { Content = NetworkAccelerationText.SaveModeButton, Width = 128, Height = 36, Margin = new Thickness(10, 0, 0, 0) };
        AutomationProperties.SetAutomationId(saveButton, "NetworkAcceleration_SaveModeButton");
        saveButton.Click += SaveModeButton_Click;

        var contentGrid = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });

        var leftPanel = CreatePanel();
        leftPanel.Margin = new Thickness(0, 0, 8, 0);
        leftPanel.Child = new StackPanel
        {
            Children =
            {
                CreateSectionTitle(NetworkAccelerationText.PreferredModeTitle),
                new TextBlock
                {
                    Margin = new Thickness(0, 6, 0, 0),
                    Text = NetworkAccelerationText.PreferredModeDescription,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = FindBrush("TextFillColorSecondaryBrush", "#FFBDB8B0")
                },
                new Border
                {
                    Margin = new Thickness(0, 16, 0, 0),
                    Padding = new Thickness(16),
                    CornerRadius = new CornerRadius(18),
                    Background = FindBrush("ControlFillColorSecondaryBrush", "#FF47463F"),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            _modeComboBox,
                            _modeDescriptionTextBlock
                        }
                    }
                },
                new WrapPanel
                {
                    Margin = new Thickness(0, 16, 0, 0),
                    Children =
                    {
                        optimizeButton,
                        resetButton,
                        saveButton
                    }
                },
                new Border
                {
                    Margin = new Thickness(0, 16, 0, 0),
                    Padding = new Thickness(14),
                    CornerRadius = new CornerRadius(16),
                    Background = FindBrush("ControlFillColorSecondaryBrush", "#FF47463F"),
                    Child = new TextBlock
                    {
                        Text = NetworkAccelerationText.AdminHint,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = FindBrush("TextFillColorSecondaryBrush", "#FFBDB8B0")
                    }
                }
            }
        };
        contentGrid.Children.Add(leftPanel);

        var rightPanel = CreatePanel();
        rightPanel.Margin = new Thickness(8, 0, 0, 0);
        var rightStack = new StackPanel();
        rightStack.Children.Add(CreateSectionTitle(NetworkAccelerationText.LiveTelemetryTitle));
        rightStack.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Text = NetworkAccelerationText.LiveTelemetryDescription,
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("TextFillColorSecondaryBrush", "#FFBDB8B0")
        });
        rightStack.Children.Add(CreateMetricCard("RX", NetworkAccelerationText.DownloadTotalLabel, "#FF4F9CFF", _downloadTotalTextBlock, new Thickness(0, 16, 0, 0)));
        rightStack.Children.Add(CreateMetricCard("TX", NetworkAccelerationText.UploadTotalLabel, "#FF27C7A8", _uploadTotalTextBlock, new Thickness(0, 12, 0, 0)));
        rightStack.Children.Add(CreateMetricCard("UPD", NetworkAccelerationText.UpdatedLabel, "#FFFFA24C", _updatedValueTextBlock, new Thickness(0, 12, 0, 0)));
        rightPanel.Child = rightStack;
        Grid.SetColumn(rightPanel, 1);
        contentGrid.Children.Add(rightPanel);
        root.Children.Add(contentGrid);

        root.Children.Add(new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(20),
            Background = FindBrush("ControlFillColorDefaultBrush", "#FF34342D"),
            BorderBrush = FindBrush("ControlStrokeColorDefaultBrush", "#443C3B35"),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    CreateSectionTitle(NetworkAccelerationText.StatusCardTitle),
                    new Border
                    {
                        Margin = new Thickness(0, 12, 0, 0),
                        Padding = new Thickness(14),
                        CornerRadius = new CornerRadius(16),
                        Background = FindBrush("ControlFillColorSecondaryBrush", "#FF47463F"),
                        Child = _statusTextBlock
                    }
                }
            }
        });

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private Border CreatePanel()
    {
        return new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(22),
            Background = FindBrush("ControlFillColorDefaultBrush", "#FF34342D"),
            BorderBrush = FindBrush("ControlStrokeColorDefaultBrush", "#443C3B35"),
            BorderThickness = new Thickness(1)
        };
    }

    private TextBlock CreateMetricValueTextBlock(double fontSize)
    {
        return new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("TextFillColorPrimaryBrush", "#FFF6F3EE")
        };
    }

    private TextBlock CreateSectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 20,
            Foreground = FindBrush("TextFillColorPrimaryBrush", "#FFF6F3EE")
        };
    }

    private Border CreateMetricCard(string badgeText, string label, string accentHex, TextBlock valueText, Thickness? margin = null)
    {
        return new Border
        {
            Margin = margin ?? new Thickness(0, 0, 12, 0),
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(18),
            Background = FindBrush("ControlFillColorDefaultBrush", "#FF34342D"),
            BorderBrush = FindBrush("ControlStrokeColorDefaultBrush", "#443C3B35"),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            CreateBadge(badgeText, "#FFF8F6F1", accentHex),
                            new TextBlock
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Text = label,
                                Foreground = FindBrush("TextFillColorSecondaryBrush", "#FFBDB8B0")
                            }
                        }
                    },
                    valueText
                }
            }
        };
    }

    private Border CreateBadge(string text, string foregroundHex, string backgroundHex)
    {
        return new Border
        {
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(8, 3, 8, 3),
            CornerRadius = new CornerRadius(999),
            Background = CreateBrush(backgroundHex),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = CreateBrush(foregroundHex)
            }
        };
    }

    private Border CreateTrafficHeroPanel()
    {
        var canvas = new Canvas
        {
            Height = 150
        };

        var dlBrush = CreateBrush("#FF5CA9FF");
        var ulBrush = CreateBrush("#FF41D4BE");
        var glowBrush = CreateBrush("#29FFFFFF");

        var glow = new Ellipse
        {
            Width = 112,
            Height = 112,
            Fill = glowBrush
        };
        canvas.Children.Add(glow);
        Canvas.SetLeft(glow, 84);
        Canvas.SetTop(glow, 16);

        AddSignalNode(canvas, 26, 42, dlBrush);
        AddSignalNode(canvas, 86, 68, dlBrush);
        AddSignalNode(canvas, 146, 48, ulBrush);
        AddSignalNode(canvas, 208, 80, ulBrush);
        AddSignalNode(canvas, 270, 58, dlBrush);

        AddSignalLine(canvas, 44, 46, 90, 66, dlBrush);
        AddSignalLine(canvas, 104, 66, 150, 52, dlBrush);
        AddSignalLine(canvas, 164, 52, 212, 78, ulBrush);
        AddSignalLine(canvas, 226, 78, 274, 62, ulBrush);

        return new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(22),
            Background = FindBrush("ControlFillColorSecondaryBrush", "#FF47463F"),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = NetworkAccelerationText.PageTitle,
                        FontSize = 14,
                        Foreground = FindBrush("TextFillColorSecondaryBrush", "#FFBDB8B0")
                    },
                    canvas,
                    new TextBlock
                    {
                        Text = NetworkAccelerationText.LiveTelemetryTitle,
                        FontSize = 16,
                        Foreground = FindBrush("TextFillColorPrimaryBrush", "#FFF6F3EE")
                    },
                    new TextBlock
                    {
                        Margin = new Thickness(0, 6, 0, 0),
                        Text = NetworkAccelerationText.LiveTelemetryDescription,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = FindBrush("TextFillColorSecondaryBrush", "#FFBDB8B0")
                    }
                }
            }
        };
    }

    private void AddSignalLine(Canvas canvas, double x1, double y1, double x2, double y2, Brush stroke)
    {
        canvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = stroke,
            StrokeThickness = 4,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }

    private void AddSignalNode(Canvas canvas, double left, double top, Brush fill)
    {
        var node = new Ellipse
        {
            Width = 18,
            Height = 18,
            Fill = fill
        };
        canvas.Children.Add(node);
        Canvas.SetLeft(node, left);
        Canvas.SetTop(node, top);
    }

    private Brush FindBrush(string resourceKey, string fallbackHex)
    {
        if (TryFindResource(resourceKey) is Brush brush)
            return brush;

        return CreateBrush(fallbackHex);
    }

    private static Brush CreateBrush(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
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
            _updatedValueTextBlock.Text = snapshot.Timestamp.ToLocalTime().ToString("HH:mm:ss", Culture);
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

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = isError
                ? Wpf.Ui.Common.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
    }

    private static string FormatRate(double mbps)
    {
        return string.Format(Culture, NetworkAccelerationText.MbpsValueFormat, mbps);
    }

    private static string FormatPercent(double value)
    {
        return string.Format(Culture, "{0:0}%", value);
    }

    private static string FormatDataSize(long bytes)
    {
        double value = bytes;
        var suffixIndex = 0;

        while (value >= 1024 && suffixIndex < DataSizeSuffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return string.Format(Culture, "{0:0.#} {1}", value, DataSizeSuffixes[suffixIndex]);
    }
}
