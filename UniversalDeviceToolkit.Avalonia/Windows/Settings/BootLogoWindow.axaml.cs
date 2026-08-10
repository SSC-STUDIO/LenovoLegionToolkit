using System;
using System.Linq;
using Avalonia;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using Microsoft.Win32;

namespace UniversalDeviceToolkit.Avalonia.Windows.Settings
{
public partial class BootLogoWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public BootLogoWindow()
    {
        InitializeComponent();

        Loaded += BootLogoWindow_Loaded;
    }

    private void BootLogoWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var (enabled, resolution, formats, _) = BootLogo.GetStatus();

        _descriptionTextBlock.Text = string.Format(Resource.BootLogoWindow_Description, resolution.DisplayName, string.Join(", ", formats.Select(f => f.ToString().ToUpperInvariant())));

        _defaultStatus.IsVisible = enabled ? false : true;
        _customStatus.IsVisible = enabled ? true : false;
        _customizeButton.IsVisible = enabled ? false : true;
        _revertToDefaultButton.IsVisible = enabled ? true : false;
    }

    private async void RevertToDefaultButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _revertToDefaultButton.IsEnabled = false;

            await BootLogo.DisableAsync();

            ShowResult(Resource.BootLogoWindow_SetDefaultSuccess, success: true);

            Refresh();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Default logo could not be set.", ex);
            ShowResult(string.Format(Resource.BootLogoWindow_SetDefaultFailed, GetDescription(ex)), success: false);
        }
        finally
        {
            _revertToDefaultButton.IsEnabled = true;
        }
    }

    private async void CustomizeButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _customizeButton.IsEnabled = false;

            var (_, _, _, filters) = BootLogo.GetStatus();

            var ofd = new System.Windows.Forms.OpenFileDialog
            {
                Title = Resource.Open,
                InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
                Filter = string.Format(T("Common_ImageFileDialogFilterFormat", "Images|{0}"), string.Join(";", filters)),
                CheckFileExists = true,
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            var file = ofd.FileName;

            await BootLogo.EnableAsync(file);

            ShowResult(Resource.BootLogoWindow_SetCustomSuccess, success: true);

            Refresh();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Custom logo could not be set.", ex);

            ShowResult(string.Format(Resource.BootLogoWindow_SetCustomFailed, GetDescription(ex)), success: false);
        }
        finally
        {
            _customizeButton.IsEnabled = true;
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void ShowResult(string text, bool success)
    {
        _resultTextBlock.Text = text;
        var key = success ? "StatusSuccessBrush" : "StatusCriticalTextBrush";
        if (this.TryFindResource(key) is global::Avalonia.Media.IBrush resultBrush)
            _resultTextBlock.Foreground = resultBrush;
    }

    private static string GetDescription(Exception exception) => exception switch
    {
        CantSetUEFIPrivilegeException => Resource.BootLogoWindow_SetError_Cannot_Set_UEFI_Privilege,
        CantMountUEFIPartitionException => Resource.BootLogoWindow_SetError_Cannot_Mount_EFI_Partition,
        NotEnoughSpaceOnUEFIPartitionException => Resource.BootLogoWindow_SetError_Not_Enough_Free_Space_On_EFI_Partition,
        InvalidBootLogoImageSizeException => Resource.BootLogoWindow_SetError_Invalid_Image_Size,
        InvalidBootLogoImageFormatException => Resource.BootLogoWindow_SetError_Invalid_Image_Format,
        _ => exception.Message
    };
}
}
