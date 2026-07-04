using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard
{
public partial class DiscreteGPUControl : AbstractRefreshingControl
{
    private readonly GPUController _gpuController = IoCContainer.Resolve<GPUController>();
    private readonly NativeWindowsMessageListener _nativeWindowsMessageListener = IoCContainer.Resolve<NativeWindowsMessageListener>();

    public static readonly DependencyProperty IsGpuActiveProperty =
        DependencyProperty.Register(nameof(IsGpuActive), typeof(bool), typeof(DiscreteGPUControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsGpuInactiveProperty =
        DependencyProperty.Register(nameof(IsGpuInactive), typeof(bool), typeof(DiscreteGPUControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsGpuPoweredOffProperty =
        DependencyProperty.Register(nameof(IsGpuPoweredOff), typeof(bool), typeof(DiscreteGPUControl), new PropertyMetadata(false));

    public static readonly DependencyProperty CanDeactivateGpuProperty =
        DependencyProperty.Register(nameof(CanDeactivateGpu), typeof(bool), typeof(DiscreteGPUControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsGpuContentReadyProperty =
        DependencyProperty.Register(nameof(IsGpuContentReady), typeof(bool), typeof(DiscreteGPUControl), new PropertyMetadata(false));

    public bool IsGpuActive
    {
        get => (bool)GetValue(IsGpuActiveProperty);
        set => SetValue(IsGpuActiveProperty, value);
    }

    public bool IsGpuInactive
    {
        get => (bool)GetValue(IsGpuInactiveProperty);
        set => SetValue(IsGpuInactiveProperty, value);
    }

    public bool IsGpuPoweredOff
    {
        get => (bool)GetValue(IsGpuPoweredOffProperty);
        set => SetValue(IsGpuPoweredOffProperty, value);
    }

    public bool CanDeactivateGpu
    {
        get => (bool)GetValue(CanDeactivateGpuProperty);
        set => SetValue(CanDeactivateGpuProperty, value);
    }

    public bool IsGpuContentReady
    {
        get => (bool)GetValue(IsGpuContentReadyProperty);
        set => SetValue(IsGpuContentReadyProperty, value);
    }

    public DiscreteGPUControl()
    {
        InitializeComponent();

        _gpuController.Refreshed += GpuController_Refreshed;
        _nativeWindowsMessageListener.Changed += NativeWindowsMessageListener_Changed;

        IsVisibleChanged += DiscreteGPUControl_IsVisibleChanged;
        Unloaded += DiscreteGPUControl_Unloaded;
    }

    private void DiscreteGPUControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _gpuController.Refreshed -= GpuController_Refreshed;
        _nativeWindowsMessageListener.Changed -= NativeWindowsMessageListener_Changed;
        IsVisibleChanged -= DiscreteGPUControl_IsVisibleChanged;
    }

    protected override void OnFinishedLoading() { }

    protected override async Task OnRefreshAsync()
    {
        if (!await _gpuController.IsSupportedAsync())
        {
            Visibility = Visibility.Collapsed;
            IsGpuContentReady = false;
            await _gpuController.StopAsync();
            return;
        }

        Visibility = Visibility.Visible;
        IsGpuContentReady = true;

        await _gpuController.StartAsync();
    }

    private async void NativeWindowsMessageListener_Changed(object? sender, NativeWindowsMessageListener.ChangedEventArgs e)
    {
        if (e.Message != NativeWindowsMessage.OnDisplayDeviceArrival)
            return;

        Visibility = Visibility.Visible;
        await RefreshAsync();
    }

    private async void DiscreteGPUControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            return;

        IsGpuContentReady = false;

        await _gpuController.StopAsync();
    }

    private void GpuController_Refreshed(object? sender, GPUStatus e) => Dispatcher.BeginInvoke(() =>
    {
        var tooltipStringBuilder = new StringBuilder(Resource.DiscreteGPUControl_PerformanceState);
        tooltipStringBuilder.AppendLine().Append("  \u2192 ").Append(e.PerformanceState ?? Resource.DiscreteGPUControl_PerformanceState_Unknown);

        if (e.State is GPUState.Unknown or GPUState.NvidiaGpuNotFound)
        {
            IsGpuActive = false;
            IsGpuInactive = false;
            IsGpuPoweredOff = false;
            _discreteGPUStatusDescription.Text = "-";
            _gpuInfoButton.ToolTip = null;
            _gpuInfoButton.IsEnabled = false;
            Visibility = Visibility.Collapsed;
            return;
        }

        if (e.State is GPUState.MonitorConnected)
            tooltipStringBuilder.AppendLine().AppendLine().Append(Resource.DiscreteGPUControl_MonitorConnected);

        if (e.State is GPUState.Active or GPUState.MonitorConnected)
        {
            var processesStringBuilder = new StringBuilder();

            if (e.ProcessCount > 0)
            {
                processesStringBuilder.Append(Resource.DiscreteGPUControl_Processes);
                foreach (var p in e.Processes.OrderBy(p => p.ProcessName))
                {
                    try { processesStringBuilder.AppendLine().Append("  \u2022 ").Append(p.ProcessName); }
                    catch
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace("Failed to append GPU process name");
                    }
                }
            }
            else
            {
                processesStringBuilder.Append(Resource.DiscreteGPUControl_NoProcesses);
            }

            IsGpuActive = true;
            IsGpuInactive = false;
            IsGpuPoweredOff = false;
            _discreteGPUStatusDescription.Text = Resource.Active;
            _gpuInfoButton.ToolTip = tooltipStringBuilder.AppendLine().AppendLine().Append(processesStringBuilder).ToString();
            _gpuInfoButton.IsEnabled = true;
        }
        else if (e.State is GPUState.PoweredOff)
        {
            IsGpuActive = false;
            IsGpuInactive = false;
            IsGpuPoweredOff = true;
            _discreteGPUStatusDescription.Text = Resource.PoweredOff;
            _gpuInfoButton.ToolTip = tooltipStringBuilder.ToString();
            _gpuInfoButton.IsEnabled = true;
        }
        else
        {
            IsGpuActive = false;
            IsGpuInactive = true;
            IsGpuPoweredOff = false;
            _discreteGPUStatusDescription.Text = Resource.Inactive;
            _gpuInfoButton.ToolTip = tooltipStringBuilder.ToString();
            _gpuInfoButton.IsEnabled = true;
        }

        CanDeactivateGpu = e.State is GPUState.Active or GPUState.Inactive;
        _killAppsMenuItem.IsEnabled = e.State is GPUState.Active;
        _restartGPUMenuItem.IsEnabled = e.State is GPUState.Active or GPUState.Inactive;

        if (e.State is GPUState.Active or GPUState.Inactive)
        {
            _deactivateGPUButtonText.SetResourceReference(ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
            _deactivateGPUButtonIcon.SetResourceReference(ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
        }
        else
        {
            _deactivateGPUButtonText.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
            _deactivateGPUButtonIcon.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
        }

        IsGpuContentReady = true;
    });

    private void DeactivateGPUButton_Click(object sender, RoutedEventArgs e)
    {
        if (_deactivateGPUButton.ContextMenu is null)
            return;

        _deactivateGPUButton.ContextMenu.PlacementTarget = _deactivateGPUButton;
        _deactivateGPUButton.ContextMenu.Placement = PlacementMode.Bottom;
        _deactivateGPUButton.ContextMenu.IsOpen = true;
    }

    private async void KillAppsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var originalCanDeactivate = CanDeactivateGpu;
        CanDeactivateGpu = false;

        try
        {
            await _gpuController.KillGPUProcessesAsync();
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to kill GPU processes.", ex);
        }
        finally
        {
            CanDeactivateGpu = originalCanDeactivate;
        }
    }

    private async void RestartGPUMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var originalCanDeactivate = CanDeactivateGpu;
        CanDeactivateGpu = false;

        try
        {
            await _gpuController.RestartGPUAsync();
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to restart GPU.", ex);
        }
        finally
        {
            CanDeactivateGpu = originalCanDeactivate;
        }
    }
}
}
