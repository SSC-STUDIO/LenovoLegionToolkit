using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LenovoLegionToolkit.WPF.Windows;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Utils;

public static class SnackbarHelper
{
    private class SnackbarMessage
    {
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public SnackbarType Type { get; set; }
        public int Priority => Type switch
        {
            SnackbarType.Error => 2,
            SnackbarType.Warning => 1,
            _ => 0
        };
    }

    private static readonly PriorityQueue<SnackbarMessage, int> _queue = new();
    private static bool _isShowing;

    public static async Task ShowAsync(string title, string? message = null, SnackbarType type = SnackbarType.Success)
    {
        var msg = new SnackbarMessage { Title = title, Message = message, Type = type };
        _queue.Enqueue(msg, 2 - msg.Priority); // 0 is highest priority in PriorityQueue

        if (_isShowing)
            return;

        _isShowing = true;
        try
        {
            while (_queue.Count > 0)
            {
                var nextMsg = _queue.Dequeue();
                await ProcessSnackbar(nextMsg);
            }
        }
        finally
        {
            _isShowing = false;
        }
    }

    public static void Show(string title, string? message = null, SnackbarType type = SnackbarType.Success)
    {
        _ = ShowAsync(title, message, type);
    }

    private static async Task ProcessSnackbar(SnackbarMessage msg)
    {
        if (Application.Current is null)
            return;

        MainWindow? mainWindow = null;
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
        });

        var snackBar = mainWindow?.Snackbar;

        if (snackBar is null)
            return;

        var timeoutTask = await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            SetupSnackbarAppearance(snackBar, msg.Title, msg.Message, msg.Type);
            SetTitleAndMessage(snackBar, msg.Title, msg.Message);
            await snackBar.ShowAsync();

            return snackBar.Timeout;
        });
        var timeout = await timeoutTask;

        // Wait for the snackbar to close before showing the next one
        // Snackbar has a Timeout property, we should wait at least that long
        await Task.Delay(timeout + TimeSpan.FromMilliseconds(500)); // Add a small buffer for animation
    }

    private static void SetupSnackbarAppearance(Snackbar snackBar, string title, string? message, SnackbarType type)
    {
        snackBar.Appearance = type switch
        {
            SnackbarType.Warning => ControlAppearance.Caution,
            SnackbarType.Error => ControlAppearance.Danger,
            _ => ControlAppearance.Secondary
        };
        snackBar.Icon = type switch
        {
            SnackbarType.Warning => new SymbolIcon { Symbol = SymbolRegular.Warning24 },
            SnackbarType.Error => new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24 },
            SnackbarType.Info => new SymbolIcon { Symbol = SymbolRegular.Info24 },
            _ => new SymbolIcon { Symbol = SymbolRegular.Checkmark24 }
        };
        snackBar.Timeout = TimeSpan.FromMilliseconds(type switch
        {
            SnackbarType.Success => 2000,
            _ => Math.Clamp(GetTextLengthInMilliseconds(title, message), 5000, 10000)
        });
        snackBar.IsCloseButtonEnabled = type switch
        {
            SnackbarType.Success => false,
            _ => true
        };
    }

    private static void SetTitleAndMessage(Snackbar snackBar, string title, string? message)
    {
        if (FindNamedTextBlock(snackBar.Content, "_snackbarTitle") is { } snackbarTitle)
            snackbarTitle.Text = title;

        if (FindNamedTextBlock(snackBar.Content, "_snackbarMessage") is { } snackbarMessage)
        {
            snackbarMessage.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
            snackbarMessage.Text = message;
        }
    }

    private static TextBlock? FindNamedTextBlock(object? root, string name) =>
        root is DependencyObject dependencyObject ? FindNamedTextBlock(dependencyObject, name) : null;

    private static TextBlock? FindNamedTextBlock(DependencyObject root, string name)
    {
        if (root is TextBlock textBlock && string.Equals(textBlock.Name, name, StringComparison.Ordinal))
            return textBlock;

        foreach (var child in EnumerateChildren(root))
        {
            var match = FindNamedTextBlock(child, name);
            if (match is not null)
                return match;
        }

        return null;
    }

    private static IEnumerable<DependencyObject> EnumerateChildren(DependencyObject parent)
    {
        var visualChildrenCount = 0;

        try
        {
            visualChildrenCount = VisualTreeHelper.GetChildrenCount(parent);
        }
        catch (InvalidOperationException)
        {
            visualChildrenCount = 0;
        }

        for (var i = 0; i < visualChildrenCount; i++)
            yield return VisualTreeHelper.GetChild(parent, i);

        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is DependencyObject dependencyObject)
                yield return dependencyObject;
        }
    }

    private static int GetTextLengthInMilliseconds(string title, string? message)
    {
        var length = 2 + (title.Length + (message?.Length ?? 0)) % 10;
        return length * 1000;
    }
}
