using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

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

    public static bool IsVerificationWaived() =>
        string.Equals(Environment.GetEnvironmentVariable("UDT_PLUGIN_INTEGRITY_MODE"), "skip", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("LLT_PLUGIN_INTEGRITY_MODE"), "skip", StringComparison.OrdinalIgnoreCase);

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