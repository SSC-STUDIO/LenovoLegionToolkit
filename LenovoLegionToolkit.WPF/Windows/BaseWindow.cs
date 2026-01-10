using System.Windows;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Windows;

public class BaseWindow : UiWindow
{
    protected BaseWindow()
    {
        SnapsToDevicePixels = true;
        ExtendsContentIntoTitleBar = true;

        // Set initial backdrop type based on settings
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var backdropType = settings.Store.WindowBackdropStyle == WindowBackdropStyle.macOS 
            ? BackgroundType.Acrylic 
            : BackgroundType.Mica;
        WindowBackdropType = backdropType;
        
        // For macOS style, Acrylic background type provides dynamic blur effect
        // that adapts to background content and color changes without needing AllowsTransparency

        Loaded += BaseWindow_Loaded;
        DpiChanged += BaseWindow_DpiChanged;
    }

    private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure backdrop type is correct when window loads
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var backdropType = settings.Store.WindowBackdropStyle == WindowBackdropStyle.macOS 
            ? BackgroundType.Acrylic 
            : BackgroundType.Mica;
        WindowBackdropType = backdropType;
    }

    private void BaseWindow_DpiChanged(object sender, DpiChangedEventArgs e) => VisualTreeHelper.SetRootDpi(this, e.NewDpi);
}
