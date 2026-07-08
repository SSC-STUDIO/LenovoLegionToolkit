using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

public class ThreadSafeBoolTests
{
    [Fact]
    public void Default_Value_IsFalse()
    {
        var tsb = new ThreadSafeBool();
        tsb.Value.Should().BeFalse();
    }

    [Fact]
    public void SetTrue_ValueBecomesTrue()
    {
        var tsb = new ThreadSafeBool();
        tsb.Value = true;
        tsb.Value.Should().BeTrue();
    }

    [Fact]
    public void SetFalse_AfterTrue_RevertsToFalse()
    {
        var tsb = new ThreadSafeBool { Value = true };
        tsb.Value = false;
        tsb.Value.Should().BeFalse();
    }

    [Fact]
    public void ConcurrentToggle_NoException()
    {
        var tsb = new ThreadSafeBool();
        var exceptions = new System.Collections.Generic.List<Exception>();

        Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try { tsb.Value = i % 2 == 0; }
            catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
        });

        exceptions.Should().BeEmpty();
    }
}

public class ThreadSafeCounterTests
{
    [Fact]
    public void Decrement_FromZero_ReturnsTrue()
    {
        var counter = new ThreadSafeCounter();
        var result = counter.Decrement();
        result.Should().BeTrue();
    }

    [Fact]
    public void IncrementThenDecrement_ReturnsFalse()
    {
        var counter = new ThreadSafeCounter();
        counter.Increment();
        var result = counter.Decrement();
        result.Should().BeFalse();
    }

    [Fact]
    public void DoubleDecrement_FromZero_DoesNotGoNegative()
    {
        var counter = new ThreadSafeCounter();
        counter.Decrement();
        var result = counter.Decrement();
        result.Should().BeTrue();
    }

    [Fact]
    public void ConcurrentIncrementDecrement_NoException()
    {
        var counter = new ThreadSafeCounter();
        var exceptions = new System.Collections.Generic.List<Exception>();

        Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                if (i % 2 == 0) counter.Increment();
                else counter.Decrement();
            }
            catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
        });

        exceptions.Should().BeEmpty();
    }
}

public class PathSecurityEdgeCaseTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidFileName_NullEmptyWhitespace_ReturnsFalse(string? name)
    {
        PathSecurity.IsValidFileName(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    public void IsValidFileName_ReservedDeviceName_ReturnsFalse(string name)
    {
        PathSecurity.IsValidFileName(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("valid.txt")]
    [InlineData("my-file_v2.json")]
    [InlineData("data.csv")]
    public void IsValidFileName_NormalNames_ReturnsTrue(string name)
    {
        PathSecurity.IsValidFileName(name).Should().BeTrue();
    }

    [Fact]
    public void IsValidFileName_PathTraversal_ReturnsFalse()
    {
        PathSecurity.IsValidFileName("../secret.txt").Should().BeFalse();
    }

    [Fact]
    public void IsValidFileName_NullByte_ReturnsFalse()
    {
        PathSecurity.IsValidFileName("file\0.txt").Should().BeFalse();
    }

    [Fact]
    public void IsValidFileName_TrailingDot_ReturnsFalse()
    {
        PathSecurity.IsValidFileName("file.").Should().BeFalse();
    }

    [Fact]
    public void IsValidPluginId_NullEmpty_ReturnsFalse()
    {
        PathSecurity.IsValidPluginId(null).Should().BeFalse();
        PathSecurity.IsValidPluginId("").Should().BeFalse();
    }

    [Fact]
    public void IsValidPluginId_MustStartWithLetter()
    {
        PathSecurity.IsValidPluginId("1plugin").Should().BeFalse();
        PathSecurity.IsValidPluginId("plugin1").Should().BeTrue();
    }

    [Fact]
    public void IsValidPluginId_PathTraversal_ReturnsFalse()
    {
        PathSecurity.IsValidPluginId("../plugin").Should().BeFalse();
    }

    [Fact]
    public void IsValidPluginId_AllowsDashUnderscoreDot()
    {
        PathSecurity.IsValidPluginId("my-plugin.v2").Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidRegistryPath_NullEmptyWhitespace_ReturnsFalse(string? path)
    {
        PathSecurity.IsValidRegistryPath(path).Should().BeFalse();
    }

    [Fact]
    public void IsValidRegistryPath_HKLM_Valid()
    {
        PathSecurity.IsValidRegistryPath(@"HKEY_LOCAL_MACHINE\SOFTWARE\Lenovo").Should().BeTrue();
    }

    [Fact]
    public void IsValidRegistryPath_HKCU_Valid()
    {
        PathSecurity.IsValidRegistryPath(@"HKCU\Software\Lenovo").Should().BeTrue();
    }

    [Fact]
    public void IsValidRegistryPath_PathTraversal_ReturnsFalse()
    {
        PathSecurity.IsValidRegistryPath(@"HKLM\..\SYSTEM").Should().BeFalse();
    }

    [Fact]
    public void IsValidRegistryPath_NullByte_ReturnsFalse()
    {
        PathSecurity.IsValidRegistryPath("HKLM\0Software\\test\\plugin").Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidDriverPath_NullEmpty_ReturnsFalse(string? path)
    {
        PathSecurity.IsValidDriverPath(path).Should().BeFalse();
    }

    [Fact]
    public void IsValidDriverPath_NonSystemPath_ReturnsFalse()
    {
        PathSecurity.IsValidDriverPath(@"C:\Temp\driver.sys").Should().BeFalse();
    }

    [Fact]
    public void IsValidDriverPath_NonSysExtension_ReturnsFalse()
    {
        PathSecurity.IsValidDriverPath(@"C:\Windows\System32\drivers\test.dll").Should().BeFalse();
    }

    [Fact]
    public void CreateSafeFilePath_NullInputs_ReturnsNull()
    {
        PathSecurity.CreateSafeFilePath(null!, "file.txt").Should().BeNull();
        PathSecurity.CreateSafeFilePath("C:\\test", null).Should().BeNull();
    }

    [Fact]
    public void CreateSafeFilePath_TTraversalAttack_IsSanitizedWithinBase()
    {
        var result = PathSecurity.CreateSafeFilePath("C:\\safe", "..\\\\..\\\\etc\\\\passwd");
        result.Should().NotBeNull();
        result.Should().StartWith("C:\\safe");
    }
}

public class RetryHelperTests
{
    [Fact]
    public async Task RetryAsync_SuccessOnFirstTry_Returns()
    {
        var callCount = 0;
        await RetryHelper.RetryAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
        });
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task RetryAsync_SuccessAfterRetry_Returns()
    {
        var callCount = 0;
        await RetryHelper.RetryAsync(async () =>
        {
            callCount++;
            if (callCount < 3)
                throw new InvalidOperationException("not yet");
            await Task.CompletedTask;
        }, maximumRetries: 3);
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task RetryAsync_ExceedsRetries_ThrowsMaximumRetries()
    {
        await Assert.ThrowsAsync<MaximumRetriesReachedException>(async () =>
        {
            await RetryHelper.RetryAsync(async () =>
            {
                throw new InvalidOperationException("always fail");
            }, maximumRetries: 2);
        });
    }

    [Fact]
    public async Task RetryAsync_OperationCanceled_ThrowsImmediately()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await RetryHelper.RetryAsync(async () =>
            {
                throw new OperationCanceledException();
            }, maximumRetries: 5);
        });
    }

    [Fact]
    public async Task RetryAsync_MatchingExceptionFilter_RespectsFilter()
    {
        var callCount = 0;
        await RetryHelper.RetryAsync(async () =>
        {
            callCount++;
            if (callCount < 2)
                throw new ArgumentException("filtered");
            await Task.CompletedTask;
        }, maximumRetries: 3, matchingException: ex => ex is ArgumentException);
        callCount.Should().Be(2);
    }
}
