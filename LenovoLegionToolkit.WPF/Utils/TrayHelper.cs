using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation;
using LenovoLegionToolkit.Lib.Automation.Pipeline;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Assets;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Windows.Utils;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace LenovoLegionToolkit.WPF.Utils;

public class TrayHelper : IDisposable
{
    private const string NAVIGATION_TAG = "navigation";
    private const string STATIC_TAG = "static";
    private const string AUTOMATION_TAG = "automation";
    private const string STATUS_TAG = "status";

    private readonly ThemeManager _themeManager = IoCContainer.Resolve<ThemeManager>();
    private readonly AutomationProcessor _automationProcessor = IoCContainer.Resolve<AutomationProcessor>();

    private readonly ContextMenu _contextMenu = new()
    {
        FontSize = 14
    };

    private readonly Action _bringToForeground;

    private NotifyIcon? _notifyIcon;

    public TrayHelper(INavigation navigation, Action bringToForeground, bool trayTooltipEnabled)
    {
        _bringToForeground = bringToForeground;

        InitializeStaticItems(navigation);

        var notifyIcon = new NotifyIcon
        {
            Icon = AssetResources.icon,
            Text = Resource.AppName
        };

        // 不再使用 ToolTipWindow，悬停时只显示应用名称
        // 状态信息已合并到右键菜单中
        // if (trayTooltipEnabled)
        //     notifyIcon.ToolTipWindow = async () => await StatusWindow.CreateAsync();

        notifyIcon.ContextMenu = _contextMenu;
        notifyIcon.OnClick += (_, _) => _bringToForeground();
        _notifyIcon = notifyIcon;

        _contextMenu.Opened += async (_, _) => await UpdateStatusItemsAsync();

        _themeManager.ThemeApplied += (_, _) => _contextMenu.Resources = App.Current.Resources;
    }

    public async Task InitializeAsync()
    {
        var pipelines = await _automationProcessor.GetPipelinesAsync();
        pipelines = pipelines.Where(p => p.Trigger is null).ToList();
        await SetAutomationItemsAsync(pipelines);

        _automationProcessor.PipelinesChanged += async (_, p) => await SetAutomationItemsAsync(p);
    }

    private void InitializeStaticItems(INavigation navigation)
    {
        foreach (var navigationItem in navigation.Items.OfType<NavigationItem>())
        {
            var navigationMenuItem = new MenuItem
            {
                SymbolIcon = navigationItem.Icon,
                Header = navigationItem.Content,
                Tag = NAVIGATION_TAG
            };
            navigationMenuItem.Click += async (_, _) =>
            {
                _contextMenu.IsOpen = false;
                _bringToForeground();

                await Task.Delay(TimeSpan.FromMilliseconds(500));
                navigation.Navigate(navigationItem.PageTag);
            };
            _contextMenu.Items.Add(navigationMenuItem);
        }

        _contextMenu.Items.Add(new Separator { Tag = NAVIGATION_TAG });

        var openMenuItem = new MenuItem { Header = Resource.Open, Tag = STATIC_TAG };
        openMenuItem.Click += (_, _) =>
        {
            _contextMenu.IsOpen = false;
            _bringToForeground();
        };
        _contextMenu.Items.Add(openMenuItem);

        var closeMenuItem = new MenuItem { Header = Resource.Close, Tag = STATIC_TAG };
        closeMenuItem.Click += async (_, _) =>
        {
            _contextMenu.IsOpen = false;
            await App.Current.ShutdownAsync(true);
        };
        _contextMenu.Items.Add(closeMenuItem);
    }

