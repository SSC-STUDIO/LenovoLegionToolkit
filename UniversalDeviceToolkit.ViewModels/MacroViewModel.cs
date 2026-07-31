using CommunityToolkit.Mvvm.ComponentModel;
using UniversalDeviceToolkit.Abstractions.Macro;

namespace UniversalDeviceToolkit.ViewModels;

public partial class MacroViewModel : ObservableObject
{
    private readonly IMacroController _controller;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private ulong _selectedKey;

    public MacroViewModel(IMacroController controller)
    {
        _controller = controller;
    }

    public IMacroController Controller => _controller;

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
