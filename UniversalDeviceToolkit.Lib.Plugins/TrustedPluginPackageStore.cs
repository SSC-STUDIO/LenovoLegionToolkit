using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

internal static class PluginInstallationCommitCoordinator
{
    private static readonly global::System.Threading.ReaderWriterLockSlim Gate =
        new(global::System.Threading.LockRecursionPolicy.SupportsRecursion);

    internal static IDisposable EnterRead()
    {
        Gate.EnterReadLock();
        return new Lease(static () => Gate.ExitReadLock());
    }

    internal static IDisposable EnterWrite()
    {
        Gate.EnterWriteLock();
        return new Lease(static () => Gate.ExitWriteLock());
    }

    internal static bool IsWriteLockHeld => Gate.IsWriteLockHeld;

    private sealed class Lease(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() =>
            global::System.Threading.Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}

/// <summary>
/// Exact, non-ambient authorization for DLLs from one verified repository package.
/// </summary>
public sealed class PluginPackageAuthorization
{
    private enum AuthorizationState
    {
        Active,
        Publishing,
        Consumed,
        Closed,
        Failed,
    }

    private static readonly ConcurrentDictionary<
        Guid,
        WeakReference<PluginPackageAuthorization>> ActiveAuthorizations = new();
    private readonly IReadOnlyDictionary<string, string> _authorizedFiles;
    private readonly Guid _transactionIdentity;
    private IPluginSignatureValidator? _boundValidator;
    private int _state = (int)AuthorizationState.Active;

    private PluginPackageAuthorization(
        Guid transactionIdentity,
        string pluginId,
        string pluginDirectory,
        IReadOnlyDictionary<string, string> authorizedFiles,
        string serializedTrustRecord)
    {
        _transactionIdentity = transactionIdentity;
        PluginId = pluginId;
        PluginDirectory = pluginDirectory;
        _authorizedFiles = authorizedFiles;
        SerializedTrustRecord = serializedTrustRecord;
    }

    public string PluginId { get; }

    public string PluginDirectory { get; }

    internal string SerializedTrustRecord { get; }

    internal static PluginPackageAuthorization Mint(
        string pluginId,
        string pluginDirectory,
        IReadOnlyDictionary<string, string> authorizedFiles,
        string serializedTrustRecord)
    {
        foreach (var entry in ActiveAuthorizations)
        {
            if (!entry.Value.TryGetTarget(out _))
            {
                ActiveAuthorizations.TryRemove(
                    new KeyValuePair<Guid, WeakReference<PluginPackageAuthorization>>(
                        entry.Key,
                        entry.Value));
            }
        }

        var transactionIdentity = Guid.NewGuid();
        var authorization = new PluginPackageAuthorization(
            transactionIdentity,
            pluginId,
            pluginDirectory,
            authorizedFiles,
            serializedTrustRecord);
        if (!ActiveAuthorizations.TryAdd(
                transactionIdentity,
                new WeakReference<PluginPackageAuthorization>(authorization)))
        {
            throw new InvalidOperationException(
                "Could not register plugin package transaction authorization.");
        }
        return authorization;
    }

    internal bool IsActive =>
        (AuthorizationState)global::System.Threading.Volatile.Read(ref _state) ==
            AuthorizationState.Active &&
        IsExactRegisteredAuthorization();

