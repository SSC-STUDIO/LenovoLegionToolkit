using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

/// <summary>
/// WPF-UI compatible items control that virtualizes its items by default: the items panel
/// defaults to a <see cref="VirtualizingStackPanel"/>.
/// </summary>
public class VirtualizingItemsControl : ItemsControl
{
    private static readonly FuncTemplate<Panel> DefaultItemsPanel =
        new(() => new VirtualizingStackPanel());

    static VirtualizingItemsControl()
    {
        ItemsPanelProperty.OverrideDefaultValue<VirtualizingItemsControl>(DefaultItemsPanel);
    }
}
