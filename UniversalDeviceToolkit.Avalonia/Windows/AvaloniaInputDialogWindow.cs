using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Dependency-free modal input dialog ported from the WPF InputDialogWindow.
/// Returns the entered text through <see cref="InputText"/> or the Task-based
/// <see cref="ShowAsync"/> helper; the OK button is disabled while the input is
/// empty unless <paramref name="allowEmpty"/> is set.
/// </summary>
public sealed class AvaloniaInputDialogWindow : Window
{
    private readonly bool _allowEmpty;
    private readonly Button _confirmButton;
    private readonly TextBox _textBox;

    public string? InputText { get; private set; }

    public AvaloniaInputDialogWindow(
        string title,
        string? message,
        string? text,
        string? primaryButton,
        string? secondaryButton,
        bool allowEmpty,
        Window? owner = null)
    {
        Title = title;
        Owner = owner;
        _allowEmpty = allowEmpty;
        Width = 420;
        MinWidth = 380;
        MaxWidth = 560;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AutomationProperties.SetAutomationId(this, "AvaloniaInputDialogWindow");
        AutomationProperties.SetName(this, title);

        _textBox = new TextBox
        {
            Text = text ?? string.Empty,
            Watermark = message ?? string.Empty,
            MaxLength = 50,
        };
        AutomationProperties.SetAutomationId(_textBox, "AvaloniaInputDialogTextBox");
        _textBox.TextChanged += (_, _) => RefreshConfirmButtonState();

        _confirmButton = new Button
        {
            Content = primaryButton ?? Get("OK", "OK"),
            MinWidth = 100,
            IsDefault = true,
        };
        AutomationProperties.SetAutomationId(_confirmButton, "AvaloniaInputDialogConfirmButton");
        AutomationProperties.SetName(_confirmButton, _confirmButton.Content?.ToString());
        _confirmButton.Click += ConfirmButton_Click;

        var cancelButton = new Button
        {
            Content = secondaryButton ?? Get("Cancel", "Cancel"),
            MinWidth = 100,
            IsCancel = true,
        };
        AutomationProperties.SetAutomationId(cancelButton, "AvaloniaInputDialogCancelButton");
        AutomationProperties.SetName(cancelButton, cancelButton.Content?.ToString());
        cancelButton.Click += (_, _) => Close();

        var titleBlock = new LocalizedTextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = GetBrush("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancelButton, _confirmButton },
        };
        Content = new Border
        {
            Padding = new Thickness(20),
            Child = new StackPanel
            {
                Spacing = 12,
                Children = { titleBlock, _textBox, buttons },
            },
        };

        RefreshConfirmButtonState();
        Opened += (_, _) =>
        {
            _textBox.CaretIndex = _textBox.Text.Length;
            _textBox.Focus();
        };
    }

    /// <summary>
    /// Shows the dialog modally and returns the entered text, or null when the
    /// user cancels. An empty confirmed value is returned as an empty string.
    /// </summary>
    public static async Task<string?> ShowAsync(
        Window owner,
        string title,
        string? message = null,
        string? text = null,
        string? primaryButton = null,
        string? secondaryButton = null,
        bool allowEmpty = false)
    {
        var dialog = new AvaloniaInputDialogWindow(
            title,
            message,
            text,
            primaryButton,
            secondaryButton,
            allowEmpty,
            owner);
        await dialog.ShowDialog(owner);
        return dialog.InputText;
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        var value = _textBox.Text?.Trim();
        if (!IsValidInput(value, _allowEmpty))
            return;

        InputText = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        Close();
    }

    private void RefreshConfirmButtonState() =>
        _confirmButton.IsEnabled = IsValidInput(_textBox.Text, _allowEmpty);

    internal static bool IsValidInput(string? text, bool allowEmpty) =>
        allowEmpty || !string.IsNullOrWhiteSpace(text);

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private static IBrush GetBrush(string key) =>
        Application.Current?.TryFindResource(key, out var resource) == true
        && resource is IBrush brush
            ? brush
            : Brushes.Transparent;
}
