using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard
{
public partial class TurnOffMonitorsControl : AbstractRefreshingControl
{
    private readonly NativeWindowsMessageListener _nativeWindowsMessageListener = IoCContainer.Resolve<NativeWindowsMessageListener>();

    public TurnOffMonitorsControl() => InitializeComponent();

    private async void TurnOffButton_Click(object sender, RoutedEventArgs e)
    {
        _turnOffButton.IsEnabled = false;

        try
        {
            await _nativeWindowsMessageListener.TurnOffMonitorAsync().ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to turn off monitors.", ex);
        }
        finally
        {
            _turnOffButton.IsEnabled = true;
        }
    }

    protected override Task OnRefreshAsync() => Task.CompletedTask;

    protected override void OnFinishedLoading() { }
}
}
