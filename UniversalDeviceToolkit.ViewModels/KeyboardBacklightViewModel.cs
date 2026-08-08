using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Abstractions.Hardware;

namespace UniversalDeviceToolkit.ViewModels;

/// <summary>
/// Host-neutral keyboard lighting operations. Hosts adapt their platform service
/// to this contract instead of moving controller state into page code-behind.
/// </summary>
public interface IKeyboardBacklightWorkspace
{
    Task<KeyboardBacklightWorkspaceState?> GetStateAsync(CancellationToken cancellationToken = default);
    Task<bool> ApplyAsync(KeyboardBacklightWorkspaceUpdate update, CancellationToken cancellationToken = default);
    Task<bool> ResetSpectrumProfileAsync(CancellationToken cancellationToken = default);
    Task<bool> ExportSpectrumProfileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> ImportSpectrumProfileAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed record KeyboardBacklightColor(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";
}

public sealed record KeyboardBacklightSpectrumEffect(
    string Type,
    string Speed,
    string Direction,
    string ClockwiseDirection,
    IReadOnlyList<KeyboardBacklightColor> Colors,
    IReadOnlyList<ushort> Keys);

public sealed record KeyboardBacklightRgbPreset(
    string Key,
    string DisplayName,
    bool IsSelected,
    string Effect,
    string Speed,
    string Brightness,
    IReadOnlyList<KeyboardBacklightColor> Zones);

public sealed record KeyboardBacklightWorkspaceState(
    string Mode,
    int Brightness,
    bool LogoEnabled,
    int SelectedProfile,
    IReadOnlyList<KeyboardBacklightSpectrumEffect> SpectrumEffects,
    IReadOnlyList<KeyboardBacklightRgbPreset> RgbPresets,
    string KeyboardLayout = "Ansi",
    string SpectrumLayout = "KeyboardOnly",
    IReadOnlyList<ushort>? KeyboardKeys = null,
    bool IsBlockedByVantage = false);

public sealed record KeyboardBacklightWorkspaceUpdate(
    string Mode,
    int? SelectedProfile = null,
    int? Brightness = null,
    bool? LogoEnabled = null,
    string? RgbPreset = null,
    string? RgbEffect = null,
    string? RgbSpeed = null,
    string? RgbBrightness = null,
    IReadOnlyList<KeyboardBacklightColor>? RgbZones = null,
    IReadOnlyList<KeyboardBacklightSpectrumEffect>? SpectrumEffects = null,
    string? KeyboardLayout = null);

public partial class KeyboardBacklightViewModel : ObservableObject
{
    private readonly IKeyboardBacklightDetectionService _detectionService;
    private readonly IKeyboardBacklightWorkspace? _workspace;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isSpectrumSupported;

    [ObservableProperty]
    private bool _isRGBSupported;

    [ObservableProperty]
    private bool _isNoKeyboardsVisible;

    [ObservableProperty]
    private bool _isAvailable;

    [ObservableProperty]
    private bool _isBlockedByVantage;

    [ObservableProperty]
    private KeyboardBacklightWorkspaceState? _state;

    [ObservableProperty]
    private string? _errorMessage;

    public KeyboardBacklightViewModel(IKeyboardBacklightDetectionService detectionService)
    {
        _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
    }

