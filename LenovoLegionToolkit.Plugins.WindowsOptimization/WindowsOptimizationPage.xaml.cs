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
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Windows.Utils;
using ISelectedActionViewModel = LenovoLegionToolkit.WPF.Windows.Utils.ISelectedActionViewModel;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.WPF;
using LenovoLegionToolkit.WPF.Controls;
using LenovoLegionToolkit.WPF.Controls.Packages;
using LenovoLegionToolkit.WPF.Extensions;
using Controls = LenovoLegionToolkit.WPF.Controls;
using System.Net.Http;
using System.Windows.Forms;
using System.Windows.Media;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace LenovoLegionToolkit.Plugins.WindowsOptimization;

public partial class WindowsOptimizationPage : INotifyPropertyChanged
{
    private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private SelectedActionsWindow? _selectedActionsWindow;
    private readonly PackageDownloaderSettings _packageDownloaderSettings = IoCContainer.Resolve<PackageDownloaderSettings>();
    private readonly PackageDownloaderFactory _packageDownloaderFactory = IoCContainer.Resolve<PackageDownloaderFactory>();

    private IPackageDownloader? _driverPackageDownloader;
    private CancellationTokenSource? _driverGetPackagesTokenSource;
    private CancellationTokenSource? _driverFilterDebounceCancellationTokenSource;
    private List<Package>? _driverPackages;

    private enum PageMode
    {
        Optimization,  // 系统优美化（包含优化和美化）
        Cleanup,
        DriverDownload
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
    private bool _isInitializingDriverDownload = false;
    private bool _transparencyEnabled;
    private bool _roundedCornersEnabled = true;
    private bool _shadowsEnabled = true;
    private string _selectedTheme = "auto";
    private System.Windows.Threading.DispatcherTimer? _beautificationStatusTimer;
    private MenuStyleSettingsWindow? _styleSettingsWindow;
    private ActionDetailsWindow? _actionDetailsWindow;
    private long _estimatedCleanupSize;
    private bool _isCalculatingSize;
    private CancellationTokenSource? _sizeCalculationCts;
    private bool _hasInitializedCleanupMode = false;
    private string _currentOperationText = string.Empty;
    private string _currentDeletingFile = string.Empty;
    private string _runCleanupButtonText = string.Empty;
    private bool _isCompactView;
    private System.Windows.Threading.DispatcherTimer? _actionStateRefreshTimer;
    private bool _isUserInteracting = false;
    private DateTime _lastUserInteraction = DateTime.MinValue;
    private readonly HashSet<string> _userUncheckedActions = new(StringComparer.OrdinalIgnoreCase);
    private bool _isRefreshingStates = false; // 标记是否正在刷新状态（程序内部操作，不应触发命令执行）
    private DateTime _lastActionItemClickTime = DateTime.MinValue;
    private string? _lastActionItemKey = null;

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

