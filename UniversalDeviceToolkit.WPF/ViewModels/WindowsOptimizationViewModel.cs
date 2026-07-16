using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.PackageDownloader;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

namespace UniversalDeviceToolkit.WPF.ViewModels;

public class WindowsOptimizationViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly WindowsOptimizationService _windowsOptimizationService;
    private readonly WindowsCleanupService _cleanupService;
    private readonly ApplicationSettings _applicationSettings;
    private static CultureInfo ActiveCulture => Resource.Culture ?? CultureInfo.CurrentUICulture;
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, ActiveCulture);

    private readonly SemaphoreSlim _optimizationStateScanLock = new(1, 1);
    private CancellationTokenSource? _driverGetPackagesTokenSource;
    private CancellationTokenSource? _driverFilterDebounceCancellationTokenSource;
    private bool _disposed;
    private bool _isRefreshingStates;

    public CancellationTokenSource? DriverGetPackagesTokenSource
    {
        get => _driverGetPackagesTokenSource;
        set => _driverGetPackagesTokenSource = value;
    }

    public CancellationTokenSource? DriverFilterDebounceCancellationTokenSource
    {
        get => _driverFilterDebounceCancellationTokenSource;
        set => _driverFilterDebounceCancellationTokenSource = value;
    }

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
        // Package downloader deps are owned by WindowsOptimizationPage; keep params for Autofac shape.
        _ = packageDownloaderSettings;
        _ = packageDownloaderFactory;

        Categories = new ObservableCollection<OptimizationCategoryViewModel>();
        OptimizationCategories = new ObservableCollection<OptimizationCategoryViewModel>();
        CleanupCategories = new ObservableCollection<OptimizationCategoryViewModel>();
        SelectedOptimizationActions = new ObservableCollection<SelectedActionViewModel>();
        SelectedCleanupActions = new ObservableCollection<SelectedActionViewModel>();
        SelectedDriverActions = new ObservableCollection<SelectedActionViewModel>();
        SelectedDriverPackages = new ObservableCollection<SelectedDriverPackageViewModel>();
        CustomCleanupRules = new ObservableCollection<CustomCleanupRuleViewModel>();
    }

    public enum PageMode
    {
        Optimization,
        Cleanup,
        DriverDownload,
        NetworkAcceleration
    }

    private PageMode _currentMode = PageMode.Optimization;
    public PageMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode == value) return;
            _currentMode = value;
            NotifyModePropertiesChanged();

            // Save the last selected mode
            _applicationSettings.Store.LastWindowsOptimizationPageMode = (int)_currentMode;
            _applicationSettings.SynchronizeStore();
        }
    }

    public bool IsCleanupMode => CurrentMode == PageMode.Cleanup;
    public bool IsDriverDownloadMode => CurrentMode == PageMode.DriverDownload;
    public bool IsNetworkAccelerationMode => CurrentMode == PageMode.NetworkAcceleration;
    public bool IsOptimizationMode => CurrentMode == PageMode.Optimization;

    /// <summary>
    /// Page header title for the active tab (avoids showing system-optimization copy on other tabs).
    /// </summary>
    public string PageHeaderTitle => CurrentMode switch
    {
        PageMode.NetworkAcceleration => Resource.NetworkAccelerationPage_Title,
        PageMode.Cleanup => Resource.WindowsOptimizationPage_Tab_Cleanup,
        PageMode.DriverDownload => Resource.WindowsOptimizationPage_Tab_DriverDownload,
        _ => Resource.SettingsPage_WindowsOptimization_Title
    };

    /// <summary>
    /// Page header subtitle for the active tab.
    /// </summary>
    public string PageHeaderDescription => CurrentMode switch
    {
        PageMode.NetworkAcceleration => Resource.NetworkAccelerationPage_Subtitle,
        PageMode.Cleanup => Resource.WindowsOptimizationPage_CleanupInfo,
        PageMode.DriverDownload => T("WindowsOptimizationPage_DriverEmpty_NotScanned_Message",
            "Choose a source and scan to list compatible driver downloads."),
        _ => Resource.WindowsOptimizationPage_Info
    };

    /// <summary>
    /// Re-raises mode-related property changes without changing <see cref="CurrentMode"/>.
    /// Used when the NA tab is re-selected after a silent mode restore.
    /// </summary>
    public void RefreshModePresentation() => NotifyModePropertiesChanged();

    private void NotifyModePropertiesChanged()
    {
        OnPropertyChanged(nameof(CurrentMode));
        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
        OnPropertyChanged(nameof(ActiveCategories));
        OnPropertyChanged(nameof(IsCleanupMode));
        OnPropertyChanged(nameof(IsDriverDownloadMode));
        OnPropertyChanged(nameof(IsNetworkAccelerationMode));
        OnPropertyChanged(nameof(IsOptimizationMode));
        OnPropertyChanged(nameof(PageHeaderTitle));
        OnPropertyChanged(nameof(PageHeaderDescription));
    }

    public string ScanCleanupButtonText => T("WindowsOptimizationPage_Scan_Button", "Scan");
    public string PauseAllButtonText => T("WindowsOptimizationPage_PauseAll_Button", "Pause All");
    public string StartAllButtonText => T("WindowsOptimizationPage_StartAll_Button", "Start All");
    public string PendingText => Resource.WindowsOptimizationPage_EstimatedCleanupSize_Pending;
    public string SelectedActionsEmptyText => Resource.WindowsOptimizationPage_SelectedActions_Empty ?? "No actions selected yet.";

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

    private string _runCleanupButtonText = T("WindowsOptimizationPage_RunCleanup_Button", "Run Cleanup");
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
    public ObservableCollection<SelectedActionViewModel> SelectedDriverActions { get; }
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
        PageMode.DriverDownload => SelectedDriverActions,
        PageMode.Optimization => SelectedOptimizationActions,
        _ => SelectedOptimizationActions
    };

    public bool HasSelectedActions => CurrentMode switch
    {
        PageMode.DriverDownload => SelectedDriverActions.Count > 0,
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
        RunOnDispatcher(InitializeCore);
    }

    private void InitializeCore()
    {
        // Restore last mode (ignore removed Beautification tab and invalid stored values)
        var lastMode = (PageMode)_applicationSettings.Store.LastWindowsOptimizationPageMode;
        if (!Enum.IsDefined(typeof(PageMode), lastMode))
            lastMode = PageMode.Optimization;
        _currentMode = lastMode;

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
            var categoryResourceManager = ResolveResourceManager(category.ResourceAnchorType);

            var actions = category.Actions.Select(action => new OptimizationActionViewModel(
                action,
                ResolveOptimizationText(action.ResourceAnchorType, categoryResourceManager, action.TitleResourceKey),
                ResolveOptimizationText(action.ResourceAnchorType, categoryResourceManager, action.DescriptionResourceKey),
                T("WindowsOptimization_Action_Recommended_Tag", "Recommended"))).ToList();

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
                ResolveOptimizationText(category.ResourceAnchorType, categoryResourceManager, category.TitleResourceKey),
                ResolveOptimizationText(category.ResourceAnchorType, categoryResourceManager, category.DescriptionResourceKey),
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

        // Must include IsNetworkAccelerationMode — otherwise restoring NA tab keeps optimization list visible.
        NotifyModePropertiesChanged();
        UpdateSelectedActions();

        StartOptimizationStateScan();
    }

    private void StartOptimizationStateScan()
    {
        _ = ObserveOptimizationStateScanAsync();
    }

    private async Task ObserveOptimizationStateScanAsync()
    {
        try
        {
            await ScanOptimizationStatesAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to scan Windows optimization states.", ex);
        }
    }

    private static string ResolveOptimizationText(Type? resourceAnchorType, ResourceManager? categoryResourceManager, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var resourceManager = ResolveResourceManager(resourceAnchorType) ?? categoryResourceManager;
        if (resourceManager is not null)
        {
            var value = LocalizationHelper.GetStringOrEnglish(resourceManager, key, string.Empty, ActiveCulture);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        var hostValue = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, string.Empty, ActiveCulture);
        if (!string.IsNullOrWhiteSpace(hostValue))
            return hostValue;

        return key;
    }

    private static ResourceManager? ResolveResourceManager(Type? resourceAnchorType)
    {
        if (resourceAnchorType is null)
            return null;

        try
        {
            var resourceType = resourceAnchorType.Assembly.GetType($"{resourceAnchorType.Namespace}.Resources.Resource");
            var property = resourceType?.GetProperty(nameof(Resource.ResourceManager), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return property?.GetValue(null) as ResourceManager;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve plugin resource manager. [type={resourceAnchorType.FullName}]", ex);
            return null;
        }
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
            foreach (var category in SnapshotActiveCategories())
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
            foreach (var category in SnapshotActiveCategories())
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

        foreach (var category in SnapshotCategories())
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
        try
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
            });
            
            // Ensure UI updates happen on UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.BeginInvoke(() => EstimatedCleanupSize = size).Task;
            }
            else
            {
                EstimatedCleanupSize = size;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to estimate cleanup size.", ex);
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
                T("WindowsOptimization_NoCleanupSelection_Warning", "Please select at least one item to clean up."),
                        SnackbarType.Warning)).Task;
                }
                return;
            }

            // Mark as scanned to enable "Run Cleanup" button (if items selected)
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.BeginInvoke(() => IsScanned = true).Task;
            }
            else
            {
                IsScanned = true;
            }
            
            await UpdateEstimatedCleanupSizeAsync();
        }
        finally
        {
        var buttonText = T("WindowsOptimizationPage_RunCleanup_Button", "Run Cleanup");
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
        if (actionVm is null)
            return;

        var desiredApplied = actionVm.IsSelected;
        var isToggleAction = OptimizationToggleActionHelper.IsToggleAction(actionVm.Key);

        if (IsBusy)
        {
            await SetOptimizationActionSelectedOnUiAsync(actionVm, !desiredApplied);
            await ShowOptimizationSnackbarAsync(
                T("WindowsOptimizationPage_Optimization_Busy_Wait", "Please wait for the current optimization to finish."),
                SnackbarType.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            if (isToggleAction)
            {
                var targetActionKey = OptimizationToggleActionHelper.ResolveTargetActionKey(actionVm.Key, desiredApplied);
                await _windowsOptimizationService.ApplyActionAsync(targetActionKey, CancellationToken.None);

                var togglePair = OptimizationToggleActionHelper.FindTogglePair(actionVm, actionVm.Category?.Actions ?? []);
                var featureEnabled = togglePair is null
                    ? desiredApplied
                    : await _windowsOptimizationService.IsActionAppliedAsync(togglePair.Value.Enable.Key, CancellationToken.None);

                if (featureEnabled != desiredApplied)
                {
                    await ShowOptimizationSnackbarAsync(
                        string.Format(
                            T("WindowsOptimizationPage_Optimization_Error_Format", "Failed to apply {0}: {1}"),
                            actionVm.Title,
                            T("WindowsOptimizationPage_Optimization_NotVerified", "The change could not be verified. Administrator privileges may be required.")),
                        SnackbarType.Error);
                }
                else
                {
                    await ShowOptimizationSnackbarAsync(
                        desiredApplied
                            ? string.Format(
                                T("WindowsOptimizationPage_Optimization_Applied_Format", "{0} applied successfully."),
                                actionVm.Title)
                            : string.Format(
                                T("WindowsOptimizationPage_Optimization_Reverted_Format", "{0} reverted successfully."),
                                actionVm.Title),
                        SnackbarType.Success);
                }

                if (togglePair is not null)
                    await ApplyTogglePairPresentationOnUiAsync(togglePair.Value.Enable, togglePair.Value.Disable, featureEnabled);
                else
                    await SetOptimizationActionSelectedOnUiAsync(actionVm, desiredApplied);

                await RunOnUiAsync(SaveOptimizationSelection);
            }
            else
            {
                if (desiredApplied)
                    await _windowsOptimizationService.ApplyActionAsync(actionVm.Key, CancellationToken.None);
                else
                    await _windowsOptimizationService.RevertActionAsync(actionVm.Key, CancellationToken.None);

                var isApplied = await _windowsOptimizationService.IsActionAppliedAsync(actionVm.Key, CancellationToken.None);

                if (isApplied != desiredApplied)
                {
                    await ShowOptimizationSnackbarAsync(
                        string.Format(
                            T("WindowsOptimizationPage_Optimization_Error_Format", "Failed to apply {0}: {1}"),
                            actionVm.Title,
                            T("WindowsOptimizationPage_Optimization_NotVerified", "The change could not be verified. Administrator privileges may be required.")),
                        SnackbarType.Error);
                }
                else
                {
                    await ShowOptimizationSnackbarAsync(
                        desiredApplied
                            ? string.Format(
                                T("WindowsOptimizationPage_Optimization_Applied_Format", "{0} applied successfully."),
                                actionVm.Title)
                            : string.Format(
                                T("WindowsOptimizationPage_Optimization_Reverted_Format", "{0} reverted successfully."),
                                actionVm.Title),
                        SnackbarType.Success);
                }

                await SetOptimizationActionSelectedOnUiAsync(actionVm, isApplied);
                await RunOnUiAsync(SaveOptimizationSelection);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to handle optimization action change for {actionVm.Key}", ex);

            await ShowOptimizationSnackbarAsync(
                string.Format(
                    T("WindowsOptimizationPage_Optimization_Error_Format", "Failed to apply {0}: {1}"),
                    actionVm.Title,
                    ex.Message),
                SnackbarType.Error);

            if (isToggleAction)
            {
                var togglePair = OptimizationToggleActionHelper.FindTogglePair(actionVm, actionVm.Category?.Actions ?? []);
                if (togglePair is not null)
                {
                    var featureEnabled = await _windowsOptimizationService.IsActionAppliedAsync(togglePair.Value.Enable.Key, CancellationToken.None);
                    await ApplyTogglePairPresentationOnUiAsync(togglePair.Value.Enable, togglePair.Value.Disable, featureEnabled);
                }
                else
                {
                    await SetOptimizationActionSelectedOnUiAsync(actionVm, !desiredApplied);
                }
            }
            else
            {
                var isApplied = await _windowsOptimizationService.IsActionAppliedAsync(actionVm.Key, CancellationToken.None);
                await SetOptimizationActionSelectedOnUiAsync(actionVm, isApplied);
            }

            await RunOnUiAsync(SaveOptimizationSelection);
        }
        finally
        {
            await RunOnUiAsync(() => IsBusy = false);
        }
    }

    private Task SetOptimizationActionSelectedOnUiAsync(OptimizationActionViewModel actionVm, bool isSelected)
    {
        return RunOnUiAsync(() =>
        {
            _isRefreshingStates = true;
            try
            {
                actionVm.IsSelected = isSelected;
                UpdateSelectedActions();
            }
            finally
            {
                _isRefreshingStates = false;
            }
        });
    }

    private Task ShowOptimizationSnackbarAsync(string message, SnackbarType type)
    {
        return RunOnUiAsync(() => SnackbarHelper.Show(
            Resource.SettingsPage_WindowsOptimization_Title,
            message,
            type));
    }

    private Task RunOnUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }

    public async Task ScanOptimizationStatesAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        await _optimizationStateScanLock.WaitAsync(cancellationToken);
        _isRefreshingStates = true;
        try
        {
            if (_disposed)
                return;
            var categories = await GetOptimizationCategorySnapshotAsync();
            var pairedActionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var category in categories)
            {
                foreach (var (enable, disable) in OptimizationToggleActionHelper.FindTogglePairs(category.Actions))
                {
                    pairedActionKeys.Add(enable.Key);
                    pairedActionKeys.Add(disable.Key);

                    var featureEnabled = await _windowsOptimizationService.IsActionAppliedAsync(enable.Key, CancellationToken.None);
                    await ApplyTogglePairPresentationOnUiAsync(enable, disable, featureEnabled);
                }
            }

            var actions = await GetOptimizationActionSnapshotAsync();
            foreach (var action in actions.Where(action => !pairedActionKeys.Contains(action.Key)))
            {
                var isApplied = await _windowsOptimizationService.IsActionAppliedAsync(action.Key, CancellationToken.None);
                await RunOnUiAsync(() =>
                {
                    action.IsVisible = true;
                    action.IsSelected = isApplied;
                });
            }

            await RunOnUiAsync(() =>
            {
                UpdateSelectedActions();
                SaveOptimizationSelection();
            });
        }
        finally
        {
            _isRefreshingStates = false;
            _optimizationStateScanLock.Release();
        }
    }

    private Task ApplyTogglePairPresentationOnUiAsync(
        OptimizationActionViewModel enable,
        OptimizationActionViewModel disable,
        bool featureEnabled)
    {
        return RunOnUiAsync(() =>
        {
            OptimizationToggleActionHelper.ApplyTogglePairPresentation(featureEnabled, enable, disable);
            enable.Category?.RaiseSelectionChanged();
            UpdateSelectedActions();
        });
    }

    private async Task<List<OptimizationCategoryViewModel>> GetOptimizationCategorySnapshotAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            return await dispatcher.InvokeAsync(SnapshotOptimizationCategories).Task;

        return SnapshotOptimizationCategories();
    }

    private async Task<List<OptimizationActionViewModel>> GetOptimizationActionSnapshotAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            return await dispatcher.InvokeAsync(SnapshotOptimizationActions).Task;

        return SnapshotOptimizationActions();
    }

    private List<OptimizationCategoryViewModel> SnapshotOptimizationCategories() =>
        OptimizationCategories.ToList();

    private List<OptimizationActionViewModel> SnapshotOptimizationActions()
    {
        return OptimizationCategories
            .ToList()
            .Where(category => category?.Actions != null)
            .SelectMany(category => category.Actions.ToList())
            .Where(action => action != null)
            .ToList();
    }

    private List<OptimizationCategoryViewModel> SnapshotCategories() =>
        Categories.ToList();

    private List<OptimizationCategoryViewModel> SnapshotActiveCategories() =>
        ActiveCategories.ToList();

    private void RunOnDispatcher(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            dispatcher.Invoke(action);
        else
            action();
    }

    public void NotifyDriverSelectionChanged()
    {
        UpdateSelectedDriverActions();
        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
    }

    private void UpdateSelectedDriverActions()
    {
        var newDriverActions = SelectedDriverPackages
            .Select(package =>
            {
                var action = new SelectedActionViewModel(
                    "driver-download",
                    package.Category,
                    package.PackageId,
                    package.Title,
                    package.Description,
                    null)
                {
                    Tag = package,
                    IsSelected = true
                };

                return action;
            })
            .ToList();

        UpdateCollection(SelectedDriverActions, newDriverActions);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        DisposeCts(ref _driverFilterDebounceCancellationTokenSource);
        DisposeCts(ref _driverGetPackagesTokenSource);
        _optimizationStateScanLock.Dispose();
    }

    private static void DisposeCts(ref CancellationTokenSource? cts)
    {
        if (cts is null)
            return;

        try
        {
            if (!cts.IsCancellationRequested)
                cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            cts.Dispose();
            cts = null;
        }
    }
}

