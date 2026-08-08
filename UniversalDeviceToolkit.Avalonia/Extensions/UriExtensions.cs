using System;
using System.Diagnostics;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class UriExtensions
{
    private static readonly char[] DangerousSchemeChars = { ':', '/', '\\', '*', '?', '"', '<', '>', '|' };

    /// <summary>
    /// Safely opens a URI using the default system handler.
    /// Only HTTP and HTTPS schemes are allowed to prevent command injection.
    /// </summary>
    /// <param name="uri">The URI to open</param>
    /// <exception cref="ArgumentException">Thrown when URI scheme is not HTTP or HTTPS</exception>
    /// <exception cref="InvalidOperationException">Thrown when URI is malformed</exception>
    public static void Open(this Uri uri)
    {
        if (uri is null)
            throw new ArgumentNullException(nameof(uri));

        // SECURITY: Only allow HTTP and HTTPS schemes to prevent command injection attacks.
        // Reject file://, ms-* (e.g. ms-settings, ms-windows-store) and any custom scheme.
        var scheme = uri.Scheme;
        if (string.IsNullOrEmpty(scheme)
            || scheme.IndexOfAny(DangerousSchemeChars) >= 0
            || scheme.StartsWith("ms-", StringComparison.OrdinalIgnoreCase)
            || scheme.StartsWith("file", StringComparison.OrdinalIgnoreCase)
            || scheme.StartsWith("javascript", StringComparison.OrdinalIgnoreCase)
            || scheme.StartsWith("vbscript", StringComparison.OrdinalIgnoreCase)
            || scheme.StartsWith("data", StringComparison.OrdinalIgnoreCase)
            || scheme.StartsWith("about", StringComparison.OrdinalIgnoreCase)
            || scheme.StartsWith("shell", StringComparison.OrdinalIgnoreCase)
            || !(scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                 || scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Only HTTP and HTTPS URIs are allowed for security reasons", nameof(uri));
        }

        // Validate that the URI is well-formed
        if (!Uri.IsWellFormedUriString(uri.AbsoluteUri, UriKind.Absolute))
        {
            throw new InvalidOperationException("The URI is not well-formed");
        }

        try
        {
            // SECURITY: The HTTP/HTTPS allow-list above prevents shell-injection from
            // file://, ms-*, javascript and other dangerous schemes. We must keep
            // UseShellExecute=true because Process.Start needs the shell to resolve the
            // URL to the registered browser handler; with UseShellExecute=false the
            // literal URL is handed to CreateProcess which does not understand URLs.
            using var process = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            _ = process;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to open URI: {ex.Message}", ex);
        }
    }
}
