using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class PlaySoundAutomationStepControl : AbstractAutomationStepControl<PlaySoundAutomationStep>
{
    private string? _path;

    private readonly TextBlock _titleTextBlock = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly Button _openButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.Folder20 },
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0)
    };

    private readonly StackPanel _stackPanel = new()
    {
        Orientation = Orientation.Horizontal
    };

    public PlaySoundAutomationStepControl(PlaySoundAutomationStep automationStep) : base(automationStep)
    {
        Icon = SymbolRegular.MusicNote2Play20;
        Title = Resource.PlaySoundAutomationStepControl_Title;
        Subtitle = Resource.PlaySoundAutomationStepControl_Message;
    }

    public override IAutomationStep CreateAutomationStep()
    {
        return new PlaySoundAutomationStep(_path);
    }

    protected override UIElement GetCustomControl()
    {
        _openButton.Click += (_, _) =>
        {
            var ofd = new OpenFileDialog
            {
                Title = Resource.Import,
                InitialDirectory = @"C:\Windows\Media",
                CheckFileExists = true,
            };

            var result = ofd.ShowDialog() ?? false;
            if (!result)
                return;

            _path = ofd.FileName;
            _titleTextBlock.Text = Path.GetFileName(_path);

            RaiseChanged();
        };

        _stackPanel.Children.Add(_titleTextBlock);
        _stackPanel.Children.Add(_openButton);

        return _stackPanel;
    }

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync()
    {
        _path = AutomationStep.Path;
        _titleTextBlock.Text = Path.GetFileName(_path) ?? "-";
        return Task.CompletedTask;
    }
}

