using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Semantic text block for localized UI copy. Compact slots use one-line ellipsis;
/// descriptive slots may grow to a bounded number of lines and expose the full value
/// through a tooltip when the renderer collapses it.
/// </summary>
public class LocalizedTextBlock : TextBlock
{
    private bool _ownsAutomationName;
    private bool _ownsToolTip;
    public static readonly StyledProperty<LocalizedOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<LocalizedTextBlock, LocalizedOverflowMode>(
            nameof(OverflowMode), LocalizedOverflowMode.Ellipsis);

    public static readonly StyledProperty<bool> AutoToolTipProperty =
        AvaloniaProperty.Register<LocalizedTextBlock, bool>(nameof(AutoToolTip), true);

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
        UpdateAutomationName();
        UpdateToolTip();
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OverflowModeProperty || change.Property == MaxLinesProperty)
            ApplyOverflowSettings();

        if (change.Property == TextProperty || change.Property == OverflowModeProperty
            || change.Property == AutoToolTipProperty || change.Property == BoundsProperty)
        {
            UpdateAutomationName();
            UpdateToolTip();
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateToolTip();

    private void ApplyOverflowSettings()
    {
        if (OverflowMode == LocalizedOverflowMode.Wrap)
        {
            TextWrapping = TextWrapping.Wrap;
            TextTrimming = TextTrimming.CharacterEllipsis;
            if (MaxLines <= 0)
                MaxLines = LocalizedOverflowPolicy.DescriptionMaxLines;
        }
        else
        {
            TextWrapping = TextWrapping.NoWrap;
            TextTrimming = TextTrimming.CharacterEllipsis;
            MaxLines = 1;
        }
    }

    private void UpdateAutomationName()
    {
        if (!string.IsNullOrEmpty(Text)
            && (_ownsAutomationName || string.IsNullOrEmpty(AutomationProperties.GetName(this))))
        {
            AutomationProperties.SetName(this, Text);
            _ownsAutomationName = true;
        }
    }

    private void UpdateToolTip()
    {
        if (!AutoToolTip || string.IsNullOrWhiteSpace(Text) || Bounds.Width <= 0)
            return;

        var layout = TextLayout;
        var truncated = OverflowMode == LocalizedOverflowMode.Ellipsis
            ? layout.WidthIncludingTrailingWhitespace > Bounds.Width + 1
            : MaxLines > 0 && layout.TextLines.Count >= MaxLines
                && layout.Height >= Bounds.Height - 1;

        if (truncated)
        {
            global::Avalonia.Controls.ToolTip.SetTip(this, Text);
            _ownsToolTip = true;
        }
        else if (_ownsToolTip)
        {
            global::Avalonia.Controls.ToolTip.SetTip(this, null);
            _ownsToolTip = false;
        }
    }
}
