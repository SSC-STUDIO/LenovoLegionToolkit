using System.ComponentModel;
using System.IO;

namespace UniversalDeviceToolkit.ViewModels.Utils;

/// <summary>
/// Row model for the large-files picker window. Pure BCL — portable across
/// all platforms (moved from WPF LargeFilesWindow code-behind).
/// </summary>
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
