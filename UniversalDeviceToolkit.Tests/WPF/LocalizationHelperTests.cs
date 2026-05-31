using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class LocalizationHelperTests : IDisposable
{
    private readonly string _tempAppData;
    private readonly string? _previousAppDataOverride;

    public LocalizationHelperTests()
    {
        _tempAppData = Path.Combine(Path.GetTempPath(), $"udt-lang-helper-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempAppData);
        _previousAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _tempAppData);
    }

    [Fact]
    public async Task SetLanguageAsync_ShouldPersistCultureNameToLangFile()
    {
        await LocalizationHelper.SetLanguageAsync(new CultureInfo("de"));

        var langPath = Path.Combine(Folders.AppData, "lang");
        File.Exists(langPath).Should().BeTrue();
        (await File.ReadAllTextAsync(langPath)).Trim().Should().Be("de");
        CultureInfo.CurrentUICulture.Name.Should().Be("de");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _previousAppDataOverride);

        try
        {
            if (Directory.Exists(_tempAppData))
                Directory.Delete(_tempAppData, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
