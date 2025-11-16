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
    private bool _isLoadingAppxList;
    private bool _optimizationInteractionEnabled = true;
    private bool _cleanupInteractionEnabled = true;
    private long _estimatedCleanupSize;
    private bool _isCalculatingSize;
    private CancellationTokenSource? _sizeCalculationCts;
    private bool _hasInitializedCleanupMode = false;
        private string _currentOperationText = string.Empty;

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
            if (!_isCleanupMode || EstimatedCleanupSize == 0)
                return string.Empty;

            return string.Format(Resource.WindowsOptimizationPage_EstimatedCleanupSize, FormatBytes(EstimatedCleanupSize));
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

    public string PendingText => GetResource("WindowsOptimizationPage_EstimatedCleanupSize_Pending");

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
        // Don't select recommended by default - let InitializeActionStatesAsync set the checkboxes based on actual system state
        UpdateSelectedActions();
        _ = InitializeActionStatesAsync();
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

            if (category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
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
        var selectedKeys = SelectedCleanupActions
            .Where(a => !string.IsNullOrWhiteSpace(a.CategoryKey) && !a.CategoryKey.Equals("cleanup.appx", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.ActionKey)
            .ToList();

        var selectedAppxPackages = SelectedCleanupActions
            .Where(a => !string.IsNullOrWhiteSpace(a.CategoryKey) && a.CategoryKey.Equals("cleanup.appx", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.ActionKey)
            .ToList();

        if (selectedKeys.Count == 0 && selectedAppxPackages.Count == 0)
        {
            await SnackbarHelper.ShowAsync(Resource.SettingsPage_WindowsOptimization_Title, Resource.WindowsOptimizationPage_EmptySelection_Message, SnackbarType.Warning);
            return;
        }

        await ExecuteAsync(
            async ct =>
            {
                // Save selected Appx packages first
                if (selectedAppxPackages.Count > 0)
                {
                    await Dispatcher.InvokeAsync(() => CurrentOperationText = GetResource("WindowsOptimizationPage_AppxManagement_Running"));
                    var packagesToRemove = AppxPackages
                        .Where(p => selectedAppxPackages.Contains(p.PackageFullName, StringComparer.OrdinalIgnoreCase) ||
                                   selectedAppxPackages.Contains(p.PackageId, StringComparer.OrdinalIgnoreCase))
                        .Where(p => !string.IsNullOrWhiteSpace(p.PackageFullName))
                        .Select(p => p.PackageFullName)
                        .ToList();

                    if (packagesToRemove.Count > 0)
                    {
                        var previousPackages = _applicationSettings.Store.AppxPackagesToRemove ?? new List<string>();
                        _applicationSettings.Store.AppxPackagesToRemove = packagesToRemove;
                        _applicationSettings.SynchronizeStore();

                        // Execute Appx cleanup
                        await _windowsOptimizationService.ExecuteActionsAsync([WindowsOptimizationService.AppxCleanupActionKey], ct).ConfigureAwait(false);

                        // Restore previous packages if needed (for future selections)
                        _applicationSettings.Store.AppxPackagesToRemove = packagesToRemove;
                        _applicationSettings.SynchronizeStore();
                    }
                    await Dispatcher.InvokeAsync(() => CurrentOperationText = string.Empty);
                }

                // Execute other cleanup actions
                if (selectedKeys.Count > 0)
                {
                    // Run one by one to display progress text
                    var actionsInOrder = SelectedCleanupActions
                        .Where(a => !string.IsNullOrWhiteSpace(a.CategoryKey) && !a.CategoryKey.Equals("cleanup.appx", StringComparison.OrdinalIgnoreCase))
                        .Where(a => selectedKeys.Contains(a.ActionKey, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var action in actionsInOrder)
                    {
                        await Dispatcher.InvokeAsync(() =>
                            CurrentOperationText = string.Format(GetResource("WindowsOptimizationPage_RunningStep"), action.ActionTitle));
                        await _windowsOptimizationService.ExecuteActionsAsync([action.ActionKey], ct).ConfigureAwait(false);
                    }
                    await Dispatcher.InvokeAsync(() => CurrentOperationText = string.Empty);
                }
            },
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
            var isCleanupCategory = category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase);

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

        if (_categoriesList != null)
            _categoriesList.IsEnabled = IsCleanupMode ? cleanupEnabled : optimizationEnabled;
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
        // Build fresh selection snapshots for comparison
        var newOptimizationActions = new List<SelectedActionViewModel>();
        var newCleanupActions = new List<SelectedActionViewModel>();

        foreach (var category in Categories)
        {
            var target = category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase)
                ? newCleanupActions
                : newOptimizationActions;

            foreach (var action in category.Actions.Where(action => action.IsEnabled && action.IsSelected))
                target.Add(new SelectedActionViewModel(category.Key, category.Title, action.Key, action.Title, action.Description, action));
        }

        // Add selected Appx packages to cleanup actions if in cleanup mode
        if (_isCleanupMode)
        {
            foreach (var package in AppxPackages.Where(package => package.IsSelected && !string.IsNullOrWhiteSpace(package.DisplayName) && !string.IsNullOrWhiteSpace(package.PackageId)))
            {
                var appxCategoryKey = "cleanup.appx";
                var appxCategoryTitle = GetResource("WindowsOptimizationPage_AppxManagement_Header");
                var actionKey = string.IsNullOrWhiteSpace(package.PackageFullName) ? package.PackageId : package.PackageFullName;
                var actionTitle = package.DisplayName;
                var actionDescription = string.IsNullOrWhiteSpace(package.Description) ? package.PackageId : package.Description;

                var appxViewModel = new SelectedActionViewModel(
                    appxCategoryKey,
                    appxCategoryTitle,
                    actionKey,
                    actionTitle,
                    actionDescription,
                    package);

                newCleanupActions.Add(appxViewModel);
            }
        }

        // Incrementally update SelectedOptimizationActions
        UpdateCollection(SelectedOptimizationActions, newOptimizationActions);
        // Incrementally update SelectedCleanupActions
        UpdateCollection(SelectedCleanupActions, newCleanupActions);

        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));

        if (_isCleanupMode)
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

    private async void LoadAppxPackages()
    {
        if (_isLoadingAppxList)
            return;

        _isLoadingAppxList = true;
        _isLoadingAppxPackages = true;

        try
        {
            foreach (var package in AppxPackages.ToList())
                package.PropertyChanged -= AppxPackageViewModel_PropertyChanged;

            AppxPackages.Clear();

            // Show loading state
            await Dispatcher.InvokeAsync(() =>
            {
                var loadingViewModel = new AppxPackageViewModel("", "", "正在加载应用，请稍候...", "", false);
                AppxPackages.Add(loadingViewModel);
            });

            var stored = _applicationSettings.Store.AppxPackagesToRemove ?? new List<string>();
            var selectedSet = new HashSet<string>(stored, StringComparer.OrdinalIgnoreCase);

            // Get installed Appx packages dynamically
            var packages = await GetInstalledAppxPackagesAsync();

            await Dispatcher.InvokeAsync(() =>
            {
                AppxPackages.Clear();

                if (packages.Count == 0)
                {
                    var emptyViewModel = new AppxPackageViewModel("", "", "未找到可卸载的应用", "", false);
                    AppxPackages.Add(emptyViewModel);
                }
                else
                {
                    foreach (var package in packages.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase))
                    {
                        var isSelected = selectedSet.Contains(package.PackageId) || selectedSet.Contains(package.PackageFullName);
                        var viewModel = new AppxPackageViewModel(
                            package.PackageId,
                            package.PackageFullName,
                            package.DisplayName,
                            package.Description,
                            isSelected);

                        viewModel.PropertyChanged += AppxPackageViewModel_PropertyChanged;
                        AppxPackages.Add(viewModel);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load Appx packages.", ex);

            await Dispatcher.InvokeAsync(() =>
            {
                AppxPackages.Clear();
                var errorViewModel = new AppxPackageViewModel("", "", $"加载失败: {ex.Message}", "", false);
                AppxPackages.Add(errorViewModel);
            });
        }
        finally
        {
            _isLoadingAppxList = false;
            _isLoadingAppxPackages = false;
        }

        RefreshCleanupActionAvailability();
        UpdateSelectedActions();
    }

    private async Task<List<AppxPackageInfo>> GetInstalledAppxPackagesAsync()
    {
        return await Task.Run(() =>
        {
            var packages = new List<AppxPackageInfo>();

            try
            {
                var psScript = "Get-AppxPackage | Where-Object { !$_.IsFramework -and !$_.NonRemovable } | ForEach-Object { [PSCustomObject]@{ PackageId = $_.Name; PackageFullName = $_.PackageFullName; DisplayName = $_.DisplayName; Publisher = $_.Publisher } } | ConvertTo-Json -Compress";

                using var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    try
                    {
                        // Parse JSON output
                        var json = output.Trim();
                        if (json.StartsWith("["))
                        {
                            var packageArray = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(json);
                            if (packageArray != null)
                            {
                                foreach (var item in packageArray)
                                {
                                    var packageId = item.TryGetProperty("PackageId", out var pkgId) ? pkgId.GetString() ?? "" : "";
                                    var packageFullName = item.TryGetProperty("PackageFullName", out var pkgFullName) ? pkgFullName.GetString() ?? "" : "";
                                    var displayName = item.TryGetProperty("DisplayName", out var dispName) ? dispName.GetString() ?? "" : "";
                                    var publisher = item.TryGetProperty("Publisher", out var pub) ? pub.GetString() ?? "" : "";

                                    if (!string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(packageFullName))
                                    {
                                        packages.Add(new AppxPackageInfo
                                        {
                                            PackageId = packageId,
                                            PackageFullName = packageFullName,
                                            DisplayName = string.IsNullOrWhiteSpace(displayName) ? packageId : displayName,
                                            Description = $"Publisher: {publisher}"
                                        });
                                    }
                                }
                            }
                        }
                        else if (json.StartsWith("{"))
                        {
                            // Single package
                            var item = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                            if (item.TryGetProperty("PackageId", out var pkgId) && item.TryGetProperty("PackageFullName", out var pkgFullName))
                            {
                                var packageId = pkgId.GetString() ?? "";
                                var packageFullName = pkgFullName.GetString() ?? "";
                                var displayName = item.TryGetProperty("DisplayName", out var dispName) ? dispName.GetString() ?? "" : "";
                                var publisher = item.TryGetProperty("Publisher", out var pub) ? pub.GetString() ?? "" : "";

                                if (!string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(packageFullName))
                                {
                                    packages.Add(new AppxPackageInfo
                                    {
                                        PackageId = packageId,
                                        PackageFullName = packageFullName,
                                        DisplayName = string.IsNullOrWhiteSpace(displayName) ? packageId : displayName,
                                        Description = $"Publisher: {publisher}"
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to parse Appx packages JSON. [output={output}]", ex);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"PowerShell error while getting Appx packages. [error={error}]");
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to get installed Appx packages.", ex);
            }

            return packages;
        });
    }

    private record AppxPackageInfo
    {
        public string PackageId { get; init; } = string.Empty;
        public string PackageFullName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    private void SaveAppxPackages()
    {
        var selectedPackages = AppxPackages
            .Where(package => package.IsSelected && !string.IsNullOrWhiteSpace(package.PackageFullName))
            .Select(package => package.PackageFullName) // Use PackageFullName for removal
            .ToList();

        // Also save PackageId for backward compatibility
        var selectedPackageIds = AppxPackages
            .Where(package => package.IsSelected && !string.IsNullOrWhiteSpace(package.PackageId))
            .Select(package => package.PackageId)
            .ToList();

        // Store both for compatibility
        _applicationSettings.Store.AppxPackagesToRemove = selectedPackages.Any() ? selectedPackages : selectedPackageIds;
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

            // Appx cleanup action removed - no longer need to update its enabled state
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

        if (_isCleanupMode)
        {
            _ = UpdateEstimatedCleanupSizeAsync();
        }
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

    private void RefreshAppxPackagesButton_Click(object sender, RoutedEventArgs e)
    {
        LoadAppxPackages();
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

    private void SetMode(bool isCleanup)
    {
        var modeChanged = _isCleanupMode != isCleanup;
        _isCleanupMode = isCleanup;

        if (modeChanged)
        {
            OnPropertyChanged(nameof(IsCleanupMode));
            OnPropertyChanged(nameof(ActiveCategories));

            if (isCleanup)
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
            else
            {
                EstimatedCleanupSize = 0;
            }
        }
        else
        {
            // Force a refresh even if the mode appears unchanged to keep bindings current
            OnPropertyChanged(nameof(ActiveCategories));
        }

        var activeCategoriesList = ActiveCategories.ToList();
        var preferredCategory = isCleanup ? _lastCleanupCategory : _lastOptimizationCategory;
        if (preferredCategory is null || !activeCategoriesList.Contains(preferredCategory))
            preferredCategory = activeCategoriesList.FirstOrDefault();

        SelectedCategory = preferredCategory;
        OnPropertyChanged(nameof(VisibleSelectedActions));
        OnPropertyChanged(nameof(HasSelectedActions));
        OnPropertyChanged(nameof(SelectedActionsSummary));
        ApplyInteractionState();
    }

    private async Task InitializeActionStatesAsync()
    {
        var actions = Categories.SelectMany(category => category.Actions).ToList();

        foreach (var action in actions)
        {
            var applied = await _windowsOptimizationService.TryGetActionAppliedAsync(action.Key, CancellationToken.None).ConfigureAwait(false);
            if (applied.HasValue)
            {
                await Dispatcher.InvokeAsync(() => action.IsSelected = applied.Value);
            }
        }

        await Dispatcher.InvokeAsync(UpdateSelectedActions);
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

        public AppxPackageViewModel(string packageId, string packageFullName, string displayName, string description, bool isSelected)
        {
            PackageId = packageId;
            PackageFullName = packageFullName;
            DisplayName = displayName;
            Description = description;
            _isSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string PackageId { get; }

        public string PackageFullName { get; }

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
}
