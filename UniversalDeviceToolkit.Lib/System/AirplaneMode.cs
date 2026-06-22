using System.Diagnostics;

namespace LenovoLegionToolkit.Lib.System;

public static class AirplaneMode
{
    public static void Open()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:network-airplanemode",
            UseShellExecute = true,
            Verb = "open",
        });
    }
}
