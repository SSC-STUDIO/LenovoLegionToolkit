using LenovoLegionToolkit.Plugins.SDK;
using Xunit;

namespace LenovoLegionToolkit.Plugins.TestCommon;

internal static class PluginPageAssertions
{
    public static IPluginPage AssertPluginPage(object? page, string? expectedTitle = null, string? expectedIcon = null)
    {
        var pluginPage = Assert.IsAssignableFrom<IPluginPage>(page);
        AssertPageMetadata(pluginPage, expectedTitle, expectedIcon);
        return pluginPage;
    }

    public static TPage AssertPluginPage<TPage>(object? page, string? expectedTitle = null, string? expectedIcon = null)
        where TPage : class, IPluginPage
    {
        var pluginPage = Assert.IsType<TPage>(page);
        AssertPageMetadata(pluginPage, expectedTitle, expectedIcon);
        return pluginPage;
    }

    private static void AssertPageMetadata(IPluginPage page, string? expectedTitle, string? expectedIcon)
    {
        if (expectedTitle is not null)
            Assert.Equal(expectedTitle, page.PageTitle);

        if (expectedIcon is not null)
            Assert.Equal(expectedIcon, page.PageIcon);
    }
}
