using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Button whose semantic name is applied after its template is connected. Avalonia's
/// content peer otherwise falls back to StackPanel/Border.ToString() for compound content.
/// </summary>
public class AccessibleButton : Button
{
    public static readonly StyledProperty<string?> AutomationNameProperty =
        AvaloniaProperty.Register<AccessibleButton, string?>(nameof(AutomationName));

    public string? AutomationName
    {
        get => GetValue(AutomationNameProperty);
        set => SetValue(AutomationNameProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyAutomationName();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AutomationNameProperty)
            ApplyAutomationName();
    }

    private void ApplyAutomationName()
    {
        if (!string.IsNullOrWhiteSpace(AutomationName))
            AutomationProperties.SetName(this, AutomationName);
    }
}
