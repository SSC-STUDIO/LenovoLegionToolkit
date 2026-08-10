using System.Threading.Tasks;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;
using TextBox = UniversalDeviceToolkit.Avalonia.Controls.TextBox;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class NotificationAutomationStepControl : AbstractAutomationStepControl<NotificationAutomationStep>
{
    private readonly TextBox _scriptPath = new()
    {
        Watermark = Resource.NotificationAutomationStepControl_NotificationText,
        Width = 300
    };

    private readonly StackPanel _stackPanel = new();

    public NotificationAutomationStepControl(NotificationAutomationStep step) : base(step)
    {
        Icon = SymbolRegular.Rocket24;
        Title = Resource.NotificationAutomationStepControl_Title;

        SizeChanged += RunAutomationStepControl_SizeChanged;
    }

    private void RunAutomationStepControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;

        var newWidth = e.NewSize.Width / 3;
        _scriptPath.Width = newWidth;
    }

    public override IAutomationStep CreateAutomationStep() => new NotificationAutomationStep(_scriptPath.Text);

    protected override Control GetCustomControl()
    {
        _scriptPath.TextChanged += (_, _) =>
        {
            if (_scriptPath.Text != AutomationStep.Text)
                RaiseChanged();
        };

        _stackPanel.Children.Add(_scriptPath);

        return _stackPanel;
    }

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync()
    {
        _scriptPath.Text = AutomationStep.Text ?? string.Empty;
        return Task.CompletedTask;
    }
}
