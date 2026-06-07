using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Humanizer;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Dashboard;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Pages
{
public partial class DashboardPage
{
    private readonly DashboardSettings _dashboardSettings = IoCContainer.Resolve<DashboardSettings>();
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();

    private readonly List<DashboardGroupControl> _dashboardGroupControls = [];
    private HyperlinkButton? _editDashboardHyperlink;
    private int _currentColumnCount = 1;

    public DashboardPage()
    {
        InitializeComponent();
        IsVisibleChanged += DashboardPage_IsVisibleChanged;
    }

    private async void DashboardPage_Initialized(object? sender, EventArgs e)
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _loader.IsLoading = false;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Dashboard initialization failed.", ex);
        }
    }

    private void DashboardPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            return;
        }
    }

    private async Task RefreshAsync()
    {
        _loader.IsLoading = true;
        SetDashboardContentReady(false);

        _scrollViewer.ScrollToTop();

        Task? sensorsReadyTask = null;
        if (_dashboardSettings.Store.ShowSensors)
        {
            sensorsReadyTask = _sensors.RestartInitialSensorDataLoad();
            _sensors.Visibility = Visibility.Visible;
        }
        else
        {
            _sensors.Visibility = Visibility.Collapsed;
        }

        _dashboardGroupControls.Clear();
        _content.ColumnDefinitions.Clear();
        _content.RowDefinitions.Clear();
        _content.Children.Clear();

        var groups = _dashboardSettings.Store.Groups ?? DashboardGroup.DefaultGroups;

        if (Log.Instance.IsTraceEnabled)
        {
            Log.Instance.Trace($"Groups:");
            foreach (var group in groups)
                Log.Instance.Trace($" - {group}");
        }

        foreach (var group in groups)
        {
            var control = new DashboardGroupControl(group);
            _content.Children.Add(control);
            _dashboardGroupControls.Add(control);
        }

        var editDashboardHyperlink = new HyperlinkButton
        {
            Icon = new SymbolIcon { Symbol = SymbolRegular.Edit24 },
            Content = Resource.DashboardPage_Customize,
            Margin = new(0, 16, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        editDashboardHyperlink.Click += (_, _) =>
        {
            var window = new EditDashboardWindow { Owner = Window.GetWindow(this) };
            window.Apply += async (_, _) => await RefreshAsync();
            window.ShowDialog();
        };

        _editDashboardHyperlink = editDashboardHyperlink;
        _content.Children.Add(editDashboardHyperlink);

        LayoutGroups(ActualWidth);

        await WaitForDashboardShellAsync(sensorsReadyTask);
        SetDashboardContentReady(true);
        _loader.IsLoading = false;
        await Task.Delay(TimeSpan.FromMilliseconds(250));
    }

    private void SetDashboardContentReady(bool ready)
    {
        _dashboardContentRoot.Visibility = Visibility.Visible;
        _dashboardContentRoot.Opacity = ready ? 1 : 0;
        _dashboardContentRoot.IsHitTestVisible = ready;
    }

    private async Task WaitForDashboardShellAsync(Task? sensorsReadyTask)
    {
        var groupInitializationTasks = _dashboardGroupControls.Select(control => control.InitializedTask).ToArray();
        if (groupInitializationTasks.Length > 0)
            await Task.WhenAll(groupInitializationTasks);

        var contentReadyTasks = _dashboardGroupControls.Select(control => control.FirstVisibleContentReadyTask).ToArray();

        if (contentReadyTasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(contentReadyTasks).WaitAsync(GetDashboardGroupContentReadyTimeout());
            }
            catch (TimeoutException)
            {
                // Do not let one regular card block the whole dashboard.
            }
        }

        if (sensorsReadyTask is not null)
            await WaitForDashboardSensorDataAsync(sensorsReadyTask);

        await Task.Delay(GetDashboardFallbackLoadingDelay());
    }

    private static async Task WaitForDashboardSensorDataAsync(Task sensorsReadyTask)
    {
        try
        {
            await sensorsReadyTask.WaitAsync(GetDashboardSensorDataReadyTimeout()).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Dashboard sensor data was not ready before the bounded loading timeout.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Dashboard sensor data readiness failed.", ex);
        }
    }

    internal static TimeSpan GetDashboardGroupContentReadyTimeout() => TimeSpan.FromSeconds(3);

    internal static TimeSpan GetDashboardSensorDataReadyTimeout() => TimeSpan.FromSeconds(12);

    internal static TimeSpan GetDashboardFallbackLoadingDelay() => TimeSpan.FromMilliseconds(120);

    private void DashboardPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;

        // Only relayout when the responsive column count actually changes.
        if (GetColumnCountForWidth(e.NewSize.Width) == _currentColumnCount && _content.ColumnDefinitions.Count > 0)
        {
            LayoutSkeletonGroups(_currentColumnCount);
            return;
        }

        LayoutGroups(e.NewSize.Width);
    }

    private void LayoutGroups(double width)
    {
        var columns = GetColumnCountForWidth(width);
        LayoutSkeletonGroups(columns);
        LayoutColumns(columns);
    }

    internal static int GetColumnCountForWidth(double width)
    {
        if (width > 1500)
            return 3;
        if (width > 1000)
            return 2;
        return 1;
    }

    private void LayoutSkeletonGroups(int columns)
    {
        if (_skeletonGroupsGrid is null || _skeletonGroup0 is null || _skeletonGroup1 is null)
            return;

        // The skeleton placeholder only models two columns; collapse to one when narrow.
        if (columns >= 2)
        {
            _skeletonGroupsGrid.ColumnDefinitions[1].Width = new(1, GridUnitType.Star);
            Grid.SetRow(_skeletonGroup0, 0);
            Grid.SetColumn(_skeletonGroup0, 0);
            Grid.SetRow(_skeletonGroup1, 0);
            Grid.SetColumn(_skeletonGroup1, 1);
            return;
        }

        _skeletonGroupsGrid.ColumnDefinitions[1].Width = new(0, GridUnitType.Pixel);
        Grid.SetRow(_skeletonGroup0, 0);
        Grid.SetColumn(_skeletonGroup0, 0);
        Grid.SetRow(_skeletonGroup1, 1);
        Grid.SetColumn(_skeletonGroup1, 0);
    }

    private void LayoutColumns(int columns)
    {
        columns = Math.Max(1, columns);

        // Rebuild column definitions to match the target column count.
        if (_content.ColumnDefinitions.Count != columns)
        {
            _content.ColumnDefinitions.Clear();
            for (var i = 0; i < columns; i++)
                _content.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1, GridUnitType.Star) });
        }

        // Ensure enough rows: one per "row" of group controls plus a trailing row for the link.
        var groupRows = (int)Math.Ceiling(_dashboardGroupControls.Count / (double)columns);
        var rowsNeeded = groupRows + 1;
        if (_content.RowDefinitions.Count != rowsNeeded)
        {
            _content.RowDefinitions.Clear();
            for (var i = 0; i < rowsNeeded; i++)
                _content.RowDefinitions.Add(new RowDefinition { Height = new(1, GridUnitType.Auto) });
        }

        for (var index = 0; index < _dashboardGroupControls.Count; index++)
        {
            var control = _dashboardGroupControls[index];
            Grid.SetRow(control, index / columns);
            Grid.SetColumn(control, index % columns);
            Grid.SetColumnSpan(control, 1);
        }

        if (_editDashboardHyperlink is not null)
        {
            Grid.SetRow(_editDashboardHyperlink, groupRows);
            Grid.SetColumn(_editDashboardHyperlink, 0);
            Grid.SetColumnSpan(_editDashboardHyperlink, columns);
        }

        _currentColumnCount = columns;
    }
}
}
