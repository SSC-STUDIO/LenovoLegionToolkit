using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

internal static class TrustedPluginPackageStore
{
    private static readonly object Lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string StorePath => Path.Combine(Folders.AppData, "trusted-plugin-packages.json");
    private static string KeyPath => Path.Combine(Folders.AppData, "trusted-plugin-packages.key");

    public static void TrustPluginDirectory(string pluginId, string pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(pluginDirectory) || !Directory.Exists(pluginDirectory))
            return;

        try
        {
            var normalizedDirectory = Path.GetFullPath(pluginDirectory);
            var dlls = Directory.GetFiles(normalizedDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(path => !Path.GetFileName(path).Contains(".resources.dll", StringComparison.OrdinalIgnoreCase))
                .Select(path => new TrustedPluginFile
                {
                    Path = Path.GetFullPath(path),
                    Sha256 = ComputeSha256(path),
                })
                .Where(file => !string.IsNullOrWhiteSpace(file.Sha256))
                .ToList();

            if (dlls.Count == 0)
                return;

            lock (Lock)
            {
                var store = ReadStore();
                store.Plugins[pluginId] = new TrustedPluginPackage
                {
                    PluginId = pluginId,
                    PluginDirectory = normalizedDirectory,
                    TrustedAtUtc = DateTimeOffset.UtcNow,
                    Files = dlls,
                };
                WriteStore(store);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to trust plugin package {pluginId}: {ex.Message}", ex);
        }
    }

    public static bool IsTrustedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            var normalizedPath = Path.GetFullPath(filePath);
            var sha256 = ComputeSha256(normalizedPath);
            if (string.IsNullOrWhiteSpace(sha256))
                return false;

            lock (Lock)
            {
                var store = ReadStore();
                foreach (var plugin in store.Plugins.Values)
                {
                    foreach (var file in plugin.Files)
                    {
                        if (!string.Equals(file.Sha256, sha256, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var storedPath = TryNormalizePath(file.Path);
                        if (string.Equals(storedPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                            return true;

                        if (!string.Equals(Path.GetFileName(file.Path), Path.GetFileName(normalizedPath), StringComparison.OrdinalIgnoreCase))
                            continue;

                        file.Path = normalizedPath;
                        var directory = Path.GetDirectoryName(normalizedPath);
                        if (!string.IsNullOrWhiteSpace(directory))
                            plugin.PluginDirectory = directory;

                        WriteStore(store);
                        return true;
                    }
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to verify trusted plugin file {filePath}: {ex.Message}", ex);
            return false;
        }
    }

    private static string? TryNormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    public static void Remove(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        try
        {
            lock (Lock)
            {
                var store = ReadStore();
                if (!store.Plugins.Remove(pluginId))
                    return;

                WriteStore(store);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to remove trusted plugin package {pluginId}: {ex.Message}", ex);
        }
    }

    private static TrustedPluginPackageStoreModel ReadStore()
    {
        try
        {
            if (!File.Exists(StorePath))
                return new TrustedPluginPackageStoreModel();

            var json = File.ReadAllText(StorePath);
            var store = NormalizeStore(JsonSerializer.Deserialize<TrustedPluginPackageStoreModel>(json));
            if (!IsStoreSignatureValid(store))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ignoring trusted plugin package store with missing or invalid integrity signature: {StorePath}");

                return new TrustedPluginPackageStoreModel();
            }

            return store;
        }
        catch
        {
            return new TrustedPluginPackageStoreModel();
        }
    }

    private static TrustedPluginPackageStoreModel NormalizeStore(TrustedPluginPackageStoreModel? store)
    {
        if (store is null)
            return new TrustedPluginPackageStoreModel();

        store.Plugins = new Dictionary<string, TrustedPluginPackage>(
            store.Plugins ?? new Dictionary<string, TrustedPluginPackage>(),
            StringComparer.OrdinalIgnoreCase);

        return store;
    }

    private static void WriteStore(TrustedPluginPackageStoreModel store)
    {
        var directory = Path.GetDirectoryName(StorePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        store.Signature = string.Empty;
        store.Signature = ComputeStoreSignature(store);

        File.WriteAllText(StorePath, JsonSerializer.Serialize(store, JsonOptions));
    }

    private static bool IsStoreSignatureValid(TrustedPluginPackageStoreModel store)
    {
        if (string.IsNullOrWhiteSpace(store.Signature))
            return false;

        var expectedSignature = ComputeStoreSignature(store);
        return FixedTimeHexEquals(store.Signature, expectedSignature);
    }

    private static bool FixedTimeHexEquals(string left, string right)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(left),
                Convert.FromHexString(right));
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeStoreSignature(TrustedPluginPackageStoreModel store)
    {
        var signature = store.Signature;
        store.Signature = string.Empty;
        try
        {
            var payload = JsonSerializer.Serialize(store, JsonOptions);
            using var hmac = new HMACSHA256(GetOrCreateStoreKey());
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        }
        finally
        {
            store.Signature = signature;
        }
    }

    private static byte[] GetOrCreateStoreKey()
    {
        if (File.Exists(KeyPath))
        {
            var protectedKey = File.ReadAllBytes(KeyPath);
            return ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
        }

        var key = RandomNumberGenerator.GetBytes(32);
        var directory = Path.GetDirectoryName(KeyPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllBytes(KeyPath, ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser));
        return key;
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
    }

    private sealed class TrustedPluginPackageStoreModel
    {
        public string Signature { get; set; } = string.Empty;
        public Dictionary<string, TrustedPluginPackage> Plugins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class TrustedPluginPackage
    {
        public string PluginId { get; set; } = string.Empty;
        public string PluginDirectory { get; set; } = string.Empty;
        public DateTimeOffset TrustedAtUtc { get; set; }
        public List<TrustedPluginFile> Files { get; set; } = [];
    }

    private sealed class TrustedPluginFile
    {
        public string Path { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
    }
}
