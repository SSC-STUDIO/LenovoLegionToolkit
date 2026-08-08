using System;
using System.ComponentModel;
using UniversalDeviceToolkit.WPF.Controls.Packages;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

public class SelectedDriverPackageViewModel : INotifyPropertyChanged, IDisposable
{
    internal PackageControl? _sourcePackageControl;

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
        AttachSource(sourcePackageControl);
    }

    public string PackageId { get; }
    public string Title { get; }
    public string Description { get; }
    public string Category { get; }

    public void AttachSource(PackageControl sourcePackageControl)
    {
        if (ReferenceEquals(_sourcePackageControl, sourcePackageControl))
            return;

        if (_sourcePackageControl is not null)
            _sourcePackageControl.PropertyChanged -= SourcePackageControl_PropertyChanged;

        _sourcePackageControl = sourcePackageControl;
        _sourcePackageControl.PropertyChanged += SourcePackageControl_PropertyChanged;

        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsCompleted));
    }

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
                    PackageControl.PackageStatus.Queued => Resource.ResourceManager.GetString("PackageControl_Queued") ?? "Queued",
                    PackageControl.PackageStatus.NotStarted when _sourcePackageControl.IsSelected => Resource.ResourceManager.GetString("PackageControl_Queued") ?? "Queued",
                    PackageControl.PackageStatus.Downloading => Resource.PackageControl_Downloading,
                    PackageControl.PackageStatus.Installing => Resource.PackageControl_Installing,
                    PackageControl.PackageStatus.Completed => Resource.PackageControl_Completed,
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
        {
            _sourcePackageControl.PropertyChanged -= SourcePackageControl_PropertyChanged;
            _sourcePackageControl = null;
        }
    }

    private void SourcePackageControl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PackageControl.IsSelected))
            OnPropertyChanged(nameof(IsSelected));
        else if (e.PropertyName == nameof(PackageControl.Status))
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsCompleted));
        }
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
