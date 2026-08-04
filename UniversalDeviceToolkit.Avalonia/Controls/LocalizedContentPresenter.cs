using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Text-aware content presenter for localized button, list item and combo-box content.
/// </summary>
public class LocalizedContentPresenter : ContentPresenter
{
    private bool _ownsAutomationName;
    private bool _ownsToolTip;
    public static readonly StyledProperty<LocalizedOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<LocalizedContentPresenter, LocalizedOverflowMode>(
            nameof(OverflowMode), LocalizedOverflowMode.Ellipsis);

    public static readonly StyledProperty<bool> AutoToolTipProperty =
        AvaloniaProperty.Register<LocalizedContentPresenter, bool>(nameof(AutoToolTip), true);

    public LocalizedOverflowMode OverflowMode
    {
        get => GetValue(OverflowModeProperty);
        set => SetValue(OverflowModeProperty, value);
    }

    public bool AutoToolTip
    {
        get => GetValue(AutoToolTipProperty);
        set => SetValue(AutoToolTipProperty, value);
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LayoutUpdated += OnLayoutUpdated;
        ApplyOverflowSettings();
        UpdateContentMetadata();
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OverflowModeProperty || change.Property == ContentProperty
            || change.Property == BoundsProperty || change.Property == AutoToolTipProperty)
        {
            ApplyOverflowSettings();
            UpdateContentMetadata();
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateContentMetadata();

    private void ApplyOverflowSettings()
    {
        TextWrapping = OverflowMode == LocalizedOverflowMode.Wrap
            ? TextWrapping.Wrap
            : TextWrapping.NoWrap;
        TextTrimming = TextTrimming.CharacterEllipsis;
        MaxLines = OverflowMode == LocalizedOverflowMode.Wrap
            ? LocalizedOverflowPolicy.DescriptionMaxLines
            : 1;
    }

    private void UpdateContentMetadata()
    {
        if (Content is not string text || string.IsNullOrWhiteSpace(text))
            return;

        if (_ownsAutomationName || string.IsNullOrEmpty(AutomationProperties.GetName(this)))
        {
            AutomationProperties.SetName(this, text);
            _ownsAutomationName = true;
        }

        if (!AutoToolTip || Bounds.Width <= 0)
            return;

        var child = Child as global::Avalonia.Controls.TextBlock;
        var truncated = child is not null && child.TextLayout.WidthIncludingTrailingWhitespace > Bounds.Width + 1;
        if (truncated)
        {
            global::Avalonia.Controls.ToolTip.SetTip(this, text);
            _ownsToolTip = true;
        }
        else if (_ownsToolTip)
        {
            global::Avalonia.Controls.ToolTip.SetTip(this, null);
            _ownsToolTip = false;
        }
    }
}
