using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Windows;

public class BaseWindow : FluentWindow
{
    private bool _compatibilityMode;
    private bool _suppressFluentWindowCallbacks;
    private readonly ScaleTransform _appScaleTransform = new();

    protected virtual FrameworkElement? AppScaleTarget => Content as FrameworkElement;

    protected BaseWindow()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        ExtendsContentIntoTitleBar = true;

        // Set initial backdrop type based on settings
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var backdropType = RenderingCompatibilityHelper.GetPreferredBackgroundType(settings);
        WindowBackdropType = backdropType;
        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);
        
        // For macOS style, Acrylic background type provides dynamic blur effect
        // that adapts to background content and color changes without needing AllowsTransparency

        Loaded += BaseWindow_Loaded;
        Closed += BaseWindow_Closed;
        DpiChanged += BaseWindow_DpiChanged;
        AppScaleManager.ScaleChanged += AppScaleManager_ScaleChanged;
    }

    protected override void OnSourceInitialized(System.EventArgs e)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        _compatibilityMode = RenderingCompatibilityHelper.ShouldForceSoftwareRendering(settings);

        if (_compatibilityMode)
        {
            var originalExtendsContentIntoTitleBar = ExtendsContentIntoTitleBar;
            var originalWindowBackdropType = WindowBackdropType;

            _suppressFluentWindowCallbacks = true;
            ExtendsContentIntoTitleBar = false;
            WindowBackdropType = WindowBackdropType.None;
            WindowStyle = WindowStyle.None;
            _suppressFluentWindowCallbacks = false;

            base.OnSourceInitialized(e);

            _suppressFluentWindowCallbacks = true;
            ExtendsContentIntoTitleBar = originalExtendsContentIntoTitleBar;
            WindowBackdropType = originalWindowBackdropType;
            _suppressFluentWindowCallbacks = false;

            RenderingCompatibilityHelper.ApplyCompatibleWindowChrome(this);
        }
        else
        {
            base.OnSourceInitialized(e);
        }

        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, hwndSource, settings);

        // Maximize to monitor work area (not full screen) so MyDockFinder Dock/Finder
        // and the taskbar are not covered and we are not treated as exclusive fullscreen.
        WindowMaximizeWorkAreaHelper.Attach(this);

        // Stabilize live resize (esp. top/left edges where Top+Height change together).
        WindowResizeStabilityHelper.Attach(this);
    }

    private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DpiAwareTypography.Apply(this);
        ApplyAppScale();

        // Ensure backdrop type is correct when window loads
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var backdropType = RenderingCompatibilityHelper.GetPreferredBackgroundType(settings);
        WindowBackdropType = backdropType;
        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, PresentationSource.FromVisual(this) as HwndSource, settings);
    }

    protected override void OnBackdropTypeChanged(WindowBackdropType oldValue, WindowBackdropType newValue)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        if (!_suppressFluentWindowCallbacks && !RenderingCompatibilityHelper.ShouldForceSoftwareRendering(settings))
            base.OnBackdropTypeChanged(oldValue, newValue);

        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, PresentationSource.FromVisual(this) as HwndSource, settings);
    }

    protected override void OnExtendsContentIntoTitleBarChanged(bool oldValue, bool newValue)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        if (!_suppressFluentWindowCallbacks && !RenderingCompatibilityHelper.ShouldForceSoftwareRendering(settings))
            base.OnExtendsContentIntoTitleBarChanged(oldValue, newValue);
        else
            RenderingCompatibilityHelper.ApplyCompatibleWindowChrome(this);

        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, PresentationSource.FromVisual(this) as HwndSource, settings);
    }

    private void BaseWindow_Closed(object? sender, System.EventArgs e)
    {
        Loaded -= BaseWindow_Loaded;
        Closed -= BaseWindow_Closed;
        DpiChanged -= BaseWindow_DpiChanged;
        AppScaleManager.ScaleChanged -= AppScaleManager_ScaleChanged;
    }

    private void AppScaleManager_ScaleChanged(object? sender, System.EventArgs e)
    {
        if (Dispatcher.CheckAccess())
            ApplyAppScale();
        else
            Dispatcher.BeginInvoke(ApplyAppScale);
    }

    private void ApplyAppScale()
    {
        _appScaleTransform.ScaleX = AppScaleManager.CurrentScale;
        _appScaleTransform.ScaleY = AppScaleManager.CurrentScale;

        if (AppScaleTarget is { } target && !ReferenceEquals(target.LayoutTransform, _appScaleTransform))
            target.LayoutTransform = _appScaleTransform;
    }

    private void BaseWindow_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        VisualTreeHelper.SetRootDpi(this, e.NewDpi);
        DpiAwareTypography.Apply(Resources, e.NewDpi.DpiScaleX);
    }
}
