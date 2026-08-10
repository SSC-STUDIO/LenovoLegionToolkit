using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class DashboardGroupControl : UserControl
{
    private readonly TaskCompletionSource _initializedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _firstVisibleContentReadyTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly DashboardGroup _dashboardGroup;

    private StackPanel? _stackPanel;
    private TextBlock? _headerTextBlock;
    private bool _initializationStarted;

    public Task InitializedTask => _initializedTaskCompletionSource.Task;
    public Task FirstVisibleContentReadyTask => _firstVisibleContentReadyTaskCompletionSource.Task;

    public DashboardGroupControl(DashboardGroup dashboardGroup)
    {
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == ContentProperty)
                OnContentChanged(e.OldValue, e.NewValue);
        };

        _dashboardGroup = dashboardGroup;

    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_initializationStarted)
            return;

        _initializationStarted = true;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _stackPanel = new StackPanel { Margin = new(0, 0, 16, 0) };

            _headerTextBlock = new TextBlock
            {
                Text = _dashboardGroup.GetName(),
                Focusable = true,
                FontSize = 24,
                FontWeight = FontWeight.Medium,
                Margin = new(0, 16, 0, 24)
            };
            _headerTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
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
                control.PropertyChanged += Control_PropertyChanged;
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

    // AVALONIA: WPF IsVisibleChanged event does not exist on Avalonia Controls; the group
    // subscribes to AvaloniaObject.PropertyChanged and forwards IsVisibleProperty changes.
    private void Control_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        UpdateGroupVisibility();
    }

    private void UpdateGroupVisibility()
    {
        if (_stackPanel is null || _headerTextBlock is null)
            return;

        var hasVisibleChild = _stackPanel.Children
            .OfType<Control>()
            .Where(child => !ReferenceEquals(child, _headerTextBlock))
            .Any(child => child.IsVisible);

        IsVisible = hasVisibleChild ? true : false;
    }

    private async void OnContentChanged(object? oldContent, object? newContent)
    {
        try
        {

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
                .Where(control => control.IsVisible)
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
