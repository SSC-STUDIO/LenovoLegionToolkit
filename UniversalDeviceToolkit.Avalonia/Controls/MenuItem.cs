using Avalonia;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// WPF-UI compatible menu item. <see cref="Avalonia.Controls.MenuItem.Icon"/> is inherited
/// from the Avalonia base; this subclass only adds the WPF-UI style string-to-icon
/// conversion for <c>Icon="SymbolName"</c> attribute values.
/// </summary>
public class MenuItem : global::Avalonia.Controls.MenuItem
{
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
            IconHelper.TryConvertStringIcon(this, IconProperty, change.NewValue);
    }
}
