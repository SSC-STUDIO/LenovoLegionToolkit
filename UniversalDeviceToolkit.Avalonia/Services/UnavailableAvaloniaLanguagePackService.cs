using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

internal sealed class UnavailableAvaloniaLanguagePackService : IAvaloniaLanguagePackService
{
    public bool IsAvailable => false;
    public bool IsActive => false;
    public CultureInfo? ActiveCulture => null;
    public float Progress => 0f;
    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    public Task<IReadOnlyList<AvaloniaLanguageOption>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<AvaloniaLanguageOption>>(
            LocalizationCatalog.SupportedCultures
                .Select(culture => new AvaloniaLanguageOption(
                    culture,
                    LocalizationCatalog.GetDisplayName(culture),
                    IsInstalled: true,
                    IsEnglish: culture.Name.Equals("en", StringComparison.OrdinalIgnoreCase)))
                .ToArray());
    }

    public bool IsEnglish(CultureInfo culture) =>
        culture.Name.Equals("en", StringComparison.OrdinalIgnoreCase);

    public bool IsInstalled(CultureInfo culture) => true;

    public Task InstallAsync(CultureInfo culture, CancellationToken cancellationToken = default) =>
        Task.FromException(new PlatformNotSupportedException("Language packs are unavailable on this host."));

    public Task UninstallAsync(CultureInfo culture, CancellationToken cancellationToken = default) =>
        Task.FromException(new PlatformNotSupportedException("Language packs are unavailable on this host."));
}
