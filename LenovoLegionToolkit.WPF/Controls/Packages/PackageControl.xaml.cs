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
    private string? _actualDownloadedFilePath; // 存储实际下载的文件路径

    public enum PackageStatus
    {
        NotStarted,    // 未开始
        Downloading,   // 下载中
        Installing,    // 安装中
        Completed      // 已完成
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
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsCompleted));
            UpdateStatusDisplay();
            
            // 如果状态变成已完成，隐藏控件（在主界面中）
            if (value == PackageStatus.Completed)
            {
                Visibility = Visibility.Collapsed;
            }
        }
    }

    public string StatusText => Status switch
    {
        PackageStatus.Downloading => "下载中",
        PackageStatus.Installing => "安装中",
        PackageStatus.Completed => "已完成",
        _ => string.Empty
    };

    public bool IsCompleted => Status == PackageStatus.Completed;

    /// <summary>
    /// 获取实际下载的文件名（包含标题前缀）
    /// </summary>
    private string GetActualFileName()
    {
        // 实际下载的文件名格式："{SanitizedTitle} - {FileName}"
        var sanitizedTitle = SanitizeFileName(_package.Title);
        return $"{sanitizedTitle} - {_package.FileName}";
    }

    /// <summary>
    /// 清理文件名中的非法字符
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
            
            // 同步更新UI中的复选框状态（避免触发事件导致循环）
            if (_selectCheckBox != null && _selectCheckBox.IsChecked != value)
            {
                // 临时移除事件处理器，避免触发SelectCheckBox_Checked/Unchecked
                _selectCheckBox.Checked -= SelectCheckBox_Checked;
                _selectCheckBox.Unchecked -= SelectCheckBox_Unchecked;
                
                _selectCheckBox.IsChecked = value;
                
                // 重新添加事件处理器
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
        
        // 如果是可更新项目，标记为推荐
        IsRecommended = package.IsUpdate;
        
        // 绑定复选框
        if (_selectCheckBox != null)
        {
            _selectCheckBox.IsChecked = IsSelected;
            _selectCheckBox.Checked += SelectCheckBox_Checked;
            _selectCheckBox.Unchecked += SelectCheckBox_Unchecked;
        }
        
        // 初始化推荐标签和状态显示
        if (_recommendedBadge != null)
        {
            // 确保推荐标签有内容
            if (string.IsNullOrEmpty(_recommendedBadge.Content?.ToString()))
            {
                _recommendedBadge.Content = "推荐";
            }
            _recommendedBadge.Visibility = IsRecommended && Status != PackageStatus.Completed 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
        
        // 初始化状态显示（必须在推荐标签之后，因为UpdateStatusDisplay可能会修改推荐标签的可见性）
        UpdateStatusDisplay();
        
        // 检查文件是否已下载，如果已下载则将下载按钮切换为安装按钮
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
            
            // 如果实际路径不存在，尝试构造的路径
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                filePath = Path.Combine(downloadPath, GetActualFileName());
            }
            
            // 如果还是找不到，尝试搜索匹配的文件
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
        
        // 如果已完成，不执行任何操作
        if (Status == PackageStatus.Completed)
            return;
        
        // 如果正在下载或安装，不重复执行
        if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
            return;
        
        // 检查文件是否已下载
        var downloadPath = _getDownloadPath();
        var filePath = _actualDownloadedFilePath;
        
        // 如果实际路径不存在，尝试构造的路径
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            filePath = Path.Combine(downloadPath, GetActualFileName());
        }
        
        // 如果还是找不到，尝试搜索匹配的文件
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
            // 文件已存在，直接安装
            await InstallPackageAsync();
        }
        else
        {
            // 文件不存在，先下载再安装
            await DownloadAndInstallPackageAsync();
        }
    }

    private void SelectCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        IsSelected = false;
        
        // 如果已完成，不允许取消
        if (Status == PackageStatus.Completed)
        {
            // 重新选中复选框
            if (_selectCheckBox != null)
                _selectCheckBox.IsChecked = true;
            return;
        }
        
        // 停止下载或安装
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

            // 保存实际下载的文件路径
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
            
            // 使用实际下载的文件路径，如果不存在则使用构造的路径
            var filePath = _actualDownloadedFilePath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                var downloadPath = _getDownloadPath();
                filePath = Path.Combine(downloadPath, GetActualFileName());
            }
            
            // 等待文件完全写入，最多等待3秒
            var maxRetries = 30; // 最多重试30次
            var retryDelay = 100; // 每次延迟100ms
            
            for (int i = 0; i < maxRetries; i++)
            {
                if (File.Exists(filePath))
                {
                    // 检查文件是否可访问（不是正在写入）
                    try
                    {
                        using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            // 文件可访问，可以继续安装
                            break;
                        }
                    }
                    catch
                    {
                        // 文件正在被写入，继续等待
                    }
                }
                
                if (i < maxRetries - 1)
                    await Task.Delay(retryDelay);
            }
            
            // 再次确认文件存在后再安装
            if (File.Exists(filePath))
            {
                await InstallPackageAsync();
            }
            else
            {
                // 如果还是找不到，尝试在下载目录中搜索匹配的文件
                var downloadPath = _getDownloadPath();
                if (Directory.Exists(downloadPath))
                {
                    var files = Directory.GetFiles(downloadPath, $"*{_package.FileName}");
                    if (files.Length > 0)
                    {
                        // 找到匹配的文件，使用第一个
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
            // 优先使用实际下载的文件路径
            var filePath = _actualDownloadedFilePath;
            
            // 如果实际路径不存在，尝试构造的路径
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                var downloadPath = _getDownloadPath();
                filePath = Path.Combine(downloadPath, GetActualFileName());
            }
            
            // 再次检查文件是否存在，如果不存在则等待一段时间后重试
            if (!File.Exists(filePath))
            {
                // 等待最多2秒，每200ms检查一次
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(200);
                    if (File.Exists(filePath))
                        break;
                }
            }
            
            // 如果还是找不到，尝试在下载目录中搜索匹配的文件
            if (!File.Exists(filePath))
            {
                var downloadPath = _getDownloadPath();
                if (Directory.Exists(downloadPath))
                {
                    var files = Directory.GetFiles(downloadPath, $"*{_package.FileName}");
                    if (files.Length > 0)
                    {
                        // 找到匹配的文件，使用第一个
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
                _recommendedBadge.Content = "推荐";
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
