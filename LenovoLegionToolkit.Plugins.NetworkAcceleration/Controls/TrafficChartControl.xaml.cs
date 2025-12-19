using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Controls;

/// <summary>
/// Traffic chart control for displaying download/upload speed history
/// </summary>
public partial class TrafficChartControl : UserControl
{
    private const int MaxDataPoints = 60;
    private const double ChartPadding = 12.0;
    private const double LineThickness = 2.5;
    
    // Colors matching the UI (red for download, blue for upload) with better opacity
    private static readonly SolidColorBrush DownloadColor = new(Color.FromRgb(0xE7, 0x4C, 0x3C));
    private static readonly SolidColorBrush UploadColor = new(Color.FromRgb(0x1E, 0x90, 0xFF));
    private static readonly SolidColorBrush DownloadGradientStart = new(Color.FromArgb(0x80, 0xE7, 0x4C, 0x3C));
    private static readonly SolidColorBrush UploadGradientStart = new(Color.FromArgb(0x80, 0x1E, 0x90, 0xFF));
    private static readonly SolidColorBrush GridLineColor = new(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
    
    private (long downloadSpeed, long uploadSpeed)[] _lastDataPoints = Array.Empty<(long, long)>();

    public TrafficChartControl()
    {
        InitializeComponent();
        SizeChanged += TrafficChartControl_SizeChanged;
        Loaded += TrafficChartControl_Loaded;
    }
    
    private void TrafficChartControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure canvas has proper size when loaded
        var border = ChartCanvas.Parent as Border;
        if (border != null)
        {
            ChartCanvas.Width = Math.Max(0, border.ActualWidth - border.Padding.Left - border.Padding.Right - ChartPadding * 2);
            ChartCanvas.Height = Math.Max(0, border.ActualHeight - border.Padding.Top - border.Padding.Bottom - ChartPadding * 2);
        }
    }

    private void TrafficChartControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update canvas size and redraw if we have data
        if (e.NewSize.Width > 0 && e.NewSize.Height > 0 && ChartCanvas != null)
        {
            var border = ChartCanvas.Parent as Border;
            if (border != null)
            {
                ChartCanvas.Width = Math.Max(0, border.ActualWidth - border.Padding.Left - border.Padding.Right);
                ChartCanvas.Height = Math.Max(0, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            }
            if (_lastDataPoints.Length > 0)
            {
                UpdateChart(_lastDataPoints);
            }
        }
    }

    /// <summary>
    /// Update chart with new speed history data
    /// </summary>
    public void UpdateChart((long downloadSpeed, long uploadSpeed)[] dataPoints)
    {
        // Store data for redraw on size change
        _lastDataPoints = dataPoints ?? Array.Empty<(long, long)>();
        
        ChartCanvas.Children.Clear();

        if (dataPoints == null || dataPoints.Length == 0)
            return;
        
        // Ensure canvas has proper size
        var border = ChartCanvas.Parent as Border;
        if (border != null && (ChartCanvas.ActualWidth <= 0 || ChartCanvas.ActualHeight <= 0))
        {
            ChartCanvas.Width = Math.Max(0, border.ActualWidth - border.Padding.Left - border.Padding.Right - ChartPadding * 2);
            ChartCanvas.Height = Math.Max(0, border.ActualHeight - border.Padding.Top - border.Padding.Bottom - ChartPadding * 2);
        }

        var canvasWidth = ChartCanvas.ActualWidth - ChartPadding * 2;
        var canvasHeight = ChartCanvas.ActualHeight - ChartPadding * 2;

        if (canvasWidth <= 0 || canvasHeight <= 0)
            return;

        // Find max speed for scaling
        var maxDownload = dataPoints.Max(d => d.downloadSpeed);
        var maxUpload = dataPoints.Max(d => d.uploadSpeed);
        var maxSpeed = Math.Max(maxDownload, maxUpload);
        
        // Ensure minimum scale for better visualization
        if (maxSpeed == 0)
            maxSpeed = 1;

        // Draw grid lines
        DrawGridLines(canvasWidth, canvasHeight);

        // Draw area fills first (behind the lines)
        DrawAreaFill(dataPoints, d => d.downloadSpeed, maxSpeed, canvasWidth, canvasHeight, DownloadGradientStart, DownloadColor);
        DrawAreaFill(dataPoints, d => d.uploadSpeed, maxSpeed, canvasWidth, canvasHeight, UploadGradientStart, UploadColor);

        // Draw lines on top
        DrawSmoothLine(dataPoints, d => d.downloadSpeed, maxSpeed, canvasWidth, canvasHeight, DownloadColor);
        DrawSmoothLine(dataPoints, d => d.uploadSpeed, maxSpeed, canvasWidth, canvasHeight, UploadColor);
    }

    private void DrawGridLines(double width, double height)
    {
        // Draw horizontal grid lines (3 lines for 4 sections) - thinner and more subtle
        for (int i = 1; i < 4; i++)
        {
            var y = ChartPadding + (height * i / 4.0);
            var line = new Line
            {
                X1 = ChartPadding,
                Y1 = y,
                X2 = ChartPadding + width,
                Y2 = y,
                Stroke = GridLineColor,
                StrokeThickness = 0.5,
                SnapsToDevicePixels = true
            };
            ChartCanvas.Children.Add(line);
        }

        // Draw vertical grid lines (5 lines for 6 sections) - more vertical divisions
        for (int i = 1; i < 6; i++)
        {
            var x = ChartPadding + (width * i / 6.0);
            var line = new Line
            {
                X1 = x,
                Y1 = ChartPadding,
                X2 = x,
                Y2 = ChartPadding + height,
                Stroke = GridLineColor,
                StrokeThickness = 0.5,
                SnapsToDevicePixels = true
            };
            ChartCanvas.Children.Add(line);
        }
    }

