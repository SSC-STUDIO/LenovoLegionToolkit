using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Common;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Windows.Utils;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage : INotifyPropertyChanged
    {
        private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
        private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
        private SelectedActionsWindow? _selectedActionsWindow;

    private bool _isBusy;
    private bool _isCleanupMode;
    private string _selectedActionsSummaryFormat = "{0}";
    private string _selectedActionsEmptyText = string.Empty;
    private OptimizationCategoryViewModel? _selectedCategory;
    private OptimizationCategoryViewModel? _lastOptimizationCategory;
    private OptimizationCategoryViewModel? _lastCleanupCategory;
    private bool _isLoadingCustomCleanupRules;
    private bool _isLoadingAppxPackages;
    private bool _optimizationInteractionEnabled = true;
    private bool _cleanupInteractionEnabled = true;

    [Flags]
    private enum InteractionScope
    {
        Optimization = 1,
        Cleanup = 2,
        All = Optimization | Cleanup
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

    public ObservableCollection<SelectedActionViewModel> SelectedOptimizationActions { get; } = [];

    public ObservableCollection<SelectedActionViewModel> SelectedCleanupActions { get; } = [];

    public ObservableCollection<SelectedActionViewModel> VisibleSelectedActions => _isCleanupMode ? SelectedCleanupActions : SelectedOptimizationActions;

    public ObservableCollection<CustomCleanupRuleViewModel> CustomCleanupRules { get; } = [];
    public ObservableCollection<AppxPackageViewModel> AppxPackages { get; } = [];

    public bool HasSelectedActions => VisibleSelectedActions.Count > 0;

    public string SelectedActionsSummary => string.Format(_selectedActionsSummaryFormat, VisibleSelectedActions.Count);

    public string SelectedActionsEmptyText => _selectedActionsEmptyText;

    public IEnumerable<OptimizationCategoryViewModel> ActiveCategories => _isCleanupMode ? CleanupCategories : OptimizationCategories;

    public OptimizationCategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory == value)
                return;

            _selectedCategory = value;
            if (_isCleanupMode)
                _lastCleanupCategory = value;
            else
                _lastOptimizationCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    public bool IsCleanupMode => _isCleanupMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly AppxPackageDefinition[] AppxPackageDefinitions =
    {
        new("Microsoft.BingNews", "Microsoft News", "News headlines and articles."),
        new("Microsoft.BingWeather", "Weather", "Weather forecasts and alerts."),
        new("Microsoft.BingFinance", "Money", "Finance and stock information."),
        new("Microsoft.BingSports", "Sports", "Sports scores and news."),
        new("Microsoft.GetHelp", "Get Help", "Microsoft support assistant."),
        new("Microsoft.Getstarted", "Tips", "Windows tips and onboarding app."),
        new("Microsoft.MixedReality.Portal", "Mixed Reality Portal", "Mixed reality setup experience."),
        new("Microsoft.Microsoft3DViewer", "3D Viewer", "View and interact with 3D models."),
        new("Microsoft.MicrosoftOfficeHub", "Office Hub", "Office app launcher and shortcuts."),
        new("Microsoft.MicrosoftSolitaireCollection", "Solitaire Collection", "Microsoft card games collection."),
        new("Microsoft.MicrosoftStickyNotes", "Sticky Notes", "Digital sticky notes."),
        new("Microsoft.OneConnect", "Mobile Plans", "Mobile network configuration app."),
        new("Microsoft.Paint3D", "Paint 3D", "3D art creation tool."),
        new("Microsoft.People", "People", "Contacts management app."),
        new("Microsoft.PowerAutomateDesktop", "Power Automate", "Desktop automation tool."),
        new("Microsoft.RemoteDesktop", "Remote Desktop", "Remote desktop client."),
        new("Microsoft.SkypeApp", "Skype", "Skype communications app."),
        new("Microsoft.Whiteboard", "Whiteboard", "Collaborative whiteboard app."),
        new("Microsoft.WindowsFeedbackHub", "Feedback Hub", "Send feedback to Microsoft."),
        new("Microsoft.Xbox.TCUI", "Xbox TCUI", "Xbox social experience components."),
        new("Microsoft.XboxApp", "Xbox Console Companion", "Legacy Xbox companion app."),
        new("Microsoft.XboxGameOverlay", "Xbox Game Overlay", "Overlay components for Xbox experiences."),
        new("Microsoft.XboxGamingOverlay", "Xbox Game Bar", "Xbox Game Bar experience."),
        new("Microsoft.XboxIdentityProvider", "Xbox Identity Provider", "Xbox authentication components."),
        new("Microsoft.XboxSpeechToTextOverlay", "Xbox Speech to Text", "Xbox speech overlay components."),
        new("Microsoft.ZuneMusic", "Groove Music", "Music playback app."),
        new("Microsoft.ZuneVideo", "Movies & TV", "Video playback app."),
        new("Microsoft.YourPhone", "Phone Link", "Link your Android phone."),
        new("Clipchamp.Clipchamp", "Clipchamp", "Microsoft Clipchamp video editor."),
        new("TikTok.TikTok", "TikTok", "TikTok video application."),
        new("SpotifyAB.SpotifyMusic", "Spotify", "Spotify music streaming app."),
        new("Disney.37853FC22B2CE", "Disney+", "Disney+ streaming app."),
        new("Microsoft.549981C3F5F10", "Cortana", "Cortana digital assistant.")
    };

    public WindowsOptimizationPage()
    {
        InitializeComponent();
        DataContext = this;

        CustomCleanupRules.CollectionChanged += CustomCleanupRules_CollectionChanged;
        LoadCustomCleanupRules();
        LoadAppxPackages();
        UpdateCleanupControlsState();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (Categories.Count > 0)
            return;

        InitializeCategories();
        SetMode(false);
        SelectRecommended(Categories);
        UpdateSelectedActions();
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

        _selectedActionsSummaryFormat = GetResource("WindowsOptimizationPage_SelectedActions_Count");
        if (string.IsNullOrWhiteSpace(_selectedActionsSummaryFormat))
            _selectedActionsSummaryFormat = "{0}";

        _selectedActionsEmptyText = GetResource("WindowsOptimizationPage_SelectedActions_Empty");
        OnPropertyChanged(nameof(SelectedActionsEmptyText));

        foreach (var category in _windowsOptimizationService.GetCategories())
        {
            var categoryVm = new OptimizationCategoryViewModel(
                category.Key,
                GetResource(category.TitleResourceKey),
                GetResource(category.DescriptionResourceKey),
                selectionSummaryFormat,
                category.Actions.Select(action => new OptimizationActionViewModel(
                    action.Key,
                    GetResource(action.TitleResourceKey),
                    GetResource(action.DescriptionResourceKey),
                    action.Recommended,
                    recommendedTagText)));

            categoryVm.SelectionChanged += Category_SelectionChanged;
            Categories.Add(categoryVm);

            if (string.Equals(category.Key, WindowsOptimizationService.CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
                CleanupCategories.Add(categoryVm);
            else
                OptimizationCategories.Add(categoryVm);
        }

        OnPropertyChanged(nameof(ActiveCategories));
        RefreshCleanupActionAvailability();
        SelectedCategory = ActiveCategories.FirstOrDefault();
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedKeys = Categories
            .SelectMany(category => category.SelectedActionKeys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedKeys.Count == 0)
        {
            await SnackbarHelper.ShowAsync(Resource.SettingsPage_WindowsOptimization_Title, Resource.WindowsOptimizationPage_EmptySelection_Message, SnackbarType.Warning);
            return;
        }

        await ExecuteAsync(
            ct => _windowsOptimizationService.ExecuteActionsAsync(selectedKeys, ct),
            Resource.WindowsOptimizationPage_ApplySelected_Success,
            Resource.WindowsOptimizationPage_ApplySelected_Error,
            InteractionScope.Optimization);
    }

    private async void RunCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(
            _windowsOptimizationService.RunCleanupAsync,
            Resource.SettingsPage_WindowsOptimization_Cleanup_Success,
            Resource.SettingsPage_WindowsOptimization_Cleanup_Error,
            InteractionScope.Cleanup);
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

        if (IsCleanupMode)
        {
            foreach (var package in AppxPackages)
                package.IsSelected = false;
            SaveAppxPackages();
        }

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
            return;

        IsBusy = true;
        ToggleInteraction(false, scope);

        try
        {
            ShowOperationIndicator(true);
            await operation(CancellationToken.None).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, successMessage, SnackbarType.Success));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Windows optimization action failed. Exception: {ex.Message}", ex);

            await Dispatcher.InvokeAsync(() => SnackbarHelper.Show(Resource.SettingsPage_WindowsOptimization_Title, errorMessage, SnackbarType.Error));
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

        foreach (var category in GetCategoriesForScope(scope))
            category.SetEnabled(isEnabled);

        ApplyInteractionState();
        UpdateCleanupControlsState();
    }

    private IEnumerable<OptimizationCategoryViewModel> GetCategoriesForScope(InteractionScope scope)
    {
        foreach (var category in Categories)
        {
            var isCleanupCategory = string.Equals(
                category.Key,
                WindowsOptimizationService.CleanupCategoryKey,
                StringComparison.OrdinalIgnoreCase);

            if (isCleanupCategory && scope.HasFlag(InteractionScope.Cleanup))
                yield return category;
            else if (!isCleanupCategory && scope.HasFlag(InteractionScope.Optimization))
                yield return category;
        }
    }

    private void ApplyInteractionState()
    {
        var optimizationEnabled = _optimizationInteractionEnabled;
        var cleanupEnabled = _cleanupInteractionEnabled;

        if (_applyButton != null)
            _applyButton.IsEnabled = optimizationEnabled;

        var primaryButtonsEnabled = IsCleanupMode ? cleanupEnabled : optimizationEnabled;

        if (_selectRecommendedButton != null)
            _selectRecommendedButton.IsEnabled = primaryButtonsEnabled;

        if (_clearButton != null)
            _clearButton.IsEnabled = primaryButtonsEnabled;

        if (_runCleanupButton != null)
            _runCleanupButton.IsEnabled = cleanupEnabled;

        if (_categoryList != null)
            _categoryList.IsEnabled = IsCleanupMode ? cleanupEnabled : optimizationEnabled;

        if (_actionList != null)
            _actionList.IsEnabled = IsCleanupMode ? cleanupEnabled : optimizationEnabled;
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

    private void Category_SelectionChanged(object? sender, EventArgs e) => UpdateSelectedActions();

    private void UpdateSelectedActions()
    {
        // 创建新的选定项集合，以便进行比较
        var newOptimizationActions = new List<SelectedActionViewModel>();
        var newCleanupActions = new List<SelectedActionViewModel>();

        foreach (var category in Categories)
        {
            var target = string.Equals(category.Key, WindowsOptimizationService.CleanupCategoryKey, StringComparison.OrdinalIgnoreCase)
                ? newCleanupActions
                : newOptimizationActions;

            foreach (var action in category.Actions.Where(action => action.IsEnabled && action.IsSelected))
                target.Add(new SelectedActionViewModel(category.Key, category.Title, action.Key, action.Title, action.Description, action));
        }

        var appxCategoryTitle = GetResource("WindowsOptimizationPage_AppxManagement_Header");
        foreach (var package in AppxPackages.Where(package => package.IsSelected))
        {
            var selectedAppx = new SelectedActionViewModel(
                WindowsOptimizationService.CleanupCategoryKey,
                appxCategoryTitle,
                $"{WindowsOptimizationService.AppxCleanupActionKey}:{package.PackageId}",
                package.DisplayName,
                package.Description,
                package);

            newCleanupActions.Add(selectedAppx);
        }

        // 增量更新SelectedOptimizationActions集合
        UpdateCollection(SelectedOptimizationActions, newOptimizationActions);
        // 增量更新SelectedCleanupActions集合
        UpdateCollection(SelectedCleanupActions, newCleanupActions);

        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
    }

    private static void UpdateCollection<T>(ObservableCollection<T> existing, List<T> updated)
    {
        // 如果两个集合内容相同，则不进行更新
        if (existing.Count == updated.Count && existing.SequenceEqual(updated))
            return;

        // 使用增量更新而非完全清空重建，以保持滚动条状态
        // 1. 移除不再存在的项目
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

        // 2. 按照 updated 的顺序重新排列 / 插入元素
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

        // 3. 如果 existing 仍然比 updated 长，移除多余的尾部元素
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

    private void LoadAppxPackages()
    {
        _isLoadingAppxPackages = true;
        try
        {
            foreach (var package in AppxPackages.ToList())
                package.PropertyChanged -= AppxPackageViewModel_PropertyChanged;

            AppxPackages.Clear();

            var stored = _applicationSettings.Store.AppxPackagesToRemove ?? new List<string>();
            var selectedSet = new HashSet<string>(stored, StringComparer.OrdinalIgnoreCase);

            foreach (var definition in AppxPackageDefinitions)
            {
                var viewModel = new AppxPackageViewModel(
                    definition.Id,
                    definition.DisplayName,
                    definition.Description,
                    selectedSet.Contains(definition.Id));

                viewModel.PropertyChanged += AppxPackageViewModel_PropertyChanged;
                AppxPackages.Add(viewModel);
            }
        }
        finally
        {
            _isLoadingAppxPackages = false;
        }

        RefreshCleanupActionAvailability();
        UpdateSelectedActions();
    }

    private void SaveAppxPackages()
    {
        var selectedPackages = AppxPackages
            .Where(package => package.IsSelected)
            .Select(package => package.PackageId)
            .ToList();

        _applicationSettings.Store.AppxPackagesToRemove = selectedPackages;
        _applicationSettings.SynchronizeStore();
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
        var hasSelectedAppxPackages = AppxPackages.Any(package => package.IsSelected);

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

            var appxAction = category.Actions.FirstOrDefault(action =>
                string.Equals(action.Key, WindowsOptimizationService.AppxCleanupActionKey, StringComparison.OrdinalIgnoreCase));

            if (appxAction is not null)
            {
                appxAction.IsEnabled = hasSelectedAppxPackages;

                if (!hasSelectedAppxPackages && appxAction.IsSelected)
                    appxAction.IsSelected = false;
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
    }

    private void AppxPackageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppxPackageViewModel.IsSelected))
            OnAppxPackagesChanged();
    }

    private void OnAppxPackagesChanged()
    {
        if (_isLoadingAppxPackages)
            return;

        SaveAppxPackages();
        RefreshCleanupActionAvailability();
        UpdateSelectedActions();
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

    private void SetMode(bool isCleanup)
    {
        var modeChanged = _isCleanupMode != isCleanup;
        _isCleanupMode = isCleanup;

        if (modeChanged)
        {
            OnPropertyChanged(nameof(IsCleanupMode));
            OnPropertyChanged(nameof(ActiveCategories));
        }
        else
        {
            // 即使模式未发生变化，也触发刷新以确保绑定在特殊情况下能够正确更新
            OnPropertyChanged(nameof(ActiveCategories));
        }

        var activeCategories = ActiveCategories.ToList();
        var preferredCategory = isCleanup ? _lastCleanupCategory : _lastOptimizationCategory;
        if (preferredCategory is null || !activeCategories.Contains(preferredCategory))
            preferredCategory = activeCategories.FirstOrDefault();

        SelectedCategory = preferredCategory;
        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
        ApplyInteractionState();
    }

    private void OptimizationNavButton_Checked(object sender, RoutedEventArgs e) => SetMode(false);

    private void CleanupNavButton_Checked(object sender, RoutedEventArgs e) => SetMode(true);

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

    private record AppxPackageDefinition(string Id, string DisplayName, string Description);

    public class AppxPackageViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public AppxPackageViewModel(string packageId, string displayName, string description, bool isSelected)
        {
            PackageId = packageId;
            DisplayName = displayName;
            Description = description;
            _isSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string PackageId { get; }

        public string DisplayName { get; }

        public string Description { get; }

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

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class OptimizationCategoryViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
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
        private readonly AppxPackageViewModel? _sourceAppxPackage;

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

        public SelectedActionViewModel(
            string categoryKey,
            string categoryTitle,
            string actionKey,
            string actionTitle,
            string description,
            AppxPackageViewModel sourceAppxPackage)
        {
            CategoryKey = categoryKey;
            CategoryTitle = categoryTitle;
            ActionKey = actionKey;
            ActionTitle = actionTitle;
            Description = description;
            _sourceAppxPackage = sourceAppxPackage;
            _sourceAppxPackage.PropertyChanged += SourceAppxPackage_PropertyChanged;
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

                if (_sourceAppxPackage is not null)
                    return _sourceAppxPackage.IsSelected;

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
                else if (_sourceAppxPackage is not null)
                {
                    if (_sourceAppxPackage.IsSelected == value)
                        return;

                    _sourceAppxPackage.IsSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public void Dispose()
        {
            if (_sourceAction is not null)
                _sourceAction.PropertyChanged -= SourceAction_PropertyChanged;

            if (_sourceAppxPackage is not null)
                _sourceAppxPackage.PropertyChanged -= SourceAppxPackage_PropertyChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SourceAction_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptimizationActionViewModel.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        private void SourceAppxPackage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppxPackageViewModel.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SelectedActionsButton_Click(object sender, RoutedEventArgs e)
    {
        // 关闭已存在的窗口（如果有）
        _selectedActionsWindow?.Close();

        // 创建并显示弹出窗口
        _selectedActionsWindow = new Windows.Utils.SelectedActionsWindow(VisibleSelectedActions, SelectedActionsEmptyText)
        {
            Owner = Window.GetWindow(this)
        };
        _selectedActionsWindow.Closed += (s, args) => _selectedActionsWindow = null;
        _selectedActionsWindow.Show();
    }
}
