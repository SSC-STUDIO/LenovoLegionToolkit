using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.WPF.Windows;
using Wpf.Ui.Common;
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
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var snackBar = mainWindow?.Snackbar;

        if (snackBar is null)
            return;

        SetupSnackbarAppearance(snackBar, msg.Title, msg.Message, msg.Type);
        SetTitleAndMessage(snackBar, msg.Title, msg.Message);

        await snackBar.ShowAsync();
        
        // Wait for the snackbar to close before showing the next one
        // Snackbar has a Timeout property, we should wait at least that long
        var timeout = snackBar.Timeout;
        await Task.Delay(timeout + 500); // Add a small buffer for animation
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
            SnackbarType.Warning => SymbolRegular.Warning24,
            SnackbarType.Error => SymbolRegular.ErrorCircle24,
            SnackbarType.Info => SymbolRegular.Info24,
            _ => SymbolRegular.Checkmark24
        };
        snackBar.Timeout = type switch
        {
            SnackbarType.Success => 2000,
            _ => Math.Clamp(GetTextLengthInMilliseconds(title, message), 5000, 10000)
        };
        snackBar.CloseButtonEnabled = type switch
        {
            SnackbarType.Success => false,
            _ => true
        };
    }

    private static void SetTitleAndMessage(FrameworkElement snackBar, string title, string? message)
    {
        if (snackBar.FindName("_snackbarTitle") is TextBlock snackbarTitle)
            snackbarTitle.Text = title;

        if (snackBar.FindName("_snackbarMessage") is TextBlock snackbarMessage)
        {
            snackbarMessage.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
            snackbarMessage.Text = message;
        }
    }

    private static int GetTextLengthInMilliseconds(string title, string? message)
    {
        var length = 2 + (title.Length + (message?.Length ?? 0)) % 10;
        return length * 1000;
    }
}
