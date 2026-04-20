using System;
using System.IO;

namespace LenovoLegionToolkit.Lib.Utils;

public static class Folders
{
    public const string AppDataOverrideEnvironmentVariable = "LLT_APPDATA_OVERRIDE";

    public static string Program => AppDomain.CurrentDomain.SetupInformation.ApplicationBase ?? string.Empty;

    public static string AppData
    {
        get
        {
            var overridePath = Environment.GetEnvironmentVariable(AppDataOverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var fullOverridePath = Path.GetFullPath(overridePath);
                Directory.CreateDirectory(fullOverridePath);
                return fullOverridePath;
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderPath = Path.Combine(appData, "LenovoLegionToolkit");
            Directory.CreateDirectory(folderPath);
            return folderPath;
        }
    }

    public static string Temp
    {
        get
        {
            var appData = Path.GetTempPath();
            var folderPath = Path.Combine(appData, "LenovoLegionToolkit");
            Directory.CreateDirectory(folderPath);
            return folderPath;
        }
    }
}
