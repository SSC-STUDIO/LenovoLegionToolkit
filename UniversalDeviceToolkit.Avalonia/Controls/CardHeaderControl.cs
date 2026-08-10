using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Controls;

public enum CardHeaderWarningSeverity
{
    Warning,
    Critical,
}

public class CardHeaderControl : UserControl
{
    private readonly AdaptiveTextBlock _titleTextBlock = new()
    {
        FontWeight = FontWeight.Medium,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxHeight = 48,
        MaxLines = 2,
        MinFontSize = 11,
        OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
        AutoToolTip = true,
    };

    private readonly AdaptiveTextBlock _subtitleTextBlock = new()
    {
        Margin = new(0, 4, 0, 0),
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxHeight = 60, // Limit subtitle to about 3 lines to prevent card bloat
        MaxLines = 3,
        MinFontSize = 11,
        OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
        AutoToolTip = true,
    };

    private readonly AdaptiveTextBlock _warningTextBlock = new()
    {
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxHeight = 60,
        MaxLines = 3,
        MinFontSize = 11,
        OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
        AutoToolTip = true,
    };

    private readonly Border _warningSurface = new()
    {
        Margin = new(0, 4, 0, 0),
        Padding = new(8, 4, 8, 4),
        CornerRadius = new(6),
        Child = null,
    };

    private readonly StackPanel _stackPanel = new();

