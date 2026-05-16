using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoLegionToolkit.Lib.Automation;
using LenovoLegionToolkit.Lib.Automation.Pipeline;
using LenovoLegionToolkit.Lib.Automation.Pipeline.Triggers;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.WPF.ViewModels;

public partial class AutomationViewModel : ObservableObject
{
    private readonly AutomationProcessor _automationProcessor;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    private IAutomationStep[] _supportedAutomationSteps = [];

    [ObservableProperty]
    private bool _enableHybridModeAutomation;

    public AutomationViewModel(AutomationProcessor automationProcessor)
    {
        _automationProcessor = automationProcessor;
    }

    public AutomationProcessor Processor => _automationProcessor;

    [RelayCommand]
    private async Task ToggleEnabledAsync(bool? isChecked)
    {
        if (isChecked.HasValue)
            await _automationProcessor.SetEnabledAsync(isChecked.Value);
    }

    [RelayCommand]
    private async Task ReloadPipelinesAsync(List<AutomationPipeline> pipelines)
    {
        IsSaving = true;
        try
        {
            await _automationProcessor.ReloadPipelinesAsync(pipelines);
            HasUnsavedChanges = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task RefreshStateAsync()
    {
        IsEnabled = _automationProcessor.IsEnabled;
        HasUnsavedChanges = false;
        await Task.CompletedTask;
    }

    public void MarkChanged() => HasUnsavedChanges = true;

    public async Task<IAutomationStep[]> GetSupportedAutomationStepsAsync()
    {
        if (!_supportedAutomationSteps.IsEmpty())
            return _supportedAutomationSteps;

        _supportedAutomationSteps = await LoadSupportedAutomationStepsAsync(EnableHybridModeAutomation);
        return _supportedAutomationSteps;
    }

    private async Task<IAutomationStep[]> LoadSupportedAutomationStepsAsync(bool enableHybridMode)
    {
        var steps = new List<IAutomationStep>
        {
            new AlwaysOnUsbAutomationStep(default),
            new BatteryAutomationStep(default),
            new BatteryNightChargeAutomationStep(default),
            new DeactivateGPUAutomationStep(default),
            new DelayAutomationStep(default),
            new DisplayBrightnessAutomationStep(50),
            new DpiScaleAutomationStep(default),
            new FlipToStartAutomationStep(default),
            new FnLockAutomationStep(default),
            new GodModePresetAutomationStep(default),
            new HDRAutomationStep(default),
            new InstantBootAutomationStep(default),
            new MacroAutomationStep(default),
            new MicrophoneAutomationStep(default),
            new SpeakerAutomationStep(default),
            new NotificationAutomationStep(default),
            new OneLevelWhiteKeyboardBacklightAutomationStep(default),
            new OverclockDiscreteGPUAutomationStep(default),
            new OverDriveAutomationStep(default),
            new PanelLogoBacklightAutomationStep(default),
            new PlaySoundAutomationStep(default),
            new PortsBacklightAutomationStep(default),
            new PowerModeAutomationStep(default),
            new QuickActionAutomationStep(default),
            new RefreshRateAutomationStep(default),
            new ResolutionAutomationStep(default),
            new RGBKeyboardBacklightAutomationStep(default),
            new RunAutomationStep(default, default, default, default),
            new SpectrumKeyboardBacklightBrightnessAutomationStep(0),
            new SpectrumKeyboardBacklightProfileAutomationStep(1),
            new SpectrumKeyboardBacklightImportProfileAutomationStep(default),
            new TouchpadLockAutomationStep(default),
            new TurnOffMonitorsAutomationStep(),
            new TurnOffWiFiAutomationStep(),
            new TurnOnWiFiAutomationStep(),
            new WhiteKeyboardBacklightAutomationStep(default),
            new WinKeyAutomationStep(default)
        };

        if (enableHybridMode)
            steps.Add(new HybridModeAutomationStep(default));

        for (var index = steps.Count - 1; index >= 0; index--)
        {
            if (!await steps[index].IsSupportedAsync())
                steps.RemoveAt(index);
        }

        return [.. steps];
    }

    public HashSet<System.Type> GetExistingTriggerTypes(IEnumerable<AutomationPipeline> pipelines)
    {
        return pipelines
            .Select(p => p.Trigger)
            .Where(t => t is not null)
            .Select(t => t!.GetType())
            .ToHashSet();
    }
}
