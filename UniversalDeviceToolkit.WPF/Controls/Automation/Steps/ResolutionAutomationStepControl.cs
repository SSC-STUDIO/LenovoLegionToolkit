using System;
using System.Windows;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class ResolutionAutomationStepControl : AbstractComboBoxAutomationStepCardControl<Resolution>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    public ResolutionAutomationStepControl(IAutomationStep<Resolution> step) : base(step)
    {
        Icon = SymbolRegular.ScaleFill24;
        Title = Resource.ResolutionAutomationStepControl_Title;
        Subtitle = Resource.ResolutionAutomationStepControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += ResolutionAutomationStepControl_Unloaded;
    }

    private void ResolutionAutomationStepControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _listener.Changed -= Listener_Changed;
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh resolution automation step");
}
