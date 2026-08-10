using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

internal static class PluginExecutableResolver
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> AuthenticodeCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Fast path for list/UI: existence only. Authenticode is expensive and must not
    /// run on the UI thread while rebuilding the plugin list.
    /// </summary>
    internal static bool TryResolveForUiListing(
        string pluginId,
        string? metadataFilePath,
        string pluginsDirectory,
        out string? exeFile,
        out string? workingDirectory) =>
        TryResolve(
            pluginId,
            metadataFilePath,
            pluginsDirectory,
            out exeFile,
            out workingDirectory,
            allowUnsignedOverride: true,
            verifyAuthenticode: false);

    /// <summary>
    /// Launch path: require Authenticode unless <paramref name="allowUnsignedOverride"/> (DEBUG).
    /// </summary>
    internal static bool TryResolve(
        string pluginId,
        string? metadataFilePath,
        string pluginsDirectory,
        out string? exeFile,
        out string? workingDirectory,
        bool allowUnsignedOverride = false,
        bool verifyAuthenticode = true)
    {
        exeFile = null;
        workingDirectory = null;

        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginsDirectory);

        var candidateDirectories = new List<string>();

        if (!string.IsNullOrWhiteSpace(metadataFilePath))
        {
            var metadataDirectory = Path.GetDirectoryName(metadataFilePath);
            if (!string.IsNullOrWhiteSpace(metadataDirectory))
                candidateDirectories.Add(metadataDirectory);
        }

        candidateDirectories.Add(Path.Combine(pluginsDirectory, pluginId));
        candidateDirectories.Add(Path.Combine(pluginsDirectory, "local", pluginId));
        foreach (var prefixed in new[]
                 {
                     $"UniversalDeviceToolkit.Plugins.{pluginId}",
                     $"LenovoLegionToolkit.Plugins.{pluginId}",
                     $"UniversalDeviceToolkit.Plugins.{pluginId.Replace("-", string.Empty)}",
                     $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}"
                 })
        {
            candidateDirectories.Add(Path.Combine(pluginsDirectory, prefixed));
        }

        foreach (var candidateDirectory in candidateDirectories
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(candidateDirectory))
                continue;

            foreach (var preferredCandidate in GetPreferredExecutableCandidates(candidateDirectory, pluginId))
            {
                if (!File.Exists(preferredCandidate))
                    continue;

                if (verifyAuthenticode && !allowUnsignedOverride && !IsAuthenticodeSigned(preferredCandidate))
                {
                    LogWarning($"[PluginExecutableResolver] Skipping unsigned executable: '{preferredCandidate}'");
                    continue;
                }

                exeFile = preferredCandidate;
                workingDirectory = candidateDirectory;
                return true;
            }
        }

        return false;
    }

    internal static bool IsAuthenticodeSigned(string filePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        if (AuthenticodeCache.TryGetValue(filePath, out var cached))
            return cached;

        try
        {
#pragma warning disable SYSLIB0057 // CreateFromSignedFile is obsolete; no direct replacement for Authenticode check
            using var certificate = X509Certificate.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057
            var signed = certificate != null;
            AuthenticodeCache[filePath] = signed;
            return signed;
        }
        catch (SecurityException ex)
        {
            Log.Instance.TraceOnce(
                "plugin-exe-cert-security",
                "Plugin executable Authenticode check denied by security policy.",
                ex);
            AuthenticodeCache[filePath] = false;
            return false;
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-exe-cert",
                "Plugin executable Authenticode check failed.",
                ex);
            AuthenticodeCache[filePath] = false;
            return false;
        }
    }

    private static IEnumerable<string> GetPreferredExecutableCandidates(string candidateDirectory, string pluginId)
    {
        yield return Path.Combine(candidateDirectory, $"{pluginId}.exe");
        yield return Path.Combine(candidateDirectory, $"UniversalDeviceToolkit.Plugins.{pluginId}.exe");
        yield return Path.Combine(candidateDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}.exe");
        yield return Path.Combine(candidateDirectory, $"UniversalDeviceToolkit.Plugins.{pluginId.Replace("-", string.Empty)}.exe");
        yield return Path.Combine(candidateDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}.exe");
    }

    private static void LogWarning(string message)
    {
        if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(message);
    }
}
