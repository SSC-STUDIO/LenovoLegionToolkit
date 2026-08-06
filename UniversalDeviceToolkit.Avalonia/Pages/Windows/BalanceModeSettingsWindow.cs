using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Avalonia counterpart of the WPF BalanceModeSettingsWindow. The dialog keeps
/// the AI setting behind the host-neutral platform contract so portable hosts
/// can report an explicit unavailable state without loading Windows services.
/// </summary>
public sealed class BalanceModeSettingsWindow : Window
{
    private readonly IPlatformServices _platformServices;
    private readonly CheckBox _aiModeCheckBox;
    private readonly TextBlock _status;
    private readonly Button _saveButton;
    private bool _isLoaded;
    private bool _isSaving;

    public BalanceModeSettingsWindow(IPlatformServices platformServices)
    {
        _platformServices = platformServices ?? throw new ArgumentNullException(nameof(platformServices));
        Title = Get("BalanceModeSettingsWindow_Title", "Balance mode settings");
        Width = 400;
        MinWidth = 400;
        MaxWidth = 400;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        AutomationProperties.SetAutomationId(this, "AvaloniaBalanceModeSettingsWindow");
        AutomationProperties.SetName(this, Title);

        _aiModeCheckBox = new CheckBox
        {
            Content = Get("BalanceModeSettingsWindow_AIEngine_Title", "AI engine"),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetAutomationId(_aiModeCheckBox, "BalanceModeAiModeCheckBox");
        AutomationProperties.SetName(
            _aiModeCheckBox,
            Get("BalanceModeSettingsWindow_AIEngine_Title", "AI engine"));

        var description = new LocalizedTextBlock
        {
            Text = Get(
                "BalanceModeSettingsWindow_AIEngine_Message",
                "Automatically adjust Balance mode while supported games are running."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
            Margin = new Thickness(32, 4, 0, 0),
        };

        _status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
        };
        AutomationProperties.SetAutomationId(_status, "AvaloniaBalanceModeSettingsStatusText");

        _saveButton = ActionButton(
            Get("Save", "Save"),
            "AvaloniaBalanceModeSettingsSaveButton",
            SaveAndCloseAsync);
        _saveButton.IsEnabled = false;

        var cancelButton = ActionButton(
            Get("Cancel", "Cancel"),
            "AvaloniaBalanceModeSettingsCancelButton",
            () =>
            {
                Close(false);
                return Task.CompletedTask;
            });

        var root = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(24),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };
        root.Children.Add(new LocalizedTextBlock
        {
            Text = Title,
            FontSize = 20,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        root.Children.Add(new StackPanel
        {
            Spacing = 2,
            Children = { _aiModeCheckBox, description },
        });
        root.Children.Add(_status);
        root.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancelButton, _saveButton },
        });

        Content = root;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isLoaded)
            return;

        _isLoaded = true;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        try
        {
            var state = await _platformServices.GetBalanceModeSettingsAsync().ConfigureAwait(true);
            _aiModeCheckBox.IsChecked = state.IsAIModeEnabled;
            _aiModeCheckBox.IsEnabled = state.IsAvailable;
            _saveButton.IsEnabled = state.IsAvailable;
            _status.Text = state.ErrorMessage
                ?? Get("Dashboard_BalanceModeSettings_Status", "Changes are applied when you save.");
            _status.Foreground = state.IsAvailable
                ? GetResource<IBrush>("TextFillColorSecondaryBrush")
                : GetResource<IBrush>("StatusWarningBrush");
        }
        catch (Exception ex)
        {
            _aiModeCheckBox.IsEnabled = false;
            _saveButton.IsEnabled = false;
            _status.Text = ex.Message;
            _status.Foreground = GetResource<IBrush>("StatusWarningBrush");
        }
    }

    private async Task SaveAndCloseAsync()
    {
        if (_isSaving || !_aiModeCheckBox.IsEnabled)
            return;

        _isSaving = true;
        _saveButton.IsEnabled = false;
        try
        {
            var saved = await _platformServices.SaveBalanceModeSettingsAsync(
                _aiModeCheckBox.IsChecked == true).ConfigureAwait(true);
            if (saved)
            {
                Close(true);
                return;
            }

            _status.Text = Get(
                "Dashboard_BalanceModeSettings_SaveFailed",
                "Balance mode settings could not be applied.");
            _status.Foreground = GetResource<IBrush>("StatusWarningBrush");
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = GetResource<IBrush>("StatusWarningBrush");
        }
        finally
        {
            _isSaving = false;
            _saveButton.IsEnabled = _aiModeCheckBox.IsEnabled;
        }
    }

    private static Button ActionButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button { Content = text, MinWidth = 96 };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);

    private static T GetResource<T>(string key)
        where T : class =>
        Application.Current?.TryGetResource(key, out var value) == true && value is T resource
            ? resource
            : (T)(object)new SolidColorBrush(Colors.Gray);
}
