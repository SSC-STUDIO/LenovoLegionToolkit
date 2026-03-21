using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Windows;

public class BaseWindow : UiWindow
{
    private bool _compatibilityMode;
    private bool _suppressUiWindowCallbacks;

    protected BaseWindow()
    {
        SnapsToDevicePixels = true;
        ExtendsContentIntoTitleBar = true;

        // Set initial backdrop type based on settings
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var backdropType = RenderingCompatibilityHelper.GetPreferredBackgroundType(settings);
        WindowBackdropType = backdropType;
        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);
        
        // For macOS style, Acrylic background type provides dynamic blur effect
        // that adapts to background content and color changes without needing AllowsTransparency

        Loaded += BaseWindow_Loaded;
        DpiChanged += BaseWindow_DpiChanged;
    }

    protected override void OnSourceInitialized(System.EventArgs e)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        _compatibilityMode = RenderingCompatibilityHelper.ShouldForceSoftwareRendering(settings);

        if (_compatibilityMode)
        {
            var originalExtendsContentIntoTitleBar = ExtendsContentIntoTitleBar;
            var originalWindowBackdropType = WindowBackdropType;

            _suppressUiWindowCallbacks = true;
            ExtendsContentIntoTitleBar = false;
            WindowBackdropType = BackgroundType.None;
            WindowStyle = WindowStyle.None;
            _suppressUiWindowCallbacks = false;

            base.OnSourceInitialized(e);

            _suppressUiWindowCallbacks = true;
            ExtendsContentIntoTitleBar = originalExtendsContentIntoTitleBar;
            WindowBackdropType = originalWindowBackdropType;
            _suppressUiWindowCallbacks = false;

            RenderingCompatibilityHelper.ApplyCompatibleWindowChrome(this);
        }
        else
        {
            base.OnSourceInitialized(e);
        }

        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, hwndSource, settings);
    }

    private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure backdrop type is correct when window loads
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var backdropType = RenderingCompatibilityHelper.GetPreferredBackgroundType(settings);
        WindowBackdropType = backdropType;
        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, PresentationSource.FromVisual(this) as HwndSource, settings);
    }

    protected override void OnBackdropTypeChanged(BackgroundType oldValue, BackgroundType newValue)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        if (!_suppressUiWindowCallbacks && !RenderingCompatibilityHelper.ShouldForceSoftwareRendering(settings))
            base.OnBackdropTypeChanged(oldValue, newValue);

        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, PresentationSource.FromVisual(this) as HwndSource, settings);
    }

    protected override void OnExtendsContentIntoTitleBarChanged(bool oldValue, bool newValue)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        if (!_suppressUiWindowCallbacks && !RenderingCompatibilityHelper.ShouldForceSoftwareRendering(settings))
            base.OnExtendsContentIntoTitleBarChanged(oldValue, newValue);
        else
            RenderingCompatibilityHelper.ApplyCompatibleWindowChrome(this);

        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, PresentationSource.FromVisual(this) as HwndSource, settings);
    }

    private void BaseWindow_DpiChanged(object sender, DpiChangedEventArgs e) => VisualTreeHelper.SetRootDpi(this, e.NewDpi);
}
