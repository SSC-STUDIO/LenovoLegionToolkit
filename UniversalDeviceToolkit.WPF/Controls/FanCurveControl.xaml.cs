using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Controls;

public partial class FanCurveControl : UserControl
{
    private const string CelsiusUnit = "\u00B0C";
    private const string RpmUnit = "RPM";
    private const double MinimumColumnWidth = 24;
    private const int ValueTickCount = 5;

    private readonly List<Slider> _sliders = [];
    private readonly List<TextBlock> _temperatureLabels = [];
    private readonly InfoTooltip _customToolTip = new();
    private readonly DebounceDispatcher _debouncer = new();
    private DispatcherOperation? _pendingDrawOperation;

    private FanTableData[]? _tableData;
    private FanTable? _minimumFanTable;
    private int _layoutGeneration;

    public FanCurveControl()
    {
        InitializeComponent();

        MouseLeave += FanCurveControl_MouseLeave;
        Unloaded += FanCurveControl_Unloaded;
        SizeChanged += (_, _) => QueueDrawGraph();
        _plotContent.SizeChanged += (_, _) => QueueDrawGraph();
        _slidersGrid.SizeChanged += (_, _) => QueueDrawGraph();
        _xAxisLabelsCanvas.SizeChanged += (_, _) => QueueDrawGraph();
        Loaded += (_, _) => QueueDrawGraph();
    }

    private void FanCurveControl_MouseLeave(object sender, MouseEventArgs e)
    {
        _customToolTip.IsOpen = false;
    }

