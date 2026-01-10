using System.ComponentModel;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

/// <summary>
/// View model interface for selected actions, used in SelectedActionsWindow
/// </summary>
public interface ISelectedActionViewModel : INotifyPropertyChanged
{
    string CategoryKey { get; }
    string CategoryTitle { get; }
    string ActionKey { get; }
    string ActionTitle { get; }
    string Description { get; }
    bool IsSelected { get; set; }
    bool IsEnabled { get; }
    object? Tag { get; set; }
}





