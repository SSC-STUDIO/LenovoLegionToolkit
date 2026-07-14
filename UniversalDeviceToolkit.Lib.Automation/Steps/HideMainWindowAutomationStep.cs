using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;

namespace UniversalDeviceToolkit.Lib.Automation.Steps;

public class HideMainWindowAutomationStep : IAutomationStep
{
    [JsonConstructor]
    public HideMainWindowAutomationStep() { }

    public Task<bool> IsSupportedAsync() => Task.FromResult(true);

    public Task RunAsync(AutomationContext context, AutomationEnvironment environment, CancellationToken token)
    {
        MessagingCenter.Publish(new MainWindowVisibilityMessage(MainWindowVisibilityAction.Hide));
        return Task.CompletedTask;
    }

    IAutomationStep IAutomationStep.DeepCopy() => new HideMainWindowAutomationStep();
}
