using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Shell;

public enum AppStatusBannerSeverity
{
    Warning,
    Success
}

public partial class AppStatusBanner : UserControl
{
    public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(
        nameof(Severity),
        typeof(AppStatusBannerSeverity),
        typeof(AppStatusBanner),
        new PropertyMetadata(AppStatusBannerSeverity.Warning, OnSeverityChanged));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(AppStatusBanner),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPersistentProperty = DependencyProperty.Register(
        nameof(IsPersistent),
        typeof(bool),
        typeof(AppStatusBanner),
        new PropertyMetadata(true));

    public AppStatusBannerSeverity Severity
    {
        get => (AppStatusBannerSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsPersistent
    {
        get => (bool)GetValue(IsPersistentProperty);
        set => SetValue(IsPersistentProperty, value);
    }

    public event RoutedEventHandler? Closed;

    public AppStatusBanner()
    {
        InitializeComponent();
        ApplySeverity();
        IsVisibleChanged += (_, _) => RaiseClosedIfHidden();
    }

    private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppStatusBanner banner)
            banner.ApplySeverity();
    }

    private void ApplySeverity()
    {
        if (Severity == AppStatusBannerSeverity.Success)
        {
            Icon.Symbol = SymbolRegular.ArrowSync24;
            Icon.SetResourceReference(Control.ForegroundProperty, "StatusSuccessBrush");
            ActionArea.Visibility = Visibility.Visible;
            return;
        }

        ActionArea.Visibility = Visibility.Collapsed;
        Icon.Symbol = SymbolRegular.Warning24;
        Icon.SetResourceReference(Control.ForegroundProperty, "StatusWarningBrush");
    }

    private void CloseButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Hide();
    }

    public void Hide()
    {
        if (Visibility == Visibility.Collapsed)
            return;

        Visibility = Visibility.Collapsed;
        Closed?.Invoke(this, new RoutedEventArgs());
    }

    private void RaiseClosedIfHidden()
    {
        if (Visibility == Visibility.Collapsed)
            Closed?.Invoke(this, new RoutedEventArgs());
    }
}
