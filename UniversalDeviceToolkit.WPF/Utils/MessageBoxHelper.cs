using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace UniversalDeviceToolkit.WPF.Utils;

public static class MessageBoxHelper
{
    public static Task<bool> ShowAsync(DependencyObject dependencyObject,
        string title,
        string message,
        string? leftButton = null,
        string? rightButton = null
    )
    {
        var window = Window.GetWindow(dependencyObject)
                     ?? Application.Current.MainWindow
                     ?? throw new InvalidOperationException("Cannot show message without window");
        return ShowAsync(window, title, message, leftButton, rightButton);
    }

    public static Task<bool> ShowAsync(Window window,
        string title,
        string message,
        string? primaryButton = null,
        string? secondaryButton = null)
    {
        var tcs = new TaskCompletionSource<bool>();

        var messageBox = new MessageBox
        {
            Owner = window,
            Title = title,
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (System.Windows.Media.Brush)window.FindResource("TextFillColorPrimaryBrush"),
            },
            PrimaryButtonText = primaryButton ?? Resource.Yes,
            SecondaryButtonText = secondaryButton ?? Resource.No,
            ShowInTaskbar = false,
            Topmost = false,
            ResizeMode = ResizeMode.NoResize,
        };
        _ = messageBox.ShowDialogAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
                tcs.TrySetResult(t.Result == MessageBoxResult.Primary);
            else
                tcs.TrySetResult(false);
        }, TaskScheduler.FromCurrentSynchronizationContext());

        return tcs.Task;
    }

    public static Task<string?> ShowInputAsync(
        DependencyObject dependencyObject,
        string title,
        string? placeholder = null,
        string? text = null,
        string? primaryButton = null,
        string? secondaryButton = null,
        bool allowEmpty = false
    )
    {
        var window = Window.GetWindow(dependencyObject)
                     ?? Application.Current.MainWindow
                     ?? throw new InvalidOperationException("Cannot show message without window");
        return ShowInputAsync(window, title, placeholder, text, primaryButton, secondaryButton, allowEmpty);
    }

    public static Task<string?> ShowInputAsync(
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

        var result = dialog.ShowDialog();
        if (result != true)
            return Task.FromResult<string?>(null);

        var input = dialog.InputText?.Trim();
        var normalized = string.IsNullOrWhiteSpace(input) ? null : input;
        return Task.FromResult(!allowEmpty && normalized is null ? null : normalized);
    }
}
