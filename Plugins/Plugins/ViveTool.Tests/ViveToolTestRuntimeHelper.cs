using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.ViveTool.Services;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

internal static class ViveToolTestRuntimeHelper
{
    private const string TempDirectoryPrefix = "llt-vivetool-test-";

    private static readonly string[] RequiredRuntimeFileNames =
    [
        ViveToolPathService.ViveToolExeName,
        "Albacore.ViVe.dll",
        "Newtonsoft.Json.dll",
        "FeatureDictionary.pfs"
    ];

    public static async Task<ViveToolTestRuntimeScope> CreateCompleteRuntimeScopeAsync(
        string exeFileName = ViveToolPathService.ViveToolExeName)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), TempDirectoryPrefix + Guid.NewGuid().ToString("N"));
        var exePath = await CreateCompleteRuntimeAsync(directoryPath, exeFileName).ConfigureAwait(false);
        return new ViveToolTestRuntimeScope(directoryPath, exePath);
    }

    public static async Task<ViveToolTestRuntimeScope> CreateCommandBackedRuntimeScopeAsync(
        IEnumerable<string>? featureDictionaryLines = null)
    {
        var runtimeScope = await CreateCompleteRuntimeScopeAsync().ConfigureAwait(false);

        try
        {
            var commandProcessorPath = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrWhiteSpace(commandProcessorPath) || !File.Exists(commandProcessorPath))
            {
                throw new InvalidOperationException("ComSpec command processor is unavailable.");
            }

            File.Copy(commandProcessorPath, runtimeScope.ExePath, overwrite: true);

            if (featureDictionaryLines is not null)
            {
                await File.WriteAllLinesAsync(
                    Path.Combine(runtimeScope.DirectoryPath, "FeatureDictionary.pfs"),
                    featureDictionaryLines).ConfigureAwait(false);
            }

            return runtimeScope;
        }
        catch
        {
            await runtimeScope.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public static async Task<string> CreateCompleteRuntimeAsync(
        string directoryPath,
        string exeFileName = ViveToolPathService.ViveToolExeName)
    {
        Directory.CreateDirectory(directoryPath);

        foreach (var requiredRuntimeFileName in RequiredRuntimeFileNames)
        {
            var fileName = requiredRuntimeFileName == ViveToolPathService.ViveToolExeName
                ? exeFileName
                : requiredRuntimeFileName;
            await File.WriteAllTextAsync(
                Path.Combine(directoryPath, fileName),
                requiredRuntimeFileName).ConfigureAwait(false);
        }

        return Path.Combine(directoryPath, exeFileName);
    }

    public static void DeleteDirectoryBestEffort(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
        catch
        {
            // cleanup best effort
        }
    }
}

internal sealed class ViveToolTestRuntimeScope(string directoryPath, string exePath) : IAsyncDisposable
{
    public string DirectoryPath { get; } = directoryPath;

    public string ExePath { get; } = exePath;

    public ValueTask DisposeAsync()
    {
        ViveToolTestRuntimeHelper.DeleteDirectoryBestEffort(DirectoryPath);
        return ValueTask.CompletedTask;
    }
}

internal static class ViveToolTestFileHelper
{
    public static ViveToolTestFileScope CreateScope(string extension, string prefix = "test_features_")
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{prefix}{Guid.NewGuid():N}{extension}");
        return new ViveToolTestFileScope(filePath);
    }

    public static void DeleteFileBestEffort(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // cleanup best effort
        }
    }
}

internal sealed class ViveToolTestFileScope(string filePath) : IDisposable
{
    public string FilePath { get; } = filePath;

    public void Dispose()
    {
        ViveToolTestFileHelper.DeleteFileBestEffort(FilePath);
    }
}
