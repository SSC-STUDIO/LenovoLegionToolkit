using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Windows.Utils;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage : INotifyPropertyChanged
    {
        private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
        private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
        private SelectedActionsWindow? _selectedActionsWindow;

    private enum PageMode
    {
        Optimization,
        Cleanup,
        Beautification
    }

    private bool _isBusy;
    private PageMode _currentMode = PageMode.Optimization;
    private string _selectedActionsSummaryFormat = "{0}";
    private string _selectedActionsEmptyText = string.Empty;
    private OptimizationCategoryViewModel? _selectedCategory;
    private OptimizationCategoryViewModel? _lastOptimizationCategory;
    private OptimizationCategoryViewModel? _lastCleanupCategory;
    private OptimizationCategoryViewModel? _lastBeautificationCategory;
    private bool _isLoadingCustomCleanupRules;
    private bool _optimizationInteractionEnabled = true;
    private bool _cleanupInteractionEnabled = true;
    private bool _beautificationInteractionEnabled = true;
    private bool _transparencyEnabled;
    private bool _roundedCornersEnabled = true;
    private bool _shadowsEnabled = true;
    private string _selectedTheme = "auto";
    private System.Windows.Threading.DispatcherTimer? _beautificationStatusTimer;
    private MenuStyleSettingsWindow? _styleSettingsWindow;
    private long _estimatedCleanupSize;
    private bool _isCalculatingSize;
    private CancellationTokenSource? _sizeCalculationCts;
    private bool _hasInitializedCleanupMode = false;
    private string _currentOperationText = string.Empty;
    private string _currentDeletingFile = string.Empty;
    private bool _isCompactView;
    private System.Windows.Threading.DispatcherTimer? _actionStateRefreshTimer;
    private bool _isUserInteracting = false;
    private DateTime _lastUserInteraction = DateTime.MinValue;
    private readonly HashSet<string> _userUncheckedActions = new(StringComparer.OrdinalIgnoreCase);
    private bool _isRefreshingStates = false; // 标记是否正在刷新状态（程序内部操作，不应触发命令执行）

    [Flags]
    private enum InteractionScope
    {
        Optimization = 1,
        Cleanup = 2,
        Beautification = 4,
        All = Optimization | Cleanup | Beautification
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => IsBusy = value);
                return;
            }

            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    public ObservableCollection<OptimizationCategoryViewModel> Categories { get; } = [];

    public ObservableCollection<OptimizationCategoryViewModel> OptimizationCategories { get; } = [];

    public ObservableCollection<OptimizationCategoryViewModel> CleanupCategories { get; } = [];

    public ObservableCollection<OptimizationCategoryViewModel> BeautificationCategories { get; } = [];

    public ObservableCollection<SelectedActionViewModel> SelectedOptimizationActions { get; } = [];

    public ObservableCollection<SelectedActionViewModel> SelectedCleanupActions { get; } = [];

    public ObservableCollection<SelectedActionViewModel> SelectedBeautificationActions { get; } = [];

    public ObservableCollection<SelectedActionViewModel> VisibleSelectedActions => _currentMode switch
    {
        PageMode.Cleanup => SelectedCleanupActions,
        PageMode.Beautification => SelectedBeautificationActions,
        _ => SelectedOptimizationActions
    };

    public ObservableCollection<CustomCleanupRuleViewModel> CustomCleanupRules { get; } = [];

    public bool HasSelectedActions => VisibleSelectedActions.Count > 0;

    public string SelectedActionsSummary
    {
        get
        {
            var count = VisibleSelectedActions.Count;
            var formattedCount = count < 10 ? $"0{count}" : count.ToString();
            return string.Format(_selectedActionsSummaryFormat, formattedCount);
        }
    }

    public string SelectedActionsEmptyText => _selectedActionsEmptyText;

    public long EstimatedCleanupSize
    {
        get => _estimatedCleanupSize;
        private set
        {
            if (_estimatedCleanupSize == value)
                return;

            _estimatedCleanupSize = value;
            OnPropertyChanged(nameof(EstimatedCleanupSize));
            OnPropertyChanged(nameof(EstimatedCleanupSizeText));
        }
    }

    public bool IsCalculatingSize
    {
        get => _isCalculatingSize;
        private set
        {
            if (_isCalculatingSize == value)
                return;

            _isCalculatingSize = value;
            OnPropertyChanged(nameof(IsCalculatingSize));
        }
    }

    public string EstimatedCleanupSizeText
    {
        get
        {
            if (_currentMode != PageMode.Cleanup || EstimatedCleanupSize == 0)
                return string.Empty;

            return string.Format(Resource.WindowsOptimizationPage_EstimatedCleanupSize, FormatBytes(EstimatedCleanupSize));
        }
    }

    public bool TransparencyEnabled
    {
        get => _transparencyEnabled;
        set
        {
            if (_transparencyEnabled == value)
                return;

            _transparencyEnabled = value;
            OnPropertyChanged(nameof(TransparencyEnabled));
            SetTransparencyEnabled(value);
        }
    }

    public bool RoundedCornersEnabled
    {
        get => _roundedCornersEnabled;
        set
        {
            if (_roundedCornersEnabled == value)
                return;

            _roundedCornersEnabled = value;
            OnPropertyChanged(nameof(RoundedCornersEnabled));
        }
    }

    public bool ShadowsEnabled
    {
        get => _shadowsEnabled;
        set
        {
            if (_shadowsEnabled == value)
                return;

            _shadowsEnabled = value;
            OnPropertyChanged(nameof(ShadowsEnabled));
        }
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value)
                return;

            _selectedTheme = value;
            OnPropertyChanged(nameof(SelectedTheme));
        }
    }

    public string BeautificationStatusText { get; private set; } = string.Empty;

    public bool CanInstall
    {
        get
        {
            var isInstalled = NilesoftShellHelper.IsInstalled();
            var isInstalledUsingShellExe = NilesoftShellHelper.IsInstalledUsingShellExe();
            // Can install if shell.exe exists but not registered (not installed using shell.exe API)
            return isInstalled && !isInstalledUsingShellExe;
        }
    }

    public bool CanUninstall
    {
        get
        {
            // Can uninstall if shell is installed and registered (using shell.exe API, which checks both file existence and registration)
            return NilesoftShellHelper.IsInstalledUsingShellExe();
        }
    }

    public string CurrentOperationText
    {
        get => _currentOperationText;
        private set
        {
            if (_currentOperationText == value)
                return;

            _currentOperationText = value;
            OnPropertyChanged(nameof(CurrentOperationText));
        }
    }

    public string CurrentDeletingFile
    {
        get => _currentDeletingFile;
        private set
        {
            if (_currentDeletingFile == value)
                return;

            _currentDeletingFile = value;
            OnPropertyChanged(nameof(CurrentDeletingFile));
        }
    }

    public string PendingText => GetResource("WindowsOptimizationPage_EstimatedCleanupSize_Pending");

    public string CompactText => GetResource("Compact");
    public string ExpandAllText => GetResource("ExpandAll");
    public string CollapseAllText => GetResource("CollapseAll");
    public string ExpandCollapseText
    {
        get => _expandCollapseText;
        private set
        {
            if (_expandCollapseText == value)
                return;
            _expandCollapseText = value;
            OnPropertyChanged(nameof(ExpandCollapseText));
        }
    }

    public bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (_isCompactView == value)
                return;
            _isCompactView = value;
            OnPropertyChanged(nameof(IsCompactView));
        }
    }

    private string _expandCollapseText = string.Empty;

    private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in ActiveCategories)
            category.IsExpanded = true;
        RefreshExpandCollapseText();
    }

    private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in ActiveCategories)
            category.IsExpanded = false;
        RefreshExpandCollapseText();
    }

    private void ToggleExpandCollapseButton_Click(object sender, RoutedEventArgs e)
    {
        var list = ActiveCategories.ToList();
        var allExpanded = list.Count > 0 && list.All(c => c.IsExpanded);
        foreach (var category in list)
            category.IsExpanded = !allExpanded;
        RefreshExpandCollapseText();
    }

    // Beautification is integrated as a regular category; no separate modal.
    private void RefreshExpandCollapseText()
    {
        var list = ActiveCategories.ToList();
        var allExpanded = list.Count > 0 && list.All(c => c.IsExpanded);
        ExpandCollapseText = allExpanded ? CollapseAllText : ExpandAllText;
    }

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

    public IEnumerable<OptimizationCategoryViewModel> ActiveCategories => _currentMode switch
    {
        PageMode.Cleanup => CleanupCategories,
        PageMode.Beautification => BeautificationCategories,
        _ => OptimizationCategories
    };

    public OptimizationCategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory == value)
                return;

            _selectedCategory = value;
            switch (_currentMode)
            {
                case PageMode.Cleanup:
                    _lastCleanupCategory = value;
                    break;
                case PageMode.Beautification:
                    _lastBeautificationCategory = value;
                    break;
                default:
                    _lastOptimizationCategory = value;
                    break;
            }
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    public bool IsCleanupMode => _currentMode == PageMode.Cleanup;
    public bool IsBeautificationMode => _currentMode == PageMode.Beautification;

    public event PropertyChangedEventHandler? PropertyChanged;

    public WindowsOptimizationPage()
    {
        InitializeComponent();
        DataContext = this;

        CustomCleanupRules.CollectionChanged += CustomCleanupRules_CollectionChanged;
        LoadCustomCleanupRules();
        UpdateCleanupControlsState();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (Categories.Count > 0)
            return;

        InitializeCategories();
        SetMode(PageMode.Optimization);
        // Initialize action states first to set checkboxes based on actual system state
        // This ensures checkboxes reflect what's actually applied, not recommended
        await InitializeActionStatesAsync();
        UpdateSelectedActions();
        
        // 确保所有交互状态在初始化后都是启用的
        ToggleInteraction(true, InteractionScope.All);
        
        Unloaded += WindowsOptimizationPage_Unloaded;
    }

    private void WindowsOptimizationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _actionStateRefreshTimer?.Stop();
        _beautificationStatusTimer?.Stop();
        _styleSettingsWindow?.Close();
    }

    private void OpenStyleSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Close existing window if open
            _styleSettingsWindow?.Close();

            // Create and show style settings window
            _styleSettingsWindow = new MenuStyleSettingsWindow
            {
                Owner = Window.GetWindow(this)
            };
            _styleSettingsWindow.Closed += (s, args) => _styleSettingsWindow = null;
            _styleSettingsWindow.Show();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to open style settings window.", ex);
        }
    }

    private void InitializeCategories()
    {
        foreach (var existing in Categories.ToList())
            existing.SelectionChanged -= Category_SelectionChanged;

        Categories.Clear();
        OptimizationCategories.Clear();
        CleanupCategories.Clear();
        BeautificationCategories.Clear();

        var selectionSummaryFormat = GetResource("WindowsOptimization_Category_SelectionSummary");
        if (string.IsNullOrWhiteSpace(selectionSummaryFormat))
            selectionSummaryFormat = "{0} / {1}";

        var recommendedTagText = GetResource("WindowsOptimization_Action_Recommended_Tag");

        _selectedActionsSummaryFormat = GetResource("WindowsOptimizationPage_SelectedActions_Count");
        if (string.IsNullOrWhiteSpace(_selectedActionsSummaryFormat))
            _selectedActionsSummaryFormat = "{0}";

        _selectedActionsEmptyText = GetResource("WindowsOptimizationPage_SelectedActions_Empty");
        OnPropertyChanged(nameof(SelectedActionsEmptyText));

        foreach (var category in WindowsOptimizationService.GetCategories())
        {
            var actions = category.Actions.Select(action => new OptimizationActionViewModel(
                    action.Key,
                    GetResource(action.TitleResourceKey),
                    GetResource(action.DescriptionResourceKey),
                    action.Recommended,
                    recommendedTagText)).ToList();

            // 为每个Action添加事件监听，处理复选框变化的逻辑，立即应用操作
            foreach (var actionVm in actions)
            {
                actionVm.PropertyChanged += async (_, args) =>
                {
                    if (args.PropertyName == nameof(OptimizationActionViewModel.IsSelected))
                    {
                        // 如果正在刷新状态（程序内部操作），不执行命令
                        if (_isRefreshingStates)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"PropertyChanged for {actionVm.Key} ignored: refreshing states");
                            return;
                        }

                        // 标记用户交互，防止立即刷新覆盖用户选择
                        _lastUserInteraction = DateTime.Now;
                        _isUserInteracting = true;

                        // 如果用户取消勾选，检查是否已应用，如果是则立即执行取消操作
                        if (!actionVm.IsSelected)
                        {
                            // 记录用户取消勾选的意图，防止自动刷新时重新勾选
                            _userUncheckedActions.Add(actionVm.Key);
                            await HandleActionUncheckedAsync(actionVm.Key);
                        }
                        else
                        {
                            // 如果用户重新勾选，从取消列表中移除，并立即执行应用操作
                            _userUncheckedActions.Remove(actionVm.Key);
                            await HandleActionCheckedAsync(actionVm.Key);
                        }
                    }
                };
            }

            var categoryVm = new OptimizationCategoryViewModel(
                category.Key,
                GetResource(category.TitleResourceKey),
                GetResource(category.DescriptionResourceKey),
                selectionSummaryFormat,
                actions);

            categoryVm.SelectionChanged += Category_SelectionChanged;
            categoryVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(OptimizationCategoryViewModel.IsExpanded))
                    RefreshExpandCollapseText();
            };
            Categories.Add(categoryVm);

            if (category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
                CleanupCategories.Add(categoryVm);
            else if (category.Key.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase))
                BeautificationCategories.Add(categoryVm);
            else
                OptimizationCategories.Add(categoryVm);
        }

        OnPropertyChanged(nameof(ActiveCategories));
        RefreshCleanupActionAvailability();
        SelectedCategory = ActiveCategories.FirstOrDefault();
        RefreshExpandCollapseText();
    }

    private async void ApplyBeautificationButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedKeys = Categories
            .Where(category => category.Key.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.SelectedActionKeys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 如果没有选择任何操作，只刷新状态，不执行任何操作
        if (selectedKeys.Count == 0)
        {
            // 刷新美化状态
            await RefreshBeautificationStatusAsync();
            // 刷新操作状态
            await RefreshActionStatesAsync(skipUserInteractionCheck: true);
            return;
        }

        await ExecuteAsync(
            ct => _windowsOptimizationService.ExecuteActionsAsync(selectedKeys, ct),
            Resource.WindowsOptimizationPage_ApplySelected_Success,
            Resource.WindowsOptimizationPage_ApplySelected_Error,
            InteractionScope.Beautification);

        // 标记用户交互，防止立即刷新覆盖用户选择
        _lastUserInteraction = DateTime.Now;
        _isUserInteracting = true;
        
        // 延迟刷新状态，给 shell 注册操作足够的时间完成
        // shell 注册需要重启资源管理器，可能需要几秒钟
        await Task.Delay(2000);
        
        // Force refresh action states after execution to update checkboxes
        await RefreshActionStatesAsync(skipUserInteractionCheck: true);
        
        // 重置交互标志
        _isUserInteracting = false;
        
        // Refresh beautification status after applying
        _ = RefreshBeautificationStatusAsync();
    }

    private async void RunCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedKeys = SelectedCleanupActions
            .Where(a => !string.IsNullOrWhiteSpace(a.CategoryKey))
            .Select(a => a.ActionKey)
            .ToList();

        // 如果没有选择任何操作，只刷新状态，不执行任何操作
        if (selectedKeys.Count == 0)
        {
            // 刷新操作状态
            await RefreshActionStatesAsync(skipUserInteractionCheck: true);
            return;
        }

        await ExecuteAsync(
            async ct =>
            {
                // Execute cleanup actions
                if (selectedKeys.Count > 0)
                {
                    // Verify that all selected keys exist in the service
                    var actionsByKey = WindowsOptimizationService.GetCategories()
                        .SelectMany(c => c.Actions)
                        .ToDictionary(a => a.Key, a => a, StringComparer.OrdinalIgnoreCase);
                    
                    var missingKeys = selectedKeys.Where(k => !actionsByKey.ContainsKey(k)).ToList();
                    if (missingKeys.Any())
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Some selected action keys were not found: {string.Join(", ", missingKeys)}");
                        throw new InvalidOperationException($"无法找到以下操作: {string.Join(", ", missingKeys)}");
                    }
                    
                    // Run one by one, show progress with per-step timing and freed size
                    var actionsInOrder = SelectedCleanupActions
                        .Where(a => selectedKeys.Contains(a.ActionKey, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (actionsInOrder.Count == 0)
                    {
                        throw new InvalidOperationException("没有找到要执行的操作。请确保已选择有效的清理操作。");
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Starting cleanup execution. Selected actions: {actionsInOrder.Count}, Keys: {string.Join(", ", selectedKeys)}");

                    long totalFreedBytes = 0;
                    var swOverall = System.Diagnostics.Stopwatch.StartNew();
                    foreach (var action in actionsInOrder)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            CurrentOperationText = string.Format(GetResource("WindowsOptimizationPage_RunningStep"), action.ActionTitle);
                            CurrentDeletingFile = string.Empty;
                        });
                        
                        long sizeBefore = 0;
                        try 
                        { 
                            sizeBefore = await _windowsOptimizationService.EstimateActionSizeAsync(action.ActionKey, ct).ConfigureAwait(false);
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Estimated size before cleanup: {FormatBytes(sizeBefore)} for action: {action.ActionKey}");
                        } 
                        catch (Exception ex)
                        { 
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Failed to estimate size before cleanup for action: {action.ActionKey}", ex);
                        }
                        
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        
                        try
                        {
                            // For custom cleanup, show file deletion progress
                            if (action.ActionKey.Equals(WindowsOptimizationService.CustomCleanupActionKey, StringComparison.OrdinalIgnoreCase))
                            {
                                await ExecuteCustomCleanupWithProgressAsync(ct);
                            }
                            else
                            {
                                await _windowsOptimizationService.ExecuteActionsAsync([action.ActionKey], ct).ConfigureAwait(false);
                            }
                            
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Action executed: {action.ActionKey}, elapsed: {sw.Elapsed.TotalSeconds:0.0}s");
                        }
                        catch (Exception ex)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Action execution failed: {action.ActionKey}", ex);
                            throw; // Re-throw to show error to user
                        }
                        
                        sw.Stop();
                        
                        // Wait a bit to ensure file system operations complete
                        await Task.Delay(500, ct).ConfigureAwait(false);
                        
                        long sizeAfter = 0;
                        try 
                        { 
                            sizeAfter = await _windowsOptimizationService.EstimateActionSizeAsync(action.ActionKey, ct).ConfigureAwait(false);
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Estimated size after cleanup: {FormatBytes(sizeAfter)} for action: {action.ActionKey}");
                        } 
                        catch (Exception ex)
                        { 
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Failed to estimate size after cleanup for action: {action.ActionKey}", ex);
                        }
                        
                        var freed = Math.Max(0, sizeBefore - sizeAfter);
                        totalFreedBytes += freed;
                        
                        await Dispatcher.InvokeAsync(() =>
                        {
                            CurrentOperationText = $"{action.ActionTitle} ✓ {FormatBytes(freed)} in {sw.Elapsed.TotalSeconds:0.0}s";
                            CurrentDeletingFile = string.Empty;
                        });
                        
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Cleanup result for {action.ActionKey}: freed {FormatBytes(freed)}, before: {FormatBytes(sizeBefore)}, after: {FormatBytes(sizeAfter)}");
                    }
                    swOverall.Stop();
                    var summaryFmt = GetResource("WindowsOptimizationPage_CleanupSummary");
                    if (string.IsNullOrWhiteSpace(summaryFmt))
                        summaryFmt = "Freed {0} in {1}s";
                    await Dispatcher.InvokeAsync(() =>
                        SnackbarHelper.Show(GetResource("SettingsPage_WindowsOptimization_Title"),
                            string.Format(summaryFmt, FormatBytes(totalFreedBytes), swOverall.Elapsed.TotalSeconds.ToString("0.0")),
                            SnackbarType.Success));
                    await Dispatcher.InvokeAsync(() =>
                    {
                        CurrentOperationText = string.Empty;
                        CurrentDeletingFile = string.Empty;
                    });
                }
            },
            Resource.SettingsPage_WindowsOptimization_Cleanup_Success,
            Resource.SettingsPage_WindowsOptimization_Cleanup_Error,
            InteractionScope.Cleanup);

        // After cleanup completes, re-estimate the remaining cleanup size
        // in case some files were not removable or new files appeared.
        try
        {
            // Reset the last value to force UI update even if it stays the same
            EstimatedCleanupSize = 0;
            await UpdateEstimatedCleanupSizeAsync();
        }
        catch
        {
            // ignore estimation errors
        }
    }

    private void SelectRecommendedButton_Click(object sender, RoutedEventArgs e)
    {
        SelectRecommended(ActiveCategories);
        UpdateSelectedActions();
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in ActiveCategories)
            category.ClearSelection();

        UpdateSelectedActions();
    }

    private void SelectRecommended(IEnumerable<OptimizationCategoryViewModel> categories)
    {
        foreach (var category in categories)
            category.SelectRecommended();
    }

    private async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        string successMessage,
        string errorMessage,
        InteractionScope scope)
    {
        if (IsBusy)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ExecuteAsync skipped: IsBusy is true");
            return;
        }
        
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ExecuteAsync started: successMessage={successMessage}");

        IsBusy = true;
        ToggleInteraction(false, scope);

        try
        {
            ShowOperationIndicator(true);
            await operation(CancellationToken.None).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, successMessage, SnackbarType.Success));
        }
        catch (OperationCanceledException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Windows optimization action was cancelled.");
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, "操作已取消", SnackbarType.Warning));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Windows optimization action failed. Exception: {ex.Message}", ex);

            var detailedError = $"{errorMessage}\n错误详情: {ex.Message}";
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, detailedError, SnackbarType.Error));
        }
        finally
        {
            IsBusy = false;
            Dispatcher.Invoke(() => ToggleInteraction(true, scope));
            ShowOperationIndicator(false);
        }
    }

    private void ToggleInteraction(bool isEnabled, InteractionScope scope)
    {
        if (scope.HasFlag(InteractionScope.Optimization))
            _optimizationInteractionEnabled = isEnabled;

        if (scope.HasFlag(InteractionScope.Cleanup))
            _cleanupInteractionEnabled = isEnabled;

        if (scope.HasFlag(InteractionScope.Beautification))
            _beautificationInteractionEnabled = isEnabled;

        foreach (var category in GetCategoriesForScope(scope))
            category.SetEnabled(isEnabled);

        ApplyInteractionState();
        UpdateCleanupControlsState();
    }

    private IEnumerable<OptimizationCategoryViewModel> GetCategoriesForScope(InteractionScope scope)
    {
        foreach (var category in Categories)
        {
            var isCleanupCategory = category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase);
            var isBeautificationCategory = category.Key.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase);

            if (isCleanupCategory && scope.HasFlag(InteractionScope.Cleanup))
                yield return category;
            else if (isBeautificationCategory && scope.HasFlag(InteractionScope.Beautification))
                yield return category;
            else if (!isCleanupCategory && !isBeautificationCategory && scope.HasFlag(InteractionScope.Optimization))
                yield return category;
        }
    }

    private void ApplyInteractionState()
    {
        var optimizationEnabled = _optimizationInteractionEnabled;
        var cleanupEnabled = _cleanupInteractionEnabled;
        var beautificationEnabled = _beautificationInteractionEnabled;

        var primaryButtonsEnabled = _currentMode switch
        {
            PageMode.Cleanup => cleanupEnabled,
            PageMode.Beautification => beautificationEnabled,
            _ => optimizationEnabled
        };

        if (_selectRecommendedButton != null)
            _selectRecommendedButton.IsEnabled = primaryButtonsEnabled;

        if (_clearButton != null)
            _clearButton.IsEnabled = primaryButtonsEnabled;

        if (_runCleanupButton != null)
            _runCleanupButton.IsEnabled = cleanupEnabled;

        if (_categoriesList != null)
        {
            var listEnabled = _currentMode switch
            {
                PageMode.Cleanup => cleanupEnabled,
                PageMode.Beautification => beautificationEnabled,
                _ => optimizationEnabled
            };
            _categoriesList.IsEnabled = listEnabled;
        }
    }

    private void ShowOperationIndicator(bool isVisible)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ShowOperationIndicator(isVisible));
            return;
        }

        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        if (_operationProgressBar != null)
        {
            _operationProgressBar.IsIndeterminate = isVisible;
            _operationProgressBar.Visibility = visibility;
        }
    }

    private static string GetResource(string resourceKey) =>
        string.IsNullOrWhiteSpace(resourceKey)
            ? string.Empty
            : Resource.ResourceManager.GetString(resourceKey) ?? resourceKey;

    private void Category_SelectionChanged(object? sender, EventArgs e)
    {
        _lastUserInteraction = DateTime.Now;
        _isUserInteracting = true;
        UpdateSelectedActions();
        // 3秒后重置交互标志，给用户足够的时间完成操作
        Task.Delay(3000).ContinueWith(_ => _isUserInteracting = false);
    }

    private void UpdateSelectedActions()
    {
        // Build fresh selection snapshots for comparison
        var newOptimizationActions = new List<SelectedActionViewModel>();
        var newCleanupActions = new List<SelectedActionViewModel>();
        var newBeautificationActions = new List<SelectedActionViewModel>();

        foreach (var category in Categories)
        {
            List<SelectedActionViewModel> target;
            if (category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
                target = newCleanupActions;
            else if (category.Key.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase))
                target = newBeautificationActions;
            else
                target = newOptimizationActions;

            foreach (var action in category.Actions.Where(action => action.IsEnabled && action.IsSelected))
                target.Add(new SelectedActionViewModel(category.Key, category.Title, action.Key, action.Title, action.Description, action));
        }


        // Incrementally update SelectedOptimizationActions
        UpdateCollection(SelectedOptimizationActions, newOptimizationActions);
        // Incrementally update SelectedCleanupActions
        UpdateCollection(SelectedCleanupActions, newCleanupActions);
        // Incrementally update SelectedBeautificationActions
        UpdateCollection(SelectedBeautificationActions, newBeautificationActions);

        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));

        if (_currentMode == PageMode.Cleanup)
        {
            _ = UpdateEstimatedCleanupSizeAsync();
        }
        else
        {
            EstimatedCleanupSize = 0;
        }
    }

    private static void UpdateCollection<T>(ObservableCollection<T> existing, List<T> updated)
    {
        // Skip updates if both collections contain identical elements
        if (existing.Count == updated.Count && existing.SequenceEqual(updated))
            return;

        // Apply incremental updates instead of clearing everything to keep scroll state intact
        // 1. Remove items that no longer exist
        for (int i = existing.Count - 1; i >= 0; i--)
        {
            var item = existing[i];
            if (!updated.Contains(item))
            {
                if (item is IDisposable disposable)
                    disposable.Dispose();
                existing.RemoveAt(i);
            }
        }

        // 2. Reorder or insert entries to match the updated sequence
        for (int i = 0; i < updated.Count; i++)
        {
            var item = updated[i];

            if (i < existing.Count)
            {
                if (EqualityComparer<T>.Default.Equals(existing[i], item))
                    continue;

                var existingIndex = existing.IndexOf(item);
                if (existingIndex >= 0)
                {
                    existing.RemoveAt(existingIndex);
                    existing.Insert(i, item);
                }
                else
                {
                    existing.Insert(i, item);
                }
            }
            else
            {
                existing.Add(item);
            }
        }

        // 3. Remove any trailing entries that no longer have counterparts
        while (existing.Count > updated.Count)
        {
            var lastIndex = existing.Count - 1;
            if (existing[lastIndex] is IDisposable disposable)
                disposable.Dispose();
            existing.RemoveAt(lastIndex);
        }
    }

    private void LoadCustomCleanupRules()
    {
        _isLoadingCustomCleanupRules = true;
        try
        {
            foreach (var rule in CustomCleanupRules.ToList())
                DetachRuleEvents(rule);

            CustomCleanupRules.Clear();

            var rules = _applicationSettings.Store.CustomCleanupRules ?? new List<CustomCleanupRule>();
            foreach (var rule in rules)
            {
                var viewModel = new CustomCleanupRuleViewModel(
                    rule.DirectoryPath,
                    rule.Extensions ?? new List<string>(),
                    rule.Recursive);

                CustomCleanupRules.Add(viewModel);
            }
        }
        finally
        {
            _isLoadingCustomCleanupRules = false;
        }

        RefreshCleanupActionAvailability();
        UpdateCleanupControlsState();
    }


    private void SaveCustomCleanupRules()
    {
        var rules = CustomCleanupRules
            .Select(rule => rule.ToModel())
            .ToList();

        _applicationSettings.Store.CustomCleanupRules = rules;
        _applicationSettings.SynchronizeStore();
    }

    private void RefreshCleanupActionAvailability()
    {
        var hasCustomRules = CustomCleanupRules.Count > 0;

        foreach (var category in Categories)
        {
            var customAction = category.Actions.FirstOrDefault(action =>
                string.Equals(action.Key, WindowsOptimizationService.CustomCleanupActionKey, StringComparison.OrdinalIgnoreCase));

            if (customAction is not null)
            {
                customAction.IsEnabled = hasCustomRules;

                if (!hasCustomRules && customAction.IsSelected)
                    customAction.IsSelected = false;
            }
        }
    }

    private void CustomCleanupRules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (CustomCleanupRuleViewModel rule in e.OldItems)
                DetachRuleEvents(rule);
        }

        if (e.NewItems is not null)
        {
            foreach (CustomCleanupRuleViewModel rule in e.NewItems)
                AttachRuleEvents(rule);
        }

        OnCustomCleanupRulesChanged();
    }

    private void CustomCleanupRuleViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) => OnCustomCleanupRulesChanged();

    private void CustomCleanupRuleExtensions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnCustomCleanupRulesChanged();

    private void OnCustomCleanupRulesChanged()
    {
        if (_isLoadingCustomCleanupRules)
            return;

        SaveCustomCleanupRules();
        RefreshCleanupActionAvailability();
        UpdateCleanupControlsState();

        if (_currentMode == PageMode.Cleanup)
        {
            _ = UpdateEstimatedCleanupSizeAsync();
        }
    }


    private void AttachRuleEvents(CustomCleanupRuleViewModel rule)
    {
        rule.PropertyChanged += CustomCleanupRuleViewModel_PropertyChanged;
        rule.Extensions.CollectionChanged += CustomCleanupRuleExtensions_CollectionChanged;
    }

    private void DetachRuleEvents(CustomCleanupRuleViewModel rule)
    {
        rule.PropertyChanged -= CustomCleanupRuleViewModel_PropertyChanged;
        rule.Extensions.CollectionChanged -= CustomCleanupRuleExtensions_CollectionChanged;
    }

    private void UpdateCleanupControlsState()
    {
        if (_addCustomCleanupRuleButton != null)
        {
            if (IsBusy)
                _addCustomCleanupRuleButton.IsEnabled = false;
            else
                _addCustomCleanupRuleButton.IsEnabled = true;
        }

        if (_clearCustomCleanupRulesButton != null)
        {
            if (IsBusy)
                _clearCustomCleanupRulesButton.IsEnabled = false;
            else
                _clearCustomCleanupRulesButton.IsEnabled = CustomCleanupRules.Count > 0;
        }
    }

    private void AddCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new CustomCleanupRuleWindow { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true && window.Result is CustomCleanupRule rule)
        {
            var viewModel = new CustomCleanupRuleViewModel(rule.DirectoryPath, rule.Extensions ?? new List<string>(), rule.Recursive);
            CustomCleanupRules.Add(viewModel);
        }
    }

    private void EditCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
            return;

        if (button.Tag is not CustomCleanupRuleViewModel viewModel)
            return;

        var window = new CustomCleanupRuleWindow(viewModel.ToModel()) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true && window.Result is CustomCleanupRule rule)
        {
            _isLoadingCustomCleanupRules = true;
            try
            {
                viewModel.DirectoryPath = rule.DirectoryPath;
                viewModel.Recursive = rule.Recursive;

                viewModel.Extensions.CollectionChanged -= CustomCleanupRuleExtensions_CollectionChanged;
                viewModel.Extensions.Clear();
                foreach (var extension in rule.Extensions ?? new List<string>())
                    viewModel.Extensions.Add(extension);
                viewModel.Extensions.CollectionChanged += CustomCleanupRuleExtensions_CollectionChanged;

                viewModel.NotifyExtensionsChanged();
            }
            finally
            {
                _isLoadingCustomCleanupRules = false;
            }

            OnCustomCleanupRulesChanged();
        }
    }

    private void RemoveCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
            return;

        if (button.Tag is not CustomCleanupRuleViewModel viewModel)
            return;

        CustomCleanupRules.Remove(viewModel);
    }

    private void ClearCustomCleanupRulesButton_Click(object sender, RoutedEventArgs e)
    {
        if (CustomCleanupRules.Count == 0)
            return;

        foreach (var rule in CustomCleanupRules.ToList())
            DetachRuleEvents(rule);

        CustomCleanupRules.Clear();
    }


    private async Task UpdateEstimatedCleanupSizeAsync()
    {
        _sizeCalculationCts?.Cancel();
        _sizeCalculationCts?.Dispose();
        _sizeCalculationCts = new CancellationTokenSource();

        var cancellationToken = _sizeCalculationCts.Token;

        try
        {
            IsCalculatingSize = true;
            // Debounce rapid changes in selection to avoid spamming estimations
            await Task.Delay(300, cancellationToken).ConfigureAwait(false);
            var actionKeys = SelectedCleanupActions.Select(a => a.ActionKey).ToList();
            var size = await _windowsOptimizationService.EstimateCleanupSizeAsync(actionKeys, cancellationToken).ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                EstimatedCleanupSize = size;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to estimate cleanup size.", ex);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IsCalculatingSize = false;
            }
        }
    }

    private void SetMode(PageMode mode)
    {
        var modeChanged = _currentMode != mode;
        _currentMode = mode;

        if (modeChanged)
        {
            OnPropertyChanged(nameof(IsCleanupMode));
            OnPropertyChanged(nameof(IsBeautificationMode));
            OnPropertyChanged(nameof(ActiveCategories));

            if (mode == PageMode.Cleanup)
            {
                if (!_hasInitializedCleanupMode)
                {
                    _hasInitializedCleanupMode = true;
                    // Select recommended actions by default when first entering cleanup mode
                    var activeCategories = ActiveCategories.ToList();
                    SelectRecommended(activeCategories);
                    UpdateSelectedActions();
                }
                _ = UpdateEstimatedCleanupSizeAsync();
            }
            else if (mode == PageMode.Beautification)
            {
                // 确保切换到美化模式时交互是启用的
                _beautificationInteractionEnabled = true;
                foreach (var category in BeautificationCategories)
                    category.SetEnabled(true);
                
                _ = RefreshBeautificationStatusAsync();
                StartBeautificationStatusMonitoring();
                TransparencyEnabled = GetTransparencyEnabled();
            }
            else
            {
                EstimatedCleanupSize = 0;
                _beautificationStatusTimer?.Stop();
            }
        }
        else
        {
            // Force a refresh even if the mode appears unchanged to keep bindings current
            OnPropertyChanged(nameof(ActiveCategories));
        }

        var activeCategoriesList = ActiveCategories.ToList();
        var preferredCategory = mode switch
        {
            PageMode.Cleanup => _lastCleanupCategory,
            PageMode.Beautification => _lastBeautificationCategory,
            _ => _lastOptimizationCategory
        };
        if (preferredCategory is null || !activeCategoriesList.Contains(preferredCategory))
            preferredCategory = activeCategoriesList.FirstOrDefault();

        SelectedCategory = preferredCategory;
        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
        ApplyInteractionState();
        RefreshExpandCollapseText();
    }

    private async Task InitializeActionStatesAsync()
    {
        // 初始化时跳过用户交互检查，确保所有复选框状态正确设置
        await RefreshActionStatesAsync(skipUserInteractionCheck: true);
        StartActionStateMonitoring();
    }

    private async Task RefreshActionStatesAsync(bool skipUserInteractionCheck = false)
    {
        // 如果用户正在交互（最近5秒内有操作），跳过更新以避免干扰用户操作
        // 但在初始化时可以跳过这个检查
        if (!skipUserInteractionCheck && (_isUserInteracting || (DateTime.Now - _lastUserInteraction).TotalSeconds < 5))
        {
            return;
        }

        // 标记正在刷新状态，防止触发命令执行
        _isRefreshingStates = true;

        try
        {
            var actions = Categories.SelectMany(category => category.Actions).ToList();

            foreach (var action in actions)
            {
                var applied = await _windowsOptimizationService.TryGetActionAppliedAsync(action.Key, CancellationToken.None).ConfigureAwait(false);
                if (applied.HasValue)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // 只在状态实际改变时才更新，避免覆盖用户的手动选择
                        var timeSinceLastInteraction = (DateTime.Now - _lastUserInteraction).TotalSeconds;
                        var isRecentInteraction = timeSinceLastInteraction < 10; // 10秒内的交互认为是最近的操作
                        
                        // 如果用户在取消列表中，且当前状态是已应用，说明取消操作可能还在进行中，不要自动勾选
                        if (_userUncheckedActions.Contains(action.Key) && applied.Value)
                        {
                            // 用户明确取消了勾选，即使系统状态还是已应用，也不要自动勾选
                            // 只有当系统状态变成未应用时，才从取消列表中移除
                            if (!applied.Value)
                            {
                                _userUncheckedActions.Remove(action.Key);
                                action.IsSelected = false;
                            }
                            else
                            {
                                // 确保复选框保持未勾选状态
                                if (action.IsSelected)
                                {
                                    action.IsSelected = false;
                                }
                            }
                            return;
                        }
                        
                        if (action.IsSelected != applied.Value)
                        {
                            // 如果用户最近有交互，且用户选择了但检查显示未应用，可能是操作正在进行中，不立即更新
                            // 但如果用户未选择而检查显示已应用，应该更新（可能是外部操作导致的）
                            if (isRecentInteraction && action.IsSelected && !applied.Value)
                            {
                                // 用户选择了但检查显示未应用，可能是操作正在进行中，暂时不更新
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Skipping state update for {action.Key}: user selected but not yet applied (operation may be in progress)");
                            }
                            else
                            {
                                action.IsSelected = applied.Value;
                                // 如果状态更新为未应用，从取消列表中移除（说明取消操作已完成）
                                if (!applied.Value && _userUncheckedActions.Contains(action.Key))
                                {
                                    _userUncheckedActions.Remove(action.Key);
                                }
                            }
                        }
                    });
                }
            }

            await Dispatcher.InvokeAsync(UpdateSelectedActions);
        }
        finally
        {
            // 重置标志，允许后续的用户操作触发命令
            _isRefreshingStates = false;
        }
    }

    private async Task HandleActionCheckedAsync(string actionKey)
    {
        try
        {
            // 确定交互范围
            var interactionScope = actionKey.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase)
                ? InteractionScope.Beautification
                : actionKey.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase)
                ? InteractionScope.Cleanup
                : InteractionScope.Optimization;

            // 对于美化操作，总是执行命令，不检查当前状态
            if (actionKey.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase))
            {
                // 对于右键美化操作，直接执行安装命令，不管当前状态
                if (actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"User checked beautify action {actionKey}, executing install/register command");
                    
                    var shellExe = NilesoftShellHelper.GetNilesoftShellExePath();
                    if (!string.IsNullOrWhiteSpace(shellExe))
                    {
                        await ExecuteAsync(
                            async ct =>
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Executing shell.exe register command: {shellExe} -register -treat -restart");
                                
                                await Task.Run(() =>
                                {
                                    var process = new System.Diagnostics.Process
                                    {
                                        StartInfo = new System.Diagnostics.ProcessStartInfo
                                        {
                                            FileName = "cmd.exe",
                                            Arguments = $"/c \"\"{shellExe}\"\" -register -treat -restart",
                                            UseShellExecute = false,
                                            CreateNoWindow = true
                                        }
                                    };
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"Starting process: cmd.exe {process.StartInfo.Arguments}");
                                    process.Start();
                                    process.WaitForExit();
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"Process exited with code: {process.ExitCode}");
                                });
                            },
                            Resource.WindowsOptimizationPage_ApplySelected_Success,
                            Resource.WindowsOptimizationPage_ApplySelected_Error,
                            interactionScope);
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Shell.exe not found, cannot execute install command");
                    }
                }
                else
                {
                    await ExecuteAsync(
                        ct => _windowsOptimizationService.ExecuteActionsAsync([actionKey], ct),
                        Resource.WindowsOptimizationPage_ApplySelected_Success,
                        Resource.WindowsOptimizationPage_ApplySelected_Error,
                        interactionScope);
                }
                
                // 延迟刷新状态，给操作足够的时间完成
                // shell 注册需要重启资源管理器，可能需要几秒钟
                var delay = actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase) ? 3000 : 2000;
                await Task.Delay(delay);
                
                // 刷新操作状态
                await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                
                // 刷新美化状态
                _ = RefreshBeautificationStatusAsync();
            }
            else
            {
                // 对于非美化操作，检查Action是否已应用
                var applied = await _windowsOptimizationService.TryGetActionAppliedAsync(actionKey, CancellationToken.None).ConfigureAwait(false);
                
                // 如果未应用，立即执行应用操作
                if (applied.HasValue && !applied.Value)
                {
                    await ExecuteAsync(
                        ct => _windowsOptimizationService.ExecuteActionsAsync([actionKey], ct),
                        Resource.WindowsOptimizationPage_ApplySelected_Success,
                        Resource.WindowsOptimizationPage_ApplySelected_Error,
                        interactionScope);
                    
                    // 延迟刷新状态，给操作足够的时间完成
                    await Task.Delay(2000);
                    
                    // 刷新操作状态
                    await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to handle checked action: {actionKey}", ex);
        }
        finally
        {
            // 重置交互标志
            _isUserInteracting = false;
        }
    }

    private async Task HandleActionUncheckedAsync(string actionKey)
    {
        try
        {
            // 对于右键美化，当用户取消勾选时，立即执行卸载操作（实时应用）
            // 移除状态检查，总是执行卸载命令
            if (actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"User unchecked beautify action {actionKey}, executing uninstall command");
                
                // 直接执行卸载命令，总是执行，不管当前状态
                var shellExe = NilesoftShellHelper.GetNilesoftShellExePath();
                if (!string.IsNullOrWhiteSpace(shellExe))
                {
                    await ExecuteAsync(
                        async ct =>
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Executing shell.exe unregister command: {shellExe} -unregister -restart");
                            
                            await Task.Run(() =>
                            {
                                var process = new System.Diagnostics.Process
                                {
                                    StartInfo = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "cmd.exe",
                                        Arguments = $"/c \"\"{shellExe}\"\" -unregister -restart",
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    }
                                };
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Starting process: cmd.exe {process.StartInfo.Arguments}");
                                process.Start();
                                process.WaitForExit();
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Process exited with code: {process.ExitCode}");
                            });
                        },
                        Resource.WindowsOptimizationPage_ApplySelected_Success,
                        Resource.WindowsOptimizationPage_ApplySelected_Error,
                        InteractionScope.Beautification);
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Shell.exe not found, cannot execute uninstall command");
                }
                    
                // 延迟刷新状态，给 shell 卸载操作足够的时间完成
                await Task.Delay(3000); // 增加到3秒，给卸载操作更多时间
                await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                
                // 检查卸载是否成功，如果成功则从取消列表中移除
                var isStillInstalled = await Task.Run(() => NilesoftShellHelper.IsInstalledUsingShellExe()).ConfigureAwait(false);
                if (!isStillInstalled)
                {
                    _userUncheckedActions.Remove(actionKey);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Uninstall successful, removed {actionKey} from unchecked list");
                }
                
                // 刷新美化状态
                _ = RefreshBeautificationStatusAsync();
            }
            else
            {
                // 对于优化和清理操作，检查Action是否已应用
                var applied = await _windowsOptimizationService.TryGetActionAppliedAsync(actionKey, CancellationToken.None).ConfigureAwait(false);
                
                if (applied.HasValue && applied.Value)
                {
                    // Action已应用，但用户取消勾选，实时撤销操作
                    // 对于优化操作（注册表、服务），可以通过删除注册表值或启用服务来撤销
                    // 对于清理操作，无法撤销，只能刷新状态
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"User unchecked action {actionKey}, attempting to undo: {actionKey}");
                    
                    // 尝试执行撤销操作
                    await UndoOptimizationActionAsync(actionKey);
                    
                    // 延迟刷新状态，给撤销操作足够的时间完成
                    await Task.Delay(1000); // 1秒延迟，给撤销操作时间
                    await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                }
                else
                {
                    // Action未应用，只需刷新状态
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"User unchecked action {actionKey}, action not applied, refreshing state");
                    await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to handle unchecked action: {actionKey}", ex);
        }
        finally
        {
            // 重置交互标志
            _isUserInteracting = false;
        }
    }

    private async Task UndoOptimizationActionAsync(string actionKey)
    {
        try
        {
            // 对于清理操作，无法撤销，只刷新状态
            if (actionKey.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Cleanup action {actionKey} cannot be undone, skipping undo operation");
                return;
            }

            // 对于优化操作，尝试撤销
            // 由于优化操作主要是注册表修改和服务禁用，撤销方式为：
            // 1. 注册表操作：删除注册表值（恢复默认）
            // 2. 服务操作：启用服务（将 Start 值改为自动或手动）
            // 3. 命令操作：大部分不可撤销
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting to undo optimization action: {actionKey}");
            
            // 检查操作是否已应用
            var applied = await _windowsOptimizationService.TryGetActionAppliedAsync(actionKey, CancellationToken.None).ConfigureAwait(false);
            
            if (applied.HasValue && applied.Value)
            {
                // 由于我们无法直接获取操作的具体内容（哪些注册表项、哪些服务）
                // 这里我们通过执行反向操作来实现撤销
                // 对于注册表操作，删除注册表值；对于服务操作，启用服务
                // 但由于操作定义的复杂性，目前先记录日志，实际的撤销逻辑需要根据操作类型来实现
                
                // 尝试通过再次检查操作状态来确认是否需要撤销
                // 如果操作已应用，我们需要通过删除注册表值或启用服务来撤销
                // 但由于我们不知道具体的操作内容，这里先实现一个占位符
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Action {actionKey} is applied, attempting to undo by reversing registry/service changes");
                
                // TODO: 实现具体的撤销逻辑
                // 需要根据 actionKey 查找对应的操作定义，然后执行反向操作
                // 对于注册表操作：删除注册表值
                // 对于服务操作：启用服务（Start = 2 或 3）
                // 对于命令操作：无法撤销
                
                // 目前先通过刷新状态来反映取消操作
                // 实际的撤销逻辑需要在 WindowsOptimizationService 中添加 UndoActionAsync 方法
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to undo optimization action: {actionKey}", ex);
        }
    }

    private void StartActionStateMonitoring()
    {
        _actionStateRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10) // Check every 10 seconds (reduced frequency to avoid interfering with user interaction)
        };
        _actionStateRefreshTimer.Tick += async (s, e) => await RefreshActionStatesAsync();
        _actionStateRefreshTimer.Start();
    }

    private void OptimizationNavButton_Checked(object sender, RoutedEventArgs e) => SetMode(PageMode.Optimization);

    private void CleanupNavButton_Checked(object sender, RoutedEventArgs e) => SetMode(PageMode.Cleanup);

    private void BeautificationNavButton_Checked(object sender, RoutedEventArgs e) => SetMode(PageMode.Beautification);

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public class CustomCleanupRuleViewModel : INotifyPropertyChanged
    {
        private string _directoryPath;
        private bool _recursive;

        public CustomCleanupRuleViewModel(string directoryPath, IEnumerable<string> extensions, bool recursive)
        {
            _directoryPath = directoryPath;
            _recursive = recursive;

            var normalizedExtensions = extensions?
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => extension.Trim())
                .ToList() ?? new List<string>();

            Extensions = new ObservableCollection<string>(normalizedExtensions);
            Extensions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ExtensionsDisplay));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> Extensions { get; }

        public string DirectoryPath
        {
            get => _directoryPath;
            set
            {
                if (_directoryPath == value)
                    return;

                _directoryPath = value;
                OnPropertyChanged(nameof(DirectoryPath));
            }
        }

        public bool Recursive
        {
            get => _recursive;
            set
            {
                if (_recursive == value)
                    return;

                _recursive = value;
                OnPropertyChanged(nameof(Recursive));
            }
        }

        public string ExtensionsDisplay =>
            Extensions.Count == 0
                ? string.Empty
                : string.Join(", ", Extensions);

        public void NotifyExtensionsChanged() => OnPropertyChanged(nameof(ExtensionsDisplay));

        public CustomCleanupRule ToModel() => new()
        {
            DirectoryPath = DirectoryPath,
            Recursive = Recursive,
            Extensions = Extensions.ToList()
        };

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public class OptimizationCategoryViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private bool _isExpanded = false; // Default to collapsed
        private readonly string _selectionSummaryFormat;

        public OptimizationCategoryViewModel(string key, string title, string description, string selectionSummaryFormat, IEnumerable<OptimizationActionViewModel> actions)
        {
            Key = key;
            Title = title;
            Description = description;
            _selectionSummaryFormat = string.IsNullOrWhiteSpace(selectionSummaryFormat) ? "{0} / {1}" : selectionSummaryFormat;
            Actions = new ObservableCollection<OptimizationActionViewModel>(actions);

            foreach (var action in Actions)
                action.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(OptimizationActionViewModel.IsSelected))
                        RaiseSelectionChanged();
                };

            RaiseSelectionChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? SelectionChanged;

        public string Key { get; }

        public string Title { get; }

        public string Description { get; }

        public ObservableCollection<OptimizationActionViewModel> Actions { get; }

        public int SelectedActionCount => Actions.Count(action => action.IsSelected);

        public string SelectionSummary => string.Format(_selectionSummaryFormat, SelectedActionCount, Actions.Count);

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
                var enabledActions = Actions.Where(action => action.IsEnabled).ToList();
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

                foreach (var action in Actions.Where(action => action.IsEnabled))
                    action.IsSelected = value.Value;

                OnPropertyChanged(nameof(HeaderCheckState));
            }
        }

        public IEnumerable<string> SelectedActionKeys =>
            Actions.Where(action => action.IsEnabled && action.IsSelected).Select(action => action.Key);

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

        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;

            foreach (var action in Actions)
                action.IsEnabled = isEnabled;

            RaiseSelectionChanged();
        }

        public void SelectRecommended()
        {
            foreach (var action in Actions.Where(action => action.IsEnabled))
                action.IsSelected = action.Recommended;

            RaiseSelectionChanged();
        }

        public void ClearSelection()
        {
            foreach (var action in Actions.Where(action => action.IsEnabled))
                action.IsSelected = false;

            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            OnPropertyChanged(nameof(HeaderCheckState));
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(SelectedActionCount));
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class SelectedActionViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly OptimizationActionViewModel? _sourceAction;

        public SelectedActionViewModel(
            string categoryKey,
            string categoryTitle,
            string actionKey,
            string actionTitle,
            string description,
            OptimizationActionViewModel sourceAction)
        {
            CategoryKey = categoryKey;
            CategoryTitle = categoryTitle;
            ActionKey = actionKey;
            ActionTitle = actionTitle;
            Description = description;
            _sourceAction = sourceAction;
            _sourceAction.PropertyChanged += SourceAction_PropertyChanged;
        }

        public string CategoryKey { get; }

        public string CategoryTitle { get; }

        public string ActionKey { get; }

        public string ActionTitle { get; }

        public string Description { get; }

        public bool IsSelected
        {
            get
            {
                if (_sourceAction is not null)
                    return _sourceAction.IsSelected;

                return false;
            }
            set
            {
                if (_sourceAction is not null)
                {
                    if (_sourceAction.IsSelected == value)
                        return;

                    _sourceAction.IsSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public void Dispose()
        {
            if (_sourceAction is not null)
                _sourceAction.PropertyChanged -= SourceAction_PropertyChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SourceAction_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptimizationActionViewModel.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SelectedActionsButton_Click(object sender, RoutedEventArgs e)
    {
        // Close the previously opened window if one exists
        _selectedActionsWindow?.Close();

        // Create and display the dialog window
        _selectedActionsWindow = new Windows.Utils.SelectedActionsWindow(VisibleSelectedActions, SelectedActionsEmptyText)
        {
            Owner = Window.GetWindow(this)
        };
        _selectedActionsWindow.Closed += (s, args) => _selectedActionsWindow = null;
        _selectedActionsWindow.Show();
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer && !e.Handled)
        {
            // 确保滚轮事件被ScrollViewer处理，即使鼠标悬停在子控件上
            e.Handled = true;
            var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
        }
    }

    private void StartBeautificationStatusMonitoring()
    {
        _beautificationStatusTimer?.Stop();
        _beautificationStatusTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _beautificationStatusTimer.Tick += async (s, e) => await RefreshBeautificationStatusAsync();
        _beautificationStatusTimer.Start();
    }

    private async Task RefreshBeautificationStatusAsync()
    {
        try
        {
            var isInstalled = await Task.Run(() => NilesoftShellHelper.IsInstalledUsingShellExe());
            await Dispatcher.InvokeAsync(() =>
            {
                TransparencyEnabled = GetTransparencyEnabled();
                UpdateBeautificationUIForRegistrationStatus(isInstalled);
            });
            await RefreshActionStatesAsync(skipUserInteractionCheck: true);
        }
        catch
        {
            // ignore
        }
    }

    private void UpdateBeautificationUIForRegistrationStatus(bool isRegistered)
    {
        var isInstalled = NilesoftShellHelper.IsInstalled();
        
        if (!isInstalled)
        {
            BeautificationStatusText = Resource.SystemBeautification_RightClick_Status_NotInstalled;
        }
        else if (isRegistered)
        {
            BeautificationStatusText = Resource.SystemBeautification_RightClick_Status_Installed;
        }
        else
        {
            BeautificationStatusText = Resource.SystemBeautification_RightClick_Status_InstalledButNotRegistered;
        }
        
        OnPropertyChanged(nameof(BeautificationStatusText));
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(CanUninstall));

        Dispatcher.Invoke(() =>
        {
            var statusTextBlock = FindName("_beautificationStatusText") as System.Windows.Controls.TextBlock;
            if (statusTextBlock != null)
            {
                statusTextBlock.Text = BeautificationStatusText;
            }

            var installButton = FindName("_installShellButton") as Wpf.Ui.Controls.Button;
            if (installButton != null)
            {
                installButton.IsEnabled = CanInstall;
            }

            var uninstallButton = FindName("_uninstallShellButton") as Wpf.Ui.Controls.Button;
            if (uninstallButton != null)
            {
                uninstallButton.IsEnabled = CanUninstall;
            }

            var enableButton = FindName("_enableClassicMenuButton") as Wpf.Ui.Controls.Button;
            if (enableButton != null)
            {
                enableButton.Content = isRegistered
                    ? Resource.SystemBeautification_RightClick_DisableClassic
                    : Resource.SystemBeautification_RightClick_EnableClassic;
                enableButton.Visibility = isRegistered ? Visibility.Collapsed : Visibility.Visible;
            }

            var restoreButton = FindName("_restoreDefaultMenuButton") as Wpf.Ui.Controls.Button;
            if (restoreButton != null)
            {
                restoreButton.Visibility = isRegistered ? Visibility.Visible : Visibility.Collapsed;
            }
        });
    }

    private void LoadBeautificationSettings()
    {
        try
        {
            var configPath = GetShellConfigPath();
            if (string.IsNullOrWhiteSpace(configPath))
            {
                // Use defaults
                SelectedTheme = "auto";
                TransparencyEnabled = GetTransparencyEnabled();
                RoundedCornersEnabled = true;
                ShadowsEnabled = true;
            }
            else
            {
                // Load from config file
                var configContent = File.ReadAllText(configPath);
                // Parse config file to get current settings
                // For now, use defaults
                SelectedTheme = "auto";
                TransparencyEnabled = GetTransparencyEnabled();
                RoundedCornersEnabled = true;
                ShadowsEnabled = true;
            }

            // Update radio buttons
            Dispatcher.Invoke(() =>
            {
                var autoRadio = FindName("_beautificationThemeAutoRadio") as System.Windows.Controls.RadioButton;
                var lightRadio = FindName("_beautificationThemeLightRadio") as System.Windows.Controls.RadioButton;
                var darkRadio = FindName("_beautificationThemeDarkRadio") as System.Windows.Controls.RadioButton;
                var classicRadio = FindName("_beautificationThemeClassicRadio") as System.Windows.Controls.RadioButton;
                var modernRadio = FindName("_beautificationThemeModernRadio") as System.Windows.Controls.RadioButton;

                if (autoRadio != null)
                    autoRadio.IsChecked = SelectedTheme == "auto";
                if (lightRadio != null)
                    lightRadio.IsChecked = SelectedTheme == "light";
                if (darkRadio != null)
                    darkRadio.IsChecked = SelectedTheme == "dark";
                if (classicRadio != null)
                    classicRadio.IsChecked = SelectedTheme == "classic";
                if (modernRadio != null)
                    modernRadio.IsChecked = SelectedTheme == "modern";
            });
        }
        catch
        {
            // Use defaults on error
            SelectedTheme = "auto";
            TransparencyEnabled = GetTransparencyEnabled();
            RoundedCornersEnabled = true;
            ShadowsEnabled = true;
        }
    }


    private static string? GetShellConfigPath()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                var direct = Path.Combine(baseDir, "shell.nss");
                if (File.Exists(direct))
                    return direct;

                var files = Directory.GetFiles(baseDir, "shell.nss", SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var candidate = Path.Combine(programFiles, "Nilesoft Shell", "shell.nss");
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static string? GetShellExePath()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                var direct = Path.Combine(baseDir, "shell.exe");
                if (File.Exists(direct) && File.Exists(Path.Combine(baseDir, "shell.dll")))
                    return direct;

                var files = Directory.GetFiles(baseDir, "shell.exe", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var dir = Path.GetDirectoryName(file);
                    if (!string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir, "shell.dll")))
                        return file;
                }
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var candidate = Path.Combine(programFiles, "Nilesoft Shell", "shell.exe");
                if (File.Exists(candidate) && File.Exists(Path.Combine(programFiles, "Nilesoft Shell", "shell.dll")))
                    return candidate;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private async void InstallShellButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exe = GetShellExePath();
            if (string.IsNullOrWhiteSpace(exe))
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_WindowsOptimization_Title, 
                    GetResource("WindowsOptimizationPage_Beautification_ShellNotFound"), 
                    SnackbarType.Warning);
                return;
            }

            await ExecuteAsync(
                async ct =>
                {
                    await Task.Run(() =>
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c \"\"{exe}\"\" -register -treat -restart",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    });
                },
                Resource.SystemBeautification_RightClick_Install,
                GetResource("WindowsOptimizationPage_Beautification_InstallError"),
                InteractionScope.Beautification);

            await RefreshBeautificationStatusAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to install shell.", ex);
        }
    }

    private async void UninstallShellButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exe = GetShellExePath();
            if (string.IsNullOrWhiteSpace(exe))
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_WindowsOptimization_Title, 
                    GetResource("WindowsOptimizationPage_Beautification_ShellNotFound"), 
                    SnackbarType.Warning);
                return;
            }

            await ExecuteAsync(
                async ct =>
                {
                    await Task.Run(() =>
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c \"\"{exe}\"\" -unregister -restart",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    });
                },
                Resource.SystemBeautification_RightClick_Uninstall,
                GetResource("WindowsOptimizationPage_Beautification_UninstallError"),
                InteractionScope.Beautification);

            await RefreshBeautificationStatusAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to uninstall shell.", ex);
        }
    }

    private void BeautificationThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton radioButton && radioButton.IsChecked == true)
        {
            SelectedTheme = radioButton.Name switch
            {
                "_beautificationThemeLightRadio" => "light",
                "_beautificationThemeDarkRadio" => "dark",
                "_beautificationThemeClassicRadio" => "classic",
                "_beautificationThemeModernRadio" => "modern",
                _ => "auto"
            };
        }
    }

    private async void ApplyBeautificationStyleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var configPath = GetShellConfigPath();
            if (string.IsNullOrWhiteSpace(configPath))
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_WindowsOptimization_Title, 
                    GetResource("WindowsOptimizationPage_Beautification_ConfigNotFound"), 
                    SnackbarType.Warning);
                return;
            }

            await ExecuteAsync(
                async ct =>
                {
                    var config = GenerateShellConfig(SelectedTheme, TransparencyEnabled, RoundedCornersEnabled, ShadowsEnabled);
                    await Task.Run(() =>
                    {
                        File.WriteAllText(configPath, config);
                    });
                },
                Resource.SystemBeautification_MenuStyleSettings_Apply,
                GetResource("WindowsOptimizationPage_Beautification_ApplyError"),
                InteractionScope.Beautification);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to apply beautification style.", ex);
        }
    }

    private static string GenerateShellConfig(string theme, bool transparencyEnabled, bool roundedCornersEnabled, bool shadowsEnabled)
    {
        string themeColors;
        switch (theme)
        {
            case "light":
                themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                break;
            case "dark":
                themeColors = "background-color: #2d2d2d;\ntext-color: #ffffff;";
                break;
            case "classic":
                themeColors = "background-color: #f0f0f0;\ntext-color: #000000;";
                break;
            case "modern":
                themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                break;
            default:
                themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                break;
        }

        return "# Generated by Lenovo Legion Toolkit\n" +
               $"# Theme: {theme}\n" +
               $"# Transparency: {(transparencyEnabled ? "enabled" : "disabled")}\n" +
               $"# Rounded corners: {(roundedCornersEnabled ? "enabled" : "disabled")}\n" +
               $"# Shadows: {(shadowsEnabled ? "enabled" : "disabled")}\n" +
               $"\n" +
               $"# Import base theme configuration\n" +
               $"import 'imports/theme.nss'\n" +
               $"import 'imports/images.nss'\n" +
               $"import 'imports/modify.nss'\n" +
               $"\n" +
               $"# Theme settings based on user selection\n" +
               $"theme\n" +
               $"{{\n" +
               $"    # Appearance settings\n" +
               $"    corner-radius: {(roundedCornersEnabled ? "5" : "0")}px;\n" +
               $"    shadow: {(shadowsEnabled ? "true" : "false")};\n" +
               $"    transparency: {(transparencyEnabled ? "true" : "false")};\n" +
               $"\n" +
               $"    # Color settings based on selected theme\n" +
               $"    {themeColors}\n" +
               $"}}\n" +
               $"\n" +
               $"# Additional configuration for different contexts\n" +
               $".menu\n" +
               $"{{\n" +
               $"    padding: 4px;\n" +
               $"    border-width: 1px;\n" +
               $"    border-style: solid;\n" +
               $"    {(roundedCornersEnabled ? "border-radius: 5px;" : "")}\n" +
               $"}}\n" +
               $"\n" +
               $".separator\n" +
               $"{{\n" +
               $"    height: 1px;\n" +
               $"    margin: 4px 20px;\n" +
               $"}}\n";
    }

    private void TransparencyToggle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool enabled;
            if (sender is ToggleSwitch toggleSwitch)
                enabled = toggleSwitch.IsChecked == true;
            else if (sender is System.Windows.Controls.CheckBox checkBox)
                enabled = checkBox.IsChecked == true;
            else
                enabled = TransparencyEnabled;

            SetTransparencyEnabled(enabled);
            TransparencyEnabled = enabled;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to toggle transparency.", ex);
        }
    }

    private static bool GetTransparencyEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            var value = key?.GetValue("EnableTransparency");
            return Convert.ToInt32(value ?? 0) != 0;
        }
        catch
        {
            return false;
        }
    }

    private static void SetTransparencyEnabled(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            key?.SetValue("EnableTransparency", enabled ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch
        {
            // ignore
        }
    }

    private async Task ExecuteCustomCleanupWithProgressAsync(CancellationToken cancellationToken)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var rules = settings.Store.CustomCleanupRules ?? new List<CustomCleanupRule>();

        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(rule.DirectoryPath))
                continue;

            var directoryPath = Environment.ExpandEnvironmentVariables(rule.DirectoryPath.Trim());

            if (!Directory.Exists(directoryPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Custom cleanup skipped. Directory not found. [path={directoryPath}]");
                continue;
            }

            var normalizedExtensions = (rule.Extensions ?? [])
                .Select(ext => ext?.TrimStart('.').ToLowerInvariant())
                .Where(extension => !string.IsNullOrEmpty(extension))
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedExtensions.Length == 0)
                continue;

            var extensionsSet = new HashSet<string>(normalizedExtensions, StringComparer.OrdinalIgnoreCase);
            var searchOption = rule.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                foreach (var file in Directory.EnumerateFiles(directoryPath, "*", searchOption))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string extension;
                    try
                    {
                        extension = Path.GetExtension(file);
                        if (!string.IsNullOrEmpty(extension))
                            extension = extension.TrimStart('.').ToLowerInvariant();
                    }
                    catch
                    {
                        continue;
                    }

                    if (!extensionsSet.Contains(extension))
                        continue;

                    try
                    {
                        // Update UI with current file being deleted
                        var fileName = Path.GetFileName(file);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            var format = GetResource("WindowsOptimizationPage_CurrentDeletingFile");
                            if (string.IsNullOrWhiteSpace(format))
                                format = "正在删除: {0}";
                            CurrentDeletingFile = string.Format(format, fileName);
                        });

                        File.Delete(file);

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Custom cleanup deleted file. [path={file}]");
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Custom cleanup failed to delete file. [path={file}]", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Custom cleanup failed to enumerate directory. [path={directoryPath}]", ex);
            }
        }
    }
}
