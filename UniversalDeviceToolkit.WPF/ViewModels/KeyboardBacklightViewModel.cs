using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Utils;
using System;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.WPF.ViewModels;

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
            if (await IsSpectrumSupportedAsync())
            {
                IsSpectrumSupported = true;
                IsLoading = false;
                return;
            }

            if (await IsRgbSupportedAsync())
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
        if (await IsSpectrumSupportedAsync())
            return true;

        return await IsRgbSupportedAsync();
    }

    private static async Task<bool> IsSpectrumSupportedAsync()
    {
        try
        {
            var spectrumController = IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
            return await spectrumController.IsSupportedAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to check Spectrum keyboard support.", ex);

            return false;
        }
    }

    private static async Task<bool> IsRgbSupportedAsync()
    {
        try
        {
            var rgbController = IoCContainer.Resolve<RGBKeyboardBacklightController>();
            return await rgbController.IsSupportedAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to check RGB keyboard support.", ex);

            return false;
        }
    }
}

