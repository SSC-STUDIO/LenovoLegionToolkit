using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Shell;

public enum AppStatusBannerSeverity
{
    Warning,
    Success
}

/// <summary>
/// Migrated from the WPF AppStatusBanner: a persistent bottom-right software
/// status banner with an icon, message, action affordance and close button.
/// </summary>
public partial class AppStatusBanner : UserControl
{
    public static readonly StyledProperty<AppStatusBannerSeverity> SeverityProperty =
        AvaloniaProperty.Register<AppStatusBanner, AppStatusBannerSeverity>(
            nameof(Severity),
            AppStatusBannerSeverity.Warning);

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<AppStatusBanner, string>(
            nameof(Message),
            string.Empty);

    public static readonly StyledProperty<bool> IsPersistentProperty =
        AvaloniaProperty.Register<AppStatusBanner, bool>(
            nameof(IsPersistent),
            true);

    public AppStatusBannerSeverity Severity
    {
        get => GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsPersistent
    {
        get => GetValue(IsPersistentProperty);
        set => SetValue(IsPersistentProperty, value);
    }

    public event EventHandler? Closed;

    public AppStatusBanner()
    {
        InitializeComponent();
        MessageProperty.Changed.AddClassHandler<AppStatusBanner>((banner, _) =>
        {
            if (banner.MessageText is not null)
                banner.MessageText.Text = banner.Message;
        });
        SeverityProperty.Changed.AddClassHandler<AppStatusBanner>((banner, _) => banner.ApplySeverity());
        ApplySeverity();
        // Do NOT raise Closed when the host collapses the banner: initial load is
        // collapsed and that would fire Closed before the host ever shows it.
        // Closed is only raised from Hide() (close button).
    }

    private void ApplySeverity()
    {
        if (Severity == AppStatusBannerSeverity.Success)
        {
            Icon.IconIdentifier = "ArrowSync24";
            Icon.Foreground = TryBrush("StatusSuccessBrush") ?? Icon.Foreground;
            ActionArea.IsVisible = true;
            return;
        }

        ActionArea.IsVisible = false;
        Icon.IconIdentifier = "Warning24";
        Icon.Foreground = TryBrush("StatusWarningBrush") ?? Icon.Foreground;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Hide();
    }

    public void Hide()
    {
        if (!IsVisible)
            return;

        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private IBrush? TryBrush(object key) =>
        this.TryFindResource(key, out var resource) ? resource as IBrush : null;
}
