using System;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Base window for all app windows. Ported from the WPF FluentWindow-based
/// BaseWindow: extends the content into the title bar area, applies the saved
/// backdrop type and software-rendering compatibility fallbacks, keeps maximized
/// windows inside the monitor work area and stabilizes live resize.
/// </summary>
public class BaseWindow : Window
{
    private bool _compatibilityMode;
    private readonly ScaleTransform _appScaleTransform = new();

    protected virtual Control? AppScaleTarget => Content as Control;

    protected BaseWindow()
    {
        ExtendClientAreaToDecorationsHint = true;

        // Set initial backdrop type based on settings
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        TransparencyLevelHint = new[] { RenderingCompatibilityHelper.GetPreferredBackgroundType(settings) };
        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);

        Loaded += BaseWindow_Loaded;
        Closed += BaseWindow_Closed;
        Deactivated += BaseWindow_Deactivated;
        ScalingChanged += BaseWindow_ScalingChanged;
        AppScaleManager.ScaleChanged += AppScaleManager_ScaleChanged;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var settings = IoCContainer.Resolve<ApplicationSettings>();
        _compatibilityMode = RenderingCompatibilityHelper.ShouldForceSoftwareRendering(settings);

        if (_compatibilityMode)
        {
            ExtendClientAreaToDecorationsHint = false;
            TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
            SystemDecorations = SystemDecorations.None;
            RenderingCompatibilityHelper.ApplyCompatibleWindowChrome(this);
        }

        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        RenderingCompatibilityHelper.ApplyBackdrop(this, TransparencyLevelHint[0], settings);
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, hwnd, settings);

        // Maximize to monitor work area (not full screen) so MyDockFinder Dock/Finder
        // and the taskbar are not covered and we are not treated as exclusive fullscreen.
        WindowMaximizeWorkAreaHelper.Attach(this);

        // Stabilize live resize (esp. top/left edges where Top+Height change together).
        WindowResizeStabilityHelper.Attach(this);
    }

    private void BaseWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        DpiAwareTypography.Apply(this);
        ApplyAppScale();

        // Ensure backdrop type is correct when window loads
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var backdropType = RenderingCompatibilityHelper.GetPreferredBackgroundType(settings);
        TransparencyLevelHint = new[] { backdropType };
        RenderingCompatibilityHelper.ApplyBackdrop(this, backdropType, settings);
        RenderingCompatibilityHelper.ApplyOpaqueWindowFallback(this, settings);
        RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(this, TryGetPlatformHandle()?.Handle ?? IntPtr.Zero, settings);
    }

    private void BaseWindow_Closed(object? sender, EventArgs e)
    {
        Loaded -= BaseWindow_Loaded;
        Closed -= BaseWindow_Closed;
        Deactivated -= BaseWindow_Deactivated;
        ScalingChanged -= BaseWindow_ScalingChanged;
        AppScaleManager.ScaleChanged -= AppScaleManager_ScaleChanged;
    }

    private void BaseWindow_Deactivated(object? sender, EventArgs e)
    {
        // Alt+Tab during a drag can skip WM_EXITSIZEMOVE. Do not leave a cached
        // client surface or a temporarily disabled backdrop behind after focus changes.
        WindowResizeStabilityHelper.RestoreIfNeeded(this);
    }

    private void AppScaleManager_ScaleChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
            ApplyAppScale();
        else
            Dispatcher.UIThread.Post(ApplyAppScale);
    }

    private void ApplyAppScale()
    {
        // Avalonia has no LayoutTransform; scale the content with a render transform
        // anchored at the top-left corner (visual parity with the WPF app-scale).
        _appScaleTransform.ScaleX = AppScaleManager.CurrentScale;
        _appScaleTransform.ScaleY = AppScaleManager.CurrentScale;

        if (AppScaleTarget is { } target && !ReferenceEquals(target.RenderTransform, _appScaleTransform))
        {
            target.RenderTransform = _appScaleTransform;
            target.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
        }
    }

    private void BaseWindow_ScalingChanged(object? sender, EventArgs e)
    {
        DpiAwareTypography.Apply(this);
    }
}
