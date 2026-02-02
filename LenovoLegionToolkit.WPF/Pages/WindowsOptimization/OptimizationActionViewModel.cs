using System.ComponentModel;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.WPF.Pages.WindowsOptimization;

public class OptimizationActionViewModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isEnabled = true;

    public OptimizationActionViewModel(string key, string title, string description, bool recommended, string recommendedTagText)
    {
        Key = key;
        Title = title;
        Description = description;
        Recommended = recommended;
        RecommendedTagText = recommendedTagText;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
