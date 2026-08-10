using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Keeps an in-flight language pack install alive across settings navigation and page recreation.
/// </summary>
public sealed class LanguagePackInstallCoordinator(LanguagePackManager languagePackManager)
{
    private readonly object _sync = new();
    private Task? _activeTask;
    private CultureInfo? _culture;
    private float _progress;

    public bool IsActive { get; private set; }

    public CultureInfo? Culture
    {
        get
        {
            lock (_sync)
                return _culture;
        }
    }

    public float Progress
    {
        get
        {
            lock (_sync)
                return _progress;
        }
    }

    public event EventHandler? Changed;

    public Task InstallAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        if (culture is null)
            throw new ArgumentNullException(nameof(culture));

        lock (_sync)
        {
            if (_activeTask is { IsCompleted: false })
                return _activeTask;
        }

        _culture = culture;
        _progress = 0f;
        IsActive = true;
        RaiseChanged();

        var progress = new Progress<float>(value =>
        {
            lock (_sync)
                _progress = value;
            RaiseChanged();
        });

        _activeTask = InstallCoreAsync(culture, progress, cancellationToken);
        return _activeTask;
    }

    private async Task InstallCoreAsync(CultureInfo culture, IProgress<float> progress, CancellationToken cancellationToken)
    {
        try
        {
            await languagePackManager.InstallAsync(culture, progress, cancellationToken);
        }
        finally
        {
            lock (_sync)
            {
                IsActive = false;
                _culture = null;
                _progress = 0f;
                _activeTask = null;
            }

            RaiseChanged();
        }
    }

    private void RaiseChanged()
    {
        var dispatcher = Dispatcher.UIThread;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.Post(() => Changed?.Invoke(this, EventArgs.Empty));
        else
            Changed?.Invoke(this, EventArgs.Empty);
    }
}
