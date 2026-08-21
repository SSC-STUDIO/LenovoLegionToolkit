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

    private static void WriteHostRelease(string hostDir, string hostName)
    {
        var json = $$"""
        {
          "hostVersion": "5.0.0",
          "hostTag": "v5.0.0",
          "artifacts": {
            "lib": "UniversalDeviceToolkit.Lib.dll",
            "libPlugins": "UniversalDeviceToolkit.Lib.Plugins.dll",
            "host": "{{hostName}}",
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

    [Fact]
    public void Run_HostBaselineUsesVersionedHostCache()
    {
        var repoDir = CreateMinimalRepo();
        var baselineDir = Path.Combine(repoDir, "HostBaseline");
        var versionedHostDir = Path.Combine(repoDir, ".host", "5.0.0");
        Directory.CreateDirectory(baselineDir);
        Directory.CreateDirectory(versionedHostDir);

        try
        {
            WriteHostRelease(baselineDir, "UniversalDeviceToolkit.Host.dll");
            CreateDummyDll(versionedHostDir, "UniversalDeviceToolkit.Lib.dll");
            CreateDummyDll(versionedHostDir, "UniversalDeviceToolkit.Lib.Plugins.dll");

            var service = new DoctorService();
            var result = service.Run(repoDir);

            var cacheCheck = result.Checks.First(c => c.Message.Contains("Host dependency cache found", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("PASS", cacheCheck.Status);
            Assert.Contains(Path.Combine(".host", "5.0.0"), cacheCheck.Message, StringComparison.OrdinalIgnoreCase);

            var libCheck = result.Checks.First(c => c.Message.Contains("Host library found", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("PASS", libCheck.Status);
            Assert.Contains(Path.Combine(".host", "5.0.0", "UniversalDeviceToolkit.Lib.dll"), libCheck.Message, StringComparison.OrdinalIgnoreCase);

            var pluginsLibCheck = result.Checks.First(c => c.Message.Contains("Host plugins library found", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("PASS", pluginsLibCheck.Status);
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
    public void Run_HostBaselineIgnoresFlatHostCache()
    {
        var repoDir = CreateMinimalRepo();
        var baselineDir = Path.Combine(repoDir, "HostBaseline");
        var flatHostDir = Path.Combine(repoDir, ".host");
        Directory.CreateDirectory(baselineDir);

        try
        {
            WriteHostRelease(baselineDir, "UniversalDeviceToolkit.Host.dll");
            CreateDummyDll(flatHostDir, "UniversalDeviceToolkit.Lib.dll");
            CreateDummyDll(flatHostDir, "UniversalDeviceToolkit.Lib.Plugins.dll");

            var service = new DoctorService();
            var result = service.Run(repoDir);

            var libCheck = result.Checks.First(c => c.Message.Contains("Host library found", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("FAIL", libCheck.Status);
            Assert.Contains(Path.Combine(".host", "5.0.0"), libCheck.Message, StringComparison.OrdinalIgnoreCase);
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
    public void Run_InvalidHostReleaseJsonFails()
    {
        var repoDir = CreateMinimalRepo();
        var hostDir = Path.Combine(repoDir, ".host");

        try
        {
            File.WriteAllText(Path.Combine(hostDir, "host-release.json"), "{ not-json");

            var service = new DoctorService();
            var result = service.Run(repoDir);

            Assert.Contains(result.Checks, c =>
                c.Status == "FAIL" &&
                c.Message.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Checks, c =>
                c.Status == "FAIL" &&
                c.Message.Contains("host version unknown", StringComparison.OrdinalIgnoreCase));
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
    public void Run_LibPluginsMissingFails()
    {
        var repoDir = CreateMinimalRepo();
        var hostDir = Path.Combine(repoDir, ".host");

        try
        {
            WriteHostRelease(hostDir, "UniversalDeviceToolkit.Host.dll");
            CreateDummyDll(hostDir, "UniversalDeviceToolkit.Lib.dll");

            var service = new DoctorService();
            var result = service.Run(repoDir);

            var pluginsLibCheck = result.Checks.First(c => c.Message.Contains("Host plugins library found", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("FAIL", pluginsLibCheck.Status);
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
