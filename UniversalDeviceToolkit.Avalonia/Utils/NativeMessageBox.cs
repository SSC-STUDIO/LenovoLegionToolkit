using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Utils;

public enum MessageBoxButton
{
    OK,
    OKCancel,
    YesNoCancel,
    YesNo
}

public enum MessageBoxImage
{
    None,
    Error,
    Hand,
    Stop,
    Question,
    Exclamation,
    Warning,
    Asterisk,
    Information
}

public enum MessageBoxResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

/// <summary>
/// Avalonia stand-in for <see cref="System.Windows.MessageBox"/> with the same
/// call surface used across the app. Renders a modal, app-styled dialog.
/// </summary>
public static class NativeMessageBox
{
    public static MessageBoxResult Show(string messageBoxText, string caption,
        MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None,
        Window? owner = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.Invoke(() =>
                Show(messageBoxText, caption, button, icon, owner));
        }

        var window = owner ?? UdtAppContext.MainWindow;
        var dialog = BuildDialog(messageBoxText, caption, button);
        return ShowDialog(window, dialog);
    }

    public static Task<MessageBoxResult> ShowAsync(string messageBoxText, string caption,
        MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None,
        Window? owner = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
                ShowAsync(messageBoxText, caption, button, icon, owner));
        }

        var window = owner ?? UdtAppContext.MainWindow;
        var dialog = BuildDialog(messageBoxText, caption, button);
        return ShowDialogAsync(window, dialog);
    }

    private static MessageBoxResult ShowDialog(Window? owner, Window dialog)
    {
        return ShowDialogAsync(owner, dialog).GetAwaiter().GetResult();
    }

    private static async Task<MessageBoxResult> ShowDialogAsync(Window? owner, Window dialog)
    {
        var tcs = new TaskCompletionSource<MessageBoxResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.Closed += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.TrySetResult(dialog.Tag as MessageBoxResult? ?? MessageBoxResult.None);
        };

        if (owner is { IsVisible: true })
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        return await tcs.Task;
    }

    private static Window BuildDialog(string messageBoxText, string caption, MessageBoxButton button)
    {
        var window = new Window
        {
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.Full,
            Title = caption
        };

        var background = TryBrush("ApplicationBackgroundBrush", new SolidColorBrush(Color.Parse("#202020")));
        var foreground = TryBrush("TextFillColorPrimaryBrush", new SolidColorBrush(Color.Parse("#E8E8E8")));
        var borderBrush = TryBrush("CardStrokeColorDefaultBrush", new SolidColorBrush(Color.Parse("#4C4C4C")));
        var buttonBrush = TryBrush("ControlFillColorDefaultBrush", new SolidColorBrush(Color.Parse("#2E2E2E")));
        var buttonHoverBrush = TryBrush("ControlFillColorSecondaryBrush", new SolidColorBrush(Color.Parse("#3A3A3A")));

        var messageText = new TextBlock
        {
            Text = messageBoxText,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 340,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = foreground,
            FontSize = 14,
            LineHeight = 20
        };

        StackPanel buttonsPanel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 24, 0, 0)
        };

        var resultButtons = new List<Button>();

        Button AddButton(string text, MessageBoxResult result, bool isDefault)
        {
            var b = new Button
            {
                Content = text,
                MinWidth = 96,
                Padding = new Thickness(16, 6),
                FontSize = 13,
                Background = buttonBrush,
                BorderBrush = borderBrush,
                CornerRadius = new CornerRadius(6),
                Foreground = foreground
            };
            b.Classes.Add("accent");
            b.PointerEntered += (_, _) => b.Background = buttonHoverBrush;
            b.PointerExited += (_, _) => b.Background = buttonBrush;
            b.Click += (_, _) =>
            {
                window.Tag = result;
                window.Close();
            };
            buttonsPanel.Children.Add(b);
            resultButtons.Add(b);
            return b;
        }

        switch (button)
        {
            case MessageBoxButton.OK:
                AddButton(Resource.OK, MessageBoxResult.OK, isDefault: true);
                break;
            case MessageBoxButton.OKCancel:
                AddButton(Resource.OK, MessageBoxResult.OK, isDefault: true);
                AddButton(Resource.Cancel, MessageBoxResult.Cancel, isDefault: false);
                break;
            case MessageBoxButton.YesNo:
                AddButton(Resource.Yes, MessageBoxResult.Yes, isDefault: true);
                AddButton(Resource.No, MessageBoxResult.No, isDefault: false);
                break;
            case MessageBoxButton.YesNoCancel:
                AddButton(Resource.Yes, MessageBoxResult.Yes, isDefault: true);
                AddButton(Resource.No, MessageBoxResult.No, isDefault: false);
                AddButton(Resource.Cancel, MessageBoxResult.Cancel, isDefault: false);
                break;
        }

        var grid = new Grid
        {
            Background = background,
            Margin = new Thickness(24),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };

        grid.Children.Add(messageText);
        Grid.SetRow(messageText, 0);
        grid.Children.Add(buttonsPanel);
        Grid.SetRow(buttonsPanel, 1);

        window.Content = grid;
        window.Tag = MessageBoxResult.None;

        return window;
    }

    private static IBrush? TryBrush(string key, IBrush fallback)
    {
        if (Application.Current?.Resources.TryGetResource(key, Application.Current.RequestedThemeVariant, out var value) == true &&
            value is IBrush brush)
            return brush;
        return fallback;
    }
}
