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
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Windows;
using LenovoLegionToolkit.WPF.Windows.Utils;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.WPF.Controls.Packages;
using LenovoLegionToolkit.WPF.Extensions;
using System.Net.Http;
using System.Windows.Forms;
using System.Windows.Media;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

#pragma warning disable CS0169 // 字段从未使用
#pragma warning disable CS1998 // 异步方法缺少 await 运算符

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage : INotifyPropertyChanged
{
    #region 服务和依赖项
    
    private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private readonly PackageDownloaderSettings _packageDownloaderSettings = IoCContainer.Resolve<PackageDownloaderSettings>();
    private readonly PackageDownloaderFactory _packageDownloaderFactory = IoCContainer.Resolve<PackageDownloaderFactory>();
    
    #endregion

    #region Shell Integration Helper

    private IShellIntegrationHelper? GetShellIntegrationHelper()
    {
        try
        {
            // Try to get the shell-integration plugin
            if (_pluginManager.TryGetPlugin("shell-integration", out var plugin) && plugin is IShellIntegrationHelper helper)
                return helper;
        }
        catch
        {
            // Plugin not available or not implementing the interface
        }
        return null;
    }

    #endregion
    
    #region 驱动下载字段
    
    private IPackageDownloader? _driverPackageDownloader;
    private CancellationTokenSource? _driverGetPackagesTokenSource;
    private CancellationTokenSource? _driverFilterDebounceCancellationTokenSource;
    private List<Package>? _driverPackages;
    private System.Windows.Threading.DispatcherTimer? _driverRetryTimer;
    private string? _driverRetryMachineType;
    private OS? _driverRetryOS;
    private PackageDownloaderFactory.Type? _driverRetryPackageDownloaderType;
    private bool _driverPackagesExpanded;
    private string _driverExpandCollapseText = string.Empty;
    
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
    
    #endregion
    
    #region 页面模式
    
    private enum PageMode
    {
        Optimization,
        Cleanup,
        DriverDownload,
        Extensions
    }
    
    [Flags]
    private enum InteractionScope
    {
        Optimization = 1,
        Cleanup = 2,
        Beautification = 4,
        All = Optimization | Cleanup | Beautification
    }
    
    private PageMode _currentMode = PageMode.Optimization;
    
    #endregion
    
    #region 主要属性
    
    private bool _isBusy;
    private SelectedActionsWindow? _selectedActionsWindow;
    private string _selectedActionsSummaryFormat = "{0}";
    private string _selectedActionsEmptyText = string.Empty;
    private OptimizationCategoryViewModel? _selectedCategory;
    private OptimizationCategoryViewModel? _lastOptimizationCategory;
    private OptimizationCategoryViewModel? _lastCleanupCategory;
    private OptimizationCategoryViewModel? _lastBeautificationCategory;
    private long _estimatedCleanupSize;
    private bool _isCalculatingSize;
    private string _currentOperationText = string.Empty;
    private string _currentDeletingFile = string.Empty;
    private string _runCleanupButtonText = string.Empty;
    
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
    public ObservableCollection<CustomCleanupRuleViewModel> CustomCleanupRules { get; } = [];
    
    public ObservableCollection<SelectedActionViewModel> VisibleSelectedActions => _currentMode switch
    {
        PageMode.Cleanup => SelectedCleanupActions,
        PageMode.Optimization => new ObservableCollection<SelectedActionViewModel>(
            SelectedOptimizationActions.Concat(SelectedBeautificationActions)),
        _ => SelectedOptimizationActions
    };
    
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
    
    public string PendingText => GetResource("WindowsOptimizationPage_EstimatedCleanupSize_Pending");
    public string CompactText => GetResource("Compact");
    public string ExpandAllText => GetResource("ExpandAll");
    public string CollapseAllText => GetResource("CollapseAll");
    
    private string _expandCollapseText = string.Empty;
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
    
    private bool _isCompactView;
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
    
    public bool IsCleanupMode => _currentMode == PageMode.Cleanup;
    public bool IsBeautificationMode => _currentMode == PageMode.Optimization && BeautificationCategories.Count > 0;
    public bool IsDriverDownloadMode => _currentMode == PageMode.DriverDownload;
    public bool IsExtensionsMode => _currentMode == PageMode.Extensions;
    
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
                    if (value != null && value.Key.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase))
                        _lastBeautificationCategory = value;
                    else
                        _lastOptimizationCategory = value;
                    break;
            }
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }
    
    public IEnumerable<OptimizationCategoryViewModel> ActiveCategories => _currentMode switch
    {
        PageMode.Cleanup => CleanupCategories,
        PageMode.DriverDownload => [],
        _ => OptimizationCategories.Concat(BeautificationCategories)
    };
    
    #endregion
    
    #region Beautification字段和属性
    
    private bool _isRefreshingStates = false;
    private readonly HashSet<string> _userUncheckedActions = new(StringComparer.OrdinalIgnoreCase);
    private bool _transparencyEnabled;
    private bool _roundedCornersEnabled = true;
    private bool _shadowsEnabled = true;
    private string _selectedTheme = "auto";
    private System.Windows.Threading.DispatcherTimer? _beautificationStatusTimer;
    private MenuStyleSettingsWindow? _styleSettingsWindow;
    private ActionDetailsWindow? _actionDetailsWindow;
    
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
            var helper = GetShellIntegrationHelper();
            if (helper == null)
                return false;
                
            var isInstalled = helper.IsInstalled();
            var isInstalledUsingShellExe = helper.IsInstalledUsingShellExe();
            return isInstalled && !isInstalledUsingShellExe;
        }
    }
    
    public bool CanUninstall
    {
        get
        {
            var helper = GetShellIntegrationHelper();
            if (helper == null)
                return false;
                
            return helper.IsInstalledUsingShellExe();
        }
    }
    
    #endregion
    
    #region 辅助方法和字段
    
    private bool _optimizationInteractionEnabled = true;
    private bool _cleanupInteractionEnabled = true;
    private bool _beautificationInteractionEnabled = true;
    
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
    
    private static string GetResource(string key)
    {
        return Resource.ResourceManager.GetString(key) ?? key;
    }
    
    private void ApplyInteractionState()
    {
        var optimizationEnabled = _optimizationInteractionEnabled;
        var cleanupEnabled = _cleanupInteractionEnabled;
        var beautificationEnabled = _beautificationInteractionEnabled;
        
        var primaryButtonsEnabled = _currentMode switch
        {
            PageMode.Cleanup => cleanupEnabled,
            PageMode.Optimization => optimizationEnabled || beautificationEnabled,
            PageMode.DriverDownload => true,
            _ => true
        };
        
        // TODO: 更新UI控件启用状态
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
    
    private async Task RefreshBeautificationStatusAsync()
    {
        try
        {
            // 清除缓存以确保获取最新的安装状态
            var helper = GetShellIntegrationHelper();
            helper?.ClearInstallationStatusCache();
            
            var isInstalled = helper != null ? await helper.IsInstalledUsingShellExeAsync() : false;
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
        var isInstalled = LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.IsInstalled();

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
            var helper = GetShellIntegrationHelper();
            var shellExePath = helper?.GetNilesoftShellExePath();
            if (string.IsNullOrWhiteSpace(shellExePath))
                return null;
                
            var shellDir = Path.GetDirectoryName(shellExePath);
            if (string.IsNullOrWhiteSpace(shellDir))
                return null;
                
            return Path.Combine(shellDir, "shell.nss");
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region 构造函数和初始化
    
    public WindowsOptimizationPage()
    {
        InitializeComponent();
        DataContext = this;
        
        CustomCleanupRules.CollectionChanged += CustomCleanupRules_CollectionChanged;
        LoadCustomCleanupRules();
        UpdateCleanupControlsState();
    }
    
    private void LoadCustomCleanupRules()
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var rules = settings.Store.CustomCleanupRules ?? new List<CustomCleanupRule>();
        
        CustomCleanupRules.Clear();
        foreach (var rule in rules)
        {
            CustomCleanupRules.Add(new CustomCleanupRuleViewModel(
                rule.DirectoryPath,
                rule.Extensions ?? [],
                rule.Recursive));
        }
    }
    
    private void UpdateCleanupControlsState()
    {
        // TODO: 实现清理控件状态更新
    }
    
    private void CustomCleanupRules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        settings.Store.CustomCleanupRules = CustomCleanupRules.Select(r => r.ToModel()).ToList();
        settings.SynchronizeStore();
    }
    
    private void WindowsOptimizationPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            // 页面变为可见时初始化
        }
    }
    
    private void WindowsOptimizationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // TODO: 清理资源
    }
    
    #endregion
    
    #region 清理规则管理
    
    private void RefreshCleanupActionAvailability()
    {
        bool hasCustomRules = CustomCleanupRules.Any(rule => 
            !string.IsNullOrWhiteSpace(rule.DirectoryPath) && 
            rule.Extensions.Count > 0);
            
        var customCleanupCategory = CleanupCategories.FirstOrDefault(c => 
            c.Key.Equals("cleanup.custom", StringComparison.OrdinalIgnoreCase));
            
        if (customCleanupCategory != null)
        {
            var customAction = customCleanupCategory.Actions.FirstOrDefault(a => 
                a.Key.Equals(WindowsOptimizationService.CustomCleanupActionKey, StringComparison.OrdinalIgnoreCase));
                
            if (customAction != null)
            {
                customAction.IsEnabled = hasCustomRules;
                if (!hasCustomRules)
                    customAction.IsSelected = false;
            }
        }
    }
    
    private async Task UpdateEstimatedCleanupSizeAsync()
    {
        if (_currentMode != PageMode.Cleanup)
            return;
            
        var selectedKeys = CleanupCategories
            .SelectMany(c => c.Actions.Where(a => a.IsEnabled && a.IsSelected))
            .Select(a => a.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
            
        if (selectedKeys.Count == 0)
        {
            EstimatedCleanupSize = 0;
            return;
        }
        
        IsCalculatingSize = true;
        try
        {
            var size = await _windowsOptimizationService.EstimateCleanupSizeAsync(selectedKeys, CancellationToken.None);
            EstimatedCleanupSize = size;
        }
        catch
        {
            EstimatedCleanupSize = 0;
        }
        finally
        {
            IsCalculatingSize = false;
        }
    }
    
    #endregion
    
    #region INotifyPropertyChanged 实现
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    #endregion
    
    #region XAML 事件处理程序占位符
    
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (Categories.Count > 0)
            return;
            
        InitializeCategories();
        SetMode(PageMode.Optimization);
        await InitializeActionStatesAsync();
        UpdateSelectedActions();
        
        ToggleInteraction(true, InteractionScope.All);
        
        Unloaded += WindowsOptimizationPage_Unloaded;
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
                
            foreach (var actionVm in actions)
            {
                 actionVm.PropertyChanged += (_, args) =>
                 {
                     if (args.PropertyName == nameof(OptimizationActionViewModel.IsSelected))
                     {
                         // TODO: 处理操作选中状态变化
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
        SelectedCategory = ActiveCategories.FirstOrDefault();
        RefreshExpandCollapseText();
    }
    
    private void Category_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateSelectedActions();
    }
    
    private void UpdateSelectedActions()
    {
        // TODO: 实现选中操作更新
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
    }
    
    private Task InitializeActionStatesAsync()
    {
        // TODO: 初始化操作状态
        return Task.CompletedTask;
    }
    
    private void RefreshExpandCollapseText()
    {
        var list = ActiveCategories.ToList();
        var allExpanded = list.Count > 0 && list.All(c => c.IsExpanded);
        ExpandCollapseText = allExpanded ? CollapseAllText : ExpandAllText;
    }
    
    private void OptimizationNavButton_Checked(object sender, RoutedEventArgs e)
    {
        SetMode(PageMode.Optimization);
    }
    
    private void CleanupNavButton_Checked(object sender, RoutedEventArgs e)
    {
        SetMode(PageMode.Cleanup);
    }
    
    private void DriverDownloadNavButton_Checked(object sender, RoutedEventArgs e)
    {
        SetMode(PageMode.DriverDownload);
    }
    
    private void ExtensionsNavButton_Checked(object sender, RoutedEventArgs e)
    {
        SetMode(PageMode.Extensions);
    }
    
    private void SetMode(PageMode mode)
    {
        if (_currentMode == mode)
            return;
            
        _currentMode = mode;
        
        // 更新UI状态
        OnPropertyChanged(nameof(IsCleanupMode));
        OnPropertyChanged(nameof(IsBeautificationMode));
        OnPropertyChanged(nameof(IsDriverDownloadMode));
        OnPropertyChanged(nameof(IsExtensionsMode));
        OnPropertyChanged(nameof(ActiveCategories));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
        
        // 恢复上次选中的分类
        SelectedCategory = mode switch
        {
            PageMode.Cleanup => _lastCleanupCategory,
            PageMode.Optimization => _lastOptimizationCategory ?? _lastBeautificationCategory,
            _ => null
        };
        
        // 确保至少有一个分类被选中
        if (SelectedCategory == null)
            SelectedCategory = ActiveCategories.FirstOrDefault();
            
        RefreshExpandCollapseText();
        ApplyInteractionState();
    }
    
    private void SelectedActionsButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现选中操作按钮点击
    }
    
    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled)
            return;
            
        var scrollViewer = sender as System.Windows.Controls.ScrollViewer;
        if (scrollViewer != null)
        {
            e.Handled = true;
            var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
        }
    }
    
    private void OpenStyleSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现打开样式设置按钮点击
    }
    
    private void ActionItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // TODO: 实现操作项鼠标左键按下事件
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
        foreach (var category in categories)
            category.SelectRecommended();
            
        UpdateSelectedActions();
    }
    
    private void DriverSelectRecommendedButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现驱动选择推荐按钮点击
    }
    
    private void DriverPauseAllButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现驱动暂停所有按钮点击
    }
    
    // 驱动下载相关事件处理程序
    private void DriverScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled)
            return;
            
        var scrollViewer = sender as System.Windows.Controls.ScrollViewer;
        if (scrollViewer != null)
        {
            e.Handled = true;
            var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
        }
    }
    
    private void DriverSourceRadio_Checked(object sender, RoutedEventArgs e)
    {
        // TODO: 实现驱动源单选按钮选中事件
    }
    
    private void DriverDownloadToText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: 实现驱动下载路径文本更改事件
    }
    
    private void DriverDownloadToButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现驱动下载路径按钮点击事件
    }
    
    private void DriverOpenDownloadToButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现打开驱动下载路径按钮点击事件
    }
    
    private void DriverFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: 实现驱动筛选器文本更改事件
    }
    
    private void DriverOnlyShowUpdatesCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        // TODO: 实现仅显示更新复选框选中事件
    }
    
    private void DriverOnlyShowUpdatesCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        // TODO: 实现仅显示更新复选框取消选中事件
    }
    
    private void DriverSortingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: 实现驱动排序组合框选择更改事件
    }
    
    // 清理相关事件处理程序
    private void RunCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现运行清理按钮点击事件
    }
    
    private void AddCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现添加自定义清理规则按钮点击事件
    }
    
    private void ClearCustomCleanupRulesButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现清除自定义清理规则按钮点击事件
    }
    
    private void EditCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现编辑自定义清理规则按钮点击事件
    }
    
    private void RemoveCustomCleanupRuleButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现移除自定义清理规则按钮点击事件
    }
    
    // 美化相关事件处理程序
     private void BeautificationThemeRadio_Checked(object sender, RoutedEventArgs e)
     {
         if (sender is System.Windows.Controls.RadioButton radio && radio.IsChecked == true)
         {
             if (radio.Name.Contains("Auto"))
                 SelectedTheme = "auto";
             else if (radio.Name.Contains("Light"))
                 SelectedTheme = "light";
             else if (radio.Name.Contains("Dark"))
                 SelectedTheme = "dark";
             else if (radio.Name.Contains("Classic"))
                 SelectedTheme = "classic";
             else if (radio.Name.Contains("Modern"))
                 SelectedTheme = "modern";
         }
     }
    
     private void TransparencyToggle_Click(object sender, RoutedEventArgs e)
     {
         TransparencyEnabled = !TransparencyEnabled;
     }
    
     private async void ApplyBeautificationStyleButton_Click(object sender, RoutedEventArgs e)
     {
         try
         {
             var configPath = GetShellConfigPath();
             if (string.IsNullOrWhiteSpace(configPath))
             {
                 // Nilesoft Shell not installed, maybe prompt installation
                 await SnackbarHelper.ShowAsync(
                     Resource.SettingsPage_WindowsOptimization_Title,
                     Resource.WindowsOptimizationPage_Beautification_ShellNotFound_Message ?? "Nilesoft Shell is not installed. Please install it first.",
                     SnackbarType.Warning);
                 return;
             }

             var config = GenerateShellConfig(SelectedTheme, TransparencyEnabled, RoundedCornersEnabled, ShadowsEnabled);
             await File.WriteAllTextAsync(configPath, config);

             await SnackbarHelper.ShowAsync(
                 Resource.SettingsPage_WindowsOptimization_Title,
                 Resource.WindowsOptimizationPage_Beautification_Applied_Message ?? "Beautification style applied successfully.",
                 SnackbarType.Success);
         }
         catch (Exception ex)
         {
             if (Log.Instance.IsTraceEnabled)
                 Log.Instance.Trace($"Failed to apply beautification style: {ex.Message}", ex);

             await SnackbarHelper.ShowAsync(
                 Resource.SettingsPage_WindowsOptimization_Title,
                 Resource.WindowsOptimizationPage_Beautification_ApplyError_Message ?? "Failed to apply beautification style.",
                 SnackbarType.Error);
         }
     }
    
    // 插件扩展相关事件处理程序
    private void PluginExtensionConfigure_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 实现插件扩展配置按钮点击事件
    }
    
    #endregion
    
    #region 异步操作和错误处理
    
    private async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        string successMessage,
        string errorMessage,
        InteractionScope scope)
    {
        if (IsBusy)
            return;
            
        IsBusy = true;
        ToggleInteraction(false, scope);
        
        try
        {
            await operation(CancellationToken.None);
            // TODO: 显示成功消息
        }
        catch (OperationCanceledException)
        {
            // TODO: 显示取消消息
        }
        catch (Exception ex)
        {
            // TODO: 显示错误消息
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"操作失败: {ex.Message}", ex);
        }
        finally
        {
            IsBusy = false;
            ToggleInteraction(true, scope);
        }
    }
    
    private async Task RefreshActionStatesAsync(bool skipUserInteractionCheck = false)
    {
        // 如果正在刷新状态，直接返回，避免重复刷新
        if (_isRefreshingStates)
            return;
            
        _isRefreshingStates = true;
        
        try
        {
            var actionsByKey = WindowsOptimizationService.GetCategories()
                .SelectMany(category => category.Actions)
                .ToDictionary(action => action.Key, StringComparer.OrdinalIgnoreCase);
                
            foreach (var category in Categories)
            {
                foreach (var action in category.Actions)
                {
                    if (!actionsByKey.TryGetValue(action.Key, out var actionDefinition))
                        continue;
                        
                    if (actionDefinition.IsAppliedAsync == null)
                        continue;
                        
                    try
                    {
                        var applied = await actionDefinition.IsAppliedAsync(CancellationToken.None).ConfigureAwait(false);
                        
                        // 计算距离上次用户交互的时间
                        var timeSinceLastInteraction = DateTime.Now - _lastUserInteraction;
                        var isRecentInteraction = timeSinceLastInteraction < TimeSpan.FromSeconds(10);
                        
                        // 如果用户在取消列表中且当前状态是已应用，取消操作可能仍在进行中，不要自动勾选
                        if (_userUncheckedActions.Contains(action.Key) && applied)
                        {
                            // 用户明确取消勾选，即使系统状态仍然是已应用，也不要自动勾选
                            // 只有当系统状态变为未应用时才从取消列表中移除
                            if (!applied)
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
                            continue;
                        }
                        
                        if (action.IsSelected != applied)
                        {
                            // 如果用户有最近交互且用户选中但检查显示未应用，操作可能正在进行中，不要立即更新
                            // 但如果用户未选中但检查显示已应用，应该更新（可能是外部操作导致的）
                            if (isRecentInteraction && action.IsSelected && !applied)
                            {
                                // 用户选中但检查显示未应用，操作可能正在进行中，暂时不更新
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"跳过 {action.Key} 的状态更新：用户选中但尚未应用（操作可能正在进行中）");
                            }
                            else
                            {
                                action.IsSelected = applied;
                                // 如果状态更新为未应用，从取消列表中移除（表示取消操作已完成）
                                if (!applied && _userUncheckedActions.Contains(action.Key))
                                {
                                    _userUncheckedActions.Remove(action.Key);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"检查操作状态失败: {action.Key}", ex);
                    }
                }
            }
            
            await Dispatcher.InvokeAsync(UpdateSelectedActions);
        }
        finally
        {
            // 重置标志以允许后续用户操作触发命令
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
                
            // 对于美化操作，始终执行命令而不检查当前状态
            if (actionKey.StartsWith("beautify.", StringComparison.OrdinalIgnoreCase))
            {
                // 对于右键美化操作，首先检查是否安装，如果未安装，先安装再应用
                if (actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"用户选中美化操作 {actionKey}，首先检查安装状态");
                        
                    // 首先检查是否安装
        var helper = GetShellIntegrationHelper();
        var isInstalled = helper?.IsInstalled() ?? false;
                    
                    var shellDll = LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.GetNilesoftShellDllPath();
                    if (string.IsNullOrWhiteSpace(shellDll))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Shell.dll 未找到，无法执行安装命令");
                        
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // 显示错误消息
                            // TODO: 实现Snackbar消息显示
                        });
                        return;
                    }
                    
                    try
                    {
                        // 如果未安装，先安装；如果已安装，直接应用设置
                        if (!isInstalled)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Nilesoft Shell 未安装，先安装");
                                
                            // 执行安装命令
                            await Task.Run(() =>
                            {
                                var process = new System.Diagnostics.Process
                                {
                                    StartInfo = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "cmd.exe",
                                        Arguments = $"/c regsvr32.exe /s \"{shellDll}\"",
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    }
                                };
                                process.Start();
                                process.WaitForExit();
                            });
                            
                            // 安装后清除缓存并等待片刻，让系统有时间完成安装
                            LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.ClearInstallationStatusCache();
                            await Task.Delay(2000);
                        }
                        else
                        {
                            // 如果已安装，直接应用设置（注册并重启资源管理器）
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Nilesoft Shell 已安装，使用 regsvr32 应用设置");
                                
                            await Task.Run(() =>
                            {
                                var process = new System.Diagnostics.Process
                                {
                                    StartInfo = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "cmd.exe",
                                        Arguments = "/c regsvr32.exe /s \"shell.dll\"",
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    }
                                };
                                process.Start();
                                process.WaitForExit();
                            });
                        }
                        
                        // 应用操作后清除缓存，强制下次检查时使用最新的实际状态
                        LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.ClearInstallationStatusCache();
                        
                        // 显示成功消息
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // TODO: 实现Snackbar成功消息显示
                        });
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"应用美化操作失败: {actionKey}", ex);
                            
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // TODO: 实现Snackbar错误消息显示
                        });
                    }
                }
                else
                {
                    // 对于其他美化操作，通过 WindowsOptimizationService 执行
                    await ExecuteAsync(
                        ct => _windowsOptimizationService.ExecuteActionsAsync([actionKey], ct),
                        "WindowsOptimizationPage_ApplySelected_Success",
                        "WindowsOptimizationPage_ApplySelected_Error",
                        interactionScope);
                }
                
                // 延迟状态刷新以给操作足够时间完成
                // Shell注册需要重启资源管理器，可能需要几秒钟
                var delay = actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase) ? 3000 : 2000;
                await Task.Delay(delay);
                
                // 清除缓存以确保刷新状态时使用最新的实际状态
                if (actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
                {
                    LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.ClearInstallationStatusCache();
                }
                
                // 刷新操作状态
                await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                
                // 刷新美化状态
                _ = RefreshBeautificationStatusAsync();
            }
            else
            {
                // 对于非美化操作，检查操作是否已应用
                var applied = await _windowsOptimizationService.TryGetActionAppliedAsync(actionKey, CancellationToken.None).ConfigureAwait(false);
                
                // 如果未应用，立即执行应用操作
                if (applied.HasValue && !applied.Value)
                {
                    await ExecuteAsync(
                        ct => _windowsOptimizationService.ExecuteActionsAsync([actionKey], ct),
                        "WindowsOptimizationPage_ApplySelected_Success",
                        "WindowsOptimizationPage_ApplySelected_Error",
                        interactionScope);
                        
                    // 延迟状态刷新以给操作足够时间完成
                    await Task.Delay(2000);
                    
                    // 刷新操作状态
                    await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"处理选中操作失败: {actionKey}", ex);
        }
    }
    
    private async Task HandleActionUncheckedAsync(string actionKey)
    {
        try
        {
            // 对于右键美化，当用户取消勾选时立即执行卸载操作（实时应用）
            // 移除状态检查，始终执行卸载命令
            if (actionKey.StartsWith("beautify.contextMenu", StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"用户取消勾选美化操作 {actionKey}，执行卸载命令");
                    
                // 直接执行卸载命令，始终执行无论当前状态如何
                var shellDll = LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.GetNilesoftShellDllPath();
                if (!string.IsNullOrWhiteSpace(shellDll))
                {
                    await ExecuteAsync(
                        async ct =>
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"卸载 Nilesoft Shell");
                                
                            await Task.Run(() =>
                            {
                                try
                                {
                                    var process = new System.Diagnostics.Process
                                    {
                                        StartInfo = new System.Diagnostics.ProcessStartInfo
                                        {
                                            FileName = "cmd.exe",
                                            Arguments = $"/c regsvr32.exe /s /u \"{shellDll}\"",
                                            UseShellExecute = false,
                                            CreateNoWindow = true
                                        }
                                    };
                                    process.Start();
                                    process.WaitForExit();
                                }
                                catch (Exception ex)
                                {
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"卸载 Nilesoft Shell 失败: {ex.Message}", ex);
                                    throw;
                                }
                            });
                        },
                        "WindowsOptimizationPage_ApplySelected_Success",
                        "WindowsOptimizationPage_ApplySelected_Error",
                        InteractionScope.Beautification);
                        
                    // 卸载操作完成后，清除缓存和注册表中的安装状态值
                    // 这确保下次检查时不会从注册表中读取旧的已安装状态
                    LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.ClearInstallationStatusCache();
                    LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.ClearRegistryInstallationStatus();
                    
                    // 将操作添加到用户取消列表，防止状态刷新时自动勾选
                    if (!_userUncheckedActions.Contains(actionKey))
                    {
                        _userUncheckedActions.Add(actionKey);
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"已添加 {actionKey} 到取消列表以防止自动勾选");
                    }
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Shell.exe 未找到，无法执行卸载命令");
                }
                
                // 延迟状态刷新以给shell卸载操作足够时间完成
                // 必须在检查状态前清除缓存，否则将使用旧的缓存值
                LenovoLegionToolkit.Plugins.ShellIntegration.Services.NilesoftShellHelper.ClearInstallationStatusCache();
                await Task.Delay(3000);
                
                // 刷新操作状态
                await RefreshActionStatesAsync(skipUserInteractionCheck: true);
                
                // 刷新美化状态
                _ = RefreshBeautificationStatusAsync();
            }
            else
            {
                // 对于非美化操作，通过 WindowsOptimizationService 执行取消操作
                var interactionScope = actionKey.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase)
                    ? InteractionScope.Cleanup
                    : InteractionScope.Optimization;
                    
                await ExecuteAsync(
                    ct => _windowsOptimizationService.ExecuteActionsAsync([actionKey], ct),
                    "WindowsOptimizationPage_ApplySelected_Success",
                    "WindowsOptimizationPage_ApplySelected_Error",
                    interactionScope);
                    
                // 延迟状态刷新
                await Task.Delay(2000);
                
                // 刷新操作状态
                await RefreshActionStatesAsync(skipUserInteractionCheck: true);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"处理取消勾选操作失败: {actionKey}", ex);
        }
    }
    
    private void ShowOperationIndicator(bool show)
    {
        // TODO: 显示或隐藏操作指示器
    }
    
    #endregion
    
    #region 嵌套 ViewModel 类占位符
    
    public class OptimizationCategoryViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private bool _isExpanded = false;
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

        public IEnumerable<string> SelectedActionKeys => Actions.Where(action => action.IsSelected).Select(action => action.Key);

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
        private bool _isSelected;

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
        public object? Tag { get; set; }

        public bool IsEnabled
        {
            get
            {
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

                return _isSelected;
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
                    if (_isSelected == value)
                        return;

                    if (!value && Tag is SelectedDriverPackageViewModel driverPackage)
                    {
                        if (driverPackage.IsCompleted)
                        {
                            _isSelected = true;
                            OnPropertyChanged(nameof(IsSelected));
                            return;
                        }

                        if (driverPackage._sourcePackageControl != null)
                        {
                            driverPackage._sourcePackageControl.IsSelected = false;
                        }
                    }
                    
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SourceAction_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptimizationActionViewModel.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        public void Dispose()
        {
            if (_sourceAction is not null)
                _sourceAction.PropertyChanged -= SourceAction_PropertyChanged;
        }

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public class CustomCleanupRuleViewModel : INotifyPropertyChanged
    {
        private string _directoryPath = string.Empty;
        private bool _recursive = false;

        public CustomCleanupRuleViewModel(string directoryPath, IEnumerable<string> extensions, bool recursive)
        {
            DirectoryPath = directoryPath;
            Extensions = new ObservableCollection<string>(extensions);
            Recursive = recursive;
        }

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

        public ObservableCollection<string> Extensions { get; }

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
                ? GetResource("CustomCleanupRule_NoExtensions") ?? "No extensions specified"
                : string.Join(", ", Extensions);

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyExtensionsChanged() => OnPropertyChanged(nameof(ExtensionsDisplay));

        public CustomCleanupRule ToModel() => new()
        {
            DirectoryPath = DirectoryPath,
            Recursive = Recursive,
            Extensions = Extensions.ToList()
        };

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public class SelectedDriverPackageViewModel : INotifyPropertyChanged, IDisposable
    {
        internal readonly Controls.Packages.PackageControl? _sourcePackageControl;

        public SelectedDriverPackageViewModel(
            string packageId,
            string title,
            string description,
            string category,
            Controls.Packages.PackageControl sourcePackageControl)
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            if (_sourcePackageControl is not null)
                _sourcePackageControl.PropertyChanged -= SourcePackageControl_PropertyChanged;
        }

        private void SourcePackageControl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Controls.Packages.PackageControl.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
            else if (e.PropertyName == nameof(Controls.Packages.PackageControl.Status))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsCompleted));

                if (_sourcePackageControl != null && _sourcePackageControl.Status == Controls.Packages.PackageControl.PackageStatus.Completed)
                {
                    _sourcePackageControl.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public class DriverDownloadProgressReporter
    {
        private readonly Action<long, long> _progressCallback;
        private long _totalBytes;
        private long _downloadedBytes;

        public DriverDownloadProgressReporter(Action<long, long> progressCallback)
        {
            _progressCallback = progressCallback;
        }

        public void ReportTotalBytes(long totalBytes)
        {
            _totalBytes = totalBytes;
            UpdateProgress();
        }

        public void ReportDownloadedBytes(long downloadedBytes)
        {
            _downloadedBytes = downloadedBytes;
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            _progressCallback(_downloadedBytes, _totalBytes);
        }
    }
    
    #endregion
}