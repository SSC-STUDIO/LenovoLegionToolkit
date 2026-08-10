using System;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

public enum InfoBarSeverity
{
    Informational,
    Success,
    Warning,
    Error
}

// AVALONIA: WPF-UI InfoBar + AutomationPeer replaced by a ContentControl with the same
// public members. Template lives in Styles/InfoBar.axaml and binds the close button to
// TemplateButtonCommand.
public class InfoBar : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<InfoBar, string?>(
        nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<InfoBar, string?>(
        nameof(Message));

    public static readonly StyledProperty<InfoBarSeverity> SeverityProperty = AvaloniaProperty.Register<InfoBar, InfoBarSeverity>(
        nameof(Severity),
        InfoBarSeverity.Informational);

    public static readonly StyledProperty<bool> IsClosableProperty = AvaloniaProperty.Register<InfoBar, bool>(
        nameof(IsClosable),
        true);

    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<InfoBar, bool>(
        nameof(IsOpen),
        true);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public InfoBarSeverity Severity
    {
        get => GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public bool IsClosable
    {
        get => GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Raised when the close button is invoked.</summary>
    public event EventHandler? Closed;

    /// <summary>Template hook for the close button (was the internal TemplateButtonCommand on WPF-UI InfoBar).</summary>
    public IRelayCommand TemplateButtonCommand { get; }

    public InfoBar()
    {
        TemplateButtonCommand = new RelayCommand(Close);
    }

    private void Close()
    {
        IsOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}
