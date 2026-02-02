using System;
using System.ComponentModel;
using LenovoLegionToolkit.WPF.Windows.Utils;

namespace LenovoLegionToolkit.WPF.Pages.WindowsOptimization;

public class SelectedActionViewModel : ISelectedActionViewModel, IDisposable
{
    private readonly OptimizationActionViewModel? _sourceAction;
    private bool _isSelected;

    public SelectedActionViewModel(
        string categoryKey,
        string categoryTitle,
        string actionKey,
        string actionTitle,
        string description,
        OptimizationActionViewModel? sourceAction)
    {
        CategoryKey = categoryKey;
        CategoryTitle = categoryTitle;
        ActionKey = actionKey;
        ActionTitle = actionTitle;
        Description = description;
        _sourceAction = sourceAction;
        if (_sourceAction is not null)
            _sourceAction.PropertyChanged += SourceAction_PropertyChanged;
    }

    public string CategoryKey { get; }
    public string CategoryTitle { get; }
    public string ActionKey { get; }
    public string ActionTitle { get; }
    public string Description { get; }
    public object? Tag { get; set; }

    public bool IsEnabled
    {
        get
        {
            if (_sourceAction is null && Tag is SelectedDriverPackageViewModel driverPackage)
            {
                return !driverPackage.IsCompleted;
            }
            return true;
        }
    }

    public bool IsSelected
    {
        get
        {
            if (_sourceAction is not null)
                return _sourceAction.IsSelected;

            return _isSelected;
        }
        set
        {
            if (_sourceAction is not null)
            {
                if (_sourceAction.IsSelected == value)
                    return;

                // Setting source property will trigger SourceAction_PropertyChanged,
                // which already calls OnPropertyChanged(nameof(IsSelected)).
                // No need to call it here to avoid double notification and recursion.
                _sourceAction.IsSelected = value;
            }
            else
            {
                if (_isSelected == value)
                    return;

                if (!value && Tag is SelectedDriverPackageViewModel driverPackage)
                {
                    if (driverPackage.IsCompleted)
                    {
                        _isSelected = true;
                        OnPropertyChanged(nameof(IsSelected));
                        return;
                    }

                    if (driverPackage._sourcePackageControl != null)
                    {
                        driverPackage._sourcePackageControl.IsSelected = false;
                    }
                }
                else if (value && Tag is SelectedDriverPackageViewModel driverPackageSelected)
                {
                    if (driverPackageSelected._sourcePackageControl != null)
                    {
                        driverPackageSelected._sourcePackageControl.IsSelected = true;
                    }
                }
                
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SourceAction_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptimizationActionViewModel.IsSelected))
            OnPropertyChanged(nameof(IsSelected));
    }

    public void Dispose()
    {
        if (_sourceAction is not null)
            _sourceAction.PropertyChanged -= SourceAction_PropertyChanged;
    }

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