    /// <summary>
    /// Draw smooth line using PolyBezier for better visual appearance
    /// </summary>
    private void DrawSmoothLine((long downloadSpeed, long uploadSpeed)[] dataPoints,
        Func<(long downloadSpeed, long uploadSpeed), long> speedSelector,
        long maxSpeed,
        double width,
        double height,
        SolidColorBrush color)
    {
        if (dataPoints.Length < 2)
            return;

        var points = new Point[dataPoints.Length];
        for (int i = 0; i < dataPoints.Length; i++)
        {
            var speed = speedSelector(dataPoints[i]);
            var normalizedSpeed = maxSpeed > 0 ? (double)speed / maxSpeed : 0.0;

            // X: from right to left (most recent on the right)
            var x = ChartPadding + width - (width * i / (dataPoints.Length - 1.0));
            // Y: from bottom to top (0 at bottom, maxSpeed at top)
            var y = ChartPadding + height - (height * normalizedSpeed);

            points[i] = new Point(x, y);
        }

        var pathFigure = new PathFigure { StartPoint = points[0] };
        var pathSegmentCollection = new PathSegmentCollection();

        // Create smooth curve using PolyBezier (quadratic Bezier for simplicity)
        for (int i = 1; i < points.Length; i++)
        {
            if (i == 1)
            {
                // First segment: use LineSegment
                pathSegmentCollection.Add(new LineSegment { Point = points[i] });
            }
            else
            {
                // Use quadratic Bezier for smooth curves
                var prevPoint = points[i - 1];
                var currentPoint = points[i];
                
                // Control point is midpoint between previous and current point
                var controlPoint = new Point(
                    (prevPoint.X + currentPoint.X) / 2,
                    (prevPoint.Y + currentPoint.Y) / 2);

                pathSegmentCollection.Add(new QuadraticBezierSegment
                {
                    Point1 = controlPoint,
                    Point2 = currentPoint
                });
            }
        }

        pathFigure.Segments = pathSegmentCollection;

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        var path = new Path
        {
            Data = pathGeometry,
            Stroke = color,
            StrokeThickness = LineThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };

        // Add subtle shadow effect for better visibility
        path.Effect = new DropShadowEffect
        {
            Color = Colors.Black,
            Direction = 270,
            ShadowDepth = 1,
            Opacity = 0.2,
            BlurRadius = 2
        };

        ChartCanvas.Children.Add(path);
    }

    /// <summary>
    /// Draw gradient fill area under the line
    /// </summary>
    private void DrawAreaFill((long downloadSpeed, long uploadSpeed)[] dataPoints,
        Func<(long downloadSpeed, long uploadSpeed), long> speedSelector,
        long maxSpeed,
        double width,
        double height,
        SolidColorBrush startColor,
        SolidColorBrush endColor)
    {
        if (dataPoints.Length < 2)
            return;

        var points = new Point[dataPoints.Length + 2];
        var bottomY = ChartPadding + height;

        // Build points along the line
        for (int i = 0; i < dataPoints.Length; i++)
        {
            var speed = speedSelector(dataPoints[i]);
            var normalizedSpeed = maxSpeed > 0 ? (double)speed / maxSpeed : 0.0;

            var x = ChartPadding + width - (width * i / (dataPoints.Length - 1.0));
            var y = ChartPadding + height - (height * normalizedSpeed);

            points[i + 1] = new Point(x, y);
        }

        // Close the path: start from bottom-left, go through curve points, end at bottom-right
        points[0] = new Point(ChartPadding, bottomY); // Bottom-left
        points[points.Length - 1] = new Point(ChartPadding + width, bottomY); // Bottom-right

        var pathFigure = new PathFigure { StartPoint = points[0] };
        var pathSegmentCollection = new PathSegmentCollection();

        // Create smooth curve for the top edge
        for (int i = 1; i < points.Length - 1; i++)
        {
            if (i == 1)
            {
                pathSegmentCollection.Add(new LineSegment { Point = points[i] });
            }
            else
            {
                var prevPoint = points[i - 1];
                var currentPoint = points[i];

                var controlPoint = new Point(
                    (prevPoint.X + currentPoint.X) / 2,
                    (prevPoint.Y + currentPoint.Y) / 2);

                pathSegmentCollection.Add(new QuadraticBezierSegment
                {
                    Point1 = controlPoint,
                    Point2 = currentPoint
                });
            }
        }

        // Close the path back to bottom
        pathSegmentCollection.Add(new LineSegment { Point = points[points.Length - 1] });

        pathFigure.Segments = pathSegmentCollection;
        pathFigure.IsClosed = true;

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        // Create gradient brush
        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop(startColor.Color, 0.0),
                new GradientStop(endColor.Color, 0.5),
                new GradientStop(Color.FromArgb(0x00, endColor.Color.R, endColor.Color.G, endColor.Color.B), 1.0)
            }
        };

        var path = new Path
        {
            Data = pathGeometry,
            Fill = gradientBrush,
            Opacity = 0.3
        };

        ChartCanvas.Children.Insert(0, path); // Insert at the beginning so it's behind the lines
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        // SizeChanged event will handle redraw
    }
}