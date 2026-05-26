using System;

namespace UniversalDeviceToolkit.WPF.Windows.Utils;

public interface INotificationWindow
{
    public event EventHandler Closed;
    public void Show(int closeAfter);
    public void Close(bool immediate);
}
