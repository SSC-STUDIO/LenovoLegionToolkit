using Avalonia;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Creates in-app snackbars that share the AppStatusBanner / NotificationToast visual language.
/// </summary>
public static class NotificationToastFactory
{
    public static Snackbar Create(SnackbarPresenter presenter, HorizontalAlignment alignment = HorizontalAlignment.Right)
    {
        var width = UdtAppContext.GetResource("NotificationToastWidth") is double toastWidth
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
            FontSize = UdtAppContext.GetResource("FontSizeBody") is double bodySize ? bodySize : 15,
            FontWeight = FontWeight.Medium,
            TextWrapping = TextWrapping.WrapWithOverflow
        };
        snackbarTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        var snackbarMessage = new TextBlock
        {
            Name = "_snackbarMessage",
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = UdtAppContext.GetResource("FontSizeCaption") is double captionSize ? captionSize : 14,
            TextWrapping = TextWrapping.WrapWithOverflow
        };
        snackbarMessage.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");

        var panel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(snackbarTitle);
        panel.Children.Add(snackbarMessage);
        return panel;
    }
}
