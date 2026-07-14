using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Packages
{
public partial class PackageControl : IProgress<float>, IDisposable
{
    private readonly IPackageDownloader _packageDownloader;
    private readonly Package _package;
    private readonly Func<string> _getDownloadPath;

    private CancellationTokenSource? _downloadPackageTokenSource;
    private bool _isSelected;
    private bool _isDownloading;
    private bool _isRecommended;
    private PackageStatus _status = PackageStatus.NotStarted;
    private Process? _installProcess; // NOTE: Cross-method process reference �?disposal must be handled at a higher level (see StopInstallation)
    private string? _actualDownloadedFilePath; // Store the actual downloaded file path

    public enum PackageStatus
    {
        NotStarted,    // Not started
        Queued,        // Queued for later start
        Downloading,   // Downloading
        Installing,    // Installing
        Completed      // Completed
    }

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

    public PackageStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompleted));
            UpdateStatusDisplay();

            if (value == PackageStatus.Completed && HideWhenCompleted)
            {
                Visibility = Visibility.Collapsed;
            }
        }
    }

    public bool IsCompleted => Status == PackageStatus.Completed;

    /// <summary>
    /// Get the actual downloaded file name (including title prefix)
    /// </summary>
    private string GetActualFileName()
    {
        // Actual downloaded file name format: "{SanitizedTitle} - {FileName}"
        var sanitizedTitle = SanitizeFileName(_package.Title);
        return $"{sanitizedTitle} - {GetSafePackageFileName()}";
    }

    private string GetSafePackageFileName() => SanitizeFileName(Path.GetFileName(_package.FileName));

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
                if (_isSelected && Status == PackageStatus.NotStarted)
                    Status = PackageStatus.Queued;
                else if (!_isSelected && Status == PackageStatus.Queued)
                    Status = PackageStatus.NotStarted;
                else
                    UpdateStatusDisplay();
            }

            // Synchronize checkbox state in UI (avoid event loop)
            if (_selectCheckBox != null && _selectCheckBox.IsChecked != value)
            {
                // Temporarily remove event handlers to avoid triggering SelectCheckBox_Checked/Unchecked
                _selectCheckBox.Checked -= SelectCheckBox_Checked;
                _selectCheckBox.Unchecked -= SelectCheckBox_Unchecked;

                _selectCheckBox.IsChecked = value;

                // Re-add event handlers
                _selectCheckBox.Checked += SelectCheckBox_Checked;
                _selectCheckBox.Unchecked += SelectCheckBox_Unchecked;
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

    public Package Package => _package;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public PackageControl(IPackageDownloader packageDownloader, Package package, Func<string> getDownloadPath)
    {
        _packageDownloader = packageDownloader;
        _package = package;
        _getDownloadPath = getDownloadPath;

        InitializeComponent();

        Unloaded += PackageControl_Unloaded;

        _dateTextBlock.Text = package.ReleaseDate.ToString(LocalizationHelper.ShortDateFormat);
        _titleTextBlock.Text = package.Title;
        _descriptionTextBlock.Text = package.Description;
        _descriptionTextBlock.Visibility = string.IsNullOrWhiteSpace(package.Description) ? Visibility.Collapsed : Visibility.Visible;
        _categoryTextBlock.Text = package.Category;
        _detailTextBlock.Text = $"{Resource.PackageControl_Version} {package.Version}  |  {package.FileSize}  |  {package.FileName}";

        // Vantage packages often set Readme to "" when missing — hide unless a real URL exists.
        _readmeButton.Visibility = HasOpenableReadme(package.Readme) ? Visibility.Visible : Visibility.Collapsed;
        _updateRebootStackPanel.Visibility = _isUpdateStackPanel.Visibility = package.IsUpdate ? Visibility.Visible : Visibility.Collapsed;

        _rebootStackPanel.Visibility = package is { IsUpdate: true, Reboot: RebootType.Delayed or RebootType.Requested or RebootType.Forced or RebootType.ForcedPowerOff }
            ? Visibility.Visible
            : Visibility.Collapsed;
        _rebootTextBlock.Text = package.Reboot switch
        {
            RebootType.Delayed or RebootType.Requested => Resource.PackageControl_RebootRecommended,
            RebootType.Forced => Resource.PackageControl_RebootRequired,
            RebootType.ForcedPowerOff => Resource.PackageControl_ShutdownRequired,
            _ => string.Empty
        };

        var showWarning = package.ReleaseDate < DateTime.UtcNow.AddYears(-1);
        _warningTextBlock.Visibility = showWarning ? Visibility.Visible : Visibility.Collapsed;

        // Mark as recommended if it's an update
        IsRecommended = package.IsUpdate;

        // Bind checkbox
        if (_selectCheckBox != null)
        {
            _selectCheckBox.IsChecked = IsSelected;
            _selectCheckBox.Checked += SelectCheckBox_Checked;
            _selectCheckBox.Unchecked += SelectCheckBox_Unchecked;
        }

        // Initialize recommended badge and status display
        if (_recommendedBadge != null)
        {
            // Ensure recommended badge has content
            if (string.IsNullOrEmpty(_recommendedBadge.Content?.ToString()))
            {
                _recommendedBadge.Content = Resource.PackageControl_Recommended;
            }
            _recommendedBadge.Visibility = IsRecommended && Status != PackageStatus.Completed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Initialize status display (must be after recommended badge, as UpdateStatusDisplay may modify badge visibility)
        UpdateStatusDisplay();

        // Check if file is already downloaded, switch download button to install button if it is
        CheckAndUpdateDownloadButtonState();
    }

    private void CheckAndUpdateDownloadButtonState()
    {
        try
        {
            if (_downloadButton == null)
                return;

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

            var fileExists = File.Exists(filePath);

            if (fileExists)
            {
            // File already exists, switch download button to install button
            // Check current icon to avoid duplicate switching
                if (_downloadButton.Icon is SymbolIcon { Symbol: SymbolRegular.Play24 })
                return; // Already install button, no switch needed

                _downloadButton.Click -= DownloadButton_Click;
                _downloadButton.Click += InstallButton_Click;

                _downloadButton.Icon = CreateActionIcon(SymbolRegular.Play24);
                _downloadButton.ToolTip = Resource.PackageControl_Install;
            }
            else
            {
                // File is not present, keep the button in download mode.
                if (_downloadButton.Icon is SymbolIcon { Symbol: SymbolRegular.ArrowDownload24 })
                return; // Already download button, no switch needed

                _downloadButton.Click -= InstallButton_Click;
        _downloadButton.Click -= DownloadButton_Click; // Remove first to avoid duplicate binding
                _downloadButton.Click += DownloadButton_Click;

                _downloadButton.Icon = CreateActionIcon(SymbolRegular.ArrowDownload24);
                _downloadButton.ToolTip = Resource.PackageControl_Download;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to check download button state.", ex);
        }
    }

    private static SymbolIcon CreateActionIcon(SymbolRegular symbol) => new()
    {
        Symbol = symbol,
        Foreground = Application.Current.TryFindResource("TextFillColorPrimaryBrush") as Brush
            ?? Brushes.White,
    };

    private void PackageControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _downloadPackageTokenSource?.Cancel();
        StopInstallation();

        Dispose();
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
        if (Status == PackageStatus.Completed || Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
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
        if (Status == PackageStatus.Downloading)
        {
            _downloadPackageTokenSource?.Cancel();
            ResetPendingStatus();
        }
        else if (Status == PackageStatus.Installing)
        {
            StopInstallation();
            ResetPendingStatus();
        }
    }

    private async void SelectCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            IsSelected = true;

            // If already completed, do nothing
            if (Status == PackageStatus.Completed)
                return;

            // If already downloading or installing, don't repeat
            if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
                return;

            if (!AutoStartOnSelection)
                return;

            await StartAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(SelectCheckBox_Checked)}.", ex);
        }
    }

    private void SelectCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        IsSelected = false;

        // If already completed, cancellation not allowed
        if (Status == PackageStatus.Completed)
        {
            // Re-select the checkbox
            if (_selectCheckBox != null)
                _selectCheckBox.IsChecked = true;
            return;
        }

        // Stop download or installation
        if (Status == PackageStatus.Downloading)
        {
            _downloadPackageTokenSource?.Cancel();
            ResetPendingStatus();
        }
        else if (Status == PackageStatus.Installing)
        {
            StopInstallation();
            ResetPendingStatus();
        }
        else if (Status == PackageStatus.Queued)
        {
            Status = PackageStatus.NotStarted;
        }
    }

    private void ResetPendingStatus()
    {
        Status = !AutoStartOnSelection && IsSelected ? PackageStatus.Queued : PackageStatus.NotStarted;
    }

    private async Task DownloadAndInstallPackageAsync()
    {
        Status = PackageStatus.Downloading;
        IsDownloading = true;

        var result = false;

        try
        {
            _idleStackPanel.Visibility = Visibility.Collapsed;
            _downloadingStackPanel.Visibility = Visibility.Visible;

            if (_downloadPackageTokenSource is not null)
                await _downloadPackageTokenSource.CancelAsync();

            _downloadPackageTokenSource = new();

            var token = _downloadPackageTokenSource.Token;

            _actualDownloadedFilePath = await _packageDownloader.DownloadPackageFileAsync(_package, _getDownloadPath(), this, token);

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

            await SnackbarHelper.ShowAsync(Resource.PackageControl_Http404Error_Title, Resource.PackageControl_Http404Error_Message, SnackbarType.Error);
            ResetPendingStatus();
        }
        catch (HttpRequestException ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading package file.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackageControl_HttpGeneralError_Title, Resource.PackageControl_HttpGeneralError_Message, SnackbarType.Error);
            ResetPendingStatus();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading package file.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackageControl_GeneralError_Title, ex.Message, SnackbarType.Error);
            ResetPendingStatus();
        }
        finally
        {
            _idleStackPanel.Visibility = Visibility.Visible;
            _downloadingStackPanel.Visibility = Visibility.Collapsed;
            _downloadProgressRing.Progress = 0;
            _downloadProgressLabel.Content = null;

            IsDownloading = false;
        }

        if (result)
        {
            await SnackbarHelper.ShowAsync(Resource.PackageControl_DownloadComplete_Title, string.Format(Resource.PackageControl_DownloadComplete_Message, _package.FileName));
            CheckAndUpdateDownloadButtonState();

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

                    await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, Resource.PackageControl_InstallError_FileNotFound, SnackbarType.Error);
                    ResetPendingStatus();
                }
            }
        }
    }

    private async Task InstallPackageAsync()
    {
        Status = PackageStatus.Installing;
        UpdateStatusDisplay(); // Update display state immediately
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

                await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, Resource.PackageControl_InstallError_FileNotFound, SnackbarType.Error);
                ResetPendingStatus();
                UpdateStatusDisplay();
                return;
            }

            var expectedInstallerFileName = GetActualFileName();
            var configuredDownloadPath = _getDownloadPath();
            if (!InstallerLaunchPathValidator.TryValidateForExecution(filePath, configuredDownloadPath, expectedInstallerFileName, out var safeInstallerPath, out var validationError))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Installer path validation failed. [filePath={filePath}, downloadPath={configuredDownloadPath}, reason={validationError}]");

                await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, validationError, SnackbarType.Error);
                ResetPendingStatus();
                UpdateStatusDisplay();
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

                await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallStarted_Title, string.Format(Resource.PackageControl_InstallStarted_Message, _package.FileName), SnackbarType.Success);

        // If process exits immediately, consider installation complete
                if (installProcess != null && installProcess.HasExited)
                    await HandleInstallProcessExitAsync(installProcess);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to start installer.", ex);
                await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, ex.Message, SnackbarType.Error);
                ResetPendingStatus();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when installing package.", ex);
            await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, ex.Message, SnackbarType.Error);
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
            var handled = await Dispatcher.InvokeAsync(() =>
            {
                if (!ReferenceEquals(_installProcess, installProcess))
                    return false;

                Status = status;
                UpdateStatusDisplay();

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Install process exited with code {exitCode}. Status set to {status}.");

                _installProcess = null;
                return true;
            });

            if (!handled)
                return;

            if (failureMessage is not null)
            {
                await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, failureMessage, SnackbarType.Error);
            }
            else if (status == PackageStatus.Completed)
            {
                // Auto-closing success toast when installer finishes cleanly.
                await SnackbarHelper.ShowAsync(
                    Resource.PackageControl_InstallStarted_Title,
                    string.Format(
                        Resource.Culture ?? System.Globalization.CultureInfo.CurrentUICulture,
                        Resource.PackageControl_DownloadComplete_Message,
                        _package.FileName),
                    SnackbarType.Success);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating status after install exit.", ex);
        }
    }

    private PackageStatus ResolveStatusForInstallerExitCode(int exitCode)
    {
        var status = GetStatusForInstallerExitCode(exitCode);
        if (status == PackageStatus.NotStarted && !AutoStartOnSelection && IsSelected)
            return PackageStatus.Queued;

        return status;
    }

    private static PackageStatus GetStatusForInstallerExitCode(int exitCode) => exitCode == 0
        ? PackageStatus.Completed
        : PackageStatus.NotStarted;

    private static string? GetInstallerExitFailureMessage(int exitCode) => exitCode == 0
        ? null
        : string.Format(Resource.PackageControl_InstallError_ExitCode, exitCode);

    private void StopInstallation()
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

    private void UpdateStatusDisplay()
    {
        if (_statusBadge is not null && _statusTextBlock is not null && _statusIcon is not null)
        {
            var status = Status == PackageStatus.NotStarted && !AutoStartOnSelection && IsSelected
                ? PackageStatus.Queued
                : Status;

            var statusText = GetStatusText(status);
            _statusBadge.Visibility = string.IsNullOrEmpty(statusText) ? Visibility.Collapsed : Visibility.Visible;
            _statusTextBlock.Text = statusText;
            _statusIcon.Symbol = status switch
            {
                PackageStatus.Queued => SymbolRegular.Clock24,
                PackageStatus.Downloading => SymbolRegular.ArrowDownload24,
                PackageStatus.Installing => SymbolRegular.Play24,
                PackageStatus.Completed => SymbolRegular.CheckmarkCircle24,
                _ => SymbolRegular.Empty
            };
        }

        if (_recommendedBadge is null)
            return;

        if (Status == PackageStatus.Completed)
            _recommendedBadge.Visibility = Visibility.Collapsed;
        else if (IsRecommended)
        {
            if (string.IsNullOrEmpty(_recommendedBadge.Content?.ToString()))
                _recommendedBadge.Content = Resource.PackageControl_Recommended;

            _recommendedBadge.Visibility = Visibility.Visible;
        }
        else
        {
            _recommendedBadge.Visibility = Visibility.Collapsed;
        }
    }

    private static string GetStatusText(PackageStatus status) => status switch
    {
        PackageStatus.Queued => LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "PackageControl_Queued",
            "Queued",
            Resource.Culture ?? CultureInfo.CurrentUICulture),
        PackageStatus.Downloading => Resource.PackageControl_Downloading,
        PackageStatus.Installing => Resource.PackageControl_Installing,
        PackageStatus.Completed => Resource.PackageControl_Completed,
        _ => string.Empty
    };

    public void Report(float value) => Dispatcher.BeginInvoke(() =>
    {
        _downloadProgressRing.IsIndeterminate = !(value > 0);
        _downloadProgressRing.Progress = value * 100;
        _downloadProgressLabel.Content = $"{value * 100:0}%";
    });

    private async void CopyToClipboard_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (sender is not TextBlock tb)
            return;

        var str = tb.Text;

        try
        {
            System.Windows.Clipboard.SetText(str);
            await SnackbarHelper.ShowAsync(Resource.CopiedToClipboard_Title, string.Format(Resource.CopiedToClipboard_Message_WithParam, str));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't copy to clipboard", ex);
        }
    }

    private void ReadmeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCreateHttpUri(_package.Readme, out var uri) || uri is null)
            return;

        try
        {
            uri.Open();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't open package readme. [readme={_package.Readme}]", ex);
        }
    }

    private static bool HasOpenableReadme(string? readme) => TryCreateHttpUri(readme, out _);

    private static bool TryCreateHttpUri(string? value, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var created) || created is null)
            return false;

        if (!string.Equals(created.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(created.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        uri = created;
        return true;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Status == PackageStatus.Completed)
                return;

            if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
                return;

            await DownloadAndInstallPackageAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(DownloadButton_Click)}.", ex);
        }
    }

    private void CancelDownloadButton_Click(object sender, RoutedEventArgs e) => _downloadPackageTokenSource?.Cancel();

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Status == PackageStatus.Completed)
                return;

            if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
                return;

            await InstallPackageAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(InstallButton_Click)}.", ex);
        }
    }
}
}