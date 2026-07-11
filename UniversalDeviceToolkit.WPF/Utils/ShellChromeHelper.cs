using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using LenovoLegionToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class ShellChromeHelper
{
    public static void ApplyContentSurfaceEffects(Border contentSurface, ApplicationSettings settings)
    {
        if (RenderingCompatibilityHelper.ShouldDisableBackdrop(settings))
        {
            contentSurface.Effect = null;
            return;
        }

        if (Application.Current.TryFindResource("ContentSurfaceDividerShadowEffect") is DropShadowEffect effect)
            contentSurface.Effect = effect;
    }
}
