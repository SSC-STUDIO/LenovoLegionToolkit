using System.IO;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Tests.Settings
{
    /// <summary>
    /// Helper class for managing settings files during tests
    /// Prevents test pollution by cleaning up settings files
    /// </summary>
    public static class SettingsCleanupHelper
    {
        /// <summary>
        /// Deletes all settings files from the AppData folder
        /// </summary>
        public static void CleanupAllSettingsFiles()
        {
            try
            {
                var appData = Folders.AppData;
                if (Directory.Exists(appData))
                {
                    foreach (var file in Directory.GetFiles(appData, "*.json"))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore deletion errors
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Deletes a specific settings file
        /// </summary>
        public static void CleanupSettingsFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(Folders.AppData, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore deletion errors
            }
        }

        /// <summary>
        /// Common settings file names used in tests
        /// </summary>
        public static class SettingsFiles
        {
            public const string GodMode = "godmode.json";
            public const string RGBKeyboard = "rgb_keyboard.json";
            public const string GPUOverclock = "gpu_oc.json";
            public const string UpdateCheck = "update_check.json";
            public const string Application = "settings.json";
            public const string Integrations = "integrations.json";
            public const string BalanceMode = "balancemode.json";
            public const string SpectrumKeyboard = "spectrum_keyboard.json";
            public const string SunriseSunset = "sunrise_sunset.json";
        }
    }
}