    private readonly Grid _grid = new()
    {
        ColumnDefinitions =
        {
            new ColumnDefinition { Width = new(1, GridUnitType.Star), MinWidth = 0 },
            new ColumnDefinition { Width = GridLength.Auto },
        },
        RowDefinitions =
        {
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto },
        },
    };

    private Control? _accessory;

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<CardHeaderControl, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<CardHeaderControl, string>(nameof(Subtitle), string.Empty);

    public static readonly StyledProperty<string> WarningProperty =
        AvaloniaProperty.Register<CardHeaderControl, string>(nameof(Warning), string.Empty);

    public static readonly StyledProperty<CardHeaderWarningSeverity> WarningSeverityProperty =
        AvaloniaProperty.Register<CardHeaderControl, CardHeaderWarningSeverity>(nameof(WarningSeverity), CardHeaderWarningSeverity.Warning);

    public static readonly StyledProperty<string?> SubtitleToolTipProperty =
        AvaloniaProperty.Register<CardHeaderControl, string?>(nameof(SubtitleToolTip), null);

    static CardHeaderControl()
    {
        TitleProperty.Changed.AddClassHandler<CardHeaderControl>(OnTitleChanged);
        SubtitleProperty.Changed.AddClassHandler<CardHeaderControl>(OnSubtitleChanged);
        WarningProperty.Changed.AddClassHandler<CardHeaderControl>(OnWarningChanged);
        WarningSeverityProperty.Changed.AddClassHandler<CardHeaderControl>(OnWarningSeverityChanged);
        SubtitleToolTipProperty.Changed.AddClassHandler<CardHeaderControl>(OnSubtitleToolTipChanged);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public VerticalAlignment TitleVerticalAlignment
    {
        get => _titleTextBlock.VerticalAlignment;
        set => _titleTextBlock.VerticalAlignment = value;
    }

    public VerticalAlignment SubtitleVerticalAlignment
    {
        get => _subtitleTextBlock.VerticalAlignment;
        set => _subtitleTextBlock.VerticalAlignment = value;
    }

    public string Warning
    {
        get => (string)GetValue(WarningProperty);
        set => SetValue(WarningProperty, value);
    }

    public CardHeaderWarningSeverity WarningSeverity
    {
        get => (CardHeaderWarningSeverity)GetValue(WarningSeverityProperty);
        set => SetValue(WarningSeverityProperty, value);
    }

    public string? SubtitleToolTip
    {
        get => (string?)GetValue(SubtitleToolTipProperty);
        set => SetValue(SubtitleToolTipProperty, value);
    }

    public Control? Accessory
    {
        get => _accessory;
        set
        {
            if (_accessory is not null)
                _grid.Children.Remove(_accessory);

            _accessory = value;

            if (_accessory is not null)
            {
                Grid.SetColumn(_accessory, 1);
                Grid.SetRow(_accessory, 0);
                Grid.SetRowSpan(_accessory, 2);

                _grid.Children.Add(_accessory);
            }

            RefreshLayout();
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Apply Typography styles. AVALONIA: controls have no Style property; the
        // keyed TextBlock style is registered on the control's own Styles collection
        // (its Selector="TextBlock" matches the control itself).
        ApplyTextBlockStyle(_titleTextBlock, "AppPageDescriptionTextBlockStyle");
        ApplyTextBlockStyle(_subtitleTextBlock, "AppCaptionTextBlockStyle");
        ApplyTextBlockStyle(_warningTextBlock, "AppCaptionTextBlockStyle");

        Grid.SetColumn(_titleTextBlock, 0);
        Grid.SetColumn(_stackPanel, 0);

        Grid.SetRow(_titleTextBlock, 0);
        Grid.SetRow(_stackPanel, 1);

        _stackPanel.Children.Add(_subtitleTextBlock);
        _warningSurface.Child = _warningTextBlock;
        _stackPanel.Children.Add(_warningSurface);

        _grid.Children.Add(_titleTextBlock);
        _grid.Children.Add(_stackPanel);

        Content = _grid;

        UpdateTextStyle();
        // AVALONIA: no IsEnabledChanged event; observe the IsEnabled property instead.
        PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsEnabledProperty)
                UpdateTextStyle();
        };
    }

    private void ApplyTextBlockStyle(TextBlock textBlock, string styleKey)
    {
        if (this.TryFindResource(styleKey) is Style style && !textBlock.Styles.Contains(style))
            textBlock.Styles.Add(style);
    }

    private void RefreshLayout()
    {
        if (string.IsNullOrWhiteSpace(Subtitle) && string.IsNullOrWhiteSpace(Warning))
            Grid.SetRowSpan(_titleTextBlock, 2);
        else
            Grid.SetRowSpan(_titleTextBlock, 1);

        _subtitleTextBlock.IsVisible = string.IsNullOrWhiteSpace(Subtitle) ? false : true;
        _warningSurface.IsVisible = string.IsNullOrWhiteSpace(Warning) ? false : true;
        UpdateWarningStyle();
    }

    private void UpdateTextStyle()
    {
        if (IsEnabled)
        {
            _titleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
            _subtitleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");
            UpdateWarningStyle();
        }
        else
        {
            _titleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
            _subtitleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
            _warningTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
            _warningSurface.Background = null;
            _warningSurface.BorderBrush = null;
            _warningSurface.BorderThickness = new(0);
        }
    }

    private void UpdateWarningStyle()
    {
        if (!IsEnabled)
            return;

        if (WarningSeverity == CardHeaderWarningSeverity.Critical)
        {
            _warningTextBlock.SetResourceReference(ForegroundProperty, "StatusCriticalTextBrush");
            _warningSurface.SetResourceReference(BackgroundProperty, "StatusCriticalBackgroundBrush");
            _warningSurface.SetResourceReference(BorderBrushProperty, "StatusCriticalBrush");
            _warningSurface.BorderThickness = new(1);
        }
        else
        {
            _warningTextBlock.SetResourceReference(ForegroundProperty, "StatusWarningBrush");
            _warningSurface.SetResourceReference(BackgroundProperty, "StatusWarningBackgroundBrush");
            _warningSurface.SetResourceReference(BorderBrushProperty, "StatusWarningBrush");
            _warningSurface.BorderThickness = new(1);
        }
    }

    private static void OnTitleChanged(CardHeaderControl control, AvaloniaPropertyChangedEventArgs e)
    {
        // _titleTextBlock is initialized as a field, so it's never null
        control._titleTextBlock.Text = e.NewValue as string ?? string.Empty;
        control.RefreshLayout();
    }

    private static void OnSubtitleChanged(CardHeaderControl control, AvaloniaPropertyChangedEventArgs e)
    {
        // _subtitleTextBlock is initialized as a field, so it's never null
        control._subtitleTextBlock.Text = e.NewValue as string ?? string.Empty;
        control.RefreshLayout();
    }

    private static void OnWarningChanged(CardHeaderControl control, AvaloniaPropertyChangedEventArgs e)
    {
        // _warningTextBlock is initialized as a field, so it's never null
        control._warningTextBlock.Text = e.NewValue as string ?? string.Empty;
        control.RefreshLayout();
    }

    private static void OnWarningSeverityChanged(CardHeaderControl control, AvaloniaPropertyChangedEventArgs e)
        => control.UpdateWarningStyle();

    private static void OnSubtitleToolTipChanged(CardHeaderControl control, AvaloniaPropertyChangedEventArgs e)
    {
        var value = e.NewValue as string;
        if (value is not null)
        {
            // An explicit tooltip overrides the adaptive auto-tooltip.
            control._subtitleTextBlock.AutoToolTip = false;
            ToolTip.SetTip(control._subtitleTextBlock, value);
            ToolTip.SetServiceEnabled(control._subtitleTextBlock, true);
        }
        else
        {
            // Let the adaptive control decide whether the full text needs a tooltip.
            control._subtitleTextBlock.AutoToolTip = true;
            ToolTip.SetTip(control._subtitleTextBlock, null);
        }

        control.RefreshLayout();
    }
}
