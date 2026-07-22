using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.PackageDownloader;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.ViewModels;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using UniversalDeviceToolkit.WPF.Windows.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

using Wpf.Ui.Controls;
using CardExpander = UniversalDeviceToolkit.WPF.Controls.Custom.CardExpander;

namespace UniversalDeviceToolkit.WPF.Pages
{
public partial class WindowsOptimizationPage : Page
{
    private static readonly object FocusRequestLock = new();
    private static string? _pendingFocusPluginId;

    private readonly WindowsOptimizationViewModel _viewModel;
    public WindowsOptimizationViewModel ViewModel => _viewModel;

    private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private readonly PackageDownloaderSettings _packageDownloaderSettings = IoCContainer.Resolve<PackageDownloaderSettings>();
    private readonly PackageDownloaderFactory _packageDownloaderFactory = IoCContainer.Resolve<PackageDownloaderFactory>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private CancellationTokenSource? _pluginRefreshCancellationTokenSource;
    private int _pluginRefreshVersion;
    private bool _hasCompletedInitialCategoriesLoad;
    
    private SelectedActionsWindow? _selectedActionsWindow;
    private ActionDetailsWindow? _actionDetailsWindow;

    public WindowsOptimizationPage()
    {
        _viewModel = IoCContainer.Resolve<WindowsOptimizationViewModel>();
        DataContext = ViewModel;

        InitializeComponent();
        
        Loaded += WindowsOptimizationPage_Loaded;
        Unloaded += WindowsOptimizationPage_Unloaded;
    }

    public static void RequestPluginCategoryFocus(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        lock (FocusRequestLock)
            _pendingFocusPluginId = pluginId;
    }

    public static void ClearPendingPluginCategoryFocus()
    {
        lock (FocusRequestLock)
            _pendingFocusPluginId = null;
    }

    private void WindowsOptimizationPage_Loaded(object sender, RoutedEventArgs e)
    {
        _pluginManager.PluginStateChanged -= PluginManager_PluginStateChanged;
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;
        ViewModel.Initialize();
        SyncNavButtonToCurrentMode();
        AttachNetworkAccelerationSelectionChrome();
        TryApplyPendingPluginFocusRequest();

        if (!_hasCompletedInitialCategoriesLoad)
            _ = RunInitialCategoriesLoadAsync();
    }

    /// <summary>
    /// First load only: InitializeCore starts a fire-and-forget optimization state scan that
    /// resolves every action's real system state. Keep the categories skeleton up until this
    /// instance's scan drains (queued behind the in-flight one), then crossfade to the list.
    /// Later navigations keep the already-populated list live during re-scans.
    /// </summary>
    private async Task RunInitialCategoriesLoadAsync()
    {
        try
        {
            await ViewModel.ScanOptimizationStatesAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Initial optimization state scan failed.", ex);
        }
        finally
        {
            _hasCompletedInitialCategoriesLoad = true;
            _categoriesLoader.IsLoading = false;
        }
    }

    /// <summary>
    /// Network acceleration multi-select chrome shares the tab-row toolbar slot
    /// used by bulk actions on other optimization modes (not floating over content).
    /// </summary>
    private void AttachNetworkAccelerationSelectionChrome()
    {
        if (_networkAccelerationControl is null)
            return;

        _networkAccelerationControl.AttachSelectionChrome(
            _networkAccelerationSelectionCount,
            _networkAccelerationSelectionFavoriteButton,
            _networkAccelerationSelectionStartButton);
    }

    private void NetworkAccelerationSelectionCountButton_Click(object sender, RoutedEventArgs e)
    {
        _networkAccelerationControl?.HandleSelectionCountClicked();
    }

    private void SyncNavButtonToCurrentMode()
    {
        // Sync UI RadioButton to ViewModel's CurrentMode (which may be restored from settings)
        switch (ViewModel.CurrentMode)
        {
            case WindowsOptimizationViewModel.PageMode.Optimization:
                _optimizationNavButton.IsChecked = true;
                break;
            case WindowsOptimizationViewModel.PageMode.Cleanup:
                _cleanupNavButton.IsChecked = true;
                break;
            case WindowsOptimizationViewModel.PageMode.DriverDownload:
                _driverDownloadNavButton.IsChecked = true;
                break;
            case WindowsOptimizationViewModel.PageMode.NetworkAcceleration:
                _networkAccelerationNavButton.IsChecked = true;
                break;
        }
    }

    private void WindowsOptimizationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _pluginManager.PluginStateChanged -= PluginManager_PluginStateChanged;

        var cancellationTokenSource = Interlocked.Exchange(ref _pluginRefreshCancellationTokenSource, null);
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();

        // Close windows
        _actionDetailsWindow?.Close();
        _selectedActionsWindow?.Close();

        // Unsubscribe from driver package PropertyChanged handlers to prevent memory leaks
        UnsubscribeFromPackageControlHandlers();

        // Do NOT dispose the ViewModel here. NavigationStore caches this page; Unloaded
        // then Loaded reuses the same instance. Disposing the VM frees SemaphoreSlim and
        // breaks ScanOptimizationStatesAsync on re-entry (ObjectDisposedException).
    }

    private void TryApplyPendingPluginFocusRequest()
    {
        string? pluginId;
        lock (FocusRequestLock)
        {
            pluginId = _pendingFocusPluginId;
            _pendingFocusPluginId = null;
        }

        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        if (!FocusPluginCategory(pluginId))
            RequestPluginCategoryFocus(pluginId);
    }