    public KeyboardBacklightViewModel(
        IKeyboardBacklightDetectionService detectionService,
        IKeyboardBacklightWorkspace workspace)
        : this(detectionService)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <summary>
    /// Loads the complete lighting projection and keeps the last accepted state
    /// when a controller rejects a later edit.
    /// </summary>
    public async Task<KeyboardBacklightWorkspaceState?> LoadWorkspaceAsync(
        CancellationToken cancellationToken = default)
    {
        if (_workspace is null)
        {
            await DetectKeyboardTypeAsync().ConfigureAwait(false);
            return null;
        }

        IsLoading = true;
        ErrorMessage = null;
        IsNoKeyboardsVisible = false;
        try
        {
            var state = await _workspace.GetStateAsync(cancellationToken).ConfigureAwait(false);
            State = state;
            IsAvailable = state is not null;
            IsSpectrumSupported = state?.Mode.Equals("Spectrum", StringComparison.OrdinalIgnoreCase) == true;
            IsRGBSupported = state?.Mode.Equals("RGB", StringComparison.OrdinalIgnoreCase) == true;
            IsBlockedByVantage = state?.IsBlockedByVantage == true;
            IsNoKeyboardsVisible = state is null;
            return state;
        }
        catch (Exception ex)
        {
            State = null;
            IsAvailable = false;
            IsSpectrumSupported = false;
            IsRGBSupported = false;
            IsBlockedByVantage = false;
            IsNoKeyboardsVisible = true;
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DetectKeyboardTypeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        IsSpectrumSupported = false;
        IsRGBSupported = false;
        IsNoKeyboardsVisible = false;

        try
        {
            if (await IsSpectrumSupportedAsync().ConfigureAwait(false))
            {
                IsSpectrumSupported = true;
                return;
            }

            if (await IsRgbSupportedAsync().ConfigureAwait(false))
            {
                IsRGBSupported = true;
                return;
            }

            IsNoKeyboardsVisible = true;
        }
        catch (Exception ex)
        {
            IsNoKeyboardsVisible = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ApplyAsync(
        KeyboardBacklightWorkspaceUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (_workspace is null || update is null)
            return false;

        ErrorMessage = null;
        try
        {
            if (!await _workspace.ApplyAsync(update, cancellationToken).ConfigureAwait(false))
            {
                ErrorMessage = "The keyboard controller rejected this change.";
                return false;
            }

            await LoadWorkspaceAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public Task<bool> SetSpectrumProfileAsync(int profile, CancellationToken cancellationToken = default) =>
        ApplyAsync(new KeyboardBacklightWorkspaceUpdate("Spectrum", SelectedProfile: profile), cancellationToken);

    public Task<bool> SetSpectrumBrightnessAsync(
        double brightness,
        CancellationToken cancellationToken = default) =>
        ApplyAsync(
            new KeyboardBacklightWorkspaceUpdate("Spectrum", Brightness: ClampSpectrumBrightness(brightness)),
            cancellationToken);

    public Task<bool> SetSpectrumLogoAsync(bool enabled, CancellationToken cancellationToken = default) =>
        ApplyAsync(new KeyboardBacklightWorkspaceUpdate("Spectrum", LogoEnabled: enabled), cancellationToken);

    public Task<bool> SetRgbPresetAsync(string preset, CancellationToken cancellationToken = default) =>
        ApplyAsync(new KeyboardBacklightWorkspaceUpdate("RGB", RgbPreset: preset), cancellationToken);

    public Task<bool> ResetSpectrumProfileAsync(CancellationToken cancellationToken = default) =>
        RunWorkspaceOperationAsync(
            token => _workspace?.ResetSpectrumProfileAsync(token) ?? Task.FromResult(false),
            cancellationToken);

    public Task<bool> ExportSpectrumProfileAsync(string filePath, CancellationToken cancellationToken = default) =>
        RunWorkspaceOperationAsync(
            token => _workspace?.ExportSpectrumProfileAsync(filePath, token) ?? Task.FromResult(false),
            cancellationToken);

    public Task<bool> ImportSpectrumProfileAsync(string filePath, CancellationToken cancellationToken = default) =>
        RunWorkspaceOperationAsync(
            token => _workspace?.ImportSpectrumProfileAsync(filePath, token) ?? Task.FromResult(false),
            cancellationToken,
            reloadAfterSuccess: true);

    public async Task<bool> IsSupportedAsync()
    {
        if (_workspace is not null)
            return await LoadWorkspaceAsync().ConfigureAwait(false) is not null;

        if (await IsSpectrumSupportedAsync().ConfigureAwait(false))
            return true;

        return await IsRgbSupportedAsync().ConfigureAwait(false);
    }

    public static int ClampSpectrumBrightness(double brightness) => Math.Clamp((int)brightness, 0, 9);

    private async Task<bool> RunWorkspaceOperationAsync(
        Func<CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken,
        bool reloadAfterSuccess = false)
    {
        if (_workspace is null)
            return false;

        ErrorMessage = null;
        try
        {
            if (!await operation(cancellationToken).ConfigureAwait(false))
            {
                ErrorMessage = "The keyboard controller rejected this change.";
                return false;
            }

            if (reloadAfterSuccess)
                await LoadWorkspaceAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    private async Task<bool> IsSpectrumSupportedAsync()
    {
        try
        {
            return await _detectionService.IsSpectrumSupportedAsync().ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsRgbSupportedAsync()
    {
        try
        {
            return await _detectionService.IsRgbSupportedAsync().ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }
}
