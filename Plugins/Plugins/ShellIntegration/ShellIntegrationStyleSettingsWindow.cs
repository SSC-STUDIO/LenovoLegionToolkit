using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

internal sealed class ShellIntegrationStyleSettingsWindow : Window
{
    public ShellIntegrationStyleSettingsWindow(ShellIntegrationPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        Title = ShellIntegrationText.SettingsPageTitle;
        Width = 760;
        Height = 620;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = BuildContent(plugin);
    }

    private static UIElement BuildContent(ShellIntegrationPlugin plugin)
    {
        var shellFolder = plugin.GetShellFolderPath();
        var configPath = plugin.GetShellConfigPath();
        var importsFolder = string.IsNullOrWhiteSpace(shellFolder) ? null : Path.Combine(shellFolder, "imports");
        var themePath = string.IsNullOrWhiteSpace(importsFolder) ? null : Path.Combine(importsFolder, "theme.nss");
        var imagesPath = string.IsNullOrWhiteSpace(importsFolder) ? null : Path.Combine(importsFolder, "images.nss");
        var modifyPath = string.IsNullOrWhiteSpace(importsFolder) ? null : Path.Combine(importsFolder, "modify.nss");

        var stack = new StackPanel
        {
            Margin = new Thickness(18)
        };

        stack.Children.Add(new TextBlock
        {
            Text = ShellIntegrationText.Subtitle,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 14)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Open the live shell style files directly from the plugin workbench. This fallback editor keeps the plugin usable when the main application style window is unavailable.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 18)
        });

        stack.Children.Add(CreatePathCard("shell.nss", configPath, "Open File"));
        stack.Children.Add(CreatePathCard("theme.nss", themePath, "Open File"));
        stack.Children.Add(CreatePathCard("images.nss", imagesPath, "Open File"));
        stack.Children.Add(CreatePathCard("modify.nss", modifyPath, "Open File"));
        stack.Children.Add(CreatePathCard("imports", importsFolder, "Open Folder", isDirectory: true));
        stack.Children.Add(CreatePathCard("Shell Folder", shellFolder, ShellIntegrationText.OpenShellFolderButton, isDirectory: true));

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stack
        };
    }

    private static UIElement CreatePathCard(string title, string? path, string buttonLabel, bool isDirectory = false)
    {
        var border = new Border
        {
            BorderBrush = System.Windows.Media.Brushes.Gainsboro,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textPanel = new StackPanel();
        textPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold
        });
        textPanel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Text = string.IsNullOrWhiteSpace(path) ? ShellIntegrationText.NotFound : path,
            TextWrapping = TextWrapping.Wrap,
            Foreground = string.IsNullOrWhiteSpace(path)
                ? System.Windows.Media.Brushes.IndianRed
                : System.Windows.Media.Brushes.DimGray
        });
        Grid.SetColumn(textPanel, 0);
        grid.Children.Add(textPanel);

        var openButton = new Button
        {
            Content = buttonLabel,
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(12, 6, 12, 6),
            IsEnabled = !string.IsNullOrWhiteSpace(path)
        };
        openButton.Click += (_, _) => OpenPath(path, isDirectory);
        Grid.SetColumn(openButton, 1);
        grid.Children.Add(openButton);

        border.Child = grid;
        return border;
    }

    private static void OpenPath(string? path, bool isDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var exists = isDirectory ? Directory.Exists(path) : File.Exists(path);
        if (!exists)
        {
            MessageBox.Show(
                isDirectory ? ShellIntegrationText.StatusShellFolderNotFound : ShellIntegrationText.StatusConfigNotFound,
                ShellIntegrationText.SettingsPageTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
