using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;

namespace UniversalDeviceToolkit.Lib.Automation.Steps;

public class ShowMainWindowAutomationStep : IAutomationStep
{
    [JsonConstructor]
    public ShowMainWindowAutomationStep() { }

    public Task<bool> IsSupportedAsync() => Task.FromResult(true);

    public Task RunAsync(AutomationContext context, AutomationEnvironment environment, CancellationToken token)
    {
        MessagingCenter.Publish(new MainWindowVisibilityMessage(MainWindowVisibilityAction.Show));
        return Task.CompletedTask;
    }

    IAutomationStep IAutomationStep.DeepCopy() => new ShowMainWindowAutomationStep();
}
