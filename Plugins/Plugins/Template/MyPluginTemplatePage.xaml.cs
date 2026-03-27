using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.Template;

/// <summary>
/// Plugin Page - Main UI for the plugin
/// </summary>
public class MyPluginTemplatePage : IPluginPage
{
    public string PageTitle => "Feature Page Title";
    public string? PageIcon => "Apps24";

    public object CreatePage()
    {
        return new MyPluginTemplateControl();
    }
}
