using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.Template;

/// <summary>
/// Plugin Page - Main UI for the plugin
/// </summary>
public class MyPluginTemplatePage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string? PageIcon => null;

    public object CreatePage()
    {
        return new MyPluginTemplateControl();
    }
}
