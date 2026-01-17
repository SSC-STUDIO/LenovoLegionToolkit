using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.ViveTool.Resources;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using LenovoLegionToolkit.Plugins.ViveTool.Services.Settings;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.ViveTool;

public partial class ViveToolSettingsPage
{
    private readonly IViveToolService _viveToolService;
    private readonly Services.Settings.ViveToolSettings _settings;

    public ViveToolSettingsPage()
    {
        InitializeComponent();
        _viveToolService = new ViveToolService();
        _settings = new Services.Settings.ViveToolSettings();
        Loaded += ViveToolSettingsPage_Loaded;
    }

    private async void ViveToolSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settings.LoadAsync();
            
            if (_viveToolPathTextBox != null)
            {
                _viveToolPathTextBox.Text = _settings.ViveToolPath ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading ViveTool settings: {ex.Message}", ex);
        }
    }

    private async void BrowseViveToolButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.ViveTool_SelectViveTool,
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedPath = openFileDialog.FileName;
                var fileName = Path.GetFileName(selectedPath);
                
                if (!fileName.Equals(ViveToolService.ViveToolExeName, StringComparison.OrdinalIgnoreCase))
                {
                    System.Windows.MessageBox.Show(Resource.ViveTool_InvalidViveToolFile, Resource.ViveTool_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var success = await _viveToolService.SetViveToolPathAsync(selectedPath).ConfigureAwait(false);
                
                if (success)
                {
                    _viveToolPathTextBox.Text = selectedPath;
                }
                else
                {
                    System.Windows.MessageBox.Show(string.Format(Resource.ViveTool_SetPathFailed, string.Empty), Resource.ViveTool_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error browsing for vivetool.exe: {ex.Message}", ex);
            System.Windows.MessageBox.Show(string.Format(Resource.ViveTool_BrowseError, ex.Message), Resource.ViveTool_Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