    private async Task UpdateStatusItemsAsync()
    {
        // Remove existing status items
        foreach (var item in _contextMenu.Items.OfType<Control>().Where(mi => STATUS_TAG.Equals(mi.Tag)).ToArray())
            _contextMenu.Items.Remove(item);

        try
        {
            var powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
            var godModeController = IoCContainer.Resolve<GodModeController>();
            var gpuController = IoCContainer.Resolve<GPUController>();
            var batteryFeature = IoCContainer.Resolve<BatteryFeature>();
            var updateChecker = IoCContainer.Resolve<UpdateChecker>();

            var statusItems = new List<Control>();
            var insertIndex = 0;

            // Power Mode
            try
            {
                if (await powerModeFeature.IsSupportedAsync())
                {
                    var state = await powerModeFeature.GetStateAsync();
                    var powerModeItem = new MenuItem
                    {
                        Header = $"{Resource.StatusTrayPopup_PowerMode}: {state.GetDisplayName()}",
                        IsEnabled = false,
                        Tag = STATUS_TAG
                    };
                    statusItems.Add(powerModeItem);

                    if (state == PowerModeState.GodMode)
                    {
                        var presetName = await godModeController.GetActivePresetNameAsync();
                        var presetItem = new MenuItem
                        {
                            Header = $"{Resource.StatusTrayPopup_Preset}: {presetName ?? "-"}",
                            IsEnabled = false,
                            Tag = STATUS_TAG
                        };
                        statusItems.Add(presetItem);
                    }
                }
            }
            catch { /* Ignored */ }

            // GPU Status
            try
            {
                if (gpuController.IsSupported())
                {
                    var gpuStatus = await gpuController.RefreshNowAsync();
                    var gpuStateText = gpuStatus.State switch
                    {
                        GPUState.Active => Resource.Active,
                        GPUState.MonitorConnected => Resource.Active,
                        GPUState.PoweredOff => Resource.PoweredOff,
                        _ => Resource.Inactive
                    };
                    var gpuItem = new MenuItem
                    {
                        Header = $"{Resource.StatusTrayPopup_DiscreteGPU}: {gpuStateText}",
                        IsEnabled = false,
                        Tag = STATUS_TAG
                    };
                    statusItems.Add(gpuItem);

                    if (gpuStatus.State is GPUState.Active or GPUState.MonitorConnected or GPUState.Inactive)
                    {
                        var powerStateItem = new MenuItem
                        {
                            Header = $"{Resource.StatusTrayPopup_PowerState}: {gpuStatus.PerformanceState ?? "-"}",
                            IsEnabled = false,
                            Tag = STATUS_TAG
                        };
                        statusItems.Add(powerStateItem);
                    }
                }
            }
            catch { /* Ignored */ }

            // Battery Information
            try
            {
                var batteryInformation = Battery.GetBatteryInformation();
                var batteryItem = new MenuItem
                {
                    Header = $"{Resource.StatusTrayPopup_Battery}: {batteryInformation.BatteryPercentage}%",
                    IsEnabled = false,
                    Tag = STATUS_TAG
                };
                statusItems.Add(batteryItem);

                if (await batteryFeature.IsSupportedAsync())
                {
                    var batteryState = await batteryFeature.GetStateAsync();
                    var modeItem = new MenuItem
                    {
                        Header = $"{Resource.StatusTrayPopup_Mode}: {batteryState.GetDisplayName()}",
                        IsEnabled = false,
                        Tag = STATUS_TAG
                    };
                    statusItems.Add(modeItem);
                }

                var dischargeItem = new MenuItem
                {
                    Header = $"{Resource.StatusTrayPopup_DischargeRate}: {batteryInformation.DischargeRate / 1000.0:+0.00;-0.00;0.00} W",
                    IsEnabled = false,
                    Tag = STATUS_TAG
                };
                statusItems.Add(dischargeItem);

                var minDischargeItem = new MenuItem
                {
                    Header = $"{Resource.StatusTrayPopup_MinDischargeRate}: {batteryInformation.MinDischargeRate / 1000.0:+0.00;-0.00;0.00} W",
                    IsEnabled = false,
                    Tag = STATUS_TAG
                };
                statusItems.Add(minDischargeItem);

                var maxDischargeItem = new MenuItem
                {
                    Header = $"{Resource.StatusTrayPopup_MaxDischargeRate}: {batteryInformation.MaxDischargeRate / 1000.0:+0.00;-0.00;0.00} W",
                    IsEnabled = false,
                    Tag = STATUS_TAG
                };
                statusItems.Add(maxDischargeItem);
            }
            catch { /* Ignored */ }

            // Update Available
            try
            {
                var hasUpdate = await updateChecker.CheckAsync(false) is not null;
                if (hasUpdate)
                {
                    var updateItem = new MenuItem
                    {
                        Header = Resource.StatusTrayPopup_UpdateAvailable,
                        IsEnabled = false,
                        Tag = STATUS_TAG
                    };
                    statusItems.Add(updateItem);
                }
            }
            catch { /* Ignored */ }

            // Insert status items at the beginning
            if (statusItems.Count > 0)
            {
                insertIndex = 0;

                // Insert status items in reverse order (so they appear in correct order)
                for (int i = statusItems.Count - 1; i >= 0; i--)
                {
                    _contextMenu.Items.Insert(insertIndex, statusItems[i]);
                }

                // Insert separator after status items if there are other items
                if (_contextMenu.Items.Count > statusItems.Count)
                {
                    _contextMenu.Items.Insert(statusItems.Count, new Separator { Tag = STATUS_TAG });
                }
            }
        }
        catch { /* Ignored */ }
    }

