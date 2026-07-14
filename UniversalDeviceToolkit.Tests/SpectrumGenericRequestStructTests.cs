using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class SpectrumGenericRequestStructTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(255)]
    public void GenericRequest_WithValue2_ShouldStoreValue2(byte value2)
    {
        var request = new LENOVO_SPECTRUM_GENERIC_REQUEST(
            LENOVO_SPECTRUM_OPERATION_TYPE.UnknownC5, 7, value2);

        request.Value.Should().Be(7);
        request.Value2.Should().Be(value2);
    }

    [Fact]
    public void GenericRequest_WithZeroValue2_ShouldDefaultToZero()
    {
        var request = new LENOVO_SPECTRUM_GENERIC_REQUEST(
            LENOVO_SPECTRUM_OPERATION_TYPE.Brightness, 50, 0);

        request.Value2.Should().Be(0);
    }

    [Fact]
    public void GenericRequest_StructLayout_ShouldBeSequentialWithExpectedOrder()
    {
        var layout = typeof(LENOVO_SPECTRUM_GENERIC_REQUEST).StructLayoutAttribute;
        layout.Should().NotBeNull();
        layout!.Value.Should().Be(LayoutKind.Sequential);
    }

    [Fact]
    public void GenericRequest_HeaderField_ShouldExistWithType()
    {
        var headerField = typeof(LENOVO_SPECTRUM_GENERIC_REQUEST)
            .GetField("Header");
        headerField.Should().NotBeNull();
        headerField!.FieldType.Should().Be(typeof(LENOVO_SPECTRUM_HEADER));
    }

    [Fact]
    public void GenericRequest_Value2Field_ShouldExistAsByte()
    {
        var value2Field = typeof(LENOVO_SPECTRUM_GENERIC_REQUEST)
            .GetField("Value2");
        value2Field.Should().NotBeNull();
        value2Field!.FieldType.Should().Be(typeof(byte));
    }
}
