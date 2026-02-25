using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.Template;

/// <summary>
/// MyPluginTemplateControl.xaml 的交互逻辑
/// </summary>
public partial class MyPluginTemplateControl : UserControl
{
    public MyPluginTemplateControl()
    {
        InitializeComponent();
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Hello from My Plugin!");
    }
}
