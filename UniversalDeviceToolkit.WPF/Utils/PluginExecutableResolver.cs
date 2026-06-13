using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class PluginExecutableResolver
{
    internal static bool TryResolve(
        string pluginId,
        string? metadataFilePath,
        string pluginsDirectory,
        IPluginSignatureValidator signatureValidator,
        out string? exeFile,
        out string? workingDirectory)
    {
        exeFile = null;
        workingDirectory = null;

        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginsDirectory);
        ArgumentNullException.ThrowIfNull(signatureValidator);

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

                if (!IsTrustedExecutable(preferredCandidate, signatureValidator))
                    continue;

                exeFile = preferredCandidate;
                workingDirectory = candidateDirectory;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetPreferredExecutableCandidates(string candidateDirectory, string pluginId)
    {
        yield return Path.Combine(candidateDirectory, $"{pluginId}.exe");
        yield return Path.Combine(candidateDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}.exe");
        yield return Path.Combine(candidateDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}.exe");
    }

    private static bool IsTrustedExecutable(string executablePath, IPluginSignatureValidator signatureValidator)
    {
        var signatureResult = signatureValidator.ValidateAsync(executablePath).GetAwaiter().GetResult();
        if (signatureResult.IsValid)
            return true;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Rejected plugin executable due to invalid signature. [path={executablePath}, status={signatureResult.Status}, error={signatureResult.ErrorMessage}]");

        return false;
    }
}
