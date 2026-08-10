using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Assets;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Pipeline;
using UniversalDeviceToolkit.Lib.Automation.Utils;
using UniversalDeviceToolkit.Lib.Utils;
using MenuItem = UniversalDeviceToolkit.Avalonia.Controls.MenuItem;

namespace UniversalDeviceToolkit.Avalonia.Utils;

public class TrayHelper : IDisposable
{
    private const string NAVIGATION_TAG = "navigation";
    private const string STATIC_TAG = "static";
    private const string AUTOMATION_TAG = "automation";

    private readonly ThemeManager _themeManager = IoCContainer.Resolve<ThemeManager>();
    private readonly AutomationProcessor _automationProcessor = IoCContainer.Resolve<AutomationProcessor>();

    private readonly ContextMenu _contextMenu = new()
    {
        FontSize = 14
    };

    private readonly Action _bringToForeground;

    private NotifyIcon? _notifyIcon;

    public TrayHelper(NavigationStore navigation, Action bringToForeground, bool trayTooltipEnabled)
    {
        _ = trayTooltipEnabled;
        _bringToForeground = bringToForeground;

        InitializeStaticItems(navigation);

        var notifyIcon = new NotifyIcon
        {
            Icon = AssetResources.icon,
            Text = AppIdentity.DisplayName
        };

        notifyIcon.ContextMenu = _contextMenu;
        notifyIcon.OnClick += (_, _) => _bringToForeground();
        _notifyIcon = notifyIcon;

        // Status readout (power mode / GPU / battery / update) is intentionally not shown
        // in the tray menu — dashboard and main window cover that.

        _themeManager.ThemeApplied += ThemeManager_ThemeApplied;
    }

    public async Task InitializeAsync()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => InitializeAsync());
            return;
        }

        var pipelines = await _automationProcessor.GetPipelinesAsync();
        pipelines = pipelines.Where(p => p.Trigger is null).ToList();
        await SetAutomationItemsAsync(pipelines);

        _automationProcessor.PipelinesChanged += AutomationProcessor_PipelinesChanged;
    }

    private void ThemeManager_ThemeApplied(object? sender, EventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() => _contextMenu.Resources = App.Current.Resources);
    }

    private void AutomationProcessor_PipelinesChanged(object? sender, List<AutomationPipeline> pipelines)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() => SetAutomationItemsAsync(pipelines));
    }

    private void InitializeStaticItems(NavigationStore navigation)
    {
        foreach (var navigationItem in navigation.Items.OfType<NavigationItem>())
        {
            var navigationMenuItem = new MenuItem
            {
                Icon = new SymbolIcon { Symbol = navigationItem.Icon },
                Header = navigationItem.Content,
                Tag = NAVIGATION_TAG
            };
            navigationMenuItem.Click += async (_, _) =>
            {
                _contextMenu.Close();
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
            _contextMenu.Close();
            _bringToForeground();
        };
        _contextMenu.Items.Add(openMenuItem);

        var closeMenuItem = new MenuItem { Header = Resource.Close, Tag = STATIC_TAG };
        closeMenuItem.Click += async (_, _) =>
        {
            _contextMenu.Close();
            await App.Current.ShutdownAsync(true);
        };
        _contextMenu.Items.Add(closeMenuItem);
    }

    private async Task SetAutomationItemsAsync(List<AutomationPipeline> pipelines)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => SetAutomationItemsAsync(pipelines));
            return;
        }

        foreach (var item in _contextMenu.Items.OfType<Control>().Where(mi => AUTOMATION_TAG.Equals(mi.Tag)).ToArray())
            _contextMenu.Items.Remove(item);

        pipelines = pipelines.Where(p => p.Trigger is null).Reverse().ToList();

        var supportedPipelines = new List<AutomationPipeline>();
        foreach (var pipeline in pipelines)
        {
            try
            {
                var supportChecks = await Task.WhenAll(pipeline.Steps.Select(s => s.IsSupportedAsync()));
                if (supportChecks.All(s => s))
                    supportedPipelines.Add(pipeline);
            }
            catch (Exception ex)
            {
                Log.Instance.TraceOnce(
                    "tray-pipeline-support",
                    "Tray automation pipeline support check failed; skipping pipeline.",
                    ex);
            }
        }

        // Insert automation block after navigation items, before Open/Close.
        var insertIndex = 0;
        for (var i = 0; i < _contextMenu.Items.Count; i++)
        {
            if (_contextMenu.Items[i] is Control control && NAVIGATION_TAG.Equals(control.Tag))
                insertIndex = i + 1;
            else if (_contextMenu.Items[i] is Separator separator && NAVIGATION_TAG.Equals(separator.Tag))
                insertIndex = i + 1;
        }

        if (supportedPipelines.Count != 0)
        {
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
                Icon = new SymbolIcon { Symbol = icon },
                // Stable keys (e.g. __udt.quickAction.deactivateGpu) must not surface in the tray.
                Header = PipelineNameLocalizer.LocalizeStoredName(pipeline.Name) ?? pipeline.Name ?? Resource.Unnamed,
                Tag = AUTOMATION_TAG
            };
            item.Click += async (_, _) =>
            {
                try
                {
                    await _automationProcessor.RunNowAsync(pipeline);
                }
                catch
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Failed to run automation pipeline from tray");
                }
            };

            _contextMenu.Items.Insert(insertIndex, item);
            insertIndex++;
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

        _themeManager.ThemeApplied -= ThemeManager_ThemeApplied;
        _automationProcessor.PipelinesChanged -= AutomationProcessor_PipelinesChanged;

        if (_notifyIcon is not null)
            _notifyIcon.Visible = false;

        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
