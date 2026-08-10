using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;
using TextBox = UniversalDeviceToolkit.Avalonia.Controls.TextBox;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class SpectrumKeyboardBacklightImportProfileAutomationStepControl : AbstractAutomationStepControl<SpectrumKeyboardBacklightImportProfileAutomationStep>
{
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private readonly TextBox _path = new()
    {
        Watermark = Resource.SpectrumKeyboardBacklightImportProfileAutomationStepControl_Path,
        Width = 300
    };

    private readonly Button _openButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.MoreHorizontal24 },
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0)
    };

    private readonly StackPanel _stackPanel = new()
    {
        Orientation = Orientation.Horizontal
    };

    public SpectrumKeyboardBacklightImportProfileAutomationStepControl(SpectrumKeyboardBacklightImportProfileAutomationStep step) : base(step)
    {
        Icon = SymbolRegular.BrightnessHigh24;
        Title = Resource.SpectrumKeyboardBacklightImportProfileAutomationStepControl_Title;
        Subtitle = Resource.SpectrumKeyboardBacklightImportProfileAutomationStepControl_Message;

        SizeChanged += RunAutomationStepControl_SizeChanged;
    }

    private void RunAutomationStepControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;

        var newWidth = e.NewSize.Width / 3;
        _path.Width = newWidth;
    }

    public override IAutomationStep CreateAutomationStep() => new SpectrumKeyboardBacklightImportProfileAutomationStep(_path.Text);

    protected override Control GetCustomControl()
    {
        _path.TextChanged += (_, _) =>
        {
            if (_path.Text != AutomationStep.Path)
                RaiseChanged();
        };

        _openButton.Click += (_, _) =>
        {
            var ofd = new System.Windows.Forms.OpenFileDialog
            {
                Title = Resource.Import,
                InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
                Filter = T("Common_JsonFileDialogFilter", "Json Files (.json)|*.json"),
                CheckFileExists = true,
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            _path.Text = ofd.FileName;
        };

        _stackPanel.Children.Add(_path);
        _stackPanel.Children.Add(_openButton);

        return _stackPanel;
    }

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync()
    {
        _path.Text = AutomationStep.Path ?? string.Empty;
        return Task.CompletedTask;
    }
}
