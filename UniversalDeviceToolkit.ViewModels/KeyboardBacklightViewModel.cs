using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Abstractions.Hardware;

namespace UniversalDeviceToolkit.ViewModels;

public partial class KeyboardBacklightViewModel : ObservableObject
{
    private readonly IKeyboardBacklightDetectionService _detectionService;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isSpectrumSupported;

    [ObservableProperty]
    private bool _isRGBSupported;

    [ObservableProperty]
    private bool _isNoKeyboardsVisible;

    public KeyboardBacklightViewModel(IKeyboardBacklightDetectionService detectionService)
    {
        _detectionService = detectionService;
    }

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
        catch (Exception)
        {
            IsNoKeyboardsVisible = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> IsSupportedAsync()
    {
        if (await IsSpectrumSupportedAsync())
            return true;

        return await IsRgbSupportedAsync();
    }

    private async Task<bool> IsSpectrumSupportedAsync()
    {
        try
        {
            return await _detectionService.IsSpectrumSupportedAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> IsRgbSupportedAsync()
    {
        try
        {
            return await _detectionService.IsRgbSupportedAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }
}
