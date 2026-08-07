#if WINDOWS

using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Owns the Avalonia implementation of the shared OSD contract. It keeps the
/// persisted settings and automation messages compatible with WPF while using
/// host-neutral sensor readings for the rendered overlay.
/// </summary>
internal sealed class AvaloniaOsdOverlayController : IDisposable
{
    private readonly IPlatformServices _platformServices;
    private readonly OsdSettings _settings;
    private AvaloniaOsdOverlayWindow? _window;
    private bool _initialized;
    private bool _disposed;

    public AvaloniaOsdOverlayController(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        _settings = IoCContainer.TryResolve<OsdSettings>() ?? new OsdSettings();
    }

    public void Initialize()
    {
        if (_initialized || _disposed)
            return;

        _initialized = true;
        MessagingCenter.Subscribe<OsdChangedMessage>(this, message =>
            Dispatcher.UIThread.Post(() => HandleState(message.State)));
        MessagingCenter.Subscribe<OsdAppearanceChangedMessage>(this, _ =>
            Dispatcher.UIThread.Post(ApplyAppearance));
        MessagingCenter.Subscribe<OsdElementChangedMessage>(this, _ =>
            Dispatcher.UIThread.Post(RefreshVisibleOverlay));

        if (_settings.Store.ShowOsd)
            HandleState(OsdState.Show);
    }

    private void HandleState(OsdState state)
    {
        if (_disposed)
            return;

        switch (state)
        {
            case OsdState.Hidden:
                _window?.Hide();
                break;
            case OsdState.Show:
                EnsureCorrectStyle();
                _window?.Show();
                _window?.Refresh();
                break;
            case OsdState.Toggle:
                if (_window?.IsVisible == true)
                    _window.Hide();
                else
                {
                    EnsureCorrectStyle();
                    _window?.Show();
                    _window?.Refresh();
                }
                break;
        }

        _settings.Store.ShowOsd = _window?.IsVisible == true;
        _settings.SynchronizeStore();
    }

    private void ApplyAppearance()
    {
        if (_disposed)
            return;

        var wasVisible = _window?.IsVisible == true;
        EnsureCorrectStyle();
        _window?.ApplySettings();
        if (wasVisible)
        {
            _window?.Show();
            _window?.Refresh();
        }
    }

    private void RefreshVisibleOverlay()
    {
        if (_window?.IsVisible == true)
            _window.Refresh();
    }

    private void EnsureCorrectStyle()
    {
        var style = _settings.Store.SelectedStyleIndex == 1
            ? AvaloniaOsdStyle.Bar
            : AvaloniaOsdStyle.Panel;
        if (_window is not null && _window.Style == style)
            return;

        var wasVisible = _window?.IsVisible == true;
        _window?.Close();
        _window = new AvaloniaOsdOverlayWindow(_platformServices, _settings, style);
        _window.Closed += OnWindowClosed;
        if (wasVisible)
            _window.Show();
    }

    private void OnWindowClosed(object? sender, EventArgs args)
    {
        if (ReferenceEquals(sender, _window))
            _window = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        MessagingCenter.Unsubscribe(this);
        if (_window is not null)
            _window.Closed -= OnWindowClosed;
        _window?.Close();
        _window = null;
    }
}

#endif
