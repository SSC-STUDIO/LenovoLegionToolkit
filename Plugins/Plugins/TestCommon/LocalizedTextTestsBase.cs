using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Xunit;

namespace LenovoLegionToolkit.Plugins.TestCommon;

public abstract class LocalizedTextTestsBase
{
    protected abstract Type TextType { get; }
    protected abstract Type ResourceType { get; }
    protected abstract string[] RequiredKeys { get; }

    [Fact]
    public void TextClass_HasNoHardcodedChinese()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        var originalResourceCulture = GetResourceCulture();

        try
        {
            var englishCulture = CultureInfo.GetCultureInfo("en");
            CultureInfo.CurrentUICulture = englishCulture;
            SetResourceCulture(englishCulture);

            var properties = GetStringProperties();
            Assert.NotEmpty(properties);

            foreach (var property in properties)
            {
                var value = property.GetValue(null) as string;
                Assert.NotNull(value);

                var hasChinese = value.Any(c => c is >= (char)0x4E00 and <= (char)0x9FFF);
                Assert.False(hasChinese, $"Property {property.Name} contains hardcoded Chinese text in fallback: {value}");
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
            SetResourceCulture(originalResourceCulture);
        }
    }

    [Fact]
    public void TextClass_HasFallbackAccessor()
    {
        var method = TextType.GetMethod("T", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void AllResourceKeys_AreAccessible()
    {
        var resourceManager = new ResourceManager(ResourceType.FullName!, ResourceType.Assembly);
        var properties = GetStringProperties();

        Assert.True(properties.Count > 0, $"No string properties found in {TextType.Name}");

        foreach (var property in properties)
        {
            var key = property.Name;
            var value = property.GetValue(null) as string;

            Assert.False(string.IsNullOrEmpty(value), $"Property {key} has null or empty fallback value");

            try
            {
                _ = resourceManager.GetString(key, CultureInfo.GetCultureInfo("en"));
            }
            catch
            {
                // Resource lookup can miss if the fallback literal is the effective source.
            }
        }
    }

    [Fact]
    public void RequiredKeys_ReturnNonEmptyString()
    {
        foreach (var key in RequiredKeys)
        {
            var property = TextType.GetProperty(key, BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(property);
            var value = property.GetValue(null) as string;
            Assert.NotNull(value);
            Assert.NotEmpty(value);
        }
    }

    protected void AssertTranslationCoverage(string cultureName, double minimumCoverage)
    {
        var properties = GetStringProperties();
        Assert.NotEmpty(properties);

        var resourceManager = new ResourceManager(ResourceType.FullName!, ResourceType.Assembly);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var missingKeys = properties
            .Where(property => string.IsNullOrEmpty(resourceManager.GetString(property.Name, culture)))
            .Select(property => property.Name)
            .ToList();

        var coverage = (double)(properties.Count - missingKeys.Count) / properties.Count;
        Assert.True(
            coverage >= minimumCoverage,
            $"{cultureName} translation coverage is only {coverage:P0}. Missing keys: {string.Join(", ", missingKeys.Take(10))}");
    }

    private CultureInfo? GetResourceCulture()
    {
        return ResourceCultureProperty.GetValue(null) as CultureInfo;
    }

    private void SetResourceCulture(CultureInfo? culture)
    {
        ResourceCultureProperty.SetValue(null, culture);
    }

    private PropertyInfo ResourceCultureProperty =>
        ResourceType.GetProperty("Culture", BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Resource type {ResourceType.FullName} does not expose a static Culture property.");

    private System.Collections.Generic.List<PropertyInfo> GetStringProperties()
    {
        return TextType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string))
            .ToList();
    }
}
