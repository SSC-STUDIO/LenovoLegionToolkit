using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

internal sealed class ShellIntegrationStyleSettingsWindow : Window
{
    public ShellIntegrationStyleSettingsWindow(ShellIntegrationPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        Title = ShellIntegrationText.SettingsPageTitle;
        Width = 880;
        Height = 720;
        MinWidth = 640;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ResolveBrush("ApplicationBackgroundBrush", SystemColors.WindowBrush);
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
            Margin = new Thickness(24)
        };

        stack.Children.Add(new Border
        {
            Margin = new Thickness(0, 0, 0, 18),
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(18),
            Background = ResolveBrush("ControlFillColorDefaultBrush", SystemColors.ControlBrush),
            BorderBrush = ResolveBrush("ControlStrokeColorDefaultBrush", Brushes.Gainsboro),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = ShellIntegrationText.SettingsPageTitle,
                        FontSize = 24,
                        FontWeight = FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Margin = new Thickness(0, 8, 0, 0),
                        Text = ShellIntegrationText.Subtitle,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = ResolveBrush("TextFillColorSecondaryBrush", Brushes.DimGray)
                    },
                    new TextBlock
                    {
                        Margin = new Thickness(0, 12, 0, 0),
                        Text = "Open the live shell style files directly from the plugin workbench. This fallback window stays available when the main application editor is unavailable.",
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = ResolveBrush("TextFillColorSecondaryBrush", Brushes.DimGray)
                    }
                }
            }
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
            Background = ResolveBrush("ControlFillColorDefaultBrush", SystemColors.ControlLightBrush),
            BorderBrush = ResolveBrush("ControlStrokeColorDefaultBrush", Brushes.Gainsboro),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
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
                ? Brushes.IndianRed
                : ResolveBrush("TextFillColorSecondaryBrush", Brushes.DimGray)
        });
        Grid.SetColumn(textPanel, 0);
        grid.Children.Add(textPanel);

        var openButton = new Button
        {
            Content = buttonLabel,
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(14, 7, 14, 7),
            MinWidth = 120,
            Background = ResolveBrush("ControlFillColorSecondaryBrush", SystemColors.ControlBrush),
            BorderBrush = ResolveBrush("ControlStrokeColorDefaultBrush", Brushes.Gainsboro),
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

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }
}
