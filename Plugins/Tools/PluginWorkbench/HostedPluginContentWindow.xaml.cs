using System.Windows;

namespace PluginWorkbench;

public partial class HostedPluginContentWindow : Window
{
    public HostedPluginContentWindow(object content, string title)
    {
        InitializeComponent();
        Title = string.IsNullOrWhiteSpace(title) ? "Plugin Dialog" : title;
        DialogTitleTextBlock.Text = Title;
        ContentHost.Content = content;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
