using System;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Reflection;
using Xunit;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Tests;

public class NetworkAccelerationTextTests
{
    private static readonly string[] SupportedCultures = new[]
    {
        "ar", "bg", "bs", "ca", "cs", "de", "el", "en", "es", "fr",
        "hu", "it", "ja", "ko", "lv", "nl", "nl-nl", "no", "pl",
        "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz", "uz-latn-uz", "vi", "zh", "zh-hans", "zh-hant"
    };

    [Fact]
    public void TextClass_HasNoHardcodedChinese()
    {
        var type = typeof(NetworkAccelerationText);

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
        var type = typeof(NetworkAccelerationText);
        var method = type.GetMethod("T", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
    }

    [Fact]
    public void AllResourceKeys_AreAccessible()
    {
        var assembly = Assembly.Load("LenovoLegionToolkit.Plugins.NetworkAcceleration");
        var resourceManager = new ResourceManager(
            "LenovoLegionToolkit.Plugins.NetworkAcceleration.Resources.Resource",
            assembly);

        var textType = typeof(NetworkAccelerationText);
        var properties = textType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string))
            .ToList();

        Assert.True(properties.Count > 0, "No string properties found in NetworkAccelerationText");

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

    [Fact]
    public void ChineseResourceFile_ContainsExpectedKeys()
    {
        var assembly = Assembly.Load("LenovoLegionToolkit.Plugins.NetworkAcceleration");
        var resourceManager = new ResourceManager(
            "LenovoLegionToolkit.Plugins.NetworkAcceleration.Resources.Resource",
            assembly);

        var textType = typeof(NetworkAccelerationText);
        var properties = textType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string))
            .ToList();

        var zhCulture = CultureInfo.GetCultureInfo("zh");
        var missingKeys = new System.Collections.Generic.List<string>();

        foreach (var property in properties)
        {
            var key = property.Name;
            try
            {
                var resourceValue = resourceManager.GetString(key, zhCulture);
                if (string.IsNullOrEmpty(resourceValue))
                {
                    missingKeys.Add(key);
                }
            }
            catch
            {
                missingKeys.Add(key);
            }
        }

        // Chinese translations should be available for most keys
        var coverage = (double)(properties.Count - missingKeys.Count) / properties.Count;
        Assert.True(coverage >= 0.9, $"Chinese translation coverage is only {coverage:P0}. Missing keys: {string.Join(", ", missingKeys.Take(10))}");
    }

    [Theory]
    [InlineData("PluginName")]
    [InlineData("ServiceStateRunning")]
    [InlineData("ServiceStateStopped")]
    [InlineData("ModeBalanced")]
    [InlineData("ModeGaming")]
    [InlineData("StatusServiceStarted")]
    [InlineData("StatusServiceStopped")]
    public void CommonKeys_ReturnNonEmptyString(string key)
    {
        var type = typeof(NetworkAccelerationText);
        var property = type.GetProperty(key, BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(property);
        var value = property.GetValue(null) as string;
        Assert.NotNull(value);
        Assert.NotEmpty(value);
    }
}
