using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls.Dashboard.Edit;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Settings;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Dashboard
{
public partial class EditDashboardWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly DashboardSettings _dashboardSettings = IoCContainer.Resolve<DashboardSettings>();

    private readonly DashboardGroup[] _groups;

    public event EventHandler? Apply;

    public EditDashboardWindow()
    {
        _groups = _dashboardSettings.Store.Groups ?? DashboardGroup.DefaultGroups;

        InitializeComponent();

        PropertyChanged += EditDashboardWindow_PropertyChanged;
    }

    private async void EditDashboardWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible)
                await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(EditDashboardWindow_PropertyChanged)}.", ex);
        }
    }

    private async Task RefreshAsync()
    {
        _loader.IsLoading = true;
        _infoBar.IsVisible = false;
        _applyRevertStackPanel.IsVisible = false;

        var loadingTask = Task.Delay(TimeSpan.FromMilliseconds(500));

        var groups = _groups;

        _groupsScrollViewer.ScrollToHome();
        _groupsStackPanel.Children.Clear();

        _sensorsSwitch.IsChecked = _dashboardSettings.Store.ShowSensors;

        foreach (var group in groups)
            _groupsStackPanel.Children.Add(CreateGroupControl(group));

        GroupsChanged();

        await loadingTask;

        _applyRevertStackPanel.IsVisible = true;
        _infoBar.IsVisible = true;
        _loader.IsLoading = false;
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await MessageBoxHelper.ShowInputAsync(this,
                Resource.EditDashboardWindow_CreateGroup_Title,
                Resource.EditDashboardWindow_CreateGroup_Message,
                primaryButton: Resource.OK,
                secondaryButton: Resource.Cancel);

            if (string.IsNullOrEmpty(result))
                return;

            _groupsStackPanel.Children.Add(CreateGroupControl(new(DashboardGroupType.Custom, result)));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(AddButton_Click)}.", ex);
        }
    }

    private void DefaultButton_Click(object sender, RoutedEventArgs e)
    {
        _dashboardSettings.Store.ShowSensors = true;
        _dashboardSettings.Store.Groups = null;
        _dashboardSettings.SynchronizeStore();

        Close();

        Apply?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        _dashboardSettings.Store.ShowSensors = _sensorsSwitch.IsChecked ?? true;
        _dashboardSettings.Store.Groups = _groupsStackPanel.Children
            .OfType<EditDashboardGroupControl>()
            .Select(c => c.GetDashboardGroup())
            .ToArray();
        _dashboardSettings.SynchronizeStore();

        Close();

        Apply?.Invoke(this, EventArgs.Empty);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private IEnumerable<DashboardItem> GetAllItems() =>
        _groupsStackPanel.Children
            .OfType<EditDashboardGroupControl>()
            .SelectMany(c => c.GetItems());

    private EditDashboardGroupControl CreateGroupControl(DashboardGroup dashboardGroup)
    {
        var control = new EditDashboardGroupControl(dashboardGroup, GetAllItems);
        control.MoveUp += EditDashboardGroupControl_MoveUp;
        control.MoveDown += EditDashboardGroupControl_MoveDown;
        control.Delete += EditDashboardGroupControl_Delete;
        control.Changed += EditDashboardGroupControl_Changed;
        return control;
    }

    private void GroupsChanged()
    {
        _groupsStackPanel.Children.OfType<EditDashboardGroupControl>().ForEach(c => c.RefreshAdd());
    }

    private void MoveGroupUp(Control control)
    {
        var index = _groupsStackPanel.Children.IndexOf(control);
        index--;

        if (index < 0)
            return;

        _groupsStackPanel.Children.Remove(control);
        _groupsStackPanel.Children.Insert(index, control);
    }

    private void MoveGroupDown(Control control)
    {
        var index = _groupsStackPanel.Children.IndexOf(control);
        index++;

        if (index >= _groupsStackPanel.Children.Count)
            return;

        _groupsStackPanel.Children.Remove(control);
        _groupsStackPanel.Children.Insert(index, control);
    }

    private void DeleteGroup(Control control)
    {
        _groupsStackPanel.Children.Remove(control);
    }

    private void EditDashboardGroupControl_MoveUp(object? sender, EventArgs e)
    {
        if (sender is EditDashboardGroupControl control)
            MoveGroupUp(control);
    }

    private void EditDashboardGroupControl_MoveDown(object? sender, EventArgs e)
    {
        if (sender is EditDashboardGroupControl control)
            MoveGroupDown(control);
    }

    private void EditDashboardGroupControl_Delete(object? sender, EventArgs e)
    {
        if (sender is EditDashboardGroupControl control)
            DeleteGroup(control);
    }

    private void EditDashboardGroupControl_Changed(object? sender, EventArgs e)
    {
        GroupsChanged();
    }
}
}
