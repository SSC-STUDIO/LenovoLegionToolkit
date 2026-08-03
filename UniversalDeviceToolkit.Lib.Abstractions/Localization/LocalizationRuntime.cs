using System.Globalization;
using System.Text;

namespace UniversalDeviceToolkit.Abstractions.Localization;

/// <summary>
/// Process-wide language lifecycle shared by the desktop, CLI and installer hosts.
/// </summary>
public static class LocalizationRuntime
{
    private static readonly object SyncRoot = new();
    private static CultureInfo _currentCulture = LocalizationCatalog.DefaultCulture;

    public static event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public static CultureInfo CurrentCulture
    {
        get
        {
            lock (SyncRoot)
                return _currentCulture;
        }
    }

    public static string LanguageFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UniversalDeviceToolkit",
        "lang");

    public static CultureInfo Initialize(CultureInfo? preferred = null, bool persist = false)
    {
        var culture = preferred;
        if (culture is null && File.Exists(LanguageFilePath))
        {
            try
            {
                culture = LocalizationCatalog.NormalizeCulture(File.ReadAllText(LanguageFilePath, Encoding.UTF8));
            }
            catch
            {
                culture = null;
            }
        }

        culture ??= LocalizationCatalog.NormalizeCulture(CultureInfo.CurrentUICulture);
        Apply(culture, persist);
        return CurrentCulture;
    }

    public static CultureInfo Initialize(string? preferredCultureName, bool persist = false) =>
        Initialize(LocalizationCatalog.NormalizeCulture(preferredCultureName), persist);

    public static Task<CultureInfo> SetCultureAsync(CultureInfo culture, bool persist = true)
    {
        var normalized = LocalizationCatalog.NormalizeCulture(culture);
        Apply(normalized, persist);
        return Task.FromResult(normalized);
    }

    public static Task<CultureInfo> SetCultureAsync(string? cultureName, bool persist = true) =>
        SetCultureAsync(LocalizationCatalog.NormalizeCulture(cultureName), persist);

    private static void Apply(CultureInfo culture, bool persist)
    {
        CultureInfo previous;
        lock (SyncRoot)
        {
            previous = _currentCulture;
            _currentCulture = culture;
        }

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        // Keep formatting deterministic for hardware values and command output.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        if (persist)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LanguageFilePath)!);
                File.WriteAllText(LanguageFilePath, culture.Name, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch
            {
                // A language preference must never prevent the application from starting.
            }
        }

        if (!previous.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
            CultureChanged?.Invoke(null, new CultureChangedEventArgs(previous, culture));
    }
}

public sealed class CultureChangedEventArgs(CultureInfo previousCulture, CultureInfo culture) : EventArgs
{
    public CultureInfo PreviousCulture { get; } = previousCulture;
    public CultureInfo Culture { get; } = culture;
}
