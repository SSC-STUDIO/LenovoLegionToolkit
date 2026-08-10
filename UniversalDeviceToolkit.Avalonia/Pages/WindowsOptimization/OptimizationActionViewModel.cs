using System.ComponentModel;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages.WindowsOptimization;

public class OptimizationActionViewModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool? _isApplied;
    private bool _isEnabled = true;
    private bool _isVisible = true;
    private bool _canEdit = true;

    public OptimizationActionViewModel(
        WindowsOptimizationActionDefinition definition,
        string title,
        string description,
        string recommendedTagText,
        string? stateUnknownText = null)
    {
        Definition = definition;
        Key = definition.Key;
        Title = title;
        Description = description;
        Recommended = definition.Recommended;
        RecommendedTagText = recommendedTagText;
        StateUnknownText = stateUnknownText;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WindowsOptimizationActionDefinition Definition { get; }
    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    public bool Recommended { get; }
    public string? RecommendedTagText { get; }
    public string? StateUnknownText { get; }
    public OptimizationCategoryViewModel? Category { get; set; }
    public bool HasRecommendedTag => Recommended && !string.IsNullOrWhiteSpace(RecommendedTagText);
    public string? StateStatusText => IsStateKnown ? null : StateUnknownText;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(CheckState));
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    /// <summary>
    /// The state detected on the local machine. IsSelected is the user's pending
    /// target state and must not be used as the applied-state source of truth.
    /// </summary>
    public bool? IsApplied
    {
        get => _isApplied;
        set
        {
            if (_isApplied == value)
                return;

            _isApplied = value;
            OnPropertyChanged(nameof(IsApplied));
            OnPropertyChanged(nameof(CheckState));
            OnPropertyChanged(nameof(IsStateKnown));
            OnPropertyChanged(nameof(AllowsIndeterminate));
            OnPropertyChanged(nameof(StateStatusText));
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    /// <summary>
    /// Three-state value used by the WPF checkbox. An indeterminate value means
    /// the action has no reliable state probe or the probe failed.
    /// </summary>
    public bool? CheckState
    {
        get => IsApplied.HasValue ? IsSelected : null;
        set
        {
            if (value.HasValue)
                IsSelected = value.Value;
        }
    }

    public bool IsStateKnown => IsApplied.HasValue;
    public bool AllowsIndeterminate => !IsStateKnown;

    public bool IsDirty => IsApplied.HasValue && IsSelected != IsApplied.Value;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;

            _isEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    /// <summary>
    /// Indicates whether the user can change the pending target state. This is
    /// separate from <see cref="IsEnabled"/>, which describes whether the
    /// current machine state could be detected.
    /// </summary>
    public bool CanEdit
    {
        get => _canEdit;
        set
        {
            if (_canEdit == value)
                return;

            _canEdit = value;
            OnPropertyChanged(nameof(CanEdit));
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
                return;

            _isVisible = value;
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
