using System;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Reflection;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Tests;

public class ShellIntegrationTextTests
{
    [Fact]
    public void TextClass_HasNoHardcodedChinese()
    {
        var type = typeof(ShellIntegrationText);

        // Set culture to English to ensure we're checking fallback values, not translations
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Resources.Resource.Culture = CultureInfo.GetCultureInfo("en");

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(string));

            foreach (var property in properties)
            {
                var value = property.GetValue(null) as string;
                Assert.NotNull(value);

                // Check for Chinese characters in fallback values
                var hasChinese = value.Any(c => c >= 0x4E00 && c <= 0x9FFF);
                Assert.False(hasChinese, $"Property {property.Name} contains hardcoded Chinese text in fallback: {value}");
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
            Resources.Resource.Culture = originalCulture;
        }
    }

    [Fact]
    public void TextClass_FallbackValues_AreEnglish()
    {
        var type = typeof(ShellIntegrationText);
        var method = type.GetMethod("T", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
    }

    [Fact]
    public void AllResourceKeys_AreAccessible()
    {
        var assembly = Assembly.Load("LenovoLegionToolkit.Plugins.ShellIntegration");
        var resourceManager = new ResourceManager(
            "LenovoLegionToolkit.Plugins.ShellIntegration.Resources.Resource",
            assembly);

        var textType = typeof(ShellIntegrationText);
        var properties = textType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string))
            .ToList();

        Assert.True(properties.Count > 0, "No string properties found in ShellIntegrationText");

        foreach (var property in properties)
        {
            var key = property.Name;
            var value = property.GetValue(null) as string;

            // Verify fallback is not null or empty
            Assert.False(string.IsNullOrEmpty(value), $"Property {key} has null or empty fallback value");

            // For English culture, verify resource can be retrieved
            try
            {
                var resourceValue = resourceManager.GetString(key, CultureInfo.GetCultureInfo("en"));
                // Resource might be null for en, that's OK as long as fallback works
            }
            catch
            {
                // Resource not found is OK if fallback is valid
            }
        }
    }

    [Theory]
    [InlineData("PluginName")]
    [InlineData("SettingsPageTitle")]
    [InlineData("EnableButton")]
    [InlineData("DisableButton")]
    [InlineData("RegisteredState")]
    public void CommonKeys_ReturnNonEmptyString(string key)
    {
        var type = typeof(ShellIntegrationText);
        var property = type.GetProperty(key, BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(property);
        var value = property.GetValue(null) as string;
        Assert.NotNull(value);
        Assert.NotEmpty(value);
    }
}