    internal void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                $"Plugin package authorization for {PluginId} is closed or invalid.");
    }

    internal void Close()
    {
        if (global::System.Threading.Interlocked.CompareExchange(
                ref _state,
                (int)AuthorizationState.Closed,
                (int)AuthorizationState.Active) !=
            (int)AuthorizationState.Active)
        {
            throw new InvalidOperationException(
                $"Plugin package authorization for {PluginId} has already been claimed or closed.");
        }
        RemoveExactRegistration();
    }

    internal bool Authorizes(string filePath)
    {
        if (!IsActive ||
            string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
            return false;

        try
        {
            var normalizedPath = Path.GetFullPath(filePath);
            return _authorizedFiles.TryGetValue(normalizedPath, out var expectedSha256) &&
                   string.Equals(
                       expectedSha256,
                       TrustedPluginPackageStore.ComputeSha256(normalizedPath),
                       StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Scoped plugin package authorization failed for {filePath}: {ex.Message}", ex);
            return false;
        }
    }

    private bool AuthorizesAllFilesForPublication() =>
        _authorizedFiles.Count > 0 &&
        _authorizedFiles.All(file =>
            File.Exists(file.Key) &&
            string.Equals(
                file.Value,
                TrustedPluginPackageStore.ComputeSha256(file.Key),
                StringComparison.OrdinalIgnoreCase));

    internal void ClaimForPublication()
    {
        if (!IsExactRegisteredAuthorization() ||
            global::System.Threading.Interlocked.CompareExchange(
                ref _state,
                (int)AuthorizationState.Publishing,
                (int)AuthorizationState.Active) !=
            (int)AuthorizationState.Active)
        {
            throw new InvalidOperationException(
                $"Plugin package authorization for {PluginId} has already been claimed or closed.");
        }
        RemoveExactRegistration();
    }

    internal void CompletePublication()
    {
        if (global::System.Threading.Interlocked.CompareExchange(
                ref _state,
                (int)AuthorizationState.Consumed,
                (int)AuthorizationState.Publishing) !=
            (int)AuthorizationState.Publishing)
        {
            throw new InvalidOperationException(
                $"Plugin package authorization for {PluginId} is not being published.");
        }
    }

    internal void FailPublication()
    {
        global::System.Threading.Interlocked.CompareExchange(
            ref _state,
            (int)AuthorizationState.Failed,
            (int)AuthorizationState.Publishing);
        RemoveExactRegistration();
    }

    internal void ValidateClaimedPublication()
    {
        if ((AuthorizationState)global::System.Threading.Volatile.Read(ref _state) !=
                AuthorizationState.Publishing ||
            !AuthorizesAllFilesForPublication())
        {
            throw new InvalidDataException(
                $"Plugin package {PluginId} changed after transaction authorization.");
        }
    }

    private bool IsExactRegisteredAuthorization() =>
        ActiveAuthorizations.TryGetValue(_transactionIdentity, out var registered) &&
        registered.TryGetTarget(out var target) &&
        ReferenceEquals(target, this);

    private void RemoveExactRegistration()
    {
        if (ActiveAuthorizations.TryGetValue(_transactionIdentity, out var registered) &&
            registered.TryGetTarget(out var target) &&
            ReferenceEquals(target, this))
        {
            ActiveAuthorizations.TryRemove(
                new KeyValuePair<Guid, WeakReference<PluginPackageAuthorization>>(
                    _transactionIdentity,
                    registered));
        }
    }

    internal IPluginSignatureValidator Scope(IPluginSignatureValidator signatureValidator)
    {
        ArgumentNullException.ThrowIfNull(signatureValidator);
        EnsureActive();
        var boundValidator = global::System.Threading.Interlocked.CompareExchange(
            ref _boundValidator,
            signatureValidator,
            null);
        if (boundValidator is not null && !ReferenceEquals(boundValidator, signatureValidator))
        {
            throw new InvalidOperationException(
                $"Plugin package authorization for {PluginId} is bound to a different signature policy.");
        }
        return new AuthorizedPluginSignatureValidator(signatureValidator, this);
    }

    private sealed class AuthorizedPluginSignatureValidator(
        IPluginSignatureValidator signatureValidator,
        PluginPackageAuthorization authorization) : IPluginSignatureValidator
    {
        public async global::System.Threading.Tasks.Task<PluginSignatureResult> ValidateAsync(string dllPath)
        {
            var signatureResult = await signatureValidator.ValidateAsync(dllPath).ConfigureAwait(false);
            if (signatureResult.IsValid || !authorization.Authorizes(dllPath))
                return signatureResult;

            return new PluginSignatureResult(
                PluginSignatureStatus.Valid,
                "Authorized by the exact verified repository package transaction.");
        }
    }
}

internal static class TrustedPluginPackageStore
{
    private static readonly object Lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    internal static Action? PersistenceBoundaryOverride { get; set; }

    private static string StorePath => Path.Combine(Folders.AppData, "trusted-plugin-packages.json");

    public static void TrustPluginDirectory(string pluginId, string pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(pluginDirectory) || !Directory.Exists(pluginDirectory))
            return;

        try
        {
            PublishAuthorizationStrict(CreateAuthorization(pluginId, pluginDirectory));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to trust plugin package {pluginId}: {ex.Message}", ex);
        }
    }

    internal static PluginPackageAuthorization CreateAuthorization(
        string pluginId,
        string pluginDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);
        if (!Directory.Exists(pluginDirectory))
            throw new DirectoryNotFoundException($"Plugin package directory does not exist: {pluginDirectory}");

        var normalizedDirectory = Path.GetFullPath(pluginDirectory);
        var files = Directory.GetFiles(normalizedDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).Contains(
                ".resources.dll",
                StringComparison.OrdinalIgnoreCase))
            .Select(path => new TrustedPluginFile
            {
                Path = Path.GetFullPath(path),
                Sha256 = ComputeSha256(path),
            })
            .ToList();
        if (files.Count == 0)
            throw new InvalidDataException($"Plugin package {pluginId} contains no trustable DLLs.");

        var package = new TrustedPluginPackage
        {
            PluginId = pluginId,
            PluginDirectory = normalizedDirectory,
            TrustedAtUtc = DateTimeOffset.UtcNow,
            Files = files,
        };
        var authorizedFiles = files.ToDictionary(
            file => file.Path,
            file => file.Sha256,
            PathComparer);
        return PluginPackageAuthorization.Mint(
            pluginId,
            normalizedDirectory,
            authorizedFiles,
            JsonSerializer.Serialize(package, JsonOptions));
    }

    internal static void PublishAuthorizationStrict(PluginPackageAuthorization authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        using var commitWrite = PluginInstallationCommitCoordinator.EnterWrite();
        PublishAuthorizationStrictUnderCommitLease(authorization);
    }

    internal static void PublishAuthorizationStrictUnderCommitLease(
        PluginPackageAuthorization authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        if (!PluginInstallationCommitCoordinator.IsWriteLockHeld)
            throw new InvalidOperationException("Plugin installation commit write lease is required.");
        authorization.ClaimForPublication();
        try
        {
            authorization.ValidateClaimedPublication();
            var package = JsonSerializer.Deserialize<TrustedPluginPackage>(
                authorization.SerializedTrustRecord,
                JsonOptions)
                ?? throw new InvalidDataException("Plugin package authorization record is invalid.");
            if (!package.PluginId.Equals(
                    authorization.PluginId,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Plugin package authorization ID does not match.");
            }

            lock (Lock)
            {
                var store = ReadStoreStrict();
                store.Plugins[authorization.PluginId] = package;
                WriteStore(store);
            }
            authorization.CompletePublication();
        }
        catch
        {
            authorization.FailPublication();
            throw;
        }
    }

    public static bool IsTrustedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            using var commitRead = PluginInstallationCommitCoordinator.EnterRead();
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
                        if (storedPath is not null && PathComparer.Equals(storedPath, normalizedPath))
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

    internal static string? CaptureExactTrustRecord(string pluginId)
    {
        using var commitRead = PluginInstallationCommitCoordinator.EnterRead();
        lock (Lock)
        {
            var store = ReadStoreStrict();
            return store.Plugins.TryGetValue(pluginId, out var package)
                ? JsonSerializer.Serialize(package, JsonOptions)
                : null;
        }
    }

    internal static void RestoreExactTrustRecord(string pluginId, string? serializedRecord)
    {
        using var commitWrite = PluginInstallationCommitCoordinator.EnterWrite();
        RestoreExactTrustRecordUnderCommitLease(pluginId, serializedRecord);
    }

    internal static void RestoreExactTrustRecordUnderCommitLease(
        string pluginId,
        string? serializedRecord)
    {
        if (!PluginInstallationCommitCoordinator.IsWriteLockHeld)
            throw new InvalidOperationException("Plugin installation commit write lease is required.");
        lock (Lock)
        {
            var store = ReadStoreStrict();
            if (serializedRecord is null)
            {
                store.Plugins.Remove(pluginId);
            }
            else
            {
                var package = JsonSerializer.Deserialize<TrustedPluginPackage>(
                    serializedRecord,
                    JsonOptions)
                    ?? throw new InvalidDataException("Captured plugin trust record is invalid.");
                if (!package.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Captured plugin trust record ID does not match.");
                store.Plugins[pluginId] = package;
            }
            WriteStore(store);
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
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "trusted-plugin-path-normalize",
                $"Failed to normalize trusted plugin path: {path}",
                ex);
            return path;
        }
    }

    internal static void RemoveStrict(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        using var commitWrite = PluginInstallationCommitCoordinator.EnterWrite();
        RemoveStrictUnderCommitLease(pluginId);
    }

    internal static void RemoveStrictUnderCommitLease(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;
        if (!PluginInstallationCommitCoordinator.IsWriteLockHeld)
            throw new InvalidOperationException("Plugin installation commit write lease is required.");
        lock (Lock)
        {
            var store = ReadStoreStrict();
            if (!store.Plugins.Remove(pluginId))
                return;

            WriteStore(store);
        }
    }

    internal static void RemoveBestEffort(string pluginId)
    {
        try
        {
            RemoveStrict(pluginId);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to remove trusted plugin package {pluginId}: {ex.Message}", ex);
        }
    }

    private static TrustedPluginPackageStoreModel ReadStoreStrict()
    {
        if (!File.Exists(StorePath))
            return new TrustedPluginPackageStoreModel();

        var encryptedBytes = File.ReadAllBytes(StorePath);
        if (encryptedBytes.Length == 0)
            return new TrustedPluginPackageStoreModel();

        var decryptedBytes = UnprotectStoreBytes(encryptedBytes);
        var envelope = JsonSerializer.Deserialize<StoreEnvelope>(decryptedBytes)
            ?? throw new InvalidDataException("Trusted plugin package store envelope is invalid.");
        if (string.IsNullOrWhiteSpace(envelope.Data) ||
            string.IsNullOrWhiteSpace(envelope.Hmac) ||
            string.IsNullOrWhiteSpace(envelope.Salt))
        {
            throw new InvalidDataException("Trusted plugin package store envelope is incomplete.");
        }

        byte[] salt;
        try
        {
            salt = Convert.FromBase64String(envelope.Salt);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Trusted plugin package store salt is invalid.", ex);
        }

        if (salt.Length != 16)
            throw new InvalidDataException("Trusted plugin package store salt has an invalid length.");

        var computedHmac = ComputeHmac(envelope.Data, salt);
        if (!string.Equals(envelope.Hmac, computedHmac, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Trusted plugin package store integrity validation failed.");

        byte[] jsonBytes;
        try
        {
            jsonBytes = Convert.FromBase64String(envelope.Data);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Trusted plugin package store payload is invalid.", ex);
        }

        return NormalizeStore(
            JsonSerializer.Deserialize<TrustedPluginPackageStoreModel>(jsonBytes)
            ?? throw new InvalidDataException("Trusted plugin package store payload is empty."));
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
                decryptedBytes = UnprotectStoreBytes(encryptedBytes);
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
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "trusted-plugin-store-read",
                "Failed to read trusted plugin package store; using empty store.",
                ex);
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
        PersistenceBoundaryOverride?.Invoke();

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
        var encrypted = ProtectStoreBytes(envelopeJson);
        File.WriteAllBytes(StorePath, encrypted);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                StorePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
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
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
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
        else
        {
            secret = Encoding.UTF8.GetBytes(
                $"{Environment.UserName}\n{Environment.MachineName}");
        }
        return Rfc2898DeriveBytes.Pbkdf2(secret, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    private static byte[] ProtectStoreBytes(byte[] envelopeBytes)
    {
        if (!OperatingSystem.IsWindows())
            return envelopeBytes;

        return ProtectedData.Protect(
            envelopeBytes,
            null,
            DataProtectionScope.CurrentUser);
    }

    private static byte[] UnprotectStoreBytes(byte[] storedBytes)
    {
        if (!OperatingSystem.IsWindows())
            return storedBytes;

        return ProtectedData.Unprotect(
            storedBytes,
            null,
            DataProtectionScope.CurrentUser);
    }

    private static string ComputeHmac(string data, byte[] salt)
    {
        var key = DeriveHmacKey(salt);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static string ComputeSha256(string filePath)
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
