using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

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
            if (_lastDataPoints.Length > 0)
            {
                UpdateChart(_lastDataPoints);
            }
        }
    }

    private void UpdateCanvasSize()
    {
        if (ChartCanvas == null)
            return;

        var border = ChartCanvas.Parent as Border;
        if (border != null && border.ActualWidth > 0 && border.ActualHeight > 0)
        {
            // Canvas should fill the entire border minus padding
            ChartCanvas.Width = Math.Max(0, border.ActualWidth - border.Padding.Left - border.Padding.Right);
            ChartCanvas.Height = Math.Max(0, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
        }
        else if (ActualWidth > 0 && ActualHeight > 0)
        {
            // Fallback: use control's actual size
            ChartCanvas.Width = ActualWidth;
            ChartCanvas.Height = ActualHeight;
        }
    }

    /// <summary>
    /// Update chart with new speed history data
    /// </summary>
    public void UpdateChart((long downloadSpeed, long uploadSpeed)[] dataPoints)
    {
        // Ensure we're on the UI thread
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateChart(dataPoints));
            return;
        }

        try
        {
            // Store data for redraw on size change
            _lastDataPoints = dataPoints ?? Array.Empty<(long, long)>();
            
            if (ChartCanvas == null)
                return;

            ChartCanvas.Children.Clear();

            if (dataPoints == null || dataPoints.Length == 0)
                return;
            
            // Ensure canvas has proper size
            UpdateCanvasSize();

            var canvasWidth = ChartCanvas.ActualWidth;
            var canvasHeight = ChartCanvas.ActualHeight;

            // Wait a bit if canvas size is not ready yet
            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                // Use DispatcherTimer to retry after layout
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(50)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    UpdateCanvasSize();
                    var retryWidth = ChartCanvas.ActualWidth;
                    var retryHeight = ChartCanvas.ActualHeight;
                    if (retryWidth > 0 && retryHeight > 0)
                    {
                        UpdateChart(_lastDataPoints);
                    }
                };
                timer.Start();
                return;
            }

            // Filter out invalid data points
            var validDataPoints = dataPoints.Where(d => d.downloadSpeed >= 0 && d.uploadSpeed >= 0).ToArray();
            if (validDataPoints.Length == 0)
                return;

            // Find max speed for scaling
            var maxDownload = validDataPoints.Length > 0 ? validDataPoints.Max(d => d.downloadSpeed) : 0;
            var maxUpload = validDataPoints.Length > 0 ? validDataPoints.Max(d => d.uploadSpeed) : 0;
            var maxSpeed = Math.Max(maxDownload, maxUpload);
            
            // Ensure minimum scale for better visualization
            if (maxSpeed == 0)
                maxSpeed = 1;

            // Draw grid lines
            DrawGridLines(canvasWidth, canvasHeight);

            // Draw lines only (no area fill for cleaner look)
            if (validDataPoints.Length >= 2)
            {
                DrawSmoothLine(validDataPoints, d => d.downloadSpeed, maxSpeed, canvasWidth, canvasHeight, DownloadColor);
                DrawSmoothLine(validDataPoints, d => d.uploadSpeed, maxSpeed, canvasWidth, canvasHeight, UploadColor);
            }
            else if (validDataPoints.Length == 1)
            {
                // Draw single point as a dot
                DrawSinglePoint(validDataPoints[0], d => d.downloadSpeed, maxSpeed, canvasWidth, canvasHeight, DownloadColor);
                DrawSinglePoint(validDataPoints[0], d => d.uploadSpeed, maxSpeed, canvasWidth, canvasHeight, UploadColor);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debug.WriteLine($"Error updating chart: {ex.Message}");
        }
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
            var x = ChartPadding + width - (width * i / (dataPoints.Length - 1.0));
            // Y: from bottom to top (0 at bottom, maxSpeed at top)
            var y = ChartPadding + height - (height * normalizedSpeed);

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

        var bottomY = ChartPadding + height;
        var leftX = ChartPadding;
        var rightX = ChartPadding + width;

        // Build curve points (from right to left, matching the line drawing order)
        // Data points are ordered with most recent on the right (index 0 = rightmost = most recent)
        var curvePoints = new Point[dataPoints.Length];
        for (int i = 0; i < dataPoints.Length; i++)
        {
            var speed = speedSelector(dataPoints[i]);
            var normalizedSpeed = maxSpeed > 0 ? (double)speed / maxSpeed : 0.0;

            // X: from right to left (most recent on the right, matching DrawSmoothLine)
            // Avoid division by zero when there's only one data point
            var x = dataPoints.Length > 1
                ? ChartPadding + width - (width * i / (dataPoints.Length - 1.0))
                : ChartPadding + width / 2.0; // Center for single point
            // Y: from bottom to top (0 at bottom, maxSpeed at top)
            var y = ChartPadding + height - (height * normalizedSpeed);

            // Clamp Y to valid range
            y = Math.Max(ChartPadding, Math.Min(ChartPadding + height, y));
            curvePoints[i] = new Point(x, y);
        }

        // Build path points in clockwise order to ensure fill is always below the curve
        // Create a PathGeometry with smooth curves
        var pathFigure = new PathFigure 
        { 
            StartPoint = new Point(leftX, bottomY) // Start at bottom-left
        };
        var pathSegmentCollection = new PathSegmentCollection();

        // Step 1: Line from bottom-left to the rightmost curve point (index 0, most recent data)
        pathSegmentCollection.Add(new LineSegment { Point = curvePoints[0] });

        // Step 2: Draw smooth curve from right to left using the same smoothness as the line
        for (int i = 1; i < curvePoints.Length; i++)
        {
            if (i == 1)
            {
                pathSegmentCollection.Add(new LineSegment { Point = curvePoints[i] });
            }
            else
            {
                // Use cubic Bezier matching the line smoothness (similar to SteamTools)
                var prevPoint = curvePoints[i - 1];
                var currentPoint = curvePoints[i];
                var prevPrevPoint = i > 1 ? curvePoints[i - 2] : prevPoint;
                var nextPoint = i < curvePoints.Length - 1 ? curvePoints[i + 1] : currentPoint;
                
                var tension = 0.5; // Same smoothness factor as line
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

        // Step 3: Line from leftmost curve point (last in array) to bottom-right
        pathSegmentCollection.Add(new LineSegment { Point = new Point(rightX, bottomY) });
        // Step 4: Path will be closed automatically back to StartPoint (bottom-left)

        pathFigure.Segments = pathSegmentCollection;
        pathFigure.IsClosed = true;

        // Create path geometry - use Nonzero fill rule and ensure path is clockwise
        // To ensure clockwise, we'll use a simple check: if the area is negative, reverse the path
        var pathGeometry = new PathGeometry
        {
            FillRule = FillRule.Nonzero
        };
        pathGeometry.Figures.Add(pathFigure);
        
        // Calculate signed area to check if path is clockwise
        // For a closed path, positive area = counterclockwise, negative = clockwise
        // We want clockwise, so if area is positive, we need to reverse
        double area = 0;
        var points = new List<Point> { new Point(leftX, bottomY) };
        foreach (var pt in curvePoints)
            points.Add(pt);
        points.Add(new Point(rightX, bottomY));
        
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }
        area /= 2.0;
        
        // If area is positive (counterclockwise), reverse the path by reversing the curve points
        if (area > 0)
        {
            // Reverse the path by creating a new path with reversed curve points
            pathGeometry = new PathGeometry { FillRule = FillRule.Nonzero };
            var reversedPathFigure = new PathFigure { StartPoint = new Point(leftX, bottomY) };
            var reversedSegments = new PathSegmentCollection();
            
            // Reverse curve points
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
                    // Use cubic Bezier matching the line smoothness
                    var prevPoint = reversedCurvePoints[i - 1];
                    var currentPoint = reversedCurvePoints[i];
                    var prevPrevPoint = i > 1 ? reversedCurvePoints[i - 2] : prevPoint;
                    var nextPoint = i < reversedCurvePoints.Length - 1 ? reversedCurvePoints[i + 1] : currentPoint;
                    
                    var tension = 0.5;
                    var cp1X = prevPoint.X + (currentPoint.X - prevPrevPoint.X) * tension / 3.0;
                    var cp1Y = prevPoint.Y + (currentPoint.Y - prevPrevPoint.Y) * tension / 3.0;
                    var cp2X = currentPoint.X - (nextPoint.X - prevPoint.X) * tension / 3.0;
                    var cp2Y = currentPoint.Y - (nextPoint.Y - prevPoint.Y) * tension / 3.0;
                    
                    reversedSegments.Add(new BezierSegment
                    {
                        Point1 = new Point(cp1X, cp1Y),
                        Point2 = new Point(cp2X, cp2Y),
                        Point3 = currentPoint
                    });
                }
            }
            
            reversedSegments.Add(new LineSegment { Point = new Point(rightX, bottomY) });
            reversedPathFigure.Segments = reversedSegments;
            reversedPathFigure.IsClosed = true;
            pathGeometry.Figures.Add(reversedPathFigure);
        }

        // Create gradient brush - from top (curve) to bottom (fade out)
        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), // Top of the gradient (at curve)
            EndPoint = new Point(0, 1),   // Bottom of the gradient (at bottom)
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            GradientStops = new GradientStopCollection
            {
                new GradientStop(startColor.Color, 0.0), // Start with semi-transparent color at top (curve)
                new GradientStop(endColor.Color, 0.2),   // Full color slightly below curve
                new GradientStop(Color.FromArgb(0x80, endColor.Color.R, endColor.Color.G, endColor.Color.B), 0.5), // Semi-transparent in middle
                new GradientStop(Color.FromArgb(0x00, endColor.Color.R, endColor.Color.G, endColor.Color.B), 1.0) // Fully transparent at bottom
            }
        };

        var path = new Path
        {
            Data = pathGeometry,
            Fill = gradientBrush,
            Opacity = 0.35
        };

        ChartCanvas.Children.Insert(0, path); // Insert at the beginning so it's behind the lines
    }

    /// <summary>
    /// Draw a single point as a small circle (for when there's only one data point)
    /// </summary>
    private void DrawSinglePoint((long downloadSpeed, long uploadSpeed) dataPoint,
        Func<(long downloadSpeed, long uploadSpeed), long> speedSelector,
        long maxSpeed,
        double width,
        double height,
        SolidColorBrush color)
    {
        var speed = speedSelector(dataPoint);
        var normalizedSpeed = maxSpeed > 0 ? (double)speed / maxSpeed : 0.0;
        
        var x = ChartPadding + width / 2.0;
        var y = ChartPadding + height - (height * normalizedSpeed);
        y = Math.Max(ChartPadding, Math.Min(ChartPadding + height, y));

        var ellipse = new System.Windows.Shapes.Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = color,
            Stroke = color,
            StrokeThickness = 2
        };

        Canvas.SetLeft(ellipse, x - 4);
        Canvas.SetTop(ellipse, y - 4);

        ChartCanvas.Children.Add(ellipse);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        // SizeChanged event will handle redraw
    }
}