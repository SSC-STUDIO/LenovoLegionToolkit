using System.Text.Json;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public class DoctorServiceTests
{
    private static string CreateMinimalRepo()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"doctor-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "Official"));
        Directory.CreateDirectory(Path.Combine(tempDir, ".host"));
        File.WriteAllText(Path.Combine(tempDir, "UniversalDeviceToolkit.Plugins.sln"), "Microsoft Visual Studio Solution File");
        return tempDir;
    }

    private static void WriteHostRelease(string hostDir, string wpfName)
    {
        var json = $$"""
        {
          "hostVersion": "5.0.0",
          "hostTag": "v5.0.0",
          "artifacts": {
            "lib": "UniversalDeviceToolkit.Lib.dll",
            "libPlugins": "UniversalDeviceToolkit.Lib.Plugins.dll",
            "wpf": "{{wpfName}}",
            "package": "UniversalDeviceToolkit_v5.0.0_win-x64.zip",
            "transitiveDependencies": []
          }
        }
        """;
        File.WriteAllText(Path.Combine(hostDir, "host-release.json"), json);
    }

    private static void CreateDummyDll(string dir, string dllName)
    {
        File.WriteAllBytes(Path.Combine(dir, dllName), [0x4D, 0x5A]);
    }

    [Fact]
    public void Run_NoWpfHostAssemblyCheck()
    {
        var repoDir = CreateMinimalRepo();
        var hostDir = Path.Combine(repoDir, ".host");

        try
        {
            WriteHostRelease(hostDir, "UniversalDeviceToolkit.Host.dll");
            CreateDummyDll(hostDir, "UniversalDeviceToolkit.Lib.dll");
            CreateDummyDll(hostDir, "UniversalDeviceToolkit.Host.dll");

            var service = new DoctorService();
            var result = service.Run(repoDir);

            Assert.DoesNotContain(result.Checks, c => c.Message.Contains("Host WPF assembly", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Checks, c => c.Message.Contains("Host library found", StringComparison.OrdinalIgnoreCase) && c.Status == "PASS");
        }
        finally
        {
            if (Directory.Exists(repoDir))
            {
                Directory.Delete(repoDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_HostLibraryCheckStillWorks()
    {
        var repoDir = CreateMinimalRepo();
        var hostDir = Path.Combine(repoDir, ".host");

        try
        {
            WriteHostRelease(hostDir, "UniversalDeviceToolkit.Host.dll");
            CreateDummyDll(hostDir, "UniversalDeviceToolkit.Lib.dll");

            var service = new DoctorService();
            var result = service.Run(repoDir);

            var libCheck = result.Checks.First(c => c.Message.Contains("Host library found", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("PASS", libCheck.Status);
        }
        finally
        {
            if (Directory.Exists(repoDir))
            {
                Directory.Delete(repoDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_LibMissingFails()
    {
        var repoDir = CreateMinimalRepo();
        var hostDir = Path.Combine(repoDir, ".host");

        try
        {
            WriteHostRelease(hostDir, "UniversalDeviceToolkit.Host.dll");
            // Deliberately do NOT create the Lib DLL

            var service = new DoctorService();
            var result = service.Run(repoDir);

            var libCheck = result.Checks.First(c => c.Message.Contains("Host library found", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("FAIL", libCheck.Status);
        }
        finally
        {
            if (Directory.Exists(repoDir))
            {
                Directory.Delete(repoDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_LibNameAlsoReadFromHostReleaseJson()
    {
        var repoDir = CreateMinimalRepo();
        var hostDir = Path.Combine(repoDir, ".host");

        try
        {
            WriteHostRelease(hostDir, "Universal Device Toolkit.dll");
            CreateDummyDll(hostDir, "UniversalDeviceToolkit.Lib.dll");
            CreateDummyDll(hostDir, "Universal Device Toolkit.dll");

            var service = new DoctorService();
            var result = service.Run(repoDir);

            var libCheck = result.Checks.First(c => c.Message.Contains("Host library", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("PASS", libCheck.Status);
            Assert.Contains("UniversalDeviceToolkit.Lib.dll", libCheck.Message);
        }
        finally
        {
            if (Directory.Exists(repoDir))
            {
                Directory.Delete(repoDir, recursive: true);
            }
        }
    }
}
