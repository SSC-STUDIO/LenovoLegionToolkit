using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public partial class LargeFilesWindow : BaseWindow
{
    private readonly List<FileInfo> _allFiles;
    private readonly ObservableCollection<LargeFileViewModel> _visibleFiles = [];
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
        _totalSelectedText.Text = $"Selected: {selectedCount} files ({FormatBytes(totalSize)})";
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

    public class LargeFileViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        public FileInfo FileInfo { get; }

        public LargeFileViewModel(FileInfo fi)
        {
            FileInfo = fi;
            Name = fi.Name;
            Size = fi.Length;
            Directory = fi.DirectoryName ?? string.Empty;
            SizeDisplay = FormatBytes(fi.Length);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public string Name { get; }
        public long Size { get; }
        public string SizeDisplay { get; }
        public string Directory { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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