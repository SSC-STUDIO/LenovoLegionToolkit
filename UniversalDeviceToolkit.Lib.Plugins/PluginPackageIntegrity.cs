using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

internal static class PluginPackageIntegrity
{
    public static async Task<string> ComputeSha256HexAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(path);
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return ToHex(hash);
    }

    /// <summary>
    /// Integrity verification can never be waived at runtime.
    /// Previously checked UDT_PLUGIN_INTEGRITY_MODE / LLT_PLUGIN_INTEGRITY_MODE environment variables,
    /// which was a security vulnerability (any user could bypass hash validation).
    /// Now always returns false — verification is always enforced.
    /// </summary>
    public static bool IsVerificationWaived()
    {
        // Log a warning if someone attempts to use the deprecated env-var bypass
        var deprecatedVar = Environment.GetEnvironmentVariable("UDT_PLUGIN_INTEGRITY_MODE")
                         ?? Environment.GetEnvironmentVariable("LLT_PLUGIN_INTEGRITY_MODE");
        if (!string.IsNullOrEmpty(deprecatedVar))
        {
            Log.Instance.Warning(
                "SECURITY: UDT_PLUGIN_INTEGRITY_MODE / LLT_PLUGIN_INTEGRITY_MODE environment variable is set but ignored. " +
                "Integrity verification can no longer be bypassed via environment variables.");
        }
        return false;
    }

    public static bool TryVerifyExpectedHash(
        string? expectedHash,
        string actualHash,
        bool requireWhenMissing,
        out string? failureReason)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            if (requireWhenMissing)
            {
                failureReason = "integrity hash is missing";
                return false;
            }

            failureReason = null;
            return true;
        }

        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            failureReason = $"integrity hash mismatch (expected {expectedHash}, actual {actualHash})";
            return false;
        }

        failureReason = null;
        return true;
    }

    private static string ToHex(byte[] hash) =>
        Convert.ToHexString(hash).ToLowerInvariant();
}
