using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin installation service - Supports pre-compiled DLL plugins only.
/// Provides safe extraction, validation, and organization of plugin packages.
/// </summary>
public class PluginInstallationService
{
    private sealed class PluginImportRecoveryException(
        string message,
        string? recoveryId,
        string? recoveryPath,
        bool unloadPending,
        params Exception[] innerExceptions) : AggregateException(message, innerExceptions)
    {
        public string? RecoveryId { get; } = recoveryId;
        public string? RecoveryPath { get; } = recoveryPath;
        public bool UnloadPending { get; } = unloadPending;
    }

    internal const int MaxArchiveEntryCount = 2048;
    internal const long MaxArchiveCompressedBytes = 128L * 1024 * 1024;
    internal const long MaxCentralDirectoryBytes = 16L * 1024 * 1024;
    internal const long MaxSingleEntryUncompressedBytes = 128L * 1024 * 1024;
    internal const long MaxTotalUncompressedBytes = 256L * 1024 * 1024;
    internal const double MaxCompressionRatio = 100d;
    internal const long MinimumCompressionRatioCheckBytes = 1024L * 1024;

    private const int ExtractionBufferSize = 81920;
    private const uint EndOfCentralDirectorySignature = 0x06054b50;
    private const uint Zip64EndOfCentralDirectorySignature = 0x06064b50;
    private const uint Zip64EndOfCentralDirectoryLocatorSignature = 0x07064b50;
    private const uint CentralDirectoryFileHeaderSignature = 0x02014b50;

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static readonly char[] InvalidArchivePathCharacters =
    [
        '<', '>', ':', '"', '|', '?', '*', '\0'
    ];

