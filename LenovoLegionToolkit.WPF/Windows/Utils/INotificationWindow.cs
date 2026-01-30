using System;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public interface INotificationWindow
{
    public event EventHandler Closed;
    public void Show(int closeAfter);
    public void Close(bool immediate);
}
