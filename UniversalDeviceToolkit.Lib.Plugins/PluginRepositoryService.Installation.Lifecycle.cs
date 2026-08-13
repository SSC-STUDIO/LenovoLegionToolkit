using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Plugins.Resources;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    /// <summary>
    /// Serializes installation transactions so concurrent downloads cannot replace or scan
    /// the same plugin directory at the same time.
    /// </summary>
    public async Task<bool> DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        var outcome = await DownloadAndInstallPluginWithOutcomeAsync(manifest)
            .ConfigureAwait(false);
        return outcome.Success && !outcome.Degraded;
    }

    public async Task<PluginOperationOutcome> DownloadAndInstallPluginWithOutcomeAsync(
        PluginManifest manifest)
    {
        if (!PathSecurity.IsValidPluginId(manifest.Id))
        {
            Log.Instance.Warning($"Rejected plugin installation with invalid plugin id: {manifest.Id}");
            DownloadFailed?.Invoke(
                this,
                string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
            return new PluginOperationOutcome(
                false,
                Error: $"Rejected plugin installation with invalid plugin id: {manifest.Id}");
        }

        using var mutation = _pluginManager.AcquirePluginMutation(manifest.Id);
        return await DownloadAndInstallPluginCoreAsync(manifest, mutation).ConfigureAwait(false);
    }

    private async Task<PluginOperationOutcome> DownloadAndInstallPluginCoreAsync(
        PluginManifest manifest,
        IDisposable mutationLease)
    {
        var tempFilePath = Path.Combine(_tempDownloadDirectory, $"{manifest.Id}.zip");
        var extractPath = Path.Combine(_tempDownloadDirectory, manifest.Id);
        PluginRuntimeSnapshot? runtimeBaseline = null;
        PluginRuntimeReconciliation? runtimeReconciliation = null;
        RepositoryInstallationTransaction? transaction = null;
        var installationPrepared = false;
        var runtimeUnloadPending = false;
        var recoveryRetained = false;

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting download and install for plugin: {manifest.Id}");

            var versionChecker = new VersionChecker();
            if (!versionChecker.IsCompatible(manifest.MinimumHostVersion))
            {
                var compatibilityMessage = string.Format(
                    Resource.Plugin_Error_Repository_HostIncompatible,
                    manifest.Id,
                    manifest.MinimumHostVersion);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(compatibilityMessage);

                DownloadFailed?.Invoke(this, compatibilityMessage);
                return new PluginOperationOutcome(false, Error: compatibilityMessage);
            }

            var downloadResult = await DownloadPluginAsync(manifest, tempFilePath).ConfigureAwait(false);
            if (!downloadResult.Success)
            {
                DownloadFailed?.Invoke(this, string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
                return new PluginOperationOutcome(
                    false,
                    Error: string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
            }

            if (!await VerifyDownloadedPackageIntegrityAsync(
                    tempFilePath,
                    manifest,
                    downloadResult.TrustAsOfficialOnlinePackage).ConfigureAwait(false))
            {
                DownloadFailed?.Invoke(this, string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
                return new PluginOperationOutcome(
                    false,
                    Error: string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
            }

            runtimeBaseline = _pluginManager.CapturePluginRuntimeSnapshot();
            if (runtimeBaseline.Identities.ContainsKey(manifest.Id) &&
                !_pluginManager.ForgetPluginRuntime(manifest.Id, mutationLease))
            {
                runtimeUnloadPending =
                    _pluginManager.GetPluginRuntimeUnloadState(manifest.Id) ==
                    PluginRuntimeUnloadState.UnloadRequested;
                throw new InvalidOperationException(
                    $"Existing plugin runtime {manifest.Id} could not be unloaded for repository update.");
            }

            transaction = await ExtractAndInstallPluginAsync(
                tempFilePath,
                extractPath,
                manifest,
                downloadResult.TrustAsOfficialOnlinePackage).ConfigureAwait(false);

            if (transaction is null)
            {
                _pluginManager.RestorePluginRuntimeSnapshot(
                    runtimeBaseline,
                    mutationLease,
                    new PluginRuntimeReconciliation([manifest.Id]));
                return new PluginOperationOutcome(
                    false,
                    Error: $"Repository package extraction failed for '{manifest.Id}'.");
            }

            LoadRepositoryRuntimeStrictWithoutAsyncRetention(
                _pluginManager,
                manifest.Id,
                transaction.InstalledMainDll,
                mutationLease,
                transaction.PackageAuthorization);

            if (!IsInstalledPluginUsable(manifest))
                throw new InvalidOperationException(
                    string.Format(Resource.Plugin_Error_Repository_NotLoadable, manifest.Id));

            _pluginManager.PreparePluginInstallation(manifest.Id, mutationLease);
            installationPrepared = true;
            ActivateRepositoryRuntimeStrictWithoutAsyncRetention(
                _pluginManager,
                manifest.Id,
                transaction.InstalledMainDll,
                mutationLease,
                transaction.PackageAuthorization);
            _pluginManager.CommitPluginInstallation(
                manifest.Id,
                mutationLease,
                transaction.CommitTrust);
            installationPrepared = false;
            transaction.Commit();
            transaction = null;
            runtimeBaseline = null;
            var completionSubscribers = DownloadCompleted?.GetInvocationList();
            if (completionSubscribers is not null)
            {
                foreach (var subscriber in completionSubscribers)
                {
                    try
                    {
                        ((EventHandler<string>)subscriber)(this, manifest.Id);
                    }
                    catch (Exception eventEx)
                    {
                        Log.Instance.Error(
                            $"A repository completion subscriber failed for {manifest.Id}.",
                            eventEx);
                    }
                }
            }
            return new PluginOperationOutcome(true);
        }
        catch (Exception ex)
        {
            var failures = new List<Exception> { ex };
            if (installationPrepared)
            {
                try
                {
                    _pluginManager.RollbackPreparedPluginInstallation(
                        manifest.Id,
                        mutationLease);
                    installationPrepared = false;
                }
                catch (Exception preparationRollbackFailure)
                {
                    failures.Add(preparationRollbackFailure);
                }
            }
            var runtimeReconciled = false;
            if (transaction is not null && runtimeBaseline is not null)
            {
                try
                {
                    runtimeReconciliation = _pluginManager.ReconcilePluginRuntimes(
                        runtimeBaseline,
                        transaction.TargetDirectory,
                        mutationLease,
                        manifest.Id);
                    runtimeReconciled = true;
                }
                catch (Exception rollbackEx)
                {
                    transaction.RetainRecoveryMaterial();
                    recoveryRetained = true;
                    failures.Add(new InvalidOperationException(
                        $"Replacement runtime unload is unconfirmed; file rollback was not attempted. " +
                        $"Replacement: {transaction.TargetDirectory}; backup: {transaction.BackupDirectory}; " +
                        $"transaction: {transaction.TransactionDirectory}",
                        rollbackEx));
                }

                if (runtimeReconciled)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rollbackEx)
                    {
                        transaction.RetainRecoveryMaterial();
                        recoveryRetained = true;
                        failures.Add(rollbackEx);
                    }
                }
            }

            if (runtimeBaseline is not null &&
                !runtimeUnloadPending &&
                (transaction is null || runtimeReconciled))
            {
                try
                {
                    _pluginManager.RestorePluginRuntimeSnapshot(
                        runtimeBaseline,
                        mutationLease,
                        runtimeReconciliation ??
                        new PluginRuntimeReconciliation([manifest.Id]));
                }
                catch (Exception rollbackEx)
                {
                    failures.Add(rollbackEx);
                }
            }

            var failure = failures.Count == 1
                ? ex
                : new AggregateException(
                    $"Repository installation failed and rollback was degraded for {manifest.Id}.",
                    failures);
            Log.Instance.Error($"Error installing plugin {manifest.Id}: {failure.Message}", failure);
            DownloadFailed?.Invoke(this, failure.Message);
            var degraded = runtimeUnloadPending || recoveryRetained || failures.Count > 1;
            return new PluginOperationOutcome(
                false,
                Degraded: degraded,
                UnloadPending: runtimeUnloadPending,
                RecoveryId: degraded ? manifest.Id : null,
                RecoveryPath: recoveryRetained
                    ? transaction?.TransactionDirectory
                    : runtimeUnloadPending
                        ? transaction?.TargetDirectory ??
                          Path.Combine(_pluginsDirectory, manifest.Id)
                        : null,
                Error: failure.Message);
        }
        finally
        {
            CleanupInstallationArtifacts(tempFilePath, extractPath);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsInstalledPluginUsable(PluginManifest manifest)
    {
        if (_pluginManager.TryGetPlugin(manifest.Id, out var plugin) && plugin is not null and not PluginManifestAdapter)
            return true;

        return PluginUiCapabilityResolver.ResolveFromManifest(manifest).HasAny ||
               PluginUiCapabilityResolver.ResolveFromInstalledManifest(manifest.Id).HasAny;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadRepositoryRuntimeStrictWithoutAsyncRetention(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease,
        PluginPackageAuthorization? authorization)
    {
        var failure = TryLoadRepositoryRuntimeStrict(
            pluginManager,
            pluginId,
            mainDllPath,
            mutationLease,
            authorization);
        if (failure is not null)
            throw new InvalidOperationException(failure);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string? TryLoadRepositoryRuntimeStrict(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease,
        PluginPackageAuthorization? authorization)
    {
        try
        {
            pluginManager.LoadPluginRuntimeStrictAsync(
                pluginId,
                mainDllPath,
                mutationLease,
                authorization)
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
    private static void ActivateRepositoryRuntimeStrictWithoutAsyncRetention(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease,
        PluginPackageAuthorization? authorization)
    {
        var failure = TryActivateRepositoryRuntimeStrict(
            pluginManager,
            pluginId,
            mainDllPath,
            mutationLease,
            authorization);
        if (failure is not null)
            throw new InvalidOperationException(failure);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string? TryActivateRepositoryRuntimeStrict(
        IPluginManager pluginManager,
        string pluginId,
        string mainDllPath,
        IDisposable mutationLease,
        PluginPackageAuthorization? authorization)
    {
        try
        {
            pluginManager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                mainDllPath,
                mutationLease,
                authorization)
                .GetAwaiter()
                .GetResult();
            return null;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    private Task RemoveUnusableInstalledPayloadAsync(string pluginId)
    {
        TrustedPluginPackageStore.RemoveStrict(pluginId);
        return RestorePluginDirectoryAsync(Path.Combine(_pluginsDirectory, pluginId), backupDir: null, pluginId);
    }

    private static void CleanupInstallationArtifacts(string tempFilePath, string extractPath)
    {
        DeletePartialDownload(tempFilePath);

        try
        {
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, recursive: true);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-repo-extract-cleanup",
                $"Failed to clean up plugin extraction directory: {extractPath}",
                ex);
        }
    }

}