    private bool FocusPluginCategory(string pluginId)
    {
        ViewModel.CurrentMode = WindowsOptimizationViewModel.PageMode.Optimization;
        var targetCategory = ViewModel.OptimizationCategories.FirstOrDefault(category =>
            string.Equals(category.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));

        if (targetCategory == null)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Windows optimization category not found for plugin '{pluginId}'.");
            return false;
        }

        foreach (var category in ViewModel.OptimizationCategories)
            category.IsExpanded = ReferenceEquals(category, targetCategory);

        Dispatcher.BeginInvoke(() =>
        {
            _categoriesList?.UpdateLayout();
            var expander = FindCategoryExpander(targetCategory);
            expander?.BringIntoView();
            expander?.Focus();
        }, DispatcherPriority.Loaded);

        return true;
    }

    private void PluginManager_PluginStateChanged(object? sender, PluginEventArgs e)
    {
        var refreshVersion = Interlocked.Increment(ref _pluginRefreshVersion);
        var cancellationTokenSource = new CancellationTokenSource();
        var previousCancellationTokenSource = Interlocked.Exchange(ref _pluginRefreshCancellationTokenSource, cancellationTokenSource);
        previousCancellationTokenSource?.Cancel();
        previousCancellationTokenSource?.Dispose();
        var cancellationToken = cancellationTokenSource.Token;

        Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                if (e.IsInstalled)
                    await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true);

                cancellationToken.ThrowIfCancellationRequested();
                if (refreshVersion != Volatile.Read(ref _pluginRefreshVersion))
                    return;

                ViewModel.Initialize();

                if (e.IsInstalled)
                    RequestPluginCategoryFocus(e.PluginId);

                TryApplyPendingPluginFocusRequest();
            }
            catch (OperationCanceledException)
            {
            }
        }, DispatcherPriority.Background);
    }

    private CardExpander? FindCategoryExpander(OptimizationCategoryViewModel categoryVm)
    {
        if (_categoriesList == null)
            return null;

        return EnumerateVisualDescendants<CardExpander>(_categoriesList)
            .FirstOrDefault(expander => ReferenceEquals(expander.DataContext, categoryVm));
    }

    private static IEnumerable<T> EnumerateVisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        var childrenCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;

            foreach (var nested in EnumerateVisualDescendants<T>(child))
                yield return nested;
        }
    }

    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;

        if (element == _optimizationNavButton)
        {
            ViewModel.CurrentMode = WindowsOptimizationViewModel.PageMode.Optimization;
        }
        else if (element == _cleanupNavButton)
        {
            ViewModel.CurrentMode = WindowsOptimizationViewModel.PageMode.Cleanup;
            if (!ViewModel.IsScanned)
            {
                // Optional: Auto scan on first switch
            }
        }
        else if (element == _driverDownloadNavButton)
        {
            ViewModel.CurrentMode = WindowsOptimizationViewModel.PageMode.DriverDownload;
            _ = InitializeDriverDownloadPage();
        }
        else if (element == _networkAccelerationNavButton)
        {
            // Assign even when already NA so visibility bindings refresh after Initialize restore.
            if (ViewModel.CurrentMode == WindowsOptimizationViewModel.PageMode.NetworkAcceleration)
            {
                ViewModel.RefreshModePresentation();
            }
            else
            {
                ViewModel.CurrentMode = WindowsOptimizationViewModel.PageMode.NetworkAcceleration;
            }
        }
    }

    private void SelectedActionsButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedActionsWindow?.Close();

        _selectedActionsWindow = new SelectedActionsWindow(ViewModel.VisibleSelectedActions, Resource.WindowsOptimizationPage_SelectedActions_Empty)
        {
            Owner = Window.GetWindow(this)
        };

        if (_selectedActionsWindow != null)
        {
            _selectedActionsWindow.Closed += (s, args) => _selectedActionsWindow = null;
            _selectedActionsWindow.Show();
        }
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer != null)
        {
            e.Handled = true;
            var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
        }
    }

    private void OpenStyleSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var categoryVm = element.Tag as OptimizationCategoryViewModel
            ?? element.DataContext as OptimizationCategoryViewModel;
        if (categoryVm is null)
            return;

        if (string.IsNullOrEmpty(categoryVm.PluginId)) return;

        try
        {
            var pluginSettingsWindow = new PluginSettingsWindow(categoryVm.PluginId)
            {
                Owner = Window.GetWindow(this)
            };
            pluginSettingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to open plugin settings window for {categoryVm.PluginId}.", ex);
        }
    }

    private void ActionItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not OptimizationActionViewModel actionVm)
            return;

        // Double click logic or details window logic
        if (e.ClickCount == 2)
        {
            OpenActionDetailsWindow(actionVm);
        }
    }

    private void OpenActionDetailsWindow(OptimizationActionViewModel actionVm)
    {
        try
        {
            _actionDetailsWindow?.Close();
            _actionDetailsWindow = new ActionDetailsWindow(actionVm.Key, actionVm.Definition)
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

    private void SelectRecommendedButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentMode == WindowsOptimizationViewModel.PageMode.DriverDownload)
        {
            DriverSelectRecommendedButton_Click(sender, e);
        }
        else
        {
            ViewModel.SelectRecommended();
        }
    }

    private async void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel.CurrentMode == WindowsOptimizationViewModel.PageMode.DriverDownload)
            {
                await StartOrPauseSelectedDriversAsync();
            }
            else
            {
                ViewModel.ClearSelection();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error clearing selection.", ex);
        }
    }
}
}
