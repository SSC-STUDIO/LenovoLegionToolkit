using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using UniversalDeviceToolkit.Lib.PackageDownloader;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Controls.Packages;

// TODO(Phase 4e): portable extraction into UniversalDeviceToolkit.ViewModels is
// blocked by the Lib-bound Package struct (UniversalDeviceToolkit.Lib.Structs)
// and WPF/Lib UI services (SnackbarHelper, InstallerLaunchPathValidator,
// localized Resource strings). This VM owns the full download/install state
// machine; PackageControl code-behind only reacts to VM events for visuals.
public sealed class PackageControlViewModel : IProgress<float>, IDisposable
{
    public enum SnackbarKind
    {
        Success,
        Warning,
        Error
    }

    public sealed class SnackbarRequestedEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public SnackbarKind Kind { get; }

        public SnackbarRequestedEventArgs(string title, string message, SnackbarKind kind)
        {
            Title = title;
            Message = message;
            Kind = kind;
        }
    }

    private readonly IPackageDownloader _packageDownloader;
    private readonly Func<string> _getDownloadPath;
    private readonly Dispatcher _dispatcher;

    private CancellationTokenSource? _downloadPackageTokenSource;
    private bool _isSelected;
    private bool _isDownloading;
    private bool _isRecommended;
    private PackageControl.PackageStatus _status = PackageControl.PackageStatus.NotStarted;
    private Process? _installProcess; // NOTE: Cross-method process reference — disposal must be handled at a higher level (see StopInstallation)
    private string? _actualDownloadedFilePath; // Store the actual downloaded file path

    public PackageControlViewModel(IPackageDownloader packageDownloader, Package package, Func<string> getDownloadPath)
    {
        _packageDownloader = packageDownloader;
        Package = package;
        _getDownloadPath = getDownloadPath;
        // Controls are always constructed on the UI thread; capturing the dispatcher
        // keeps every event delivery (and Report) on the UI thread, matching the
        // original code-behind semantics.
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    public Package Package { get; }

    public bool AutoStartOnSelection { get; set; } = true;

    public bool HideWhenCompleted { get; set; } = true;

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (_isDownloading == value)
                return;

            _isDownloading = value;
            OnPropertyChanged();
        }
    }

    public PackageControl.PackageStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompleted));
            StatusChanged?.Invoke(this, value);
        }
    }

    public bool IsCompleted => Status == PackageControl.PackageStatus.Completed;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            if (!AutoStartOnSelection)
            {
                if (_isSelected && Status == PackageControl.PackageStatus.NotStarted)
                    Status = PackageControl.PackageStatus.Queued;
                else if (!_isSelected && Status == PackageControl.PackageStatus.Queued)
                    Status = PackageControl.PackageStatus.NotStarted;
                else
                    StatusRefreshRequested?.Invoke(this, EventArgs.Empty);
            }

            OnPropertyChanged();
        }
    }

    public bool IsRecommended
    {
        get => _isRecommended;
        private set
        {
            if (_isRecommended == value)
                return;
            _isRecommended = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Raised on the UI thread whenever the download/install status changes.</summary>
    public event EventHandler<PackageControl.PackageStatus>? StatusChanged;

    /// <summary>Raised when the queued/not-started display mapping should be re-evaluated.</summary>
    public event EventHandler? StatusRefreshRequested;

    /// <summary>Raised with progress percent (0-1) for the progress ring.</summary>
    public event EventHandler<float>? ProgressChanged;

    /// <summary>Raised when a snackbar (toast) should be shown.</summary>
    public event EventHandler<SnackbarRequestedEventArgs>? SnackbarRequested;

    /// <summary>Raised when the download/install button icon and action should be re-evaluated.</summary>
    public event EventHandler? DownloadButtonStateChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal void Initialize(bool isRecommended)
    {
        IsRecommended = isRecommended;
        CheckDownloadedFileState();
    }

    /// <summary>
    /// True when the downloaded installer file already exists on disk (button becomes Install).
    /// </summary>
    public bool DownloadedFileExists { get; private set; }

    /// <summary>
    /// Get the actual downloaded file name (including title prefix)
    /// </summary>
    private string GetActualFileName()
    {
        // Actual downloaded file name format: "{SanitizedTitle} - {FileName}"
        var sanitizedTitle = SanitizeFileName(Package.Title);
        return $"{sanitizedTitle} - {GetSafePackageFileName()}";
    }

    private string GetSafePackageFileName() => SanitizeFileName(Path.GetFileName(Package.FileName));

    private string? FindDownloadedPackagePath(string downloadPath)
    {
        if (!Directory.Exists(downloadPath))
            return null;

        var expectedName = GetActualFileName();
        foreach (var candidate in Directory.EnumerateFiles(downloadPath, "*", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(Path.GetFileName(candidate), expectedName, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Clean up invalid characters from file name
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
        return Regex.Replace(name, invalidRegStr, "_");
    }

    internal void CheckDownloadedFileState()
    {
        try
        {
            var downloadPath = _getDownloadPath();
            var filePath = _actualDownloadedFilePath;

            // If actual path doesn't exist, try constructed path
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                filePath = Path.Combine(downloadPath, GetActualFileName());
            }

            // If still not found, try searching for matching files
            if (!File.Exists(filePath) && Directory.Exists(downloadPath))
            {
                var locatedPath = FindDownloadedPackagePath(downloadPath);
                if (!string.IsNullOrEmpty(locatedPath))
                {
                    filePath = locatedPath;
                    _actualDownloadedFilePath = filePath;
                }
            }

            DownloadedFileExists = File.Exists(filePath);
            DownloadButtonStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to check download button state.", ex);
        }
    }

    public void Dispose()
    {
        _downloadPackageTokenSource?.Dispose();
        _downloadPackageTokenSource = null;

        _installProcess?.Dispose();
        _installProcess = null;
    }

    public async Task StartAsync()
    {
        if (Status == PackageControl.PackageStatus.Completed || Status == PackageControl.PackageStatus.Downloading || Status == PackageControl.PackageStatus.Installing)
            return;

        // Check if file is already downloaded
        var downloadPath = _getDownloadPath();
        var filePath = _actualDownloadedFilePath;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            filePath = Path.Combine(downloadPath, GetActualFileName());
        }

        if (!File.Exists(filePath) && Directory.Exists(downloadPath))
        {
            var locatedPath = FindDownloadedPackagePath(downloadPath);
            if (!string.IsNullOrEmpty(locatedPath))
            {
                filePath = locatedPath;
                _actualDownloadedFilePath = filePath;
            }
        }

        if (File.Exists(filePath))
        {
            await InstallPackageAsync();
        }
        else
        {
            await DownloadAndInstallPackageAsync();
        }
    }

    public void Pause()
    {
        if (Status == PackageControl.PackageStatus.Downloading)
        {
            _downloadPackageTokenSource?.Cancel();
            ResetPendingStatus();
        }
        else if (Status == PackageControl.PackageStatus.Installing)
        {
            StopInstallation();
            ResetPendingStatus();
        }
    }

    public void CancelDownload() => _downloadPackageTokenSource?.Cancel();

    internal void ResetPendingStatus()
    {
        Status = !AutoStartOnSelection && IsSelected ? PackageControl.PackageStatus.Queued : PackageControl.PackageStatus.NotStarted;
    }

    private async Task DownloadAndInstallPackageAsync()
    {
        Status = PackageControl.PackageStatus.Downloading;
        IsDownloading = true;

        var result = false;

        try
        {
            if (_downloadPackageTokenSource is not null)
                await _downloadPackageTokenSource.CancelAsync();

            _downloadPackageTokenSource = new();

            var token = _downloadPackageTokenSource.Token;

            _actualDownloadedFilePath = await _packageDownloader.DownloadPackageFileAsync(Package, _getDownloadPath(), this, token);

            result = true;
        }
        catch (OperationCanceledException)
        {
            ResetPendingStatus();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Not found 404.", ex);

            RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_Http404Error_Title, UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_Http404Error_Message, SnackbarKind.Error);
            ResetPendingStatus();
        }
        catch (HttpRequestException ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading package file.", ex);

            RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_HttpGeneralError_Title, UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_HttpGeneralError_Message, SnackbarKind.Error);
            ResetPendingStatus();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading package file.", ex);

            RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_GeneralError_Title, ex.Message, SnackbarKind.Error);
            ResetPendingStatus();
        }
        finally
        {
            IsDownloading = false;
        }

        if (result)
        {
            RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_DownloadComplete_Title, string.Format(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_DownloadComplete_Message, Package.FileName), SnackbarKind.Success);
            CheckDownloadedFileState();

            // Use actual downloaded file path, if it doesn't exist use constructed path
            var filePath = _actualDownloadedFilePath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                var downloadPath = _getDownloadPath();
                filePath = Path.Combine(downloadPath, GetActualFileName());
            }

            // Wait for file to be fully written, maximum 3 seconds
            var maxRetries = 30; // Maximum 30 retries
            var retryDelay = 100; // 100ms delay between retries

            for (int i = 0; i < maxRetries; i++)
            {
                if (File.Exists(filePath))
                {
                    // Check if file is accessible (not being written)
                    try
                    {
                        using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            // File is accessible, can proceed with installation
                            break;
                        }
                    }
                    catch
                    {
                        // File is being written, continue waiting
                    }
                }

                if (i < maxRetries - 1)
                    await Task.Delay(retryDelay);
            }

            // Confirm file exists before installation
            if (File.Exists(filePath))
            {
                await InstallPackageAsync();
            }
            else
            {
                // If still not found, try searching for matching files in download directory
                var downloadPath = _getDownloadPath();
                if (Directory.Exists(downloadPath))
                {
                    var locatedPath = FindDownloadedPackagePath(downloadPath);
                    if (!string.IsNullOrEmpty(locatedPath))
                    {
                        // Found matching files, use the first one
                        filePath = locatedPath;
                        _actualDownloadedFilePath = filePath;

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found file by search: {filePath}");
                    }
                }

                if (File.Exists(filePath))
                {
                    await InstallPackageAsync();
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"File not found. Expected: {filePath}, Actual downloaded: {_actualDownloadedFilePath}");

                    RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_Title, UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_FileNotFound, SnackbarKind.Error);
                    ResetPendingStatus();
                }
            }
        }
    }

    private async Task InstallPackageAsync()
    {
        Status = PackageControl.PackageStatus.Installing;
        StatusRefreshRequested?.Invoke(this, EventArgs.Empty); // Update display state immediately
        try
        {
            // Prefer actual downloaded file path
            var filePath = _actualDownloadedFilePath;

            // If actual path doesn't exist, try constructed path
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                var downloadPath = _getDownloadPath();
                filePath = Path.Combine(downloadPath, GetActualFileName());
            }

            // Check file existence again, wait and retry if not found
            if (!File.Exists(filePath))
            {
                // Wait up to 2 seconds, checking every 200ms
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(200);
                    if (File.Exists(filePath))
                        break;
                }
            }

            // If still not found, try searching for matching files in download directory
            if (!File.Exists(filePath))
            {
                var downloadPath = _getDownloadPath();
                if (Directory.Exists(downloadPath))
                {
                    var locatedPath = FindDownloadedPackagePath(downloadPath);
                    if (!string.IsNullOrEmpty(locatedPath))
                    {
                        // Found matching files, use the first one
                        filePath = locatedPath;
                        _actualDownloadedFilePath = filePath;

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found file by search in InstallPackageAsync: {filePath}");
                    }
                }
            }

            if (!File.Exists(filePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"File not found in InstallPackageAsync. Expected: {filePath}, Actual downloaded: {_actualDownloadedFilePath}");

                RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_Title, UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_FileNotFound, SnackbarKind.Error);
                ResetPendingStatus();
                StatusRefreshRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            var expectedInstallerFileName = GetActualFileName();
            var configuredDownloadPath = _getDownloadPath();
            if (!InstallerLaunchPathValidator.TryValidateForExecution(filePath, configuredDownloadPath, expectedInstallerFileName, out var safeInstallerPath, out var validationError))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Installer path validation failed. [filePath={filePath}, downloadPath={configuredDownloadPath}, reason={validationError}]");

                RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_Title, validationError, SnackbarKind.Error);
                ResetPendingStatus();
                StatusRefreshRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Run installer
            var processStartInfo = new ProcessStartInfo
            {
                FileName = safeInstallerPath,
                UseShellExecute = true,
                Verb = "runas" // Run with admin privileges
            };

            try
            {
                var installProcess = Process.Start(processStartInfo);
                _installProcess = installProcess;

                if (installProcess != null)
                {
                    installProcess.EnableRaisingEvents = true;
                    installProcess.Exited += async (s, e) =>
                    {
                        await HandleInstallProcessExitAsync(installProcess);
                    };
                }

                RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallStarted_Title, string.Format(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallStarted_Message, Package.FileName), SnackbarKind.Success);

                // If process exits immediately, consider installation complete
                if (installProcess != null && installProcess.HasExited)
                    await HandleInstallProcessExitAsync(installProcess);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to start installer.", ex);
                RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_Title, ex.Message, SnackbarKind.Error);
                ResetPendingStatus();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when installing package.", ex);
            RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_Title, ex.Message, SnackbarKind.Error);
            ResetPendingStatus();
        }
    }

    private async Task HandleInstallProcessExitAsync(Process installProcess)
    {
        var exitCode = installProcess.ExitCode;
        var status = ResolveStatusForInstallerExitCode(exitCode);
        var failureMessage = GetInstallerExitFailureMessage(exitCode);

        try
        {
            var handled = await _dispatcher.InvokeAsync(() =>
            {
                if (!ReferenceEquals(_installProcess, installProcess))
                    return false;

                Status = status;
                StatusRefreshRequested?.Invoke(this, EventArgs.Empty);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Install process exited with code {exitCode}. Status set to {status}.");

                _installProcess = null;
                return true;
            });

            if (!handled)
                return;

            if (failureMessage is not null)
            {
                RaiseSnackbar(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_Title, failureMessage, SnackbarKind.Error);
            }
            else if (status == PackageControl.PackageStatus.Completed)
            {
                // Auto-closing success toast when installer finishes cleanly.
                RaiseSnackbar(
                    UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallStarted_Title,
                    string.Format(
                        UniversalDeviceToolkit.WPF.Resources.Resource.Culture ?? System.Globalization.CultureInfo.CurrentUICulture,
                        UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_DownloadComplete_Message,
                        Package.FileName),
                    SnackbarKind.Success);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating status after install exit.", ex);
        }
    }

    private PackageControl.PackageStatus ResolveStatusForInstallerExitCode(int exitCode)
    {
        var status = GetStatusForInstallerExitCode(exitCode);
        if (status == PackageControl.PackageStatus.NotStarted && !AutoStartOnSelection && IsSelected)
            return PackageControl.PackageStatus.Queued;

        return status;
    }

    internal static PackageControl.PackageStatus GetStatusForInstallerExitCode(int exitCode) => exitCode == 0
        ? PackageControl.PackageStatus.Completed
        : PackageControl.PackageStatus.NotStarted;

    internal static string? GetInstallerExitFailureMessage(int exitCode) => exitCode == 0
        ? null
        : string.Format(UniversalDeviceToolkit.WPF.Resources.Resource.PackageControl_InstallError_ExitCode, exitCode);

    internal void StopInstallation()
    {
        if (_installProcess != null && !_installProcess.HasExited)
        {
            try
            {
                _installProcess.Kill(true);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to stop installation process.", ex);
            }
            _installProcess = null;
        }
    }

    private void RaiseSnackbar(string title, string message, SnackbarKind kind) =>
        SnackbarRequested?.Invoke(this, new SnackbarRequestedEventArgs(title, message, kind));

    public void Report(float value) => _dispatcher.BeginInvoke(() =>
        ProgressChanged?.Invoke(this, value));
}
