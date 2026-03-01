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
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.ViewModels;
using LenovoLegionToolkit.WPF.Windows.Utils;
using LenovoLegionToolkit.WPF.Windows.Settings;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Pages.WindowsOptimization;

using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage : UiPage
{
    private static readonly object FocusRequestLock = new();
    private static string? _pendingFocusPluginId;

    private readonly WindowsOptimizationViewModel _viewModel;
    public WindowsOptimizationViewModel ViewModel => _viewModel;

    private readonly WindowsOptimizationService _windowsOptimizationService = IoCContainer.Resolve<WindowsOptimizationService>();
    private readonly PackageDownloaderSettings _packageDownloaderSettings = IoCContainer.Resolve<PackageDownloaderSettings>();
    private readonly PackageDownloaderFactory _packageDownloaderFactory = IoCContainer.Resolve<PackageDownloaderFactory>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    
    private SelectedActionsWindow? _selectedActionsWindow;
    private ActionDetailsWindow? _actionDetailsWindow;

    public WindowsOptimizationPage()
    {
        _viewModel = IoCContainer.Resolve<WindowsOptimizationViewModel>();
        DataContext = ViewModel;

        InitializeComponent();

        ViewModel.Initialize();
        
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

    private void WindowsOptimizationPage_Loaded(object sender, RoutedEventArgs e)
    {
        TryApplyPendingPluginFocusRequest();
    }

    private void WindowsOptimizationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Close windows
        _actionDetailsWindow?.Close();
        _selectedActionsWindow?.Close();
        
        // Clean up CancellationTokenSource instances to prevent memory leaks
        CleanupCancellationTokenSources();
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

        FocusPluginCategory(pluginId);
    }

    private void FocusPluginCategory(string pluginId)
    {
        ViewModel.CurrentMode = WindowsOptimizationViewModel.PageMode.Optimization;
        var targetCategory = ViewModel.OptimizationCategories.FirstOrDefault(category =>
            string.Equals(category.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));

        if (targetCategory == null)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Windows optimization category not found for plugin '{pluginId}'.");
            return;
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
    }

    private Expander? FindCategoryExpander(OptimizationCategoryViewModel categoryVm)
    {
        if (_categoriesList == null)
            return null;

        return EnumerateVisualDescendants<Expander>(_categoriesList)
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

    private void CleanupCancellationTokenSources()
    {
        // Clean up driver filter debounce token source
        if (_driverFilterDebounceCancellationTokenSource != null)
        {
            try
            {
                if (!_driverFilterDebounceCancellationTokenSource.Token.IsCancellationRequested)
                    _driverFilterDebounceCancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            finally
            {
                _driverFilterDebounceCancellationTokenSource?.Dispose();
                _driverFilterDebounceCancellationTokenSource = null;
            }
        }

        // Clean up driver get packages token source
        if (_driverGetPackagesTokenSource != null)
        {
            try
            {
                if (!_driverGetPackagesTokenSource.Token.IsCancellationRequested)
                    _driverGetPackagesTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            finally
            {
                _driverGetPackagesTokenSource?.Dispose();
                _driverGetPackagesTokenSource = null;
            }
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
            InitializeDriverDownloadPage();
        }
    }

    private void SelectedActionsButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedActionsWindow?.Close();

        if (ViewModel.CurrentMode == WindowsOptimizationViewModel.PageMode.DriverDownload)
        {
            // Handle driver download selected actions display
        }
        else
        {
            _selectedActionsWindow = new SelectedActionsWindow(ViewModel.VisibleSelectedActions, Resource.WindowsOptimizationPage_SelectedActions_Empty)
            {
                Owner = Window.GetWindow(this)
            };
        }

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
        if (sender is not FrameworkElement element || element.DataContext is not OptimizationCategoryViewModel categoryVm)
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
            OpenActionDetailsWindow(actionVm.Key);
        }
    }

    private void OpenActionDetailsWindow(string actionKey)
    {
        try
        {
            _actionDetailsWindow?.Close();
            _actionDetailsWindow = new ActionDetailsWindow(actionKey, null) // Definition can be fetched inside
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

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentMode == WindowsOptimizationViewModel.PageMode.DriverDownload)
        {
            // Handle driver download clear/pause logic
        }
        else
        {
            ViewModel.ClearSelection();
        }
    }
}
