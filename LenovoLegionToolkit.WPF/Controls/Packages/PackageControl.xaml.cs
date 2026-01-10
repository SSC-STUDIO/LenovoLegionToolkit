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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Controls.Packages;

public partial class PackageControl : IProgress<float>
{
    private readonly IPackageDownloader _packageDownloader;
    private readonly Package _package;
    private readonly Func<string> _getDownloadPath;

    private CancellationTokenSource? _downloadPackageTokenSource;
    private bool _isSelected;
    private bool _isRecommended;
    private PackageStatus _status = PackageStatus.NotStarted;
    private Process? _installProcess;
    private string? _actualDownloadedFilePath; // Store the actual downloaded file path

    public enum PackageStatus
    {
        NotStarted,    // Not started
        Downloading,   // Downloading
        Installing,    // Installing
        Completed      // Completed
    }

    public bool IsDownloading { get; private set; }

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
            
            // If status changes to Completed, hide the control (in the main interface)
            if (value == PackageStatus.Completed)
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
        return $"{sanitizedTitle} - {_package.FileName}";
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

        _readmeButton.Visibility = package.Readme is null ? Visibility.Collapsed : Visibility.Visible;
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
                var files = Directory.GetFiles(downloadPath, $"*{_package.FileName}");
                if (files.Length > 0)
                {
                    filePath = files[0];
                    _actualDownloadedFilePath = filePath;
                }
            }
            
            var fileExists = File.Exists(filePath);
            
            if (fileExists)
            {
                // 文件已存在，将下载按钮切换为安装按钮
                // 检查当前图标，避免重复切换
                if (_downloadButton.Icon is SymbolRegular icon && icon == SymbolRegular.Play24)
                    return; // 已经是安装按钮，无需切换
                    
                // 移除下载事件，添加安装事件
                _downloadButton.Click -= DownloadButton_Click;
                _downloadButton.Click += InstallButton_Click;
                
                _downloadButton.Icon = SymbolRegular.Play24;
                _downloadButton.ToolTip = Resource.PackageControl_Install;
            }
            else
            {
                // 文件不存在，确保按钮是下载按钮
                // 检查当前图标，避免重复切换
                if (_downloadButton.Icon is SymbolRegular icon && icon == SymbolRegular.ArrowDownload24)
                    return; // 已经是下载按钮，无需切换
                    
                // 移除安装事件，重新添加下载事件
                _downloadButton.Click -= InstallButton_Click;
                _downloadButton.Click -= DownloadButton_Click; // 先移除，避免重复绑定
                _downloadButton.Click += DownloadButton_Click;
                
                _downloadButton.Icon = SymbolRegular.ArrowDownload24;
                _downloadButton.ToolTip = Resource.PackageControl_Download;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to check download button state.", ex);
        }
    }

    private void PackageControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _downloadPackageTokenSource?.Cancel();
        StopInstallation();
    }

    private async void SelectCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        IsSelected = true;
        
        // If already completed, do nothing
        if (Status == PackageStatus.Completed)
            return;
        
        // If already downloading or installing, don't repeat
        if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
            return;
        
        // Check if file is already downloaded
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
            var files = Directory.GetFiles(downloadPath, $"*{_package.FileName}");
            if (files.Length > 0)
            {
                filePath = files[0];
                _actualDownloadedFilePath = filePath;
            }
        }
        
        if (File.Exists(filePath))
        {
            // File exists, install directly
            await InstallPackageAsync();
        }
        else
        {
            // File doesn't exist, download first then install
            await DownloadAndInstallPackageAsync();
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
            Status = PackageStatus.NotStarted;
        }
        else if (Status == PackageStatus.Installing)
        {
            StopInstallation();
            Status = PackageStatus.NotStarted;
        }
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

            // Save the actual downloaded file path
        _actualDownloadedFilePath = await _packageDownloader.DownloadPackageFileAsync(_package, _getDownloadPath(), this, token);

            result = true;
        }
        catch (OperationCanceledException)
        {
            Status = PackageStatus.NotStarted;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Not found 404.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackageControl_Http404Error_Title, Resource.PackageControl_Http404Error_Message, SnackbarType.Error);
            Status = PackageStatus.NotStarted;
        }
        catch (HttpRequestException ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading package file.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackageControl_HttpGeneralError_Title, Resource.PackageControl_HttpGeneralError_Message, SnackbarType.Error);
            Status = PackageStatus.NotStarted;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading package file.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackageControl_GeneralError_Title, ex.Message, SnackbarType.Error);
            Status = PackageStatus.NotStarted;
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
                    var files = Directory.GetFiles(downloadPath, $"*{_package.FileName}");
                    if (files.Length > 0)
                    {
                        // Found matching files, use the first one
                        filePath = files[0];
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
                    Status = PackageStatus.NotStarted;
                }
            }
        }
    }

    private async Task InstallPackageAsync()
    {
        Status = PackageStatus.Installing;
        UpdateStatusDisplay(); // 立即更新显示状态
        
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
                var files = Directory.GetFiles(downloadPath, $"*{_package.FileName}");
                if (files.Length > 0)
                {
                    // Found matching files, use the first one
                    filePath = files[0];
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
                Status = PackageStatus.NotStarted;
                UpdateStatusDisplay(); // 更新状态显示
                return;
            }
            
            // 运行安装程序
            var processStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                Verb = "runas" // 以管理员权限运行
            };
            
            try
            {
                _installProcess = Process.Start(processStartInfo);
                if (_installProcess != null)
                {
                    _installProcess.EnableRaisingEvents = true;
                    _installProcess.Exited += async (s, e) =>
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                Status = PackageStatus.Completed;
                                UpdateStatusDisplay();
                                
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Install process exited. Status set to Completed. Badge should be visible.");
                                
                                _installProcess = null;
                            }
                            catch (Exception ex)
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Error updating status after install exit.", ex);
                            }
                        });
                    };
                }
                
                await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallStarted_Title, string.Format(Resource.PackageControl_InstallStarted_Message, _package.FileName), SnackbarType.Success);
                
                // 如果进程立即退出，认为安装完成
                if (_installProcess != null && _installProcess.HasExited)
                {
                    Status = PackageStatus.Completed;
                    UpdateStatusDisplay();
                    
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Install process exited immediately. Status set to Completed.");
                    
                    _installProcess = null;
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace((FormattableString)$"Failed to start installer.", ex);
                await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, ex.Message, SnackbarType.Error);
                Status = PackageStatus.NotStarted;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Error occurred when installing package.", ex);
            await SnackbarHelper.ShowAsync(Resource.PackageControl_InstallError_Title, ex.Message, SnackbarType.Error);
            Status = PackageStatus.NotStarted;
        }
    }

    private void StopInstallation()
    {
        if (_installProcess != null && !_installProcess.HasExited)
        {
            try
            {
                _installProcess.Kill();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace((FormattableString)$"Failed to stop installation process.", ex);
            }
            _installProcess = null;
        }
    }

    private void UpdateStatusDisplay()
    {
        // 状态标签已移除，只处理推荐标签的显示逻辑
        if (_recommendedBadge == null)
            return;
            
        // 根据状态更新推荐标签的可见性
        if (Status == PackageStatus.Completed)
        {
            // 已完成时隐藏推荐标签
            _recommendedBadge.Visibility = Visibility.Collapsed;
        }
        else if (IsRecommended)
        {
            // 未完成且是推荐项目时显示推荐标签
            if (string.IsNullOrEmpty(_recommendedBadge.Content?.ToString()))
            {
                _recommendedBadge.Content = Resource.PackageControl_Recommended;
            }
            _recommendedBadge.Visibility = Visibility.Visible;
        }
        else
        {
            _recommendedBadge.Visibility = Visibility.Collapsed;
        }
    }

    public void Report(float value) => Dispatcher.Invoke(() =>
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
        if (_package.Readme is null)
            return;

        new Uri(_package.Readme).Open();
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        // 如果已完成，不执行任何操作
        if (Status == PackageStatus.Completed)
            return;
        
        // 如果正在下载或安装，不重复执行
        if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
            return;
        
        await DownloadAndInstallPackageAsync();
    }

    private void CancelDownloadButton_Click(object sender, RoutedEventArgs e) => _downloadPackageTokenSource?.Cancel();
    
    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        // 如果已完成，不执行任何操作
        if (Status == PackageStatus.Completed)
            return;
        
        // 如果正在下载或安装，不重复执行
        if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
                return;
        
        await InstallPackageAsync();
    }
}
