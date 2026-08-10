using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;

namespace UniversalDeviceToolkit.Avalonia.Pages.WindowsOptimization;

public class OptimizationCategoryViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _isEnabled = true;
    private bool _isExpanded = false;
    private readonly string _selectionSummaryFormat;
    private readonly PropertyChangedEventHandler _actionPropertyChangedHandler;

    public OptimizationCategoryViewModel(string key, string title, string description, string selectionSummaryFormat, IEnumerable<OptimizationActionViewModel> actions, string? pluginId = null)
    {
        Key = key;
        Title = title;
        Description = description;
        PluginId = pluginId;
        _selectionSummaryFormat = string.IsNullOrWhiteSpace(selectionSummaryFormat) ? "{0} / {1}" : selectionSummaryFormat;
        Actions = new ObservableCollection<OptimizationActionViewModel>(actions);

        // Check if the plugin has a settings page
        if (!string.IsNullOrEmpty(PluginId))
        {
            try
            {
                var pluginManager = IoCContainer.Resolve<IPluginManager>();
                var plugin = pluginManager.GetRegisteredPlugins().FirstOrDefault(p => p.Id == PluginId);
                if (plugin is PluginBase pluginBase)
                {
                    HasSettings = pluginBase.GetSettingsPage() != null;
                }

                if (!HasSettings)
                {
                    var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(PluginId);
                    HasSettings = capabilities.SupportsSettingsPage;
                }
            }
            catch
            {
                HasSettings = false;
            }
        }

        _actionPropertyChangedHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(OptimizationActionViewModel.IsSelected)
                || args.PropertyName == nameof(OptimizationActionViewModel.IsEnabled)
                || args.PropertyName == nameof(OptimizationActionViewModel.IsVisible)
                || args.PropertyName == nameof(OptimizationActionViewModel.CanEdit))
            {
                RaiseSelectionChanged();
            }
        };

        foreach (var action in Actions)
            action.PropertyChanged += _actionPropertyChangedHandler;

        RaiseSelectionChanged();
    }

    public void Dispose()
    {
        foreach (var action in Actions)
            action.PropertyChanged -= _actionPropertyChangedHandler;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SelectionChanged;

    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    public string? PluginId { get; }
    public bool HasSettings { get; }
    public ObservableCollection<OptimizationActionViewModel> Actions { get; }
    
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
                
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    public int VisibleActionCount => Actions.Count(action => action.IsVisible);
    public int SelectedActionCount => Actions.Count(action => action.IsVisible && action.IsSelected);
    public string SelectionSummary => string.Format(_selectionSummaryFormat, SelectedActionCount, VisibleActionCount);

    public bool IsEnabled
    {
        get => _isEnabled;
        private set
        {
            if (_isEnabled == value)
                return;

            _isEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    public bool? HeaderCheckState
    {
        get
        {
            var enabledActions = Actions.Where(action => action.IsEnabled && action.IsVisible && action.CanEdit).ToList();
            if (enabledActions.Count == 0)
                return false;

            var selectedCount = enabledActions.Count(action => action.IsSelected);
            if (selectedCount == 0)
                return false;

            if (selectedCount == enabledActions.Count)
                return true;

            return null;
        }
        set
        {
            if (!value.HasValue)
                return;

            foreach (var action in Actions.Where(action => action.IsEnabled && action.IsVisible && action.CanEdit))
                action.IsSelected = value.Value;

            OnPropertyChanged(nameof(HeaderCheckState));
        }
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;

        foreach (var action in Actions)
            action.IsEnabled = isEnabled;

        RaiseSelectionChanged();
    }

    public void SelectRecommended()
    {
        foreach (var action in Actions.Where(action => action.IsEnabled && action.IsVisible && action.CanEdit))
            action.IsSelected = OptimizationToggleActionHelper.GetRecommendedSelectedState(action, Actions);

        RaiseSelectionChanged();
    }

    public void ClearSelection()
    {
        foreach (var action in Actions.Where(action => action.IsEnabled && action.IsVisible && action.CanEdit))
            action.IsSelected = false;

        RaiseSelectionChanged();
    }

    public void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(HeaderCheckState));
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(SelectedActionCount));
        OnPropertyChanged(nameof(VisibleActionCount));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<string> SelectedActionKeys => Actions.Where(action => action.IsSelected).Select(action => action.Key);

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
