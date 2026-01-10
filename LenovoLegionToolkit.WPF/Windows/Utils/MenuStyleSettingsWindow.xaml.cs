using System;
using System.Collections.Generic;
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

        public MenuStyleSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            LoadImplementationDetails();
        }

        private void LoadSettings()
        {
            // Try to locate shell.nss configuration file
            _shellConfigPath = GetShellConfigPath();
            if (string.IsNullOrEmpty(_shellConfigPath))
            {
                // If no config file found, use defaults
                _themeAutoRadio.IsChecked = true;
                _transparencyToggle.IsChecked = false;
                _roundedCornersToggle.IsChecked = true;
                _shadowsToggle.IsChecked = true;
                return;
            }

            // Read existing configuration from shell.nss file
            try
            {
                var configContent = File.ReadAllText(_shellConfigPath);
                
                // For now, use defaults - in a real implementation, we would parse the config file
                _themeAutoRadio.IsChecked = true;
                _transparencyToggle.IsChecked = false;
                _roundedCornersToggle.IsChecked = true;
                _shadowsToggle.IsChecked = true;
            }
            catch
            {
                // If config file can't be read, use defaults
                _themeAutoRadio.IsChecked = true;
                _transparencyToggle.IsChecked = false;
                _roundedCornersToggle.IsChecked = true;
                _shadowsToggle.IsChecked = true;
            }
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

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTheme = GetSelectedTheme();
                var transparencyEnabled = _transparencyToggle.IsChecked == true;
                var roundedCornersEnabled = _roundedCornersToggle.IsChecked == true;
                var shadowsEnabled = _shadowsToggle.IsChecked == true;

                // Determine the path to shell.nss
                if (string.IsNullOrEmpty(_shellConfigPath))
                {
                    MessageBox.Show(
                        Resource.MenuStyleSettingsWindow_ConfigFileNotFound_Message,
                        Resource.MenuStyleSettingsWindow_ConfigFileNotFound_Title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Create a basic theme configuration based on selections
                var themeConfig = GenerateShellConfig(selectedTheme, transparencyEnabled, roundedCornersEnabled, shadowsEnabled);

                // Write the configuration to the file
                File.WriteAllText(_shellConfigPath, themeConfig);

                MessageBox.Show(
                    Resource.MenuStyleSettingsWindow_SettingsApplied_Message,
                    Resource.MenuStyleSettingsWindow_SettingsApplied_Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Resource.MenuStyleSettingsWindow_FailedToApplySettings_Message, ex.Message),
                    Resource.MenuStyleSettingsWindow_Error_Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string GetSelectedTheme()
        {
            if (_themeLightRadio.IsChecked == true) return "light";
            if (_themeDarkRadio.IsChecked == true) return "dark";
            if (_themeClassicRadio.IsChecked == true) return "classic";
            if (_themeModernRadio.IsChecked == true) return "modern";
            return "auto"; // default
        }

        private string GenerateShellConfig(string theme, bool transparencyEnabled, bool roundedCornersEnabled, bool shadowsEnabled)
        {
            string themeColors;
            switch (theme)
            {
                case "light":
                    themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                    break;
                case "dark":
                    themeColors = "background-color: #2d2d2d;\ntext-color: #ffffff;";
                    break;
                case "classic":
                    themeColors = "background-color: #f0f0f0;\ntext-color: #000000;";
                    break;
                case "modern":
                    themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                    break;
                default:
                    themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                    break;
            }

            var config = "# Generated by Lenovo Legion Toolkit\n" +
                        $"# Theme: {theme}\n" +
                        $"# Transparency: {(transparencyEnabled ? "enabled" : "disabled")}\n" +
                        $"# Rounded corners: {(roundedCornersEnabled ? "enabled" : "disabled")}\n" +
                        $"# Shadows: {(shadowsEnabled ? "enabled" : "disabled")}\n" +
                        $"\n" +
                        $"# Import base theme configuration\n" +
                        $"import 'imports/theme.nss'\n" +
                        $"import 'imports/images.nss'\n" +
                        $"import 'imports/modify.nss'\n" +
                        $"\n" +
                        $"# Theme settings based on user selection\n" +
                        $"theme\n" +
                        $"{{\n" +
                        $"    # Appearance settings\n" +
                        $"    corner-radius: {(roundedCornersEnabled ? "5" : "0")}px;\n" +
                        $"    shadow: {(shadowsEnabled ? "true" : "false")};\n" +
                        $"    transparency: {(transparencyEnabled ? "true" : "false")};\n" +
                        $"\n" +
                        $"    # Color settings based on selected theme\n" +
                        $"    {themeColors}\n" +
                        $"}}\n" +
                        $"\n" +
                        $"# Additional configuration for different contexts\n" +
                        $".menu\n" +
                        $"{{\n" +
                        $"    padding: 4px;\n" +
                        $"    border-width: 1px;\n" +
                        $"    border-style: solid;\n" +
                        $"    {(roundedCornersEnabled ? "border-radius: 5px;" : "")}\n" +
                        $"}}\n" +
                        $"\n" +
                        $".separator\n" +
                        $"{{\n" +
                        $"    height: 1px;\n" +
                        $"    margin: 4px 20px;\n" +
                        $"}}\n";

            return config;
        }

        private void LoadImplementationDetails()
        {
            try
            {
                var details = GetContextMenuImplementationDetails();
                
                // Clear and fill in detailed information
                _detailsStackPanel.Children.Clear();
                foreach (var detail in details)
                {
                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = detail,
                        FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New"),
                        FontSize = 12,
                        Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorPrimaryBrush"),
                        Margin = new Thickness(0, 0, 0, 8),
                        TextWrapping = TextWrapping.Wrap
                    };
                    _detailsStackPanel.Children.Add(textBlock);
                }
            }
            catch (Exception ex)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Failed to load implementation details.", ex);
            }
        }

        private List<string> GetContextMenuImplementationDetails()
        {
            var details = new List<string>();
            
            // COM registration command
            details.Add(Resource.MenuStyleSettingsWindow_Details_COMRegistration);
            var shellExePath = Lib.System.NilesoftShellHelper.GetNilesoftShellExePath();
            if (!string.IsNullOrWhiteSpace(shellExePath))
            {
                details.Add(Resource.MenuStyleSettingsWindow_Details_ShellExeCommand);
                details.Add(string.Format(Resource.MenuStyleSettingsWindow_Details_Path, shellExePath));
            }
            else
            {
                details.Add($"   {Resource.MenuStyleSettingsWindow_ShellExeNotFound}");
            }
            
            details.Add("");
            
            // Configuration file description
            details.Add(Resource.MenuStyleSettingsWindow_Details_ConfigFile);
            var configPath = GetShellConfigPath();
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                details.Add(string.Format(Resource.MenuStyleSettingsWindow_Details_ConfigFilePath, configPath));
            }
            else
            {
                details.Add($"   {Resource.MenuStyleSettingsWindow_ConfigPathNotFound}");
            }
            details.Add(Resource.MenuStyleSettingsWindow_Details_ConfigFileSyntax);
            
            details.Add("");
            
            // Configuration file format example
            details.Add(Resource.MenuStyleSettingsWindow_Details_ConfigFileExample);
            details.Add(Resource.MenuStyleSettingsWindow_Details_ThemeExample);
            
            details.Add("");
            
            // How it works
            details.Add(Resource.MenuStyleSettingsWindow_Details_WorkingPrinciple);
            details.Add(Resource.MenuStyleSettingsWindow_Details_Principle1);
            details.Add(Resource.MenuStyleSettingsWindow_Details_Principle2);
            details.Add(Resource.MenuStyleSettingsWindow_Details_Principle3);
            details.Add(Resource.MenuStyleSettingsWindow_Details_Principle4);
            details.Add(Resource.MenuStyleSettingsWindow_Details_Principle5);
            
            details.Add("");
            
            // Effect description
            details.Add(Resource.MenuStyleSettingsWindow_Details_EffectDescription);
            details.Add(Resource.MenuStyleSettingsWindow_Details_EffectAutoTheme);
            details.Add(Resource.MenuStyleSettingsWindow_Details_EffectTransparency);
            details.Add(Resource.MenuStyleSettingsWindow_Details_EffectRoundedCorners);
            details.Add(Resource.MenuStyleSettingsWindow_Details_EffectShadows);
            
            return details;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}