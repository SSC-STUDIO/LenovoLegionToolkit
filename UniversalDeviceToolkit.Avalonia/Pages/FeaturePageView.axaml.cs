using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class FeaturePageView : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly FeaturePageDescriptor _descriptor;
    private readonly Action<string>? _actionRequested;
    private bool _isApplying;
    private FeaturePageState? _lastState;
    private PluginCatalogState? _pluginCatalog;
    private bool _showCleanup;

    protected FeaturePageView(
        IPlatformServices platformServices,
        FeaturePageDescriptor descriptor,
        Action<string>? actionRequested = null)
    {
        _platformServices = platformServices;
        _descriptor = descriptor;
        _actionRequested = actionRequested;
        InitializeComponent();
        PageTitle.Text = descriptor.Title;
        PageDescription.Text = descriptor.Description;
        PageIcon.IconIdentifier = descriptor.IconIdentifier;
        StatusTitle.Text = AvaloniaLocalization.GetString("FeaturePage_StatusTitle", "Feature status");
        StatusMessage.Text = AvaloniaLocalization.GetString("FeaturePage_Loading", "Reading the current platform capability...");
        AutomationProperties.SetName(this, descriptor.Title);
        PluginSearchBox.TextChanged += PluginSearchBox_TextChanged;
        PluginFilterBox.SelectionChanged += PluginFilterBox_SelectionChanged;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshStateAsync();
    }

    private async Task RefreshStateAsync()
    {
        try
        {
            _isApplying = true;
            var state = await _platformServices.GetFeaturePageStateAsync(_descriptor.RouteKey);
            _lastState = state;
            var isOptimization = string.Equals(_descriptor.RouteKey, "WindowsOptimization", StringComparison.Ordinal);
            var isPluginExtensions = string.Equals(_descriptor.RouteKey, "PluginExtensions", StringComparison.Ordinal);
            OptimizationToolbar.IsVisible = isOptimization || isPluginExtensions;
            PluginCatalogToolbar.IsVisible = isPluginExtensions;
            OptimizationModeButton.IsVisible = isOptimization;
            CleanupModeButton.IsVisible = isOptimization;
            NetworkAccelerationButton.IsVisible = isOptimization;
            DriverDownloadButton.IsVisible = isOptimization;
            PluginImportButton.IsVisible = isPluginExtensions;
            if (isPluginExtensions)
            {
                _pluginCatalog = await _platformServices.GetPluginCatalogAsync().ConfigureAwait(true);
                PluginUpdateButton.IsEnabled = _pluginCatalog.Plugins.Any(plugin =>
                    plugin.IsInstalled && plugin.AvailableUpdateVersion is not null && !plugin.IsSystemPlugin);
                if (!string.IsNullOrWhiteSpace(_pluginCatalog.StatusMessage))
                    state = state with { StatusMessage = _pluginCatalog.StatusMessage };
            }
            _lastState = state;
            StatusTitle.Text = state.IsAvailable
                ? AvaloniaLocalization.GetString("FeaturePage_Available", "Available")
                : AvaloniaLocalization.GetString("FeaturePage_Unsupported", "Unavailable on this device");
            StatusMessage.Text = string.IsNullOrWhiteSpace(state.StatusMessage)
                ? _descriptor.UnsupportedReason
                : state.StatusMessage;
            StatusCard.Background = GetResource<IBrush>(state.IsAvailable ? "StatusSuccessBackgroundBrush" : "StatusInfoBackgroundBrush");
            StatusCard.BorderBrush = GetResource<IBrush>(state.IsAvailable ? "StatusSuccessBrush" : "StatusInfoBrush");

            RenderFeatureItems(state);
        }
        catch (Exception ex)
        {
            StatusTitle.Text = AvaloniaLocalization.GetString("FeaturePage_LoadFailed", "Unable to load feature state");
            StatusMessage.Text = ex.Message;
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void RenderFeatureItems(FeaturePageState state)
    {
        FeatureItems.Items.Clear();
        if (_descriptor.RouteKey.Equals("PluginExtensions", StringComparison.Ordinal))
        {
            RenderPluginCatalog();
            UpdateOptimizationCommands(state);
            return;
        }
        var visibleActions = state.Actions.Where(action =>
            !_descriptor.RouteKey.Equals("WindowsOptimization", StringComparison.Ordinal)
            || (_showCleanup
                ? FeatureActionContract.IsCleanupAction(action.Key)
                    || action.Key is FeatureActionContract.CleanupScanActionKey
                    or FeatureActionContract.CleanupRunActionKey
                    or FeatureActionContract.CleanupClearActionKey
                : !FeatureActionContract.IsCleanupAction(action.Key)
                    && action.Key != FeatureActionContract.CleanupScanActionKey
                    && action.Key != FeatureActionContract.CleanupRunActionKey
                    && action.Key != FeatureActionContract.CleanupClearActionKey)).ToArray();

        string? lastCategory = null;
        foreach (var item in visibleActions)
        {
            if (!string.IsNullOrWhiteSpace(item.Category)
                && !string.Equals(lastCategory, item.Category, StringComparison.Ordinal))
            {
                FeatureItems.Items.Add(CreateCategoryHeading(item.Category));
                lastCategory = item.Category;
            }

            FeatureItems.Items.Add(CreateFeatureCard(item));
        }

        if (_showCleanup && _descriptor.RouteKey.Equals("WindowsOptimization", StringComparison.Ordinal))
            FeatureItems.Items.Add(CreateCustomCleanupPanel(state.CustomCleanupRules ?? []));

        if (visibleActions.Length == 0)
            FeatureItems.Items.Add(CreateEmptyState());
        UpdateOptimizationCommands(state);
    }

    private void RenderPluginCatalog()
    {
        var catalog = _pluginCatalog;
        if (catalog is null || catalog.Plugins.Count == 0)
        {
            FeatureItems.Items.Add(CreateEmptyState());
            return;
        }

        var filter = PluginSearchBox.Text?.Trim() ?? string.Empty;
        var filterMode = PluginFilterBox.SelectedIndex;
        var matches = catalog.Plugins.Where(plugin =>
            (filterMode switch
            {
                1 => plugin.IsInstalled,
                2 => !plugin.IsInstalled,
                3 => plugin.IsInstalled && plugin.AvailableUpdateVersion is not null,
                _ => true,
            })
            && (string.IsNullOrWhiteSpace(filter)
                || plugin.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || plugin.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || plugin.Description.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || plugin.Tags.Any(tag => tag.Contains(filter, StringComparison.CurrentCultureIgnoreCase))));

        var count = 0;
        foreach (var plugin in matches)
        {
            FeatureItems.Items.Add(CreatePluginCatalogCard(plugin));
            count++;
        }

        if (count == 0)
        {
            FeatureItems.Items.Add(new LocalizedTextBlock
            {
                Text = AvaloniaLocalization.GetString(
                    "PluginExtensionsPage_NoSearchResults",
                    "No plugin extensions match the current search."),
                Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 3,
            });
        }
    }

    private Border CreatePluginCatalogCard(PluginCatalogItem plugin)
    {
        var title = new LocalizedTextBlock
        {
            Text = plugin.Name,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var details = new LocalizedTextBlock
        {
            Text = string.Join(" ", new[]
            {
                plugin.Description,
                string.IsNullOrWhiteSpace(plugin.Author) ? null : $"{plugin.Author} |",
                $"v{plugin.Version}",
                plugin.AvailableUpdateVersion is null ? null : $"-> v{plugin.AvailableUpdateVersion}",
            }.Where(value => !string.IsNullOrWhiteSpace(value))),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var capabilities = new LocalizedTextBlock
        {
            Text = FormatPluginCapabilities(plugin),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        };
        var copy = new StackPanel { Spacing = 4, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(details);
        copy.Children.Add(capabilities);

        var commands = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Top,
        };
        if (plugin.IsInstalled)
        {
            if (plugin.AvailableUpdateVersion is not null && !plugin.IsSystemPlugin)
            {
                var update = CreatePluginButton(
                    AvaloniaLocalization.GetString("PluginExtensionsPage_Update", "Update"),
                    $"plugin-update:{plugin.Id}",
                    plugin.Name);
                commands.Children.Add(update);
            }

            if (plugin.SupportsFeaturePage)
                commands.Children.Add(CreatePluginButton(
                    AvaloniaLocalization.GetString("PluginExtensionsPage_Open", "Open"),
                    $"plugin-open:{plugin.Id}",
                    plugin.Name));

            if (!plugin.IsSystemPlugin)
                commands.Children.Add(CreatePluginButton(
                    AvaloniaLocalization.GetString("PluginExtensionsPage_Uninstall", "Uninstall"),
                    $"plugin-uninstall:{plugin.Id}",
                    plugin.Name));
        }
        else
        {
            commands.Children.Add(CreatePluginButton(
                AvaloniaLocalization.GetString("PluginExtensionsPage_Install", "Install"),
                $"plugin-install:{plugin.Id}",
                plugin.Name));
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 14 };
        grid.Children.Add(new NavigationIcon
        {
            IconIdentifier = string.IsNullOrWhiteSpace(plugin.Id) ? "Apps24" : "Apps24",
            FontSize = GetResource<double>("IconSizeLG"),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Top,
        });
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);
        Grid.SetColumn(commands, 2);
        grid.Children.Add(commands);
        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8),
            Child = grid,
        };
        AutomationProperties.SetName(card, plugin.Name);
        ToolTip.SetTip(card, plugin.Details ?? plugin.Description);
        return card;
    }

    private Button CreatePluginButton(string content, string actionKey, string pluginName)
    {
        var button = new Button { Content = content, MinWidth = 82 };
        AutomationProperties.SetName(button, $"{content} {pluginName}");
        button.Tag = actionKey;
        button.Click += PluginActionButton_Click;
        return button;
    }

    private string FormatPluginCapabilities(PluginCatalogItem plugin)
    {
        var capabilities = new List<string>();
        if (plugin.SupportsSettingsPage)
            capabilities.Add(AvaloniaLocalization.GetString("PluginExtensionsPage_CapabilitySettings", "Settings"));
        if (plugin.SupportsFeaturePage)
            capabilities.Add(AvaloniaLocalization.GetString("PluginExtensionsPage_CapabilityQuickOpen", "Feature page"));
        if (plugin.SupportsOptimizationActions)
            capabilities.Add(AvaloniaLocalization.GetString("PluginExtensionsPage_CapabilityOptimize", "Optimization"));
        return capabilities.Count == 0
            ? AvaloniaLocalization.GetString("PluginExtensionsPage_NoCapabilities", "No UI capabilities reported")
            : string.Join(" | ", capabilities);
    }

    private void UpdateOptimizationCommands(FeaturePageState state)
    {
        if (!OptimizationToolbar.IsVisible)
            return;

        if (!string.Equals(_descriptor.RouteKey, "WindowsOptimization", StringComparison.Ordinal))
        {
            OptimizationCommands.IsVisible = false;
            CleanupCommands.IsVisible = false;
            return;
        }

        OptimizationCommands.IsVisible = !_showCleanup;
        CleanupCommands.IsVisible = _showCleanup;
        var cleanupSelected = state.Actions.Any(item => FeatureActionContract.IsCleanupAction(item.Key) && item.IsSelected);
        foreach (var button in CleanupCommands.Children.OfType<Button>())
        {
            var actionKey = button.Tag?.ToString();
            button.IsEnabled = actionKey switch
            {
                FeatureActionContract.CleanupClearActionKey => cleanupSelected,
                FeatureActionContract.CleanupScanActionKey => cleanupSelected,
                FeatureActionContract.CleanupRunActionKey => cleanupSelected,
                _ => true,
            };
        }
    }

    private void PluginSearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_lastState is not null && _descriptor.RouteKey.Equals("PluginExtensions", StringComparison.Ordinal))
            RenderFeatureItems(_lastState);
    }

    private void PluginFilterBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_lastState is not null && _descriptor.RouteKey.Equals("PluginExtensions", StringComparison.Ordinal))
            RenderFeatureItems(_lastState);
    }

    private async void PluginRefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            _pluginCatalog = await _platformServices.GetPluginCatalogAsync(forceRefresh: true);
            if (_lastState is not null)
                RenderFeatureItems(_lastState);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async void PluginUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            var updates = _pluginCatalog?.Plugins
                .Where(plugin => plugin.IsInstalled && plugin.AvailableUpdateVersion is not null && !plugin.IsSystemPlugin)
                .ToArray() ?? [];
            foreach (var plugin in updates)
                await _platformServices.UpdatePluginAsync(plugin.Id);

            _pluginCatalog = await _platformServices.GetPluginCatalogAsync(forceRefresh: true);
            if (_lastState is not null)
                RenderFeatureItems(_lastState);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async void PluginActionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string actionKey } || _isApplying)
            return;

        _isApplying = true;
        try
        {
            var accepted = actionKey.StartsWith("plugin-update:", StringComparison.OrdinalIgnoreCase)
                ? await _platformServices.UpdatePluginAsync(actionKey["plugin-update:".Length..])
                : actionKey.StartsWith("plugin-install:", StringComparison.OrdinalIgnoreCase)
                    ? await _platformServices.InstallPluginAsync(actionKey["plugin-install:".Length..])
                : await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, actionKey, true);
            if (accepted)
            {
                if (actionKey.StartsWith("plugin-open:", StringComparison.OrdinalIgnoreCase))
                    _actionRequested?.Invoke(actionKey);
                _pluginCatalog = await _platformServices.GetPluginCatalogAsync();
                if (_lastState is not null)
                    RenderFeatureItems(_lastState);
            }
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void OptimizationModeButton_Click(object? sender, RoutedEventArgs e)
    {
        _showCleanup = false;
        if (_lastState is not null)
            RenderFeatureItems(_lastState);
    }

    private void CleanupModeButton_Click(object? sender, RoutedEventArgs e)
    {
        _showCleanup = true;
        if (_lastState is not null)
            RenderFeatureItems(_lastState);
    }

    private void NetworkAccelerationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var window = new Window
        {
            Title = AvaloniaLocalization.GetString("NetworkAccelerationPage_Title", "Network acceleration"),
            Width = 760,
            Height = 680,
            MinWidth = 620,
            MinHeight = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new NetworkAccelerationPage(_platformServices),
        };
        window.Show(owner);
    }

    private void DriverDownloadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var window = new Window
        {
            Title = AvaloniaLocalization.GetString("WindowsOptimizationPage_Tab_DriverDownload", "Driver downloads"),
            Width = 860,
            Height = 720,
            MinWidth = 680,
            MinHeight = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DriverDownloadPage(_platformServices),
        };
        window.Show(owner);
    }

    private async void PluginImportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not TopLevel topLevel)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = AvaloniaLocalization.GetString(
                "PluginExtensionsPage_SelectPluginFiles",
                "Select plugin ZIP files"),
            FileTypeFilter =
            [new FilePickerFileType("Plugin ZIP") { Patterns = ["*.zip"] }],
        });

        if (files.Count == 0)
            return;

        var imported = 0;
        foreach (var file in files)
        {
            if (await _platformServices.ImportPluginAsync(file.Path.LocalPath))
                imported++;
        }

        if (imported == 0)
        {
            ToolTip.SetTip(
                PluginImportButton,
                AvaloniaLocalization.GetString(
                    "PluginExtensionsPage_BulkImportFailed",
                    "No plugin packages could be imported."));
            return;
        }

        ToolTip.SetTip(
            PluginImportButton,
            string.Format(
                AvaloniaLocalization.GetString(
                    "PluginExtensionsPage_BulkImportSuccessMessage",
                    "Imported {0} plugin package(s)."),
                imported));
        await RefreshStateAsync();
    }

    private async void OptimizationCommandButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string actionKey } || _isApplying)
            return;

        _isApplying = true;
        try
        {
            if (await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, actionKey, true))
                await RefreshStateAsync();
        }
        finally
        {
            _isApplying = false;
        }
    }

    private Border CreateFeatureCard(FeatureActionItem item)
    {
        Control action;
        if (item.IsToggle)
        {
            var toggle = new CheckBox
            {
                IsChecked = item.IsSelected,
                IsEnabled = item.IsEnabled,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 48,
            };
            AutomationProperties.SetAutomationId(toggle, $"Avalonia{_descriptor.RouteKey}_{item.Key}Toggle");
            AutomationProperties.SetName(toggle, item.Title);
            ToolTip.SetTip(toggle, item.Description);
            toggle.IsCheckedChanged += async (_, _) =>
            {
                if (_isApplying || toggle.IsChecked is not bool selected)
                    return;
                var accepted = await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, item.Key, selected);
                if (!accepted)
                    ToolTip.SetTip(toggle, item.Description + " " + item.Status);
                else
                    await RefreshStateAsync();
            };
            action = toggle;
        }
        else
        {
            var button = new Button
            {
                Content = item.Status,
                IsEnabled = item.IsEnabled,
                MinWidth = 120,
                VerticalAlignment = VerticalAlignment.Top,
            };
            AutomationProperties.SetAutomationId(button, $"Avalonia{_descriptor.RouteKey}_{item.Key}Action");
            AutomationProperties.SetName(button, item.Title);
            ToolTip.SetTip(button, item.Description);
            button.Click += async (_, _) =>
            {
                var accepted = await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, item.Key, true);
                if (!accepted)
                    ToolTip.SetTip(button, item.Description + " " + item.Status);
                else
                {
                    _actionRequested?.Invoke(item.Key);
                    await RefreshStateAsync();
                }
            };
            action = button;
        }

        var title = new LocalizedTextBlock
        {
            Text = item.Title,
            FontSize = GetResource<double>("FontSizeBody"),
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = item.Description,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var copy = new StackPanel { Spacing = 4, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(description);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 14 };
        var icon = new NavigationIcon
        {
            IconIdentifier = item.IsToggle ? "ToggleRight24" : _descriptor.PrimaryActionIcon,
            FontSize = GetResource<double>("IconSizeLG"),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Top,
        };
        grid.Children.Add(icon);
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);
        Grid.SetColumn(action, 2);
        grid.Children.Add(action);

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8),
            Child = grid,
        };
        AutomationProperties.SetName(card, item.Title);
        return card;
    }

    private Border CreateEmptyState() => new()
    {
        Background = GetResource<IBrush>("CardBackgroundBrush"),
        BorderBrush = GetResource<IBrush>("CardBorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
        Padding = new Thickness(16),
        Child = new LocalizedTextBlock
        {
            Text = AvaloniaLocalization.GetString("FeaturePage_NoActions", "No actions were reported by the platform adapter."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        },
    };

    private Border CreateCustomCleanupPanel(IReadOnlyList<CustomCleanupRuleItem> rules)
    {
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new LocalizedTextBlock
        {
            Text = AvaloniaLocalization.GetString(
                "WindowsOptimizationPage_CustomCleanup_Header",
                "Custom cleanup"),
            FontSize = GetResource<double>("FontSizeSubsection"),
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        content.Children.Add(new LocalizedTextBlock
        {
            Text = AvaloniaLocalization.GetString(
                "WindowsOptimizationPage_CustomCleanup_Description",
                "Choose folders and file extensions to include in custom cleanup."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        });

        var rulesPanel = new StackPanel { Spacing = 6 };
        if (rules.Count == 0)
        {
            rulesPanel.Children.Add(new LocalizedTextBlock
            {
                Text = AvaloniaLocalization.GetString(
                    "WindowsOptimizationPage_CustomCleanup_Empty",
                    "No custom cleanup rules have been added."),
                Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 2,
            });
        }
        else
        {
            for (var index = 0; index < rules.Count; index++)
            {
                var rule = rules[index];
                rulesPanel.Children.Add(CreateCustomCleanupRuleRow(rule, index));
            }
        }

        content.Children.Add(rulesPanel);

        var commands = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var add = new Button
        {
            Content = AvaloniaLocalization.GetString(
                "WindowsOptimizationPage_CustomCleanup_Add", "Add"),
        };
        AutomationProperties.SetAutomationId(add, "AvaloniaCustomCleanupAddButton");
        add.Click += async (_, _) => await AddCustomCleanupRuleAsync(rules);
        commands.Children.Add(add);

        var clear = new Button
        {
            Content = AvaloniaLocalization.GetString(
                "WindowsOptimizationPage_CustomCleanup_Clear", "Clear"),
            IsEnabled = rules.Count > 0,
        };
        AutomationProperties.SetAutomationId(clear, "AvaloniaCustomCleanupClearButton");
        clear.Click += async (_, _) => await ClearCustomCleanupRulesAsync();
        commands.Children.Add(clear);
        content.Children.Add(commands);

        var panel = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
            Padding = new Thickness(16),
            Child = content,
        };
        AutomationProperties.SetName(panel, AvaloniaLocalization.GetString(
            "WindowsOptimizationPage_CustomCleanup_Header", "Custom cleanup"));
        return panel;
    }

    private Border CreateCustomCleanupRuleRow(CustomCleanupRuleItem rule, int index)
    {
        var extensions = rule.Extensions.Count == 0
            ? AvaloniaLocalization.GetString("CustomCleanupRule_NoExtensions", "All extensions")
            : string.Join(", ", rule.Extensions);
        var summary = $"{extensions} · "
            + (rule.Recursive
                ? AvaloniaLocalization.GetString("WindowsOptimizationPage_CustomCleanup_Recursive_Label", "Recursive")
                : AvaloniaLocalization.GetString("CustomCleanupRule_NonRecursive", "Current folder only"));

        var copy = new StackPanel { Spacing = 2, MinWidth = 0 };
        copy.Children.Add(new LocalizedTextBlock
        {
            Text = rule.DirectoryPath,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        });
        copy.Children.Add(new LocalizedTextBlock
        {
            Text = summary,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        });

        var edit = new Button
        {
            Content = AvaloniaLocalization.GetString(
                "WindowsOptimizationPage_CustomCleanup_Edit", "Edit"),
            VerticalAlignment = VerticalAlignment.Top,
        };
        AutomationProperties.SetAutomationId(edit, $"AvaloniaCustomCleanupEditButton{index}");
        edit.Click += async (_, _) => await EditCustomCleanupRuleAsync(index, rule);

        var remove = new Button
        {
            Content = AvaloniaLocalization.GetString(
                "WindowsOptimizationPage_CustomCleanup_Remove", "Remove"),
            VerticalAlignment = VerticalAlignment.Top,
        };
        AutomationProperties.SetAutomationId(remove, $"AvaloniaCustomCleanupRemoveButton{index}");
        remove.Click += async (_, _) => await RemoveCustomCleanupRuleAsync(index);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 8,
        };
        grid.Children.Add(copy);
        Grid.SetColumn(edit, 1);
        grid.Children.Add(edit);
        Grid.SetColumn(remove, 2);
        grid.Children.Add(remove);
        return new Border
        {
            Background = GetResource<IBrush>("SubtleFillColorTertiaryBrush"),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusControl"),
            Padding = new Thickness(10, 8),
            Child = grid,
        };
    }

    private async Task AddCustomCleanupRuleAsync(IReadOnlyList<CustomCleanupRuleItem> rules)
    {
        var rule = await ShowCustomCleanupRuleEditorAsync(null);
        if (rule is null)
            return;

        var updated = rules.ToList();
        updated.Add(rule);
        await SaveCustomCleanupRulesAsync(updated);
    }

    private async Task EditCustomCleanupRuleAsync(int index, CustomCleanupRuleItem current)
    {
        var rule = await ShowCustomCleanupRuleEditorAsync(current);
        if (rule is null || _lastState?.CustomCleanupRules is not { } rules || index < 0 || index >= rules.Count)
            return;

        var updated = rules.ToList();
        updated[index] = rule;
        await SaveCustomCleanupRulesAsync(updated);
    }

    private async Task RemoveCustomCleanupRuleAsync(int index)
    {
        if (_lastState?.CustomCleanupRules is not { } rules || index < 0 || index >= rules.Count)
            return;

        var updated = rules.ToList();
        updated.RemoveAt(index);
        await SaveCustomCleanupRulesAsync(updated);
    }

    private Task ClearCustomCleanupRulesAsync() => SaveCustomCleanupRulesAsync([]);

    private async Task SaveCustomCleanupRulesAsync(IReadOnlyList<CustomCleanupRuleItem> rules)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            if (await _platformServices.SaveCustomCleanupRulesAsync(rules))
                await RefreshStateAsync();
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async Task<CustomCleanupRuleItem?> ShowCustomCleanupRuleEditorAsync(CustomCleanupRuleItem? current)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return null;

        var editor = new CustomCleanupRuleEditorWindow(current);
        return await editor.ShowDialog<CustomCleanupRuleItem?>(owner);
    }

    private LocalizedTextBlock CreateCategoryHeading(string category) => new()
    {
        Text = category,
        FontSize = GetResource<double>("FontSizeSubsection"),
        FontWeight = FontWeight.Medium,
        Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
        Margin = new Thickness(0, 10, 0, 2),
        OverflowMode = LocalizedOverflowMode.Wrap,
        MaxLines = 2,
    };

    private T GetResource<T>(object key)
    {
        if (this.TryFindResource(key, out var value) && value is T typedValue)
            return typedValue;

        if (typeof(T) == typeof(IBrush))
            return (T)(object)new SolidColorBrush(Colors.Transparent);
        if (typeof(T) == typeof(double))
            return (T)(object)14d;
        if (typeof(T) == typeof(CornerRadius))
            return (T)(object)new CornerRadius(8);
        throw new InvalidOperationException($"Missing Avalonia resource '{key}'.");
    }

    protected sealed record FeaturePageDescriptor(
        string RouteKey,
        string Title,
        string Description,
        string IconIdentifier,
        string UnsupportedReason,
        string PrimaryActionTitle,
        string PrimaryActionDescription,
        string PrimaryActionIcon,
        bool PrimaryActionEnabled = false);

}

