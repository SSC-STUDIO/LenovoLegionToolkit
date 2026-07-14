using System.ComponentModel;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

public class OptimizationActionViewModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isEnabled = true;
    private bool _isVisible = true;

    public OptimizationActionViewModel(WindowsOptimizationActionDefinition definition, string title, string description, string recommendedTagText)
    {
        Definition = definition;
        Key = definition.Key;
        Title = title;
        Description = description;
        Recommended = definition.Recommended;
        RecommendedTagText = recommendedTagText;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WindowsOptimizationActionDefinition Definition { get; }
    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    public bool Recommended { get; }
    public string? RecommendedTagText { get; }
    public OptimizationCategoryViewModel? Category { get; set; }
    public bool HasRecommendedTag => Recommended && !string.IsNullOrWhiteSpace(RecommendedTagText);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

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
