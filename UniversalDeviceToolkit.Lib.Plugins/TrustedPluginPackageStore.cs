using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

internal static class TrustedPluginPackageStore
{
    private const string HmacFieldName = "_hmac";

    private static readonly object Lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string StorePath => Path.Combine(Folders.AppData, "trusted-plugin-packages.json");

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

            var encryptedBytes = File.ReadAllBytes(StorePath);
            if (encryptedBytes.Length == 0)
                return new TrustedPluginPackageStoreModel();

            byte[] decryptedBytes;
            try
            {
                decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                ResetStore();
                return new TrustedPluginPackageStoreModel();
            }

            var envelope = JsonSerializer.Deserialize<StoreEnvelope>(decryptedBytes);
            if (envelope is null || string.IsNullOrWhiteSpace(envelope.Data) || string.IsNullOrWhiteSpace(envelope.Hmac) || string.IsNullOrWhiteSpace(envelope.Salt))
            {
                ResetStore();
                return new TrustedPluginPackageStoreModel();
            }

            byte[] salt;
            try
            {
                salt = Convert.FromBase64String(envelope.Salt);
            }
            catch
            {
                ResetStore();
                return new TrustedPluginPackageStoreModel();
            }

            if (salt.Length != 16)
            {
                ResetStore();
                return new TrustedPluginPackageStoreModel();
            }

            var computedHmac = ComputeHmac(envelope.Data, salt);
            if (!string.Equals(envelope.Hmac, computedHmac, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("HMAC mismatch in trusted plugin package store; resetting to empty store.");

                ResetStore();
                return new TrustedPluginPackageStoreModel();
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Data));
            return NormalizeStore(JsonSerializer.Deserialize<TrustedPluginPackageStoreModel>(json));
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

        var json = JsonSerializer.Serialize(store, JsonOptions);
        var dataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var salt = RandomNumberGenerator.GetBytes(16);
        var hmac = ComputeHmac(dataBase64, salt);

        var envelope = new StoreEnvelope
        {
            Data = dataBase64,
            Hmac = hmac,
            Salt = Convert.ToBase64String(salt),
        };

        var envelopeJson = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        var encrypted = ProtectedData.Protect(envelopeJson, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(StorePath, encrypted);
    }

    private static void ResetStore()
    {
        try
        {
            if (File.Exists(StorePath))
                File.Delete(StorePath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to delete trusted plugin store file", ex);
        }
    }

    private static byte[] DeriveHmacKey(byte[] salt)
    {
        byte[] secret;
        using (var identity = WindowsIdentity.GetCurrent())
        {
            if (identity.User is not null)
            {
                var sidBinary = new byte[identity.User.BinaryLength];
                identity.User.GetBinaryForm(sidBinary, 0);
                secret = sidBinary;
            }
            else
            {
                secret = Encoding.UTF8.GetBytes(Environment.MachineName);
            }
        }
        return Rfc2898DeriveBytes.Pbkdf2(secret, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    private static string ComputeHmac(string data, byte[] salt)
    {
        var key = DeriveHmacKey(salt);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
    }

    private sealed class StoreEnvelope
    {
        public string Data { get; set; } = string.Empty;
        public string Hmac { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
    }

    private sealed class TrustedPluginPackageStoreModel
    {
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