    private void FanCurveControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _layoutGeneration++;
        _pendingDrawOperation?.Abort();
        _pendingDrawOperation = null;
        _customToolTip.IsOpen = false;
        MouseLeave -= FanCurveControl_MouseLeave;
        foreach (var slider in _sliders)
        {
            slider.MouseMove -= Slider_MouseMove;
            slider.ValueChanged -= Slider_OnValueChanged;
        }
    }

    public void SetFanTableInfo(FanTableInfo fanTableInfo, FanTable minimumFanTable)
    {
        _layoutGeneration++;
        _pendingDrawOperation?.Abort();
        _pendingDrawOperation = null;

        foreach (var slider in _sliders)
        {
            slider.MouseMove -= Slider_MouseMove;
            slider.ValueChanged -= Slider_OnValueChanged;
        }

        _sliders.Clear();
        _slidersGrid.Children.Clear();
        _slidersGrid.ColumnDefinitions.Clear();
        ClearGraphLayers();

        var tableValues = fanTableInfo.Table.GetTable();
        for (var i = 0; i < tableValues.Length; i++)
        {
            _slidersGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
                MinWidth = MinimumColumnWidth,
            });

            var slider = GenerateSlider(i, 0, 10);
            // Suppress ValueChanged side-effects while seeding the firmware table.
            slider.ValueChanged -= Slider_OnValueChanged;
            slider.Value = tableValues[i];
            slider.ValueChanged += Slider_OnValueChanged;
            _sliders.Add(slider);
            _slidersGrid.Children.Add(slider);
        }

        _tableData = fanTableInfo.Data;
        _minimumFanTable = minimumFanTable;
        UpdateTemperatureAxisLabels();

        // Dynamic columns and slider templates must be arranged before the graph is drawn.
        QueueDrawGraph();
    }

    public FanTableInfo? GetFanTableInfo()
    {
        if (_tableData is null)
            return null;

        var fanTable = _sliders.Select(s => (ushort)s.Value).ToArray();
        return new(_tableData, new FanTable(fanTable));
    }

    private Slider GenerateSlider(int index, int minimum, int maximum)
    {
        var slider = new Slider
        {
            Orientation = Orientation.Vertical,
            IsSnapToTickEnabled = true,
            TickFrequency = 1,
            Maximum = maximum,
            Minimum = minimum,
            Tag = index,
            Style = (Style)FindResource("FanCurveSliderStyle"),
        };

        slider.MouseMove += Slider_MouseMove;
        slider.ValueChanged += Slider_OnValueChanged;

        // Each point owns one dynamic star-sized column. There is no artificial gutter column.
        Grid.SetColumn(slider, index);
        return slider;
    }

    private void Slider_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not Slider slider)
            return;

        if (slider.Template.FindName("PART_Track", slider) is not Track track)
            return;

        if (!track.Thumb.IsMouseOver || _tableData is null)
        {
            _customToolTip.IsOpen = false;
            return;
        }

        _customToolTip.Update(_tableData, (int)slider.Tag, (int)slider.Value - 1);
        _customToolTip.Placement = PlacementMode.Custom;
        _customToolTip.PlacementTarget = track.Thumb;
        _customToolTip.CustomPopupPlacementCallback = ToolTipCustomPopupPlacementCallback;
        _customToolTip.HorizontalOffset += -0.1;
        _customToolTip.HorizontalOffset += +0.1;
        _customToolTip.IsOpen = true;
    }

    private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // The graph is a direct representation of the controls, so it must redraw with every
        // value change instead of waiting for the debounced firmware validation below.
        QueueDrawGraph();

        _debouncer.Throttle(100, () =>
        {
            if (_sliders.Count < 10)
                return;

            if (sender is not Slider currentSlider)
                return;

            if (currentSlider is { IsKeyboardFocusWithin: false, IsMouseCaptureWithin: false })
            {
                QueueDrawGraph();
                return;
            }

            if (_minimumFanTable.HasValue)
            {
                var index = (int)currentSlider.Tag;
                var minimum = _minimumFanTable.Value.GetTable();

                if (index >= 0 && index < minimum.Length && currentSlider.Value < minimum[index])
                {
                    currentSlider.Value = minimum[index];
                    return;
                }
            }

            VerifyValues(currentSlider);
            QueueDrawGraph();
        });
    }

    private static CustomPopupPlacement[] ToolTipCustomPopupPlacementCallback(Size size, Size targetSize, Point _)
    {
        return
        [
            new(new((targetSize.Width - size.Width) * 0.5, -targetSize.Height - size.Height + 8), PopupPrimaryAxis.Vertical)
        ];
    }

    private void VerifyValues(Slider currentSlider)
    {
        var currentIndex = _sliders.IndexOf(currentSlider);
        if (currentIndex < 0)
            return;

        var currentValue = currentSlider.Value;

        // Fan speed must be non-decreasing as temperature increases from left to right.
        for (var i = 0; i < currentIndex; i++)
        {
            if (_sliders[i].Value > currentValue)
                _sliders[i].Value = currentValue;
        }

        for (var i = currentIndex + 1; i < _sliders.Count; i++)
        {
            if (_sliders[i].Value < currentValue)
                _sliders[i].Value = currentValue;
        }
    }

    private void QueueDrawGraph()
    {
        if (!IsLoaded)
            return;

        var generation = ++_layoutGeneration;
        _pendingDrawOperation?.Abort();
        _pendingDrawOperation = Dispatcher.InvokeAsync(() => DrawGraph(generation), DispatcherPriority.Render);
    }

    private void ClearGraphLayers()
    {
        _gridCanvas.Children.Clear();
        _graphCanvas.Children.Clear();
    }

    private void DrawGraph(int generation)
    {
        if (generation != _layoutGeneration || !IsLoaded)
            return;

        UpdateLayout();

        if (generation != _layoutGeneration)
            return;

        if (_sliders.Count < 2 || _plotContent.ActualWidth < 2 || _plotContent.ActualHeight < 2)
        {
            ClearGraphLayers();
            return;
        }

        if (!TryGetValueRangeY(out var valueRange) || !TryGetValueBackedGraphPoints(valueRange, out var points))
        {
            // A render pass can happen before one of the dynamic Slider templates has
            // produced its Track/Thumb. Leave the layers empty until the next layout pass;
            // do not mix rendered points with estimated fallback points.
            ClearGraphLayers();
            _pendingDrawOperation = Dispatcher.InvokeAsync(() => DrawGraph(generation), DispatcherPriority.ContextIdle);
            return;
        }

        var lineBrush = ResolveLineBrush();
        var fillBrush = ResolveFillBrush(lineBrush);

        _gridCanvas.Children.Clear();
        _gridCanvas.Children.Add(new Path
        {
            Data = CreateGridGeometry(_plotContent.ActualWidth, valueRange.Top, valueRange.Bottom),
            Stroke = ResolveGridBrush(),
            StrokeThickness = 0.75,
            Opacity = 0.7,
            IsHitTestVisible = false,
        });

        var linePath = new Path
        {
            Data = new PathGeometry { Figures = new PathFigureCollection { CreatePolylineFigure(points) } },
            Stroke = lineBrush,
            StrokeThickness = 2.25,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false,
        };

        var fillPath = new Path
        {
            Data = new PathGeometry { Figures = new PathFigureCollection { CreateAreaFigure(points, valueRange.Bottom) } },
            Fill = fillBrush,
            IsHitTestVisible = false,
        };

        _graphCanvas.Children.Clear();
        _graphCanvas.Children.Add(fillPath);
        _graphCanvas.Children.Add(linePath);
        PositionTemperatureAxisLabels(points);
    }

    private bool TryGetValueBackedGraphPoints((double Top, double Bottom) valueRange, out Point[] points)
    {
        points = new Point[_sliders.Count];
        for (var i = 0; i < _sliders.Count; i++)
        {
            var slider = _sliders[i];
            if (slider.Template.FindName("PART_Track", slider) is not Track track
                || track.Thumb is not FrameworkElement thumb
                || track.ActualWidth <= 0
                || track.ActualHeight <= 0
                || thumb.ActualWidth <= 0
                || thumb.ActualHeight <= 0)
                return false;

            try
            {
                var x = thumb.TranslatePoint(new Point(thumb.ActualWidth / 2, thumb.ActualHeight / 2), _graphCanvas).X;
                var y = GetValueY(slider.Value, slider.Minimum, slider.Maximum, valueRange.Top, valueRange.Bottom);
                if (!IsFinite(x) || !IsFinite(y))
                    return false;

                points[i] = new Point(x, y);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetValueRangeY(out (double Top, double Bottom) valueRange)
    {
        valueRange = default;
        if (_sliders.Count > 0
            && _sliders[0].Template.FindName("PART_Track", _sliders[0]) is Track track
            && track.Thumb is FrameworkElement thumb
            && thumb.ActualHeight > 0
            && track.ActualHeight > 0)
        {
            try
            {
                var trackTop = track.TranslatePoint(new Point(0, 0), _gridCanvas).Y;
                var trackBottom = track.TranslatePoint(new Point(0, track.ActualHeight), _gridCanvas).Y;
                var radius = thumb.ActualHeight / 2;
                var top = trackTop + radius;
                var bottom = trackBottom - radius;
                if (IsFinite(top) && IsFinite(bottom) && bottom > top)
                {
                    valueRange = (top, bottom);
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
                // Wait for the next render pass while WPF is arranging the template.
            }
        }

        return false;
    }

    private static PathGeometry CreateGridGeometry(double width, double top, double bottom)
    {
        var geometry = new PathGeometry();
        for (var i = 0; i <= ValueTickCount; i++)
        {
            var y = top + (bottom - top) * i / ValueTickCount;
            var figure = new PathFigure
            {
                StartPoint = new Point(0, y),
                IsClosed = false,
                IsFilled = false,
            };
            figure.Segments.Add(new LineSegment(new Point(width, y), true));
            geometry.Figures.Add(figure);
        }

        return geometry;
    }

    internal static PathFigure CreatePolylineFigure(IReadOnlyList<Point> points)
    {
        var figure = new PathFigure { StartPoint = points[0], IsFilled = false, IsClosed = false };
        foreach (var segment in CreatePolylineSegments(points))
            figure.Segments.Add(segment);
        return figure;
    }

    internal static PathFigure CreateAreaFigure(IReadOnlyList<Point> points, double baselineY)
    {
        var figure = new PathFigure
        {
            StartPoint = new Point(points[0].X, baselineY),
            IsFilled = true,
            IsClosed = true,
        };

        foreach (var point in points)
            figure.Segments.Add(new LineSegment(point, true));
        figure.Segments.Add(new LineSegment(new Point(points[^1].X, baselineY), true));
        return figure;
    }

    internal static PathSegmentCollection CreatePolylineSegments(IReadOnlyList<Point> points)
    {
        var segments = new PathSegmentCollection();
        for (var i = 0; i < points.Count - 1; i++)
            segments.Add(new LineSegment(points[i + 1], true));

        return segments;
    }

    private void UpdateTemperatureAxisLabels()
    {
        _xAxisLabelsCanvas.Children.Clear();
        _temperatureLabels.Clear();

        var temperatures = _tableData?
            .Where(data => data.Temps.Length >= _sliders.Count)
            .Select(data => data.Temps)
            .FirstOrDefault();

        if (_sliders.Count == 0)
            return;

        for (var index = 0; index < _sliders.Count; index++)
        {
            ushort? temperature = temperatures is { Length: > 0 } && index < temperatures.Length
                ? temperatures[index]
                : null;
            var label = new TextBlock
            {
                Text = temperature is null or >= 127 ? "-" : $"{temperature}{CelsiusUnit}",
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
                ToolTip = temperature is null or >= 127 ? null : $"{temperature}{CelsiusUnit}",
            };
            _temperatureLabels.Add(label);
            _xAxisLabelsCanvas.Children.Add(label);
        }
    }

    private void PositionTemperatureAxisLabels(IReadOnlyList<Point> points)
    {
        if (points.Count != _temperatureLabels.Count || points.Count == 0 || _xAxisLabelsCanvas.ActualWidth <= 0)
            return;

        var labelWidth = Math.Clamp(_xAxisLabelsCanvas.ActualWidth / points.Count, 32, 56);
        var maximumLeft = Math.Max(0, _xAxisLabelsCanvas.ActualWidth - labelWidth);
        for (var index = 0; index < points.Count; index++)
        {
            var label = _temperatureLabels[index];
            label.Width = labelWidth;
            var x = _graphCanvas.TranslatePoint(points[index], _xAxisLabelsCanvas).X;
            Canvas.SetLeft(label, Math.Clamp(x - labelWidth / 2, 0, maximumLeft));
        }
    }

    internal static double GetValueY(double value, double minimum, double maximum, double top, double bottom)
    {
        if (maximum <= minimum || bottom <= top)
            return bottom;

        var ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);
        return bottom - ratio * (bottom - top);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static Brush ResolveLineBrush()
    {
        if (Application.Current?.Resources["ChartUtilizationBrush"] is Brush chart)
            return chart;
        if (Application.Current?.Resources["AccentFillColorDefaultBrush"] is Brush accent)
            return accent;
        if (Application.Current?.Resources["SystemAccentColorPrimaryBrush"] is Brush system)
            return system;
        return new SolidColorBrush(Color.FromRgb(0x4F, 0x9D, 0xF7));
    }

    private static Brush ResolveGridBrush()
    {
        if (Application.Current?.Resources["ChartGridlineBrush"] is Brush grid)
            return grid;
        if (Application.Current?.Resources["TextFillColorDisabledBrush"] is Brush disabled)
            return disabled;
        return new SolidColorBrush(Color.FromArgb(48, 128, 128, 128));
    }

    private static Brush ResolveFillBrush(Brush lineBrush)
    {
        var color = lineBrush is SolidColorBrush solid
            ? solid.Color
            : Color.FromRgb(0x4F, 0x9D, 0xF7);

        var fill = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(110, color.R, color.G, color.B), 0));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(24, color.R, color.G, color.B), 1));
        fill.Freeze();
        return fill;
    }

    private sealed class InfoTooltip : ToolTip
    {
        private readonly Grid _grid = new()
        {
            ColumnDefinitions =
            {
                new() { Width = GridLength.Auto },
                new() { Width = GridLength.Auto }
            },
            RowDefinitions =
            {
                new() { Height = GridLength.Auto },
                new() { Height = GridLength.Auto },
                new() { Height = GridLength.Auto },
                new() { Height = GridLength.Auto }
            }
        };

        private readonly TextBlock _cpuDescription = new() { Text = Resource.FanCurveControl_CPU, FontWeight = FontWeights.Medium, Margin = new(0, 0, 8, 0) };
        private readonly TextBlock _cpuSensorDescription = new() { Text = Resource.FanCurveControl_CPUSensor, FontWeight = FontWeights.Medium, Margin = new(0, 0, 8, 0) };
        private readonly TextBlock _gpuDescription = new() { Text = Resource.FanCurveControl_GPU, FontWeight = FontWeights.Medium, Margin = new(0, 0, 8, 0) };
        private readonly TextBlock _gpu2Description = new() { Text = Resource.FanCurveControl_GPU2, FontWeight = FontWeights.Medium, Margin = new(0, 0, 8, 0) };

        private readonly TextBlock _cpuValue = new();
        private readonly TextBlock _cpuSensorValue = new();
        private readonly TextBlock _gpuValue = new();
        private readonly TextBlock _gpu2Value = new();

        public InfoTooltip()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SetResourceReference(StyleProperty, typeof(ToolTip));

            Grid.SetColumn(_cpuDescription, 0);
            Grid.SetColumn(_cpuSensorDescription, 0);
            Grid.SetColumn(_gpuDescription, 0);
            Grid.SetColumn(_gpu2Description, 0);
            Grid.SetColumn(_cpuValue, 1);
            Grid.SetColumn(_cpuSensorValue, 1);
            Grid.SetColumn(_gpuValue, 1);
            Grid.SetColumn(_gpu2Value, 1);

            Grid.SetRow(_cpuDescription, 0);
            Grid.SetRow(_cpuSensorDescription, 1);
            Grid.SetRow(_gpuDescription, 2);
            Grid.SetRow(_gpu2Description, 3);
            Grid.SetRow(_cpuValue, 0);
            Grid.SetRow(_cpuSensorValue, 1);
            Grid.SetRow(_gpuValue, 2);
            Grid.SetRow(_gpu2Value, 3);

            _grid.Children.Add(_cpuDescription);
            _grid.Children.Add(_cpuSensorDescription);
            _grid.Children.Add(_gpuDescription);
            _grid.Children.Add(_gpu2Description);
            _grid.Children.Add(_cpuValue);
            _grid.Children.Add(_cpuSensorValue);
            _grid.Children.Add(_gpuValue);
            _grid.Children.Add(_gpu2Value);
            Content = _grid;
        }

        public void Update(FanTableData[] tableData, int index, int value)
        {
            Update(tableData, index, value, FanTableType.CPU, _cpuDescription, _cpuValue);
            Update(tableData, index, value, FanTableType.CPUSensor, _cpuSensorDescription, _cpuSensorValue);
            Update(tableData, index, value, FanTableType.GPU, _gpuDescription, _gpuValue);
            Update(tableData, index, value, FanTableType.GPU2, _gpu2Description, _gpu2Value);
        }

        private static void Update(FanTableData[] tableData, int index, int value, FanTableType type, TextBlock descriptionTextBlock, TextBlock valueTextBlock)
        {
            var text = tableData
                .Where(td => td.Type == type)
                .Select(td => GetDescription(td, index, value))
                .FirstOrDefault();

            var visibility = text is null ? Visibility.Collapsed : Visibility.Visible;
            valueTextBlock.Text = text ?? "-";
            valueTextBlock.Visibility = visibility;
            descriptionTextBlock.Visibility = visibility;
        }

        private static string GetDescription(FanTableData tableData, int index, int value)
        {
            try
            {
                var temp = tableData.Temps[index];
                if (temp >= 127)
                    return "-";

                var rpm = value < 0 ? 0 : tableData.FanSpeeds[value];
                return $"{temp}{CelsiusUnit} @ {rpm} {RpmUnit}";
            }
            catch
            {
                return "-";
            }
        }
    }
}
