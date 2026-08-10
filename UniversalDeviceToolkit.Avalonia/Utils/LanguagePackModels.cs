using System;

namespace UniversalDeviceToolkit.Avalonia.Utils;

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
    Unknown
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

public enum LanguageGateOutcome
{
    Continue,
    ContinueEnglish,
    Exit
}

/// <summary>
/// Unified catalog entry for language pack lifecycle operations.
/// </summary>
public sealed record LanguagePackCatalogEntry(
    string Culture,
    string? Parent,
    long Size,
    string Sha256,
    string? ResourceVersion,
    string? MinAppVersion,
    string Url,
    string DisplayName);
