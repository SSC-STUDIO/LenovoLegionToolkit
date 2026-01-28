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
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Windows.Utils;
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

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage : INotifyPropertyChanged
{
    private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly PackageDownloaderSettings _packageDownloaderSettings = IoCContainer.Resolve<PackageDownloaderSettings>();
    private readonly PackageDownloaderFactory _packageDownloaderFactory = IoCContainer.Resolve<PackageDownloaderFactory>();
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
    private enum PageMode
    {
        Optimization,
        Cleanup,
        DriverDownload
    }
    
    [Flags]
    private enum InteractionScope
    {
        Optimization = 1,
        Cleanup = 2,
        All = Optimization | Cleanup
    }
    
    private PageMode _currentMode = PageMode.Optimization;
    private bool _isBusy;
    private SelectedActionsWindow? _selectedActionsWindow;
    private string _selectedActionsSummaryFormat = "{0}";
    private string _selectedActionsEmptyText = string.Empty;
    private OptimizationCategoryViewModel? _selectedCategory;
    private OptimizationCategoryViewModel? _lastOptimizationCategory;
    private OptimizationCategoryViewModel? _lastCleanupCategory;
    private bool _isLoadingCustomCleanupRules;
    private long _estimatedCleanupSize;
    private bool _isCalculatingSize;
    private CancellationTokenSource? _sizeCalculationCts;
    private string _currentOperationText = string.Empty;
    private string _currentDeletingFile = string.Empty;
    private string _runCleanupButtonText = string.Empty;
    private bool _hasInitializedCleanupMode;
    
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
    public ObservableCollection<SelectedActionViewModel> SelectedOptimizationActions { get; } = [];
    public ObservableCollection<SelectedActionViewModel> SelectedCleanupActions { get; } = [];
    public ObservableCollection<SelectedDriverPackageViewModel> SelectedDriverPackages { get; } = [];
    public ObservableCollection<CustomCleanupRuleViewModel> CustomCleanupRules { get; } = [];
    
    public ObservableCollection<SelectedActionViewModel> VisibleSelectedActions => _currentMode switch
    {
        PageMode.Cleanup => SelectedCleanupActions,
        PageMode.Optimization => SelectedOptimizationActions,
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
    public bool IsDriverDownloadMode => _currentMode == PageMode.DriverDownload;

    
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
        _ => OptimizationCategories
    };
    private bool _isRefreshingStates = false;
    private readonly HashSet<string> _userUncheckedActions = new(StringComparer.OrdinalIgnoreCase);
    private bool _isUserInteracting;
    private DateTime _lastUserInteraction = DateTime.MinValue;
    private System.Windows.Threading.DispatcherTimer? _actionStateRefreshTimer;
    private DateTime _lastActionItemClickTime = DateTime.MinValue;
    private string? _lastActionItemKey;
    private ActionDetailsWindow? _actionDetailsWindow;
    
    private bool _optimizationInteractionEnabled = true;
    private bool _cleanupInteractionEnabled = true;
    
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

    private static void UpdateCollection<T>(ObservableCollection<T> existing, List<T> updated)
    {
        if (existing.Count == updated.Count && existing.SequenceEqual(updated))
            return;
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
        while (existing.Count > updated.Count)
        {
            var lastIndex = existing.Count - 1;
            if (existing[lastIndex] is IDisposable disposable)
                disposable.Dispose();
            existing.RemoveAt(lastIndex);
        }
    }
    
    private void ApplyInteractionState()
    {
        var optimizationEnabled = _optimizationInteractionEnabled;
        var cleanupEnabled = _cleanupInteractionEnabled;
        
        var primaryButtonsEnabled = _currentMode switch
        {
            PageMode.Cleanup => cleanupEnabled,
            PageMode.Optimization => optimizationEnabled,
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
                PageMode.Optimization => optimizationEnabled,
                _ => optimizationEnabled
            };
            categoriesList.IsEnabled = listEnabled;
        }
    }
    
    private void ToggleInteraction(bool isEnabled, InteractionScope scope)
    {
        if (scope.HasFlag(InteractionScope.Optimization))
            _optimizationInteractionEnabled = isEnabled;
            
        if (scope.HasFlag(InteractionScope.Cleanup))
            _cleanupInteractionEnabled = isEnabled;
            
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
            
            if (isCleanupCategory && scope.HasFlag(InteractionScope.Cleanup))
                yield return category;
            else if (!isCleanupCategory && scope.HasFlag(InteractionScope.Optimization))
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
    
    
    private void WindowsOptimizationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _actionStateRefreshTimer?.Stop();
        _actionDetailsWindow?.Close();
    }
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
    
    private void UpdateCleanupControlsState()
    {
        if (_addCustomCleanupRuleButton != null)
            _addCustomCleanupRuleButton.IsEnabled = !IsBusy;

        if (_clearCustomCleanupRulesButton != null)
            _clearCustomCleanupRulesButton.IsEnabled = !IsBusy && CustomCleanupRules.Count > 0;
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
            if (HasSelectedActions)
                _ = UpdateEstimatedCleanupSizeAsync();
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

    private void SaveCustomCleanupRules()
    {
        var rules = CustomCleanupRules.Select(rule => rule.ToModel()).ToList();
        _applicationSettings.Store.CustomCleanupRules = rules;
        _applicationSettings.SynchronizeStore();
    }
    
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
            await Task.Delay(300, cancellationToken).ConfigureAwait(false);
            var actionKeys = SelectedCleanupActions.Select(a => a.ActionKey).ToList();
            if (actionKeys.Count == 0)
            {
                EstimatedCleanupSize = 0;
                IsCalculatingSize = false;
                return;
            }
            var size = await _windowsOptimizationService.EstimateCleanupSizeAsync(actionKeys, cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
                EstimatedCleanupSize = size;
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
                IsCalculatingSize = false;
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
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
                         if (_isRefreshingStates)
                             return;

                         _lastUserInteraction = DateTime.Now;
                         _isUserInteracting = true;

                         _ = Dispatcher.InvokeAsync(async () =>
                         {
                             if (!actionVm.IsSelected)
                             {
                                 _userUncheckedActions.Add(actionVm.Key);
                                 await HandleActionUncheckedAsync(actionVm.Key);
                             }
                             else
                             {
                                 _userUncheckedActions.Remove(actionVm.Key);
                                 await HandleActionCheckedAsync(actionVm.Key);
                             }
                         });
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
            else
                OptimizationCategories.Add(categoryVm);
        }
        
        OnPropertyChanged(nameof(ActiveCategories));
        SelectedCategory = ActiveCategories.FirstOrDefault();
        RefreshExpandCollapseText();
    }
    
    private void Category_SelectionChanged(object? sender, EventArgs e)
    {
        _lastUserInteraction = DateTime.Now;
        _isUserInteracting = true;
        UpdateSelectedActions();
        OnPropertyChanged(nameof(SelectedActionsSummary));
        Task.Delay(3000).ContinueWith(_ => _isUserInteracting = false);
    }
    
    private void UpdateSelectedActions()
    {
        var newOptimizationActions = new List<SelectedActionViewModel>();
        var newCleanupActions = new List<SelectedActionViewModel>();

        foreach (var category in Categories)
        {
            List<SelectedActionViewModel> target;
            if (category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
                target = newCleanupActions;
            else
                target = newOptimizationActions;

            foreach (var action in category.Actions.Where(action => action.IsEnabled && action.IsSelected))
                target.Add(new SelectedActionViewModel(category.Key, category.Title, action.Key, action.Title, action.Description, action));
        }

        UpdateCollection(SelectedOptimizationActions, newOptimizationActions);
        UpdateCollection(SelectedCleanupActions, newCleanupActions);

        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));

        if (_currentMode == PageMode.Cleanup)
        {
            if (HasSelectedActions)
                _ = UpdateEstimatedCleanupSizeAsync();
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
    
    private async Task InitializeActionStatesAsync()
    {
        await RefreshActionStatesAsync(skipUserInteractionCheck: true);
        StartActionStateMonitoring();
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
        InitializeDriverDownloadPage();
    }
    
    private async void InitializeDriverDownloadPage()
    {
        _isInitializingDriverDownload = true;
        try
        {
            if (_driverOsComboBox != null && _driverOsComboBox.Items.Count == 0)
                _driverOsComboBox.SetItems(Enum.GetValues<OS>(), OSExtensions.GetCurrent(), os => os.GetDisplayName());

            if (_driverMachineTypeTextBox != null && string.IsNullOrWhiteSpace(_driverMachineTypeTextBox.Text))
            {
                try
                {
                    var machineInfo = await Compatibility.GetMachineInformationAsync();
                    _driverMachineTypeTextBox.Text = machineInfo.MachineType;
                }
                catch
                {
                }
            }

            if (_driverDownloadToText != null && string.IsNullOrWhiteSpace(_driverDownloadToText.Text))
            {
                var downloadsFolder = KnownFolders.GetPath(KnownFolder.Downloads);
                _driverDownloadToText.Text = Directory.Exists(_packageDownloaderSettings.Store.DownloadPath)
                    ? _packageDownloaderSettings.Store.DownloadPath
                    : downloadsFolder;
            }

            if (_driverSourcePrimaryRadio != null && _driverSourcePrimaryRadio.Tag == null)
                _driverSourcePrimaryRadio.Tag = PackageDownloaderFactory.Type.Vantage;
            if (_driverSourceSecondaryRadio != null && _driverSourceSecondaryRadio.Tag == null)
                _driverSourceSecondaryRadio.Tag = PackageDownloaderFactory.Type.PCSupport;

            if (_driverSearchControlsGrid != null)
                _driverSearchControlsGrid.Visibility = Visibility.Collapsed;
            if (_driverInfoBar != null)
                _driverInfoBar.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _isInitializingDriverDownload = false;
        }
    }
    

    
    private void SetMode(PageMode mode)
    {
        var modeChanged = _currentMode != mode;
        _currentMode = mode;

        if (modeChanged)
        {
            OnPropertyChanged(nameof(IsCleanupMode));
            OnPropertyChanged(nameof(IsDriverDownloadMode));
            OnPropertyChanged(nameof(ActiveCategories));
            OnPropertyChanged(nameof(VisibleSelectedActions));
            OnPropertyChanged(nameof(HasSelectedActions));
            OnPropertyChanged(nameof(SelectedActionsSummary));

            if (mode == PageMode.Cleanup)
            {
                if (!_hasInitializedCleanupMode)
                {
                    _hasInitializedCleanupMode = true;
                    var activeCategories = ActiveCategories.ToList();
                    SelectRecommended(activeCategories);
                    UpdateSelectedActions();
                }
                _ = UpdateEstimatedCleanupSizeAsync();
            }
            else if (mode == PageMode.DriverDownload)
            {
                ApplyInteractionState();
            }
            else
            {
                StopDriverRetryTimer();
            }

            if (mode == PageMode.Optimization)
            {
                EstimatedCleanupSize = 0;
            }
            else
            {
                EstimatedCleanupSize = 0;
            }
        }
        else
        {
            OnPropertyChanged(nameof(ActiveCategories));
        }

        if (mode != PageMode.DriverDownload)
        {
            var activeCategoriesList = ActiveCategories.ToList();
            var preferredCategory = mode switch
            {
                PageMode.Cleanup => _lastCleanupCategory,
                PageMode.Optimization => _lastOptimizationCategory,
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
    
    private void SelectedActionsButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedActionsWindow?.Close();

        if (_currentMode == PageMode.DriverDownload)
        {
            var driverPackages = new ObservableCollection<SelectedActionViewModel>();
            foreach (var dp in SelectedDriverPackages)
            {
                var viewModel = new SelectedActionViewModel(
                    dp.Category,
                    dp.Category,
                    dp.PackageId,
                    dp.Title,
                    $"{dp.Description}{(dp.IsCompleted ? " [已完成]" : string.Empty)}",
                    null!);
                viewModel.Tag = dp;
                viewModel.IsSelected = true;
                driverPackages.Add(viewModel);
            }

            _selectedActionsWindow = new Windows.Utils.SelectedActionsWindow(
                driverPackages,
                Resource.WindowsOptimizationPage_SelectedActions_Empty ?? string.Empty)
            {
                Owner = Window.GetWindow(this)
            };
        }
        else
        {
            _selectedActionsWindow = new Windows.Utils.SelectedActionsWindow(VisibleSelectedActions, SelectedActionsEmptyText)
            {
                Owner = Window.GetWindow(this)
            };
        }

        _selectedActionsWindow.Closed += (s, args) => _selectedActionsWindow = null;
        _selectedActionsWindow.Show();
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
    }


    private void OpenActionDetailsWindow(string actionKey)
    {
        try
        {
            _actionDetailsWindow?.Close();

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
            if (sender is not System.Windows.FrameworkElement element)
                return;
            if (element.DataContext is not OptimizationActionViewModel actionViewModel)
                return;

            var now = DateTime.Now;
            var isDoubleClick = _lastActionItemKey == actionViewModel.Key &&
                                (now - _lastActionItemClickTime).TotalMilliseconds < 500;
            _lastActionItemClickTime = now;
            _lastActionItemKey = actionViewModel.Key;

            if (isDoubleClick)
            {
                if (actionViewModel.Key.StartsWith("explorer.", StringComparison.OrdinalIgnoreCase) ||
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
        _isRefreshingStates = true;
        try
        {
            var allActions = categories.SelectMany(c => c.Actions);
            foreach (var action in allActions.Where(a => a.Recommended))
                _userUncheckedActions.Remove(action.Key);
            foreach (var category in categories)
                category.SelectRecommended();
            UpdateSelectedActions();
        }
        finally
        {
            _isRefreshingStates = false;
        }
    }
    
    private void DriverSelectRecommendedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverPackagesStackPanel?.Children == null)
            return;

        foreach (var child in _driverPackagesStackPanel.Children.OfType<PackageControl>())
        {
            if (child.IsRecommended)
            {
                child.IsSelected = true;
            }
        }
    }
    
    private void DriverPauseAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverPackagesStackPanel?.Children == null)
            return;

        foreach (var child in _driverPackagesStackPanel.Children.OfType<PackageControl>())
        {
            if (child.Status == PackageControl.PackageStatus.Downloading ||
                child.Status == PackageControl.PackageStatus.Installing)
                child.IsSelected = false;
        }
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
    
    private async void DriverSearchButton_Click(object sender, RoutedEventArgs e)
    {
        await DriverDownloadPackagesButton_Click(sender, e);
    }
    
    private async Task DriverDownloadPackagesButton_Click(object sender, RoutedEventArgs e)
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

// Ensure InfoBar is always visible
            if (_driverInfoBar != null)
            {
                _driverInfoBar.IsOpen = true;
                _driverInfoBar.Visibility = Visibility.Visible;
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
                // Ensure _driverLoader is visible so users can see filter and sort controls
                if (_driverLoader != null)
                {
                    _driverLoader.Visibility = Visibility.Visible;
                }
                await SnackbarHelper.ShowAsync(Resource.PackagesPage_DownloadFailed_Title,
                    Resource.PackagesPage_DownloadFailed_Message);
                return;
            }

            // Show loading indicator
            if (_driverLoadingIndicator != null)
                _driverLoadingIndicator.Visibility = Visibility.Visible;

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
                // Both sources support "show updates only" feature, always show checkbox
                _driverOnlyShowUpdatesCheckBox.Visibility = Visibility.Visible;
                // Set default checked state based on source type
                if (packageDownloaderType == PackageDownloaderFactory.Type.Vantage)
                {
                    _driverOnlyShowUpdatesCheckBox.IsChecked = _packageDownloaderSettings.Store.OnlyShowUpdates;
                }
                else
                {
                    // Secondary source defaults to not checking "show updates only"
                    _driverOnlyShowUpdatesCheckBox.IsChecked = false;
                }
            }

            _driverPackageDownloader = _packageDownloaderFactory.GetInstance(packageDownloaderType);
            var packages = await _driverPackageDownloader.GetPackagesAsync(machineType, os, new DriverDownloadProgressReporter(this), token);

            _driverPackages = packages;

            DriverReload();

            // Stop auto-retry timer (if running)
            StopDriverRetryTimer();

            // Hide loading indicator
            if (_driverLoadingIndicator != null)
                _driverLoadingIndicator.Visibility = Visibility.Collapsed;

            // Show search controls after loading completes
            if (_driverSearchControlsGrid != null)
            {
                _driverSearchControlsGrid.Visibility = Visibility.Visible;
            }

            // Update button states after loading completes
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
// Only hide loading indicator if it's not a network error
            // On network errors, loading indicator remains visible and shows "network problem"
            if (!errorOccurred)
            {
                if (_driverLoadingIndicator != null)
                    _driverLoadingIndicator.Visibility = Visibility.Collapsed;
            }

            if (errorOccurred)
            {
                if (_driverPackagesStackPanel != null)
                    _driverPackagesStackPanel.Children.Clear();
// Don't hide _driverLoader on error, keep visible to show error status or allow user retry
                // Ensure _driverLoader is always visible so users can see error messages or retry
                if (_driverLoader != null)
                {
                    _driverLoader.Visibility = Visibility.Visible;
                }
            }
        }
    }
    
    private async void DriverSourceRadio_Checked(object sender, RoutedEventArgs e)
    {
        // If initializing, don't trigger refresh
        if (_isInitializingDriverDownload)
            return;

        // Check if machine type and operating system are set
        var machineType = _driverMachineTypeTextBox?.Text.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(machineType) || machineType.Length != 4 ||
            _driverOsComboBox == null || !_driverOsComboBox.TryGetSelectedItem<OS>(out _))
        {
            return;
        }

        // If currently scanning driver packages, cancel current scan and restart
        if (_driverGetPackagesTokenSource != null && !_driverGetPackagesTokenSource.Token.IsCancellationRequested)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Cancelling current driver package scan due to source change...");

            // Cancel current scan
            await _driverGetPackagesTokenSource.CancelAsync();

            // Wait a short time to ensure cancellation operation completes
            await Task.Delay(100);
        }

        // 确保 InfoBar 始终可见
        if (_driverInfoBar != null)
        {
            _driverInfoBar.IsOpen = true;
        }

        // Automatically refresh driver package list when switching sources (regardless of whether previously loaded)
        await DriverDownloadPackagesButton_Click(this, new RoutedEventArgs());
    }
    
    private void DriverDownloadToText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_driverDownloadToText == null)
            return;

        var location = _driverDownloadToText.Text.Trim();
        if (!string.IsNullOrWhiteSpace(location) && Directory.Exists(location))
        {
            _packageDownloaderSettings.Store.DownloadPath = location;
            _packageDownloaderSettings.SynchronizeStore();
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
    
    private void DriverOpenDownloadToButton_Click(object sender, RoutedEventArgs e)
    {
        var location = GetDriverDownloadLocation();
        if (Directory.Exists(location))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = location,
                UseShellExecute = true
            });
        }
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
    
    private void DriverSortingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_driverPackages is null)
            return;

        if (_driverPackagesStackPanel != null)
            _driverPackagesStackPanel.Children.Clear();
        if (_driverScrollViewer != null)
            _driverScrollViewer.ScrollToHome();

        DriverReload();
    }
    
    // 清理相关事件处理程序
    private async void RunCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedCleanupActions = new List<(string CategoryKey, string CategoryTitle, string ActionKey, string ActionTitle, string Description)>();
        foreach (var category in CleanupCategories)
        {
            foreach (var action in category.Actions.Where(a => a.IsEnabled && a.IsSelected))
                selectedCleanupActions.Add((category.Key, category.Title, action.Key, action.Title, action.Description));
        }

        var selectedKeys = selectedCleanupActions
            .Select(a => a.ActionKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedKeys.Count == 0)
        {
            await SnackbarHelper.ShowAsync(
                Resource.SettingsPage_WindowsOptimization_Title,
                GetResource("WindowsOptimizationPage_Cleanup_NoSelection_Warning") ?? "Please select at least one cleanup option before executing cleanup operations.",
                SnackbarType.Warning);
            await RefreshActionStatesAsync(skipUserInteractionCheck: true);
            return;
        }

        await ExecuteAsync(
            async ct =>
            {
                var actionsByKey = WindowsOptimizationService.GetCategories()
                    .SelectMany(c => c.Actions)
                    .ToDictionary(a => a.Key, a => a, StringComparer.OrdinalIgnoreCase);
                var missingKeys = selectedKeys.Where(k => !actionsByKey.ContainsKey(k)).ToList();
                if (missingKeys.Any())
                    throw new InvalidOperationException($"Unable to find the following actions: {string.Join(", ", missingKeys)}");

                var actionsInOrder = selectedCleanupActions
                    .Where(a => selectedKeys.Contains(a.ActionKey, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                long totalFreedBytes = 0;
                var swOverall = Stopwatch.StartNew();
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
                    try { sizeBefore = await _windowsOptimizationService.EstimateActionSizeAsync(action.ActionKey, ct).ConfigureAwait(false); } catch { }

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        if (action.ActionKey.Equals(WindowsOptimizationService.CustomCleanupActionKey, StringComparison.OrdinalIgnoreCase))
                            await ExecuteCustomCleanupWithProgressAsync(ct);
                        else
                            await _windowsOptimizationService.ExecuteActionsAsync([action.ActionKey], ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        sw.Stop();
                    }

                    await Task.Delay(500, ct).ConfigureAwait(false);

                    long sizeAfter = 0;
                    try { sizeAfter = await _windowsOptimizationService.EstimateActionSizeAsync(action.ActionKey, ct).ConfigureAwait(false); } catch { }

                    var freed = Math.Max(0, sizeBefore - sizeAfter);
                    totalFreedBytes += freed;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        CurrentOperationText = $"{action.ActionTitle} ✓ {FormatBytes(freed)} in {sw.Elapsed.TotalSeconds:0.0}s";
                        CurrentDeletingFile = string.Empty;
                    });
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
                    RunCleanupButtonText = string.Empty;
                });
            },
            Resource.SettingsPage_WindowsOptimization_Cleanup_Success,
            Resource.SettingsPage_WindowsOptimization_Cleanup_Error,
            InteractionScope.Cleanup);

        try
        {
            EstimatedCleanupSize = 0;
            await UpdateEstimatedCleanupSizeAsync();
        }
        catch { }
    }

    private async Task ExecuteCustomCleanupWithProgressAsync(CancellationToken cancellationToken)
    {
        var rules = _applicationSettings.Store.CustomCleanupRules ?? new List<CustomCleanupRule>();

        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(rule.DirectoryPath))
                continue;

            var directoryPath = Environment.ExpandEnvironmentVariables(rule.DirectoryPath.Trim());
            if (!Directory.Exists(directoryPath))
                continue;

            var normalizedExtensions = (rule.Extensions ?? [])
                .Select(NormalizeExtension)
                .Where(extension => !string.IsNullOrEmpty(extension))
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
                    }
                    catch
                    {
                        continue;
                    }

                    if (!extensionsSet.Contains(extension))
                        continue;

                    await Dispatcher.InvokeAsync(() => CurrentDeletingFile = file);

                    try
                    {
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

        await Dispatcher.InvokeAsync(() => CurrentDeletingFile = string.Empty);
    }

    private static string NormalizeExtension(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : "." + trimmed;
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
    
    private void ClearCustomCleanupRulesButton_Click(object sender, RoutedEventArgs e)
    {
        if (CustomCleanupRules.Count == 0)
            return;
        foreach (var rule in CustomCleanupRules.ToList())
            DetachRuleEvents(rule);
        CustomCleanupRules.Clear();
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
            if (scope.HasFlag(InteractionScope.Cleanup))
                RunCleanupButtonText = string.Format(Resource.WindowsOptimizationPage_RunCleanupButtonText_Format, 0);
            ShowOperationIndicator(true);
            await operation(CancellationToken.None);
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, successMessage, SnackbarType.Success));
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, Resource.WindowsOptimizationPage_OperationCancelled, SnackbarType.Warning));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"操作失败: {ex.Message}", ex);
            var detailedError = $"{errorMessage}\n{string.Format(Resource.WindowsOptimizationPage_ErrorDetails, ex.Message)}";
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, detailedError, SnackbarType.Error));
        }
        finally
        {
            IsBusy = false;
            ToggleInteraction(true, scope);
            if (scope.HasFlag(InteractionScope.Cleanup))
                RunCleanupButtonText = string.Empty;
            ShowOperationIndicator(false);
        }
    }
    
    private async Task RefreshActionStatesAsync(bool skipUserInteractionCheck = false)
    {
        if (!skipUserInteractionCheck && (_isUserInteracting || (DateTime.Now - _lastUserInteraction).TotalSeconds < 5))
            return;

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
                        var timeSinceLastInteraction = (DateTime.Now - _lastUserInteraction).TotalSeconds;
                        var isRecentInteraction = timeSinceLastInteraction < 10;

                        if (_userUncheckedActions.Contains(action.Key) && applied.Value)
                        {
                            if (!applied.Value)
                            {
                                _userUncheckedActions.Remove(action.Key);
                                action.IsSelected = false;
                            }
                            else
                            {
                                if (action.IsSelected)
                                    action.IsSelected = false;
                            }
                            return;
                        }

                        if (action.IsSelected != applied.Value)
                        {
                            if (isRecentInteraction && action.IsSelected && !applied.Value)
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"跳过 {action.Key} 的状态更新：用户选中但尚未应用（操作可能正在进行中）");
                            }
                            else
                            {
                                action.IsSelected = applied.Value;
                                if (!applied.Value && _userUncheckedActions.Contains(action.Key))
                                    _userUncheckedActions.Remove(action.Key);
                            }
                        }
                    });
                }
            }
            await Dispatcher.InvokeAsync(UpdateSelectedActions);
        }
        finally
        {
            _isRefreshingStates = false;
        }
    }

    private void StartActionStateMonitoring()
    {
        _actionStateRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _actionStateRefreshTimer.Tick += async (s, e) => await RefreshActionStatesAsync();
        _actionStateRefreshTimer.Start();
    }
    
    private async Task HandleActionCheckedAsync(string actionKey)
    {
        try
        {
            _lastUserInteraction = DateTime.Now;
            var interactionScope = actionKey.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase)
                ? InteractionScope.Cleanup
                : InteractionScope.Optimization;

            // Check if action is applied
            var applied = await _windowsOptimizationService.TryGetActionAppliedAsync(actionKey, CancellationToken.None).ConfigureAwait(false);
            
            // If not applied, execute apply action
            if (applied.HasValue && !applied.Value)
            {
                await ExecuteAsync(
                    ct => _windowsOptimizationService.ExecuteActionsAsync([actionKey], ct),
                    GetResource("WindowsOptimizationPage_ApplySelected_Success"),
                    GetResource("WindowsOptimizationPage_ApplySelected_Error"),
                    interactionScope);
                    
                // Delay refresh
                await Task.Delay(2000);
                
                // Refresh states
                await RefreshActionStatesAsync(skipUserInteractionCheck: true);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to handle selected action: {actionKey}", ex);
        }
        finally
        {
            _isUserInteracting = false;
        }
    }
    
    private async Task HandleActionUncheckedAsync(string actionKey)
    {
        try
        {
            _lastUserInteraction = DateTime.Now;
            
            // Execute cancel action via WindowsOptimizationService
            var interactionScope = actionKey.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase)
                ? InteractionScope.Cleanup
                : InteractionScope.Optimization;
                
            await ExecuteAsync(
                ct => _windowsOptimizationService.ExecuteActionsAsync([actionKey], ct),
                GetResource("WindowsOptimizationPage_ApplySelected_Success"),
                GetResource("WindowsOptimizationPage_ApplySelected_Error"),
                interactionScope);
                
            // Delay refresh
            await Task.Delay(2000);
            
            // Refresh states
            await RefreshActionStatesAsync(skipUserInteractionCheck: true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to handle uncheck action: {actionKey}", ex);
        }
        finally
        {
            _isUserInteracting = false;
        }
    }
    
    private void ShowOperationIndicator(bool show)
    {
    }

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
    
    public class SelectedActionViewModel : ISelectedActionViewModel, IDisposable
    {
        private readonly OptimizationActionViewModel? _sourceAction;
        private bool _isSelected;

        public SelectedActionViewModel(
            string categoryKey,
            string categoryTitle,
            string actionKey,
            string actionTitle,
            string description,
            OptimizationActionViewModel? sourceAction)
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
                    else if (value && Tag is SelectedDriverPackageViewModel driverPackageSelected)
                    {
                        if (driverPackageSelected._sourcePackageControl != null)
                        {
                            driverPackageSelected._sourcePackageControl.IsSelected = true;
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
            Extensions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ExtensionsDisplay));
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
        internal readonly PackageControl? _sourcePackageControl;

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
                        PackageControl.PackageStatus.Downloading => "Downloading",
                        PackageControl.PackageStatus.Installing => "Installing",
                        PackageControl.PackageStatus.Completed => "Completed",
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
            if (e.PropertyName == nameof(PackageControl.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
            else if (e.PropertyName == nameof(PackageControl.Status))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsCompleted));

                if (_sourcePackageControl != null && _sourcePackageControl.Status == PackageControl.PackageStatus.Completed)
                {
                    _sourcePackageControl.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            // Progress reporting can be implemented here if needed
        });
    }

    private bool _isInitializingDriverDownload = false;

    private bool IsDriverDownloadBusy()
    {
        if (_driverPackagesStackPanel?.Children is null)
            return false;

        return _driverPackagesStackPanel.Children
            .OfType<PackageControl>()
            .Any(pc => pc.IsDownloading || pc.Status == PackageControl.PackageStatus.Installing);
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
                    if (control.Status == PackageControl.PackageStatus.Completed)
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

// Initialize selected driver packages list
        UpdateSelectedDriverPackages();

        // Check selected driver packages, hide corresponding controls if completed
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
            var clearHidden = new Wpf.Ui.Controls.Hyperlink
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
    
    private void UpdateSelectedDriverPackages()
    {
        if (_driverPackagesStackPanel?.Children == null)
            return;

        var newSelectedPackages = new List<SelectedDriverPackageViewModel>();
        foreach (var child in _driverPackagesStackPanel.Children.OfType<PackageControl>())
        {
            if (child.IsSelected)
            {
                var existing = SelectedDriverPackages.FirstOrDefault(p => p.PackageId == child.Package.Id);
                if (existing != null)
                {
                    newSelectedPackages.Add(existing);
                }
                else
                {
                    newSelectedPackages.Add(new SelectedDriverPackageViewModel(
                        child.Package.Id,
                        child.Package.Title,
                        child.Package.Description,
                        child.Package.Category,
                        child));
                }
            }
        }

        foreach (var existing in SelectedDriverPackages.ToList())
        {
            if (!newSelectedPackages.Any(p => p.PackageId == existing.PackageId))
            {
                existing.Dispose();
                SelectedDriverPackages.Remove(existing);
            }
        }

        foreach (var newPackage in newSelectedPackages)
        {
            if (!SelectedDriverPackages.Any(p => p.PackageId == newPackage.PackageId))
                SelectedDriverPackages.Add(newPackage);
        }

        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
    }
    
    private void StopDriverRetryTimer()
    {
        if (_driverRetryTimer != null)
        {
            _driverRetryTimer.Stop();
            _driverRetryTimer = null;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Driver retry timer stopped.");
        }
    }
}
