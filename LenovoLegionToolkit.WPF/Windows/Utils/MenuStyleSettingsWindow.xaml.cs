using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Windows;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace LenovoLegionToolkit.WPF.Windows.Utils
{
    public partial class MenuStyleSettingsWindow : BaseWindow
    {
        private string? _shellConfigPath;
        private string? _baseDirectory;
        private string? _themeNssPath;
        private string? _imagesNssPath;
        private string? _modifyNssPath;

        // Theme colors structure
        private class ThemeColors
        {
            public string BackgroundColor { get; set; } = "#ffffff";
            public string TextColor { get; set; } = "#000000";
            public string HoverBackgroundColor { get; set; } = "#f0f0f0";
            public string HoverTextColor { get; set; } = "#000000";
            public string SelectedBackgroundColor { get; set; } = "#0078d4";
            public string SelectedTextColor { get; set; } = "#ffffff";
        }

        private ThemeColors _themeColors = new();

        public MenuStyleSettingsWindow()
        {
            InitializeComponent();
            LoadAllConfigFiles();
            LoadLocalizedStrings();
        }

        private void LoadLocalizedStrings()
        {
            // Load localized strings for UI elements
            if (_shellNssTab != null)
            {
                _shellNssTab.Content = Resource.ResourceManager.GetString("MenuStyleSettingsWindow_Tab_ShellNss", Resource.Culture) ?? "shell.nss";
            }

            if (_themeNssTab != null)
            {
                _themeNssTab.Content = Resource.ResourceManager.GetString("MenuStyleSettingsWindow_Tab_ThemeNss", Resource.Culture) ?? "theme.nss";
            }

            if (_imagesNssTab != null)
            {
                _imagesNssTab.Content = Resource.ResourceManager.GetString("MenuStyleSettingsWindow_Tab_ImagesNss", Resource.Culture) ?? "images.nss";
            }

            if (_modifyNssTab != null)
            {
                _modifyNssTab.Content = Resource.ResourceManager.GetString("MenuStyleSettingsWindow_Tab_ModifyNss", Resource.Culture) ?? "modify.nss";
            }
        }



        private void UpdateColorFromTextBox(System.Windows.Controls.TextBox textBox, Button button)
        {
            string hexColor = textBox.Text.Trim();
            if (IsValidHexColor(hexColor))
            {
                var color = HexToColor(hexColor);
                if (button != null)
                {
                    button.Tag = color;
                    button.Background = new SolidColorBrush(color);
                    button.Foreground = new SolidColorBrush(ContrastColor(color));
                }
            }
        }

        private Color ContrastColor(Color color)
        {
            byte r = color.R;
            byte g = color.G;
            byte b = color.B;
            double brightness = (r * 0.299 + g * 0.587 + b * 0.114) / 255;
            return brightness > 128 ? Colors.Black : Colors.White;
        }

        private string ColorToHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        private Color HexToColor(string hex)
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromRgb(r, g, b);
        }

        private bool IsValidHexColor(string hex)
        {
            return Regex.IsMatch(hex, @"^#[0-9A-Fa-f]{6}$");
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

            // Update path text boxes
            if (_shellNssPathTextBox != null)
            {
                _shellNssPathTextBox.Text = _shellConfigPath ?? "未找到文件";
            }
            if (_imagesNssPathTextBox != null)
            {
                _imagesNssPathTextBox.Text = _imagesNssPath ?? "未找到文件";
            }
            if (_modifyNssPathTextBox != null)
            {
                _modifyNssPathTextBox.Text = _modifyNssPath ?? "未找到文件";
            }

            // Only load theme.nss content, others use external editor
            LoadFileContent(_themeNssTextBox, _themeNssPath, GetDefaultThemeNssTemplate());

            // Load theme colors from text to UI
            LoadThemeColorsFromText();
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


        private void ShellNssTab_Checked(object sender, RoutedEventArgs e)
        {
            // Tab visibility is handled by XAML binding
        }

        private void ThemeNssTab_Checked(object sender, RoutedEventArgs e)
        {
            // Tab visibility is handled by XAML binding
        }

        private void ImagesNssTab_Checked(object sender, RoutedEventArgs e)
        {
            // Tab visibility is handled by XAML binding
        }

        private void ModifyNssTab_Checked(object sender, RoutedEventArgs e)
        {
            // Tab visibility is handled by XAML binding
        }

        private void LoadThemeColorsFromText()
        {
            if (_themeNssTextBox == null)
                return;

            string content = _themeNssTextBox.Text;

            // Parse theme colors
            _themeColors.BackgroundColor = ExtractColorValue(content, "background-color") ?? "#ffffff";
            _themeColors.TextColor = ExtractColorValue(content, "text-color") ?? "#000000";
            _themeColors.HoverBackgroundColor = ExtractColorValue(content, "hover-background-color") ?? "#f0f0f0";
            _themeColors.HoverTextColor = ExtractColorValue(content, "hover-text-color") ?? "#000000";
            _themeColors.SelectedBackgroundColor = ExtractColorValue(content, "selected-background-color") ?? "#0078d4";
            _themeColors.SelectedTextColor = ExtractColorValue(content, "selected-text-color") ?? "#ffffff";

            // Update UI controls
            UpdateUIControls();
        }

        private string? ExtractColorValue(string content, string propertyName)
        {
            string pattern = $@"{propertyName}*:*([#0-9a-fA-F]+);";
            Match match = Regex.Match(content, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            return null;
        }

        private void UpdateUIControls()
        {
            // Update text boxes
            if (_backgroundColorTextBox != null)
                _backgroundColorTextBox.Text = _themeColors.BackgroundColor;
            if (_textColorTextBox != null)
                _textColorTextBox.Text = _themeColors.TextColor;
            if (_hoverBackgroundColorTextBox != null)
                _hoverBackgroundColorTextBox.Text = _themeColors.HoverBackgroundColor;
            if (_hoverTextColorTextBox != null)
                _hoverTextColorTextBox.Text = _themeColors.HoverTextColor;
            if (_selectedBackgroundColorTextBox != null)
                _selectedBackgroundColorTextBox.Text = _themeColors.SelectedBackgroundColor;
            if (_selectedTextColorTextBox != null)
                _selectedTextColorTextBox.Text = _themeColors.SelectedTextColor;
        }

        private void UpdateThemeTextFromUI()
        {
            if (_themeNssTextBox == null)
                return;

            // Get current text content
            string content = _themeNssTextBox.Text;

            // Update theme colors from UI controls
            UpdateThemeColorsFromUI();

            // Build new theme content
            string newThemeContent = GenerateThemeContent(content);

            // Update the text box
            _themeNssTextBox.Text = newThemeContent;
        }

        private void UpdateThemeColorsFromUI()
        {
            if (_backgroundColorTextBox != null)
                _themeColors.BackgroundColor = _backgroundColorTextBox.Text.Trim();
            if (_textColorTextBox != null)
                _themeColors.TextColor = _textColorTextBox.Text.Trim();
            if (_hoverBackgroundColorTextBox != null)
                _themeColors.HoverBackgroundColor = _hoverBackgroundColorTextBox.Text.Trim();
            if (_hoverTextColorTextBox != null)
                _themeColors.HoverTextColor = _hoverTextColorTextBox.Text.Trim();
            if (_selectedBackgroundColorTextBox != null)
                _themeColors.SelectedBackgroundColor = _selectedBackgroundColorTextBox.Text.Trim();
            if (_selectedTextColorTextBox != null)
                _themeColors.SelectedTextColor = _selectedTextColorTextBox.Text.Trim();
        }

        private string GenerateThemeContent(string originalContent)
        {
            // Check if theme block exists
            string themeBlockPattern = @"theme\s*\{[\s\S]*?\}";
            Match themeBlockMatch = Regex.Match(originalContent, themeBlockPattern);

            if (themeBlockMatch.Success)
            {
                // Update existing theme block
                string updatedThemeBlock = GenerateThemeBlock();
                return Regex.Replace(originalContent, themeBlockPattern, updatedThemeBlock);
            }
            else
            {
                // Add new theme block
                string newThemeBlock = GenerateThemeBlock();
                return $"{originalContent}\n\n{newThemeBlock}";
            }
        }

        private string GenerateThemeBlock()
        {
            return $@"theme
{{
    # 基础颜色
    background-color: {_themeColors.BackgroundColor};
    text-color: {_themeColors.TextColor};
    
    # 悬停颜色
    hover-background-color: {_themeColors.HoverBackgroundColor};
    hover-text-color: {_themeColors.HoverTextColor};
    
    # 选中颜色
    selected-background-color: {_themeColors.SelectedBackgroundColor};
    selected-text-color: {_themeColors.SelectedTextColor};
}}";
        }

        private void LoadToUIBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadThemeColorsFromText();
        }

        private void UpdateFromUIBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateThemeTextFromUI();
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Confirm with user
                var result = MessageBox.Show(
                    "确定要应用配置吗？\n\n这将：\n1. 保存所有配置文件\n2. 重启资源管理器（Explorer）使配置生效\n\n注意：重启将关闭所有打开的文件夹窗口和任务栏，然后自动重新启动。",
                    "确认应用配置",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Disable button to prevent multiple clicks
                if (_applyButton != null)
                    _applyButton.IsEnabled = false;

                // Step 1: Save all configuration files
                var savedFiles = new List<string>();
                var failedFiles = new List<string>();

                await SaveAllFilesAsync(savedFiles, failedFiles);

                // Check if save was successful
                if (failedFiles.Count > 0 && savedFiles.Count == 0)
                {
                    MessageBox.Show(
                        $"无法保存配置文件：\n{string.Join("\n", failedFiles)}\n\n请检查文件权限后重试。",
                        "保存失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Step 2: Restart Explorer
                await RestartExplorerAsync();

                // Show success message
                var message = "";
                if (savedFiles.Count > 0)
                {
                    message = $"已成功保存以下文件：\n{string.Join("\n", savedFiles)}\n\n";
                }

                if (failedFiles.Count > 0)
                {
                    message += $"保存失败的文件：\n{string.Join("\n", failedFiles)}\n\n";
                }

                message += "资源管理器已重启，配置已生效！";

                MessageBox.Show(
                    message,
                    failedFiles.Count > 0 ? "部分应用成功" : "应用成功",
                    MessageBoxButton.OK,
                    failedFiles.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"应用配置时出错：\n{ex.Message}\n\n您可以手动保存文件并在任务管理器中重启 Explorer 进程。",
                    "应用失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                if (_applyButton != null)
                    _applyButton.IsEnabled = true;
            }
        }

        private async Task SaveAllFilesAsync(List<string> savedFiles, List<string> failedFiles)
        {
            // Only save theme.nss since others are edited externally
            if (!string.IsNullOrEmpty(_themeNssPath) && _themeNssTextBox != null)
            {
                try
                {
                    var dir = Path.GetDirectoryName(_themeNssPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    await File.WriteAllTextAsync(_themeNssPath, _themeNssTextBox.Text, System.Text.Encoding.UTF8);
                    savedFiles.Add("theme.nss");
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"theme.nss: {ex.Message}");
                }
            }
        }

        private static async Task RestartExplorerAsync()
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
                await Task.Delay(1000).ConfigureAwait(false);

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

        #region File Operations
        
        private void OpenShellNssBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(_shellConfigPath);
        }

        private void OpenShellNssFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_shellConfigPath))
            {
                string? folderPath = Path.GetDirectoryName(_shellConfigPath);
                OpenFolder(folderPath);
            }
        }

        private void OpenThemeNssBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(_themeNssPath);
        }

        private void OpenImagesNssBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(_imagesNssPath);
        }

        private void OpenImagesNssFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_imagesNssPath))
            {
                string? folderPath = Path.GetDirectoryName(_imagesNssPath);
                OpenFolder(folderPath);
            }
        }

        private void OpenModifyNssBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(_modifyNssPath);
        }

        private void OpenModifyNssFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_modifyNssPath))
            {
                string? folderPath = Path.GetDirectoryName(_modifyNssPath);
                OpenFolder(folderPath);
            }
        }

        private void OpenFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("文件不存在或路径无效", "打开文件失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 使用系统默认程序打开文件
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开文件: {ex.Message}", "打开文件失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolder(string? folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show("文件夹不存在或路径无效", "打开文件夹失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 打开文件夹
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开文件夹: {ex.Message}", "打开文件夹失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}