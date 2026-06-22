using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class PluginExecutableResolver
{
    internal static bool TryResolve(
        string pluginId,
        string? metadataFilePath,
        string pluginsDirectory,
        out string? exeFile,
        out string? workingDirectory,
        bool allowUnsignedOverride = false)
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
        candidateDirectories.Add(Path.Combine(pluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}"));
        candidateDirectories.Add(Path.Combine(pluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}"));

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

                if (!allowUnsignedOverride && !IsAuthenticodeSigned(preferredCandidate))
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

        try
        {
#pragma warning disable SYSLIB0057 // CreateFromSignedFile is obsolete; no direct replacement for Authenticode check
            using var certificate = X509Certificate.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057
            return certificate != null;
        }
        catch (SecurityException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> GetPreferredExecutableCandidates(string candidateDirectory, string pluginId)
    {
        yield return Path.Combine(candidateDirectory, $"{pluginId}.exe");
        yield return Path.Combine(candidateDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}.exe");
        yield return Path.Combine(candidateDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}.exe");
    }

    private static void LogWarning(string message)
    {
        if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace(message);
    }
}
