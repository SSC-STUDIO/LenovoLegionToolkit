using System;
using System.IO;
using System.Windows;
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
                        "Configuration file not found. Please make sure Nilesoft Shell is installed.",
                        "Configuration File Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Create a basic theme configuration based on selections
                var themeConfig = GenerateShellConfig(selectedTheme, transparencyEnabled, roundedCornersEnabled, shadowsEnabled);

                // Write the configuration to the file
                File.WriteAllText(_shellConfigPath, themeConfig);

                MessageBox.Show(
                    "Settings applied successfully. Changes will take effect after restarting File Explorer.",
                    "Settings Applied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to apply settings: {ex.Message}",
                    "Error",
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}