public sealed class ActionsPage(IPlatformServices services) : AutomationPage(services);

public sealed class WindowsOptimizationPage(IPlatformServices services) : FeaturePageView(services, new(
    "WindowsOptimization",
    "System optimization",
    "Review Windows optimization actions and their current state.",
    "Gauge24",
    "Windows optimization actions require the Windows optimization adapter.",
    "Review optimization actions",
    "Apply or roll back supported Windows optimization actions from the shared optimization service.",
    "Gauge24"));

public sealed class PluginExtensionsPage : FeaturePageView
{
    public PluginExtensionsPage(IPlatformServices services, Action<string>? actionRequested = null)
        : base(services, new(
            "PluginExtensions",
            "Plugin Extensions",
            "Discover and manage optional plugin extensions.",
            "Apps24",
            "Plugin discovery and installation require the plugin service adapter.",
            "Review installed extensions",
            "Manage installed and registered extensions through the shared plugin manager.",
            "Apps24"), actionRequested)
    {
    }
}

/// <summary>
/// Small host-neutral editor for one custom cleanup rule. The folder picker is used
/// for the directory while extensions remain editable as a comma/semicolon list.
/// </summary>
internal sealed class CustomCleanupRuleEditorWindow : Window
{
    private readonly TextBox _directory;
    private readonly TextBox _extensions;
    private readonly CheckBox _recursive;
    private readonly TextBlock _error;

