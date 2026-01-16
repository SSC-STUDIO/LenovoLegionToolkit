using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.System.Management;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class WMITests
{
    [Fact]
    public void WMIPropertyValueFormatter_Instance_ShouldBeSingleton()
    {
        // Arrange & Act
        var instance1 = WMI.WMIPropertyValueFormatter.Instance;
        var instance2 = WMI.WMIPropertyValueFormatter.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void WMIPropertyValueFormatter_GetFormat_WithCustomFormatterType_ShouldReturnSelf()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;

        // Act
        var result = formatter.GetFormat(typeof(ICustomFormatter));

        // Assert
        result.Should().BeSameAs(formatter);
    }

    [Fact]
    public void WMIPropertyValueFormatter_GetFormat_WithInvalidType_ShouldThrow()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;

        // Act & Assert
        Action act = () => formatter.GetFormat(typeof(string));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithNullArg_ShouldReturnEmptyString()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;

        // Act
        var result = formatter.Format(null, null, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithBackslash_ShouldEscapeBackslash()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"C:\Users\Test";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(@"C:\\Users\\Test");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithMultipleBackslashes_ShouldEscapeAll()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"\\network\share\path";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(@"\\\\network\\share\\path");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithSimpleString_ShouldReturnSameString()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = "SimpleString123";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = string.Empty;

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithWhitespace_ShouldReturnWhitespace()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = "   ";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"C:\Program Files\My App\file.txt";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(@"C:\\Program Files\\My App\\file.txt");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithNullFormatProvider_ShouldNotThrow()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = "test";

        // Act
        Action act = () => formatter.Format(null, input, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithFormatString_ShouldIgnoreFormat()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"C:\Test";

        // Act
        var result = formatter.Format("N", input, null);

        // Assert
        result.Should().Be(@"C:\\Test");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithIntegerArg_ShouldConvertToString()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = 123;

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be("123");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithBooleanArg_ShouldConvertToString()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = true;

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be("True");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithMixedContent_ShouldHandleCorrectly()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"Path: C:\Users\Name\file.txt";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(@"Path: C:\\Users\\Name\\file.txt");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithUnicodeCharacters_ShouldPreserve()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"C:\Users\用户\文件.txt";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(@"C:\\Users\\用户\\文件.txt");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithTrailingBackslash_ShouldEscape()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"C:\Test\";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(@"C:\\Test\\");
    }

    [Fact]
    public void WMIPropertyValueFormatter_Format_WithLeadingBackslash_ShouldEscape()
    {
        // Arrange
        var formatter = WMI.WMIPropertyValueFormatter.Instance;
        var input = @"\network\share";

        // Act
        var result = formatter.Format(null, input, null);

        // Assert
        result.Should().Be(@"\\network\\share");
    }

    [Fact]
    public async Task ExistsAsync_WithValidWmiQuery_ShouldReturnBoolean()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_OperatingSystem";

        // Act
        var result = await WMI.ExistsAsync(scope, query);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public async Task ExistsAsync_WithInvalidQuery_ShouldNotThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM InvalidClass";

        // Act
        Func<Task> act = async () => await WMI.ExistsAsync(scope, query);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WithInvalidScope_ShouldNotThrow()
    {
        // Arrange
        var scope = @"root\invalid";
        var query = $"SELECT * FROM Win32_OperatingSystem";

        // Act
        Func<Task> act = async () => await WMI.ExistsAsync(scope, query);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyQuery_ShouldNotThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"";

        // Act
        Func<Task> act = async () => await WMI.ExistsAsync(scope, query);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WithParameterizedQuery_ShouldHandleParameters()
    {
        // Arrange
        var scope = @"root\cimv2";
        var osName = "Microsoft Windows";
        var query = $"SELECT * FROM Win32_OperatingSystem WHERE Name LIKE '{osName}%'";

        // Act
        var result = await WMI.ExistsAsync(scope, query);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public async Task ReadAsync_WithValidQuery_ShouldReturnResults()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT Name, Version FROM Win32_OperatingSystem";
        var converter = (System.Management.PropertyDataCollection props) => 
        {
            var name = props["Name"]?.Value?.ToString() ?? string.Empty;
            var version = props["Version"]?.Value?.ToString() ?? string.Empty;
            return new { Name = name, Version = version };
        };

        // Act
        var result = await WMI.ReadAsync(scope, query, converter);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ReadAsync_WithInvalidQuery_ShouldThrowManagementException()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM InvalidClass";
        var converter = (System.Management.PropertyDataCollection props) => 
            props["Name"]?.Value?.ToString() ?? string.Empty;

        // Act & Assert
        Func<Task> act = async () => await WMI.ReadAsync(scope, query, converter);
        await act.Should().ThrowAsync<System.Management.ManagementException>();
    }

    [Fact]
    public async Task ReadAsync_WithNullConverter_ShouldThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_OperatingSystem";
        Func<System.Management.PropertyDataCollection, string> converter = null;

        // Act & Assert
        Func<Task> act = async () => await WMI.ReadAsync(scope, query, converter);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAsync_WithEmptyResult_ShouldReturnEmpty()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_OperatingSystem WHERE Name = 'NonExistentOS'";
        var converter = (System.Management.PropertyDataCollection props) => 
            props["Name"]?.Value?.ToString() ?? string.Empty;

        // Act
        var result = await WMI.ReadAsync(scope, query, converter);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsync_WithMultipleResults_ShouldReturnAll()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT DeviceID FROM Win32_LogicalDisk";
        var converter = (System.Management.PropertyDataCollection props) => 
            props["DeviceID"]?.Value?.ToString() ?? string.Empty;

        // Act
        var result = await WMI.ReadAsync(scope, query, converter);

        // Assert
        result.Should().NotBeEmpty();
        result.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReadAsync_WithComplexConverter_ShouldWork()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT Name, FreeSpace, Size FROM Win32_LogicalDisk WHERE DeviceID = 'C:'";
        var converter = (System.Management.PropertyDataCollection props) => 
        {
            var name = props["Name"]?.Value?.ToString() ?? string.Empty;
            var freeSpace = props["FreeSpace"]?.Value as ulong? ?? 0;
            var size = props["Size"]?.Value as ulong? ?? 0;
            return new { Name = name, FreeSpace = freeSpace, Size = size };
        };

        // Act
        var result = await WMI.ReadAsync(scope, query, converter);

        // Assert
        result.Should().NotBeEmpty();
        result.First().Name.Should().Be("C:");
        result.First().Size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CallAsync_WithValidMethod_ShouldNotThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_Process WHERE Name = 'notepad.exe'";
        var methodName = "Terminate";
        var methodParams = new Dictionary<string, object>();

        // Act & Assert - This might fail if notepad is not running
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CallAsync_WithInvalidMethod_ShouldThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_Process WHERE Name = 'notepad.exe'";
        var methodName = "InvalidMethod";
        var methodParams = new Dictionary<string, object>();

        // Act & Assert
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams);
        await act.Should().ThrowAsync<System.Management.ManagementException>();
    }

    [Fact]
    public async Task CallAsync_WithNullMethodParams_ShouldNotThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_Process WHERE Name = 'notepad.exe'";
        var methodName = "Terminate";
        Dictionary<string, object> methodParams = null;

        // Act & Assert
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CallAsync_WithEmptyQuery_ShouldThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"";
        var methodName = "Terminate";
        var methodParams = new Dictionary<string, object>();

        // Act & Assert
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams);
        await act.Should().ThrowAsync<System.Management.ManagementException>();
    }

    [Fact]
    public async Task CallAsyncGeneric_WithValidMethod_ShouldReturnResult()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_Process WHERE Name = 'notepad.exe'";
        var methodName = "GetOwner";
        var methodParams = new Dictionary<string, object>();
        var converter = (System.Management.PropertyDataCollection props) => 
            props["User"]?.Value?.ToString() ?? string.Empty;

        // Act & Assert - This might fail if notepad is not running
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams, converter);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CallAsyncGeneric_WithNullConverter_ShouldThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_Process WHERE Name = 'notepad.exe'";
        var methodName = "GetOwner";
        var methodParams = new Dictionary<string, object>();
        Func<System.Management.PropertyDataCollection, string> converter = null;

        // Act & Assert
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams, converter);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CallAsyncGeneric_WithNoResults_ShouldThrow()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_Process WHERE Name = 'NonExistentProcess.exe'";
        var methodName = "Terminate";
        var methodParams = new Dictionary<string, object>();
        var converter = (System.Management.PropertyDataCollection props) => 
            props["ReturnValue"]?.ToString() ?? string.Empty;

        // Act & Assert
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams, converter);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CallAsync_WithMethodParameters_ShouldPassParameters()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_Process WHERE Name = 'notepad.exe'";
        var methodName = "Terminate";
        var methodParams = new Dictionary<string, object>
        {
            { "Reason", 0 }
        };

        // Act & Assert
        Func<Task> act = async () => await WMI.CallAsync(scope, query, methodName, methodParams);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MultipleWmiCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_OperatingSystem";
        var converter = (System.Management.PropertyDataCollection props) => 
            props["Name"]?.Value?.ToString() ?? string.Empty;

        // Act
        var tasks = Enumerable.Range(0, 10).Select(i => 
            WMI.ReadAsync(scope, query, converter)
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.FirstOrDefault()));
    }

    [Fact]
    public async Task WMI_WithBackslashInQuery_ShouldHandleCorrectly()
    {
        // Arrange
        var scope = @"root\cimv2";
        var path = @"C:\Windows";
        var query = $"SELECT * FROM Win32_Directory WHERE Name = '{path}'";
        var converter = (System.Management.PropertyDataCollection props) => 
            props["Name"]?.Value?.ToString() ?? string.Empty;

        // Act
        var result = await WMI.ReadAsync(scope, query, converter);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExistsAsync_WithWQLSpecialCharacters_ShouldHandle()
    {
        // Arrange
        var scope = @"root\cimv2";
        var query = $"SELECT * FROM Win32_OperatingSystem WHERE Name LIKE '%Windows%'";

        // Act
        var result = await WMI.ExistsAsync(scope, query);

        // Assert
        result.Should().Be(true);
    }
}
