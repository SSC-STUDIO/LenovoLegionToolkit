using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Controls.Shell;

public enum AppStatusBannerSeverity
{
    Warning,
    Success
}

/// <summary>
/// A persistent bottom-right status banner with an icon, message, action affordance
/// and close button (port of the WPF AppStatusBanner).
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

    static AppStatusBanner()
    {
        SeverityProperty.Changed.AddClassHandler<AppStatusBanner>((banner, _) => banner.ApplySeverity());
    }

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

    public event EventHandler<RoutedEventArgs>? Closed;

    public AppStatusBanner()
    {
        InitializeComponent();
        ApplySeverity();
        // Do NOT raise Closed on IsVisibleChanged→Collapsed: initial load is Collapsed and
        // that would fire Closed before the host ever shows the banner (false "user dismissed").
        // Closed is only raised from Hide() (close button).
    }

    private void ApplySeverity()
    {
        if (Severity == AppStatusBannerSeverity.Success)
        {
            Icon.Symbol = SymbolRegular.ArrowSync24;
            Icon.SetResourceReference(TextBlock.ForegroundProperty, "StatusSuccessBrush");
            ActionArea.IsVisible = true;
            return;
        }

        ActionArea.IsVisible = false;
        Icon.Symbol = SymbolRegular.Warning24;
        Icon.SetResourceReference(TextBlock.ForegroundProperty, "StatusWarningBrush");
    }

    private void CloseButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Handled = true;
        Hide();
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
        Closed?.Invoke(this, new RoutedEventArgs());
    }
}
