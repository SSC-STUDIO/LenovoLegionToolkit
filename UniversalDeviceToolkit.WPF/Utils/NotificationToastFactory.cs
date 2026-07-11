using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Creates in-app snackbars that share the AppStatusBanner / NotificationToast visual language.
/// </summary>
public static class NotificationToastFactory
{
    public static Snackbar Create(SnackbarPresenter presenter, HorizontalAlignment alignment = HorizontalAlignment.Right)
    {
        var width = Application.Current.TryFindResource("NotificationToastWidth") is double toastWidth
            ? toastWidth
            : 360d;

        return new Snackbar(presenter)
        {
            Width = width,
            MaxWidth = width,
            MinWidth = width,
            HorizontalAlignment = alignment,
            IsCloseButtonEnabled = false,
            Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 },
            Content = CreateContent()
        };
    }

    public static StackPanel CreateContent()
    {
        var snackbarTitle = new TextBlock
        {
            Name = "_snackbarTitle",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = Application.Current.TryFindResource("FontSizeBody") is double bodySize ? bodySize : 15,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.WrapWithOverflow
        };
        snackbarTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        var snackbarMessage = new TextBlock
        {
            Name = "_snackbarMessage",
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = Application.Current.TryFindResource("FontSizeCaption") is double captionSize ? captionSize : 14,
            TextWrapping = TextWrapping.WrapWithOverflow
        };
        snackbarMessage.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

        var panel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(snackbarTitle);
        panel.Children.Add(snackbarMessage);
        return panel;
    }
}
