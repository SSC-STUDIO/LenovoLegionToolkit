using System;
using System.ComponentModel;
using System.Windows;
using LenovoLegionToolkit.WPF.Controls.Packages;

namespace LenovoLegionToolkit.WPF.Pages.WindowsOptimization;

public class SelectedDriverPackageViewModel : INotifyPropertyChanged, IDisposable
{
    internal readonly PackageControl? _sourcePackageControl;

    public SelectedDriverPackageViewModel(
        string packageId,
        string title,
        string description,
        string category,
        PackageControl sourcePackageControl)
    {
        PackageId = packageId;
        Title = title;
        Description = description;
        Category = category;
        _sourcePackageControl = sourcePackageControl;
        _sourcePackageControl.PropertyChanged += SourcePackageControl_PropertyChanged;
    }

    public string PackageId { get; }
    public string Title { get; }
    public string Description { get; }
    public string Category { get; }

    public bool IsSelected
    {
        get
        {
            if (_sourcePackageControl is not null)
                return _sourcePackageControl.IsSelected;

            return false;
        }
        set
        {
            if (_sourcePackageControl is not null)
            {
                if (_sourcePackageControl.IsSelected == value)
                    return;

                _sourcePackageControl.IsSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public string StatusText
    {
        get
        {
            if (_sourcePackageControl is not null)
            {
                return _sourcePackageControl.Status switch
                {
                    PackageControl.PackageStatus.Downloading => "Downloading",
                    PackageControl.PackageStatus.Installing => "Installing",
                    PackageControl.PackageStatus.Completed => "Completed",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }
    }

    public bool IsCompleted
    {
        get
        {
            if (_sourcePackageControl is not null)
                return _sourcePackageControl.IsCompleted;
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        if (_sourcePackageControl is not null)
            _sourcePackageControl.PropertyChanged -= SourcePackageControl_PropertyChanged;
    }

    private void SourcePackageControl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PackageControl.IsSelected))
            OnPropertyChanged(nameof(IsSelected));
        else if (e.PropertyName == nameof(PackageControl.Status))
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsCompleted));

            if (_sourcePackageControl != null && _sourcePackageControl.Status == PackageControl.PackageStatus.Completed)
            {
                _sourcePackageControl.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
