using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Utils;
using System;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.WPF.ViewModels;

public partial class KeyboardBacklightViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isSpectrumSupported;

    [ObservableProperty]
    private bool _isRGBSupported;

    [ObservableProperty]
    private bool _isNoKeyboardsVisible;

    [RelayCommand]
    private async Task DetectKeyboardTypeAsync()
    {
        IsLoading = true;
        IsSpectrumSupported = false;
        IsRGBSupported = false;
        IsNoKeyboardsVisible = false;

        try
        {
            var spectrumController = IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
            if (await spectrumController.IsSupportedAsync())
            {
                IsSpectrumSupported = true;
                IsLoading = false;
                return;
            }

            var rgbController = IoCContainer.Resolve<RGBKeyboardBacklightController>();
            if (await rgbController.IsSupportedAsync())
            {
                IsRGBSupported = true;
                IsLoading = false;
                return;
            }

            IsNoKeyboardsVisible = true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to detect keyboard type", ex);

            IsNoKeyboardsVisible = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public static async Task<bool> IsSupportedAsync()
    {
        var spectrumController = IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
        if (await spectrumController.IsSupportedAsync())
            return true;

        var rgbController = IoCContainer.Resolve<RGBKeyboardBacklightController>();
        if (await rgbController.IsSupportedAsync())
            return true;

        return false;
    }
}
