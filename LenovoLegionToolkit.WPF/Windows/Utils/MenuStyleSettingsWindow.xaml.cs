using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Windows;
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

        private sealed class ThemeColors
        {
            public string BackgroundColor { get; set; } = "#ffffff";
            public string TextColor { get; set; } = "#000000";
            public string HoverBackgroundColor { get; set; } = "#f0f0f0";
            public string HoverTextColor { get; set; } = "#000000";
            public string SelectedBackgroundColor { get; set; } = "#0078d4";
            public string SelectedTextColor { get; set; } = "#ffffff";
        }

        private readonly ThemeColors _themeColors = new();

        public MenuStyleSettingsWindow()
        {
            InitializeComponent();
            LoadLocalizedStrings();
            LoadAllConfigFiles();
            ApplyConfigurationAvailability();
        }

        private static string Localized(string key, string fallback)
        {
            return Resource.ResourceManager.GetString(key, Resource.Culture) ?? fallback;
        }

        private static string LocalizedFormat(string key, string fallback, params object[] args)
        {
            return string.Format(Localized(key, fallback), args);
        }

        private void LoadLocalizedStrings()
        {
            if (_shellNssTab != null)
                _shellNssTab.Content = Localized("MenuStyleSettingsWindow_Tab_ShellNss", "shell.nss");

            if (_themeNssTab != null)
                _themeNssTab.Content = Localized("MenuStyleSettingsWindow_Tab_ThemeNss", "theme.nss");

            if (_imagesNssTab != null)
                _imagesNssTab.Content = Localized("MenuStyleSettingsWindow_Tab_ImagesNss", "images.nss");

            if (_modifyNssTab != null)
                _modifyNssTab.Content = Localized("MenuStyleSettingsWindow_Tab_ModifyNss", "modify.nss");
        }

        private void LoadAllConfigFiles()
        {
            _shellConfigPath = GetShellConfigPath();
            _baseDirectory = GetBaseDirectory();

            if (!string.IsNullOrEmpty(_baseDirectory))
            {
                _themeNssPath = Path.Combine(_baseDirectory, "imports", "theme.nss");
                _imagesNssPath = Path.Combine(_baseDirectory, "imports", "images.nss");
                _modifyNssPath = Path.Combine(_baseDirectory, "imports", "modify.nss");
            }

            if (_configPathTextBlock != null)
            {
                _configPathTextBlock.Text = !string.IsNullOrEmpty(_baseDirectory)
                    ? LocalizedFormat("MenuStyleSettingsWindow_BaseDirectory", "Base Directory: {0}", _baseDirectory)
                    : Localized("MenuStyleSettingsWindow_ConfigFileDirectoryNotFound", "Configuration file directory not found");
            }

            var missingFileText = Localized("MenuStyleSettingsWindow_FileNotFound", "File not found");

            if (_shellNssPathTextBox != null)
                _shellNssPathTextBox.Text = _shellConfigPath ?? missingFileText;

            if (_imagesNssPathTextBox != null)
                _imagesNssPathTextBox.Text = _imagesNssPath ?? missingFileText;

            if (_modifyNssPathTextBox != null)
                _modifyNssPathTextBox.Text = _modifyNssPath ?? missingFileText;

            var themeLoaded = LoadFileContent(_themeNssTextBox, _themeNssPath, GetDefaultThemeNssTemplate());
            if (themeLoaded)
                LoadThemeColorsFromText();
            else
                UpdateUIControls();
        }

        private bool LoadFileContent(TextBox? textBox, string? filePath, string defaultTemplate)
        {
            if (textBox == null)
                return false;

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                textBox.Text = defaultTemplate;
                return false;
            }

            try
            {
                textBox.Text = File.ReadAllText(filePath, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                textBox.Text =
                    LocalizedFormat("MenuStyleSettingsWindow_CannotReadConfigFile", "Cannot read configuration file: {0}", ex.Message)
                    + Environment.NewLine + Environment.NewLine
                    + defaultTemplate;
                return false;
            }
        }

        private void ApplyConfigurationAvailability()
        {
            var hasShellConfig = FileExists(_shellConfigPath);
            var hasThemeConfig = FileExists(_themeNssPath);
            var hasImagesConfig = FileExists(_imagesNssPath);
            var hasModifyConfig = FileExists(_modifyNssPath);

            SetTabAvailability(_shellNssTab, hasShellConfig);
            SetTabAvailability(_themeNssTab, hasThemeConfig);
            SetTabAvailability(_imagesNssTab, hasImagesConfig);
            SetTabAvailability(_modifyNssTab, hasModifyConfig);
            EnsureSelectedTab();

            if (_openShellNssBtn != null)
                _openShellNssBtn.IsEnabled = hasShellConfig;

            if (_openShellNssFolderBtn != null)
                _openShellNssFolderBtn.IsEnabled = DirectoryExistsForFile(_shellConfigPath);

            if (_openThemeNssBtn != null)
                _openThemeNssBtn.IsEnabled = hasThemeConfig;

            if (_openImagesNssBtn != null)
                _openImagesNssBtn.IsEnabled = hasImagesConfig;

            if (_openImagesNssFolderBtn != null)
                _openImagesNssFolderBtn.IsEnabled = DirectoryExistsForFile(_imagesNssPath);

            if (_openModifyNssBtn != null)
                _openModifyNssBtn.IsEnabled = hasModifyConfig;

            if (_openModifyNssFolderBtn != null)
                _openModifyNssFolderBtn.IsEnabled = DirectoryExistsForFile(_modifyNssPath);

            if (_themeNssTextBox != null)
                _themeNssTextBox.IsReadOnly = !hasThemeConfig;

            if (_updateFromUIBtn != null)
                _updateFromUIBtn.IsEnabled = hasThemeConfig;

            if (_loadToUIBtn != null)
                _loadToUIBtn.IsEnabled = hasThemeConfig;

            if (_applyButton != null)
                _applyButton.IsEnabled = hasThemeConfig;
        }

        private static bool FileExists(string? path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        private static bool DirectoryExistsForFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var directory = Path.GetDirectoryName(filePath);
            return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory);
        }

        private static void SetTabAvailability(RadioButton? tab, bool isAvailable)
        {
            if (tab == null)
                return;

            tab.Visibility = isAvailable ? Visibility.Visible : Visibility.Collapsed;
            tab.IsEnabled = isAvailable;

            if (!isAvailable && tab.IsChecked == true)
                tab.IsChecked = false;
        }

        private void EnsureSelectedTab()
        {
            RadioButton? firstVisibleTab = null;

            foreach (var tab in new[] { _shellNssTab, _themeNssTab, _imagesNssTab, _modifyNssTab })
            {
                if (tab == null || tab.Visibility != Visibility.Visible || !tab.IsEnabled)
                    continue;

                firstVisibleTab ??= tab;

                if (tab.IsChecked == true)
                    return;
            }

            if (firstVisibleTab != null)
                firstVisibleTab.IsChecked = true;
        }

        private void UpdateColorFromTextBox(TextBox textBox, Button button)
        {
            var hexColor = textBox.Text.Trim();
            if (!IsValidHexColor(hexColor))
                return;

            var color = HexToColor(hexColor);
            button.Tag = color;
            button.Background = new SolidColorBrush(color);
            button.Foreground = new SolidColorBrush(ContrastColor(color));
        }

        private static Color ContrastColor(Color color)
        {
            var brightness = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) / 255d;
            return brightness > 0.5 ? Colors.Black : Colors.White;
        }

        private static Color HexToColor(string hex)
        {
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex.Substring(1);

            var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromRgb(r, g, b);
        }

        private static bool IsValidHexColor(string hex)
        {
            return Regex.IsMatch(hex, "^#[0-9A-Fa-f]{6}$");
        }

        private string? GetBaseDirectory()
        {
            if (!string.IsNullOrEmpty(_shellConfigPath))
            {
                var directory = Path.GetDirectoryName(_shellConfigPath);
                if (!string.IsNullOrEmpty(directory))
                    return directory;
            }

            try
            {
                var baseDir = AppContext.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(baseDir))
                {
                    var direct = Path.Combine(baseDir, "shell.nss");
                    if (File.Exists(direct))
                        return baseDir;

                    var files = Directory.GetFiles(baseDir, "shell.nss", SearchOption.AllDirectories);
                    if (files.Length > 0)
                        return Path.GetDirectoryName(files[0]);
                }
            }
            catch
            {
                // Ignore fallback probing failures.
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var candidate = Path.Combine(programFiles, "Nilesoft Shell");
                if (Directory.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private string GetDefaultThemeNssTemplate()
        {
            return @"# Theme configuration file (theme.nss)
# This file defines theme-related style settings

theme
{
    # Base colors
    background-color: #ffffff;
    text-color: #000000;

    # Hover colors
    hover-background-color: #f0f0f0;
    hover-text-color: #000000;

    # Selected colors
    selected-background-color: #0078d4;
    selected-text-color: #ffffff;
}

# For more theme options, refer to the official Nilesoft Shell documentation
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

                    var files = Directory.GetFiles(baseDir, "shell.nss", SearchOption.AllDirectories);
                    if (files.Length > 0)
                        return files[0];
                }

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
                // Ignore probing failures and leave the editor in unavailable mode.
            }

            return null;
        }

        private void ShellNssTab_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void ThemeNssTab_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void ImagesNssTab_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void ModifyNssTab_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void LoadThemeColorsFromText()
        {
            if (_themeNssTextBox == null)
                return;

            var content = _themeNssTextBox.Text;
            _themeColors.BackgroundColor = ExtractColorValue(content, "background-color") ?? "#ffffff";
            _themeColors.TextColor = ExtractColorValue(content, "text-color") ?? "#000000";
            _themeColors.HoverBackgroundColor = ExtractColorValue(content, "hover-background-color") ?? "#f0f0f0";
            _themeColors.HoverTextColor = ExtractColorValue(content, "hover-text-color") ?? "#000000";
            _themeColors.SelectedBackgroundColor = ExtractColorValue(content, "selected-background-color") ?? "#0078d4";
            _themeColors.SelectedTextColor = ExtractColorValue(content, "selected-text-color") ?? "#ffffff";

            UpdateUIControls();
        }

        private static string? ExtractColorValue(string content, string propertyName)
        {
            var pattern = $@"{Regex.Escape(propertyName)}\s*:\s*([#0-9A-Fa-f]+)\s*;";
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
            return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : null;
        }

        private void UpdateUIControls()
        {
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

            if (_backgroundColorTextBox != null && _backgroundColorPicker != null)
                UpdateColorFromTextBox(_backgroundColorTextBox, _backgroundColorPicker);
            if (_textColorTextBox != null && _textColorPicker != null)
                UpdateColorFromTextBox(_textColorTextBox, _textColorPicker);
            if (_hoverBackgroundColorTextBox != null && _hoverBackgroundColorPicker != null)
                UpdateColorFromTextBox(_hoverBackgroundColorTextBox, _hoverBackgroundColorPicker);
            if (_hoverTextColorTextBox != null && _hoverTextColorPicker != null)
                UpdateColorFromTextBox(_hoverTextColorTextBox, _hoverTextColorPicker);
            if (_selectedBackgroundColorTextBox != null && _selectedBackgroundColorPicker != null)
                UpdateColorFromTextBox(_selectedBackgroundColorTextBox, _selectedBackgroundColorPicker);
            if (_selectedTextColorTextBox != null && _selectedTextColorPicker != null)
                UpdateColorFromTextBox(_selectedTextColorTextBox, _selectedTextColorPicker);
        }

        private void UpdateThemeTextFromUI()
        {
            if (_themeNssTextBox == null)
                return;

            UpdateThemeColorsFromUI();
            _themeNssTextBox.Text = GenerateThemeContent(_themeNssTextBox.Text);
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
            const string themeBlockPattern = @"theme\s*\{[\s\S]*?\}";
            var updatedThemeBlock = GenerateThemeBlock();

            return Regex.IsMatch(originalContent, themeBlockPattern)
                ? Regex.Replace(originalContent, themeBlockPattern, updatedThemeBlock)
                : $"{originalContent}{Environment.NewLine}{Environment.NewLine}{updatedThemeBlock}";
        }

        private string GenerateThemeBlock()
        {
            return $@"theme
{{
    # Base colors
    background-color: {_themeColors.BackgroundColor};
    text-color: {_themeColors.TextColor};

    # Hover colors
    hover-background-color: {_themeColors.HoverBackgroundColor};
    hover-text-color: {_themeColors.HoverTextColor};

    # Selected colors
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
                var result = MessageBox.Show(
                    Localized(
                        "MenuStyleSettingsWindow_ConfirmApplyConfig",
                        "Are you sure you want to apply the configuration?\n\nThis will:\n1. Save all configuration files\n2. Restart File Explorer to apply the configuration\n\nNote: Restarting will close all open folder windows and the taskbar, then automatically restart."),
                    Localized("MenuStyleSettingsWindow_ConfirmApplyConfig_Title", "Confirm Apply"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                if (_applyButton != null)
                    _applyButton.IsEnabled = false;

                var savedFiles = new List<string>();
                var failedFiles = new List<string>();

                await SaveAllFilesAsync(savedFiles, failedFiles);

                if (failedFiles.Count > 0 && savedFiles.Count == 0)
                {
                    MessageBox.Show(
                        LocalizedFormat(
                            "MenuStyleSettingsWindow_SaveFailed_Message",
                            "Unable to save configuration files:\n{0}\n\nPlease check file permissions and try again.",
                            string.Join(Environment.NewLine, failedFiles)),
                        Localized("MenuStyleSettingsWindow_SaveFailed_Title", "Save Failed"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                await ExplorerRestartHelper.RestartAsync();

                var messageBuilder = new StringBuilder();
                if (savedFiles.Count > 0)
                {
                    messageBuilder.AppendLine(LocalizedFormat(
                        "MenuStyleSettingsWindow_SavedFiles_Message",
                        "Saved files:\n{0}",
                        string.Join(Environment.NewLine, savedFiles)));
                    messageBuilder.AppendLine();
                }

                if (failedFiles.Count > 0)
                {
                    messageBuilder.AppendLine(LocalizedFormat(
                        "MenuStyleSettingsWindow_FailedFiles_Message",
                        "Failed files:\n{0}",
                        string.Join(Environment.NewLine, failedFiles)));
                    messageBuilder.AppendLine();
                }

                messageBuilder.Append(Localized(
                    "MenuStyleSettingsWindow_ApplySucceeded_Message",
                    "File Explorer has been restarted and the configuration is now active."));

                MessageBox.Show(
                    messageBuilder.ToString(),
                    failedFiles.Count > 0
                        ? Localized("MenuStyleSettingsWindow_PartialApplySucceeded_Title", "Applied with warnings")
                        : Localized("MenuStyleSettingsWindow_ApplySucceeded_Title", "Apply Succeeded"),
                    MessageBoxButton.OK,
                    failedFiles.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizedFormat(
                        "MenuStyleSettingsWindow_ApplyFailed_Message",
                        "An error occurred while applying the configuration:\n{0}\n\nYou can save files manually and restart Explorer from Task Manager.",
                        ex.Message),
                    Localized("MenuStyleSettingsWindow_ApplyFailed_Title", "Apply Failed"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (_applyButton != null)
                    _applyButton.IsEnabled = FileExists(_themeNssPath);
            }
        }

        private async Task SaveAllFilesAsync(List<string> savedFiles, List<string> failedFiles)
        {
            if (string.IsNullOrEmpty(_themeNssPath) || _themeNssTextBox == null)
                return;

            try
            {
                var directory = Path.GetDirectoryName(_themeNssPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(_themeNssPath, _themeNssTextBox.Text, Encoding.UTF8);
                savedFiles.Add("theme.nss");
            }
            catch (Exception ex)
            {
                failedFiles.Add($"theme.nss: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenShellNssBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(_shellConfigPath);
        }

        private void OpenShellNssFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Path.GetDirectoryName(_shellConfigPath));
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
            OpenFolder(Path.GetDirectoryName(_imagesNssPath));
        }

        private void OpenModifyNssBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(_modifyNssPath);
        }

        private void OpenModifyNssFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Path.GetDirectoryName(_modifyNssPath));
        }

        private void OpenFile(string? filePath)
        {
            if (!FileExists(filePath))
            {
                MessageBox.Show(
                    Localized("MenuStyleSettingsWindow_FileNotFound", "File not found"),
                    Localized("MenuStyleSettingsWindow_OpenFileFailed_Title", "Open File Failed"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizedFormat("MenuStyleSettingsWindow_OpenFileFailed_Message", "Unable to open file: {0}", ex.Message),
                    Localized("MenuStyleSettingsWindow_OpenFileFailed_Title", "Open File Failed"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenFolder(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show(
                    Localized("MenuStyleSettingsWindow_ConfigFileDirectoryNotFound", "Configuration file directory not found"),
                    Localized("MenuStyleSettingsWindow_OpenFolderFailed_Title", "Open Folder Failed"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizedFormat("MenuStyleSettingsWindow_OpenFolderFailed_Message", "Unable to open folder: {0}", ex.Message),
                    Localized("MenuStyleSettingsWindow_OpenFolderFailed_Title", "Open Folder Failed"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
