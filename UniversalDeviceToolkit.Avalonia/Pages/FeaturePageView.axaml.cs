using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class FeaturePageView : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly FeaturePageDescriptor _descriptor;
    private readonly Action<string>? _actionRequested;
    private readonly Action? _pluginCatalogChanged;
    private bool _isApplying;
    private FeaturePageState? _lastState;
    private PluginCatalogState? _pluginCatalog;
    private bool _showCleanup;
    private bool _coordinatorWasActive;
    private ActionDetailsWindow? _actionDetailsWindow;

    /// <summary>
    /// Raised after a plugin whose optimization category was installed completes,
    /// so the shell can focus the matching optimization area. The catalog model
    /// does not expose the category key, so the payload is the plugin id.
    /// </summary>
    public event Action<string>? FocusOptimizationCategoryRequested;

    protected FeaturePageView(
        IPlatformServices platformServices,
        FeaturePageDescriptor descriptor,
        Action<string>? actionRequested = null,
        Action? pluginCatalogChanged = null)
    {
        _platformServices = platformServices;
        _descriptor = descriptor;
        _actionRequested = actionRequested;
        _pluginCatalogChanged = pluginCatalogChanged;
        InitializeComponent();
        PageTitle.Text = descriptor.Title;
        PageDescription.Text = descriptor.Description;
        PageIcon.IconIdentifier = descriptor.IconIdentifier;
        StatusTitle.Text = AvaloniaLocalization.GetString("FeaturePage_StatusTitle", "Feature status");
        StatusMessage.Text = AvaloniaLocalization.GetString("FeaturePage_Loading", "Reading the current platform capability...");
        AutomationProperties.SetName(this, descriptor.Title);
        PluginSearchBox.TextChanged += PluginSearchBox_TextChanged;
        PluginFilterBox.SelectionChanged += PluginFilterBox_SelectionChanged;
        AvaloniaPluginInstallCoordinator.Current.Changed += OnPluginInstallCoordinatorChanged;
        if (PluginLanguageService.Current is { } languageService)
            languageService.LanguagesChanged += OnPluginLanguagesChanged;
        Loaded += OnLoaded;
        Unloaded += (_, _) =>
        {
            _actionDetailsWindow?.Close();
            _actionDetailsWindow = null;
            AvaloniaPluginInstallCoordinator.Current.Changed -= OnPluginInstallCoordinatorChanged;
            if (PluginLanguageService.Current is { } language)
                language.LanguagesChanged -= OnPluginLanguagesChanged;
        };
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
                PluginInstallButton.IsEnabled = _pluginCatalog.Plugins.Any(plugin =>
                    !plugin.IsInstalled && !plugin.IsSystemPlugin);
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
            var statusBrushKey = state.IsAvailable ? "StatusSuccessBrush" : "StatusCriticalBrush";
            StatusCard.Background = GetResource<IBrush>(state.IsAvailable ? "StatusSuccessBackgroundBrush" : "StatusCriticalBackgroundBrush");
            StatusCard.BorderBrush = GetResource<IBrush>(statusBrushKey);
            StatusIconBackground.Background = GetResource<IBrush>(statusBrushKey);

            RenderFeatureItems(state);
        }
        catch (Exception ex)
        {
            StatusTitle.Text = AvaloniaLocalization.GetString("FeaturePage_LoadFailed", "Unable to load feature state");
            StatusMessage.Text = ex.Message;
            StatusCard.Background = GetResource<IBrush>("StatusCriticalBackgroundBrush");
            StatusCard.BorderBrush = GetResource<IBrush>("StatusCriticalBrush");
            StatusIconBackground.Background = GetResource<IBrush>("StatusCriticalBrush");
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
                    && action.Key != FeatureActionContract.CleanupClearActionKey
                    && action.Key != FeatureActionContract.OptimizationApplyRecommendedActionKey
                    && action.Key != FeatureActionContract.OptimizationApplySelectedActionKey
                    && action.Key != FeatureActionContract.OptimizationClearSelectionActionKey)).ToArray();

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
        if (catalog is null)
        {
            CatalogOfflineBanner.IsVisible = false;
            CatalogSummaryPanel.IsVisible = false;
            FeatureItems.Items.Add(CreateEmptyState());
            return;
        }

        CatalogOfflineBanner.IsVisible = !catalog.IsAvailable;
        if (!catalog.IsAvailable)
            CatalogOfflineMessage.Text = catalog.StatusMessage;
        RenderCatalogSummary(catalog);

        if (catalog.Plugins.Count == 0)
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

    private void RenderCatalogSummary(PluginCatalogState catalog)
    {
        CatalogSummaryPanel.Children.Clear();
        var installed = catalog.Plugins.Count(plugin => plugin.IsInstalled);
        var updates = catalog.Plugins.Count(plugin =>
            plugin.IsInstalled && plugin.AvailableUpdateVersion is not null && !plugin.IsSystemPlugin);
        if (installed == 0 && updates == 0)
        {
            CatalogSummaryPanel.IsVisible = false;
            return;
        }

        CatalogSummaryPanel.IsVisible = true;
        CatalogSummaryPanel.Children.Add(CreateSummaryChip(
            string.Format(
                AvaloniaLocalization.GetString(
                    "PluginExtensionsPage_InstalledCount",
                    "{0} installed"),
                installed),
            "success"));
        if (updates > 0)
        {
            CatalogSummaryPanel.Children.Add(CreateSummaryChip(
                string.Format(
                    AvaloniaLocalization.GetString(
                        "PluginExtensionsPage_UpdatesCount",
                        "{0} updates available"),
                    updates),
                "warning"));
        }
    }

    private Border CreateSummaryChip(string text, string variant)
    {
        var chip = new Border
        {
            Child = new LocalizedTextBlock
            {
                Text = text,
                OverflowMode = LocalizedOverflowMode.Ellipsis,
                MaxLines = 1,
            },
        };
        chip.Classes.Add("badge");
        chip.Classes.Add(variant);
        AutomationProperties.SetName(chip, text);
        return chip;
    }

    private Border CreatePluginCatalogCard(PluginCatalogItem plugin)
    {
        var coordinator = AvaloniaPluginInstallCoordinator.Current;
        var isBusy = coordinator.IsQueuedOrActive(plugin.Id);
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
                update.IsEnabled = !isBusy;
                commands.Children.Add(update);
            }

            if (plugin.SupportsFeaturePage)
            {
                var open = CreatePluginButton(
                    AvaloniaLocalization.GetString("PluginExtensionsPage_Open", "Open"),
                    $"plugin-open:{plugin.Id}",
                    plugin.Name);
                open.IsEnabled = !isBusy;
                commands.Children.Add(open);
            }

            if (plugin.SupportsSettingsPage)
            {
                var configure = CreatePluginButton(
                    AvaloniaLocalization.GetString("PluginExtensionsPage_Configure", "Configure"),
                    $"plugin-settings:{plugin.Id}",
                    plugin.Name);
                configure.IsEnabled = !isBusy;
                commands.Children.Add(configure);
            }

            if (!plugin.IsSystemPlugin)
            {
                var uninstall = CreatePluginButton(
                    AvaloniaLocalization.GetString("PluginExtensionsPage_Uninstall", "Uninstall"),
                    $"plugin-uninstall:{plugin.Id}",
                    plugin.Name);
                uninstall.IsEnabled = !isBusy;
                commands.Children.Add(uninstall);
            }
        }
        else
        {
            var install = CreatePluginButton(
                AvaloniaLocalization.GetString("PluginExtensionsPage_Install", "Install"),
                $"plugin-install:{plugin.Id}",
                plugin.Name);
            install.IsEnabled = !isBusy;
            commands.Children.Add(install);
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 14 };
        grid.Children.Add(new NavigationIcon
        {
            IconIdentifier = "Apps24",
            FontSize = GetResource<double>("IconSizeLG"),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Top,
        });
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);
        Grid.SetColumn(commands, 2);
        grid.Children.Add(commands);

        var cardContent = new StackPanel { Spacing = 10 };
        cardContent.Children.Add(grid);
        if (isBusy)
        {
            var progress = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 6,
                IsVisible = true,
            };
            AutomationProperties.SetName(progress, $"{plugin.Name} install progress");
            cardContent.Children.Add(progress);
            var status = new LocalizedTextBlock
            {
                Text = coordinator.StatusText ?? AvaloniaLocalization.GetString(
                    "PluginExtensionsPage_Processing",
                    "Processing..."),
                Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                OverflowMode = LocalizedOverflowMode.Ellipsis,
                MaxLines = 1,
            };
            cardContent.Children.Add(status);
        }

        cardContent.Children.Add(CreatePluginDetailsExpander(plugin));

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8),
            Child = cardContent,
            ContextMenu = CreatePluginContextMenu(plugin),
        };
        AutomationProperties.SetName(card, plugin.Name);
        ToolTip.SetTip(card, plugin.Details ?? plugin.Description);
        return card;
    }

    private Expander CreatePluginDetailsExpander(PluginCatalogItem plugin)
    {
        var content = new StackPanel { Spacing = 8 };
        if (!string.IsNullOrWhiteSpace(plugin.Details))
        {
            content.Children.Add(CreateDetailRow(
                AvaloniaLocalization.GetString("PluginExtensionsPage_DetailsLabel", "Details"),
                plugin.Details));
        }

        content.Children.Add(CreateDetailRow(
            AvaloniaLocalization.GetString("PluginExtensionsPage_VersionLabel", "Version"),
            $"v{plugin.Version}"));
        if (!string.IsNullOrWhiteSpace(plugin.Author))
        {
            content.Children.Add(CreateDetailRow(
                AvaloniaLocalization.GetString("PluginExtensionsPage_AuthorLabel", "Developer"),
                plugin.Author));
        }

        if (plugin.Tags.Count > 0)
        {
            content.Children.Add(CreateDetailRow(
                AvaloniaLocalization.GetString("PluginExtensionsPage_TagsLabel", "Tags"),
                string.Join(", ", plugin.Tags)));
        }

        if (plugin.AvailableUpdateVersion is not null)
        {
            content.Children.Add(CreateDetailRow(
                AvaloniaLocalization.GetString("PluginExtensionsPage_UpdateAvailableLabel", "Update"),
                $"v{plugin.AvailableUpdateVersion}"));
        }

        AddPluginLanguageSelector(plugin, content);

        var expander = new Expander
        {
            Header = AvaloniaLocalization.GetString("PluginExtensionsPage_DetailsLabel", "Details"),
            Content = content,
        };
        AutomationProperties.SetName(expander, $"{plugin.Name} details");
        return expander;
    }

    private Border CreateDetailRow(string label, string value)
    {
        var content = new StackPanel { Spacing = 2, MinWidth = 0 };
        content.Children.Add(new LocalizedTextBlock
        {
            Text = label,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        });
        content.Children.Add(new LocalizedTextBlock
        {
            Text = value,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 6,
        });
        return new Border
        {
            Background = GetResource<IBrush>("SubtleFillColorTertiaryBrush"),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusControl"),
            Padding = new Thickness(10, 8),
            Child = content,
        };
    }

    private void AddPluginLanguageSelector(PluginCatalogItem plugin, StackPanel content)
    {
        var languageService = PluginLanguageService.Current;
        if (languageService is null)
            return;

        var cultures = new List<(string? CultureName, string Display)>
        {
            (null, AvaloniaLocalization.GetString(
                "PluginExtensionsPage_FollowAppLanguage",
                "Follow app language")),
        };
        foreach (var culture in LocalizationCatalog.SupportedCultures)
            cultures.Add((culture.Name, LocalizationCatalog.GetDisplayName(culture)));

        var combo = new ComboBox
        {
            ItemsSource = cultures.Select(item => item.Display).ToArray(),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxWidth = 300,
        };
        var current = languageService.GetLanguage(plugin.Id);
        combo.SelectedIndex = 0;
        for (var index = 0; index < cultures.Count; index++)
        {
            if (string.Equals(cultures[index].CultureName, current, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = index;
                break;
            }
        }

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedIndex < 0 || combo.SelectedIndex >= cultures.Count)
                return;

            try
            {
                languageService.SetLanguage(plugin.Id, cultures[combo.SelectedIndex].CultureName);
            }
            catch
            {
                // Language overrides are best effort and must never break the store.
            }
        };

        content.Children.Add(new LocalizedTextBlock
        {
            Text = AvaloniaLocalization.GetString(
                "PluginExtensionsPage_LanguageLabel",
                "Plugin language"),
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        });
        content.Children.Add(combo);
    }

    private ContextMenu CreatePluginContextMenu(PluginCatalogItem plugin)
    {
        var copyId = new MenuItem
        {
            Header = AvaloniaLocalization.GetString(
                "PluginExtensionsPage_CopyPluginId",
                "Copy plugin ID"),
        };
        copyId.Click += async (_, _) => await CopyPluginIdAsync(plugin.Id);
        var menu = new ContextMenu();
        menu.Items.Add(copyId);
        return menu;
    }

    private async Task CopyPluginIdAsync(string pluginId)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(pluginId);
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
        var hasPendingOptimizationChanges = state.Actions.Any(item =>
            !FeatureActionContract.IsCleanupAction(item.Key)
            && item.Key != FeatureActionContract.CleanupScanActionKey
            && item.Key != FeatureActionContract.CleanupRunActionKey
            && item.Key != FeatureActionContract.CleanupClearActionKey
            && item.Key != FeatureActionContract.OptimizationApplyRecommendedActionKey
            && item.Key != FeatureActionContract.OptimizationApplySelectedActionKey
            && item.Key != FeatureActionContract.OptimizationClearSelectionActionKey
            && item.IsToggle
            && item.IsSelected != item.IsApplied);
        ApplySelectedButton.IsEnabled = hasPendingOptimizationChanges;
        // WPF keeps Clear selection available in optimization mode. It only
        // changes the pending intent; it never rolls back the system directly.
        OptimizationClearButton.IsEnabled = true;
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
            _pluginCatalogChanged?.Invoke();
            if (_lastState is not null)
                RenderFeatureItems(_lastState);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void CatalogRetryButton_Click(object? sender, RoutedEventArgs e)
    {
        PluginRefreshButton_Click(sender, e);
    }

    private void OnPluginLanguagesChanged()
    {
        if (_descriptor.RouteKey.Equals("PluginExtensions", StringComparison.Ordinal)
            && _lastState is not null)
        {
            if (Dispatcher.UIThread.CheckAccess())
                RenderFeatureItems(_lastState);
            else
                Dispatcher.UIThread.Post(() => RenderFeatureItems(_lastState));
        }
    }

    private void OnPluginInstallCoordinatorChanged()
    {
        if (!_descriptor.RouteKey.Equals("PluginExtensions", StringComparison.Ordinal))
            return;

        var coordinator = AvaloniaPluginInstallCoordinator.Current;
        var wasActive = _coordinatorWasActive;
        var isActive = coordinator.IsActive;
        _coordinatorWasActive = isActive;
        if (Dispatcher.UIThread.CheckAccess())
            HandlePluginCoordinatorChanged(wasActive, isActive);
        else
            Dispatcher.UIThread.Post(() => HandlePluginCoordinatorChanged(wasActive, isActive));
    }

    private void HandlePluginCoordinatorChanged(bool wasActive, bool isActive)
    {
        if (_lastState is not null)
            RenderFeatureItems(_lastState);

        var coordinator = AvaloniaPluginInstallCoordinator.Current;
        var active = coordinator.IsActive;
        if (PluginInstallButton is not null && PluginUpdateButton is not null && _pluginCatalog is not null)
        {
            PluginInstallButton.IsEnabled = !active
                && _pluginCatalog.Plugins.Any(plugin => !plugin.IsInstalled && !plugin.IsSystemPlugin);
            PluginUpdateButton.IsEnabled = !active
                && _pluginCatalog.Plugins.Any(plugin =>
                    plugin.IsInstalled && plugin.AvailableUpdateVersion is not null && !plugin.IsSystemPlugin);
        }

        if (wasActive && !isActive)
            _ = RefreshCatalogAfterCoordinatorAsync();
    }

    private async Task RefreshCatalogAfterCoordinatorAsync()
    {
        try
        {
            _pluginCatalog = await _platformServices.GetPluginCatalogAsync();
            _pluginCatalogChanged?.Invoke();
            if (_lastState is not null)
                RenderFeatureItems(_lastState);
        }
        catch
        {
            // The store UI keeps its last known catalog when the refresh fails.
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
                .Select(plugin => plugin.Id)
                .ToArray() ?? [];
            if (updates.Length > 0)
            {
                var result = await AvaloniaPluginInstallCoordinator.Current.UpdateAsync(
                    updates,
                    pluginId => _platformServices.UpdatePluginAsync(pluginId));
                if (!result.Succeeded)
                    ShowPluginOperationFailure(result);
            }

            _pluginCatalog = await _platformServices.GetPluginCatalogAsync(forceRefresh: true);
            _pluginCatalogChanged?.Invoke();
            if (_lastState is not null)
                RenderFeatureItems(_lastState);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async void PluginInstallButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            var installable = _pluginCatalog?.Plugins
                .Where(plugin => !plugin.IsInstalled && !plugin.IsSystemPlugin)
                .Select(plugin => plugin.Id)
                .ToArray() ?? [];
            PluginOperationBatchResult? result = null;
            if (installable.Length > 0)
            {
                result = await AvaloniaPluginInstallCoordinator.Current.InstallAsync(
                    installable,
                    pluginId => _platformServices.InstallPluginAsync(pluginId));
                if (!result.Succeeded)
                    ShowPluginOperationFailure(result);
            }

            _pluginCatalog = await _platformServices.GetPluginCatalogAsync(forceRefresh: true);
            _pluginCatalogChanged?.Invoke();
            if (_lastState is not null)
                RenderFeatureItems(_lastState);
            foreach (var pluginId in result?.Operations
                         .Where(operation => operation.Succeeded)
                         .Select(operation => operation.PluginId) ?? [])
                RaiseOptimizationCategoryFocusRequest(pluginId);
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
            var isPluginOpen = actionKey.StartsWith("plugin-open:", StringComparison.OrdinalIgnoreCase);
            var isPluginSettings = actionKey.StartsWith("plugin-settings:", StringComparison.OrdinalIgnoreCase);
            var installedPluginId = actionKey.StartsWith("plugin-install:", StringComparison.OrdinalIgnoreCase)
                ? actionKey["plugin-install:".Length..]
                : null;
            var accepted = actionKey.StartsWith("plugin-update:", StringComparison.OrdinalIgnoreCase)
                ? await RunCoordinatedPluginActionAsync(
                    [actionKey["plugin-update:".Length..]],
                    pluginId => _platformServices.UpdatePluginAsync(pluginId),
                    "update")
                : actionKey.StartsWith("plugin-install:", StringComparison.OrdinalIgnoreCase)
                    ? await RunCoordinatedPluginActionAsync(
                        [actionKey["plugin-install:".Length..]],
                        pluginId => _platformServices.InstallPluginAsync(pluginId),
                        "install")
                    : actionKey.StartsWith("plugin-uninstall:", StringComparison.OrdinalIgnoreCase)
                        ? await RunCoordinatedPluginActionAsync(
                            [actionKey["plugin-uninstall:".Length..]],
                            pluginId => _platformServices.SetFeatureActionAsync(
                                _descriptor.RouteKey,
                                $"plugin-uninstall:{pluginId}",
                                true),
                            "uninstall")
                        : isPluginOpen || isPluginSettings
                            ? true
                            : await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, actionKey, true);
            if (accepted)
            {
                if (isPluginOpen || isPluginSettings)
                    _actionRequested?.Invoke(actionKey);
                else
                {
                    _pluginCatalog = await _platformServices.GetPluginCatalogAsync();
                    _pluginCatalogChanged?.Invoke();
                    if (_lastState is not null)
                        RenderFeatureItems(_lastState);
                    if (installedPluginId is not null)
                        RaiseOptimizationCategoryFocusRequest(installedPluginId);
                }
            }
            else
            {
                ToolTip.SetTip(
                    PluginCatalogToolbar,
                    AvaloniaLocalization.GetString(
                        "PluginExtensionsPage_ActionFailed",
                        "The plugin operation could not be completed."));
            }
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async Task<bool> RunCoordinatedPluginActionAsync(
        IReadOnlyCollection<string> pluginIds,
        Func<string, Task<bool>> installer,
        string operation)
    {
        if (pluginIds.Count == 0)
            return true;

        var coordinator = AvaloniaPluginInstallCoordinator.Current;
        PluginOperationBatchResult result;
        switch (operation)
        {
            case "update":
                result = await coordinator.UpdateAsync(pluginIds, installer);
                break;
            case "uninstall":
                result = await coordinator.UninstallAsync(pluginIds, installer);
                break;
            default:
                result = await coordinator.InstallAsync(pluginIds, installer);
                break;
        }

        if (!result.Succeeded)
            ShowPluginOperationFailure(result);
        return result.Succeeded;
    }

    private void ShowPluginOperationFailure(PluginOperationBatchResult result)
    {
        StatusTitle.Text = AvaloniaLocalization.GetString(
            "PluginExtensionsPage_ActionFailed",
            "The plugin operation could not be completed.");
        StatusMessage.Text = result.ErrorMessage ?? StatusTitle.Text;
        StatusCard.Background = GetResource<IBrush>("StatusCriticalBackgroundBrush");
        StatusCard.BorderBrush = GetResource<IBrush>("StatusCriticalBrush");
        StatusIconBackground.Background = GetResource<IBrush>("StatusCriticalBrush");
    }

    private void RaiseOptimizationCategoryFocusRequest(string pluginId)
    {
        var plugin = _pluginCatalog?.Plugins
            .FirstOrDefault(candidate => candidate.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        if (plugin is { IsInstalled: true, SupportsOptimizationActions: true })
            FocusOptimizationCategoryRequested?.Invoke(plugin.Id);
    }

    private void OptimizationModeButton_Click(object? sender, RoutedEventArgs e)
    {
        _showCleanup = false;
        HideCleanupProgress();
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
        _pluginCatalogChanged?.Invoke();
    }

    private async void OptimizationCommandButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string actionKey } || _isApplying)
            return;

        _isApplying = true;
        try
        {
            if (actionKey.Equals(FeatureActionContract.CleanupRunActionKey, StringComparison.OrdinalIgnoreCase))
            {
                await RunCleanupAsync();
                return;
            }

            if (await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, actionKey, true))
                await RefreshStateAsync();
        }
        finally
        {
            _isApplying = false;
        }
    }

    private async Task RunCleanupAsync()
    {
        var selectedItems = _lastState?.Actions.Count(action =>
            FeatureActionContract.IsCleanupAction(action.Key) && action.IsSelected) ?? 0;
        if (selectedItems == 0)
            return;

        var progressToastId = AvaloniaProgressToastHelper.Start(
            AvaloniaLocalization.GetString("SettingsPage_WindowsOptimization_Title", "System optimization"));
        var runningText = AvaloniaLocalization.GetString(
            "WindowsOptimizationPage_RunningCleanup",
            "Running cleanup...");
        ShowCleanupProgress(runningText);
        AvaloniaProgressToastHelper.Update(progressToastId, 0, runningText);

        var progress = new Progress<CleanupProgressState>(state =>
        {
            var percentage = state.TotalCount == 0
                ? 0
                : (int)Math.Round(state.CompletedCount * 100d / state.TotalCount);
            var progressText = string.Format(
                AvaloniaLocalization.GetString(
                    "WindowsOptimizationPage_RunningStep",
                    "Running {0}..."),
                state.ActionTitle);
            CleanupProgressText.Text = progressText;
            CleanupProgressPercentage.Text = $"{percentage}%";
            CleanupProgressBar.IsIndeterminate = false;
            CleanupProgressBar.Value = percentage;
            AvaloniaProgressToastHelper.Update(progressToastId, percentage, progressText);
        });

        CleanupExecutionResult result;
        try
        {
            result = await _platformServices.RunSelectedCleanupAsync(progress);
        }
        catch
        {
            result = new CleanupExecutionResult(selectedItems, 0, selectedItems, 0, TimeSpan.Zero, []);
        }
        finally
        {
            AvaloniaProgressToastHelper.Complete(progressToastId);
        }

        ShowCleanupSummary(FormatCleanupSummary(result));
        await RefreshStateAsync();
    }

    private static string FormatCleanupSummary(CleanupExecutionResult result)
    {
        if (result.FailedCount == 0 && result.SucceededCount > 0)
        {
            return AvaloniaProgressToastHelper.FormatCleanupSummary(
                result.SucceededCount,
                result.Elapsed,
                result.FreedBytes);
        }

        if (result.HasPartialFailure)
        {
            return string.Format(
                AvaloniaLocalization.GetString(
                    "WindowsOptimizationPage_CleanupPartialSummary",
                    "Freed {0}. {1} succeeded, {2} failed."),
                AvaloniaProgressToastHelper.FormatBytes(result.FreedBytes),
                result.SucceededCount,
                result.FailedCount);
        }

        return AvaloniaLocalization.GetString(
            "SettingsPage_WindowsOptimization_Cleanup_Error",
            "Cleanup failed.");
    }

    private void ShowCleanupProgress(string runningText)
    {
        CleanupProgressCard.IsVisible = true;
        CleanupProgressText.Text = runningText;
        CleanupProgressPercentage.Text = string.Empty;
        CleanupProgressBar.IsVisible = true;
        CleanupProgressBar.IsIndeterminate = true;
        CleanupSummaryText.IsVisible = false;
    }

    private void ShowCleanupSummary(string summary)
    {
        CleanupProgressCard.IsVisible = true;
        CleanupProgressText.Text = AvaloniaLocalization.GetString(
            "WindowsOptimizationPage_CleanupCompleted",
            "Cleanup finished");
        CleanupProgressPercentage.Text = "100%";
        CleanupProgressBar.IsVisible = false;
        CleanupSummaryText.Text = summary;
        CleanupSummaryText.IsVisible = true;
    }

    private void HideCleanupProgress()
    {
        CleanupProgressCard.IsVisible = false;
        CleanupProgressText.Text = string.Empty;
        CleanupProgressPercentage.Text = string.Empty;
        CleanupProgressBar.IsVisible = false;
        CleanupSummaryText.IsVisible = false;
        CleanupSummaryText.Text = string.Empty;
    }

    private Border CreateFeatureCard(FeatureActionItem item)
    {
        var statusKind = FeatureActionContract.ResolveStatusKind(item);
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
                bool accepted;
                try
                {
                    accepted = await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, item.Key, selected);
                }
                catch (Exception ex)
                {
                    accepted = false;
                    if (System.Diagnostics.Debugger.IsAttached)
                        System.Diagnostics.Debug.WriteLine($"Feature action failed: {ex.Message}");
                }

                if (!accepted)
                {
                    _isApplying = true;
                    try
                    {
                        toggle.IsChecked = item.IsSelected;
                    }
                    finally
                    {
                        _isApplying = false;
                    }
                    ToolTip.SetTip(toggle, item.Description + " " + item.Status);
                }
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
        if (statusKind != FeatureActionStatusKind.Neutral && !string.IsNullOrWhiteSpace(item.Status))
            copy.Children.Add(CreateStatusBadge(item.Status, statusKind));

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
        if (_descriptor.RouteKey.Equals("WindowsOptimization", StringComparison.Ordinal))
            card.PointerPressed += (_, args) => ActionCard_PointerPressed(item, args);
        AutomationProperties.SetName(card, item.Title);
        return card;
    }

    private void ActionCard_PointerPressed(FeatureActionItem item, PointerPressedEventArgs args)
    {
        if (args.ClickCount < 2)
            return;

        args.Handled = true;
        if (_actionDetailsWindow is { IsVisible: true })
        {
            _actionDetailsWindow.Activate();
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        _actionDetailsWindow = new ActionDetailsWindow(item);
        _actionDetailsWindow.Closed += (_, _) => _actionDetailsWindow = null;
        _actionDetailsWindow.Show(owner);
    }

    private Border CreateStatusBadge(string status, FeatureActionStatusKind statusKind)
    {
        var badge = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new LocalizedTextBlock
            {
                Text = status,
                OverflowMode = LocalizedOverflowMode.Ellipsis,
                MaxLines = 1,
                MaxWidth = 220,
            },
        };
        badge.Classes.Add("badge");
        badge.Classes.Add(statusKind switch
        {
            FeatureActionStatusKind.Success => "success",
            FeatureActionStatusKind.Warning => "warning",
            FeatureActionStatusKind.Critical => "danger",
            _ => "info",
        });
        ToolTip.SetTip(badge, status);
        AutomationProperties.SetName(badge, status);
        return badge;
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
    public PluginExtensionsPage(
        IPlatformServices services,
        Action<string>? actionRequested = null,
        Action? pluginCatalogChanged = null)
        : base(services, new(
            "PluginExtensions",
            "Plugin Extensions",
            "Discover and manage optional plugin extensions.",
            "Apps24",
            "Plugin discovery and installation require the plugin service adapter.",
            "Review installed extensions",
            "Manage installed and registered extensions through the shared plugin manager.",
            "Apps24"), actionRequested, pluginCatalogChanged)
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
            IsChecked = current?.Recursive ?? false,
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
