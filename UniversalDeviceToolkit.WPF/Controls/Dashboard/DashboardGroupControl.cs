using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class DashboardGroupControl : UserControl
{
    private readonly TaskCompletionSource _initializedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _firstVisibleContentReadyTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly DashboardGroup _dashboardGroup;

    private StackPanel? _stackPanel;
    private TextBlock? _headerTextBlock;

    public Task InitializedTask => _initializedTaskCompletionSource.Task;
    public Task FirstVisibleContentReadyTask => _firstVisibleContentReadyTaskCompletionSource.Task;

    public DashboardGroupControl(DashboardGroup dashboardGroup)
    {
        _dashboardGroup = dashboardGroup;

        Initialized += DashboardGroupControl_Initialized;
    }

    private async void DashboardGroupControl_Initialized(object? sender, System.EventArgs e)
    {
        try
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
            _headerTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
            AutomationProperties.SetName(_headerTextBlock, _headerTextBlock.Text);
            _stackPanel.Children.Add(_headerTextBlock);

            var controls = new List<AbstractRefreshingControl>();
            foreach (var item in _dashboardGroup.Items)
            {
                try
                {
                    var itemControls = await item.GetControlAsync().WaitAsync(TimeSpan.FromSeconds(6));
                    controls.AddRange(itemControls);
                }
                catch (TimeoutException ex)
                {
                    Log.Instance.Error($"Timed out creating dashboard control for {item}: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to initialize dashboard group {_dashboardGroup.GetName()}: {ex.Message}");
        }
        finally
        {
            _initializedTaskCompletionSource.TrySetResult();
        }
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

    protected override async void OnContentChanged(object oldContent, object newContent)
    {
        try
        {
            base.OnContentChanged(oldContent, newContent);

            if (newContent is not StackPanel)
                return;

            await TryCompleteFirstVisibleContentReadyAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(OnContentChanged)}.", ex);
        }
    }

    private async Task TryCompleteFirstVisibleContentReadyAsync()
    {
        try
        {
            if (_stackPanel is null)
                return;

            var visibleRefreshingControls = _stackPanel.Children
                .OfType<AbstractRefreshingControl>()
                .Where(control => control.Visibility == Visibility.Visible)
                .ToArray();

            if (visibleRefreshingControls.Length == 0)
            {
                _firstVisibleContentReadyTaskCompletionSource.TrySetResult();
                return;
            }

            await Task.WhenAll(visibleRefreshingControls.Select(control => control.InitialRefreshCompletedTask));
            _firstVisibleContentReadyTaskCompletionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed waiting for dashboard group {_dashboardGroup.GetName()} content readiness: {ex.Message}");
            _firstVisibleContentReadyTaskCompletionSource.TrySetResult();
        }
    }
}
