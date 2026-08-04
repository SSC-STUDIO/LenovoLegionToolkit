using System.Globalization;
using System.Windows.Data;
using UniversalDeviceToolkit.Plugins.ViveTool;
using UniversalDeviceToolkit.Plugins.ViveTool.Resources;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

public class FeatureStatusConverterTests
{
    [Theory]
    [InlineData(FeatureFlagStatus.Enabled, nameof(Resource.ViveTool_StatusEnabled))]
    [InlineData(FeatureFlagStatus.Disabled, nameof(Resource.ViveTool_StatusDisabled))]
    [InlineData(FeatureFlagStatus.Default, nameof(Resource.ViveTool_StatusDefault))]
    [InlineData((FeatureFlagStatus)999, nameof(Resource.ViveTool_StatusUnknown))]
    public void Convert_ReturnsLocalizedStatusText(FeatureFlagStatus status, string resourceName)
    {
        var converter = new FeatureStatusConverter();

        var result = converter.Convert(status, typeof(string), string.Empty, CultureInfo.InvariantCulture);

        var expected = typeof(Resource).GetProperty(resourceName)?.GetValue(null) as string;
        Assert.Equal(expected, result as string);
    }

    [Fact]
    public void ConvertBack_ReturnsDoNothing_InsteadOfThrowing()
    {
        var converter = new FeatureStatusConverter();

        var result = converter.ConvertBack("Enabled", typeof(FeatureFlagStatus), string.Empty, CultureInfo.InvariantCulture);

        Assert.Same(Binding.DoNothing, result);
    }
}
