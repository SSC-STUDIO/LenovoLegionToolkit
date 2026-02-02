using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Pages.WindowsOptimization;

public class CustomCleanupRuleViewModel : INotifyPropertyChanged
{
    private string _directoryPath = string.Empty;
    private bool _recursive = false;

    public CustomCleanupRuleViewModel(string directoryPath, IEnumerable<string> extensions, bool recursive)
    {
        DirectoryPath = directoryPath;
        Extensions = new ObservableCollection<string>(extensions);
        Recursive = recursive;
        Extensions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ExtensionsDisplay));
    }

    public string DirectoryPath
    {
        get => _directoryPath;
        set
        {
            if (_directoryPath == value)
                return;
            _directoryPath = value;
            OnPropertyChanged(nameof(DirectoryPath));
        }
    }

    public ObservableCollection<string> Extensions { get; }

    public bool Recursive
    {
        get => _recursive;
        set
        {
            if (_recursive == value)
                return;
            _recursive = value;
            OnPropertyChanged(nameof(Recursive));
        }
    }

    public string ExtensionsDisplay =>
        Extensions.Count == 0
            ? Resource.ResourceManager.GetString("CustomCleanupRule_NoExtensions") ?? "No extensions specified"
            : string.Join(", ", Extensions);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyExtensionsChanged() => OnPropertyChanged(nameof(ExtensionsDisplay));

    public CustomCleanupRule ToModel() => new()
    {
        DirectoryPath = DirectoryPath,
        Recursive = Recursive,
        Extensions = Extensions.ToList()
    };

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
