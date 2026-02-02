using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Pages.WindowsOptimization;

namespace LenovoLegionToolkit.WPF.ViewModels;

public class WindowsOptimizationViewModel : INotifyPropertyChanged
{
    private readonly WindowsOptimizationService _windowsOptimizationService;
    private readonly WindowsCleanupService _cleanupService;
    private readonly ApplicationSettings _applicationSettings;
    private readonly PackageDownloaderSettings _packageDownloaderSettings;
    private readonly PackageDownloaderFactory _packageDownloaderFactory;

    private readonly HashSet<string> _userUncheckedActions = new(StringComparer.OrdinalIgnoreCase);
    private bool _isRefreshingStates;

    public WindowsOptimizationViewModel(
        WindowsOptimizationService windowsOptimizationService,
        WindowsCleanupService cleanupService,
        ApplicationSettings applicationSettings,
        PackageDownloaderSettings packageDownloaderSettings,
        PackageDownloaderFactory packageDownloaderFactory)
    {
        _windowsOptimizationService = windowsOptimizationService;
        _cleanupService = cleanupService;
        _applicationSettings = applicationSettings;
        _packageDownloaderSettings = packageDownloaderSettings;
        _packageDownloaderFactory = packageDownloaderFactory;

        Categories = new ObservableCollection<OptimizationCategoryViewModel>();
        OptimizationCategories = new ObservableCollection<OptimizationCategoryViewModel>();
        CleanupCategories = new ObservableCollection<OptimizationCategoryViewModel>();
        SelectedOptimizationActions = new ObservableCollection<SelectedActionViewModel>();
        SelectedCleanupActions = new ObservableCollection<SelectedActionViewModel>();
        SelectedDriverPackages = new ObservableCollection<SelectedDriverPackageViewModel>();
        CustomCleanupRules = new ObservableCollection<CustomCleanupRuleViewModel>();
    }

    public enum PageMode
    {
        Optimization,
        Cleanup,
        DriverDownload
    }

