using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Windows.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

public static class MessageBoxHelper
{
    public static Task<bool> ShowAsync(Control dependencyObject,
        string title,
        string message,
        string? leftButton = null,
        string? rightButton = null
    )
    {
        var window = TopLevel.GetTopLevel(dependencyObject) as Window
                     ?? UdtAppContext.MainWindow
                     ?? throw new InvalidOperationException("Cannot show message without window");
        return ShowAsync(window, title, message, leftButton, rightButton);
    }

    public static async Task<bool> ShowAsync(Window window,
        string title,
        string message,
        string? primaryButton = null,
        string? secondaryButton = null)
    {
        var result = await NativeMessageBox.ShowAsync(message, title, MessageBoxButton.YesNo, MessageBoxImage.Information, window);
        return result == MessageBoxResult.Yes;
    }

    public static Task<string?> ShowInputAsync(
        Control dependencyObject,
        string title,
        string? placeholder = null,
        string? text = null,
        string? primaryButton = null,
        string? secondaryButton = null,
        bool allowEmpty = false
    )
    {
        var window = TopLevel.GetTopLevel(dependencyObject) as Window
                     ?? UdtAppContext.MainWindow
                     ?? throw new InvalidOperationException("Cannot show message without window");
        return ShowInputAsync(window, title, placeholder, text, primaryButton, secondaryButton, allowEmpty);
    }

    public static async Task<string?> ShowInputAsync(
        Window window,
        string title,
        string? placeholder = null,
        string? text = null,
        string? primaryButton = null,
        string? secondaryButton = null,
        bool allowEmpty = false
    )
    {
        var dialog = new InputDialogWindow(
            title,
            placeholder,
            text,
            primaryButton ?? Resource.OK,
            secondaryButton ?? Resource.Cancel,
            allowEmpty,
            window);

        if (window is { IsVisible: true })
            await dialog.ShowDialog(window);
        else
            dialog.Show();

        if (dialog.InputText is null)
            return null;

        var input = dialog.InputText.Trim();
        var normalized = string.IsNullOrWhiteSpace(input) ? null : input;
        return !allowEmpty && normalized is null ? null : normalized;
    }
}
