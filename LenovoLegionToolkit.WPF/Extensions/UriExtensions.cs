using System;
using System.Diagnostics;

namespace LenovoLegionToolkit.WPF.Extensions;

public static class UriExtensions
{
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

        // SECURITY: Only allow HTTP and HTTPS schemes to prevent command injection attacks
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
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
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to open URI: {ex.Message}", ex);
        }
    }
}
