using System.Globalization;

namespace UniversalDeviceToolkit.Avalonia.Services;

public enum LanguagePackFailureKind
{
    Cancelled,
    CatalogUnavailable,
    CultureNotInCatalog,
    AppVersionTooOld,
    DownloadFailed,
    HashMismatch,
    CorruptPackage,
    ValidationFailed,
    ApplyFailed,
    Unknown,
}

public sealed class LanguagePackException : Exception
{
    public LanguagePackFailureKind Kind { get; }
    public string? Culture { get; }

    public LanguagePackException(LanguagePackFailureKind kind, string message, string? culture = null, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        Culture = culture;
    }
}

public sealed record LanguagePackCatalogEntry(
    string Culture,
    string? Parent,
    long Size,
    string Sha256,
    string? ResourceVersion,
    string? MinAppVersion,
    string Url,
    string DisplayName);

public sealed record AvaloniaLanguageOption(
    CultureInfo Culture,
    string DisplayName,
    bool IsInstalled,
    bool IsEnglish);

public interface IAvaloniaLanguagePackService
{
    bool IsAvailable { get; }
    bool IsActive { get; }
    CultureInfo? ActiveCulture { get; }
    float Progress { get; }
    event EventHandler? Changed;

    Task<IReadOnlyList<AvaloniaLanguageOption>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    bool IsEnglish(CultureInfo culture);
    bool IsInstalled(CultureInfo culture);
    Task InstallAsync(CultureInfo culture, CancellationToken cancellationToken = default);
    Task UninstallAsync(CultureInfo culture, CancellationToken cancellationToken = default);
}

public static class AvaloniaLanguagePackServiceFactory
{
    private static readonly object SyncRoot = new();
    private static IAvaloniaLanguagePackService? _instance;

    public static IAvaloniaLanguagePackService Create()
    {
        lock (SyncRoot)
            return _instance ??= CreateCore();
    }

    private static IAvaloniaLanguagePackService CreateCore() =>
#if WINDOWS
        new WindowsAvaloniaLanguagePackService();
#else
        new UnavailableAvaloniaLanguagePackService();
#endif
}
