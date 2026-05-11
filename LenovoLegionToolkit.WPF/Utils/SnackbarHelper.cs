using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.WPF.Windows;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Utils;

public static class SnackbarHelper
{
    /// <summary>Shows a snackbar with title and optional body (WPF-UI 4: <see cref="Snackbar.ShowAsync()"/> has no text parameters).</summary>
    public static async Task ShowSnackbarAsync(Snackbar snackbar, string title, string? message = null)
    {
        snackbar.Title = title;
        snackbar.Content = string.IsNullOrEmpty(message)
            ? null
            : new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
        await snackbar.ShowAsync();
    }

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
        await Task.Delay(timeout + TimeSpan.FromMilliseconds(500));
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
        snackBar.Timeout = type switch
        {
            SnackbarType.Success => TimeSpan.FromMilliseconds(2000),
            _ => TimeSpan.FromMilliseconds(Math.Clamp(GetTextLengthInMilliseconds(title, message), 5000, 10000))
        };
        snackBar.IsCloseButtonEnabled = type switch
        {
            SnackbarType.Success => false,
            _ => true
        };
    }

    private static void SetTitleAndMessage(FrameworkElement snackBar, string title, string? message)
    {
        if (snackBar is not Snackbar snackbar)
            return;

        snackbar.Title = title;
        snackbar.Content = string.IsNullOrEmpty(message)
            ? null
            : new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
    }

    private static int GetTextLengthInMilliseconds(string title, string? message)
    {
        var length = 2 + (title.Length + (message?.Length ?? 0)) % 10;
        return length * 1000;
    }
}
