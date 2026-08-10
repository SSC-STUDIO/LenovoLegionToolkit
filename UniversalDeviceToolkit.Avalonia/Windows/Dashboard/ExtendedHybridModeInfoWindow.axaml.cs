using System.Linq;
using Avalonia;
using Avalonia.Interactivity;

namespace UniversalDeviceToolkit.Avalonia.Windows.Dashboard
{
public partial class ExtendedHybridModeInfoWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    public ExtendedHybridModeInfoWindow(HybridModeState[] hybridModeStates)
    {
        InitializeComponent();

        _hybridPanel.IsVisible = hybridModeStates.Contains(HybridModeState.On)
            ? true
            : false;
        _hybridIgpuPanel.IsVisible = hybridModeStates.Contains(HybridModeState.OnIGPUOnly)
            ? true
            : false;
        _hybridAutoPanel.IsVisible = hybridModeStates.Contains(HybridModeState.OnAuto)
            ? true
            : false;
        _dgpuPanel.IsVisible = hybridModeStates.Contains(HybridModeState.Off)
            ? true
            : false;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
}
