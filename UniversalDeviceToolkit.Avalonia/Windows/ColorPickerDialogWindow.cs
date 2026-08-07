using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Modal color dialog built on Avalonia.Controls.ColorPicker's ColorView
/// (HSV sliders plus hex input). Shows the picked color via
/// <see cref="Window.ShowDialog{TResult}"/>; cancel yields null.
/// </summary>
internal sealed class ColorPickerDialogWindow : Window
{
    private readonly ColorView _colorView;

    public Color? SelectedColor { get; private set; }

    public ColorPickerDialogWindow(Color initialColor)
    {
        Title = Get("ColorPickerDialogWindow_Title", "Pick a color");
        Width = 480;
        MinWidth = 420;
        MaxWidth = 560;
        Height = 620;
        MinHeight = 540;
        MaxHeight = 760;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AutomationProperties.SetAutomationId(this, "AvaloniaColorPickerDialog");
        AutomationProperties.SetName(this, Title);

        _colorView = new ColorView
        {
            Color = initialColor,
            ColorModel = ColorModel.Hsva,
            IsColorSpectrumVisible = true,
            IsColorSpectrumSliderVisible = true,
            IsColorModelVisible = true,
            IsComponentSliderVisible = true,
            IsComponentTextInputVisible = true,
            IsHexInputVisible = true,
            IsColorPreviewVisible = true,
            IsAlphaEnabled = false,
            IsAlphaVisible = false,
        };
        AutomationProperties.SetAutomationId(_colorView, "AvaloniaColorPickerColorView");

        var titleText = new TextBlock
        {
            Text = Title,
            FontSize = 20,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        };
        var description = new TextBlock
        {
            Text = Get("ColorPickerDialogWindow_Description", "Choose a color. The keyboard applies the value when you confirm."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        };

        var okButton = CreateButton(
            Get("Common_OK", "OK"),
            "AvaloniaColorPickerDialogOk",
            () =>
            {
                SelectedColor = _colorView.Color;
                Close(_colorView.Color);
            });
        var cancelButton = CreateButton(
            Get("Common_Cancel", "Cancel"),
            "AvaloniaColorPickerDialogCancel",
            () => Close());

        var actions = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 10 };
        Grid.SetColumn(okButton, 1);
        actions.Children.Add(okButton);
        Grid.SetColumn(cancelButton, 2);
        actions.Children.Add(cancelButton);

        var content = new StackPanel
        {
            Spacing = 12,
            Children = { titleText, description, _colorView, actions },
        };
        Content = new Border { Padding = new Thickness(24, 20), Child = content };
    }

    private Button CreateButton(string label, string automationId, Action onClick)
    {
        var button = new Button
        {
            Content = label,
            MinWidth = 110,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => onClick();
        return button;
    }

    private static T GetResource<T>(string key)
        where T : class =>
        Application.Current?.TryGetResource(key, out var value) == true && value is T resource
            ? resource
            : (T)(object)new SolidColorBrush(Colors.Gray);

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);
}
