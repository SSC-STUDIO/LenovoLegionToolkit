using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.Template;

/// <summary>
/// Settings Page - Plugin configuration UI
/// </summary>
public class MyPluginTemplateSettingsPage : IPluginPage
{
    public string PageTitle => "Settings";
    public string? PageIcon => "Settings24";

    public object CreatePage()
    {
        return new MyPluginTemplateSettingsControl();
    }
}
