using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard
{
public partial class DiscreteGPUControl : global::UniversalDeviceToolkit.Avalonia.Controls.AbstractRefreshingControl
{
    private readonly GPUController _gpuController = IoCContainer.Resolve<GPUController>();
    private readonly NativeWindowsMessageListener _nativeWindowsMessageListener = IoCContainer.Resolve<NativeWindowsMessageListener>();

    public static readonly StyledProperty<bool> IsGpuActiveProperty =
        AvaloniaProperty.Register<DiscreteGPUControl, bool>(nameof(IsGpuActive), false);

    public static readonly StyledProperty<bool> IsGpuInactiveProperty =
        AvaloniaProperty.Register<DiscreteGPUControl, bool>(nameof(IsGpuInactive), false);

    public static readonly StyledProperty<bool> IsGpuPoweredOffProperty =
        AvaloniaProperty.Register<DiscreteGPUControl, bool>(nameof(IsGpuPoweredOff), false);

    public static readonly StyledProperty<bool> CanDeactivateGpuProperty =
        AvaloniaProperty.Register<DiscreteGPUControl, bool>(nameof(CanDeactivateGpu), false);

    public static readonly StyledProperty<bool> IsGpuContentReadyProperty =
        AvaloniaProperty.Register<DiscreteGPUControl, bool>(nameof(IsGpuContentReady), false);

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

        PropertyChanged += DiscreteGPUControl_IsVisibleChanged;
        Unloaded += DiscreteGPUControl_Unloaded;
        Loaded += DiscreteGPUControl_Loaded;
    }

    private void DiscreteGPUControl_Loaded(object sender, RoutedEventArgs e)
    {
        _gpuController.Refreshed -= GpuController_Refreshed;
        _gpuController.Refreshed += GpuController_Refreshed;
        _nativeWindowsMessageListener.Changed -= NativeWindowsMessageListener_Changed;
        _nativeWindowsMessageListener.Changed += NativeWindowsMessageListener_Changed;
        PropertyChanged -= DiscreteGPUControl_IsVisibleChanged;
        PropertyChanged += DiscreteGPUControl_IsVisibleChanged;
    }

    private void DiscreteGPUControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _gpuController.Refreshed -= GpuController_Refreshed;
        _nativeWindowsMessageListener.Changed -= NativeWindowsMessageListener_Changed;
        PropertyChanged -= DiscreteGPUControl_IsVisibleChanged;
    }

    protected override void OnFinishedLoading() { }

    protected override async Task OnRefreshAsync()
    {
        if (!await _gpuController.IsSupportedAsync())
        {
            IsVisible = false;
            IsGpuContentReady = false;
            await _gpuController.StopAsync();
            return;
        }

        IsVisible = true;
        IsGpuContentReady = true;

        await _gpuController.StartAsync();
    }

    private async void NativeWindowsMessageListener_Changed(object? sender, NativeWindowsMessageListener.ChangedEventArgs e)
    {
        try
        {
            if (e.Message != NativeWindowsMessage.OnDisplayDeviceArrival)
                return;

            IsVisible = true;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(NativeWindowsMessageListener_Changed)}.", ex);
        }
    }

    private async void DiscreteGPUControl_IsVisibleChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // AVALONIA: WPF IsVisibleChanged event does not exist; this is subscribed to
        // AvaloniaObject.PropertyChanged and filters for Visual.IsVisibleProperty.
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible)
                return;

            await _gpuController.StopAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(DiscreteGPUControl_IsVisibleChanged)}.", ex);
        }
    }

    private void GpuController_Refreshed(object? sender, GPUStatus e) => Dispatcher.UIThread.Post(() =>
    {
        var tooltipStringBuilder = new StringBuilder(Resource.DiscreteGPUControl_PerformanceState);
        tooltipStringBuilder.AppendLine().Append("  \u2192 ").Append(e.PerformanceState ?? Resource.DiscreteGPUControl_PerformanceState_Unknown);

        if (e.State is GPUState.NvidiaGpuNotFound)
        {
            IsGpuActive = false;
            IsGpuInactive = false;
            IsGpuPoweredOff = false;
            _discreteGPUStatusDescription.Text = "-";
            ToolTip.SetTip(_gpuInfoButton, null);
            _gpuInfoButton.IsEnabled = false;
            IsVisible = false;
            return;
        }

        // Unknown is a normal transient state while NVAPI is starting. Keep
        // the card in the graphics section until the first real state arrives.
        IsVisible = true;

        if (e.State is GPUState.Unknown)
        {
            IsGpuActive = false;
            IsGpuInactive = false;
            IsGpuPoweredOff = false;
            _discreteGPUStatusDescription.Text = "-";
            ToolTip.SetTip(_gpuInfoButton, null);
            _gpuInfoButton.IsEnabled = false;
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
                foreach (var line in FormatProcessListLines(e.Processes.Select(TryGetProcessName)))
                    processesStringBuilder.AppendLine().Append(line);
            }
            else
            {
                processesStringBuilder.Append(Resource.DiscreteGPUControl_NoProcesses);
            }

            IsGpuActive = true;
            IsGpuInactive = false;
            IsGpuPoweredOff = false;
            _discreteGPUStatusDescription.Text = Resource.Active;
            ToolTip.SetTip(_gpuInfoButton, tooltipStringBuilder.AppendLine().AppendLine().Append(processesStringBuilder).ToString());
            _gpuInfoButton.IsEnabled = true;
        }
        else if (e.State is GPUState.PoweredOff)
        {
            IsGpuActive = false;
            IsGpuInactive = false;
            IsGpuPoweredOff = true;
            _discreteGPUStatusDescription.Text = Resource.PoweredOff;
            ToolTip.SetTip(_gpuInfoButton, tooltipStringBuilder.ToString());
            _gpuInfoButton.IsEnabled = true;
        }
        else
        {
            IsGpuActive = false;
            IsGpuInactive = true;
            IsGpuPoweredOff = false;
            _discreteGPUStatusDescription.Text = Resource.Inactive;
            ToolTip.SetTip(_gpuInfoButton, tooltipStringBuilder.ToString());
            _gpuInfoButton.IsEnabled = true;
        }

        CanDeactivateGpu = e.State is GPUState.Active or GPUState.Inactive;
        _killAppsMenuItem.IsEnabled = e.State is GPUState.Active;
        _restartGPUMenuItem.IsEnabled = e.State is GPUState.Active or GPUState.Inactive;

        if (e.State is GPUState.Active or GPUState.Inactive)
        {
            _deactivateGPUButtonText.SetResourceReference(TextBlock.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
            _deactivateGPUButtonIcon.SetResourceReference(TextBlock.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
        }
        else
        {
            _deactivateGPUButtonText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorDisabledBrush");
            _deactivateGPUButtonIcon.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorDisabledBrush");
        }

        IsGpuContentReady = true;
    });

    internal static string? TryGetProcessName(Process process)
    {
        try
        {
            return process.HasExited ? null : process.ProcessName;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Groups same-name GPU processes (multi-process apps like msedge) into one tooltip
    /// line each, with a "× N" count suffix. Sorted by count desc, then name.
    /// Display-only: the upstream PID list must stay ungrouped for KillGPUProcessesAsync.
    /// </summary>
    internal static IEnumerable<string> FormatProcessListLines(IEnumerable<string?> processNames)
    {
        return processNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name!, StringComparer.CurrentCultureIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => g.Count() > 1 ? $"  • {g.Key} × {g.Count()}" : $"  • {g.Key}");
    }

    private void DeactivateGPUButton_Click(object sender, RoutedEventArgs e)
    {
        if (_deactivateGPUButton.ContextMenu is null)
            return;

        // AVALONIA: ContextMenu has no PlacementTarget/Placement — it opens at the pointer position.
        _deactivateGPUButton.ContextMenu.Open();
    }

    private async void KillAppsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var originalCanDeactivate = CanDeactivateGpu;

        try
        {
            CanDeactivateGpu = false;

            await _gpuController.KillGPUProcessesAsync();
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to kill GPU processes.", ex);
        }
        finally
        {
            CanDeactivateGpu = originalCanDeactivate;
        }
    }

    private async void RestartGPUMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var originalCanDeactivate = CanDeactivateGpu;

        try
        {
            CanDeactivateGpu = false;

            await _gpuController.RestartGPUAsync();
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to restart GPU.", ex);
        }
        finally
        {
            CanDeactivateGpu = originalCanDeactivate;
        }
    }
}
}
