using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Color = Avalonia.Media.Color;
using Colors = Avalonia.Media.Colors;
using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;
using Size = Avalonia.Size;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils;

public class NotificationWindow : BaseWindow, INotificationWindow
{
    private readonly ScreenInfo _screenInfo;

    private readonly Border _container = new()
    {
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
        Margin = new(10, 10, 16, 10),
    };

    private readonly Border _iconChrome = new()
    {
        Width = 36,
        Height = 36,
        Margin = new(0, 0, 10, 0),
        CornerRadius = ResolveCornerRadius("CornerRadiusControl", 12),
    };

    private readonly SymbolIcon _symbolIcon = new()
    {
        FontSize = 18,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly SymbolIcon _overlaySymbolIcon = new()
    {
        FontSize = 18,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly TextBlock _textBlock = new()
    {
        FontSize = 15,
        FontWeight = FontWeight.Medium,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly BoxShadow _dropShadow = new()
    {
        Blur = 24,
        OffsetY = 6,
        Color = Color.FromArgb(82, 0, 0, 0),
    };

    private bool _gettingBitMap;
    private bool _isClosing;

    public new event EventHandler? Closed;

    public NotificationWindow(SymbolRegular symbol, SymbolRegular? overlaySymbol, Action<SymbolIcon>? symbolTransform, string text, Action? clickAction, ScreenInfo screenInfo, NotificationPosition position)
    {
        InitializeStyle();
        InitializeContent(symbol, overlaySymbol, symbolTransform, text);

        _screenInfo = screenInfo;

        Opened += (_, _) => InitializePosition(screenInfo.WorkArea, screenInfo.DpiX, screenInfo.DpiY, position);
        PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            Close();
            clickAction?.Invoke();
        };
    }

    public void Show(int closeAfter)
    {
        Show(closeAfter, null);
    }

    public void Show(int closeAfter, Window? owner)
    {
        Opacity = 0;
        if (owner is not null)
            Show(owner);
        else
            Show();

        RunOpacityAnimation(this, 0, 1, TimeSpan.FromMilliseconds(300), new CubicEaseOut());

        Task.Delay(closeAfter).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() => Close(false));
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

        var fadeOut = RunOpacityAnimation(this, 1, 0, TimeSpan.FromMilliseconds(300), new CubicEaseIn());
        fadeOut.ContinueWith(_ =>
        {
            base.Close();
            Closed?.Invoke(this, EventArgs.Empty);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public Bitmap GetBitmapView()
    {
        _gettingBitMap = true;
        Show();
        _gettingBitMap = false;

        // Force layout update
        _container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        _container.Arrange(new Rect(new Size(Width, Height)));

        var rtb = new RenderTargetBitmap(new PixelSize(Math.Max(1, (int)Width), Math.Max(1, (int)Height)), new Vector(96, 96));
        rtb.Render(_container);

        base.Close();

        using var ms = new MemoryStream();
        rtb.Save(ms);
        ms.Position = 0;
        using var bitmap = new Bitmap(ms);

        var multiplierX = _screenInfo.DpiX / 96d;
        var multiplierY = _screenInfo.DpiY / 96d;
        var newWidth = (int)(bitmap.Width * multiplierX);
        var newHeight = (int)(bitmap.Height * multiplierY);
        var resizedBitmap = new Bitmap(newWidth, newHeight);
        using var graphics = Graphics.FromImage(resizedBitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var borderPath = GetRoundedRectanglePath(new(0, 0, newWidth, newHeight), 18);
        using var penPath = GetRoundedRectanglePath(new(1, 1, newWidth - 3, newHeight - 3), 18);

        graphics.SetClip(borderPath);
        graphics.DrawImage(bitmap, 0, 0, newWidth, newHeight);
        graphics.ResetClip();

            var isDark = Application.Current?.RequestedThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark;
        var borderColor = isDark
            ? System.Drawing.Color.FromArgb(54, 255, 255, 255)
            : System.Drawing.Color.FromArgb(31, 0, 0, 0);

        using var pen = new System.Drawing.Pen(borderColor, 3);
        graphics.DrawPath(pen, penPath);

        return resizedBitmap;
    }

    private void InitializeStyle()
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = global::Avalonia.Media.Brushes.Transparent;

        Focusable = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;

        _mainGrid.FlowDirection = LocalizationHelper.Direction;
        _textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        _container.CornerRadius = ResolveCornerRadius("CornerRadiusCard", 18);
        _container.SetResourceReference(Border.BackgroundProperty, "NotificationGlassSurfaceBrush");
        _container.SetResourceReference(Border.BorderBrushProperty, "NotificationGlassBorderBrush");
        _iconChrome.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        _iconChrome.CornerRadius = ResolveCornerRadius("CornerRadiusControl", 12);

        if (Application.Current?.TryFindResource("NotificationGlassShadowEffect", out var shadowValue) == true && shadowValue is BoxShadows glassShadow)
        {
            _container.BoxShadow = glassShadow;
        }
        else
        {
        var isDark = Application.Current?.RequestedThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark;
            var shadow = new BoxShadow
            {
                Blur = _dropShadow.Blur,
                OffsetY = _dropShadow.OffsetY,
                Color = isDark ? Colors.Black : Color.FromArgb(64, 0, 0, 0),
            };
            _container.BoxShadow = new BoxShadows(shadow);
        }
    }

    private void InitializePosition(Rect workArea, uint dpiX, uint dpiY, NotificationPosition position)
    {
        _container.Measure(new Size(double.PositiveInfinity, 80));

        var multiplierX = dpiX / 96d;
        var multiplierY = dpiY / 96d;
        Rect nativeWorkArea = new(workArea.Left, workArea.Top, workArea.Width * multiplierX, workArea.Height * multiplierY);

        // Add padding for shadow
        const int SHADOW_PADDING = 20;
        Width = MaxWidth = MinWidth = Math.Max(_container.DesiredSize.Width, 360) + (SHADOW_PADDING * 2);
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

        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd != IntPtr.Zero)
        {
            PInvoke.SetWindowPos((HWND)hwnd, HWND.Null, (int)nativeLeft, (int)nativeTop, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        }
    }

    private void InitializeContent(SymbolRegular symbol, SymbolRegular? overlaySymbol, Action<SymbolIcon>? symbolTransform, string text)
    {
        _symbolIcon.Symbol = symbol;
        _textBlock.Text = text;
        AutomationProperties.SetName(this, text);

        if (overlaySymbol.HasValue)
        {
            _overlaySymbolIcon.Symbol = overlaySymbol.Value;
            var overlayHost = new Grid();
            overlayHost.Children.Add(_symbolIcon);
            overlayHost.Children.Add(_overlaySymbolIcon);
            _iconChrome.Child = overlayHost;
        }
        else
        {
            _iconChrome.Child = _symbolIcon;
        }

        Grid.SetColumn(_iconChrome, 0);
        Grid.SetColumn(_textBlock, 1);

        _mainGrid.Children.Add(_iconChrome);
        _mainGrid.Children.Add(_textBlock);

        symbolTransform?.Invoke(_symbolIcon);

        _container.Child = _mainGrid;
        _container.Margin = new Thickness(20); // Shadow space
        Content = _container;
    }

    private static CornerRadius ResolveCornerRadius(string resourceKey, double fallback)
    {
        if (Application.Current?.TryFindResource(resourceKey, out var value) == true && value is CornerRadius radius)
            return radius;
        return new CornerRadius(fallback);
    }

    private static Task RunOpacityAnimation(Visual target, double from, double to, TimeSpan duration, Easing easing)
    {
        var animation = new Animation
        {
            Duration = duration,
            Easing = easing,
            FillMode = global::Avalonia.Animation.FillMode.Both,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, from) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, to) } },
            }
        };
        return animation.RunAsync(target);
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
