using System.Threading.Tasks;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard
{
public partial class TurnOffMonitorsControl : global::UniversalDeviceToolkit.Avalonia.Controls.AbstractRefreshingControl
{
    private readonly NativeWindowsMessageListener _nativeWindowsMessageListener = IoCContainer.Resolve<NativeWindowsMessageListener>();

    public TurnOffMonitorsControl() => InitializeComponent();

    private async void TurnOffButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _turnOffButton.IsEnabled = false;

            await _nativeWindowsMessageListener.TurnOffMonitorAsync();
        }
        catch (System.Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to turn off monitors.", ex);
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
