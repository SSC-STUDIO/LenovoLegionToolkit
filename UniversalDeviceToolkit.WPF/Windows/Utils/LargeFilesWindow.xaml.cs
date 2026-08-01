using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UniversalDeviceToolkit.ViewModels.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class LargeFilesWindow : BaseWindow
{
    private readonly List<FileInfo> _allFiles;
    private readonly ObservableCollection<LargeFileViewModel> _visibleFiles = [];
    private readonly DebounceDispatcher _customSizeFilterDebouncer = new();
    private long _currentMinSize = 1024L * 1024 * 1024; // 1GB

    public List<FileInfo> SelectedFiles { get; private set; } = [];

    public LargeFilesWindow(List<FileInfo> allFiles)
    {
        _allFiles = allFiles;
        InitializeComponent();
        _filesDataGrid.ItemsSource = _visibleFiles;
        UpdateVisibleFiles();
    }

    private void UpdateVisibleFiles()
    {
        _visibleFiles.Clear();
        foreach (var fi in _allFiles.Where(f => f.Length >= _currentMinSize).OrderByDescending(f => f.Length))
        {
            _visibleFiles.Add(new LargeFileViewModel(fi));
        }
        UpdateTotalSelected();
    }

    private void UpdateTotalSelected()
    {
        var selectedCount = _visibleFiles.Count(f => f.IsSelected);
        var totalSize = _visibleFiles.Where(f => f.IsSelected).Sum(f => f.Size);
        _totalSelectedText.Text = string.Format(Resource.LargeFilesWindow_SelectedFiles, selectedCount, FormatBytes(totalSize));
    }

    private void SizeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sizeFilterComboBox.SelectedItem is ComboBoxItem item)
        {
            if (item.Tag.ToString() == "custom")
            {
                _customSizeTextBox.Visibility = Visibility.Visible;
                _customSizeUnit.Visibility = Visibility.Visible;
            }
            else
            {
                _customSizeTextBox.Visibility = Visibility.Collapsed;
                _customSizeUnit.Visibility = Visibility.Collapsed;
                if (long.TryParse(item.Tag.ToString(), out var size))
                {
                    _currentMinSize = size;
                    UpdateVisibleFiles();
                }
            }
        }
    }

    private void CustomSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        => _customSizeFilterDebouncer.Debounce(400, ApplyCustomSizeFilter);

    private void ApplyCustomSizeFilter()
    {
        if (double.TryParse(_customSizeTextBox.Text, out var gb))
        {
            _currentMinSize = (long)(gb * 1024 * 1024 * 1024);
            UpdateVisibleFiles();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedFiles = _visibleFiles.Where(f => f.IsSelected).Select(f => f.FileInfo).ToList();
        DialogResult = true;
        Close();
    }

    private string FormatBytes(long bytes)
    {
        string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
        int i;
        double dblSByte = bytes;
        for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblSByte = bytes / 1024.0;
        }

        return $"{dblSByte:0.##} {Suffix[i]}";
    }
}
}
