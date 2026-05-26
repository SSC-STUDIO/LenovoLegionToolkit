using CommunityToolkit.Mvvm.ComponentModel;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Macro;

namespace UniversalDeviceToolkit.WPF.ViewModels;

public partial class MacroViewModel : ObservableObject
{
    private readonly MacroController _controller;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private ulong _selectedKey;

    public MacroViewModel(MacroController controller)
    {
        _controller = controller;
    }

    public MacroController Controller => _controller;

    public void LoadState()
    {
        IsEnabled = _controller.IsEnabled;
    }

    public void SetEnabled(bool enabled)
    {
        _controller.SetEnabled(enabled);
        IsEnabled = enabled;
    }

    public void SelectKey(ulong key)
    {
        SelectedKey = key;
    }
}