    private PageMode _currentMode = PageMode.Optimization;
    public PageMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode == value) return;
            _currentMode = value;
            OnPropertyChanged(nameof(CurrentMode));
            OnPropertyChanged(nameof(VisibleSelectedActions));
            OnPropertyChanged(nameof(HasSelectedActions));
            OnPropertyChanged(nameof(SelectedActionsSummary));
            OnPropertyChanged(nameof(ActiveCategories));
            OnPropertyChanged(nameof(IsCleanupMode));
            OnPropertyChanged(nameof(IsDriverDownloadMode));

            // Save the last selected mode
            _applicationSettings.Store.LastWindowsOptimizationPageMode = (int)_currentMode;
            _applicationSettings.SynchronizeStore();
        }
    }

    public bool IsCleanupMode => CurrentMode == PageMode.Cleanup;
    public bool IsDriverDownloadMode => CurrentMode == PageMode.DriverDownload;

    public string ScanCleanupButtonText => Resource.ResourceManager.GetString("WindowsOptimizationPage_Scan_Button") ?? "Scan";
    public string PauseAllButtonText => Resource.ResourceManager.GetString("WindowsOptimizationPage_PauseAll_Button") ?? "Pause All";
    public string StartAllButtonText => Resource.ResourceManager.GetString("WindowsOptimizationPage_StartAll_Button") ?? "Start All";
    public string PendingText => Resource.WindowsOptimizationPage_EstimatedCleanupSize_Pending;

    private bool _isAnyDriverRunning;
    public bool IsAnyDriverRunning
    {
        get => _isAnyDriverRunning;
        set
        {
            if (_isAnyDriverRunning == value) return;
            _isAnyDriverRunning = value;
            OnPropertyChanged(nameof(IsAnyDriverRunning));
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    private bool _isScanned;
    public bool IsScanned
    {
        get => _isScanned;
        set
        {
            if (_isScanned == value) return;
            _isScanned = value;
            OnPropertyChanged(nameof(IsScanned));
            UpdateSelectedActions();
        }
    }

    private string _currentOperationText = string.Empty;
    public string CurrentOperationText
    {
        get => _currentOperationText;
        set
        {
            if (_currentOperationText == value) return;
            _currentOperationText = value;
            OnPropertyChanged(nameof(CurrentOperationText));
        }
    }

    private string _currentDeletingFile = string.Empty;
    public string CurrentDeletingFile
    {
        get => _currentDeletingFile;
        set
        {
            if (_currentDeletingFile == value) return;
            _currentDeletingFile = value;
            OnPropertyChanged(nameof(CurrentDeletingFile));
        }
    }

    private string _runCleanupButtonText = Resource.ResourceManager.GetString("WindowsOptimizationPage_RunCleanup_Button") ?? "Run Cleanup";
    public string RunCleanupButtonText
    {
        get => _runCleanupButtonText;
        set
        {
            if (_runCleanupButtonText == value) return;
            _runCleanupButtonText = value;
            OnPropertyChanged(nameof(RunCleanupButtonText));
        }
    }

    private bool _isCompactView;
    public bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (_isCompactView == value) return;
            _isCompactView = value;
            OnPropertyChanged(nameof(IsCompactView));
        }
    }

    private bool _isCalculatingSize;
    public bool IsCalculatingSize
    {
        get => _isCalculatingSize;
        set
        {
            if (_isCalculatingSize == value) return;
            _isCalculatingSize = value;
            OnPropertyChanged(nameof(IsCalculatingSize));
        }
    }

    private bool _isCleaning;
    public bool IsCleaning
    {
        get => _isCleaning;
        set
        {
            if (_isCleaning == value) return;
            _isCleaning = value;
            OnPropertyChanged(nameof(IsCleaning));
        }
    }

    public ObservableCollection<OptimizationCategoryViewModel> Categories { get; }
    public ObservableCollection<OptimizationCategoryViewModel> OptimizationCategories { get; }
    public ObservableCollection<OptimizationCategoryViewModel> CleanupCategories { get; }
    public ObservableCollection<SelectedActionViewModel> SelectedOptimizationActions { get; }
    public ObservableCollection<SelectedActionViewModel> SelectedCleanupActions { get; }
    public ObservableCollection<SelectedDriverPackageViewModel> SelectedDriverPackages { get; }
    public ObservableCollection<CustomCleanupRuleViewModel> CustomCleanupRules { get; }

    public IEnumerable<OptimizationCategoryViewModel> ActiveCategories => CurrentMode switch
    {
        PageMode.Cleanup => CleanupCategories,
        PageMode.Optimization => OptimizationCategories,
        _ => OptimizationCategories
    };

    public ObservableCollection<SelectedActionViewModel> VisibleSelectedActions => CurrentMode switch
    {
        PageMode.Cleanup => SelectedCleanupActions,
        PageMode.Optimization => SelectedOptimizationActions,
        _ => SelectedOptimizationActions
    };

    public bool HasSelectedActions => CurrentMode switch
    {
        PageMode.DriverDownload => SelectedDriverPackages.Count > 0,
        PageMode.Cleanup => CleanupCategories
            .Where(c => c?.Actions != null)
            .SelectMany(c => c.Actions)
            .Any(a => a != null && a.IsEnabled && a.IsSelected),
        _ => VisibleSelectedActions.Count > 0
    };

    public string SelectedActionsSummary
    {
        get
        {
            int count = CurrentMode switch
            {
                PageMode.DriverDownload => SelectedDriverPackages.Count,
                PageMode.Cleanup => CleanupCategories
                    .Where(c => c?.Actions != null)
                    .SelectMany(c => c.Actions)
                    .Count(a => a != null && a.IsSelected),
                _ => VisibleSelectedActions.Count
            };
            return string.Format(Resource.WindowsOptimizationPage_SelectedActions_Count, count);
        }
    }

    private long _estimatedCleanupSize;
    public long EstimatedCleanupSize
    {
        get => _estimatedCleanupSize;
        set
        {
            if (_estimatedCleanupSize == value) return;
            _estimatedCleanupSize = value;
            OnPropertyChanged(nameof(EstimatedCleanupSize));
            OnPropertyChanged(nameof(EstimatedCleanupSizeText));
        }
    }

    public string EstimatedCleanupSizeText => (CurrentMode == PageMode.Cleanup && EstimatedCleanupSize > 0)
        ? string.Format(Resource.WindowsOptimizationPage_EstimatedCleanupSize, FormatBytes(EstimatedCleanupSize))
        : string.Empty;

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public void Initialize()
    {
        // Restore last mode
        var lastMode = (PageMode)_applicationSettings.Store.LastWindowsOptimizationPageMode;
        if (Enum.IsDefined(typeof(PageMode), lastMode))
        {
            _currentMode = lastMode;
        }

        // Unsubscribe from existing events to prevent memory leaks
        foreach (var category in Categories)
        {
            category.SelectionChanged -= Category_SelectionChanged;
            if (category.Actions != null)
            {
                foreach (var action in category.Actions)
                {
                    action.PropertyChanged -= Action_PropertyChanged;
                }
            }
            category.Dispose();
        }

        Categories.Clear();
        OptimizationCategories.Clear();
        CleanupCategories.Clear();

        foreach (var category in _windowsOptimizationService.GetCategories())
        {
            var actions = category.Actions.Select(action => new OptimizationActionViewModel(
                action.Key,
                Resource.ResourceManager.GetString(action.TitleResourceKey) ?? action.TitleResourceKey,
                Resource.ResourceManager.GetString(action.DescriptionResourceKey) ?? action.DescriptionResourceKey,
                action.Recommended,
                "Recommended")).ToList();

            var isCleanup = category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase);

            foreach (var actionVm in actions)
            {
                if (isCleanup)
                {
                    // Restore selection from settings
                    if (_applicationSettings.Store.SelectedCleanupActions != null)
                    {
                        actionVm.IsSelected = _applicationSettings.Store.SelectedCleanupActions.Contains(actionVm.Key);
                    }
                }
                else
                {
                    // Optimization actions: restore selection from settings (but will be overridden by scan)
                    // The scan will detect actual system state, but we restore user's previous selection intent
                    if (_applicationSettings.Store.SelectedOptimizationActions != null)
                    {
                        actionVm.IsSelected = _applicationSettings.Store.SelectedOptimizationActions.Contains(actionVm.Key);
                    }
                }

                actionVm.PropertyChanged += Action_PropertyChanged;
            }

            var categoryVm = new OptimizationCategoryViewModel(
                category.Key,
                Resource.ResourceManager.GetString(category.TitleResourceKey) ?? category.TitleResourceKey,
                Resource.ResourceManager.GetString(category.DescriptionResourceKey) ?? category.DescriptionResourceKey,
                Resource.WindowsOptimization_Category_SelectionSummary,
                actions,
                category.PluginId);

            foreach (var actionVm in actions)
                actionVm.Category = categoryVm;

            categoryVm.SelectionChanged += Category_SelectionChanged;
            
            Categories.Add(categoryVm);

            if (category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
                CleanupCategories.Add(categoryVm);
            else
                OptimizationCategories.Add(categoryVm);
        }

        OnPropertyChanged(nameof(CurrentMode));
        OnPropertyChanged(nameof(IsCleanupMode));
        OnPropertyChanged(nameof(IsDriverDownloadMode));
        OnPropertyChanged(nameof(ActiveCategories));
        UpdateSelectedActions();

        _ = ScanOptimizationStatesAsync();
    }

    private void Action_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not OptimizationActionViewModel actionVm || e.PropertyName != nameof(OptimizationActionViewModel.IsSelected))
            return;

        if (_isRefreshingStates) return;

        var isCleanup = actionVm.Category?.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isCleanup)
        {
            // Ensure UI updates happen on UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateSelectedActions();
                    SaveCleanupSelection();
                });
            }
            else
            {
                UpdateSelectedActions();
                SaveCleanupSelection();
            }
        }
        else
        {
            // Ensure async operation is properly handled
            _ = HandleOptimizationActionChangeAsync(actionVm);
        }
    }

    private void Category_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateSelectedActions();
    }

    public void SelectRecommended()
    {
        _isRefreshingStates = true;
        try
        {
            foreach (var category in ActiveCategories)
                category.SelectRecommended();
            
            UpdateSelectedActions();
        }
        finally
        {
            _isRefreshingStates = false;
        }

        // Save selection after refreshing states
        if (CurrentMode == PageMode.Cleanup)
            SaveCleanupSelection();
        else
            SaveOptimizationSelection();
    }

    public void ClearSelection()
    {
        _isRefreshingStates = true;
        try
        {
            foreach (var category in ActiveCategories)
                category.ClearSelection();
            
            UpdateSelectedActions();
        }
        finally
        {
            _isRefreshingStates = false;
        }

        // Save selection after refreshing states
        if (CurrentMode == PageMode.Cleanup)
            SaveCleanupSelection();
        else
            SaveOptimizationSelection();
    }

    private void UpdateSelectedActions()
    {
        // Pre-allocate lists with estimated capacity to reduce allocations
        var newOptimizationActions = new List<SelectedActionViewModel>();
        var newCleanupActions = new List<SelectedActionViewModel>();

        // Cache string comparison to avoid repeated allocations
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        const string cleanupPrefix = "cleanup.";

        foreach (var category in Categories)
        {
            if (category?.Actions == null) continue;

            // Determine target list once per category
            var isCleanup = category.Key.StartsWith(cleanupPrefix, comparison);
            var target = isCleanup ? newCleanupActions : newOptimizationActions;

            // Filter and add in single pass to reduce iterations
            foreach (var action in category.Actions)
            {
                if (action != null && action.IsEnabled && action.IsSelected)
                {
                    target.Add(new SelectedActionViewModel(
                        category.Key, 
                        category.Title, 
                        action.Key, 
                        action.Title, 
                        action.Description, 
                        action));
                }
            }
        }

        UpdateCollection(SelectedOptimizationActions, newOptimizationActions);
        UpdateCollection(SelectedCleanupActions, newCleanupActions);

        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));

        if (CurrentMode == PageMode.Cleanup)
        {
            if (IsScanned)
            {
                if (HasSelectedActions)
                    _ = UpdateEstimatedCleanupSizeAsync();
                else
                    EstimatedCleanupSize = 0;
            }
            else
            {
                EstimatedCleanupSize = 0;
            }
        }
    }

    private void UpdateCollection(ObservableCollection<SelectedActionViewModel> collection, List<SelectedActionViewModel> newList)
    {
        // Use ActionKey and CategoryKey as unique identifier
        var newKeys = newList.Select(x => $"{x.CategoryKey}:{x.ActionKey}").ToHashSet();
        var oldKeys = collection.Select(x => $"{x.CategoryKey}:{x.ActionKey}").ToHashSet();

        // Remove items that are not in the new list
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            var item = collection[i];
            var key = $"{item.CategoryKey}:{item.ActionKey}";
            if (!newKeys.Contains(key))
            {
                collection.RemoveAt(i);
                item.Dispose();
            }
        }

        // Add items that are not in the old list
        foreach (var item in newList)
        {
            var key = $"{item.CategoryKey}:{item.ActionKey}";
            if (!oldKeys.Contains(key))
            {
                collection.Add(item);
            }
            else
            {
                // Item already exists, dispose the new one since we're not using it
                item.Dispose();
            }
        }
    }

    public async Task UpdateEstimatedCleanupSizeAsync()
    {
        var selectedKeys = SelectedCleanupActions.Select(a => a.ActionKey).ToList();
        
        var size = await _cleanupService.EstimateCleanupSizeAsync(selectedKeys, CancellationToken.None, path =>
        {
            // Update progress text on UI thread
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    CurrentOperationText = path;
                });
            }
        }).ConfigureAwait(false);
        
        // Ensure UI updates happen on UI thread
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            await Application.Current.Dispatcher.BeginInvoke(() => EstimatedCleanupSize = size);
        }
        else
        {
            EstimatedCleanupSize = size;
        }
    }

    public async Task ScanCleanupAsync(CancellationToken cancellationToken)
    {
        // Ensure UI updates happen on UI thread
        void UpdateUIState(bool busy, bool calculating, string operationText, string buttonText)
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsBusy = busy;
                    IsCalculatingSize = calculating;
                    CurrentOperationText = operationText;
                    RunCleanupButtonText = buttonText;
                });
            }
            else
            {
                IsBusy = busy;
                IsCalculatingSize = calculating;
                CurrentOperationText = operationText;
                RunCleanupButtonText = buttonText;
            }
        }

        UpdateUIState(true, true, Resource.WindowsOptimizationPage_EstimatedCleanupSize_Pending, string.Empty);
        
        try
        {
            if (SelectedCleanupActions.Count == 0)
            {
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => SnackbarHelper.Show(
                        Resource.SettingsPage_WindowsOptimization_Title,
                        Resource.ResourceManager.GetString("WindowsOptimization_NoCleanupSelection_Warning") ?? "Please select at least one item to clean up.",
                        SnackbarType.Warning));
                }
                return;
            }

            // Mark as scanned to enable "Run Cleanup" button (if items selected)
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.BeginInvoke(() => IsScanned = true);
            }
            else
            {
                IsScanned = true;
            }
            
            await UpdateEstimatedCleanupSizeAsync().ConfigureAwait(false);
        }
        finally
        {
            var buttonText = Resource.ResourceManager.GetString("WindowsOptimizationPage_RunCleanup_Button") ?? "Run Cleanup";
            UpdateUIState(false, false, string.Empty, buttonText);
        }
    }

    private void SaveCleanupSelection()
    {
        // Optimize LINQ query with null checks
        var selectedKeys = CleanupCategories
            .Where(c => c?.Actions != null)
            .SelectMany(c => c.Actions)
            .Where(a => a != null && a.IsSelected)
            .Select(a => a.Key)
            .ToList();

        _applicationSettings.Store.SelectedCleanupActions = selectedKeys;
        _applicationSettings.SynchronizeStore();
    }

    private void SaveOptimizationSelection()
    {
        // Save optimization actions selection state
        // Note: This saves the current UI state, which should reflect the actual system state after scanning
        var selectedKeys = OptimizationCategories
            .Where(c => c?.Actions != null)
            .SelectMany(c => c.Actions)
            .Where(a => a != null && a.IsSelected)
            .Select(a => a.Key)
            .ToList();

        _applicationSettings.Store.SelectedOptimizationActions = selectedKeys;
        _applicationSettings.SynchronizeStore();
    }

    private async Task HandleOptimizationActionChangeAsync(OptimizationActionViewModel actionVm)
    {
        // Null check
        if (actionVm == null)
            return;

        if (IsBusy) return;

        IsBusy = true;
        try
        {
            // Execute the action immediately when clicked
            await _windowsOptimizationService.ApplyActionAsync(actionVm.Key, CancellationToken.None).ConfigureAwait(false);

            // Re-scan state to verify it was applied
            var isApplied = await _windowsOptimizationService.IsActionAppliedAsync(actionVm.Key, CancellationToken.None).ConfigureAwait(false);

            // Ensure UI updates happen on UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _isRefreshingStates = true;
                    try
                    {
                        actionVm.IsSelected = isApplied;
                        UpdateSelectedActions();
                        // Save selection state after applying
                        SaveOptimizationSelection();
                    }
                    finally
                    {
                        _isRefreshingStates = false;
                    }
                });
            }
            else
            {
                _isRefreshingStates = true;
                try
                {
                    actionVm.IsSelected = isApplied;
                    UpdateSelectedActions();
                    // Save selection state after applying
                    SaveOptimizationSelection();
                }
                finally
                {
                    _isRefreshingStates = false;
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception for debugging
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to handle optimization action change for {actionVm?.Key ?? "unknown"}", ex);

            // Provide feedback to user
            if (Application.Current?.Dispatcher != null)
            {
                await Application.Current.Dispatcher.BeginInvoke(() => SnackbarHelper.Show(
                    Resource.SettingsPage_WindowsOptimization_Title,
                    string.Format(Resource.ResourceManager.GetString("WindowsOptimizationPage_Optimization_Error_Format") ?? "Failed to apply {0}: {1}", actionVm?.Title ?? "Unknown", ex.Message),
                    SnackbarType.Error));
            }

            // Re-scan on error to ensure UI reflects actual state
            var isApplied = actionVm is not null ? await _windowsOptimizationService.IsActionAppliedAsync(actionVm.Key, CancellationToken.None).ConfigureAwait(false) : false;
            
            // Ensure UI updates happen on UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _isRefreshingStates = true;
                    try
                    {
                        if (actionVm is not null)
                            actionVm.IsSelected = isApplied;
                        UpdateSelectedActions();
                        // Save selection state even on error (reflects actual state)
                        SaveOptimizationSelection();
                    }
                    finally
                    {
                        _isRefreshingStates = false;
                    }
                });
            }
            else
            {
                _isRefreshingStates = true;
                try
                {
                    if (actionVm is not null)
                        actionVm.IsSelected = isApplied;
                    UpdateSelectedActions();
                    // Save selection state even on error (reflects actual state)
                    SaveOptimizationSelection();
                }
                finally
                {
                    _isRefreshingStates = false;
                }
            }
        }
        finally
        {
            // Ensure UI updates happen on UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.BeginInvoke(() => IsBusy = false);
            }
            else
            {
                IsBusy = false;
            }
        }
    }

    public async Task ScanOptimizationStatesAsync()
    {
        _isRefreshingStates = true;
        try
        {
            foreach (var category in OptimizationCategories)
            {
                if (category?.Actions == null) continue;
                
                foreach (var action in category.Actions)
                {
                    if (action == null) continue;
                    
                    // Scan to detect actual system state
                    var isApplied = await _windowsOptimizationService.IsActionAppliedAsync(action.Key, CancellationToken.None).ConfigureAwait(false);
                    
                    // Ensure UI updates happen on UI thread
                    if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                    {
                        await Application.Current.Dispatcher.BeginInvoke(() => action.IsSelected = isApplied);
                    }
                    else
                    {
                        action.IsSelected = isApplied;
                    }
                }
            }
            
            // Ensure UI updates happen on UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateSelectedActions();
                    // Save the scanned state (actual system state) to settings
                    SaveOptimizationSelection();
                });
            }
            else
            {
                UpdateSelectedActions();
                // Save the scanned state (actual system state) to settings
                SaveOptimizationSelection();
            }
        }
        finally
        {
            _isRefreshingStates = false;
        }
    }

    public void NotifyDriverSelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
