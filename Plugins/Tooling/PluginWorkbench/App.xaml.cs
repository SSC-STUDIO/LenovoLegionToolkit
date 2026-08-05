using System.Windows;

namespace PluginWorkbench;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var options = PluginWorkbenchLaunchOptions.Parse(e.Args);
        var mainWindow = new MainWindow(options);
        MainWindow = mainWindow;
        mainWindow.Show();

        base.OnStartup(e);
    }
}
