using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace LenovoLegionToolkit.WPF.Controls;

public class CardHeaderControl : UserControl
{
    private readonly TextBlock _titleTextBlock = new()
    {
        FontSize = 14,
        FontWeight = FontWeights.Medium,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    private readonly TextBlock _subtitleTextBlock = new()
    {
        FontSize = 12,
        Margin = new(0, 4, 0, 0),
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    private readonly TextBlock _warningTextBlock = new()
    {
        FontSize = 12,
        Margin = new(0, 4, 0, 0),
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    private readonly StackPanel _stackPanel = new();

    private readonly Grid _grid = new()
    {
        ColumnDefinitions =
        {
            new ColumnDefinition { Width = new(1, GridUnitType.Star) },
            new ColumnDefinition { Width = GridLength.Auto },
        },
        RowDefinitions =
        {
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto },
        },
    };

    private UIElement? _accessory;

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(CardHeaderControl),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(CardHeaderControl),
            new PropertyMetadata(string.Empty, OnSubtitleChanged));

    public static readonly DependencyProperty WarningProperty =
        DependencyProperty.Register(
            nameof(Warning),
            typeof(string),
            typeof(CardHeaderControl),
            new PropertyMetadata(string.Empty, OnWarningChanged));

    public static readonly DependencyProperty SubtitleToolTipProperty =
        DependencyProperty.Register(
            nameof(SubtitleToolTip),
            typeof(string),
            typeof(CardHeaderControl),
            new PropertyMetadata(null, OnSubtitleToolTipChanged));

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

    public string? SubtitleToolTip
    {
        get => (string?)GetValue(SubtitleToolTipProperty);
        set => SetValue(SubtitleToolTipProperty, value);
    }

    public UIElement? Accessory
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

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        Grid.SetColumn(_titleTextBlock, 0);
        Grid.SetColumn(_stackPanel, 0);

        Grid.SetRow(_titleTextBlock, 0);
        Grid.SetRow(_stackPanel, 1);

        _stackPanel.Children.Add(_subtitleTextBlock);
        _stackPanel.Children.Add(_warningTextBlock);

        _grid.Children.Add(_titleTextBlock);
        _grid.Children.Add(_stackPanel);

        Content = _grid;

        UpdateTextStyle();
        IsEnabledChanged += (_, _) => UpdateTextStyle();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CardHeaderControlAutomationPeer(this);

    private void RefreshLayout()
    {
        if (string.IsNullOrWhiteSpace(Subtitle) && string.IsNullOrWhiteSpace(Warning))
            Grid.SetRowSpan(_titleTextBlock, 2);
        else
            Grid.SetRowSpan(_titleTextBlock, 1);

        _subtitleTextBlock.Visibility = string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
        _warningTextBlock.Visibility = string.IsNullOrWhiteSpace(Warning) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateTextStyle()
    {
        if (IsEnabled)
        {
            _titleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
            _subtitleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");
            _warningTextBlock.SetResourceReference(ForegroundProperty, "SystemFillColorCautionBrush");
        }
        else
        {
            _titleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
            _subtitleTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
            _warningTextBlock.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
        }
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CardHeaderControl control)
            return;

        // _titleTextBlock is initialized as a field, so it's never null
        control._titleTextBlock.Text = e.NewValue as string ?? string.Empty;
        control.RefreshLayout();
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CardHeaderControl control)
            return;

        // _subtitleTextBlock is initialized as a field, so it's never null
        control._subtitleTextBlock.Text = e.NewValue as string ?? string.Empty;
        control.RefreshLayout();
    }

    private static void OnWarningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CardHeaderControl control)
            return;

        // _warningTextBlock is initialized as a field, so it's never null
        control._warningTextBlock.Text = e.NewValue as string ?? string.Empty;
        control.RefreshLayout();
    }

    private static void OnSubtitleToolTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CardHeaderControl control)
            return;

        // _subtitleTextBlock is initialized as a field, so it's never null
        var value = e.NewValue as string;
        control._subtitleTextBlock.ToolTip = value;
        ToolTipService.SetIsEnabled(control._subtitleTextBlock, value is not null);
        control.RefreshLayout();
    }

    private class CardHeaderControlAutomationPeer(CardHeaderControl owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(CardHeaderControl);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ItemContainer)
                return this;

            return base.GetPattern(patternInterface);
        }

        protected override string GetNameCore()
        {
            var result = base.GetNameCore() ?? string.Empty;

            if (result == string.Empty)
                result = AutomationProperties.GetName(owner);

            if (result == string.Empty && !string.IsNullOrWhiteSpace(owner._titleTextBlock.Text))
            {
                result = owner._titleTextBlock.Text;

                if (!string.IsNullOrWhiteSpace(owner._subtitleTextBlock.Text))
                    result += $", {owner._subtitleTextBlock.Text}";
            }

            return result;
        }
    }
}
