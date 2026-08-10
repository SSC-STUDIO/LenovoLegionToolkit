using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;
using TextBox = UniversalDeviceToolkit.Avalonia.Controls.TextBox;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class RunAutomationStepControl : AbstractAutomationStepControl<RunAutomationStep>
{
    private readonly TextBox _scriptPath = new()
    {
        Watermark = Resource.RunAutomationStepControl_ExePath,
        Width = 300,
        Margin = new(0, 0, 0, 8),
    };

    private readonly TextBox _scriptArguments = new()
    {
        Watermark = Resource.RunAutomationStepControl_ExeArguments,
        Width = 300,
        Margin = new(0, 0, 0, 8),
    };

    private readonly CheckBox _checkBoxProcessRunSilently = new()
    {
        Content = Resource.RunAutomationStepControl_ProcessRunSilently,
    };

    private readonly CheckBox _checkBoxProcessWaitUntilFinished = new()
    {
        Content = Resource.RunAutomationStepControl_ProcessWaitUntilFinished,
    };

    private readonly StackPanel _stackPanel = new();

    public RunAutomationStepControl(RunAutomationStep step) : base(step)
    {
        Icon = SymbolRegular.WindowConsole20;
        Title = Resource.RunAutomationStepControl_Title;
        Subtitle = Resource.RunAutomationStepControl_Message;
        TitleVerticalAlignment = VerticalAlignment.Bottom;
        SubtitleVerticalAlignment = VerticalAlignment.Top;

        AutomationProperties.SetName(_scriptPath, Resource.RunAutomationStepControl_ExePath);
        AutomationProperties.SetName(_scriptArguments, Resource.RunAutomationStepControl_ExeArguments);
        AutomationProperties.SetName(_checkBoxProcessRunSilently, Resource.RunAutomationStepControl_ProcessRunSilently);
        AutomationProperties.SetName(_checkBoxProcessWaitUntilFinished, Resource.RunAutomationStepControl_ProcessWaitUntilFinished);

        ToolTip.SetTip(_checkBoxProcessRunSilently, Resource.RunAutomationStepControl_ProcessRunSilently_Description);
        ToolTip.SetTip(_checkBoxProcessWaitUntilFinished, Resource.RunAutomationStepControl_ProcessWaitUntilFinished_Description);

        SizeChanged += RunAutomationStepControl_SizeChanged;
    }

    private void RunAutomationStepControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;

        var newWidth = e.NewSize.Width / 3;
        _scriptPath.Width = newWidth;
        _scriptArguments.Width = newWidth;
    }

    public override IAutomationStep CreateAutomationStep()
    {
        return new RunAutomationStep(_scriptPath.Text,
            _scriptArguments.Text,
            _checkBoxProcessRunSilently.IsChecked ?? true,
            _checkBoxProcessWaitUntilFinished.IsChecked ?? true);
    }

    protected override Control GetCustomControl()
    {
        _scriptPath.TextChanged += (_, _) =>
        {
            if (_scriptPath.Text != AutomationStep.ScriptPath)
                RaiseChanged();
        };
        _scriptArguments.TextChanged += (_, _) =>
        {
            if (_scriptArguments.Text != AutomationStep.ScriptArguments)
                RaiseChanged();
        };
        _checkBoxProcessRunSilently.Checked += (_, _) =>
        {
            if (_checkBoxProcessRunSilently.IsChecked != AutomationStep.RunSilently)
                RaiseChanged();
        };
        _checkBoxProcessWaitUntilFinished.Checked += (_, _) =>
        {
            if (_checkBoxProcessWaitUntilFinished.IsChecked != AutomationStep.WaitUntilFinished)
                RaiseChanged();
        };
        _checkBoxProcessRunSilently.Unchecked += (_, _) =>
        {
            if (_checkBoxProcessRunSilently.IsChecked != AutomationStep.RunSilently)
                RaiseChanged();
        };
        _checkBoxProcessWaitUntilFinished.Unchecked += (_, _) =>
        {
            if (_checkBoxProcessWaitUntilFinished.IsChecked != AutomationStep.WaitUntilFinished)
                RaiseChanged();
        };

        _stackPanel.Children.Add(_scriptPath);
        _stackPanel.Children.Add(_scriptArguments);
        _stackPanel.Children.Add(_checkBoxProcessRunSilently);
        _stackPanel.Children.Add(_checkBoxProcessWaitUntilFinished);

        return _stackPanel;
    }

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync()
    {
        _scriptPath.Text = AutomationStep.ScriptPath ?? string.Empty;
        _scriptArguments.Text = AutomationStep.ScriptArguments ?? string.Empty;
        _checkBoxProcessRunSilently.IsChecked = AutomationStep.RunSilently;
        _checkBoxProcessWaitUntilFinished.IsChecked = AutomationStep.WaitUntilFinished;
        return Task.CompletedTask;
    }
}
