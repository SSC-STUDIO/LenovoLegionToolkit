using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// ComboBox with a stable semantic name for UI Automation, independent of the selected
/// item presenter. This keeps the control name intact when the item is a custom view model.
/// </summary>
public class AccessibleComboBox : ComboBox
{
    public static readonly StyledProperty<string?> AutomationNameProperty =
        AvaloniaProperty.Register<AccessibleComboBox, string?>(nameof(AutomationName));

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
