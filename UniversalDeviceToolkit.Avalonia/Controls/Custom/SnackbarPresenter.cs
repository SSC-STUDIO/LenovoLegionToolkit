using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

/// <summary>
/// WPF-UI compatible snackbar presenter. Shows a single toast (usually anchored at the
/// bottom of a window) and hides it automatically after <see cref="Timeout"/> or when
/// <see cref="Close"/> is called. Visuals are provided by Styles/NotificationToast.axaml
/// (template part <c>PART_CloseButton</c>); without a template the <see cref="Content"/>
/// is still displayed by the default <see cref="ContentControl"/> theme.
/// </summary>
public class SnackbarPresenter : ContentControl
{
    /// <summary>Template part: the close (X) button.</summary>
    public const string PART_CloseButton = "PART_CloseButton";

    private DispatcherTimer? _timer;
    private TaskCompletionSource<bool>? _showTcs;

    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SnackbarPresenter, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<SnackbarPresenter, object?>(nameof(Icon));

    /// <summary>Defines the <see cref="Appearance"/> property.</summary>
    public static readonly StyledProperty<ControlAppearance> AppearanceProperty =
        AvaloniaProperty.Register<SnackbarPresenter, ControlAppearance>(nameof(Appearance), ControlAppearance.Secondary);

    /// <summary>Defines the <see cref="Timeout"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> TimeoutProperty =
        AvaloniaProperty.Register<SnackbarPresenter, TimeSpan>(nameof(Timeout), TimeSpan.FromSeconds(5));

    /// <summary>Defines the <see cref="IsCloseButtonEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsCloseButtonEnabledProperty =
        AvaloniaProperty.Register<SnackbarPresenter, bool>(nameof(IsCloseButtonEnabled), true);

    /// <summary>
    /// Gets or sets the snackbar title text.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon shown in the snackbar. A <see cref="SymbolIcon"/> (or any
    /// control) is the common value; strings such as <c>"Checkmark24"</c> are converted to
    /// a <see cref="SymbolIcon"/> automatically.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the visual appearance of the snackbar.
    /// </summary>
    public ControlAppearance Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    /// <summary>
    /// Gets or sets how long the snackbar stays visible before it closes automatically.
    /// </summary>
    public TimeSpan Timeout
    {
        get => GetValue(TimeoutProperty);
        set => SetValue(TimeoutProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the close (X) button is shown.
    /// </summary>
    public bool IsCloseButtonEnabled
    {
        get => GetValue(IsCloseButtonEnabledProperty);
        set => SetValue(IsCloseButtonEnabledProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<Button>(PART_CloseButton) is { } closeButton)
            closeButton.Click += (_, _) => Close();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
            IconHelper.TryConvertStringIcon(this, IconProperty, change.NewValue);
    }

    /// <summary>
    /// Shows the snackbar. It stays visible for <see cref="Timeout"/> and then closes.
    /// </summary>
    public void Show()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Show);
            return;
        }

        _showTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        IsVisible = true;

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = Timeout };
        _timer.Tick += (_, _) => Close();
        _timer.Start();
    }

    /// <summary>
    /// Shows the snackbar and returns a task that completes when it closes (after
    /// <see cref="Timeout"/> or when <see cref="Close"/> is invoked).
    /// </summary>
    public Task ShowAsync()
    {
        Show();
        return _showTcs?.Task ?? Task.CompletedTask;
    }

    /// <summary>
    /// Hides the snackbar immediately.
    /// </summary>
    public void Close()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Close);
            return;
        }

        _timer?.Stop();
        _timer = null;

        IsVisible = false;

        var tcs = Interlocked.Exchange(ref _showTcs, null);
        tcs?.TrySetResult(true);
    }
}

/// <summary>
/// Thin wrapper around a <see cref="SnackbarPresenter"/>: forwards the display properties
/// and <see cref="Show()"/>/<see cref="ShowAsync()"/> calls to the presenter.
/// </summary>
public class Snackbar
{
    private readonly SnackbarPresenter _presenter;

    /// <summary>
    /// Initializes a new instance wrapping the given presenter.
    /// </summary>
    public Snackbar(SnackbarPresenter presenter)
    {
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
    }

    /// <summary>
    /// Gets or sets the visual appearance of the snackbar.
    /// </summary>
    public ControlAppearance Appearance
    {
        get => _presenter.Appearance;
        set => _presenter.Appearance = value;
    }

    /// <summary>
    /// Gets or sets the icon shown in the snackbar.
    /// </summary>
    public object? Icon
    {
        get => _presenter.Icon;
        set => _presenter.Icon = value;
    }

    /// <summary>
    /// Gets or sets how long the snackbar stays visible before it closes automatically.
    /// </summary>
    public TimeSpan Timeout
    {
        get => _presenter.Timeout;
        set => _presenter.Timeout = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the close (X) button is shown.
    /// </summary>
    public bool IsCloseButtonEnabled
    {
        get => _presenter.IsCloseButtonEnabled;
        set => _presenter.IsCloseButtonEnabled = value;
    }

    /// <summary>
    /// Gets or sets the snackbar title text.
    /// </summary>
    public string? Title
    {
        get => _presenter.Title;
        set => _presenter.Title = value;
    }

    /// <summary>
    /// Gets or sets the snackbar content.
    /// </summary>
    public object? Content
    {
        get => _presenter.Content;
        set => _presenter.Content = value;
    }

    /// <summary>
    /// Gets or sets the snackbar width.
    /// </summary>
    public double Width
    {
        get => _presenter.Width;
        set => _presenter.Width = value;
    }

    /// <summary>
    /// Gets or sets the snackbar maximum width.
    /// </summary>
    public double MaxWidth
    {
        get => _presenter.MaxWidth;
        set => _presenter.MaxWidth = value;
    }

    /// <summary>
    /// Gets or sets the snackbar minimum width.
    /// </summary>
    public double MinWidth
    {
        get => _presenter.MinWidth;
        set => _presenter.MinWidth = value;
    }

    /// <summary>
    /// Gets or sets the snackbar horizontal alignment.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment
    {
        get => _presenter.HorizontalAlignment;
        set => _presenter.HorizontalAlignment = value;
    }

    /// <summary>
    /// Shows the snackbar.
    /// </summary>
    public void Show() => _presenter.Show();

    /// <summary>
    /// Shows the snackbar and returns a task completing when it closes.
    /// </summary>
    public Task ShowAsync() => _presenter.ShowAsync();
}
