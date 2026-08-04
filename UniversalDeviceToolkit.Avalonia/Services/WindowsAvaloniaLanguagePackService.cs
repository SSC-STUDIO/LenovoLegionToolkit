#if WINDOWS

using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.ResourcesCatalog;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Avalonia host adapter for the same signed language-pack lifecycle used by WPF.
/// The manager owns safe extraction and satellite loading; this adapter owns UI state.
/// </summary>
internal sealed class WindowsAvaloniaLanguagePackService : IAvaloniaLanguagePackService
{
    private readonly LanguagePackManager _manager;
    private readonly object _gate = new();
    private float _progress;
    private bool _isActive;

    public WindowsAvaloniaLanguagePackService()
    {
        _manager = new LanguagePackManager(
            IoCContainer.TryResolve<OnlineResourceCatalogClient>()
            ?? new OnlineResourceCatalogClient(new HttpClientFactory()));
        _manager.ProcessPendingUninstall();
    }

    public bool IsAvailable => true;
    public bool IsActive { get { lock (_gate) return _isActive; } }
    public CultureInfo? ActiveCulture => LocalizationRuntime.CurrentCulture;
    public float Progress { get { lock (_gate) return _progress; } }
    public event EventHandler? Changed;

    public async Task<IReadOnlyList<AvaloniaLanguageOption>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var catalog = await TryQueryCatalogAsync(cancellationToken).ConfigureAwait(false);
        var catalogNames = catalog
            .Select(entry => entry.Culture)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return LocalizationCatalog.SupportedCultures
            .Where(culture => catalog.Count == 0 || catalogNames.Contains(culture.Name) || IsEnglish(culture))
            .Select(culture => new AvaloniaLanguageOption(
                culture,
                catalog.FirstOrDefault(entry => entry.Culture.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))?.DisplayName
                    is { Length: > 0 } displayName
                    ? displayName
                    : LocalizationCatalog.GetDisplayName(culture),
                _manager.IsInstalled(culture),
                IsEnglish(culture)))
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public bool IsEnglish(CultureInfo culture) => _manager.IsEnglish(culture);

    public bool IsInstalled(CultureInfo culture) => _manager.IsInstalled(culture);

    public async Task InstallAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        SetOperationState(active: true, progress: 0f);
        try
        {
            var progress = new Progress<float>(value => SetOperationState(active: true, progress: value));
            await _manager.InstallAsync(culture, progress, cancellationToken).ConfigureAwait(false);
            await LocalizationRuntime.SetCultureAsync(culture, persist: true).ConfigureAwait(false);
        }
        finally
        {
            SetOperationState(active: false, progress: 1f);
        }
    }

    public async Task UninstallAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsEnglish(culture))
            return;

        if (ActiveCulture?.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase) == true)
            await LocalizationRuntime.SetCultureAsync(LocalizationCatalog.DefaultCulture, persist: true).ConfigureAwait(false);

        _manager.QueueUninstall(culture);
        _manager.Uninstall(culture);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task<IReadOnlyList<LanguagePackCatalogEntry>> TryQueryCatalogAsync(CancellationToken token)
    {
        try
        {
            return await _manager.QueryCatalogAsync(token).ConfigureAwait(false);
        }
        catch (LanguagePackException) when (!token.IsCancellationRequested)
        {
            // Built-in resources remain selectable while the catalog is offline.
            return [];
        }
    }

    private void SetOperationState(bool active, float progress)
    {
        lock (_gate)
        {
            _isActive = active;
            _progress = Math.Clamp(progress, 0f, 1f);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}

#endif
