using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LenovoLegionToolkit.WPF.Resources;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using TextBox = Wpf.Ui.Controls.TextBox;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Utils;

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

    public static async Task<bool> ShowAsync(Window window,
        string title,
        string message,
        string? primaryButton = null,
        string? secondaryButton = null)
    {
        var messageBox = new MessageBox
        {
            Owner = window,
            Title = title,
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
            },
            PrimaryButtonText = primaryButton ?? Resource.Yes,
            SecondaryButtonText = secondaryButton ?? Resource.No,
            ShowInTaskbar = false,
            Topmost = false,
            ResizeMode = ResizeMode.NoResize,
        };

        var result = await messageBox.ShowDialogAsync();
        return result == Wpf.Ui.Controls.MessageBoxResult.Primary;
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
        var textBox = new TextBox
        {
            MaxLines = 1,
            MaxLength = 50,
            PlaceholderText = placeholder ?? string.Empty,
            TextWrapping = TextWrapping.Wrap
        };

        var messageBox = new MessageBox
        {
            Owner = window,
            Title = title,
            Content = textBox,
            PrimaryButtonAppearance = ControlAppearance.Transparent,
            PrimaryButtonText = primaryButton ?? Resource.OK,
            SecondaryButtonText = secondaryButton ?? Resource.Cancel,
            ShowInTaskbar = false,
            Topmost = false,
            MinHeight = 160,
            MaxHeight = 160,
            ResizeMode = ResizeMode.NoResize,
        };

        void SyncPrimaryEnabled()
        {
            var isEmpty = !allowEmpty && string.IsNullOrWhiteSpace(textBox.Text);
            messageBox.IsPrimaryButtonEnabled = !isEmpty;
            messageBox.PrimaryButtonAppearance = isEmpty ? ControlAppearance.Transparent : ControlAppearance.Primary;
        }

        textBox.TextChanged += (_, _) => SyncPrimaryEnabled();

        textBox.Text = text ?? string.Empty;
        textBox.SelectionStart = text?.Length ?? 0;
        textBox.SelectionLength = 0;
        SyncPrimaryEnabled();

        FocusManager.SetFocusedElement(window, textBox);

        var result = await messageBox.ShowDialogAsync();

        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary)
            return null;

        var content = textBox.Text?.Trim();
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }
}
