using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UniversalDeviceToolkit.CLI.Lib;

public static class Constants
{
    public const string DEFAULT_PIPE_NAME = "LenovoLegionToolkit-IPC-0";
#if UDT_TEST_HOOKS
    public const string APPDATA_OVERRIDE_ENVIRONMENT_VARIABLE = "UDT_APPDATA_OVERRIDE";
#endif
    public static string PIPE_NAME => GetPipeNameFromEnvironment();

    public static string GetPipeName(string? isolationPath = null)
    {
        if (string.IsNullOrWhiteSpace(isolationPath))
            return DEFAULT_PIPE_NAME;

        try
        {
            var fullPath = Path.GetFullPath(isolationPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(fullPath))
                return DEFAULT_PIPE_NAME;

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fullPath));
            var suffix = Convert.ToHexString(hashBytes[..8]).ToLowerInvariant();
            return $"{DEFAULT_PIPE_NAME}-{suffix}";
        }
        catch
        {
            return DEFAULT_PIPE_NAME;
        }
    }

    public static string GetPipeNameFromEnvironment()
    {
#if UDT_TEST_HOOKS
        var overridePath = Environment.GetEnvironmentVariable(APPDATA_OVERRIDE_ENVIRONMENT_VARIABLE);
        return GetPipeName(overridePath);
#else
        return DEFAULT_PIPE_NAME;
#endif
    }
}