    private async Task SetAutomationItemsAsync(List<AutomationPipeline> pipelines)
    {
        foreach (var item in _contextMenu.Items.OfType<Control>().Where(mi => AUTOMATION_TAG.Equals(mi.Tag)).ToArray())
            _contextMenu.Items.Remove(item);

        pipelines = pipelines.Where(p => p.Trigger is null).Reverse().ToList();

        // Filter out pipelines whose steps are not supported on this machine
        var supportedPipelines = new List<AutomationPipeline>();
        foreach (var pipeline in pipelines)
        {
            try
            {
                var supportChecks = await Task.WhenAll(pipeline.Steps.Select(s => s.IsSupportedAsync()));
                if (supportChecks.All(s => s))
                    supportedPipelines.Add(pipeline);
            }
            catch
            {
                // If any check fails, consider the pipeline unsupported
            }
        }

        if (supportedPipelines.Count != 0)
        {
            // Find where to insert (after status items and separator if any)
            var insertIndex = 0;
            for (int i = 0; i < _contextMenu.Items.Count; i++)
            {
                if (_contextMenu.Items[i] is Control control && STATUS_TAG.Equals(control.Tag))
                    insertIndex = i + 1;
                else if (_contextMenu.Items[i] is Separator separator && STATUS_TAG.Equals(separator.Tag))
                    insertIndex = i + 1;
            }
            _contextMenu.Items.Insert(insertIndex, new Separator { Tag = AUTOMATION_TAG });
            insertIndex++;
        }

        foreach (var pipeline in supportedPipelines)
        {
            var icon = Enum.TryParse<SymbolRegular>(pipeline.IconName, out var iconParsed)
                ? iconParsed
                : SymbolRegular.Play24;

            var item = new MenuItem
            {
                SymbolIcon = icon,
                Header = pipeline.Name ?? Resource.Unnamed,
                Tag = AUTOMATION_TAG
            };
            item.Click += async (_, _) =>
            {
                try
                {
                    await _automationProcessor.RunNowAsync(pipeline);
                }
                catch {  /* Ignored. */ }
            };

            // Find where to insert (after status items, status separator, and automation separator if any)
            var insertIndex = 0;
            for (int i = 0; i < _contextMenu.Items.Count; i++)
            {
                if (_contextMenu.Items[i] is Control control && (STATUS_TAG.Equals(control.Tag) || AUTOMATION_TAG.Equals(control.Tag)))
                    insertIndex = i + 1;
                else if (_contextMenu.Items[i] is Separator separator && (STATUS_TAG.Equals(separator.Tag) || AUTOMATION_TAG.Equals(separator.Tag)))
                    insertIndex = i + 1;
            }
            _contextMenu.Items.Insert(insertIndex, item);
        }
    }

    public void MakeVisible()
    {
        if (_notifyIcon is null)
            return;

        _notifyIcon.Visible = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_notifyIcon is not null)
            _notifyIcon.Visible = false;

        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
