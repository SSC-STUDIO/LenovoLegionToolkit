using System;
using LenovoLegionToolkit.Lib;
using System.Windows.Forms;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using Wpf.Ui.Appearance;

namespace LenovoLegionToolkit.WPF.Utils;

internal static class RenderingCompatibilityHelper
{
    public static BackgroundType GetPreferredBackgroundType(ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings))
            return BackgroundType.None;

        return settings?.Store.WindowBackdropStyle == WindowBackdropStyle.macOS
            ? BackgroundType.Acrylic
            : BackgroundType.Mica;
    }

    public static bool ShouldDisableBackdrop(ApplicationSettings? settings = null)
    {
        try
        {
            if (settings?.Store.ForceSoftwareRendering == true)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Window backdrop disabled because software rendering is forced.");

                return true;
            }

            if (SystemInformation.TerminalServerSession)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Remote desktop session detected. Window backdrop disabled.");

                return true;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to determine window-backdrop compatibility; disabling backdrop.", ex);

            return true;
        }

        return false;
    }
}
