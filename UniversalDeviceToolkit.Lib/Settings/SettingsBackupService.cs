using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Settings;

public sealed class SettingsBackupService
{
    public const int CurrentFormatVersion = 1;
    private const string ManifestName = "udt-settings-backup.json";

    public string Export(string destinationFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFile);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationFile))!);
        if (File.Exists(destinationFile)) File.Delete(destinationFile);

        using var archive = ZipFile.Open(destinationFile, ZipArchiveMode.Create);
        var files = Directory.Exists(Folders.AppData)
            ? Directory.EnumerateFiles(Folders.AppData, "*.json", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName)
            : Enumerable.Empty<string>();
        foreach (var file in files)
            archive.CreateEntryFromFile(file, $"settings/{Path.GetFileName(file)}", CompressionLevel.Optimal);

        var entry = archive.CreateEntry(ManifestName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(JsonSerializer.Serialize(new Manifest(CurrentFormatVersion, DateTimeOffset.UtcNow)));
        return destinationFile;
    }

    public string Import(string sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);
        using var archive = ZipFile.OpenRead(sourceFile);
        var manifestEntry = archive.GetEntry(ManifestName) ?? throw new InvalidDataException("Missing UDT settings backup manifest.");
        Manifest manifest;
        using (var reader = new StreamReader(manifestEntry.Open()))
            manifest = JsonSerializer.Deserialize<Manifest>(reader.ReadToEnd()) ?? throw new InvalidDataException("Invalid UDT settings backup manifest.");
        if (manifest.FormatVersion > CurrentFormatVersion)
            throw new NotSupportedException($"Backup format {manifest.FormatVersion} is newer than supported format {CurrentFormatVersion}.");

        var settingsEntries = archive.Entries
            .Where(entry => entry.FullName.StartsWith("settings/", StringComparison.Ordinal)
                            && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (var entry in settingsEntries)
        {
            if (!PathSecurity.IsValidFileName(entry.Name))
                throw new InvalidDataException($"Unsafe settings entry: {entry.FullName}");
        }

        var backupsDir = Path.Combine(Folders.AppData, "Backups");
        Directory.CreateDirectory(backupsDir);
        var rollback = Path.Combine(backupsDir, $"pre-import-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
        Export(rollback);

        var staging = Path.Combine(backupsDir, $"import-staging-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(staging);
            foreach (var entry in settingsEntries)
                entry.ExtractToFile(Path.Combine(staging, entry.Name), overwrite: true);

            Directory.CreateDirectory(Folders.AppData);

            // Replace semantics (not merge): apply backup files, then remove AppData *.json
            // that are not present in the backup so stale settings cannot survive import.
            var importedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.EnumerateFiles(staging, "*.json", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(Folders.AppData, name), overwrite: true);
                importedNames.Add(name);
            }

            foreach (var existing in Directory.EnumerateFiles(Folders.AppData, "*.json", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(existing);
                if (!importedNames.Contains(name))
                    File.Delete(existing);
            }

            return rollback;
        }
        catch
        {
            Restore(rollback);
            throw;
        }
        finally
        {
            try
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
            }
            catch (Exception ex)
            {
                Log.Instance.WarningOnce(
                    "settings-backup-staging-cleanup",
                    $"Best-effort cleanup of settings backup staging failed: {staging}",
                    ex);
            }
        }
    }

    private static void Restore(string backupFile)
    {
        using var archive = ZipFile.OpenRead(backupFile);
        Directory.CreateDirectory(Folders.AppData);

        var restoredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries.Where(entry =>
                     entry.FullName.StartsWith("settings/", StringComparison.Ordinal)
                     && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            if (!PathSecurity.IsValidFileName(entry.Name))
                continue;
            entry.ExtractToFile(Path.Combine(Folders.AppData, entry.Name), overwrite: true);
            restoredNames.Add(entry.Name);
        }

        // Mirror import replace semantics on rollback.
        foreach (var existing in Directory.EnumerateFiles(Folders.AppData, "*.json", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(existing);
            if (!restoredNames.Contains(name))
            {
                try { File.Delete(existing); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Log.Instance.Warning($"Failed to remove leftover settings file during restore: {existing}", ex);
                }
            }
        }
    }

    private sealed record Manifest(int FormatVersion, DateTimeOffset CreatedAtUtc);
}
