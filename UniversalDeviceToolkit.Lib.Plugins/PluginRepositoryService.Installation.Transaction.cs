using System;
using System.IO;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    private RepositoryInstallationTransaction InstallExtractedPluginPayload(
        string extractPath,
        string pluginDir,
        PluginManifest manifest,
        bool trustAsOfficialOnlinePackage)
    {
        var pluginsRoot = Path.GetFullPath(_pluginsDirectory);
        var pluginsParent = Path.GetDirectoryName(pluginsRoot)
            ?? throw new InvalidDataException("Plugin root has no parent directory.");
        PluginInstallationService.ValidateOwnedTransactionPath(
            pluginsParent,
            pluginsRoot,
            "repository plugin root");
        var transactionRoot = Path.GetFullPath(
            Path.Combine(pluginsParent, ".udt-plugin-transactions"));
        var transactionDir = Path.Combine(transactionRoot, Guid.NewGuid().ToString("N"));
        var backupDir = Path.Combine(transactionDir, "backup");
        Directory.CreateDirectory(transactionRoot);
        PluginInstallationService.ValidateOwnedTransactionPath(
            pluginsParent,
            transactionRoot,
            "repository transaction root");
        PluginInstallationService.RestrictPrivateTransactionPermissions(transactionRoot);
        Directory.CreateDirectory(transactionDir);
        PluginInstallationService.RestrictPrivateTransactionPermissions(transactionDir);
        PluginInstallationService.ValidateOwnedTransactionPath(
            transactionRoot,
            transactionDir,
            "repository private transaction");
        PluginInstallationService.ValidateOwnedTransactionPath(
            pluginsRoot,
            pluginDir,
            "repository plugin target");

        if (!_atomicMoveSupported(transactionRoot, pluginsRoot))
            throw new IOException("Repository installation requires same-volume atomic directory rename support.");

        var originalTrustRecord =
            TrustedPluginPackageStore.CaptureExactTrustRecord(manifest.Id);
        var transaction = new RepositoryInstallationTransaction(
            pluginDir,
            transactionDir,
            backupDir,
            manifest.Id,
            originalTrustRecord,
            _moveDirectory,
            _deleteDirectory,
            _repositoryMutationBoundary,
            pluginsRoot);

        if (Directory.Exists(pluginDir))
        {
            try
            {
                PluginInstallationService.ValidateOwnedTransactionPath(
                    pluginsRoot,
                    pluginDir,
                    "repository backup source");
                var sourceIdentity = PluginInstallationService.CaptureExistingPathIdentities(
                    pluginsRoot,
                    pluginDir,
                    "repository backup source");
                _repositoryMutationBoundary(pluginDir);
                PluginInstallationService.VerifyPathIdentities(
                    sourceIdentity,
                    "repository backup source");
                PluginInstallationService.ValidateOwnedTransactionPath(
                    pluginsRoot,
                    pluginDir,
                    "repository backup source");
                _moveDirectory(pluginDir, backupDir);
                PluginInstallationService.ValidateOwnedTransactionPath(
                    transactionDir,
                    backupDir,
                    "repository isolated backup");
                transaction.MarkBackupCreated();
            }
            catch
            {
                if (Directory.Exists(transactionDir))
                {
                    try
                    {
                        PluginInstallationService.ValidateOwnedTransactionPath(
                            transactionRoot,
                            transactionDir,
                            "failed repository transaction cleanup");
                        var transactionIdentity =
                            PluginInstallationService.CaptureExistingPathIdentities(
                                transactionRoot,
                                transactionDir,
                                "failed repository transaction cleanup");
                        _repositoryMutationBoundary(transactionDir);
                        PluginInstallationService.VerifyPathIdentities(
                            transactionIdentity,
                            "failed repository transaction cleanup");
                        _deleteDirectory(transactionDir);
                    }
                    catch (Exception cleanupFailure)
                    {
                        Log.Instance.TraceOnce(
                            "plugin-repo-failed-transaction-cleanup",
                            $"Failed repository transaction directory was retained: {transactionDir}",
                            cleanupFailure);
                    }
                }
                throw;
            }
        }

        try
        {
            var stagedDirectory = Path.Combine(transactionDir, "replacement");
            Directory.CreateDirectory(stagedDirectory);
            PluginInstallationService.ValidateOwnedTransactionPath(
                transactionDir,
                stagedDirectory,
                "repository staged replacement");

            foreach (var file in Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories))
            {
                if (ShouldSkipPluginPayloadFile(file))
                    continue;

                var fileInfo = new FileInfo(file);
                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    throw new InvalidDataException($"Repository payload contains a reparse point: {file}");

                var relativePath = Path.GetRelativePath(extractPath, file);
                var destPath = Path.GetFullPath(Path.Combine(stagedDirectory, relativePath));
                if (!PathSecurity.IsPathWithinAllowedDirectory(destPath, stagedDirectory))
                    throw new UnauthorizedAccessException($"Repository payload escapes target: {relativePath}");

                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(file, destPath, overwrite: true);
            }

            EnsureInstalledManifest(stagedDirectory, manifest);
            TryStageCanonicalPluginSharedAssembly(stagedDirectory);
            TryStageCanonicalPluginSdkAssembly(stagedDirectory);
            if (FindPluginMainDll(stagedDirectory, manifest.Id) is null)
                throw new InvalidDataException($"Repository payload for {manifest.Id} has no canonical main assembly.");

            var stagedIdentity = PluginInstallationService.CaptureExistingPathIdentities(
                transactionDir,
                stagedDirectory,
                "repository staged replacement");
            _repositoryMutationBoundary(stagedDirectory);
            PluginInstallationService.VerifyPathIdentities(
                stagedIdentity,
                "repository staged replacement");
            PluginInstallationService.ValidateOwnedTransactionPath(
                transactionDir,
                stagedDirectory,
                "repository staged replacement");
            PluginInstallationService.ValidateOwnedTransactionPath(
                pluginsRoot,
                pluginDir,
                "repository replacement target");
            _moveDirectory(stagedDirectory, pluginDir);
            PluginInstallationService.ValidateOwnedTransactionPath(
                pluginsRoot,
                pluginDir,
                "repository installed replacement");
            if (trustAsOfficialOnlinePackage)
            {
                transaction.SetPackageAuthorization(
                    TrustedPluginPackageStore.CreateAuthorization(
                        manifest.Id,
                        pluginDir));
            }

            transaction.SetInstalledMainDll(
                FindPluginMainDll(pluginDir, manifest.Id)
                ?? throw new InvalidDataException($"Installed repository payload for {manifest.Id} has no canonical main assembly."));
            return transaction;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private sealed class RepositoryInstallationTransaction
    {
        private bool _backupCreated;
        private bool _completed;
        private bool _retained;
        private bool _trustFinalized;
        private string? _backupFingerprint;
        private readonly Action<string, string> _moveDirectory;
        private readonly Action<string> _deleteDirectory;
        private readonly Action<string> _mutationBoundary;
        private readonly string _pluginsRoot;

        public RepositoryInstallationTransaction(
            string targetDirectory,
            string transactionDirectory,
            string backupDirectory,
            string pluginId,
            string? originalTrustRecord,
            Action<string, string> moveDirectory,
            Action<string> deleteDirectory,
            Action<string> mutationBoundary,
            string pluginsRoot)
        {
            TargetDirectory = targetDirectory;
            TransactionDirectory = transactionDirectory;
            BackupDirectory = backupDirectory;
            PluginId = pluginId;
            OriginalTrustRecord = originalTrustRecord;
            _moveDirectory = moveDirectory;
            _deleteDirectory = deleteDirectory;
            _mutationBoundary = mutationBoundary;
            _pluginsRoot = pluginsRoot;
        }

        public string TargetDirectory { get; }
        public string TransactionDirectory { get; }
        public string BackupDirectory { get; }
        public string PluginId { get; }
        public string? OriginalTrustRecord { get; }
        public string InstalledMainDll { get; private set; } = string.Empty;
        public PluginPackageAuthorization? PackageAuthorization { get; private set; }

        public void MarkBackupCreated()
        {
            _backupCreated = true;
            _backupFingerprint =
                PluginInstallationService.ComputeDirectoryFingerprint(BackupDirectory);
        }
        public void SetInstalledMainDll(string path) => InstalledMainDll = path;
        public void SetPackageAuthorization(PluginPackageAuthorization authorization) =>
            PackageAuthorization = authorization;
        public void RetainRecoveryMaterial() => _retained = true;

        public void CommitTrust()
        {
            if (PackageAuthorization is not null)
            {
                if (PluginInstallationCommitCoordinator.IsWriteLockHeld)
                {
                    TrustedPluginPackageStore.PublishAuthorizationStrictUnderCommitLease(
                        PackageAuthorization);
                }
                else
                {
                    TrustedPluginPackageStore.PublishAuthorizationStrict(PackageAuthorization);
                }
            }
            else
            {
                if (PluginInstallationCommitCoordinator.IsWriteLockHeld)
                    TrustedPluginPackageStore.RemoveStrictUnderCommitLease(PluginId);
                else
                    TrustedPluginPackageStore.RemoveStrict(PluginId);
            }
            _trustFinalized = true;
        }

        public void Commit()
        {
            if (!_trustFinalized)
                throw new InvalidOperationException("Repository trust was not finalized before commit.");

            _completed = true;
            try
            {
                if (Directory.Exists(BackupDirectory))
                {
                    ValidateBeforeMutation(TransactionDirectory, BackupDirectory, "repository committed backup cleanup");
                    _deleteDirectory(BackupDirectory);
                }
                if (Directory.Exists(TransactionDirectory))
                {
                    ValidateBeforeMutation(
                        Path.GetDirectoryName(TransactionDirectory)!,
                        TransactionDirectory,
                        "repository committed transaction cleanup");
                    _deleteDirectory(TransactionDirectory);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.TraceOnce(
                    "plugin-repo-transaction-cleanup",
                    $"Committed repository transaction cleanup was deferred: {TransactionDirectory}",
                    ex);
            }
        }

        public void Rollback()
        {
            if (PackageAuthorization?.IsActive == true)
                PackageAuthorization.Close();
            if (_completed)
                return;
            if (_retained)
            {
                throw new InvalidOperationException(
                    $"Repository recovery material is retained because runtime unload is unconfirmed: {TransactionDirectory}");
            }

            var failedReplacement = Path.Combine(TransactionDirectory, "failed-replacement");
            try
            {
                if (_backupCreated && Directory.Exists(BackupDirectory))
                {
                    ValidateBeforeMutation(
                        TransactionDirectory,
                        BackupDirectory,
                        "repository rollback backup");
                    var currentBackupFingerprint =
                        PluginInstallationService.ComputeDirectoryFingerprint(BackupDirectory);
                    if (string.IsNullOrWhiteSpace(_backupFingerprint) ||
                        !string.Equals(
                            currentBackupFingerprint,
                            _backupFingerprint,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"Repository rollback backup changed after it was created: {BackupDirectory}");
                    }
                }

                if (Directory.Exists(TargetDirectory))
                {
                    ValidateBeforeMutation(_pluginsRoot, TargetDirectory, "repository rollback replacement");
                    _moveDirectory(TargetDirectory, failedReplacement);
                    PluginInstallationService.ValidateOwnedTransactionPath(
                        TransactionDirectory,
                        failedReplacement,
                        "repository retained failed replacement");
                }
                if (_backupCreated && Directory.Exists(BackupDirectory))
                {
                    ValidateBeforeMutation(TransactionDirectory, BackupDirectory, "repository rollback backup");
                    _moveDirectory(BackupDirectory, TargetDirectory);
                    PluginInstallationService.ValidateOwnedTransactionPath(
                        _pluginsRoot,
                        TargetDirectory,
                        "repository restored target");
                }

                if (_trustFinalized)
                {
                    TrustedPluginPackageStore.RestoreExactTrustRecord(
                        PluginId,
                        OriginalTrustRecord);
                }

                if (Directory.Exists(failedReplacement))
                {
                    ValidateBeforeMutation(TransactionDirectory, failedReplacement, "repository failed replacement cleanup");
                    _deleteDirectory(failedReplacement);
                }
                if (Directory.Exists(TransactionDirectory))
                {
                    ValidateBeforeMutation(
                        Path.GetDirectoryName(TransactionDirectory)!,
                        TransactionDirectory,
                        "repository rollback transaction cleanup");
                    _deleteDirectory(TransactionDirectory);
                }
                _completed = true;
            }
            catch (Exception ex)
            {
                throw new AggregateException(
                    $"Repository installation rollback is incomplete. Recovery material: {TransactionDirectory}",
                    ex);
            }
        }

        private void ValidateBeforeMutation(string root, string path, string description)
        {
            PluginInstallationService.ValidateOwnedTransactionPath(root, path, description);
            var identities = PluginInstallationService.CaptureExistingPathIdentities(
                root,
                path,
                description);
            _mutationBoundary(path);
            PluginInstallationService.VerifyPathIdentities(identities, description);
            PluginInstallationService.ValidateOwnedTransactionPath(root, path, description);
        }
    }
}
