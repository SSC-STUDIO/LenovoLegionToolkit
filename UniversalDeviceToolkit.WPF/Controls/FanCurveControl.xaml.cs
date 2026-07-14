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

    /// <summary>Matches Track Margin="0,10" in FanCurveSliderStyle — usable thumb travel area.</summary>
    private const double TrackVerticalMargin = 10;

    private readonly List<Slider> _sliders = [];
    private readonly InfoTooltip _customToolTip = new();
    private readonly DebounceDispatcher _debouncer = new();
    private Path? _cachedLinePath;
    private Polygon? _cachedFillPolygon;
    private Brush? _cachedLineBrush;

    private FanTableData[]? _tableData;
    private FanTable? _minimumFanTable;

    public FanCurveControl()
    {
        InitializeComponent();

        MouseLeave += FanCurveControl_MouseLeave;
        Unloaded += FanCurveControl_Unloaded;
        SizeChanged += (_, _) => DrawGraph();
        _slidersGrid.SizeChanged += (_, _) => DrawGraph();
    }

    private void FanCurveControl_MouseLeave(object sender, MouseEventArgs e)
    {
        _customToolTip.IsOpen = false;
    }

    private void FanCurveControl_Unloaded(object sender, RoutedEventArgs e)
    {
        MouseLeave -= FanCurveControl_MouseLeave;
        foreach (var slider in _sliders)
        {
            slider.MouseMove -= Slider_MouseMove;
            slider.ValueChanged -= Slider_OnValueChanged;
        }
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var size = base.ArrangeOverride(arrangeBounds);
        DrawGraph();
        return size;
    }

    public void SetFanTableInfo(FanTableInfo fanTableInfo, FanTable minimumFanTable)
    {
        foreach (var slider in _sliders)
        {
            slider.MouseMove -= Slider_MouseMove;
            slider.ValueChanged -= Slider_OnValueChanged;
        }

        _sliders.Clear();
        _slidersGrid.Children.Clear();
        InvalidateGraphCache();

        var tableValues = fanTableInfo.Table.GetTable();

        for (var i = 0; i < tableValues.Length; i++)
        {
            var slider = GenerateSlider(i, 0, 10);
            // Suppress ValueChanged side-effects while seeding firmware table.
            slider.ValueChanged -= Slider_OnValueChanged;
            slider.Value = tableValues[i];
            slider.ValueChanged += Slider_OnValueChanged;
            _sliders.Add(slider);
            _slidersGrid.Children.Add(slider);
        }

        _tableData = fanTableInfo.Data;
        _minimumFanTable = minimumFanTable;

        // Layout → render → draw from values (not fragile thumb visuals).
        Dispatcher.InvokeAsync(() =>
        {
            UpdateLayout();
            DrawGraph();
        }, DispatcherPriority.Loaded);
        Dispatcher.InvokeAsync(DrawGraph, DispatcherPriority.Render);
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

        // Align with original LLT: column 0 is left gutter, points use 1..N.
        Grid.SetColumn(slider, index + 1);

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

        // Slider value is 0..10 step index; FanSpeeds table is 0-based RPM ladder → value-1.
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
        _debouncer.Throttle(100, () =>
        {
            if (_sliders.Count < 10)
                return;

            if (sender is not Slider currentSlider)
                return;

            if (currentSlider is { IsKeyboardFocusWithin: false, IsMouseCaptureWithin: false })
            {
                // Still redraw — programmatic/min clamps need the line to follow.
                DrawGraph();
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
            DrawGraph();
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

        // Fan table must be non-decreasing with temperature (left → right).
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

    private void InvalidateGraphCache()
    {
        _cachedLinePath = null;
        _cachedFillPolygon = null;
        _cachedLineBrush = null;
        _canvas.Children.Clear();
    }

    private void DrawGraph()
    {
        if (_sliders.Count < 2)
            return;

        var lineBrush = ResolveLineBrush();
        var fillBrush = ResolveFillBrush(lineBrush);

        // Compute points from values (old LLT approach) — thumb visuals lag layout and
        // produced a decorative/wrong curve with orphaned points.
        var points = _sliders
            .Select(GetGraphPointFromValue)
            .ToArray();

        if (points.Length < 2)
            return;

        // Need at least some real width after layout.
        if (points.All(p => p.X <= 0) || _slidersGrid.ActualHeight < 8)
            return;

        if (!ReferenceEquals(_cachedLineBrush, lineBrush))
        {
            _cachedLineBrush = lineBrush;
            InvalidateGraphCache();
        }

        if (_cachedLinePath is null || _cachedFillPolygon is null)
        {
            _canvas.Children.Clear();

            _cachedFillPolygon = new Polygon { IsHitTestVisible = false };
            _canvas.Children.Add(_cachedFillPolygon);

            _cachedLinePath = new Path
            {
                StrokeThickness = 2.25,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false,
            };
            _canvas.Children.Add(_cachedLinePath);
        }

        _cachedLinePath.Stroke = lineBrush;

        var pathSegmentCollection = new PathSegmentCollection();
        foreach (var point in points.Skip(1))
            pathSegmentCollection.Add(new LineSegment { Point = point, IsStroked = true });

        _cachedLinePath.Data = new PathGeometry
        {
            Figures =
            [
                new PathFigure
                {
                    StartPoint = points[0],
                    Segments = pathSegmentCollection,
                    IsClosed = false,
                    IsFilled = false,
                }
            ]
        };

        var canvasHeight = _canvas.ActualHeight > 1 ? _canvas.ActualHeight : _slidersGrid.ActualHeight;
        var baselineY = Math.Max(0, canvasHeight - 1);
        var pointCollection = new PointCollection { new(points[0].X, baselineY) };
        foreach (var point in points)
            pointCollection.Add(point);
        pointCollection.Add(new(points[^1].X, baselineY));

        _cachedFillPolygon.Fill = fillBrush;
        _cachedFillPolygon.Points = pointCollection;
    }

    /// <summary>
    /// Map slider value → canvas point. High value = top of chart (100% fan), low = bottom.
    /// Matches original UniversalDeviceToolkit FanCurveControl geometry.
    /// </summary>
    private Point GetGraphPointFromValue(Slider slider)
    {
        var height = slider.ActualHeight > 1 ? slider.ActualHeight : _slidersGrid.ActualHeight;
        var width = slider.ActualWidth > 1 ? slider.ActualWidth : (_slidersGrid.ActualWidth / Math.Max(1, _sliders.Count));

        var range = Math.Max(1e-6, slider.Maximum - slider.Minimum);
        var ratio = (slider.Value - slider.Minimum) / range; // 0..1

        // Track has vertical margin; map onto usable band so points sit on the thumb travel path.
        var usable = Math.Max(1, height - 2 * TrackVerticalMargin);
        var yInSlider = TrackVerticalMargin + usable * (1.0 - ratio);
        var xInSlider = width * 0.5;

        return slider.TranslatePoint(new Point(xInSlider, yInSlider), _canvas);
    }

    private static Brush ResolveLineBrush()
    {
        if (Application.Current?.Resources["ChartUtilizationBrush"] is SolidColorBrush chart)
            return chart;
        if (Application.Current?.Resources["AccentFillColorDefaultBrush"] is SolidColorBrush accent)
            return accent;
        if (Application.Current?.Resources["SystemAccentColorPrimaryBrush"] is SolidColorBrush system)
            return system;
        return new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
    }

    private static Brush ResolveFillBrush(Brush lineBrush)
    {
        var color = lineBrush is SolidColorBrush solid
            ? solid.Color
            : Color.FromRgb(0x00, 0x78, 0xD4);

        var fill = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(110, color.R, color.G, color.B), 0.0));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(24, color.R, color.G, color.B), 1.0));
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
