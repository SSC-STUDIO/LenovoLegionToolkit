using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private const double ChartPadding = 20.0;
    private const double LineThickness = 3.0;
    
    // Colors matching the UI (red for download, blue for upload) with better opacity
    private static readonly SolidColorBrush DownloadColor = new(Color.FromRgb(0xE7, 0x4C, 0x3C));
    private static readonly SolidColorBrush UploadColor = new(Color.FromRgb(0x1E, 0x90, 0xFF));
    private static readonly SolidColorBrush DownloadGradientStart = new(Color.FromArgb(0x80, 0xE7, 0x4C, 0x3C));
    private static readonly SolidColorBrush UploadGradientStart = new(Color.FromArgb(0x80, 0x1E, 0x90, 0xFF));
    private static readonly SolidColorBrush GridLineColor = new(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
    
    private readonly object _dataLock = new();
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
        UpdateCanvasSize();
    }

    private void TrafficChartControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update canvas size and redraw if we have data
        if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            UpdateCanvasSize();
            (long, long)[] dataPoints;
            lock (_dataLock)
            {
                dataPoints = _lastDataPoints;
            }
            if (dataPoints.Length > 0)
            {
                UpdateChart(dataPoints);
            }
        }
    }

    private void UpdateCanvasSize()
    {
        if (ChartCanvas == null)
            return;

        // Use Dispatcher to ensure we're on UI thread
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateCanvasSize);
            return;
        }

        var border = ChartCanvas.Parent as Border;
        if (border != null && border.ActualWidth > 0 && border.ActualHeight > 0)
        {
            // Canvas should fill the entire border minus padding
            var newWidth = Math.Max(0, border.ActualWidth - border.Padding.Left - border.Padding.Right);
            var newHeight = Math.Max(0, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            
            // Only update if size actually changed to avoid unnecessary redraws
            if (Math.Abs(ChartCanvas.Width - newWidth) > 0.1 || Math.Abs(ChartCanvas.Height - newHeight) > 0.1)
            {
                ChartCanvas.Width = newWidth;
                ChartCanvas.Height = newHeight;
            }
        }
        else if (ActualWidth > 0 && ActualHeight > 0)
        {
            // Fallback: use control's actual size
            var newWidth = ActualWidth;
            var newHeight = ActualHeight;
            
            if (Math.Abs(ChartCanvas.Width - newWidth) > 0.1 || Math.Abs(ChartCanvas.Height - newHeight) > 0.1)
            {
                ChartCanvas.Width = newWidth;
                ChartCanvas.Height = newHeight;
            }
        }
    }

    /// <summary>
    /// Update chart with new speed history data
    /// </summary>
    public void UpdateChart((long downloadSpeed, long uploadSpeed)[] dataPoints)
    {
        // Ensure we're on UI thread
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateChart(dataPoints));
            return;
        }

        // Store data for redraw on size change (thread-safe)
        var dataToStore = dataPoints ?? Array.Empty<(long, long)>();
        lock (_dataLock)
        {
            _lastDataPoints = dataToStore;
        }
        
        if (ChartCanvas == null)
            return;

        ChartCanvas.Children.Clear();

        if (dataPoints == null || dataPoints.Length == 0)
            return;
        
        // Ensure canvas has proper size
        UpdateCanvasSize();

        var canvasWidth = ChartCanvas.ActualWidth;
        var canvasHeight = ChartCanvas.ActualHeight;

        // Wait for layout if size is not yet available
        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            // Schedule update after layout
            Dispatcher.BeginInvoke(new Action(() => UpdateChart(dataPoints)), System.Windows.Threading.DispatcherPriority.Loaded);
            return;
        }

        // Find max speed for scaling
        var maxDownload = dataPoints.Length > 0 ? dataPoints.Max(d => d.downloadSpeed) : 0;
        var maxUpload = dataPoints.Length > 0 ? dataPoints.Max(d => d.uploadSpeed) : 0;
        var maxSpeed = Math.Max(maxDownload, maxUpload);
        
        // Ensure minimum scale for better visualization
        if (maxSpeed == 0)
            maxSpeed = 1;

        try
        {
            // Draw grid lines
            DrawGridLines(canvasWidth, canvasHeight);

            // Draw area fills first (behind the lines)
            if (dataPoints.Length >= 2)
            {
                DrawAreaFill(dataPoints, d => d.downloadSpeed, maxSpeed, canvasWidth, canvasHeight, DownloadGradientStart, DownloadColor);
                DrawAreaFill(dataPoints, d => d.uploadSpeed, maxSpeed, canvasWidth, canvasHeight, UploadGradientStart, UploadColor);
            }

            // Draw lines on top
            if (dataPoints.Length >= 2)
            {
                DrawSmoothLine(dataPoints, d => d.downloadSpeed, maxSpeed, canvasWidth, canvasHeight, DownloadColor);
                DrawSmoothLine(dataPoints, d => d.uploadSpeed, maxSpeed, canvasWidth, canvasHeight, UploadColor);
            }
            else if (dataPoints.Length == 1)
            {
                // Draw single point as a dot
                DrawSinglePoint(dataPoints[0].downloadSpeed, dataPoints[0].uploadSpeed, maxSpeed, canvasWidth, canvasHeight);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            System.Diagnostics.Debug.WriteLine($"Error updating chart: {ex.Message}");
        }
    }

    private void DrawSinglePoint(long downloadSpeed, long uploadSpeed, long maxSpeed, double width, double height)
    {
        if (ChartCanvas == null)
            return;

        // Draw download point
        var downloadY = ChartPadding + height - (height * downloadSpeed / (double)maxSpeed);
        var downloadX = ChartPadding + width; // Rightmost position (most recent)
        
        var downloadEllipse = new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = DownloadColor,
            Stroke = DownloadColor,
            StrokeThickness = 2
        };
        Canvas.SetLeft(downloadEllipse, downloadX - 3);
        Canvas.SetTop(downloadEllipse, downloadY - 3);
        ChartCanvas.Children.Add(downloadEllipse);

        // Draw upload point
        var uploadY = ChartPadding + height - (height * uploadSpeed / (double)maxSpeed);
        var uploadX = ChartPadding + width; // Rightmost position (most recent)
        
        var uploadEllipse = new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = UploadColor,
            Stroke = UploadColor,
            StrokeThickness = 2
        };
        Canvas.SetLeft(uploadEllipse, uploadX - 3);
        Canvas.SetTop(uploadEllipse, uploadY - 3);
        ChartCanvas.Children.Add(uploadEllipse);
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
    /// Draw smooth line using Catmull-Rom spline for better visual appearance (similar to SteamTools)
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
            // Ensure we don't divide by zero
            var xRatio = dataPoints.Length > 1 ? (double)i / (dataPoints.Length - 1.0) : 0.0;
            var x = ChartPadding + width - (width * xRatio);
            // Y: from bottom to top (0 at bottom, maxSpeed at top)
            // Clamp Y to valid range
            var y = ChartPadding + height - (height * Math.Max(0, Math.Min(1, normalizedSpeed)));

            points[i] = new Point(x, y);
        }

        // Use PathGeometry with smooth curves (similar to SteamTools LineSmoothness = 1)
        var pathFigure = new PathFigure { StartPoint = points[0] };
        var pathSegmentCollection = new PathSegmentCollection();

        // Create smooth curve using cubic Bezier for better smoothness
        for (int i = 1; i < points.Length; i++)
        {
            if (i == 1)
            {
                // First segment: use LineSegment for smooth start
                pathSegmentCollection.Add(new LineSegment { Point = points[i] });
            }
            else
            {
                // Use cubic Bezier for smoother curves (similar to SteamTools)
                var prevPoint = points[i - 1];
                var currentPoint = points[i];
                var prevPrevPoint = i > 1 ? points[i - 2] : prevPoint;
                var nextPoint = i < points.Length - 1 ? points[i + 1] : currentPoint;
                
                // Calculate control points for smooth curve
                // Use Catmull-Rom style control points for better smoothness
                var tension = 0.5; // Smoothness factor (0 = straight, 1 = very smooth)
                var cp1X = prevPoint.X + (currentPoint.X - prevPrevPoint.X) * tension / 3.0;
                var cp1Y = prevPoint.Y + (currentPoint.Y - prevPrevPoint.Y) * tension / 3.0;
                var cp2X = currentPoint.X - (nextPoint.X - prevPoint.X) * tension / 3.0;
                var cp2Y = currentPoint.Y - (nextPoint.Y - prevPoint.Y) * tension / 3.0;

                pathSegmentCollection.Add(new BezierSegment
                {
                    Point1 = new Point(cp1X, cp1Y),
                    Point2 = new Point(cp2X, cp2Y),
                    Point3 = currentPoint
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

        var curvePoints = CalculateCurvePoints(dataPoints, speedSelector, maxSpeed, width, height);
        var pathGeometry = CreatePathGeometry(curvePoints, width, height);
        var gradientBrush = CreateGradientBrush(startColor, endColor);

        if (pathGeometry.Figures.Count > 0)
        {
            var path = new Path
            {
                Data = pathGeometry,
                Fill = gradientBrush,
                Opacity = 0.35
            };

            ChartCanvas.Children.Insert(0, path);
        }
    }

    private Point[] CalculateCurvePoints((long downloadSpeed, long uploadSpeed)[] dataPoints,
        Func<(long downloadSpeed, long uploadSpeed), long> speedSelector,
        long maxSpeed,
        double width,
        double height)
    {
        var curvePoints = new Point[dataPoints.Length];
        for (int i = 0; i < dataPoints.Length; i++)
        {
            var speed = speedSelector(dataPoints[i]);
            var normalizedSpeed = maxSpeed > 0 ? (double)speed / maxSpeed : 0.0;

            var xRatio = dataPoints.Length > 1 ? (double)i / (dataPoints.Length - 1.0) : 0.0;
            var x = ChartPadding + width - (width * xRatio);
            var y = ChartPadding + height - (height * Math.Max(0, Math.Min(1, normalizedSpeed)));

            curvePoints[i] = new Point(x, y);
        }
        return curvePoints;
    }

    private PathGeometry CreatePathGeometry(Point[] curvePoints, double width, double height)
    {
        var bottomY = ChartPadding + height;
        var leftX = ChartPadding;
        var rightX = ChartPadding + width;

        var pathFigure = CreatePathFigure(curvePoints, leftX, bottomY, rightX);
        var area = CalculateSignedArea(curvePoints, leftX, bottomY, rightX);

        if (area > 0 && curvePoints.Length >= 2)
        {
            return CreateReversedPathGeometry(curvePoints, leftX, bottomY, rightX);
        }

        var pathGeometry = new PathGeometry { FillRule = FillRule.Nonzero };
        pathGeometry.Figures.Add(pathFigure);
        return pathGeometry;
    }

    private PathFigure CreatePathFigure(Point[] curvePoints, double leftX, double bottomY, double rightX)
    {
        var pathFigure = new PathFigure { StartPoint = new Point(leftX, bottomY) };
        var pathSegmentCollection = new PathSegmentCollection();

        pathSegmentCollection.Add(new LineSegment { Point = curvePoints[0] });

        for (int i = 1; i < curvePoints.Length; i++)
        {
            if (i == 1)
            {
                pathSegmentCollection.Add(new LineSegment { Point = curvePoints[i] });
            }
            else
            {
                var bezierSegment = CreateBezierSegment(curvePoints, i);
                pathSegmentCollection.Add(bezierSegment);
            }
        }

        pathSegmentCollection.Add(new LineSegment { Point = new Point(rightX, bottomY) });
        pathFigure.Segments = pathSegmentCollection;
        pathFigure.IsClosed = true;

        return pathFigure;
    }

    private BezierSegment CreateBezierSegment(Point[] points, int index)
    {
        const double tension = 0.5;
        var prevPoint = points[index - 1];
        var currentPoint = points[index];
        var prevPrevPoint = index > 1 ? points[index - 2] : prevPoint;
        var nextPoint = index < points.Length - 1 ? points[index + 1] : currentPoint;

        var cp1X = prevPoint.X + (currentPoint.X - prevPrevPoint.X) * tension / 3.0;
        var cp1Y = prevPoint.Y + (currentPoint.Y - prevPrevPoint.Y) * tension / 3.0;
        var cp2X = currentPoint.X - (nextPoint.X - prevPoint.X) * tension / 3.0;
        var cp2Y = currentPoint.Y - (nextPoint.Y - prevPoint.Y) * tension / 3.0;

        return new BezierSegment
        {
            Point1 = new Point(cp1X, cp1Y),
            Point2 = new Point(cp2X, cp2Y),
            Point3 = currentPoint
        };
    }

    private double CalculateSignedArea(Point[] curvePoints, double leftX, double bottomY, double rightX)
    {
        var points = new List<Point> { new Point(leftX, bottomY) };
        points.AddRange(curvePoints);
        points.Add(new Point(rightX, bottomY));

        if (points.Count < 3)
            return 0;

        double area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }
        return area / 2.0;
    }

    private PathGeometry CreateReversedPathGeometry(Point[] curvePoints, double leftX, double bottomY, double rightX)
    {
        var pathGeometry = new PathGeometry { FillRule = FillRule.Nonzero };
        var reversedPathFigure = new PathFigure { StartPoint = new Point(leftX, bottomY) };
        var reversedSegments = new PathSegmentCollection();
        var reversedCurvePoints = curvePoints.Reverse().ToArray();

        reversedSegments.Add(new LineSegment { Point = reversedCurvePoints[0] });

        for (int i = 1; i < reversedCurvePoints.Length; i++)
        {
            if (i == 1)
            {
                reversedSegments.Add(new LineSegment { Point = reversedCurvePoints[i] });
            }
            else
            {
                var bezierSegment = CreateBezierSegment(reversedCurvePoints, i);
                reversedSegments.Add(bezierSegment);
            }
        }

        reversedSegments.Add(new LineSegment { Point = new Point(rightX, bottomY) });
        reversedPathFigure.Segments = reversedSegments;
        reversedPathFigure.IsClosed = true;
        pathGeometry.Figures.Add(reversedPathFigure);

        return pathGeometry;
    }

    private LinearGradientBrush CreateGradientBrush(SolidColorBrush startColor, SolidColorBrush endColor)
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            GradientStops = new GradientStopCollection
            {
                new GradientStop(startColor.Color, 0.0),
                new GradientStop(endColor.Color, 0.2),
                new GradientStop(Color.FromArgb(0x80, endColor.Color.R, endColor.Color.G, endColor.Color.B), 0.5),
                new GradientStop(Color.FromArgb(0x00, endColor.Color.R, endColor.Color.G, endColor.Color.B), 1.0)
            }
        };
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        // SizeChanged event will handle redraw
    }
}