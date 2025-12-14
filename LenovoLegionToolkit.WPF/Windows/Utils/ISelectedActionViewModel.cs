using System.ComponentModel;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

/// <summary>
/// 选中操作的视图模型接口，用于 SelectedActionsWindow
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