    public CustomCleanupRuleEditorWindow(CustomCleanupRuleItem? current)
    {
        Title = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Title", "Custom cleanup rule");
        Width = 620;
        MinWidth = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _directory = new TextBox
        {
            Text = current?.DirectoryPath ?? string.Empty,
            Watermark = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Folder_Label", "Folder"),
            MinWidth = 360,
        };
        _extensions = new TextBox
        {
            Text = current is null ? string.Empty : string.Join(", ", current.Extensions),
            Watermark = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Extensions_Hint", ".tmp, .log"),
            MinWidth = 360,
        };
        _recursive = new CheckBox
        {
            Content = AvaloniaLocalization.GetString(
                "CustomCleanupRuleWindow_Recursive_Label", "Include subfolders"),
            IsChecked = current?.Recursive ?? true,
        };
        _error = new TextBlock
        {
            Foreground = new SolidColorBrush(Colors.IndianRed),
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap,
        };

        var browse = new Button
        {
            Content = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Browse_Button", "Browse"),
        };
        browse.Click += BrowseButton_Click;

        var save = new Button
        {
            Content = AvaloniaLocalization.GetString("Common_Save", "Save"),
            IsDefault = true,
        };
        save.Click += SaveButton_Click;
        var cancel = new Button
        {
            Content = AvaloniaLocalization.GetString("Common_Cancel", "Cancel"),
            IsCancel = true,
        };

        var directoryRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
        directoryRow.Children.Add(_directory);
        Grid.SetColumn(browse, 1);
        directoryRow.Children.Add(browse);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Folder_Label", "Folder"),
                    FontWeight = FontWeight.Medium,
                },
                directoryRow,
                new TextBlock
                {
                    Text = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Extensions_Label", "Extensions"),
                    FontWeight = FontWeight.Medium,
                },
                _extensions,
                new TextBlock
                {
                    Text = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Extensions_Hint", "Separate extensions with commas or semicolons."),
                    Foreground = new SolidColorBrush(Colors.Gray),
                    TextWrapping = TextWrapping.Wrap,
                },
                _recursive,
                _error,
                buttons,
            },
        };
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = AvaloniaLocalization.GetString("CustomCleanupRuleWindow_Browse_Button", "Choose folder"),
        });
        var path = folders.FirstOrDefault()?.Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
            _directory.Text = path;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_directory.Text))
        {
            _error.Text = AvaloniaLocalization.GetString(
                "CustomCleanupRuleWindow_Error_Folder", "Choose a folder first.");
            _error.IsVisible = true;
            return;
        }

        var extensions = (_extensions.Text ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Close(new CustomCleanupRuleItem(_directory.Text.Trim(), extensions, _recursive.IsChecked == true));
    }
}