    private static readonly HashSet<string> ReservedWindowsPathNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "CONIN$", "CONOUT$",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "COM\u00B9", "COM\u00B2", "COM\u00B3",
        "LPT\u00B9", "LPT\u00B2", "LPT\u00B3"
    };

    private readonly IPluginManager _pluginManager;
    private readonly Action<string, string> _moveDirectory;
    private readonly Action<string> _mutationBoundary;
    private readonly Func<string, string, bool> _atomicMoveSupported;

    public PluginInstallationService(IPluginManager pluginManager)
        : this(
            pluginManager,
            Directory.Move,
            static _ => { },
            ProbeAtomicDirectoryMove)
    {
    }

    internal PluginInstallationService(
        IPluginManager pluginManager,
        Action<string, string> moveDirectory,
        Action<string>? mutationBoundary = null,
        Func<string, string, bool>? atomicMoveSupported = null)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _moveDirectory = moveDirectory ?? throw new ArgumentNullException(nameof(moveDirectory));
        _mutationBoundary = mutationBoundary ?? (static _ => { });
        _atomicMoveSupported = atomicMoveSupported ?? ProbeAtomicDirectoryMove;
    }

    /// <summary>
    /// Installs a pre-compiled DLL plugin from a ZIP file.
    /// </summary>
    /// <param name="zipFilePath">Path to the plugin ZIP file.</param>
    /// <param name="pluginsDir">Target directory for plugins.</param>
    /// <returns>True if installation was successful.</returns>
    public Task<bool> ExtractAndInstallPluginAsync(string zipFilePath, string pluginsDir) =>
        ExtractAndInstallPluginAsync(zipFilePath, pluginsDir, CancellationToken.None);

    /// <summary>
    /// Installs a pre-compiled DLL plugin from a ZIP file.
    /// </summary>
    /// <param name="zipFilePath">Path to the plugin ZIP file.</param>
    /// <param name="pluginsDir">Target directory for plugins.</param>
    /// <param name="cancellationToken">Token used to cancel archive extraction and installation.</param>
    /// <returns>True if installation was successful.</returns>
    public async Task<bool> ExtractAndInstallPluginAsync(
        string zipFilePath,
        string pluginsDir,
        CancellationToken cancellationToken)
    {
        // SECURITY: Validate zip file path
        if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
        {
            Log.Instance.Warning("SECURITY: Invalid or non-existent ZIP file path");
            return false;
        }

        // SECURITY: Validate plugins directory
        if (string.IsNullOrWhiteSpace(pluginsDir))
        {
            Log.Instance.Warning("SECURITY: Invalid plugins directory");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var pluginsRoot = GetCanonicalPluginsRoot(pluginsDir);
        var transactionRoot = GetCanonicalTransactionRoot(pluginsRoot);
        var transactionDir = Path.Combine(transactionRoot, Guid.NewGuid().ToString("N"));
        var tempDir = Path.Combine(transactionDir, "extract");
        string? backupDir = null;
        string? localRoot = null;
        string? targetDir = null;
        string? pluginId = null;
        var hadExistingTarget = false;
        var existingTargetDisplaced = false;
        var replacementMovedIntoPlace = false;
        var targetMutationOccurred = false;
        var pluginIdValidated = false;
        var replacementRuntimeScanStarted = false;
        var installationPrepared = false;
        var preserveRecoveryMaterial = false;
        IDisposable? pluginMutationLease = null;
        PluginRuntimeSnapshot? runtimeBaseline = null;
        PluginRuntimeReconciliation? runtimeReconciliation = null;
        string? originalMainDll = null;
        var originalRuntimeWasLoaded = false;
        string? backupFingerprint = null;

        try
        {
            CreateDirectorySecure(
                Path.GetDirectoryName(pluginsRoot)
                    ?? throw new InvalidDataException("Configured plugin root has no parent."),
                pluginsRoot,
                "configured plugin root");
            CreateDirectorySecure(
                Path.GetDirectoryName(transactionRoot)
                    ?? throw new InvalidDataException("Plugin transaction root has no parent."),
                transactionRoot,
                "plugin transaction root");
            RestrictTransactionDirectoryPermissions(transactionRoot);
            CreateDirectorySecure(transactionRoot, transactionDir, "private plugin transaction directory");
            RestrictTransactionDirectoryPermissions(transactionDir);
            CreateDirectorySecure(transactionDir, tempDir, "plugin extraction directory");
            
            // SECURITY: Use safe ZIP extraction to prevent path traversal
            await ExtractZipSafelyAsync(zipFilePath, tempDir, cancellationToken).ConfigureAwait(false);

            // Analyze and fix the plugin structure
            var archiveDerivedPluginId = Path.GetFileNameWithoutExtension(zipFilePath);
            pluginId = await AnalyzeAndFixPluginStructureAsync(
                    tempDir,
                    IsSafePluginId(archiveDerivedPluginId) ? archiveDerivedPluginId : null)
                .ConfigureAwait(false);
            if (string.IsNullOrEmpty(pluginId))
            {
                throw new InvalidOperationException("No unambiguous plugin DLL found in ZIP file. Main assemblies must use a recognized plugin prefix or exactly match a manifest, wrapper-directory, or archive-derived plugin ID.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // SECURITY: The manifest-derived ID must be validated before it is used in any
            // installation or backup path.
            if (!IsSafePluginId(pluginId))
                throw new InvalidDataException("Plugin package contains an invalid plugin ID.");
            pluginIdValidated = true;
            pluginMutationLease = _pluginManager.AcquirePluginMutation(pluginId);

            localRoot = GetCanonicalLocalPluginRoot(pluginsDir);
            CreateDirectorySecure(pluginsRoot, localRoot, "local plugin root");
            targetDir = EnsureContainedPluginPath(
                localRoot,
                Path.Combine(localRoot, pluginId),
                "plugin installation target");
            hadExistingTarget = Directory.Exists(targetDir);
            runtimeBaseline = _pluginManager.CapturePluginRuntimeSnapshot()
                ?? new PluginRuntimeSnapshot(
                    new Dictionary<string, PluginRuntimeIdentity>(
                        StringComparer.OrdinalIgnoreCase));
            originalRuntimeWasLoaded = runtimeBaseline.Identities.ContainsKey(pluginId);
            if (hadExistingTarget)
            {
                originalMainDll = await ValidatePluginAsync(targetDir).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(originalMainDll))
                {
                    throw new InvalidOperationException(
                        $"Existing plugin {pluginId} has no verifiable recovery assembly.");
                }
            }

            RemoveSharedRuntimePayloadFiles(tempDir);
            cancellationToken.ThrowIfCancellationRequested();

            if (!_pluginManager.ForgetPluginRuntime(pluginId, pluginMutationLease))
            {
                var unloadPending =
                    _pluginManager.GetPluginRuntimeUnloadState(pluginId) ==
                    PluginRuntimeUnloadState.UnloadRequested;
                if (unloadPending)
                {
                    throw new PluginOperationRecoveryException(
                        $"Plugin {pluginId} runtime unload is pending; import files were not mutated.",
                        pluginId,
                        targetDir,
                        unloadPending: true);
                }
                throw new InvalidOperationException($"Plugin {pluginId} runtime could not be unloaded.");
            }

            // Install ZIP-imported plugins to a 'local' subdirectory
            if (hadExistingTarget)
            {
                backupDir = EnsureContainedTransactionPath(
                    transactionDir,
                    Path.Combine(transactionDir, "backup"),
                    "plugin backup target");
                targetDir = EnsureContainedPluginPath(localRoot, targetDir, "plugin installation target");
                MoveDirectorySecure(
                    localRoot,
                    targetDir,
                    transactionDir,
                    backupDir,
                    "existing plugin backup");
                backupFingerprint = ComputeDirectoryFingerprint(backupDir);
                existingTargetDisplaced = true;
                targetMutationOccurred = true;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Renamed existing plugin directory {targetDir} to {backupDir} to resolve conflict during import.");
            }

            // Move organized temp directory to target location
            targetDir = EnsureContainedPluginPath(localRoot, targetDir, "plugin installation target");
            MoveDirectorySecure(
                transactionDir,
                tempDir,
                localRoot,
                targetDir,
                "plugin replacement placement");
            replacementMovedIntoPlace = true;
            targetMutationOccurred = true;
            TryStageCanonicalPluginSharedAssembly(targetDir);
            RemovePluginSdkPayloadFiles(targetDir);

            // Validate the installed plugin
            var installedMainDll = await ValidatePluginAsync(targetDir).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(installedMainDll))
            {
                throw new InvalidOperationException($"Plugin {pluginId} validation failed after installation.");
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Successfully installed plugin {pluginId} to {targetDir}");

            // Do NOT auto-trust local ZIP imports. Under production RequireSignature policy,
            // TrustedPluginPackageStore would let unsigned DLLs load as "trusted online package".
            // Local ZIPs must either be Authenticode-signed or use AllowUnsigned/dev mode.
            // Official marketplace installs still call TrustPluginDirectory after hash verification.

            // Force-load the imported payload before marking it installed so runtime
            // capabilities such as optimization categories are immediately available.
            cancellationToken.ThrowIfCancellationRequested();
            replacementRuntimeScanStarted = true;
            LoadPluginRuntimeStrictWithoutAsyncRetention(
                _pluginManager,
                pluginId,
                installedMainDll,
                pluginMutationLease);
            _pluginManager.PreparePluginInstallation(pluginId, pluginMutationLease);
            installationPrepared = true;
            ActivatePluginRuntimeStrictWithoutAsyncRetention(
                _pluginManager,
                pluginId,
                installedMainDll,
                pluginMutationLease);

            // Cancellation before activation commit rolls back. Once lifecycle commit starts,
            // it runs to completion or throws and restores the target plugin's markers.
            cancellationToken.ThrowIfCancellationRequested();
            _pluginManager.CommitPluginInstallation(pluginId, pluginMutationLease);
            installationPrepared = false;

            if (!string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir))
            {
                try
                {
                    DeleteDirectorySecure(
                        transactionDir,
                        backupDir,
                        "plugin backup cleanup");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to clean up plugin import backup directory {backupDir}: {ex.Message}", ex);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            var backupRestored = false;
            Exception? rollbackFailure = null;
            if (installationPrepared &&
                pluginIdValidated &&
                !string.IsNullOrWhiteSpace(pluginId))
            {
                try
                {
                    _pluginManager.RollbackPreparedPluginInstallation(
                        pluginId,
                        pluginMutationLease);
                    installationPrepared = false;
                }
                catch (Exception preparationRollbackFailure)
                {
                    rollbackFailure = preparationRollbackFailure;
                    preserveRecoveryMaterial = true;
                }
            }
            if (replacementRuntimeScanStarted &&
                runtimeBaseline is not null &&
                !string.IsNullOrWhiteSpace(targetDir))
            {
                try
                {
                    runtimeReconciliation = _pluginManager.ReconcilePluginRuntimes(
                        runtimeBaseline,
                        targetDir,
                        pluginMutationLease,
                        pluginId);
                }
                catch (Exception reconcileEx)
                {
                    rollbackFailure = reconcileEx;
                    preserveRecoveryMaterial = true;
                }
            }

            if (rollbackFailure is null &&
                !string.IsNullOrWhiteSpace(localRoot) &&
                !string.IsNullOrWhiteSpace(targetDir))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(backupDir) &&
                        Directory.Exists(backupDir))
                    {
                        backupDir = EnsureContainedTransactionPath(
                            transactionDir,
                            backupDir,
                            "plugin rollback backup");
                        var currentBackupFingerprint = ComputeDirectoryFingerprint(backupDir);
                        if (string.IsNullOrWhiteSpace(backupFingerprint) ||
                            !string.Equals(
                                currentBackupFingerprint,
                                backupFingerprint,
                                StringComparison.Ordinal))
                        {
                            throw new InvalidDataException(
                                $"Plugin rollback backup changed after it was created: {backupDir}");
                        }
                    }

                    if (replacementMovedIntoPlace && Directory.Exists(targetDir))
                    {
                        targetDir = EnsureContainedPluginPath(localRoot, targetDir, "plugin rollback target");
                        DeleteDirectorySecure(localRoot, targetDir, "plugin rollback target");
                        replacementMovedIntoPlace = false;
                    }

                    if (!string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir))
                    {
                        backupDir = EnsureContainedTransactionPath(
                            transactionDir,
                            backupDir,
                            "plugin rollback backup");
                        targetDir = EnsureContainedPluginPath(localRoot, targetDir, "plugin rollback target");
                        MoveDirectorySecure(
                            transactionDir,
                            backupDir,
                            localRoot,
                            targetDir,
                            "plugin backup restoration");
                        backupRestored = true;

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Rolled back imported plugin directory for {Path.GetFileName(targetDir)} from backup {backupDir}.");
                    }
                }
                catch (Exception rollbackEx)
                {
                    rollbackFailure = rollbackEx;
                    preserveRecoveryMaterial = true;
                }
            }

            if ((backupRestored || hadExistingTarget && !existingTargetDisplaced) &&
                rollbackFailure is null &&
                originalRuntimeWasLoaded &&
                pluginIdValidated &&
                !string.IsNullOrWhiteSpace(pluginId) &&
                _pluginManager.GetPluginRuntimeUnloadState(pluginId) !=
                PluginRuntimeUnloadState.UnloadRequested &&
                !string.IsNullOrWhiteSpace(originalMainDll) &&
                runtimeBaseline is not null)
            {
                try
                {
                    _pluginManager.RestorePluginRuntimeSnapshot(
                        runtimeBaseline,
                        pluginMutationLease,
                        runtimeReconciliation ??
                        new PluginRuntimeReconciliation([pluginId]));
                }
                catch (Exception reloadEx)
                {
                    rollbackFailure = new InvalidOperationException(
                        $"Original plugin {pluginId} files were restored but runtime activation failed.",
                        reloadEx);
                    preserveRecoveryMaterial = true;
                }
            }

            if (pluginIdValidated && !string.IsNullOrWhiteSpace(pluginId))
            {
                // Never derive trust from restored files. Leave the store unchanged when the
                // original survived, and remove stale trust only after a target mutation left
                // no original installation in place.
                var originalInstallationPreserved =
                    hadExistingTarget && (!existingTargetDisplaced || backupRestored);
                if (targetMutationOccurred &&
                    !originalInstallationPreserved &&
                    _pluginManager.GetPluginRuntimeUnloadState(pluginId) !=
                    PluginRuntimeUnloadState.UnloadRequested)
                {
                    try
                    {
                        TrustedPluginPackageStore.RemoveStrict(pluginId);
                    }
                    catch (Exception trustCleanupFailure)
                    {
                        rollbackFailure = rollbackFailure is null
                            ? trustCleanupFailure
                            : new AggregateException(
                                "Plugin import trust cleanup and rollback both failed.",
                                rollbackFailure,
                                trustCleanupFailure);
                        preserveRecoveryMaterial = true;
                    }
                }
            }

            Log.Instance.Error($"Failed to install plugin from {zipFilePath}: {ex.Message}", ex);
            if (rollbackFailure is not null)
            {
                var recoveryLocation =
                    !string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir)
                        ? backupDir
                        : targetDir ?? transactionDir;
                var unloadPending =
                    pluginIdValidated &&
                    !string.IsNullOrWhiteSpace(pluginId) &&
                    _pluginManager.GetPluginRuntimeUnloadState(pluginId) ==
                        PluginRuntimeUnloadState.UnloadRequested;
                throw new PluginImportRecoveryException(
                    $"Plugin import failed and rollback is incomplete. Recovery material: {recoveryLocation}",
                    pluginIdValidated ? pluginId : null,
                    recoveryLocation,
                    unloadPending,
                    ex,
                    rollbackFailure);
            }
            throw;
        }
        finally
        {
            try
            {
                var backupStillPresent =
                    !string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir);
                if (!preserveRecoveryMaterial &&
                    !backupStillPresent &&
                    Directory.Exists(transactionDir))
                {
                    DeleteDirectorySecure(transactionRoot, transactionDir, "plugin transaction cleanup");
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to cleanup temporary directory {tempDir}: {ex.Message}", ex);
            }
            finally
            {
                pluginMutationLease?.Dispose();
            }
        }
    }

    public async Task<PluginOperationOutcome> ExtractAndInstallPluginWithOutcomeAsync(
        string zipFilePath,
        string pluginsDir,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await ExtractAndInstallPluginAsync(
                    zipFilePath,
                    pluginsDir,
                    cancellationToken)
                .ConfigureAwait(false);
            return success
                ? new PluginOperationOutcome(true)
                : new PluginOperationOutcome(
                    false,
                    Error: $"Failed to import plugin from '{zipFilePath}'.");
        }
        catch (PluginOperationRecoveryException ex)
        {
            return new PluginOperationOutcome(
                false,
                Degraded: true,
                UnloadPending: ex.UnloadPending,
                RecoveryId: ex.RecoveryId,
                RecoveryPath: ex.RecoveryPath,
                Error: ex.Message);
        }
        catch (PluginImportRecoveryException ex)
        {
            return new PluginOperationOutcome(
                false,
                Degraded: true,
                UnloadPending: ex.UnloadPending,
                RecoveryId: ex.RecoveryId,
                RecoveryPath: ex.RecoveryPath,
                Error: ex.Message);
        }
        catch (Exception ex)
        {
            return new PluginOperationOutcome(
                false,
                Error: $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    internal static string ComputeDirectoryFingerprint(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        var normalizedDirectory = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(normalizedDirectory))
            throw new DirectoryNotFoundException(normalizedDirectory);

        using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var filePath in Directory.GetFiles(
                     normalizedDirectory,
                     "*",
                     SearchOption.AllDirectories)
                 .OrderBy(
                     path => Path.GetRelativePath(normalizedDirectory, path),
                     StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(normalizedDirectory, filePath)
                .Replace(Path.DirectorySeparatorChar, '/');
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            aggregate.AppendData(BitConverter.GetBytes(pathBytes.Length));
            aggregate.AppendData(pathBytes);
            using var stream = File.OpenRead(filePath);
            var fileHash = SHA256.HashData(stream);
            aggregate.AppendData(BitConverter.GetBytes(stream.Length));
            aggregate.AppendData(fileHash);
        }

        return Convert.ToHexString(aggregate.GetHashAndReset());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadPluginRuntimeStrictWithoutAsyncRetention(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease)
    {
        var failure = TryLoadPluginRuntimeStrict(
            pluginManager,
            pluginId,
            mainDllPath,
            mutationLease);
        if (failure is not null)
            throw new InvalidOperationException(failure);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string? TryLoadPluginRuntimeStrict(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease)
    {
        try
        {
            pluginManager.LoadPluginRuntimeStrictAsync(
                pluginId,
                mainDllPath,
                mutationLease)
                .GetAwaiter()
                .GetResult();
            return null;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ActivatePluginRuntimeStrictWithoutAsyncRetention(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease)
    {
        var failure = TryActivatePluginRuntimeStrict(
            pluginManager,
            pluginId,
            mainDllPath,
            mutationLease);
        if (failure is not null)
            throw new InvalidOperationException(failure);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string? TryActivatePluginRuntimeStrict(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease)
    {
        try
        {
            pluginManager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                mainDllPath,
                mutationLease)
                .GetAwaiter()
                .GetResult();
            return null;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    /// <summary>
    /// Validates the plugin directory for required files and valid assembly.
    /// </summary>
    private async Task<string?> ValidatePluginAsync(string pluginDir)
    {
        try
        {
            await Task.Yield();

            var manifestPluginId = TryReadPluginIdFromManifest(pluginDir);
            if (File.Exists(Path.Combine(pluginDir, "plugin.json")) &&
                string.IsNullOrWhiteSpace(manifestPluginId))
            {
                return null;
            }
            var expectedPluginId = manifestPluginId ?? Path.GetFileName(pluginDir);
            var pluginDll = FindPluginMainDll(
                pluginDir,
                manifestPluginId,
                expectedPluginId,
                SearchOption.TopDirectoryOnly);

            if (string.IsNullOrEmpty(pluginDll))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Validation failed: No plugin DLL found.");
                return null;
            }

            // Optional: Check for manifest file if required by the system
            var manifestFile = Path.Combine(pluginDir, "plugin.json");
            if (!File.Exists(manifestFile))
            {
                // Some plugins might not have a manifest yet, but we should log it
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Warning: Plugin manifest (plugin.json) not found.");
            }

            // Verify it's a valid .NET assembly
            try
            {
                global::System.Reflection.AssemblyName.GetAssemblyName(pluginDll);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Validation failed: {pluginDll} is not a valid assembly. {ex.Message}");
                return null;
            }

            return Path.GetFullPath(pluginDll);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Plugin validation error: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Analyzes the extracted directory and reorganizes it into a standard plugin structure.
    /// </summary>
    /// <returns>The plugin ID if successful.</returns>
    public async Task<string?> AnalyzeAndFixPluginStructureAsync(
        string extractDir,
        string? expectedRootPluginId = null)
    {
        await Task.Yield();

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Analyzing plugin structure in {extractDir}");

            // Case 1: Root directory contains the DLL
            var rootManifestPluginId = TryReadPluginIdFromManifest(extractDir);
            if (File.Exists(Path.Combine(extractDir, "plugin.json")) &&
                string.IsNullOrWhiteSpace(rootManifestPluginId))
            {
                return null;
            }
            if (rootManifestPluginId is not null && !IsSafePluginId(rootManifestPluginId))
                throw new InvalidDataException("Plugin package contains an invalid plugin ID.");
            var rootDll = FindPluginMainDll(
                extractDir,
                rootManifestPluginId,
                expectedPluginId: rootManifestPluginId is null ? expectedRootPluginId : null,
                SearchOption.TopDirectoryOnly);
            if (rootDll != null)
            {
                return GetPluginIdFromDll(rootDll, rootManifestPluginId);
            }

            // Case 2: DLL is inside a subfolder
            var subDirs = Directory.GetDirectories(extractDir);
            var candidates = new List<(string Directory, string Dll, string? ManifestPluginId)>();
            foreach (var subDir in subDirs)
            {
                var subDirManifestPluginId = TryReadPluginIdFromManifest(subDir);
                if (File.Exists(Path.Combine(subDir, "plugin.json")) &&
                    string.IsNullOrWhiteSpace(subDirManifestPluginId))
                {
                    continue;
                }
                if (subDirManifestPluginId is not null && !IsSafePluginId(subDirManifestPluginId))
                    throw new InvalidDataException("Plugin package contains an invalid plugin ID.");
                var expectedPluginId = subDirManifestPluginId ?? Path.GetFileName(subDir);
                var subDirDll = FindPluginMainDll(
                    subDir,
                    subDirManifestPluginId,
                    expectedPluginId,
                    SearchOption.TopDirectoryOnly);
                if (subDirDll != null)
                    candidates.Add((subDir, subDirDll, subDirManifestPluginId));
            }

            if (candidates.Count != 1)
                return null;

            var candidate = candidates[0];
            var pluginId = GetPluginIdFromDll(candidate.Dll, candidate.ManifestPluginId);
            await MoveDirectoryContentsAsync(candidate.Directory, extractDir).ConfigureAwait(false);
            DeleteDirectorySecure(
                extractDir,
                candidate.Directory,
                "plugin package wrapper cleanup");
            return pluginId;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error analyzing plugin structure: {ex.Message}", ex);
            return null;
        }
    }

    internal static string? FindPluginMainDll(
        string searchRoot,
        string? manifestPluginId,
        string? expectedPluginId,
        SearchOption searchOption)
    {
        var pluginDlls = Directory.GetFiles(searchRoot, "*.dll", searchOption)
            .Where(path => !IsIgnoredDll(path))
            .ToList();

        if (!pluginDlls.Any())
            return null;

        if (!string.IsNullOrWhiteSpace(manifestPluginId))
        {
            var manifestMatches = pluginDlls
                .Where(path => MatchesCanonicalPluginId(path, manifestPluginId))
                .ToList();
            return manifestMatches.Count == 1 ? manifestMatches[0] : null;
        }

        if (!string.IsNullOrWhiteSpace(expectedPluginId))
        {
            var idDerivedMatches = pluginDlls
                .Where(path => MatchesCanonicalPluginId(path, expectedPluginId))
                .ToList();

            if (idDerivedMatches.Count == 1)
                return idDerivedMatches[0];
            if (idDerivedMatches.Count > 1)
                return null;
        }

        var prefixedDlls = pluginDlls
            .Where(path => PluginAssemblyNaming.IsPluginPrefixedFileName(Path.GetFileName(path)))
            .ToList();

        if (prefixedDlls.Count == 1)
            return prefixedDlls[0];

        return null;
    }

    private static bool MatchesCanonicalPluginId(string dllPath, string pluginId)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(dllPath);
        var assemblyPluginId =
            PluginAssemblyNaming.ExtractPluginIdFromAssemblyFileName(fileNameWithoutExtension)
            ?? fileNameWithoutExtension;
        return NormalizePluginIdentityToken(assemblyPluginId).Equals(
            NormalizePluginIdentityToken(pluginId),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePluginIdentityToken(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool IsIgnoredDll(string dllPath)
    {
        var fileName = Path.GetFileName(dllPath);
        return fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase) ||
               PluginAssemblyNaming.IsSdkOrSharedDllFileName(fileName);
    }

    private static bool ShouldSkipPluginPayloadFile(string filePath)
    {
        return PluginAssemblyNaming.IsSdkOrSharedDllFileName(Path.GetFileName(filePath));
    }

    private void RemoveSharedRuntimePayloadFiles(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            return;

        EnsureDirectoryTreeContainsNoLinks(rootDirectory, "plugin payload cleanup");
        foreach (var file in Directory.GetFiles(rootDirectory, "*.*", SearchOption.AllDirectories))
        {
            if (!ShouldSkipPluginPayloadFile(file))
                continue;

            try
            {
                DeleteFileSecure(rootDirectory, file, "shared plugin payload cleanup");
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to remove shared plugin runtime payload file {file}: {ex.Message}", ex);
            }
        }
    }

    private void RemovePluginSdkPayloadFiles(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            return;

        foreach (var sdkFileName in PluginAssemblyNaming.EnumerateSdkDllFileNames())
        {
            foreach (var file in Directory.GetFiles(rootDirectory, sdkFileName, SearchOption.AllDirectories))
            {
                try
                {
                    DeleteFileSecure(rootDirectory, file, "plugin SDK payload cleanup");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to remove plugin SDK payload file {file}: {ex.Message}", ex);
                }
            }
        }
    }
    private void TryStageCanonicalPluginSharedAssembly(string pluginDirectory)
    {
        var sourcePath = PluginAssemblyNaming.EnumerateAppBaseSharedCandidates().FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        try
        {
            EnsureDirectoryTreeContainsNoLinks(pluginDirectory, "plugin shared runtime staging");
            _mutationBoundary(pluginDirectory);
            EnsureDirectoryTreeContainsNoLinks(pluginDirectory, "plugin shared runtime staging");
            // Always stage both UDT and legacy filenames so dual-load plugins resolve either simple name.
            PluginAssemblyNaming.StageDualNamedSharedDll(sourcePath, pluginDirectory);
            EnsureDirectoryTreeContainsNoLinks(pluginDirectory, "plugin shared runtime staging");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stage canonical plugin shared runtime into {pluginDirectory}: {ex.Message}", ex);
        }
    }

    private static string? TryReadPluginIdFromManifest(string pluginDir)
    {
        try
        {
            if (!PathSecurity.IsValidDirectoryPath(pluginDir))
            {
                Log.Instance.Warning($"SECURITY: Invalid plugin directory path: {pluginDir}");
                return null;
            }

            var fullPluginDir = Path.GetFullPath(pluginDir);
            var manifestPath = Path.Combine(fullPluginDir, "plugin.json");

            if (!PathSecurity.IsPathWithinAllowedDirectory(manifestPath, fullPluginDir))
            {
                Log.Instance.Warning($"SECURITY: Manifest path traversal detected: {manifestPath}");
                return null;
            }
            
            if (!File.Exists(manifestPath))
                return null;

            using var stream = File.OpenRead(manifestPath);
            using var document = JsonDocument.Parse(stream);

            string? pluginId = null;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase))
                    continue;

                pluginId = property.Value.GetString();
                break;
            }

            return string.IsNullOrWhiteSpace(pluginId) ? null : pluginId.Trim();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read plugin manifest in {pluginDir}: {ex.Message}", ex);
            return null;
        }
    }

    private static bool IsSafePluginId(string pluginId)
    {
        var nameBeforeExtension = pluginId.Split('.')[0].TrimEnd('.', ' ');
        return PathSecurity.IsValidPluginId(pluginId) &&
               PathSecurity.IsValidFileName(pluginId) &&
               !Path.IsPathRooted(pluginId) &&
               string.Equals(Path.GetFileName(pluginId), pluginId, StringComparison.Ordinal) &&
               !ReservedWindowsPathNames.Contains(nameBeforeExtension);
    }

    private static string GetCanonicalPluginsRoot(string pluginsDir)
    {
        var pluginsRoot = Path.GetFullPath(pluginsDir);
        var parent = Path.GetDirectoryName(pluginsRoot)
            ?? throw new InvalidDataException("Configured plugin root has no parent directory.");

        EnsureExistingPathComponentsNotLinks(parent, pluginsRoot, "configured plugin root");
        return pluginsRoot;
    }

    private static string GetCanonicalTransactionRoot(string pluginsRoot)
    {
        var canonicalPluginsRoot = Path.GetFullPath(pluginsRoot);
        var parent = Path.GetDirectoryName(canonicalPluginsRoot)
            ?? throw new InvalidDataException("Configured plugin root has no parent directory.");
        var transactionRoot = Path.GetFullPath(Path.Combine(parent, ".udt-plugin-transactions"));

        if (!IsStrictlyWithinDirectory(transactionRoot, parent) ||
            transactionRoot.Equals(canonicalPluginsRoot, PathComparison) ||
            IsStrictlyWithinDirectory(transactionRoot, canonicalPluginsRoot))
        {
            throw new InvalidDataException("Plugin transaction root is not isolated from scanner roots.");
        }

        EnsureExistingPathComponentsNotLinks(parent, transactionRoot, "plugin transaction root");
        return transactionRoot;
    }

    private static string GetCanonicalLocalPluginRoot(string pluginsDir)
    {
        var pluginsRoot = GetCanonicalPluginsRoot(pluginsDir);
        var localRoot = Path.GetFullPath(Path.Combine(pluginsRoot, "local"));

        if (!IsStrictlyWithinDirectory(localRoot, pluginsRoot))
            throw new InvalidDataException("Local plugin root escapes the configured plugins directory.");

        EnsureExistingPathComponentsNotLinks(pluginsRoot, localRoot, "local plugin root");
        return localRoot;
    }

    private static string EnsureContainedPluginPath(string localRoot, string candidatePath, string description)
    {
        var canonicalRoot = Path.GetFullPath(localRoot);
        var canonicalCandidate = Path.GetFullPath(candidatePath);

        if (!IsStrictlyWithinDirectory(canonicalCandidate, canonicalRoot) ||
            !PathSecurity.IsPathWithinAllowedDirectory(canonicalCandidate, canonicalRoot))
        {
            throw new InvalidDataException($"{description} escapes the local plugin root.");
        }

        EnsureExistingPathComponentsNotLinks(canonicalRoot, canonicalCandidate, description);
        return canonicalCandidate;
    }

    private static string EnsureContainedTransactionPath(
        string transactionDirectory,
        string candidatePath,
        string description)
    {
        var canonicalRoot = Path.GetFullPath(transactionDirectory);
        var canonicalCandidate = Path.GetFullPath(candidatePath);
        if (!IsStrictlyWithinDirectory(canonicalCandidate, canonicalRoot))
            throw new InvalidDataException($"{description} escapes the private transaction directory.");

        EnsureExistingPathComponentsNotLinks(canonicalRoot, canonicalCandidate, description);
        return canonicalCandidate;
    }

    private static bool IsStrictlyWithinDirectory(string candidatePath, string baseDirectory)
    {
        var canonicalCandidate = Path.GetFullPath(candidatePath);
        var canonicalBase = Path.GetFullPath(baseDirectory);
        var basePrefix = canonicalBase.EndsWith(Path.DirectorySeparatorChar) ||
                         canonicalBase.EndsWith(Path.AltDirectorySeparatorChar)
            ? canonicalBase
            : canonicalBase + Path.DirectorySeparatorChar;

        return !canonicalCandidate.Equals(canonicalBase, PathComparison) &&
               canonicalCandidate.StartsWith(basePrefix, PathComparison);
    }

    private static void EnsureExistingPathComponentsNotLinks(
        string trustedRoot,
        string candidatePath,
        string description)
    {
        var canonicalRoot = Path.GetFullPath(trustedRoot);
        var canonicalCandidate = Path.GetFullPath(candidatePath);
        if (!canonicalCandidate.Equals(canonicalRoot, PathComparison) &&
            !IsStrictlyWithinDirectory(canonicalCandidate, canonicalRoot))
        {
            throw new InvalidDataException($"{description} escapes its trusted filesystem root.");
        }

        EnsureNotReparsePoint(canonicalRoot, description);
        var relativePath = Path.GetRelativePath(canonicalRoot, canonicalCandidate);
        if (relativePath.Equals(".", StringComparison.Ordinal))
            return;

        var current = canonicalRoot;
        foreach (var segment in relativePath.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!PathExistsWithoutFollowingLinks(current))
                break;
            EnsureNotReparsePoint(current, description);
        }
    }

    private static bool PathExistsWithoutFollowingLinks(string path)
    {
        try
        {
            _ = File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static void EnsureNotReparsePoint(string path, string description)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"{description} must not be a symbolic link or reparse point.");

            FileSystemInfo fileSystemInfo = (attributes & FileAttributes.Directory) != 0
                ? new DirectoryInfo(path)
                : new FileInfo(path);
            if (fileSystemInfo.LinkTarget is not null)
                throw new InvalidDataException($"{description} must not be a symbolic link or reparse point.");
        }
        catch (FileNotFoundException)
        {
            // Nonexistent descendants are permitted only after every existing ancestor passed.
        }
        catch (DirectoryNotFoundException)
        {
            // Nonexistent descendants are permitted only after every existing ancestor passed.
        }
    }

    // These checks deliberately fail closed at every mutation boundary. .NET does not expose
    // portable handle-relative mkdir/rename/unlink operations with no-follow semantics, so a
    // malicious process running as the same user can still race between the final check and the
    // path-based operation. Random private transaction names, same-volume renames, link walking,
    // and immediate post-checks reduce that residual platform limitation but do not eliminate it.
    private void CreateDirectorySecure(string trustedRoot, string path, string description)
    {
        EnsureExistingPathComponentsNotLinks(trustedRoot, path, description);
        var identities = CaptureExistingPathIdentities(trustedRoot, path, description);
        _mutationBoundary(path);
        VerifyPathIdentities(identities, description);
        EnsureExistingPathComponentsNotLinks(trustedRoot, path, description);
        if (OperatingSystem.IsWindows() || Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            Directory.CreateDirectory(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        EnsureExistingPathComponentsNotLinks(trustedRoot, path, description);
        VerifyPostMutationPathIdentities(identities, description);
    }

    private void MoveDirectorySecure(
        string sourceRoot,
        string sourcePath,
        string destinationRoot,
        string destinationPath,
        string description)
    {
        var sourceParent = Path.GetDirectoryName(Path.GetFullPath(sourcePath))
            ?? throw new InvalidDataException($"{description} source has no parent.");
        var destinationParent = Path.GetDirectoryName(Path.GetFullPath(destinationPath))
            ?? throw new InvalidDataException($"{description} destination has no parent.");
        if (!_atomicMoveSupported(sourceParent, destinationParent))
        {
            throw new InvalidDataException(
                $"{description} cannot use an atomic rename across these filesystem locations.");
        }

        EnsureExistingPathComponentsNotLinks(sourceRoot, sourcePath, description);
        EnsureExistingPathComponentsNotLinks(destinationRoot, destinationPath, description);
        EnsureDirectoryTreeContainsNoLinks(sourcePath, description);
        var sourceIdentities = CaptureExistingPathIdentities(sourceRoot, sourcePath, description);
        var sourceParentIdentities = CaptureExistingPathIdentities(
            sourceRoot,
            Path.GetDirectoryName(sourcePath) ?? sourceRoot,
            description);
        var destinationIdentities = CaptureExistingPathIdentities(
            destinationRoot,
            destinationPath,
            description);
        _mutationBoundary(destinationPath);
        VerifyPathIdentities(sourceIdentities, description);
        VerifyPathIdentities(destinationIdentities, description);
        EnsureExistingPathComponentsNotLinks(sourceRoot, sourcePath, description);
        EnsureExistingPathComponentsNotLinks(destinationRoot, destinationPath, description);
        EnsureDirectoryTreeContainsNoLinks(sourcePath, description);
        if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
            throw new IOException($"{description} destination already exists.");

        _moveDirectory(sourcePath, destinationPath);
        if (Directory.Exists(sourcePath) || !Directory.Exists(destinationPath))
            throw new IOException($"{description} did not complete atomically.");

        EnsureExistingPathComponentsNotLinks(sourceRoot, sourceRoot, description);
        VerifyPostMutationPathIdentities(sourceParentIdentities, description);
        VerifyPostMutationPathIdentities(destinationIdentities, description);
        EnsureExistingPathComponentsNotLinks(destinationRoot, destinationPath, description);
        EnsureDirectoryTreeContainsNoLinks(destinationPath, description);
    }

    internal static bool ProbeAtomicDirectoryMove(
        string sourceDirectory,
        string destinationDirectory)
    {
        var token = Guid.NewGuid().ToString("N");
        var sourceProbe = Path.Combine(sourceDirectory, $".udt-move-probe-{token}");
        var destinationProbe = Path.Combine(destinationDirectory, $".udt-move-probe-{token}");
        try
        {
            Directory.CreateDirectory(sourceProbe);
            Directory.Move(sourceProbe, destinationProbe);
            Directory.Move(destinationProbe, sourceProbe);
            Directory.Delete(sourceProbe);
            return true;
        }
        catch
        {
            try
            {
                if (Directory.Exists(destinationProbe))
                    Directory.Move(destinationProbe, sourceProbe);
                if (Directory.Exists(sourceProbe))
                    Directory.Delete(sourceProbe);
            }
            catch
            {
                // Empty unpredictable probe directories contain no plugin payload or backup.
            }
            return false;
        }
    }

    internal static void ValidateOwnedTransactionPath(
        string trustedRoot,
        string path,
        string description)
    {
        var canonicalRoot = Path.GetFullPath(trustedRoot);
        var canonicalPath = Path.GetFullPath(path);
        if (!canonicalPath.Equals(canonicalRoot, PathComparison) &&
            !IsStrictlyWithinDirectory(canonicalPath, canonicalRoot))
        {
            throw new InvalidDataException($"{description} escapes its trusted root.");
        }

        EnsureExistingPathComponentsNotLinks(canonicalRoot, canonicalPath, description);
        if (Directory.Exists(canonicalPath))
            EnsureDirectoryTreeContainsNoLinks(canonicalPath, description);
    }

    internal static void RestrictPrivateTransactionPermissions(string path) =>
        RestrictTransactionDirectoryPermissions(path);

    private void DeleteDirectorySecure(string trustedRoot, string path, string description)
    {
        EnsureExistingPathComponentsNotLinks(trustedRoot, path, description);
        EnsureDirectoryTreeContainsNoLinks(path, description);
        var identities = CaptureExistingPathIdentities(trustedRoot, path, description);
        var parentIdentities = CaptureExistingPathIdentities(
            trustedRoot,
            Path.GetDirectoryName(path) ?? trustedRoot,
            description);
        _mutationBoundary(path);
        VerifyPathIdentities(identities, description);
        EnsureExistingPathComponentsNotLinks(trustedRoot, path, description);
        EnsureDirectoryTreeContainsNoLinks(path, description);
        Directory.Delete(path, recursive: true);
        VerifyPostMutationPathIdentities(parentIdentities, description);
        EnsureExistingPathComponentsNotLinks(trustedRoot, trustedRoot, description);
    }

    private void DeleteFileSecure(string trustedRoot, string path, string description)
    {
        EnsureExistingPathComponentsNotLinks(trustedRoot, path, description);
        EnsureNotReparsePoint(path, description);
        var identities = CaptureExistingPathIdentities(trustedRoot, path, description);
        var parentIdentities = CaptureExistingPathIdentities(
            trustedRoot,
            Path.GetDirectoryName(path) ?? trustedRoot,
            description);
        _mutationBoundary(path);
        VerifyPathIdentities(identities, description);
        EnsureExistingPathComponentsNotLinks(trustedRoot, path, description);
        EnsureNotReparsePoint(path, description);
        File.Delete(path);
        VerifyPostMutationPathIdentities(parentIdentities, description);
        EnsureExistingPathComponentsNotLinks(trustedRoot, trustedRoot, description);
    }

    private void MoveFileSecure(
        string sourceRoot,
        string sourcePath,
        string destinationRoot,
        string destinationPath,
        string description)
    {
        EnsureExistingPathComponentsNotLinks(sourceRoot, sourcePath, description);
        EnsureNotReparsePoint(sourcePath, description);
        EnsureExistingPathComponentsNotLinks(destinationRoot, destinationPath, description);
        var sourceIdentities = CaptureExistingPathIdentities(sourceRoot, sourcePath, description);
        var sourceParentIdentities = CaptureExistingPathIdentities(
            sourceRoot,
            Path.GetDirectoryName(sourcePath) ?? sourceRoot,
            description);
        var destinationIdentities = CaptureExistingPathIdentities(
            destinationRoot,
            destinationPath,
            description);
        _mutationBoundary(destinationPath);
        VerifyPathIdentities(sourceIdentities, description);
        VerifyPathIdentities(destinationIdentities, description);
        EnsureExistingPathComponentsNotLinks(sourceRoot, sourcePath, description);
        EnsureNotReparsePoint(sourcePath, description);
        EnsureExistingPathComponentsNotLinks(destinationRoot, destinationPath, description);
        File.Move(sourcePath, destinationPath);
        if (File.Exists(sourcePath) || !File.Exists(destinationPath))
            throw new IOException($"{description} did not complete.");
        VerifyPostMutationPathIdentities(sourceParentIdentities, description);
        VerifyPostMutationPathIdentities(destinationIdentities, description);
        EnsureExistingPathComponentsNotLinks(sourceRoot, sourceRoot, description);
        EnsureExistingPathComponentsNotLinks(destinationRoot, destinationPath, description);
        EnsureNotReparsePoint(destinationPath, description);
    }

    internal static IReadOnlyList<PathIdentity> CaptureExistingPathIdentities(
        string trustedRoot,
        string candidatePath,
        string description)
    {
        EnsureExistingPathComponentsNotLinks(trustedRoot, candidatePath, description);
        var canonicalRoot = Path.GetFullPath(trustedRoot);
        var canonicalCandidate = Path.GetFullPath(candidatePath);
        var paths = new List<string> { canonicalRoot };
        var relativePath = Path.GetRelativePath(canonicalRoot, canonicalCandidate);
        if (!relativePath.Equals(".", StringComparison.Ordinal))
        {
            var current = canonicalRoot;
            foreach (var segment in relativePath.Split(
                         [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                         StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if (!PathExistsWithoutFollowingLinks(current))
                    break;
                paths.Add(current);
            }
        }

        return paths
            .Select(path => new PathIdentity(
                path,
                File.GetAttributes(path),
                File.GetCreationTimeUtc(path)))
            .ToArray();
    }

    internal static void VerifyPathIdentities(
        IReadOnlyList<PathIdentity> identities,
        string description)
    {
        foreach (var identity in identities)
        {
            EnsureNotReparsePoint(identity.Path, description);
            if (!PathExistsWithoutFollowingLinks(identity.Path) ||
                File.GetAttributes(identity.Path) != identity.Attributes ||
                File.GetCreationTimeUtc(identity.Path) != identity.CreationTimeUtc)
            {
                throw new InvalidDataException(
                    $"{description} changed identity at a filesystem mutation boundary.");
            }
        }
    }

    private static void VerifyPostMutationPathIdentities(
        IReadOnlyList<PathIdentity> identities,
        string description)
    {
        foreach (var identity in identities)
        {
            if (!PathExistsWithoutFollowingLinks(identity.Path))
                throw new InvalidDataException($"{description} changed after a filesystem mutation.");
            EnsureNotReparsePoint(identity.Path, description);
        }

        // Windows exposes a stable creation timestamp for this check. Unix creation-time
        // emulation can change when directory entries mutate, so comparing it after our own
        // operation would reject valid transactions; link/ancestor checks still run there.
        if (OperatingSystem.IsWindows())
            VerifyPathIdentities(identities, description);
    }

    private static void EnsureDirectoryTreeContainsNoLinks(string rootPath, string description)
    {
        if (!Directory.Exists(rootPath))
            return;

        var pending = new Stack<string>();
        pending.Push(rootPath);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            EnsureNotReparsePoint(directory, description);
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                EnsureNotReparsePoint(entry.FullName, description);
                if ((entry.Attributes & FileAttributes.Directory) != 0)
                    pending.Push(entry.FullName);
            }
        }
    }

    private static void RestrictTransactionDirectoryPermissions(string transactionDirectory)
    {
        if (OperatingSystem.IsWindows())
            return;

        File.SetUnixFileMode(
            transactionDirectory,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private string GetPluginIdFromDll(string dllPath, string? manifestPluginId = null)
    {
        if (!string.IsNullOrWhiteSpace(manifestPluginId))
            return manifestPluginId.Trim();

        var dllName = Path.GetFileNameWithoutExtension(dllPath);

        var fromPrefix = PluginAssemblyNaming.ExtractPluginIdFromAssemblyFileName(dllName);
        if (!string.IsNullOrWhiteSpace(fromPrefix))
            return fromPrefix;

        return dllName;
    }

    /// <summary>
    /// Moves all files and subdirectories from source to target.
    /// </summary>
    public async Task MoveDirectoryContentsAsync(string sourceDir, string targetDir)
    {
        await Task.Run(() =>
        {
            var trustedRoot = Path.GetFullPath(targetDir);
            EnsureExistingPathComponentsNotLinks(trustedRoot, sourceDir, "plugin package source");
            EnsureDirectoryTreeContainsNoLinks(sourceDir, "plugin package source");

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (ShouldSkipPluginPayloadFile(file))
                    continue;

                var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                if (File.Exists(destFile))
                    DeleteFileSecure(trustedRoot, destFile, "plugin package file replacement");
                MoveFileSecure(
                    trustedRoot,
                    file,
                    trustedRoot,
                    destFile,
                    "plugin package file move");
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destDir = Path.Combine(targetDir, Path.GetFileName(dir));
                if (Directory.Exists(destDir))
                    DeleteDirectorySecure(trustedRoot, destDir, "plugin package directory replacement");
                MoveDirectorySecure(
                    trustedRoot,
                    dir,
                    trustedRoot,
                    destDir,
                    "plugin package directory move");
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Deep copies a directory.
    /// </summary>
    public void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            if (ShouldSkipPluginPayloadFile(file))
                continue;

            var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }

    }

    /// <summary>
    /// Extracts ZIP file safely by validating entry paths to prevent path traversal attacks.
    /// </summary>
    private async Task ExtractZipSafelyAsync(
        string zipFilePath,
        string extractDir,
        CancellationToken cancellationToken)
    {
        using var archiveStream = new FileStream(
            zipFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            ExtractionBufferSize,
            FileOptions.SequentialScan);
        ValidateArchiveCentralDirectory(archiveStream);
        archiveStream.Position = 0;
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
        cancellationToken.ThrowIfCancellationRequested();

        if (archive.Entries.Count > MaxArchiveEntryCount)
        {
            throw new InvalidDataException(
                $"Plugin archive contains too many entries. Maximum: {MaxArchiveEntryCount}.");
        }

        var canonicalExtractRoot = Path.GetFullPath(extractDir);
        var destinationPaths = new HashSet<string>(PathComparer);
        var validatedEntries = new List<ValidatedZipEntry>(archive.Entries.Count);
        long declaredTotalBytes = 0;

        // Validate every entry before writing any archive content.
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validatedEntry = ValidateArchiveEntryPath(entry, canonicalExtractRoot, destinationPaths);
            if (validatedEntry.IsDirectory && entry.Length != 0)
                throw new InvalidDataException("Plugin archive contains a directory entry with data.");

            if (entry.Length > MaxSingleEntryUncompressedBytes)
            {
                throw new InvalidDataException(
                    $"Plugin archive entry exceeds the {MaxSingleEntryUncompressedBytes}-byte limit.");
            }

            if (!validatedEntry.IsDirectory &&
                entry.Length >= MinimumCompressionRatioCheckBytes &&
                (entry.CompressedLength <= 0 ||
                 entry.Length / (double)entry.CompressedLength > MaxCompressionRatio))
            {
                throw new InvalidDataException(
                    $"Plugin archive entry exceeds the {MaxCompressionRatio:0}-to-1 compression ratio limit.");
            }

            try
            {
                declaredTotalBytes = checked(declaredTotalBytes + entry.Length);
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException("Plugin archive uncompressed size is invalid.", ex);
            }

            if (declaredTotalBytes > MaxTotalUncompressedBytes)
            {
                throw new InvalidDataException(
                    $"Plugin archive exceeds the {MaxTotalUncompressedBytes}-byte total uncompressed limit.");
            }

            validatedEntries.Add(validatedEntry);
        }

        var buffer = new byte[ExtractionBufferSize];
        long extractedTotalBytes = 0;

        foreach (var validatedEntry in validatedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (validatedEntry.IsDirectory)
            {
                CreateDirectorySecure(
                    canonicalExtractRoot,
                    validatedEntry.DestinationPath,
                    "plugin archive directory");
                continue;
            }

            var destinationDirectory = Path.GetDirectoryName(validatedEntry.DestinationPath);
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new InvalidDataException("Plugin archive contains an invalid destination path.");

            CreateDirectorySecure(
                canonicalExtractRoot,
                destinationDirectory,
                "plugin archive destination directory");

            await using var source = validatedEntry.Entry.Open();
            EnsureExistingPathComponentsNotLinks(
                canonicalExtractRoot,
                validatedEntry.DestinationPath,
                "plugin archive destination file");
            _mutationBoundary(validatedEntry.DestinationPath);
            EnsureExistingPathComponentsNotLinks(
                canonicalExtractRoot,
                validatedEntry.DestinationPath,
                "plugin archive destination file");
            await using var destination = new FileStream(
                validatedEntry.DestinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                ExtractionBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            EnsureExistingPathComponentsNotLinks(
                canonicalExtractRoot,
                validatedEntry.DestinationPath,
                "plugin archive destination file");

            long extractedEntryBytes = 0;
            while (true)
            {
                var bytesRead = await source
                    .ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                    break;

                extractedEntryBytes += bytesRead;
                extractedTotalBytes += bytesRead;

                if (extractedEntryBytes > MaxSingleEntryUncompressedBytes)
                    throw new InvalidDataException("Plugin archive entry exceeded its extraction size limit.");

                if (extractedTotalBytes > MaxTotalUncompressedBytes)
                    throw new InvalidDataException("Plugin archive exceeded its total extraction size limit.");

                if (extractedEntryBytes >= MinimumCompressionRatioCheckBytes &&
                    (validatedEntry.Entry.CompressedLength <= 0 ||
                     extractedEntryBytes / (double)validatedEntry.Entry.CompressedLength > MaxCompressionRatio))
                {
                    throw new InvalidDataException("Plugin archive entry exceeded its compression ratio limit.");
                }

                await destination
                    .WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken)
                    .ConfigureAwait(false);
            }

            if (extractedEntryBytes != validatedEntry.Entry.Length)
                throw new InvalidDataException("Plugin archive entry length does not match its metadata.");

            EnsureExistingPathComponentsNotLinks(
                canonicalExtractRoot,
                validatedEntry.DestinationPath,
                "plugin archive destination file");
        }
    }

    private static void ValidateArchiveCentralDirectory(FileStream stream)
    {
        if (!stream.CanSeek || stream.Length < 22 || stream.Length > MaxArchiveCompressedBytes)
        {
            throw new InvalidDataException(
                $"Plugin archive must be between 22 and {MaxArchiveCompressedBytes} bytes.");
        }

        var searchLength = (int)Math.Min(stream.Length, 22L + ushort.MaxValue);
        var searchOffset = stream.Length - searchLength;
        stream.Position = searchOffset;
        var tail = ReadExactly(stream, searchLength);

        var eocdIndex = -1;
        for (var index = tail.Length - 22; index >= 0; index--)
        {
            if (ReadUInt32(tail, index) != EndOfCentralDirectorySignature)
                continue;

            var commentLength = ReadUInt16(tail, index + 20);
            if (index + 22 + commentLength == tail.Length)
            {
                eocdIndex = index;
                break;
            }
        }

        if (eocdIndex < 0)
            throw new InvalidDataException("Plugin archive has no valid end-of-central-directory record.");

        var eocdOffset = checked(searchOffset + eocdIndex);
        var diskNumber = ReadUInt16(tail, eocdIndex + 4);
        var centralDirectoryDisk = ReadUInt16(tail, eocdIndex + 6);
        var entriesOnDisk16 = ReadUInt16(tail, eocdIndex + 8);
        var totalEntries16 = ReadUInt16(tail, eocdIndex + 10);
        var centralDirectorySize32 = ReadUInt32(tail, eocdIndex + 12);
        var centralDirectoryOffset32 = ReadUInt32(tail, eocdIndex + 16);
        if (diskNumber != 0 || centralDirectoryDisk != 0 || entriesOnDisk16 != totalEntries16)
            throw new InvalidDataException("Multi-disk plugin archives are not supported.");

        ulong entryCount = totalEntries16;
        ulong centralDirectorySize = centralDirectorySize32;
        ulong centralDirectoryOffset = centralDirectoryOffset32;
        long centralDirectoryBoundary = eocdOffset;
        var requiresZip64 =
            totalEntries16 == ushort.MaxValue ||
            centralDirectorySize32 == uint.MaxValue ||
            centralDirectoryOffset32 == uint.MaxValue;

        if (requiresZip64)
        {
            const int locatorLength = 20;
            var locatorOffset = eocdOffset - locatorLength;
            if (locatorOffset < 0)
                throw new InvalidDataException("Plugin ZIP64 archive is missing its locator.");

            stream.Position = locatorOffset;
            var locator = ReadExactly(stream, locatorLength);
            if (ReadUInt32(locator, 0) != Zip64EndOfCentralDirectoryLocatorSignature ||
                ReadUInt32(locator, 4) != 0 ||
                ReadUInt32(locator, 16) != 1)
            {
                throw new InvalidDataException("Plugin ZIP64 archive has an invalid locator.");
            }

            var zip64RecordOffsetValue = ReadUInt64(locator, 8);
            if (zip64RecordOffsetValue > long.MaxValue)
                throw new InvalidDataException("Plugin ZIP64 record offset is invalid.");

            var zip64RecordOffset = (long)zip64RecordOffsetValue;
            if (zip64RecordOffset < 0 || zip64RecordOffset + 56 > locatorOffset)
                throw new InvalidDataException("Plugin ZIP64 record is outside the archive.");

            stream.Position = zip64RecordOffset;
            var zip64Header = ReadExactly(stream, 56);
            if (ReadUInt32(zip64Header, 0) != Zip64EndOfCentralDirectorySignature)
                throw new InvalidDataException("Plugin ZIP64 end record is invalid.");

            var zip64RecordSize = ReadUInt64(zip64Header, 4);
            if (zip64RecordSize < 44 ||
                zip64RecordSize > (ulong)(locatorOffset - zip64RecordOffset - 12) ||
                (ulong)zip64RecordOffset + 12 + zip64RecordSize != (ulong)locatorOffset)
            {
                throw new InvalidDataException("Plugin ZIP64 end record size is inconsistent.");
            }

            if (ReadUInt32(zip64Header, 16) != 0 ||
                ReadUInt32(zip64Header, 20) != 0 ||
                ReadUInt64(zip64Header, 24) != ReadUInt64(zip64Header, 32))
            {
                throw new InvalidDataException("Multi-disk plugin ZIP64 archives are not supported.");
            }

            entryCount = ReadUInt64(zip64Header, 32);
            centralDirectorySize = ReadUInt64(zip64Header, 40);
            centralDirectoryOffset = ReadUInt64(zip64Header, 48);
            centralDirectoryBoundary = zip64RecordOffset;
        }

        if (entryCount > MaxArchiveEntryCount)
            throw new InvalidDataException($"Plugin archive contains more than {MaxArchiveEntryCount} entries.");
        if (centralDirectorySize > MaxCentralDirectoryBytes)
            throw new InvalidDataException(
                $"Plugin archive central directory exceeds {MaxCentralDirectoryBytes} bytes.");
        if (centralDirectoryOffset > long.MaxValue || centralDirectorySize > long.MaxValue)
            throw new InvalidDataException("Plugin archive central directory metadata is invalid.");

        var centralDirectoryStart = (long)centralDirectoryOffset;
        var centralDirectoryLength = (long)centralDirectorySize;
        long centralDirectoryEnd;
        try
        {
            centralDirectoryEnd = checked(centralDirectoryStart + centralDirectoryLength);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("Plugin archive central directory overflows its file bounds.", ex);
        }

        if (centralDirectoryStart < 0 ||
            centralDirectoryEnd != centralDirectoryBoundary ||
            centralDirectoryEnd > stream.Length)
        {
            throw new InvalidDataException("Plugin archive central directory bounds are inconsistent.");
        }

        stream.Position = centralDirectoryStart;
        for (ulong index = 0; index < entryCount; index++)
        {
            var header = ReadExactly(stream, 46);
            if (ReadUInt32(header, 0) != CentralDirectoryFileHeaderSignature)
                throw new InvalidDataException("Plugin archive central directory contains an invalid entry.");

            var variableLength =
                (long)ReadUInt16(header, 28) +
                ReadUInt16(header, 30) +
                ReadUInt16(header, 32);
            if (stream.Position + variableLength > centralDirectoryEnd)
                throw new InvalidDataException("Plugin archive central directory entry exceeds its bounds.");

            stream.Position += variableLength;
        }

        if (stream.Position != centralDirectoryEnd)
            throw new InvalidDataException("Plugin archive central directory entry count is inconsistent.");
    }

    private static byte[] ReadExactly(Stream stream, int byteCount)
    {
        var buffer = new byte[byteCount];
        stream.ReadExactly(buffer);
        return buffer;
    }

    private static ushort ReadUInt16(byte[] buffer, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset, sizeof(ushort)));

    private static uint ReadUInt32(byte[] buffer, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset, sizeof(uint)));

    private static ulong ReadUInt64(byte[] buffer, int offset) =>
        BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(offset, sizeof(ulong)));

    private static ValidatedZipEntry ValidateArchiveEntryPath(
        ZipArchiveEntry entry,
        string canonicalExtractRoot,
        HashSet<string> destinationPaths)
    {
        if (string.IsNullOrWhiteSpace(entry.FullName) || entry.FullName.IndexOf('\0') >= 0)
            throw new InvalidDataException("Plugin archive contains an invalid entry path.");

        var normalizedPath = entry.FullName.Replace('\\', '/');
        if (normalizedPath.StartsWith("/", StringComparison.Ordinal) ||
            Path.IsPathRooted(entry.FullName) ||
            Path.IsPathRooted(normalizedPath) ||
            normalizedPath.Contains(':'))
        {
            throw new InvalidDataException("Plugin archive contains a rooted entry path.");
        }

        var isDirectory = normalizedPath.EndsWith("/", StringComparison.Ordinal);
        var segments = normalizedPath.Split('/', StringSplitOptions.None);
        if (isDirectory)
            segments = segments[..^1];

        if (segments.Length == 0 || segments.Any(segment => !IsValidArchivePathSegment(segment)))
            throw new InvalidDataException("Plugin archive contains an invalid entry path.");

        var destinationPath = Path.GetFullPath(
            Path.Combine(canonicalExtractRoot, Path.Combine(segments)));
        if (!IsStrictlyWithinDirectory(destinationPath, canonicalExtractRoot))
            throw new InvalidDataException("Plugin archive entry escapes the extraction directory.");

        if (!destinationPaths.Add(destinationPath))
            throw new InvalidDataException("Plugin archive contains duplicate destination entries.");

        return new ValidatedZipEntry(entry, destinationPath, isDirectory);
    }

    private static bool IsValidArchivePathSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment) ||
            segment.Equals(".", StringComparison.Ordinal) ||
            segment.Equals("..", StringComparison.Ordinal) ||
            segment.EndsWith(".", StringComparison.Ordinal) ||
            segment.EndsWith(" ", StringComparison.Ordinal) ||
            segment.IndexOfAny(InvalidArchivePathCharacters) >= 0 ||
            segment.Any(char.IsControl))
        {
            return false;
        }

        var nameBeforeExtension = segment.Split('.')[0].TrimEnd('.', ' ');
        return !ReservedWindowsPathNames.Contains(nameBeforeExtension);
    }

    private readonly record struct ValidatedZipEntry(
        ZipArchiveEntry Entry,
        string DestinationPath,
        bool IsDirectory);

    internal readonly record struct PathIdentity(
        string Path,
        FileAttributes Attributes,
        DateTime CreationTimeUtc);
}
