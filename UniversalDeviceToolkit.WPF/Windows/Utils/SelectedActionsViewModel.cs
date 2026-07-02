using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

namespace UniversalDeviceToolkit.WPF.Windows.Utils;

public class SelectedActionsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ObservableCollection<SelectedActionViewModel> _selectedActions;

    public SelectedActionsViewModel(ObservableCollection<SelectedActionViewModel> selectedActions, string emptyText)
    {
        _selectedActions = selectedActions ?? throw new ArgumentNullException(nameof(selectedActions));
        EmptyText = emptyText ?? string.Empty;

        _selectedActions.CollectionChanged += SelectedActions_CollectionChanged;
        foreach (var action in _selectedActions)
            action.PropertyChanged += Action_PropertyChanged;

        UpdateEmptyState();
    }

    public ObservableCollection<SelectedActionViewModel> SelectedActions => _selectedActions;

    public string EmptyText { get; }

    public bool HasItems => _selectedActions.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SelectedActions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (SelectedActionViewModel action in e.OldItems)
                action.PropertyChanged -= Action_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (SelectedActionViewModel action in e.NewItems)
                action.PropertyChanged += Action_PropertyChanged;
        }

        UpdateEmptyState();
    }

    private void Action_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedActionViewModel.IsSelected))
            UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        OnPropertyChanged(nameof(HasItems));
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        _selectedActions.CollectionChanged -= SelectedActions_CollectionChanged;
        foreach (var action in _selectedActions)
            action.PropertyChanged -= Action_PropertyChanged;

        GC.SuppressFinalize(this);
    }
}
