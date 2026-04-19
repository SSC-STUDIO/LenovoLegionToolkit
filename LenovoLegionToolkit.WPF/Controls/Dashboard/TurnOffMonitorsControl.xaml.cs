using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.WPF.Controls;

namespace LenovoLegionToolkit.WPF.Controls.Dashboard
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
            await _nativeWindowsMessageListener.TurnOffMonitorAsync();
        }
        catch (System.Exception ex)
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"Failed to turn off monitors.", ex);
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
