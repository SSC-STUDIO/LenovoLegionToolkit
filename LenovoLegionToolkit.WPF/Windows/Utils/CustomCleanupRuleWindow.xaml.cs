using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public partial class CustomCleanupRuleWindow
{
    public CustomCleanupRule? Result { get; private set; }

    public CustomCleanupRuleWindow(CustomCleanupRule? existingRule = null)
    {
        InitializeComponent();

        if (existingRule is not null)
        {
            _folderTextBox.Text = existingRule.DirectoryPath;
            _extensionsTextBox.Text = string.Join(", ", existingRule.Extensions ?? new List<string>());
            _recursiveCheckBox.IsChecked = existingRule.Recursive;
        }
        else
        {
            _recursiveCheckBox.IsChecked = true;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrWhiteSpace(_folderTextBox.Text))
            dialog.SelectedPath = _folderTextBox.Text.Trim();

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            _folderTextBox.Text = dialog.SelectedPath;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = (_folderTextBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageBox.Show(Resource.CustomCleanupRuleWindow_Error_Folder, Resource.CustomCleanupRuleWindow_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var extensions = ParseExtensions(_extensionsTextBox.Text);
        if (extensions.Count == 0)
        {
            MessageBox.Show(Resource.CustomCleanupRuleWindow_Error_Extensions, Resource.CustomCleanupRuleWindow_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new CustomCleanupRule
        {
            DirectoryPath = folder,
            Recursive = _recursiveCheckBox.IsChecked == true,
            Extensions = extensions
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static List<string> ParseExtensions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        return raw
            .Split(new[] { ',', ';', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ext =>
            {
                var trimmed = ext.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    return string.Empty;

                return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : "." + trimmed;
            })
            .Where(ext => !string.IsNullOrEmpty(ext))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

