using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.WPF.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Windows.Size;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public class NotificationWindow : UiWindow, INotificationWindow
{
    private readonly ScreenInfo _screenInfo;

    private readonly Border _container = new()
    {
        CornerRadius = new CornerRadius(8),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(0),
    };

    private readonly Grid _mainGrid = new()
    {
        ColumnDefinitions =
        {
            new() { Width = GridLength.Auto, },
            new() { Width = new(1, GridUnitType.Star) },
        },
        Margin = new(16, 16, 32, 16),
    };

    private readonly SymbolIcon _symbolIcon = new()
    {
        FontSize = 32,
        Margin = new(0, 0, 16, 0),
    };

    private readonly SymbolIcon _overlaySymbolIcon = new()
    {
        FontSize = 32,
        Margin = new(0, 0, 16, 0),
    };

    private readonly Label _textBlock = new()
    {
        FontSize = 16,
        FontWeight = FontWeights.Medium,
        VerticalContentAlignment = VerticalAlignment.Center,
    };

    private readonly DropShadowEffect _dropShadow = new()
    {
        BlurRadius = 20,
        ShadowDepth = 0,
        Opacity = 0.4,
    };

    private bool _gettingBitMap;
    private bool _isClosing;

    public new event EventHandler? Closed;

    public NotificationWindow(SymbolRegular symbol, SymbolRegular? overlaySymbol, Action<SymbolIcon>? symbolTransform, string text, Action? clickAction, ScreenInfo screenInfo, NotificationPosition position)
    {
        InitializeStyle();
        InitializeContent(symbol, overlaySymbol, symbolTransform, text);

        _screenInfo = screenInfo;

        SourceInitialized += (_, _) => InitializePosition(screenInfo.WorkArea, screenInfo.DpiX, screenInfo.DpiY, position);
        MouseDown += (_, _) =>
        {
            Close();
            clickAction?.Invoke();
        };
    }

    public void Show(int closeAfter)
    {
        Opacity = 0;
        Show();

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fadeIn);

        Task.Delay(closeAfter).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() => Close(false));
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void Close(bool immediate)
    {
        if (_isClosing && !immediate)
            return;

        _isClosing = true;

        if (immediate)
        {
            base.Close();
            Closed?.Invoke(this, EventArgs.Empty);
            return;
        }

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            base.Close();
            Closed?.Invoke(this, EventArgs.Empty);
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    public Bitmap GetBitmapView()
    {
        _gettingBitMap = true;
        Show();
        _gettingBitMap = false;

        // Force layout update
        _container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        _container.Arrange(new Rect(new Size(Width, Height)));

        RenderTargetBitmap rtb = new((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(_container);

        base.Close();

        var ms = new MemoryStream();
        var encoder = new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        encoder.Save(ms);
        using var bitmap = new Bitmap(ms);

        var multiplierX = _screenInfo.DpiX / 96d;
        var multiplierY = _screenInfo.DpiY / 96d;
        var newWidth = (int)(bitmap.Width * multiplierX);
        var newHeight = (int)(bitmap.Height * multiplierY);
        var resizedBitmap = new Bitmap(newWidth, newHeight);
        using var graphics = Graphics.FromImage(resizedBitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var borderPath = GetRoundedRectanglePath(new(0, 0, newWidth, newHeight), 10);
        var penPath = GetRoundedRectanglePath(new(1, 1, newWidth - 3, newHeight - 3), 10);

        graphics.SetClip(borderPath);
        graphics.DrawImage(bitmap, 0, 0, newWidth, newHeight);
        graphics.ResetClip();

        var appTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();
        var borderColor = appTheme == ThemeType.Dark ? System.Drawing.Color.FromArgb(64, 64, 64) : System.Drawing.Color.FromArgb(200, 200, 200);

        using var pen = new System.Drawing.Pen(borderColor, 3);
        graphics.DrawPath(pen, penPath);

        return resizedBitmap;
    }

    private void InitializeStyle()
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        WindowBackdropType = BackgroundType.None;
        Background = System.Windows.Media.Brushes.Transparent;
        AllowsTransparency = true;

        Focusable = false;
        Topmost = true;
        ExtendsContentIntoTitleBar = true;
        ShowInTaskbar = false;
        ShowActivated = false;

        _mainGrid.FlowDirection = LocalizationHelper.Direction;
        _textBlock.Foreground = (SolidColorBrush)FindResource("TextFillColorPrimaryBrush");

        _container.Background = (SolidColorBrush)FindResource("ApplicationBackgroundBrush");
        _container.BorderBrush = (SolidColorBrush)FindResource("CardStrokeColorDefaultBrush");

        var appTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();
        _dropShadow.Color = appTheme == ThemeType.Dark ? Colors.Black : Color.FromArgb(64, 0, 0, 0);
        _container.Effect = _dropShadow;
    }

    private void InitializePosition(Rect workArea, uint dpiX, uint dpiY, NotificationPosition position)
    {
        _container.Measure(new Size(double.PositiveInfinity, 80));

        var multiplierX = dpiX / 96d;
        var multiplierY = dpiY / 96d;
        Rect nativeWorkArea = new(workArea.Left, workArea.Top, workArea.Width * multiplierX, workArea.Height * multiplierY);

        // Add padding for shadow
        const int SHADOW_PADDING = 20;
        Width = MaxWidth = MinWidth = Math.Max(_container.DesiredSize.Width, 300) + (SHADOW_PADDING * 2);
        Height = MaxHeight = MinHeight = _container.DesiredSize.Height + (SHADOW_PADDING * 2);

        double nativeLeft = 0;
        double nativeTop = 0;

        if (_gettingBitMap)
        {
            nativeLeft = -1048576;
            nativeTop = -1048576;
        }
        else
        {
            var nativeWidth = Width * multiplierX;
            var nativeHeight = Height * multiplierY;

            const int MARGIN = 16;
            var nativeMarginX = (MARGIN - SHADOW_PADDING) * multiplierX;
            var nativeMarginY = (MARGIN - SHADOW_PADDING) * multiplierY;

            switch (position)
            {
                case NotificationPosition.BottomRight:
                    nativeLeft = nativeWorkArea.Right - nativeWidth - nativeMarginX;
                    nativeTop = nativeWorkArea.Bottom - nativeHeight - nativeMarginY;
                    break;
                case NotificationPosition.BottomCenter:
                    nativeLeft = nativeWorkArea.Left + (nativeWorkArea.Width - nativeWidth) / 2;
                    nativeTop = nativeWorkArea.Bottom - nativeHeight - nativeMarginY;
                    break;
                case NotificationPosition.BottomLeft:
                    nativeLeft = nativeWorkArea.Left + nativeMarginX;
                    nativeTop = nativeWorkArea.Bottom - nativeHeight - nativeMarginY;
                    break;
                case NotificationPosition.CenterLeft:
                    nativeLeft = nativeWorkArea.Left + nativeMarginX;
                    nativeTop = nativeWorkArea.Top + (nativeWorkArea.Height - nativeHeight) / 2;
                    break;
                case NotificationPosition.TopLeft:
                    nativeLeft = nativeWorkArea.Left + nativeMarginX;
                    nativeTop = nativeWorkArea.Top + nativeMarginY;
                    break;
                case NotificationPosition.TopCenter:
                    nativeLeft = nativeWorkArea.Left + (nativeWorkArea.Width - nativeWidth) / 2;
                    nativeTop = nativeWorkArea.Top + nativeMarginY;
                    break;
                case NotificationPosition.TopRight:
                    nativeLeft = nativeWorkArea.Right - nativeWidth - nativeMarginX;
                    nativeTop = nativeWorkArea.Top + nativeMarginY;
                    break;
                case NotificationPosition.CenterRight:
                    nativeLeft = nativeWorkArea.Right - nativeWidth - nativeMarginX;
                    nativeTop = nativeWorkArea.Top + (nativeWorkArea.Height - nativeHeight) / 2;
                    break;
                case NotificationPosition.Center:
                    nativeLeft = nativeWorkArea.Left + (nativeWorkArea.Width - nativeWidth) / 2;
                    nativeTop = nativeWorkArea.Top + (nativeWorkArea.Height - nativeHeight) / 2;
                    break;
            }
        }

        var windowInteropHandler = new WindowInteropHelper(this);

        PInvoke.SetWindowPos((HWND)windowInteropHandler.Handle, HWND.Null, (int)nativeLeft, (int)nativeTop, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
    }

    private void InitializeContent(SymbolRegular symbol, SymbolRegular? overlaySymbol, Action<SymbolIcon>? symbolTransform, string text)
    {
        _symbolIcon.Symbol = symbol;
        _textBlock.Content = text;

        Grid.SetColumn(_symbolIcon, 0);
        Grid.SetColumn(_textBlock, 1);

        _mainGrid.Children.Add(_symbolIcon);
        _mainGrid.Children.Add(_textBlock);

        if (overlaySymbol.HasValue)
        {
            _overlaySymbolIcon.Symbol = overlaySymbol.Value;
            Grid.SetColumn(_overlaySymbolIcon, 0);
            _mainGrid.Children.Add(_overlaySymbolIcon);
        }

        symbolTransform?.Invoke(_symbolIcon);

        _container.Child = _mainGrid;
        _container.Margin = new Thickness(20); // Shadow space
        Content = _container;
    }

    private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
