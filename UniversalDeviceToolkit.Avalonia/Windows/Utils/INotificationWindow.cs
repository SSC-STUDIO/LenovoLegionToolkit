using System;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils;

public interface INotificationWindow
{
    public event EventHandler Closed;
    public void Show(int closeAfter);
    public void Close(bool immediate);
}
