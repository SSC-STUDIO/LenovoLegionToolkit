using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

/// <summary>
/// WPF-UI compatible title bar (used with borderless windows). Renders the window title,
/// optional center/trailing content and minimize/maximize/close caption buttons, and
/// supports window dragging (<see cref="Window.BeginMoveDrag"/>) and double-click to
/// maximize. Fully functional without a template.
/// </summary>
public class TitleBar : Panel
{
    private const double CaptionButtonWidth = 46;

    private Window? _window;
    private IDisposable? _windowStateSubscription;
    private Button? _maximizeButton;

    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Title));

    /// <summary>Defines the <see cref="CanMaximize"/> property.</summary>
    public static readonly StyledProperty<bool> CanMaximizeProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(CanMaximize), true);

    /// <summary>Defines the <see cref="ShowMaximize"/> property.</summary>
    public static readonly StyledProperty<bool> ShowMaximizeProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(ShowMaximize), true);

    /// <summary>Defines the <see cref="ShowMinimize"/> property.</summary>
    public static readonly StyledProperty<bool> ShowMinimizeProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(ShowMinimize), true);

    /// <summary>Defines the <see cref="CenterContent"/> property.</summary>
    public static readonly StyledProperty<object?> CenterContentProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(CenterContent));

    /// <summary>Defines the <see cref="TrailingContent"/> property.</summary>
    public static readonly StyledProperty<object?> TrailingContentProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(TrailingContent));

    /// <summary>
    /// Gets or sets the window title text shown on the left side.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the window can be maximized/restored.
    /// </summary>
    public bool CanMaximize
    {
        get => GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the maximize button is shown.
    /// </summary>
    public bool ShowMaximize
    {
        get => GetValue(ShowMaximizeProperty);
        set => SetValue(ShowMaximizeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the minimize button is shown.
    /// </summary>
    public bool ShowMinimize
    {
        get => GetValue(ShowMinimizeProperty);
        set => SetValue(ShowMinimizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the content displayed centered in the title bar.
    /// </summary>
    public object? CenterContent
    {
        get => GetValue(CenterContentProperty);
        set => SetValue(CenterContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the content displayed at the trailing (right) edge, before the caption
    /// buttons.
    /// </summary>
    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public TitleBar()
    {
        PointerPressed += OnTitleBarPointerPressed;
        RebuildLayout();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _window = TopLevel.GetTopLevel(this) as Window;

        if (_window is not null)
            _window.PropertyChanged += Window_PropertyChanged;
    }

    private void Window_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
            UpdateMaximizeButton();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_window is not null)
            _window.PropertyChanged -= Window_PropertyChanged;
        _window = null;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty
            || change.Property == CenterContentProperty
            || change.Property == TrailingContentProperty
            || change.Property == ShowMinimizeProperty
            || change.Property == ShowMaximizeProperty
            || change.Property == CanMaximizeProperty)
        {
            RebuildLayout();
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_window is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (e.ClickCount == 2)
        {
            if (CanMaximize)
            {
                ToggleMaximize();
                e.Handled = true;
            }

            return;
        }

        _window.BeginMoveDrag(e);
    }

    private void ToggleMaximize()
    {
        if (_window is null || !CanMaximize)
            return;

        _window.WindowState = _window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void RebuildLayout()
    {
        Children.Clear();

        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var titleBlock = new TextBlock
        {
            Text = Title,
            Margin = new Thickness(12, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(titleBlock, 0);
        grid.Children.Add(titleBlock);

        // AVALONIA: ContentPresenter was renamed to ContentControl.
        var centerPresenter = new ContentControl
        {
            Content = CenterContent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumn(centerPresenter, 1);
        grid.Children.Add(centerPresenter);

        var trailingPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (TrailingContent is { } trailing)
        {
            var trailingPresenter = new ContentControl
            {
                Content = trailing,
                VerticalAlignment = VerticalAlignment.Center
            };
            trailingPanel.Children.Add(trailingPresenter);
        }

        if (ShowMinimize || ShowMaximize)
        {
            var captionStack = new StackPanel { Orientation = Orientation.Horizontal };

            if (ShowMinimize)
            {
                captionStack.Children.Add(CreateCaptionButton(SymbolRegular.Subtract20, "Minimize", Minimize));
            }

            if (ShowMaximize)
            {
                _maximizeButton = CreateCaptionButton(SymbolRegular.Square20, "Maximize", ToggleMaximize);
                captionStack.Children.Add(_maximizeButton);
            }

            var closeButton = CreateCaptionButton(SymbolRegular.Dismiss20, "Close", () => _window?.Close());
            captionStack.Children.Add(closeButton);

            trailingPanel.Children.Add(captionStack);
        }

        Grid.SetColumn(trailingPanel, 2);
        grid.Children.Add(trailingPanel);

        Children.Add(grid);

        UpdateMaximizeButton();
    }

    private static Button CreateCaptionButton(SymbolRegular symbol, string automationName, Action onClick)
    {
        var button = new Button
        {
            Width = CaptionButtonWidth,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = global::Avalonia.Media.Brushes.Transparent,
            Content = new SymbolIcon { Symbol = symbol }
        };
        button.Click += (_, _) => onClick();
        AutomationProperties.SetName(button, automationName);
        return button;
    }

    private void Minimize()
    {
        if (_window is not null)
            _window.WindowState = WindowState.Minimized;
    }

    private void UpdateMaximizeButton()
    {
        if (_maximizeButton is null)
            return;

        _maximizeButton.IsEnabled = CanMaximize;

        var isMaximized = _window?.WindowState == WindowState.Maximized;
        _maximizeButton.Content = new SymbolIcon
        {
            Symbol = isMaximized ? SymbolRegular.SquareMultiple20 : SymbolRegular.Square20
        };
    }
}
