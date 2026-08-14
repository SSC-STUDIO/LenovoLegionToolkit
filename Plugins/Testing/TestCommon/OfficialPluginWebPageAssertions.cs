using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.TestCommon;

public static class OfficialPluginWebPageAssertions
{
    public static string FindOfficialPluginRoot(string folderName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var sibling = Path.Combine(dir.FullName, folderName);
            if (File.Exists(Path.Combine(sibling, "plugin.manifest.json")))
                return sibling;

            var nested = Path.Combine(dir.FullName, "Plugins", "Official", folderName);
            if (File.Exists(Path.Combine(nested, "plugin.manifest.json")))
                return nested;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate Plugins/Official/{folderName}.");
    }

    public static string ReadManifestText(string folderName) =>
        File.ReadAllText(Path.Combine(FindOfficialPluginRoot(folderName), "plugin.manifest.json"));

    public static string ReadWebPageHtml(string folderName) =>
        File.ReadAllText(Path.Combine(FindOfficialPluginRoot(folderName), "web", "index.html"));

    public static void AssertManifestDeclaresWebPage(string folderName)
    {
        var root = FindOfficialPluginRoot(folderName);
        var manifestPath = Path.Combine(root, "plugin.manifest.json");
        var htmlPath = Path.Combine(root, "web", "index.html");
        var cssPath = Path.Combine(root, "web", "plugin-ui.css");

        Assert.True(File.Exists(manifestPath));
        Assert.True(File.Exists(htmlPath));
        Assert.True(File.Exists(cssPath));

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var contributes = doc.RootElement.GetProperty("contributes");
        Assert.Equal("web/index.html", contributes.GetProperty("webPage").GetProperty("entry").GetString());
        Assert.Equal(JsonValueKind.Null, contributes.GetProperty("settingsPage").ValueKind);
    }
}
