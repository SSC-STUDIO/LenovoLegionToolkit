using Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage(IPlatformServices platformServices)
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel(platformServices);
    }
}