            // 当IsBusy状态改变时，更新按钮状态（特别是驱动下载模式）
            ApplyInteractionState();
        }
    }

    public ObservableCollection<OptimizationCategoryViewModel> Categories { get; } = [];

    public ObservableCollection<OptimizationCategoryViewModel> OptimizationCategories { get; } = [];

    public ObservableCollection<OptimizationCategoryViewModel> CleanupCategories { get; } = [];

    public ObservableCollection<OptimizationCategoryViewModel> BeautificationCategories { get; } = [];

    public ObservableCollection<SelectedActionViewModel> SelectedOptimizationActions { get; } = [];

    public ObservableCollection<SelectedActionViewModel> SelectedCleanupActions { get; } = [];

    public ObservableCollection<SelectedActionViewModel> SelectedBeautificationActions { get; } = [];

    public ObservableCollection<SelectedDriverPackageViewModel> SelectedDriverPackages { get; } = [];

    public ObservableCollection<SelectedActionViewModel> VisibleSelectedActions => _currentMode switch
    {
        PageMode.Cleanup => SelectedCleanupActions,
        // Optimization模式（系统优美化）同时显示优化和美化操作
        PageMode.Optimization => new ObservableCollection<SelectedActionViewModel>(
            SelectedOptimizationActions.Concat(SelectedBeautificationActions)),
        _ => SelectedOptimizationActions
    };

    public ObservableCollection<CustomCleanupRuleViewModel> CustomCleanupRules { get; } = [];

    public bool HasSelectedActions => _currentMode switch
    {
        PageMode.DriverDownload => SelectedDriverPackages.Count > 0,
        PageMode.Cleanup => CleanupCategories
            .SelectMany(c => c.Actions)
            .Any(a => a.IsEnabled && a.IsSelected),
        _ => VisibleSelectedActions.Count > 0
    };

    public string SelectedActionsSummary
    {
        get
        {
            int count;
            if (_currentMode == PageMode.DriverDownload)
            {
                count = SelectedDriverPackages.Count;
            }
            else if (_currentMode == PageMode.Cleanup)
            {
                // 在清理模式下，直接从 CleanupCategories 计算实际选中的操作数量
                // 注意：只检查 IsSelected，不检查 IsEnabled，因为操作可能在执行清理时被临时禁用
                // 但用户仍然应该看到他们选中的项目数量
                count = CleanupCategories
                    .SelectMany(c => c.Actions)
                    .Count(a => a.IsSelected);
            }
            else
            {
                count = VisibleSelectedActions.Count;
            }
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

    public string RunCleanupButtonText
    {
        get => string.IsNullOrWhiteSpace(_runCleanupButtonText)
            ? GetResource("WindowsOptimizationPage_RunCleanup_Button")
            : _runCleanupButtonText;
        private set
        {
            if (_runCleanupButtonText == value)
                return;

            _runCleanupButtonText = value;
            OnPropertyChanged(nameof(RunCleanupButtonText));
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
    private string _driverExpandCollapseText = string.Empty;
    private bool _driverPackagesExpanded = false;

    public string DriverExpandCollapseText
    {
        get => _driverExpandCollapseText;
        private set
        {
            if (_driverExpandCollapseText == value)
                return;
            _driverExpandCollapseText = value;
            OnPropertyChanged(nameof(DriverExpandCollapseText));
        }
    }

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
        PageMode.DriverDownload => [], // 驱动下载模式下返回空集合
        _ => OptimizationCategories.Concat(BeautificationCategories) // 系统优美化模式下同时显示优化和美化分类
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
                default:
                    // Optimization模式（系统优美化）同时保存优化和美化分类的选中状态
                    if (value != null && value.Key.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase))
                        _lastBeautificationCategory = value;
                    else
                        _lastOptimizationCategory = value;
                    break;
            }
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    private void WindowsOptimizationPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            UpdatePluginTabsVisibility();
        }
    }

    private void UpdatePluginTabsVisibility()
    {
        // 更新清理标签页可见性
        var hasCleanupPlugin = _pluginManager.IsInstalled(PluginConstants.Cleanup);
        if (_cleanupNavButton != null)
        {
            _cleanupNavButton.Visibility = hasCleanupPlugin ? Visibility.Visible : Visibility.Collapsed;
        }

        // 更新驱动下载标签页可见性
        var hasDriverDownloadPlugin = _pluginManager.IsInstalled(PluginConstants.DriverDownload);
        if (_driverDownloadNavButton != null)
        {
            _driverDownloadNavButton.Visibility = hasDriverDownloadPlugin ? Visibility.Visible : Visibility.Collapsed;
        }

        // 如果当前模式是已卸载的插件模式，切换到优化模式
        if (!hasCleanupPlugin && _currentMode == PageMode.Cleanup)
        {
            SetMode(PageMode.Optimization);
        }

        if (!hasDriverDownloadPlugin && _currentMode == PageMode.DriverDownload)
        {
            SetMode(PageMode.Optimization);
        }
    }
    public bool IsCleanupMode => _currentMode == PageMode.Cleanup;
    public bool IsBeautificationMode => _currentMode == PageMode.Optimization && BeautificationCategories.Count > 0; // 已合并到Optimization模式，当存在美化分类时显示美化UI
    public bool IsDriverDownloadMode => _currentMode == PageMode.DriverDownload;

    public event PropertyChangedEventHandler? PropertyChanged;

    public WindowsOptimizationPage()
    {
        IsVisibleChanged += WindowsOptimizationPage_IsVisibleChanged;
        InitializeComponent();
        DataContext = this;

        CustomCleanupRules.CollectionChanged += CustomCleanupRules_CollectionChanged;
        LoadCustomCleanupRules();
        UpdateCleanupControlsState();

        // 初始化驱动下载模式的展开/折叠文本
        RefreshDriverExpandCollapseText();

        // 订阅插件状态变化事件
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;
    }

    private void PluginManager_PluginStateChanged(object? sender, PluginEventArgs e)
    {
        // 如果清理或驱动下载插件状态发生变化，更新标签页可见性
        if (e.PluginId == PluginConstants.Cleanup || e.PluginId == PluginConstants.DriverDownload)
        {
            Dispatcher.Invoke(() =>
            {
                UpdatePluginTabsVisibility();
            });
        }
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // 设置页面文本
        if (FindName("_titleTextBlock") is System.Windows.Controls.TextBlock titleTextBlock)
            titleTextBlock.Text = GetResource("SettingsPage_WindowsOptimization_Title");
        if (FindName("_infoTextBlock") is System.Windows.Controls.TextBlock infoTextBlock)
            infoTextBlock.Text = GetResource("WindowsOptimizationPage_Info");
        if (FindName("_selectedActionsHeaderTextBlock") is System.Windows.Controls.TextBlock headerTextBlock)
            headerTextBlock.Text = GetResource("WindowsOptimizationPage_SelectedActions_Header");
        if (FindName("_optimizationNavButton") is System.Windows.Controls.RadioButton optimizationButton)
            optimizationButton.Content = GetResource("WindowsOptimizationPage_Tab_Optimization");
        if (FindName("_cleanupNavButton") is System.Windows.Controls.RadioButton cleanupButton)
            cleanupButton.Content = GetResource("WindowsOptimizationPage_Tab_Cleanup");
        if (FindName("_driverDownloadNavButton") is System.Windows.Controls.RadioButton driverButton)
            driverButton.Content = GetResource("WindowsOptimizationPage_Tab_DriverDownload");
        if (FindName("_selectRecommendedButton") is System.Windows.Controls.Button selectButton)
        {
            selectButton.Content = GetResource("WindowsOptimizationPage_SelectRecommended_Button");
            selectButton.ToolTip = GetResource("WindowsOptimizationPage_SelectRecommended_Button");
        }
        if (FindName("_clearButton") is System.Windows.Controls.Button clearButton)
        {
            clearButton.Content = GetResource("WindowsOptimizationPage_ClearSelection_Button");
            clearButton.ToolTip = GetResource("WindowsOptimizationPage_ClearSelection_Button");
        }

        if (Categories.Count > 0)
            return;

        InitializeCategories();
        
        // 更新插件标签页可见性
        UpdatePluginTabsVisibility();
        
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
        _actionDetailsWindow?.Close();
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

    private void OpenActionDetailsWindow(string actionKey)
    {
        try
        {
            // Close existing window if open
            _actionDetailsWindow?.Close();

            // Get action definition
            WindowsOptimizationActionDefinition? actionDefinition = null;
            var categories = WindowsOptimizationService.GetCategories();
            foreach (var category in categories)
            {
                var action = category.Actions.FirstOrDefault(a =>
                    string.Equals(a.Key, actionKey, StringComparison.OrdinalIgnoreCase));
                if (action != null)
                {
                    actionDefinition = action;
                    break;
                }
            }

            // Create and show action details window
            _actionDetailsWindow = new ActionDetailsWindow(actionKey, actionDefinition)
            {
                Owner = Window.GetWindow(this)
            };
            _actionDetailsWindow.Closed += (s, args) => _actionDetailsWindow = null;
            _actionDetailsWindow.Show();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to open action details window.", ex);
        }
    }

    private void ActionItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            // 获取点击的操作项
            if (sender is not System.Windows.FrameworkElement element)
                return;

            if (element.DataContext is not OptimizationActionViewModel actionViewModel)
                return;

            // 检测双击：检查是否是同一个操作项，且两次点击间隔小于 500ms
            var now = DateTime.Now;
            var isDoubleClick = _lastActionItemKey == actionViewModel.Key &&
                               (now - _lastActionItemClickTime).TotalMilliseconds < 500;

            // 更新上次点击信息
            _lastActionItemClickTime = now;
            _lastActionItemKey = actionViewModel.Key;

            // 如果是双击
            if (isDoubleClick)
            {
                // 美化相关的操作：打开样式设置窗口
                if (actionViewModel.Key.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
                {
                    OpenStyleSettingsButton_Click(sender, e);
                    e.Handled = true;
                }
                // 优化和清理操作：打开操作详情窗口
                else if (actionViewModel.Key.StartsWith("explorer.", StringComparison.OrdinalIgnoreCase) ||
                         actionViewModel.Key.StartsWith("performance.", StringComparison.OrdinalIgnoreCase) ||
                         actionViewModel.Key.StartsWith("services.", StringComparison.OrdinalIgnoreCase) ||
                         actionViewModel.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
                {
                    OpenActionDetailsWindow(actionViewModel.Key);
                    e.Handled = true;
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to handle action item click.", ex);
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
        // 如果资源不存在，使用默认值
        if (string.IsNullOrWhiteSpace(recommendedTagText))
            recommendedTagText = Resource.WindowsOptimization_Action_Recommended_Tag;

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
        // 直接从 Categories 中获取当前选中的清理操作，确保获取的是最新状态
        // 而不是依赖 SelectedCleanupActions 集合（可能包含过时的实例）
        var selectedCleanupActions = new List<(string CategoryKey, string CategoryTitle, string ActionKey, string ActionTitle, string Description)>();

        foreach (var category in CleanupCategories)
        {
            foreach (var action in category.Actions.Where(a => a.IsEnabled && a.IsSelected))
            {
                selectedCleanupActions.Add((category.Key, category.Title, action.Key, action.Title, action.Description));
            }
        }

        var selectedKeys = selectedCleanupActions
            .Select(a => a.ActionKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 如果没有选择任何操作，显示警告提示并返回
        if (selectedKeys.Count == 0)
        {
            // 记录详细信息以便调试
            if (Log.Instance.IsTraceEnabled)
            {
                var totalCleanupActions = CleanupCategories.SelectMany(c => c.Actions).Count();
                var selectedCleanupActionsCount = CleanupCategories.SelectMany(c => c.Actions).Count(a => a.IsEnabled && a.IsSelected);
                Log.Instance.Trace($"RunCleanup: No selected actions. Total cleanup actions: {totalCleanupActions}, Selected: {selectedCleanupActionsCount}");
            }

            // 显示警告提示（从主窗口底部弹出的橙色警告窗口）
            await SnackbarHelper.ShowAsync(
                Resource.SettingsPage_WindowsOptimization_Title,
                GetResource("WindowsOptimizationPage_Cleanup_NoSelection_Warning") ?? "请至少选择一个清理选项后再执行清理操作。",
                SnackbarType.Warning);

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
                    // 使用从 Categories 中获取的当前选中操作列表，确保使用最新状态
                    var actionsInOrder = selectedCleanupActions
                        .Where(a => selectedKeys.Contains(a.ActionKey, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (actionsInOrder.Count == 0)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"No actions found. selectedKeys count: {selectedKeys.Count}, Keys: {string.Join(", ", selectedKeys)}");
                        throw new InvalidOperationException("没有找到要执行的操作。请确保已选择有效的清理操作。");
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Starting cleanup execution. Selected actions: {actionsInOrder.Count}, Keys: {string.Join(", ", selectedKeys)}");

                    long totalFreedBytes = 0;
                    var swOverall = System.Diagnostics.Stopwatch.StartNew();
                    var totalActions = actionsInOrder.Count;
                    var currentActionIndex = 0;
                    foreach (var action in actionsInOrder)
                    {
                        currentActionIndex++;
                        var progressPercentage = (int)Math.Round((double)currentActionIndex / totalActions * 100);

                        await Dispatcher.InvokeAsync(() =>
                        {
                            CurrentOperationText = string.Format(GetResource("WindowsOptimizationPage_RunningStep"), action.ActionTitle);
                            CurrentDeletingFile = string.Empty;
                            RunCleanupButtonText = string.Format(Resource.WindowsOptimizationPage_RunCleanupButtonText_Format, progressPercentage);
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
                        // 清理完成，重置按钮文本
                        RunCleanupButtonText = string.Empty;
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
        if (_currentMode == PageMode.DriverDownload)
        {
            DriverSelectRecommendedButton_Click(sender, e);
        }
        else
        {
            SelectRecommended(ActiveCategories);
        }
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == PageMode.DriverDownload)
        {
            DriverPauseAllButton_Click(sender, e);
        }
        else
        {
            foreach (var category in ActiveCategories)
                category.ClearSelection();

            UpdateSelectedActions();
        }
    }

    private void SelectRecommended(IEnumerable<OptimizationCategoryViewModel> categories)
    {
        // 标记正在刷新状态，防止触发命令执行和状态刷新覆盖
        _isRefreshingStates = true;

        try
        {
            // 先清除所有推荐操作在取消列表中的记录，因为用户现在明确选择了推荐项目
            var allActions = categories.SelectMany(c => c.Actions);
            foreach (var action in allActions.Where(a => a.Recommended))
            {
                _userUncheckedActions.Remove(action.Key);
            }

            // 然后调用每个分类的SelectRecommended方法
            foreach (var category in categories)
                category.SelectRecommended();

            UpdateSelectedActions();
        }
        finally
        {
            // 重置标志，允许后续的用户操作触发命令
            _isRefreshingStates = false;
        }
    }

    // 驱动下载模式的事件处理函数
    private void DriverScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled)
            return;

        // 优先使用字段引用，如果不可用则使用 sender
        var scrollViewer = _driverScrollViewer ?? sender as System.Windows.Controls.ScrollViewer;

        if (scrollViewer != null)
        {
            // 确保滚轮事件被ScrollViewer处理，即使鼠标悬停在子控件上
            e.Handled = true;
            var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
        }
    }

    private void DriverToggleExpandCollapseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverPackagesStackPanel == null)
            return;

        _driverPackagesExpanded = !_driverPackagesExpanded;
        RefreshDriverExpandCollapseText();

        // 驱动包没有展开/折叠功能，暂时只更新文本
        // 未来可以添加展开/折叠详细信息的逻辑
    }

    private void RefreshDriverExpandCollapseText()
    {
        DriverExpandCollapseText = _driverPackagesExpanded ? CollapseAllText : ExpandAllText;
    }

    private void DriverSelectRecommendedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverPackagesStackPanel == null)
            return;

        // 选择所有推荐的驱动包（可更新项目）
        foreach (var child in _driverPackagesStackPanel.Children)
        {
            if (child is Controls.Packages.PackageControl packageControl && packageControl.IsRecommended)
            {
                packageControl.IsSelected = true;
            }
        }

        UpdateSelectedDriverPackages();
    }

    private void DriverClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverPackagesStackPanel == null)
            return;

        // 清除所有驱动包的选择
        foreach (var child in _driverPackagesStackPanel.Children)
        {
            if (child is Controls.Packages.PackageControl packageControl)
            {
                packageControl.IsSelected = false;
            }
        }

        UpdateSelectedDriverPackages();
    }

    private void DriverPauseAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverPackagesStackPanel == null)
            return;

        // 暂停所有正在下载或安装的驱动包
        foreach (var child in _driverPackagesStackPanel.Children)
        {
            if (child is Controls.Packages.PackageControl packageControl)
            {
                // 如果正在下载或安装，取消选择以停止操作
                if (packageControl.Status == Controls.Packages.PackageControl.PackageStatus.Downloading ||
                    packageControl.Status == Controls.Packages.PackageControl.PackageStatus.Installing)
                {
                    packageControl.IsSelected = false;
                }
            }
        }

        UpdateSelectedDriverPackages();
    }

    private void UpdateSelectedDriverPackages()
    {
        if (_driverPackagesStackPanel == null)
            return;

        var newSelectedPackages = new List<SelectedDriverPackageViewModel>();

        foreach (var child in _driverPackagesStackPanel.Children)
        {
            if (child is Controls.Packages.PackageControl packageControl && packageControl.IsSelected)
            {
                var existing = SelectedDriverPackages.FirstOrDefault(p => p.PackageId == packageControl.Package.Id);
                if (existing != null)
                {
                    newSelectedPackages.Add(existing);
                }
                else
                {
                    newSelectedPackages.Add(new SelectedDriverPackageViewModel(
                        packageControl.Package.Id,
                        packageControl.Package.Title,
                        packageControl.Package.Description ?? string.Empty,
                        packageControl.Package.Category,
                        packageControl));
                }
            }
        }

        // 清理不再选择的包
        foreach (var existing in SelectedDriverPackages.ToList())
        {
            if (!newSelectedPackages.Any(p => p.PackageId == existing.PackageId))
            {
                existing.Dispose();
                SelectedDriverPackages.Remove(existing);
            }
        }

        // 添加新选择的包
        foreach (var newPackage in newSelectedPackages)
        {
            if (!SelectedDriverPackages.Any(p => p.PackageId == newPackage.PackageId))
            {
                SelectedDriverPackages.Add(newPackage);
            }
        }

        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
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
            // 如果是清理操作，初始化按钮文本
            if (scope.HasFlag(InteractionScope.Cleanup))
            {
                RunCleanupButtonText = string.Format(Resource.WindowsOptimizationPage_RunCleanupButtonText_Format, 0);
            }

            ShowOperationIndicator(true);
            await operation(CancellationToken.None).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, successMessage, SnackbarType.Success));
        }
        catch (OperationCanceledException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Windows optimization action was cancelled.");
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, Resource.WindowsOptimizationPage_OperationCancelled, SnackbarType.Warning));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Windows optimization action failed. Exception: {ex.Message}", ex);

            var detailedError = $"{errorMessage}\n{string.Format(Resource.WindowsOptimizationPage_ErrorDetails, ex.Message)}";
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, detailedError, SnackbarType.Error));
        }
        finally
        {
            IsBusy = false;
            Dispatcher.Invoke(() =>
            {
                ToggleInteraction(true, scope);
                // 如果是在清理模式下，重置按钮文本
                if (scope.HasFlag(InteractionScope.Cleanup))
                {
                    RunCleanupButtonText = string.Empty;
                }
            });
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
            // Optimization模式（系统优美化）同时启用优化和美化交互
            PageMode.Optimization => optimizationEnabled || beautificationEnabled,
            // 驱动下载模式：按钮应该始终启用（除非正在执行驱动下载相关的操作）
            // 清理操作的 IsBusy 状态不应该影响驱动下载模式的按钮
            PageMode.DriverDownload => !IsDriverDownloadBusy(),
            _ => optimizationEnabled
        };

        if (_selectRecommendedButton != null)
            _selectRecommendedButton.IsEnabled = primaryButtonsEnabled;

        if (_clearButton != null)
            _clearButton.IsEnabled = primaryButtonsEnabled;

        if (_runCleanupButton != null)
            _runCleanupButton.IsEnabled = cleanupEnabled;

        var categoriesList = FindName("_categoriesList") as System.Windows.Controls.ItemsControl;
        if (categoriesList != null)
        {
            var listEnabled = _currentMode switch
            {
                PageMode.Cleanup => cleanupEnabled,
                // Optimization模式（系统优美化）同时启用优化和美化交互
                PageMode.Optimization => optimizationEnabled || beautificationEnabled,
                _ => optimizationEnabled
            };
            categoriesList.IsEnabled = listEnabled;
        }
    }

    private void ShowOperationIndicator(bool isVisible)
    {
        // 进度条已被移除，此方法保留为空实现以保持接口兼容性
    }

    private static string GetResource(string resourceKey) =>
        string.IsNullOrWhiteSpace(resourceKey)
            ? string.Empty
            : Resource.ResourceManager.GetString(resourceKey, Resource.Culture) ?? resourceKey;

    private void Category_SelectionChanged(object? sender, EventArgs e)
    {
        _lastUserInteraction = DateTime.Now;
        _isUserInteracting = true;
        UpdateSelectedActions();
        // 确保 SelectedActionsSummary 也被更新
        OnPropertyChanged(nameof(SelectedActionsSummary));
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
            // 只有当有选中的操作时才估算，如果没有选择任何操作，直接设置为0
            if (HasSelectedActions)
            {
                _ = UpdateEstimatedCleanupSizeAsync();
            }
            else
            {
                EstimatedCleanupSize = 0;
                IsCalculatingSize = false;
            }
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
            // 只有当有选中的操作时才估算，如果没有选择任何操作，直接设置为0
            if (HasSelectedActions)
            {
                _ = UpdateEstimatedCleanupSizeAsync();
            }
            else
            {
                EstimatedCleanupSize = 0;
                IsCalculatingSize = false;
            }
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
        // 如果没有选中的操作，直接设置为0并返回
        if (!HasSelectedActions)
        {
            EstimatedCleanupSize = 0;
            IsCalculatingSize = false;
            return;
        }

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

            // 再次检查（延迟后可能选择已改变）
            if (actionKeys.Count == 0)
            {
                EstimatedCleanupSize = 0;
                IsCalculatingSize = false;
                return;
            }

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
        // 检查是否可以切换到指定模式（需要插件支持）
        if (mode == PageMode.Cleanup && !_pluginManager.IsInstalled(PluginConstants.Cleanup))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Cannot switch to Cleanup mode: plugin not installed");
            return;
        }

        if (mode == PageMode.DriverDownload && !_pluginManager.IsInstalled(PluginConstants.DriverDownload))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Cannot switch to DriverDownload mode: plugin not installed");
            return;
        }

        var modeChanged = _currentMode != mode;
        _currentMode = mode;

        if (modeChanged)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"SetMode called: {mode}, IsDriverDownloadMode will be: {mode == PageMode.DriverDownload}");

            OnPropertyChanged(nameof(IsCleanupMode));
            OnPropertyChanged(nameof(IsBeautificationMode));
            OnPropertyChanged(nameof(IsDriverDownloadMode));
            OnPropertyChanged(nameof(ActiveCategories));
            OnPropertyChanged(nameof(VisibleSelectedActions));
            OnPropertyChanged(nameof(HasSelectedActions));
            OnPropertyChanged(nameof(SelectedActionsSummary));

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"After property change: IsDriverDownloadMode = {IsDriverDownloadMode}");

            // 可见性切换已通过 XAML 中的 DataTrigger 自动处理
            // 这里保留代码以确保兼容性，但主要依赖 XAML 中的 DataTrigger
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var categoriesList = FindName("_categoriesList") as System.Windows.Controls.ScrollViewer;

                // 由于 XAML 中的 DataTrigger 已经处理了可见性，这里主要是为了确保兼容性
                // 如果 DataTrigger 没有正确工作，这里的代码可以作为后备
                if (categoriesList != null)
                {
                    if (mode == PageMode.DriverDownload)
                    {
                        categoriesList.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        categoriesList.Visibility = Visibility.Visible;
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);

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
            else if (mode == PageMode.DriverDownload)
            {
                // 初始化驱动下载模式，立即开始刷新
                RefreshDriverExpandCollapseText();
                // 确保切换到驱动下载模式时按钮状态正确（不依赖清理或优化模式的交互状态）
                // ApplyInteractionState() 会在 SetMode 末尾被调用，这里提前调用以确保按钮立即启用
                ApplyInteractionState();
                // 确保驱动下载内容区域可见
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var driverLoader = FindName("_driverLoader") as System.Windows.Controls.Grid;
                    if (driverLoader != null)
                        driverLoader.Visibility = Visibility.Visible;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                _ = InitializeDriverDownloadModeAsync();
            }
            else if (mode == PageMode.Optimization)
            {
                // 系统优美化模式：同时处理优化和美化
                // 确保切换到优化模式时交互是启用的
                _beautificationInteractionEnabled = true;
                foreach (var category in BeautificationCategories)
                    category.SetEnabled(true);

                _ = RefreshBeautificationStatusAsync();
                StartBeautificationStatusMonitoring();
                TransparencyEnabled = GetTransparencyEnabled();
                EstimatedCleanupSize = 0;
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

        // 驱动下载模式下不需要设置 SelectedCategory 和相关的优化界面属性
        if (mode != PageMode.DriverDownload)
        {
            var activeCategoriesList = ActiveCategories.ToList();
            var preferredCategory = mode switch
            {
                PageMode.Cleanup => _lastCleanupCategory,
                // Optimization模式（系统优美化）优先选择优化分类，如果没有则选择美化分类
                PageMode.Optimization => _lastOptimizationCategory ?? _lastBeautificationCategory,
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
                // 对于右键美化操作，先检查是否已安装，如果没有安装则先安装，然后应用
                if (actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"User checked beautify action {actionKey}, checking installation status first");

                    // 先检查是否已安装
                    var isInstalled = await Task.Run(() => NilesoftShellHelper.IsInstalledUsingShellExe()).ConfigureAwait(false);
                    
                    var shellExe = NilesoftShellHelper.GetNilesoftShellExePath();
                    if (string.IsNullOrWhiteSpace(shellExe))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Shell.exe not found, cannot execute install command");
                        
                        await Dispatcher.InvokeAsync(() =>
                        {
                            SnackbarHelper.Show(
                                GetResource("WindowsOptimizationPage_Beautification_ShellNotFound_Title") ?? Resource.SettingsPage_WindowsOptimization_Title,
                                GetResource("WindowsOptimizationPage_Beautification_ShellNotFound_Message") ?? "无法找到 shell.exe，请确保 Nilesoft Shell 已正确安装。",
                                SnackbarType.Warning);
                        });
                        return;
                    }

                    try
                    {
                        // 如果没有安装，先安装；如果已安装，直接应用设置
                        if (!isInstalled)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Nilesoft Shell not installed, installing first");

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
                                    Log.Instance.Trace($"Starting install process: cmd.exe {process.StartInfo.Arguments}");
                                process.Start();
                                process.WaitForExit();
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Install process exited with code: {process.ExitCode}");
                            });

                            // 安装后清除缓存并等待一下，让系统有时间完成安装
                            NilesoftShellHelper.ClearInstallationStatusCache();
                            await Task.Delay(2000);
                        }
                        else
                        {
                            // 如果已安装，直接应用设置（注册并重启资源管理器）
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Nilesoft Shell already installed, applying settings: {shellExe} -register -treat -restart");

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
                                    Log.Instance.Trace($"Starting apply process: cmd.exe {process.StartInfo.Arguments}");
                                process.Start();
                                process.WaitForExit();
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Apply process exited with code: {process.ExitCode}");
                            });
                        }

                        // 应用操作完成后，清除缓存以强制下次重新检查状态
                        NilesoftShellHelper.ClearInstallationStatusCache();

                        // 使用 Snackbar 从底部弹出显示成功消息
                        await Dispatcher.InvokeAsync(() =>
                        {
                            SnackbarHelper.Show(
                                Resource.SettingsPage_WindowsOptimization_Title,
                                GetResource("WindowsOptimizationPage_ApplySelected_Success") ?? "设置已成功应用。",
                                SnackbarType.Success);
                        });
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to apply beautification action: {actionKey}", ex);

                        await Dispatcher.InvokeAsync(() =>
                        {
                            var errorMessage = GetResource("WindowsOptimizationPage_ApplySelected_Error") ?? "应用设置时发生错误。";
                            var detailedError = $"{errorMessage}\n{string.Format(Resource.WindowsOptimizationPage_ErrorDetails ?? "错误详情: {0}", ex.Message)}";
                            SnackbarHelper.Show(
                                Resource.SettingsPage_WindowsOptimization_Title,
                                detailedError,
                                SnackbarType.Error);
                        });
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

                // 在刷新状态前清除缓存，确保使用最新的实际状态
                if (actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
                {
                    NilesoftShellHelper.ClearInstallationStatusCache();
                }

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
                                Log.Instance.Trace($"Executing shell.exe unregister command: {shellExe} -unregister -treat -restart");

                            await Task.Run(() =>
                            {
                                var process = new System.Diagnostics.Process
                                {
                                    StartInfo = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "cmd.exe",
                                        Arguments = $"/c \"\"{shellExe}\"\" -unregister -treat -restart",
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

                    // 卸载操作完成后，清除缓存和注册表中的安装状态值
                    // 这确保下次检查时不会从注册表读取到旧的已安装状态
                    NilesoftShellHelper.ClearInstallationStatusCache();
                    NilesoftShellHelper.ClearRegistryInstallationStatus();

                    // 将操作添加到用户取消列表，防止在状态刷新时自动勾选
                    if (!_userUncheckedActions.Contains(actionKey))
                    {
                        _userUncheckedActions.Add(actionKey);
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Added {actionKey} to unchecked list to prevent auto-checking");
                    }
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Shell.exe not found, cannot execute uninstall command");
                }

                // 延迟刷新状态，给 shell 卸载操作足够的时间完成
                // await Task.Delay(3000); // 增加到3秒，给卸载操作更多时间

                // 再次清除注册表值和缓存，确保卸载后的状态被正确反映
                // 必须在检查状态之前清除缓存，否则会使用旧的缓存值
                // NilesoftShellHelper.ClearRegistryInstallationStatus();
                NilesoftShellHelper.ClearInstallationStatusCache();

                await RefreshActionStatesAsync(skipUserInteractionCheck: true);

                // 检查卸载是否成功，如果成功则从取消列表中移除
                // 在检查前再次清除缓存，确保不使用旧的缓存值
                NilesoftShellHelper.ClearInstallationStatusCache();
                var isStillInstalled = await Task.Run(() => NilesoftShellHelper.IsInstalledUsingShellExe()).ConfigureAwait(false);
                if (!isStillInstalled)
                {
                    _userUncheckedActions.Remove(actionKey);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Uninstall successful, removed {actionKey} from unchecked list");
                }
                else
                {
                    // 如果卸载后仍然显示已安装，可能是注册表值未更新，强制清除并再次检查
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Uninstall may not have completed, clearing registry and rechecking...");
                    NilesoftShellHelper.ClearRegistryInstallationStatus();
                    NilesoftShellHelper.ClearInstallationStatusCache();
                    await Task.Delay(1000); // 再等待1秒
                    // 在重新检查前再次清除缓存，确保不使用旧的缓存值
                    NilesoftShellHelper.ClearInstallationStatusCache();
                    var recheckInstalled = await Task.Run(() => NilesoftShellHelper.IsInstalledUsingShellExe()).ConfigureAwait(false);
                    if (!recheckInstalled)
                    {
                        _userUncheckedActions.Remove(actionKey);
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Recheck confirmed uninstall successful, removed {actionKey} from unchecked list");
                    }
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

    private void DriverDownloadNavButton_Checked(object sender, RoutedEventArgs e)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace((FormattableString)$"Driver download tab clicked, switching to DriverDownload mode");
        SetMode(PageMode.DriverDownload);
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace((FormattableString)$"IsDriverDownloadMode = {IsDriverDownloadMode}");
    }

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

        public void RaiseSelectionChanged()
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

    public class SelectedActionViewModel : ISelectedActionViewModel, IDisposable
    {
        private readonly OptimizationActionViewModel? _sourceAction;
        private bool _isSelected; // 用于驱动下载模式，当 sourceAction 为 null 时存储选中状态

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
            if (_sourceAction is not null)
                _sourceAction.PropertyChanged += SourceAction_PropertyChanged;
        }

        public string CategoryKey { get; }

        public string CategoryTitle { get; }

        public string ActionKey { get; }

        public string ActionTitle { get; }

        public string Description { get; }

        // 用于存储驱动下载模式下的额外信息（如 SelectedDriverPackageViewModel）
        public object? Tag { get; set; }

        public bool IsEnabled
        {
            get
            {
                // 如果是驱动下载模式且已完成，则禁用复选框
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

                return _isSelected; // 驱动下载模式：返回存储的选中状态
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
                else
                {
                    // 驱动下载模式：更新存储的选中状态
                    if (_isSelected == value)
                        return;

                    // 如果取消选中，检查是否是已完成的驱动包
                    if (!value && Tag is SelectedDriverPackageViewModel driverPackage)
                    {
                        if (driverPackage.IsCompleted)
                        {
                            // 已完成的不能取消，恢复选中状态
                            _isSelected = true;
                            OnPropertyChanged(nameof(IsSelected));
                            return;
                        }

                        // 未完成的，取消选中（这会触发PackageControl的Unchecked事件，停止下载/安装）
                        if (driverPackage._sourcePackageControl != null)
                        {
                            driverPackage._sourcePackageControl.IsSelected = false;
                        }
                    }
                    else if (value && Tag is SelectedDriverPackageViewModel driverPackage2)
                    {
                        // 重新选中
                        if (driverPackage2._sourcePackageControl != null)
                        {
                            driverPackage2._sourcePackageControl.IsSelected = true;
                        }
                    }

                    _isSelected = value;
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

        if (_currentMode == PageMode.DriverDownload)
        {
            // 驱动下载模式：显示已选择的驱动包，默认选中（除了已完成的）
            var driverPackages = new ObservableCollection<ISelectedActionViewModel>();
            foreach (var dp in SelectedDriverPackages)
            {
                var viewModel = new SelectedActionViewModel(
                    dp.Category,
                    dp.Category,
                    dp.PackageId,
                    dp.Title,
                    $"{dp.Description}{(dp.IsCompleted ? " [已完成]" : string.Empty)}", // 在描述中显示状态
                    null!); // null! 表示我们知道这里可以为 null，因为驱动包不需要 sourceAction

                // 存储对 SelectedDriverPackageViewModel 的引用，以便取消时能访问状态
                viewModel.Tag = dp;

                // 已完成的默认选中且不可取消，未完成的默认选中
                viewModel.IsSelected = true;
                driverPackages.Add(viewModel);
            }

            _selectedActionsWindow = new LenovoLegionToolkit.WPF.Windows.Utils.SelectedActionsWindow(
                driverPackages,
                Resource.WindowsOptimizationPage_SelectedActions_Empty ?? string.Empty)
            {
                Owner = Window.GetWindow(this)
            };
        }
        else
        {
            // 其他模式：显示已选择的操作
            var visibleActions = new ObservableCollection<ISelectedActionViewModel>(VisibleSelectedActions.Cast<ISelectedActionViewModel>());
            _selectedActionsWindow = new LenovoLegionToolkit.WPF.Windows.Utils.SelectedActionsWindow(visibleActions, SelectedActionsEmptyText)
            {
                Owner = Window.GetWindow(this)
            };
        }

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
            Interval = TimeSpan.FromSeconds(5) // 每5秒更新一次shell安装状态
        };
        _beautificationStatusTimer.Tick += async (s, e) => await RefreshBeautificationStatusAsync();
        _beautificationStatusTimer.Start();
    }

    private async Task RefreshBeautificationStatusAsync()
    {
        try
        {
            // 清除缓存以确保获取最新的安装状态
            NilesoftShellHelper.ClearInstallationStatusCache();
            
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
                                Arguments = $"/c \"\"{exe}\"\" -unregister -treat -restart",
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

    // Driver Download Mode Methods
    private async Task InitializeDriverDownloadModeAsync()
    {
        try
        {
            // 确保在 UI 线程上执行
            if (!Dispatcher.CheckAccess())
            {
                await Dispatcher.InvokeAsync(() => _ = InitializeDriverDownloadModeAsync());
                return;
            }

            // 标记正在初始化，避免触发来源切换事件
            _isInitializingDriverDownload = true;

            // 等待控件加载完成
            await Task.Delay(100);

            // 确保驱动下载内容可见
            var driverLoader = FindName("_driverLoader") as System.Windows.Controls.Grid;
            if (driverLoader != null)
                driverLoader.Visibility = Visibility.Visible;


            if (_driverMachineTypeTextBox != null && _driverOsComboBox != null && _driverDownloadToText != null)
            {
                var machineInfo = await Compatibility.GetMachineInformationAsync();
                _driverMachineTypeTextBox.Text = machineInfo.MachineType;
                _driverOsComboBox.SetItems(Enum.GetValues<OS>(), OSExtensions.GetCurrent(), os => os.GetDisplayName());

                var downloadsFolder = KnownFolders.GetPath(KnownFolder.Downloads);
                _driverDownloadToText.PlaceholderText = downloadsFolder;
                _driverDownloadToText.Text = Directory.Exists(_packageDownloaderSettings.Store.DownloadPath)
                    ? _packageDownloaderSettings.Store.DownloadPath
                    : downloadsFolder;


                if (_driverSourcePrimaryRadio != null)
                    _driverSourcePrimaryRadio.Tag = PackageDownloaderFactory.Type.Vantage;
                if (_driverSourceSecondaryRadio != null)
                    _driverSourceSecondaryRadio.Tag = PackageDownloaderFactory.Type.PCSupport;

                // 如果机器类型和操作系统已经设置好了，立即开始加载驱动包
                var machineType = _driverMachineTypeTextBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(machineType) && machineType.Length == 4 &&
                    _driverOsComboBox != null && _driverOsComboBox.TryGetSelectedItem<OS>(out _))
                {
                    // 自动触发加载驱动包
                    DriverDownloadPackagesButton_Click(this, new RoutedEventArgs());
                }
            }
            else
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace((FormattableString)$"Driver download controls not found, they may not be loaded yet.");
            }

            // 初始化完成，允许来源切换事件触发刷新
            _isInitializingDriverDownload = false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to initialize driver download mode.", ex);
        }
        finally
        {
            // 确保在异常情况下也重置标志
            _isInitializingDriverDownload = false;
        }
    }

    private void DriverDownloadToText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_driverDownloadToText == null)
            return;

        var location = _driverDownloadToText.Text;

        if (!Directory.Exists(location))
            return;

        _packageDownloaderSettings.Store.DownloadPath = location;
        _packageDownloaderSettings.SynchronizeStore();
    }

    private void DriverOpenDownloadToButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var location = GetDriverDownloadLocation();

            if (!Directory.Exists(location))
                return;

            Process.Start("explorer", location);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to open download location.", ex);
        }
    }

    private void DriverDownloadToButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverDownloadToText == null)
            return;

        using var ofd = new FolderBrowserDialog();
        ofd.InitialDirectory = _driverDownloadToText.Text;

        if (ofd.ShowDialog() != DialogResult.OK)
            return;

        var selectedPath = ofd.SelectedPath;
        _driverDownloadToText.Text = selectedPath;
        _packageDownloaderSettings.Store.DownloadPath = selectedPath;
        _packageDownloaderSettings.SynchronizeStore();
    }

    private async void DriverDownloadPackagesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ShouldInterruptDriverDownloadsIfRunning())
            return;

        var errorOccurred = false;
        try
        {
            if (_driverLoader != null)
            {
                _driverLoader.Visibility = Visibility.Visible;
            }

            // 确保 InfoBar 始终可见
            var driverInfoBar = FindName("_driverInfoBar") as Controls.Custom.InfoBar;
            if (driverInfoBar != null)
            {
                driverInfoBar.IsOpen = true;
            }

            _driverPackages = null;

            if (_driverPackagesStackPanel != null)
                _driverPackagesStackPanel.Children.Clear();
            if (_driverScrollViewer != null)
                _driverScrollViewer.ScrollToHome();

            if (_driverFilterTextBox != null)
                _driverFilterTextBox.Text = string.Empty;
            if (_driverSortingComboBox != null)
                _driverSortingComboBox.SelectedIndex = 2;

            var machineType = _driverMachineTypeTextBox?.Text.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(machineType) || machineType.Length != 4 ||
                _driverOsComboBox == null || !_driverOsComboBox.TryGetSelectedItem(out OS os))
            {
                // 确保 _driverLoader 可见，以便用户可以看到筛选和排序控件
                if (_driverLoader != null)
                {
                    _driverLoader.Visibility = Visibility.Visible;
                }
                await SnackbarHelper.ShowAsync(Resource.PackagesPage_DownloadFailed_Title,
                    Resource.PackagesPage_DownloadFailed_Message);
                return;
            }

            // 显示加载提示
            var loadingIndicator = FindName("_driverLoadingIndicator") as System.Windows.Controls.Border;
            if (loadingIndicator != null)
                loadingIndicator.Visibility = Visibility.Visible;

            if (_driverGetPackagesTokenSource is not null)
                await _driverGetPackagesTokenSource.CancelAsync();

            _driverGetPackagesTokenSource = new();

            var token = _driverGetPackagesTokenSource.Token;

            var packageDownloaderType = new[] { _driverSourcePrimaryRadio, _driverSourceSecondaryRadio }
                .Where(r => r != null && r.IsChecked == true)
                .Select(r => (PackageDownloaderFactory.Type)r.Tag)
                .FirstOrDefault();

            if (_driverOnlyShowUpdatesCheckBox != null)
            {
                // 两种来源都支持"仅显示更新"功能，始终显示复选框
                _driverOnlyShowUpdatesCheckBox.Visibility = Visibility.Visible;
                // 根据来源类型设置默认选中状态
                if (packageDownloaderType == PackageDownloaderFactory.Type.Vantage)
                {
                    _driverOnlyShowUpdatesCheckBox.IsChecked = _packageDownloaderSettings.Store.OnlyShowUpdates;
                }
                else
                {
                    // 备用来源默认不选中"仅显示更新"
                    _driverOnlyShowUpdatesCheckBox.IsChecked = false;
                }
            }

            _driverPackageDownloader = _packageDownloaderFactory.GetInstance(packageDownloaderType);
            var packages = await _driverPackageDownloader.GetPackagesAsync(machineType, os, new DriverDownloadProgressReporter(this), token);

            _driverPackages = packages;

            DriverReload();

            // 隐藏加载提示
            if (loadingIndicator != null)
                loadingIndicator.Visibility = Visibility.Collapsed;

            // 加载完成后，更新按钮状态
            ApplyInteractionState();
        }
        catch (UpdateCatalogNotFoundException ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Update catalog not found.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackagesPage_UpdateCatalogNotFound_Title, Resource.PackagesPage_UpdateCatalogNotFound_Message, SnackbarType.Info);

            errorOccurred = true;
        }
        catch (OperationCanceledException)
        {
            errorOccurred = true;
        }
        catch (HttpRequestException ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading packages.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackagesPage_Error_Title, Resource.PackagesPage_Error_CheckInternet_Message, SnackbarType.Error);

            errorOccurred = true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading packages.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackagesPage_Error_Title, ex.Message, SnackbarType.Error);

            errorOccurred = true;
        }
        finally
        {
            // 确保在异常情况下也隐藏加载提示
            var loadingIndicator = FindName("_driverLoadingIndicator") as System.Windows.Controls.Border;
            if (loadingIndicator != null)
                loadingIndicator.Visibility = Visibility.Collapsed;

            if (errorOccurred)
            {
                if (_driverPackagesStackPanel != null)
                    _driverPackagesStackPanel.Children.Clear();
                // 不在错误时隐藏 _driverLoader，保持可见以显示错误状态或允许用户重试
                // 确保 _driverLoader 始终可见，以便用户可以看到错误信息或重试
                if (_driverLoader != null)
                {
                    _driverLoader.Visibility = Visibility.Visible;
                }
            }
        }
    }

    private async void DriverSourceRadio_Checked(object sender, RoutedEventArgs e)
    {
        // 如果正在初始化，不触发刷新
        if (_isInitializingDriverDownload)
            return;

        // 检查机器类型和操作系统是否已设置
        var machineType = _driverMachineTypeTextBox?.Text.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(machineType) || machineType.Length != 4 ||
            _driverOsComboBox == null || !_driverOsComboBox.TryGetSelectedItem<OS>(out _))
        {
            return;
        }

        // 如果正在扫描驱动包，取消当前扫描并重新开始
        if (_driverGetPackagesTokenSource != null && !_driverGetPackagesTokenSource.Token.IsCancellationRequested)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Cancelling current driver package scan due to source change...");

            // 取消当前扫描
            await _driverGetPackagesTokenSource.CancelAsync();

            // 等待一小段时间确保取消操作完成
            await Task.Delay(100);
        }

        // 确保 InfoBar 始终可见
        var driverInfoBar = FindName("_driverInfoBar") as Controls.Custom.InfoBar;
        if (driverInfoBar != null)
        {
            driverInfoBar.IsOpen = true;
        }

        // 切换来源时自动刷新驱动包列表（无论之前是否已经加载过）
        DriverDownloadPackagesButton_Click(this, new RoutedEventArgs());
    }

    private async void DriverFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!await ShouldInterruptDriverDownloadsIfRunning())
            return;

        try
        {
            if (_driverPackages is null)
                return;

            if (_driverFilterDebounceCancellationTokenSource is not null)
                await _driverFilterDebounceCancellationTokenSource.CancelAsync();

            _driverFilterDebounceCancellationTokenSource = new();

            await Task.Delay(500, _driverFilterDebounceCancellationTokenSource.Token);

            if (_driverPackagesStackPanel != null)
                _driverPackagesStackPanel.Children.Clear();
            if (_driverScrollViewer != null)
                _driverScrollViewer.ScrollToHome();

            DriverReload();
        }
        catch (OperationCanceledException) { }
    }

    private async void DriverOnlyShowUpdatesCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!await ShouldInterruptDriverDownloadsIfRunning())
            return;

        if (_driverPackages is null)
            return;

        if (_driverOnlyShowUpdatesCheckBox != null)
        {
            _packageDownloaderSettings.Store.OnlyShowUpdates = _driverOnlyShowUpdatesCheckBox.IsChecked ?? false;
            _packageDownloaderSettings.SynchronizeStore();
        }

        if (_driverPackagesStackPanel != null)
            _driverPackagesStackPanel.Children.Clear();
        if (_driverScrollViewer != null)
            _driverScrollViewer.ScrollToHome();

        DriverReload();
    }

    private async void DriverSortingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!await ShouldInterruptDriverDownloadsIfRunning())
            return;

        if (_driverPackages is null)
            return;

        if (_driverPackagesStackPanel != null)
            _driverPackagesStackPanel.Children.Clear();
        if (_driverScrollViewer != null)
            _driverScrollViewer.ScrollToHome();

        DriverReload();
    }

    private string GetDriverDownloadLocation()
    {
        if (_driverDownloadToText == null)
            return KnownFolders.GetPath(KnownFolder.Downloads);

        var location = _driverDownloadToText.Text.Trim();

        if (!Directory.Exists(location))
        {
            var downloads = KnownFolders.GetPath(KnownFolder.Downloads);
            location = downloads;
            _driverDownloadToText.Text = downloads;
            _packageDownloaderSettings.Store.DownloadPath = downloads;
            _packageDownloaderSettings.SynchronizeStore();
        }

        return location;
    }

    private ContextMenu? GetDriverContextMenu(Package package, IEnumerable<Package> packages)
    {
        if (_packageDownloaderSettings.Store.HiddenPackages.Contains(package.Id))
            return null;

        var hideMenuItem = new MenuItem
        {
            SymbolIcon = SymbolRegular.EyeOff24,
            Header = Resource.Hide,
        };
        hideMenuItem.Click += (_, _) =>
        {
            _packageDownloaderSettings.Store.HiddenPackages.Add(package.Id);
            _packageDownloaderSettings.SynchronizeStore();

            DriverReload();
        };

        var hideAllMenuItem = new MenuItem
        {
            SymbolIcon = SymbolRegular.EyeOff24,
            Header = Resource.HideAll,
        };
        hideAllMenuItem.Click += (_, _) =>
        {
            foreach (var id in packages.Select(p => p.Id))
                _packageDownloaderSettings.Store.HiddenPackages.Add(id);
            _packageDownloaderSettings.SynchronizeStore();

            DriverReload();
        };

        var cm = new ContextMenu();
        cm.Items.Add(hideMenuItem);
        cm.Items.Add(hideAllMenuItem);
        return cm;
    }

    /// <summary>
    /// 检查驱动下载相关的操作是否正在进行
    /// </summary>
    private bool IsDriverDownloadBusy()
    {
        // 只检查是否有驱动包正在下载或安装
        // 加载驱动包列表时不应该禁用按钮，因为用户仍然可以选择推荐项或暂停操作
        if (_driverPackagesStackPanel?.Children != null)
        {
            var isAnyDownloadingOrInstalling = _driverPackagesStackPanel.Children
                .OfType<Controls.Packages.PackageControl>()
                .Any(pc => pc.Status == Controls.Packages.PackageControl.PackageStatus.Downloading ||
                          pc.Status == Controls.Packages.PackageControl.PackageStatus.Installing ||
                          pc.IsDownloading);
            if (isAnyDownloadingOrInstalling)
                return true;
        }

        return false;
    }

    private async Task<bool> ShouldInterruptDriverDownloadsIfRunning()
    {
        if (_driverPackagesStackPanel?.Children is null)
            return true;

        if (_driverPackagesStackPanel.Children.ToArray().OfType<PackageControl>().Where(pc => pc.IsDownloading).IsEmpty())
            return true;

        return await MessageBoxHelper.ShowAsync(this, Resource.PackagesPage_DownloadInProgress_Title, Resource.PackagesPage_DownloadInProgress_Message);
    }

    private void DriverReload()
    {
        if (_driverPackageDownloader is null || _driverPackagesStackPanel == null)
            return;

        _driverPackagesStackPanel.Children.Clear();

        if (_driverPackages is null || _driverPackages.Count == 0)
            return;

        var packages = DriverSortAndFilter(_driverPackages);

        foreach (var package in packages)
        {
            var control = new PackageControl(_driverPackageDownloader, package, GetDriverDownloadLocation)
            {
                ContextMenu = GetDriverContextMenu(package, packages)
            };

            // 监听选择状态变化
            control.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PackageControl.IsSelected))
                {
                    UpdateSelectedDriverPackages();
                }
                // 监听状态变化，如果变成已完成则隐藏控件
                else if (e.PropertyName == nameof(PackageControl.Status) ||
                         e.PropertyName == nameof(PackageControl.IsDownloading))
                {
                    if (control.Status == Controls.Packages.PackageControl.PackageStatus.Completed)
                    {
                        control.Visibility = Visibility.Collapsed;
                    }
                    // 当驱动包状态改变时，更新按钮状态（如果当前在驱动下载模式）
                    if (_currentMode == PageMode.DriverDownload)
                    {
                        ApplyInteractionState();
                    }
                }
            };

            _driverPackagesStackPanel.Children.Add(control);
        }

        // 初始化已选择的驱动包列表
        UpdateSelectedDriverPackages();

        // 检查已选择的驱动包，如果已完成则隐藏对应的控件
        foreach (var selectedPackage in SelectedDriverPackages)
        {
            if (selectedPackage.IsCompleted && selectedPackage._sourcePackageControl != null)
            {
                selectedPackage._sourcePackageControl.Visibility = Visibility.Collapsed;
            }
        }

        if (packages.IsEmpty())
        {
            var tb = new TextBlock
            {
                Text = Resource.PackagesPage_NoMatchingDownloads,
                Foreground = (SolidColorBrush)FindResource("TextFillColorSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new(0, 32, 0, 32),
                Focusable = true
            };
            _driverPackagesStackPanel.Children.Add(tb);
        }

        if (_packageDownloaderSettings.Store.HiddenPackages.Count != 0)
        {
            var clearHidden = new Hyperlink
            {
                Icon = SymbolRegular.Eye24,
                Content = Resource.WindowsOptimizationPage_ShowHiddenDownloads,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            clearHidden.Click += (_, _) =>
            {
                _packageDownloaderSettings.Store.HiddenPackages.Clear();
                _packageDownloaderSettings.SynchronizeStore();

                DriverReload();
            };
            _driverPackagesStackPanel.Children.Add(clearHidden);
        }
    }

    private List<Package> DriverSortAndFilter(List<Package> packages)
    {
        var selectedIndex = _driverSortingComboBox?.SelectedIndex ?? 2;
        var result = selectedIndex switch
        {
            0 => packages.OrderBy(p => p.Title),
            1 => packages.OrderBy(p => p.Category),
            2 => packages.OrderByDescending(p => p.ReleaseDate),
            _ => packages.AsEnumerable(),
        };

        result = result.Where(p => !_packageDownloaderSettings.Store.HiddenPackages.Contains(p.Id));

        if (_driverOnlyShowUpdatesCheckBox?.IsChecked ?? false)
            result = result.Where(p => p.IsUpdate);

        var filterText = _driverFilterTextBox?.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(filterText))
            result = result.Where(p => p.Index.Contains(filterText, StringComparison.InvariantCultureIgnoreCase));

        return result.ToList();
    }

    private class DriverDownloadProgressReporter : IProgress<float>
    {
        private readonly WindowsOptimizationPage _page;

        public DriverDownloadProgressReporter(WindowsOptimizationPage page)
        {
            _page = page;
        }

        public void Report(float value) => _page.Dispatcher.Invoke(() =>
        {
            // LoadableControl removed, no loading indicator needed
        });
    }

    public class SelectedDriverPackageViewModel : INotifyPropertyChanged, IDisposable
    {
        internal readonly LenovoLegionToolkit.WPF.Controls.Packages.PackageControl? _sourcePackageControl;

        public SelectedDriverPackageViewModel(
            string packageId,
            string title,
            string description,
            string category,
            LenovoLegionToolkit.WPF.Controls.Packages.PackageControl sourcePackageControl)
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

        public void Dispose()
        {
            if (_sourcePackageControl is not null)
                _sourcePackageControl.PropertyChanged -= SourcePackageControl_PropertyChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string StatusText
        {
            get
            {
                if (_sourcePackageControl is not null)
                {
                    return _sourcePackageControl.Status switch
                    {
                        Controls.Packages.PackageControl.PackageStatus.Downloading => "下载中",
                        Controls.Packages.PackageControl.PackageStatus.Installing => "安装中",
                        Controls.Packages.PackageControl.PackageStatus.Completed => "已完成",
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

        private void SourcePackageControl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Controls.Packages.PackageControl.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
            else if (e.PropertyName == nameof(Controls.Packages.PackageControl.Status))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsCompleted));

                // 如果状态变成已完成，隐藏控件（在主界面中）
                if (_sourcePackageControl != null && _sourcePackageControl.Status == Controls.Packages.PackageControl.PackageStatus.Completed)
                {
                    _sourcePackageControl.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
