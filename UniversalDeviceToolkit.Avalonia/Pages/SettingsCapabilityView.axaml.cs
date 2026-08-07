using Avalonia.Automation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class SettingsCapabilityView : UserControl
{
    private readonly string _pageKey;
    private readonly IAvaloniaSettingsService _settingsService;
    private bool _isApplying;
    private DispatcherTimer? _feedbackTimer;
    private DispatcherTimer? _updateRepositorySettingsTimer;
    private string? _pendingUpdateRepositoryOwner;
    private string? _pendingUpdateRepositoryName;

    protected SettingsCapabilityView(string pageKey, string titleFallback, string descriptionFallback)
    {
        _pageKey = pageKey;
        _settingsService = AvaloniaSettingsServiceFactory.Create();
        InitializeComponent();

        TitleBlock.Text = titleFallback;
        DescriptionBlock.Text = descriptionFallback;
        StatusTitleBlock.Text = AvaloniaLocalization.GetString(
            "Settings_PlatformUnavailable_Title",
            "Settings availability");
        AutomationProperties.SetName(this, titleFallback);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        control.Loaded -= OnLoaded;
        await RefreshPageAsync();
    }

    private async Task RefreshPageAsync()
    {
        try
        {
            _isApplying = true;
            var data = await _settingsService.GetPageAsync(_pageKey);
            TitleBlock.Text = data.Title;
            DescriptionBlock.Text = data.Description;
            AutomationProperties.SetName(this, data.Title);

            AvailabilityCard.IsVisible = !data.IsAvailable || !string.IsNullOrWhiteSpace(data.UnavailableReason);
            StatusTitleBlock.Text = data.IsAvailable
                ? AvaloniaLocalization.GetString("Settings_Page_StatusTitle", "Settings ready")
                : AvaloniaLocalization.GetString("Settings_PlatformUnavailable_Title", "Unavailable in this host");
            StatusMessageBlock.Text = data.UnavailableReason
                ?? AvaloniaLocalization.GetString("Settings_Page_StatusMessage", "Changes are saved immediately.");

            OptionsPanel.Children.Clear();
            var options = data.Options
                .Where(option => option.IsVisible)
                .ToList();
            if (_pageKey == "Application"
                && options.Any(option => option.Key == "HardwareSectionsVisible")
                && options.Any(option => option.Key == "HardwareSectionsOrder"))
            {
                options.Add(new AvaloniaSettingOption(
                    "HardwareSectionsConfigure",
                    AvaloniaLocalization.GetString("SensorSections_Configure_Title", "Configure sensor sections"),
                    AvaloniaLocalization.GetString("SensorSections_Configure_Description", "Choose visible dashboard sensor sections and their order."),
                    AvaloniaSettingEditor.Action,
                    data.IsAvailable,
                    ActionText: AvaloniaLocalization.GetString("Configure", "Configure")));
            }
            if (_pageKey == "Application"
                && options.Any(option => option.Key.StartsWith("Osd", StringComparison.OrdinalIgnoreCase)))
            {
                options.Add(new AvaloniaSettingOption(
                    "OsdConfigure",
                    AvaloniaLocalization.GetString("OsdSettings_Configure_Title", "Configure on-screen display"),
                    AvaloniaLocalization.GetString("OsdSettings_Configure_Description", "Configure the on-screen display appearance, thresholds, and sensor items."),
                    AvaloniaSettingEditor.Action,
                    data.IsAvailable,
                    ActionText: AvaloniaLocalization.GetString("Configure", "Configure")));
            }
            foreach (var option in options)
                OptionsPanel.Children.Add(CreateOptionCard(option));

            if (!data.Options.Any(option => option.IsVisible))
            {
                OptionsPanel.Children.Add(CreateEmptyState(data.IsAvailable));
            }
        }
        catch (Exception ex)
        {
            AvailabilityCard.IsVisible = true;
            StatusTitleBlock.Text = AvaloniaLocalization.GetString("Settings_Page_LoadFailed", "Unable to load settings");
            StatusMessageBlock.Text = ex.Message;
        }
        finally
        {
            _isApplying = false;
        }
    }

    private Control CreateOptionCard(AvaloniaSettingOption option)
    {
        var title = new Controls.LocalizedTextBlock
        {
            Text = option.Title,
            FontSize = (double)Resources["FontSizeBody"]!,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new Controls.LocalizedTextBlock
        {
            Text = option.Description,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var textStack = new StackPanel { Spacing = 3, MinWidth = 0 };
        textStack.Children.Add(title);
        textStack.Children.Add(description);
        if (!string.IsNullOrWhiteSpace(option.Warning))
        {
            var warning = new Controls.LocalizedTextBlock
            {
                Text = option.Warning,
                Foreground = GetResource<IBrush>("StatusWarningBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 3,
            };
            textStack.Children.Add(warning);
        }

        Control editor = option.Editor switch
        {
            AvaloniaSettingEditor.Toggle => CreateToggle(option),
            AvaloniaSettingEditor.Selection => CreateSelection(option),
            AvaloniaSettingEditor.MultiSelection => CreateMultiSelection(option),
            AvaloniaSettingEditor.Text => CreateTextBox(option),
            AvaloniaSettingEditor.Action => CreateAction(option),
            _ => new TextBlock { Text = option.Title },
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(textStack);
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = string.IsNullOrWhiteSpace(option.Warning)
                ? GetResource<IBrush>("CardBorderBrush")
                : GetResource<IBrush>("StatusWarningBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = (CornerRadius)Resources["CornerRadiusCard"]!,
            Padding = new Thickness(16),
            Child = grid,
        };
        if (!string.IsNullOrWhiteSpace(option.Warning))
            ToolTip.SetTip(card, option.Warning);
        AutomationProperties.SetName(card, option.Title);
        return card;
    }

    private CheckBox CreateToggle(AvaloniaSettingOption option)
    {
        var toggle = new CheckBox
        {
            IsChecked = option.BoolValue,
            IsEnabled = option.IsEnabled,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 48,
        };
        AutomationProperties.SetAutomationId(toggle, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(toggle, option.Title);
        ToolTip.SetTip(toggle, option.Description);
        toggle.IsCheckedChanged += async (_, _) =>
        {
            if (_isApplying || toggle.IsChecked is not bool value)
                return;
            if (!await PersistAsync(() => _settingsService.SetToggleAsync(_pageKey, option.Key, value), toggle))
            {
                _isApplying = true;
                toggle.IsChecked = option.BoolValue;
                _isApplying = false;
                return;
            }

            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                mainWindow.ApplyNavigationVisibility();
                if (_pageKey == "Display" && option.Key == "NavigationPaneExpanded")
                    mainWindow.ApplyNavigationPaneState();
            }

            // The WPF notifications window disables every dependent editor as
            // soon as the master switch changes. Rebuild the Avalonia option
            // cards as well so the current view reflects that state immediately.
            if (_pageKey == "Display" && option.Key == "DontShowNotifications")
            {
                await RefreshPageAsync();
                return;
            }

            if (_pageKey == "Application" && option.Key is "EnableHardwareSensors" or "ShowOsd")
                await RefreshPageAsync();
        };
        return toggle;
    }

    private ComboBox CreateSelection(AvaloniaSettingOption option)
    {
        var combo = new ComboBox
        {
            ItemsSource = option.Values ?? [],
            SelectedItem = option.SelectedValue,
            IsEnabled = option.IsEnabled,
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(combo, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(combo, option.Title);
        ToolTip.SetTip(combo, option.Description);
        combo.SelectionChanged += async (_, _) =>
        {
            if (_isApplying || combo.SelectedItem is not string value)
                return;
            if (!await PersistAsync(() => _settingsService.SetSelectionAsync(_pageKey, option.Key, value), combo))
            {
                _isApplying = true;
                combo.SelectedItem = option.SelectedValue;
                _isApplying = false;
                return;
            }

            if (_pageKey == "Display"
                && option.Key == "WindowBackdrop"
                && TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                mainWindow.ApplyWindowBackdrop();
            }

            if (_pageKey == "Power" && option.Key == "PowerModeMapping")
                await RefreshPageAsync();
        };
        return combo;
    }

    private TextBox CreateTextBox(AvaloniaSettingOption option)
    {
        var textBox = new TextBox
        {
            Text = option.TextValue ?? string.Empty,
            IsEnabled = option.IsEnabled,
            MinWidth = 240,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(textBox, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(textBox, option.Title);
        ToolTip.SetTip(textBox, option.Description);

        if (_pageKey == "Update" && option.Key is "RepositoryOwner" or "RepositoryName")
        {
            SetPendingUpdateRepositoryValue(option.Key, textBox.Text);
            textBox.TextChanged += (_, _) =>
            {
                if (_isApplying)
                    return;

                SetPendingUpdateRepositoryValue(option.Key, textBox.Text);
                QueueUpdateRepositorySettingsPersistence();
            };
            return textBox;
        }

        textBox.LostFocus += async (_, _) =>
        {
            if (_isApplying)
                return;
            if (!await PersistAsync(() => _settingsService.SetTextAsync(_pageKey, option.Key, textBox.Text), textBox))
            {
                _isApplying = true;
                textBox.Text = option.TextValue ?? string.Empty;
                _isApplying = false;
            }
        };
        return textBox;
    }

    // WPF saves both repository fields together after typing pauses. Keeping the
    // snapshot here preserves that behavior without forcing a focus change.
    private void SetPendingUpdateRepositoryValue(string optionKey, string? value)
    {
        if (optionKey == "RepositoryOwner")
            _pendingUpdateRepositoryOwner = value;
        else if (optionKey == "RepositoryName")
            _pendingUpdateRepositoryName = value;
    }

    private void QueueUpdateRepositorySettingsPersistence()
    {
        _updateRepositorySettingsTimer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400),
        };
        _updateRepositorySettingsTimer.Tick -= UpdateRepositorySettingsTimer_Tick;
        _updateRepositorySettingsTimer.Tick += UpdateRepositorySettingsTimer_Tick;
        _updateRepositorySettingsTimer.Stop();
        _updateRepositorySettingsTimer.Start();
    }

    private async void UpdateRepositorySettingsTimer_Tick(object? sender, EventArgs e)
    {
        _updateRepositorySettingsTimer?.Stop();
        if (_isApplying)
            return;

        try
        {
            await _settingsService.SetTextAsync("Update", "RepositoryOwner", _pendingUpdateRepositoryOwner);
            await _settingsService.SetTextAsync("Update", "RepositoryName", _pendingUpdateRepositoryName);
        }
        catch (Exception ex)
        {
            ShowActionFeedback(
                AvaloniaLocalization.GetString("Settings_Page_SaveFailed", "Unable to save settings"),
                ex.Message,
                "error");
        }
    }

    private Control CreateMultiSelection(AvaloniaSettingOption option)
    {
        var selected = new HashSet<string>(option.SelectedValues ?? [], StringComparer.Ordinal);
        if (selected.Count == 0 && !string.IsNullOrWhiteSpace(option.SelectedValue))
            selected.Add(option.SelectedValue);

        var checkBoxes = new List<CheckBox>();
        var panel = new StackPanel
        {
            Spacing = 4,
            MinWidth = 220,
            MaxWidth = 360,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var valueIndex = 0;
        foreach (var value in option.Values ?? [])
        {
            var checkBox = new CheckBox
            {
                Content = value,
                IsChecked = selected.Contains(value),
                IsEnabled = option.IsEnabled,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            AutomationProperties.SetAutomationId(
                checkBox,
                $"AvaloniaSettings_{_pageKey}_{option.Key}_{valueIndex++}");
            AutomationProperties.SetName(checkBox, value);
            ToolTip.SetTip(checkBox, option.Description);
            checkBox.IsCheckedChanged += async (_, _) =>
            {
                if (_isApplying || checkBox.IsChecked is not bool isChecked)
                    return;

                var previous = selected.ToArray();
                if (isChecked && value.Equals(option.Values?.FirstOrDefault(), StringComparison.Ordinal))
                {
                    selected.Clear();
                    selected.Add(value);
                    _isApplying = true;
                    foreach (var sibling in checkBoxes.Where(sibling => !ReferenceEquals(sibling, checkBox)))
                        sibling.IsChecked = false;
                    _isApplying = false;
                }
                else if (isChecked)
                {
                    selected.Remove(option.Values?.FirstOrDefault() ?? string.Empty);
                    selected.Add(value);
                }
                else
                    selected.Remove(value);

                if (!await PersistAsync(
                    () => _settingsService.SetMultiSelectionAsync(_pageKey, option.Key, selected.ToArray()),
                    checkBox))
                {
                    selected.Clear();
                    foreach (var previousValue in previous)
                        selected.Add(previousValue);
                    _isApplying = true;
                    foreach (var sibling in checkBoxes)
                        sibling.IsChecked = selected.Contains(sibling.Content?.ToString() ?? string.Empty);
                    _isApplying = false;
                }
            };
            checkBoxes.Add(checkBox);
            panel.Children.Add(checkBox);
        }

        AutomationProperties.SetName(panel, option.Title);
        ToolTip.SetTip(panel, option.Description);
        return panel;
    }

    private Button CreateAction(AvaloniaSettingOption option)
    {
        var button = new Button
        {
            Content = option.ActionText ?? option.Title,
            IsEnabled = option.IsEnabled,
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(button, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(button, option.Title);
        ToolTip.SetTip(button, option.Warning ?? option.Description);
        button.Click += async (_, _) =>
        {
            if (_isApplying)
                return;

            if (_pageKey == "Update" && option.Key == "CheckForUpdates")
            {
                await CheckForUpdatesAsync(button, option.IsEnabled);
                return;
            }

            if (_pageKey == "Application" && option.Key == "HardwareSectionsConfigure")
            {
                await ConfigureHardwareSectionsAsync(button);
                return;
            }

            if (_pageKey == "Application" && option.Key == "OsdConfigure")
            {
                await ConfigureOsdAsync(button);
                return;
            }

#if WINDOWS
            if (_pageKey == "Power" && option.Key == "OpenPowerModes")
            {
                await ConfigurePowerMappingAsync(PowerMappingKind.WindowsPowerMode, button);
                return;
            }

            if (_pageKey == "Power" && option.Key == "OpenPowerPlans")
            {
                await ConfigurePowerMappingAsync(PowerMappingKind.WindowsPowerPlan, button);
                return;
            }
#endif

            if (_pageKey == "Display" && option.Key == "BootLogo")
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = option.Title,
                });
                var filePath = files.FirstOrDefault()?.Path.LocalPath;
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                await PersistAsync(
                    () => _settingsService.SetBootLogoAsync(filePath),
                    button);
                return;
            }

            if (_pageKey == "Application" && option.Key is "ExportSettings" or "ImportSettings")
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return;

                var file = option.Key == "ExportSettings"
                    ? (await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        SuggestedFileName = $"udt-settings-{DateTime.Now:yyyyMMdd-HHmmss}.udtbackup",
                        DefaultExtension = "udtbackup",
                        FileTypeChoices =
                        [new FilePickerFileType("UDT settings backup") { Patterns = ["*.udtbackup"] }],
                    }))
                    : (await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        AllowMultiple = false,
                        FileTypeFilter =
                        [new FilePickerFileType("UDT settings backup") { Patterns = ["*.udtbackup"] }],
                    })).FirstOrDefault();

                var filePath = file?.Path.LocalPath;
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                var imported = await PersistAsync(
                    () => option.Key == "ExportSettings"
                        ? _settingsService.ExportSettingsAsync(filePath)
                        : _settingsService.ImportSettingsAsync(filePath),
                    button);
                if (imported && option.Key == "ImportSettings")
                    await RefreshPageAsync();
                return;
            }

            await PersistAsync(
                () => _settingsService.InvokeActionAsync(_pageKey, option.Key),
                button);
        };
        return button;
    }

    private async Task ConfigureHardwareSectionsAsync(Button button)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        button.IsEnabled = false;
        try
        {
            var dialog = new HardwareSensorSectionsWindow(_settingsService);
            await dialog.ShowDialog(owner);
            await RefreshPageAsync();
        }
        catch (Exception ex)
        {
            ToolTip.SetTip(button, ex.Message);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async Task ConfigureOsdAsync(Button button)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        button.IsEnabled = false;
        try
        {
            var dialog = new OsdSettingsWindow(_settingsService);
            await dialog.ShowDialog(owner);
            await RefreshPageAsync();
        }
        catch (Exception ex)
        {
            ToolTip.SetTip(button, ex.Message);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

#if WINDOWS
    private async Task ConfigurePowerMappingAsync(PowerMappingKind kind, Button button)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        button.IsEnabled = false;
        try
        {
            await new PowerMappingSettingsWindow(kind).ShowDialog(owner);
            await RefreshPageAsync();
        }
        catch (Exception ex)
        {
            ToolTip.SetTip(button, ex.Message);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
#endif

    private async Task CheckForUpdatesAsync(Button button, bool wasEnabled)
    {
        button.IsEnabled = false;
        OpenUpdateWindowButton.IsVisible = false;
        ShowActionFeedback(
            AvaloniaLocalization.GetString("SettingsPage_CheckUpdates_Started_Title", "Checking for updates..."),
            null,
            "informational",
            autoHide: false);

        try
        {
            var result = await _settingsService.CheckForUpdatesAsync();
            var feedback = AvaloniaUpdateFeedback.Resolve(result);
            var title = AvaloniaLocalization.GetString(feedback.TitleKey, "Update check complete");
            var message = feedback.MessageKey is null
                ? null
                : AvaloniaLocalization.GetString(feedback.MessageKey, "The update check did not complete.");

            if (feedback.Kind == AvaloniaUpdateFeedbackKind.UpdateAvailable
                && !string.IsNullOrWhiteSpace(result.LatestVersion)
                && message is not null)
            {
                message = string.Format(message, result.LatestVersion);
            }

            OpenUpdateWindowButton.IsVisible = feedback.Kind == AvaloniaUpdateFeedbackKind.UpdateAvailable;

            ShowActionFeedback(title, message, feedback.Kind switch
            {
                AvaloniaUpdateFeedbackKind.UpdateAvailable => "informational",
                AvaloniaUpdateFeedbackKind.NoUpdates => "success",
                AvaloniaUpdateFeedbackKind.RateLimitReached => "error",
                _ => "error",
            }, autoHide: feedback.Kind != AvaloniaUpdateFeedbackKind.UpdateAvailable);
        }
        catch (Exception ex)
        {
            OpenUpdateWindowButton.IsVisible = false;
            ToolTip.SetTip(button, ex.Message);
            ShowActionFeedback(
                AvaloniaLocalization.GetString("MainWindow_CheckForUpdates_Error_Title", "Failed to check for updates"),
                ex.Message,
                "error");
        }
        finally
        {
            button.IsEnabled = wasEnabled;
        }
    }

    private void OpenUpdateWindowButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

#if WINDOWS
        if (UniversalDeviceToolkit.Avalonia.Startup.AvaloniaUpdateCheckCoordinator.Current is { } coordinator)
            _ = coordinator.ShowUpdateAsync(owner);
#endif
    }

    private void ShowActionFeedback(string title, string? message, string variant, bool autoHide = true)
    {
        foreach (var className in new[] { "informational", "success", "warning", "error" })
            ActionFeedbackBar.Classes.Remove(className);
        ActionFeedbackBar.Classes.Add(variant);
        ActionFeedbackTitleBlock.Text = title;
        ActionFeedbackMessageBlock.Text = message ?? string.Empty;
        ActionFeedbackMessageBlock.IsVisible = !string.IsNullOrWhiteSpace(message);
        ActionFeedbackBar.IsVisible = true;

        _feedbackTimer?.Stop();
        if (!autoHide)
            return;

        _feedbackTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _feedbackTimer.Stop();
        _feedbackTimer.Tick -= FeedbackTimer_Tick;
        _feedbackTimer.Tick += FeedbackTimer_Tick;
        _feedbackTimer.Start();
    }

    private void FeedbackTimer_Tick(object? sender, EventArgs e)
    {
        _feedbackTimer?.Stop();
        ActionFeedbackBar.IsVisible = false;
    }

    private Control CreateEmptyState(bool isAvailable) => new Border
    {
        Background = GetResource<IBrush>("CardBackgroundBrush"),
        BorderBrush = GetResource<IBrush>("CardBorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = (CornerRadius)Resources["CornerRadiusCard"]!,
        Padding = new Thickness(16),
        Child = new Controls.LocalizedTextBlock
        {
            Text = isAvailable
                ? AvaloniaLocalization.GetString("Settings_Page_NoOptions", "No settings are available for this page.")
                : AvaloniaLocalization.GetString("Settings_Page_UnavailableDetail", "The controls remain visible when the adapter is available on this platform."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        },
    };

    private async Task<bool> PersistAsync(Func<Task> action, Control control)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            ToolTip.SetTip(control, ex.Message);
            return false;
        }
    }

    private T GetResource<T>(object key) where T : class
    {
        if (this.TryFindResource(key, out var resource) && resource is T typedValue)
            return typedValue;

        if (typeof(T) == typeof(IBrush))
            return (T)(object)new SolidColorBrush(Colors.Transparent);

        throw new InvalidOperationException($"Missing Avalonia resource '{key}'.");
    }
}

public sealed class SettingsSmartKeysView() : SettingsCapabilityView(
    "SmartKeys",
    "Smart Keys",
    "Configure Fn-lock and Smart Key behavior.");

public sealed class SettingsUpdateView() : SettingsCapabilityView(
    "Update",
    "Update",
    "Choose how Universal Device Toolkit checks for new releases.");

public sealed class SettingsPowerView() : SettingsCapabilityView(
    "Power",
    "Power",
    "Configure power mode mapping and battery behavior.");

public sealed class SettingsIntegrationsView() : SettingsCapabilityView(
    "Integrations",
    "Integrations",
    "Connect Universal Device Toolkit to supported external tools and services.");
