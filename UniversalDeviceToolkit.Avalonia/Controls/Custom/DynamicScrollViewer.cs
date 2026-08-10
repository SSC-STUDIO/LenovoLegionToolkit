using Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

/// <summary>
/// WPF-UI compatible scroll viewer with auto-hiding scroll bars. The scroll behavior and
/// visuals are provided by the styles in Styles/DynamicScrollBar.axaml; this class only
/// needs to exist so XAML/Styles can target it by type.
/// </summary>
public class DynamicScrollViewer : ScrollViewer
{
}
