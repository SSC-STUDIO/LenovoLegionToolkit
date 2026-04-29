using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
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
        AutomationProperties.SetAutomationId(this, "ShellIntegrationStyleSettingsWindow");
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
            Margin = new Thickness(20)
        };

        var titleTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 18),
            Text = ShellIntegrationText.SettingsPageTitle,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetAutomationId(titleTextBlock, "ShellIntegrationStyleSettingsTitle");
        stack.Children.Add(titleTextBlock);

        stack.Children.Add(CreatePathRow("shell.nss", configPath, "Open File"));
        stack.Children.Add(CreatePathRow("theme.nss", themePath, "Open File"));
        stack.Children.Add(CreatePathRow("images.nss", imagesPath, "Open File"));
        stack.Children.Add(CreatePathRow("modify.nss", modifyPath, "Open File"));
        stack.Children.Add(CreatePathRow("imports", importsFolder, "Open Folder", isDirectory: true));
        stack.Children.Add(CreatePathRow("Shell Folder", shellFolder, ShellIntegrationText.OpenShellFolderButton, isDirectory: true, isLast: true));

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stack
        };
    }

    private static UIElement CreatePathRow(string title, string? path, string buttonLabel, bool isDirectory = false, bool isLast = false)
    {
        var automationSegment = NormalizeAutomationSegment(title);
        var border = new Border
        {
            BorderBrush = ResolveBrush("ControlStrokeColorDefaultBrush", Brushes.Gainsboro),
            BorderThickness = isLast ? new Thickness(0) : new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 12, 0, 12)
        };
        AutomationProperties.SetAutomationId(border, $"ShellIntegrationStyleRow_{automationSegment}");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textPanel = new StackPanel();
        var titleTextBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold
        };
        AutomationProperties.SetAutomationId(titleTextBlock, $"ShellIntegrationStyleTitle_{automationSegment}");
        textPanel.Children.Add(titleTextBlock);

        var pathTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Text = string.IsNullOrWhiteSpace(path) ? ShellIntegrationText.NotFound : path,
            TextWrapping = TextWrapping.Wrap,
            Foreground = string.IsNullOrWhiteSpace(path)
                ? ResolveBrush("SystemFillColorCriticalBrush", Brushes.IndianRed)
                : ResolveBrush("TextFillColorSecondaryBrush", Brushes.DimGray)
        };
        AutomationProperties.SetAutomationId(pathTextBlock, $"ShellIntegrationStylePath_{automationSegment}");
        textPanel.Children.Add(pathTextBlock);
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
        AutomationProperties.SetAutomationId(openButton, $"ShellIntegrationStyleOpen_{automationSegment}");
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

    private static string NormalizeAutomationSegment(string value)
    {
        var chars = value
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return chars.Length == 0 ? "Path" : new string(chars);
    }
}
