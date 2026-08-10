using System;
using Humanizer;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Windows.Automation.TabItemContent
{
public partial class UserInactivityPipelineTriggerTabItemContent : global::Avalonia.Controls.UserControl, IAutomationPipelineTriggerTabItemContent<IUserInactivityPipelineTrigger>
{
    private static readonly TimeSpan[] TimeSpans =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(3),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30)
    ];

    private readonly IUserInactivityPipelineTrigger _trigger;

    public UserInactivityPipelineTriggerTabItemContent(IUserInactivityPipelineTrigger trigger)
    {
        _trigger = trigger;
        InitializeComponent();

        _timeoutComboBox.SetItems(TimeSpans, trigger.InactivityTimeSpan, t => t.Humanize(culture: System.Globalization.CultureInfo.CurrentUICulture));
    }

    public IUserInactivityPipelineTrigger GetTrigger()
    {
        var state = _timeoutComboBox.TryGetSelectedItem(out TimeSpan tt)
            ? tt
            : TimeSpan.FromSeconds(30);
        return _trigger.DeepCopy(state);
    }
}
}
