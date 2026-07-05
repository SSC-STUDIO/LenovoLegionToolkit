using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class ErrorDialogWindow
{
    public new bool? DialogResult { get; private set; }
    public bool ShouldExit => DialogResult == true;

    public ErrorDialogWindow(string exceptionMessage, Window? owner = null)
    {
        InitializeComponent();

        if (owner != null)
        {
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        _continueButton.Content = Resource.Continue;
        _exitButton.Content = Resource.Exit;
        AutomationProperties.SetName(_continueButton, Resource.Continue);
        AutomationProperties.SetName(_exitButton, Resource.Exit);

        _exceptionText.Text = exceptionMessage;

        _continueButton.Click += (s, e) =>
        {
            DialogResult = false;
            Close();
        };

        _exitButton.Click += (s, e) =>
        {
            DialogResult = true;
            Close();
        };

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    public ErrorDialogWindow()
    {
        InitializeComponent();
    }
}
}
