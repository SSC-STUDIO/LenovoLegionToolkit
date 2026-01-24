using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Pages
{
    public class PluginViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _version = "1.0.0";
        private string _iconLetter = "?";
        private SolidColorBrush _iconBackground = Brushes.Gray;
        private string _installButtonText = "Install";
        private string _pluginId = string.Empty;
        private bool _isInstalled;

        private bool _isInstalling;
        private double _installProgress;
        private string _installStatusText = string.Empty;
        private bool _updateAvailable;
        private bool _isInstallButtonVisible;
        private bool _supportsConfiguration;


        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                    UpdateIconLetter();
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public string Version
        {
            get => _version;
            set
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged(nameof(Version));
                }
            }
        }

        public string IconLetter
        {
            get => _iconLetter;
            set
            {
                if (_iconLetter != value)
                {
                    _iconLetter = value;
                    OnPropertyChanged(nameof(IconLetter));
                }
            }
        }

        public SolidColorBrush IconBackground
        {
            get => _iconBackground;
            set
            {
                if (_iconBackground != value)
                {
                    _iconBackground = value;
                    OnPropertyChanged(nameof(IconBackground));
                }
            }
        }

        public string InstallButtonText
        {
            get => _installButtonText;
            set
            {
                if (_installButtonText != value)
                {
                    _installButtonText = value;
                    OnPropertyChanged(nameof(InstallButtonText));
                }
            }
        }

        public string UninstallButtonText
        {
            get
            {
                // Try to get localized text, fallback to hardcoded values
                try
                {
                    var property = typeof(Resource).GetProperty("PackageControl_Uninstall");
                    if (property != null)
                    {
                        var value = property.GetValue(null) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
                catch
                {
                    // Ignore errors
                }
                
                // Fallback based on current culture
                var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
                if (culture.StartsWith("zh-Hant"))
                {
                    return "解除安裝";
                }
                else if (culture.StartsWith("zh"))
                {
                    return "卸载";
                }
                else if (culture.StartsWith("ja"))
                {
                    return "アンインストール";
                }
                else if (culture.StartsWith("ko"))
                {
                    return "제거";
                }
                else if (culture.StartsWith("de"))
                {
                    return "Deinstallieren";
                }
                else if (culture.StartsWith("fr"))
                {
                    return "Désinstaller";
                }
                else if (culture.StartsWith("es"))
                {
                    return "Desinstalar";
                }
                else if (culture.StartsWith("pt"))
                {
                    return "Desinstalar";
                }
                else if (culture.StartsWith("ru"))
                {
                    return "Удалить";
                }
                else if (culture.StartsWith("it"))
                {
                    return "Disinstalla";
                }
                else if (culture.StartsWith("pl"))
                {
                    return "Odinstaluj";
                }
                else if (culture.StartsWith("tr"))
                {
                    return "Kaldır";
                }
                else if (culture.StartsWith("uk") || culture.StartsWith("ru"))
                {
                    return "Видалити";
                }
                else if (culture.StartsWith("vi"))
                {
                    return "Gỡ cài đặt";
                }
                else if (culture.StartsWith("th"))
                {
                    return "ถอนการติดตั้ง";
                }
                else if (culture.StartsWith("ar"))
                {
                    return "إلغاء التثبيت";
                }
                return "Uninstall";
            }
        }



        public string OpenButtonText
        {
            get
            {
                // Try to get localized text, fallback to hardcoded values
                try
                {
                    var property = typeof(Resource).GetProperty("Open");
                    if (property != null)
                    {
                        var value = property.GetValue(null) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
                catch
                {
                    // Ignore errors
                }
                
                // Fallback based on current culture
                var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
                if (culture.StartsWith("zh-Hant"))
                {
                    return "開啟";
                }
                else if (culture.StartsWith("zh"))
                {
                    return "打开";
                }
                else if (culture.StartsWith("ja"))
                {
                    return "開く";
                }
                else if (culture.StartsWith("ko"))
                {
                    return "열기";
                }
                else if (culture.StartsWith("de"))
                {
                    return "Öffnen";
                }
                else if (culture.StartsWith("fr"))
                {
                    return "Ouvrir";
                }
                else if (culture.StartsWith("es"))
                {
                    return "Abrir";
                }
                else if (culture.StartsWith("pt"))
                {
                    return "Abrir";
                }
                else if (culture.StartsWith("ru"))
                {
                    return "Открыть";
                }
                else if (culture.StartsWith("it"))
                {
                    return "Apri";
                }
                else if (culture.StartsWith("pl"))
                {
                    return "Otwórz";
                }
                else if (culture.StartsWith("tr"))
                {
                    return "Aç";
                }
                else if (culture.StartsWith("uk") || culture.StartsWith("ru"))
                {
                    return "Відкрити";
                }
                else if (culture.StartsWith("vi"))
                {
                    return "Mở";
                }
                else if (culture.StartsWith("th"))
                {
                    return "เปิด";
                }
                else if (culture.StartsWith("ar"))
                {
                    return "فتح";
                }
                return "Open";
            }
        }



public string PluginId
        {
            get => _pluginId;
            set
            {
                if (_pluginId != value)
                {
                    _pluginId = value;
                    OnPropertyChanged(nameof(PluginId));
                }
            }
        }

        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled != value)
                {
                    _isInstalled = value;
                    OnPropertyChanged(nameof(IsInstalled));
                    
                    // Update button text when installation status changes
                    UpdateInstallButtonText();
                }
            }
        }



        public bool IsInstalling
        {
            get => _isInstalling;
            set
            {
                if (_isInstalling != value)
                {
                    _isInstalling = value;
                    OnPropertyChanged(nameof(IsInstalling));
                }
            }
        }

        public bool IsInstallButtonVisible
        {
            get => _isInstallButtonVisible;
            set
            {
                if (_isInstallButtonVisible != value)
                {
                    _isInstallButtonVisible = value;
                    OnPropertyChanged(nameof(IsInstallButtonVisible));
                }
            }
        }



        public double InstallProgress
        {
            get => _installProgress;
            set
            {
                if (Math.Abs(_installProgress - value) > 0.01)
                {
                    _installProgress = value;
                    OnPropertyChanged(nameof(InstallProgress));
                }
            }
        }

        public string InstallStatusText
        {
            get => _installStatusText;
            set
            {
                if (_installStatusText != value)
                {
                    _installStatusText = value;
                    OnPropertyChanged(nameof(InstallStatusText));
                }
            }
        }

        public bool SupportsConfiguration
        {
            get => _supportsConfiguration;
            set
            {
                if (_supportsConfiguration != value)
                {
                    _supportsConfiguration = value;
                    OnPropertyChanged(nameof(SupportsConfiguration));
                }
            }
        }

        public IPlugin Plugin { get; private set; }

    public PluginViewModel(IPlugin plugin, bool isInstalled, bool updateAvailable = false, string version = "1.0.0", string? iconBackground = null)
    {
        Plugin = plugin;
        PluginId = plugin.Id;
        Name = plugin.Name ?? plugin.Id;
        Description = plugin.Description ?? string.Empty;
        Version = $"v{version}";
        IsInstalled = isInstalled;
        _updateAvailable = updateAvailable;
        SupportsConfiguration = false; // Will be set later based on plugin capabilities
        
        UpdateInstallButtonText();
        UpdateIconLetter(iconBackground);
    }

        private void UpdateInstallButtonText()
        {
            var oldText = InstallButtonText;
            
            if (IsInstalled)
            {
                InstallButtonText = _updateAvailable ? Resource.Update : Resource.PluginExtensionsPage_PluginInstalled;
                IsInstallButtonVisible = _updateAvailable;
            }
            else
            {
                InstallButtonText = Resource.PluginExtensionsPage_InstallPlugin;
                IsInstallButtonVisible = true;
            }
            
            // Add debug logging
            try
            {
                var logger = LenovoLegionToolkit.Lib.Utils.Log.Instance;
                if (logger.IsTraceEnabled)
                {
                    logger.Trace($"UpdateInstallButtonText for {PluginId}: {oldText} -> {InstallButtonText} (IsInstalled={IsInstalled}, UpdateAvailable={_updateAvailable}, Visible={IsInstallButtonVisible})");
                }
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public void SetUpdateAvailable(bool updateAvailable)
        {
            if (_updateAvailable != updateAvailable)
            {
                _updateAvailable = updateAvailable;
                UpdateInstallButtonText();
            }
        }

        public void CheckConfigurationSupport(IPlugin plugin)
        {
            try
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"CheckConfigurationSupport for {PluginId}: plugin type = {plugin.GetType().Name}");
                }
                
                if (plugin is LenovoLegionToolkit.Plugins.SDK.PluginBase sdkPlugin)
                {
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {PluginId} is SDK plugin, calling GetSettingsPage...");
                    }
                    
                    var settingsPage = sdkPlugin.GetSettingsPage();
                    SupportsConfiguration = settingsPage != null;
                    
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {PluginId} configuration check: GetSettingsPage returned {settingsPage?.GetType().Name ?? "null"}, SupportsConfiguration={SupportsConfiguration}");
                    }
                }
                else
                {
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {PluginId} is not an SDK plugin, SupportsConfiguration=false");
                    }
                    SupportsConfiguration = false;
                }
            }
            catch (Exception ex)
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error checking configuration support for plugin {PluginId}: {ex.Message}", ex);
                }
                SupportsConfiguration = false;
            }
        }

        private void UpdateIconLetter(string? iconBackgroundColor = null)
        {
            var displayName = Name;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = PluginId;

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var words = displayName.Split(' ', '-', '_');
                var letters = new List<char>();
                
                foreach (var word in words.Where(w => !string.IsNullOrWhiteSpace(w)))
                {
                    if (char.IsLetter(word[0]))
                        letters.Add(char.ToUpper(word[0]));
                    else if (char.IsDigit(word[0]))
                        letters.Add(word[0]);
                        
                    if (letters.Count >= 2) break;
                }

                if (letters.Count == 0 && displayName.Length > 0)
                {
                    if (char.IsLetter(displayName[0]))
                        letters.Add(char.ToUpper(displayName[0]));
                    else if (char.IsDigit(displayName[0]))
                        letters.Add(displayName[0]);
                }

                IconLetter = new string(letters.Take(2).ToArray());
                
                // Use iconBackground from store.json if provided, otherwise generate based on plugin ID
                if (!string.IsNullOrWhiteSpace(iconBackgroundColor))
                {
                    try
                    {
                        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(iconBackgroundColor);
                        IconBackground = new SolidColorBrush(color);
                    }
                    catch
                    {
                        // If parsing fails, fall back to generated color
                        var hash = PluginId.GetHashCode();
                        var hue = Math.Abs(hash % 360);
                        IconBackground = new SolidColorBrush(HsvToRgb(hue, 0.7, 0.8));
                    }
                }
                else
                {
                    // Generate background color based on plugin ID (stable hash)
                    var hash = PluginId.GetHashCode();
                    var hue = Math.Abs(hash % 360);
                    IconBackground = new SolidColorBrush(HsvToRgb(hue, 0.7, 0.8));
                }
            }
        }

        private static Color HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;

            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else if (h >= 300 && h < 360)
            {
                r = c; g = 0; b = x;
            }

            return Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255)
            );
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
