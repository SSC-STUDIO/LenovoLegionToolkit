using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace LenovoLegionToolkit.WPF.Windows.Utils
{
    public partial class MenuStyleSettingsWindow : UiWindow
    {
        private string? _shellConfigPath;
        private string? _baseDirectory;
        private string? _themeNssPath;
        private string? _imagesNssPath;
        private string? _modifyNssPath;

        public MenuStyleSettingsWindow()
        {
            InitializeComponent();
            LoadAllConfigFiles();
        }

        private void LoadAllConfigFiles()
        {
            // Try to locate shell.nss configuration file and base directory
            _shellConfigPath = GetShellConfigPath();
            _baseDirectory = GetBaseDirectory();
            
            // Determine paths for import files
            if (!string.IsNullOrEmpty(_baseDirectory))
            {
                _themeNssPath = Path.Combine(_baseDirectory, "imports", "theme.nss");
                _imagesNssPath = Path.Combine(_baseDirectory, "imports", "images.nss");
                _modifyNssPath = Path.Combine(_baseDirectory, "imports", "modify.nss");
            }
            
            // Update config path display
            if (_configPathTextBlock != null)
            {
                var pathInfo = !string.IsNullOrEmpty(_baseDirectory) 
                    ? $"基础目录: {_baseDirectory}" 
                    : "未找到配置文件目录";
                _configPathTextBlock.Text = pathInfo;
            }

            // Load shell.nss
            LoadFileContent(_shellNssTextBox, _shellConfigPath, GetDefaultShellNssTemplate());

            // Load import files
            LoadFileContent(_themeNssTextBox, _themeNssPath, GetDefaultThemeNssTemplate());
            LoadFileContent(_imagesNssTextBox, _imagesNssPath, GetDefaultImagesNssTemplate());
            LoadFileContent(_modifyNssTextBox, _modifyNssPath, GetDefaultModifyNssTemplate());
        }

        private void LoadFileContent(System.Windows.Controls.TextBox textBox, string? filePath, string defaultTemplate)
        {
            if (textBox == null)
                return;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                textBox.Text = defaultTemplate;
                return;
            }

            try
            {
                var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                textBox.Text = content;
            }
            catch (Exception ex)
            {
                textBox.Text = $"# 无法读取配置文件: {ex.Message}\n\n{defaultTemplate}";
            }
        }

        private string? GetBaseDirectory()
        {
            // Try to get directory from shell.nss path
            if (!string.IsNullOrEmpty(_shellConfigPath))
            {
                var dir = Path.GetDirectoryName(_shellConfigPath);
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }

            // Try AppContext.BaseDirectory
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                if (File.Exists(Path.Combine(baseDir, "shell.nss")))
                    return baseDir;

                // Search recursively
                var files = Directory.GetFiles(baseDir, "shell.nss", SearchOption.AllDirectories);
                if (files.Length > 0)
                    return Path.GetDirectoryName(files[0]);
            }

            // Fallback to ProgramFiles
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var candidate = Path.Combine(programFiles, "Nilesoft Shell");
                if (Directory.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private string GetDefaultShellNssTemplate()
        {
            return @"# Nilesoft Shell 配置文件 (shell.nss)
# 此文件用于自定义 Windows 右键菜单的外观和行为

# 导入基础主题配置
import 'imports/theme.nss'
import 'imports/images.nss'
import 'imports/modify.nss'

# 主题设置
theme
{
    # 外观设置
    corner-radius: 5px;
    shadow: true;
    transparency: false;
    
    # 颜色设置
    background-color: #ffffff;
    text-color: #000000;
}

# 菜单样式
.menu
{
    padding: 4px;
    border-width: 1px;
    border-style: solid;
    border-radius: 5px;
}

# 分隔符样式
.separator
{
    height: 1px;
    margin: 4px 20px;
}

# 更多配置选项请参考 Nilesoft Shell 官方文档
";
        }

        private string GetDefaultThemeNssTemplate()
        {
            return @"# Theme configuration file (theme.nss)
# 此文件用于定义主题相关的样式设置

# 主题颜色定义
theme
{
    # 基础颜色
    background-color: #ffffff;
    text-color: #000000;
    
    # 悬停颜色
    hover-background-color: #f0f0f0;
    hover-text-color: #000000;
    
    # 选中颜色
    selected-background-color: #0078d4;
    selected-text-color: #ffffff;
}

# 更多主题配置选项请参考 Nilesoft Shell 官方文档
";
        }

        private string GetDefaultImagesNssTemplate()
        {
            return @"# Images configuration file (images.nss)
# 此文件用于定义菜单项图标和图像设置

# 图标设置示例
# item
# {
#     icon: 'path/to/icon.png';
#     icon-size: 16px;
# }

# 更多图像配置选项请参考 Nilesoft Shell 官方文档
";
        }

        private string GetDefaultModifyNssTemplate()
        {
            return @"# Modify configuration file (modify.nss)
# 此文件用于修改和自定义右键菜单项

# 菜单项修改示例
# menu
# {
#     item
#     {
#         text: '自定义项';
#         command: 'notepad.exe';
#     }
# }

# 更多菜单修改选项请参考 Nilesoft Shell 官方文档
";
        }

        private string? GetShellConfigPath()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(baseDir))
                {
                    var direct = Path.Combine(baseDir, "shell.nss");
                    if (File.Exists(direct))
                        return direct;

                    // Search recursively if not found directly
                    var files = Directory.GetFiles(baseDir, "shell.nss", SearchOption.AllDirectories);
                    if (files.Length > 0)
                        return files[0];
                }

                // Fallback to default installation path
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if (!string.IsNullOrWhiteSpace(programFiles))
                {
                    var candidate = Path.Combine(programFiles, "Nilesoft Shell", "shell.nss");
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var savedFiles = new List<string>();
                var failedFiles = new List<string>();

                // Save shell.nss
                if (!string.IsNullOrEmpty(_shellConfigPath) && _shellNssTextBox != null)
                {
                    try
                    {
                        // Ensure directory exists
                        var dir = Path.GetDirectoryName(_shellConfigPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.WriteAllText(_shellConfigPath, _shellNssTextBox.Text, System.Text.Encoding.UTF8);
                        savedFiles.Add("shell.nss");
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"shell.nss: {ex.Message}");
                    }
                }

                // Save theme.nss
                if (!string.IsNullOrEmpty(_themeNssPath) && _themeNssTextBox != null)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(_themeNssPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.WriteAllText(_themeNssPath, _themeNssTextBox.Text, System.Text.Encoding.UTF8);
                        savedFiles.Add("theme.nss");
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"theme.nss: {ex.Message}");
                    }
                }

                // Save images.nss
                if (!string.IsNullOrEmpty(_imagesNssPath) && _imagesNssTextBox != null)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(_imagesNssPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.WriteAllText(_imagesNssPath, _imagesNssTextBox.Text, System.Text.Encoding.UTF8);
                        savedFiles.Add("images.nss");
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"images.nss: {ex.Message}");
                    }
                }

                // Save modify.nss
                if (!string.IsNullOrEmpty(_modifyNssPath) && _modifyNssTextBox != null)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(_modifyNssPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.WriteAllText(_modifyNssPath, _modifyNssTextBox.Text, System.Text.Encoding.UTF8);
                        savedFiles.Add("modify.nss");
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"modify.nss: {ex.Message}");
                    }
                }

                // Show result message
                var message = "";
                if (savedFiles.Count > 0)
                {
                    message = $"已成功保存以下文件：\n{string.Join("\n", savedFiles)}\n\n";
                }

                if (failedFiles.Count > 0)
                {
                    message += $"保存失败的文件：\n{string.Join("\n", failedFiles)}\n\n";
                }

                if (savedFiles.Count > 0)
                {
                    message += "注意：修改配置后需要重启资源管理器（Explorer）才能生效。\n您可以在任务管理器中结束 Explorer 进程，然后重新启动它。";
                    MessageBox.Show(
                        message,
                        failedFiles.Count > 0 ? "部分保存成功" : "保存成功",
                        MessageBoxButton.OK,
                        failedFiles.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "所有文件保存失败。\n请确保 Nilesoft Shell 已正确安装，并且有写入权限。",
                        "保存失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"保存配置文件时出错：\n{ex.Message}",
                    "保存失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ConfigTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // This method can be used to track which tab is selected if needed
        }

        private void RestartExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Confirm with user
                var result = MessageBox.Show(
                    "确定要重启资源管理器（Explorer）吗？\n\n这将关闭所有打开的文件夹窗口和任务栏，然后自动重新启动。",
                    "确认重启资源管理器",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Disable button to prevent multiple clicks
                if (_restartExplorerButton != null)
                    _restartExplorerButton.IsEnabled = false;

                // Restart Explorer
                RestartExplorer();

                MessageBox.Show(
                    "资源管理器已重启！\n\n如果配置已保存，新的配置应该已经生效。",
                    "重启成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"重启资源管理器时出错：\n{ex.Message}\n\n您可以手动在任务管理器中结束 Explorer 进程，然后重新启动它。",
                    "重启失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                if (_restartExplorerButton != null)
                    _restartExplorerButton.IsEnabled = true;
            }
        }

        private static void RestartExplorer()
        {
            try
            {
                // First, kill explorer.exe
                var killInfo = new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = "/f /im explorer.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var killProcess = Process.Start(killInfo);
                if (killProcess != null)
                {
                    killProcess.WaitForExit(5000);
                }

                // Wait a moment for processes to fully terminate
                System.Threading.Thread.Sleep(1000);

                // Then, start explorer.exe
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Failed to restart Explorer.", ex);
                throw;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}