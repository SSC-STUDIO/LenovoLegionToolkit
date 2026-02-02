using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;

namespace LenovoLegionToolkit.WPF.Controls.Dashboard;

public class DashboardGroupControl : UserControl
{
    private readonly TaskCompletionSource _initializedTaskCompletionSource = new();

    private readonly DashboardGroup _dashboardGroup;

    private StackPanel? _stackPanel;
    private TextBlock? _headerTextBlock;

    public Task InitializedTask => _initializedTaskCompletionSource.Task;

    public DashboardGroupControl(DashboardGroup dashboardGroup)
    {
        _dashboardGroup = dashboardGroup;

        Initialized += DashboardGroupControl_Initialized;
    }

    private async void DashboardGroupControl_Initialized(object? sender, System.EventArgs e)
    {
        _stackPanel = new StackPanel { Margin = new(0, 0, 16, 0) };

        _headerTextBlock = new TextBlock
        {
            Text = _dashboardGroup.GetName(),
            Focusable = true,
            FontSize = 24,
            FontWeight = FontWeights.Medium,
            Margin = new(0, 16, 0, 24)
        };
        AutomationProperties.SetName(_headerTextBlock, _headerTextBlock.Text);
        _stackPanel.Children.Add(_headerTextBlock);

        var controls = new List<AbstractRefreshingControl>();
        foreach (var item in _dashboardGroup.Items)
        {
            try
            {
                var itemControls = await item.GetControlAsync().ConfigureAwait(false);
                controls.AddRange(itemControls);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Failed to create control for {item}: {ex.Message}");
            }
        }

        foreach (var control in controls)
        {
            control.IsVisibleChanged += Control_IsVisibleChanged;
            _stackPanel.Children.Add(control);
        }

        Content = _stackPanel;

        UpdateGroupVisibility();

        _initializedTaskCompletionSource.TrySetResult();
    }

    private void Control_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateGroupVisibility();

    private void UpdateGroupVisibility()
    {
        if (_stackPanel is null || _headerTextBlock is null)
            return;

        var hasVisibleChild = _stackPanel.Children
            .OfType<UIElement>()
            .Where(child => !ReferenceEquals(child, _headerTextBlock))
            .Any(child => child.Visibility == Visibility.Visible);

        Visibility = hasVisibleChild ? Visibility.Visible : Visibility.Collapsed;
    }
}
