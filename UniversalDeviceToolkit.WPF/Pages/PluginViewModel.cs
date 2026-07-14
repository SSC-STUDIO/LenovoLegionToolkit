using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using LenovoLegionToolkit.Lib.Plugins;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Pages
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
        private bool _supportsConfiguration;
        private bool _supportsFeaturePage;
        private bool _supportsOptimizationCategory;
        private bool _supportsExecutableEntryPoint;
        private bool _isInstalling;
        private double _installProgress;
        private string _installStatusText = string.Empty;
        private bool _updateAvailable;
        private bool _isLocal;
        private string _location = string.Empty;
        private bool _shouldShowInstallButton;
        private string _newVersion = string.Empty;
        private string _releaseDate = string.Empty;
        private string _changelog = string.Empty;
        private string _author = string.Empty;
        private string _detailedDescription = string.Empty;
        private string _usageGuide = string.Empty;
        private IReadOnlyList<string> _tags = Array.Empty<string>();
        private bool _isDetailsExpanded;

        public string NewVersion
        {
            get => _newVersion;
            set
            {
                if (_newVersion != value)
                {
                    _newVersion = value;
                    OnPropertyChanged(nameof(NewVersion));
                    OnPropertyChanged(nameof(HasUpdateDetails));
                    OnPropertyChanged(nameof(HasExpandableContent));
                    OnPropertyChanged(nameof(ShowExpandedDetails));
                }
            }
        }

        public string ReleaseDate
        {
            get => _releaseDate;
            set
            {
                if (_releaseDate != value)
                {
                    _releaseDate = value;
                    OnPropertyChanged(nameof(ReleaseDate));
                    OnPropertyChanged(nameof(HasReleaseDate));
                    OnPropertyChanged(nameof(HasUpdateDetails));
                    OnPropertyChanged(nameof(HasExpandableContent));
                    OnPropertyChanged(nameof(ShowExpandedDetails));
                }
            }
        }

        public string Changelog
        {
            get => _changelog;
            set
            {
                if (_changelog != value)
                {
                    _changelog = value;
                    OnPropertyChanged(nameof(Changelog));
                    OnPropertyChanged(nameof(HasChangelog));
                    OnPropertyChanged(nameof(HasChangelogUrl));
                    OnPropertyChanged(nameof(HasUpdateDetails));
                    OnPropertyChanged(nameof(HasExpandableContent));
                    OnPropertyChanged(nameof(ShowExpandedDetails));
                }
            }
        }

        public bool HasReleaseDate => !string.IsNullOrWhiteSpace(_releaseDate);

        public bool HasChangelog => !string.IsNullOrWhiteSpace(_changelog);

        public bool HasChangelogUrl =>
            Uri.TryCreate(_changelog, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

        public bool HasUpdateDetails => UpdateInfoVisible &&
            (!string.IsNullOrWhiteSpace(NewVersion) ||
             !string.IsNullOrWhiteSpace(ReleaseDate) ||
             !string.IsNullOrWhiteSpace(Changelog));

        public string DetailedDescription
        {
            get => _detailedDescription;
            set
            {
                if (_detailedDescription != value)
                {
                    _detailedDescription = value;
                    OnPropertyChanged(nameof(DetailedDescription));
                    OnPropertyChanged(nameof(HasDetailedDescription));
                    OnPropertyChanged(nameof(HasExpandableContent));
                    OnPropertyChanged(nameof(ShowExpandedDetails));
                }
            }
        }

        public bool HasDetailedDescription => !string.IsNullOrWhiteSpace(_detailedDescription);

        public string UsageGuide
        {
            get => _usageGuide;
            set
            {
                if (_usageGuide != value)
                {
                    _usageGuide = value;
                    OnPropertyChanged(nameof(UsageGuide));
                    OnPropertyChanged(nameof(HasUsageGuide));
                    OnPropertyChanged(nameof(HasExpandableContent));
                    OnPropertyChanged(nameof(ShowExpandedDetails));
                }
            }
        }

        public bool HasUsageGuide => !string.IsNullOrWhiteSpace(_usageGuide);

        public IReadOnlyList<string> Tags
        {
            get => _tags;
            set
            {
                value ??= Array.Empty<string>();
                if (!_tags.SequenceEqual(value))
                {
                    _tags = value.ToArray();
                    OnPropertyChanged(nameof(Tags));
                    OnPropertyChanged(nameof(HasTags));
                }
            }
        }

        public bool HasTags => _tags.Count > 0;

        public bool IsDetailsExpanded
        {
            get => _isDetailsExpanded;
            set
            {
                if (_isDetailsExpanded != value)
                {
                    _isDetailsExpanded = value;
                    OnPropertyChanged(nameof(IsDetailsExpanded));
                    OnPropertyChanged(nameof(ShowExpandedDetails));
                    OnPropertyChanged(nameof(ToggleDetailsText));
                    OnPropertyChanged(nameof(ToggleDetailsTooltip));
                    OnPropertyChanged(nameof(ToggleDetailsSymbol));
                }
            }
        }

        public bool HasExpandableContent => HasDetailedDescription || HasUsageGuide || HasUpdateDetails;

        public bool ShowExpandedDetails => IsDetailsExpanded && HasExpandableContent;

        public string DetailsLabel => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_DetailsLabel", "Details", Resource.Culture);

        public string UsageGuideLabel => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_UsageGuideLabel", "Usage Guide", Resource.Culture);

        public string ToggleDetailsText => IsDetailsExpanded
            ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_CollapseDetails", "Hide details", Resource.Culture)
            : LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_ShowDetails", "Show details", Resource.Culture);

        public string ToggleDetailsTooltip => ToggleDetailsText;

        public string ToggleDetailsSymbol => IsDetailsExpanded ? "ChevronUp24" : "ChevronDown24";

        public bool ShouldShowStatusBadge => IsInstalled || UpdateAvailable || IsLocal;

        public string StatusText
        {
            get
            {
                if (IsInstalling)
                    return string.IsNullOrWhiteSpace(InstallStatusText)
                        ? Resource.PluginExtensionsPage_PreparingDownload
                        : InstallStatusText;

                if (_updateAvailable)
                    return Resource.PluginExtensionsPage_CapabilityUpdate;

                if (IsInstalled)
                    return Resource.PluginExtensionsPage_PluginInstalled;

                return Resource.PluginExtensionsPage_InstallPlugin;
            }
        }

        public bool ShouldShowInstalledActions => IsInstalled && !IsInstalling;

        public bool ShouldShowConfigureButton => IsInstalled && SupportsConfiguration && !IsInstalling;

        public string CapabilitySummary
        {
            get
            {
                var capabilities = new List<string>();

                if (SupportsConfiguration)
                    capabilities.Add(Resource.PluginExtensionsPage_CapabilitySettings);

                if (SupportsOpenAction)
                    capabilities.Add(Resource.PluginExtensionsPage_CapabilityQuickOpen);

                if (SupportsOptimizationCategory)
                    capabilities.Add(Resource.PluginExtensionsPage_CapabilityOptimize);

                if (_updateAvailable)
                    capabilities.Add(Resource.PluginExtensionsPage_CapabilityUpdate);

                if (IsInstalled && capabilities.Count == 0)
                    return Resource.PluginExtensionsPage_PluginInstalled;

                return capabilities.Count > 0
                    ? string.Join(" / ", capabilities)
                    : LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_Available", "Available", Resource.Culture);
            }
        }

        public string Author
        {
            get => _author;
            set
            {
                if (_author != value)
                {
                    _author = value;
                    OnPropertyChanged(nameof(Author));
                }
            }
        }

        public bool ShouldShowInstallButton
        {
            get => _shouldShowInstallButton;
            set
            {
                if (_shouldShowInstallButton != value)
                {
                    _shouldShowInstallButton = value;
                    OnPropertyChanged(nameof(ShouldShowInstallButton));
                }
            }
        }

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

        public string Location
        {
            get => _location;
            set
            {
                if (_location != value)
                {
                    _location = value;
                    OnPropertyChanged(nameof(Location));
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

        public string UninstallButtonText => Resource.PluginExtensionsPage_Uninstall;

        public string ConfigureButtonText => Resource.PluginExtensionsPage_Configure;

        public string OpenButtonText => Resource.PluginExtensionsPage_Open;

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
                    OnPropertyChanged(nameof(ShouldShowStatusBadge));
                    OnPropertyChanged(nameof(ShouldShowInstalledActions));
                    OnPropertyChanged(nameof(ShouldShowConfigureButton));
                    OnPropertyChanged(nameof(SupportsOpenAction));
                    OnPropertyChanged(nameof(CapabilitySummary));
                    OnPropertyChanged(nameof(UpdateInfoVisible));
                    OnPropertyChanged(nameof(HasUpdateDetails));
                    OnPropertyChanged(nameof(HasExpandableContent));
                    OnPropertyChanged(nameof(ShowExpandedDetails));
                    OnPropertyChanged(nameof(StatusText));
                    
                    // Update button text when installation status changes
                    UpdateInstallButtonText();
                }
            }
        }

        public bool UpdateAvailable => _updateAvailable;

        public bool UpdateInfoVisible => _isInstalled && _updateAvailable;

        public bool SupportsConfiguration
        {
            get => _supportsConfiguration;
            set
            {
                if (_supportsConfiguration != value)
                {
                    _supportsConfiguration = value;
                    OnPropertyChanged(nameof(SupportsConfiguration));
                    OnPropertyChanged(nameof(ShouldShowConfigureButton));
                    OnPropertyChanged(nameof(SupportsOpenAction));
                    OnPropertyChanged(nameof(ShouldShowInstalledActions));
                    OnPropertyChanged(nameof(CapabilitySummary));
                }
            }
        }

        public bool SupportsFeaturePage
        {
            get => _supportsFeaturePage;
            set
            {
                if (_supportsFeaturePage != value)
                {
                    _supportsFeaturePage = value;
                    OnPropertyChanged(nameof(SupportsFeaturePage));
                    OnPropertyChanged(nameof(SupportsOpenAction));
                    OnPropertyChanged(nameof(ShouldShowInstalledActions));
                    OnPropertyChanged(nameof(CapabilitySummary));
                }
            }
        }

        public bool SupportsOptimizationCategory
        {
            get => _supportsOptimizationCategory;
            set
            {
                if (_supportsOptimizationCategory != value)
                {
                    _supportsOptimizationCategory = value;
                    OnPropertyChanged(nameof(SupportsOptimizationCategory));
                    OnPropertyChanged(nameof(SupportsOpenAction));
                    OnPropertyChanged(nameof(ShouldShowInstalledActions));
                    OnPropertyChanged(nameof(CapabilitySummary));
                }
            }
        }

        public bool SupportsExecutableEntryPoint
        {
            get => _supportsExecutableEntryPoint;
            set
            {
                if (_supportsExecutableEntryPoint != value)
                {
                    _supportsExecutableEntryPoint = value;
                    OnPropertyChanged(nameof(SupportsExecutableEntryPoint));
                    OnPropertyChanged(nameof(SupportsOpenAction));
                    OnPropertyChanged(nameof(ShouldShowInstalledActions));
                    OnPropertyChanged(nameof(CapabilitySummary));
                }
            }
        }

        public bool SupportsOpenAction => _supportsFeaturePage || _supportsOptimizationCategory || _supportsExecutableEntryPoint || _supportsConfiguration;

        public bool IsInstalling
        {
            get => _isInstalling;
            set
            {
                if (_isInstalling != value)
                {
                    _isInstalling = value;
                    OnPropertyChanged(nameof(IsInstalling));
                    OnPropertyChanged(nameof(ShouldShowInstalledActions));
                    OnPropertyChanged(nameof(ShouldShowConfigureButton));
                    OnPropertyChanged(nameof(SupportsOpenAction));
                    OnPropertyChanged(nameof(CapabilitySummary));
                    OnPropertyChanged(nameof(SecondaryLineText));
                    OnPropertyChanged(nameof(ShouldShowSecondaryLine));
                    OnPropertyChanged(nameof(IsInstallProgressIndeterminate));
                    OnPropertyChanged(nameof(ShouldShowDeterminateInstallFill));
                    OnPropertyChanged(nameof(ShouldShowIndeterminateInstallFill));
                    OnPropertyChanged(nameof(ShouldShowCardDescription));
                    OnPropertyChanged(nameof(ShouldShowStatusBadge));
                    OnPropertyChanged(nameof(StatusText));
                    UpdateInstallButtonText();
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
                    OnPropertyChanged(nameof(SecondaryLineText));
                    OnPropertyChanged(nameof(ShouldShowSecondaryLine));
                    OnPropertyChanged(nameof(IsInstallProgressIndeterminate));
                    OnPropertyChanged(nameof(ShouldShowDeterminateInstallFill));
                    OnPropertyChanged(nameof(ShouldShowIndeterminateInstallFill));
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
                    OnPropertyChanged(nameof(SecondaryLineText));
                    OnPropertyChanged(nameof(ShouldShowSecondaryLine));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string SecondaryLineText
        {
            get
            {
                // Do not surface capability chips (settings / quick open / optimize) on plugin cards.
                if (!IsInstalling)
                    return string.Empty;

                if (!string.IsNullOrWhiteSpace(InstallStatusText))
                {
                    return InstallProgress > 0
                        ? $"{InstallStatusText}  ·  {InstallProgress:0}%"
                        : InstallStatusText;
                }

                return Resource.PluginExtensionsPage_PreparingDownload;
            }
        }

        public bool ShouldShowSecondaryLine => !string.IsNullOrWhiteSpace(SecondaryLineText);

        public bool IsInstallProgressIndeterminate => IsInstalling && InstallProgress <= 0;

        public bool ShouldShowDeterminateInstallFill => IsInstalling && InstallProgress > 0;

        public bool ShouldShowIndeterminateInstallFill => IsInstalling && InstallProgress <= 0;

        public bool ShouldShowCardDescription => !IsInstalling && !string.IsNullOrWhiteSpace(Description);

        public bool IsLocal
        {
            get => _isLocal;
            set
            {
                if (_isLocal != value)
                {
                    _isLocal = value;
                    OnPropertyChanged(nameof(IsLocal));
                }
            }
        }

        public IPlugin Plugin { get; private set; }

    public PluginViewModel(IPlugin plugin, bool isInstalled, bool updateAvailable = false, string version = "1.0.0", bool isLocal = false)
    {
        Plugin = plugin;
        PluginId = plugin.Id;
        Name = plugin.Name ?? plugin.Id;
        Description = plugin.Description ?? string.Empty;
        Version = $"v{version}";
        IsInstalled = isInstalled;
        _updateAvailable = updateAvailable;
        IsLocal = isLocal;
        
        UpdateInstallButtonText();
        UpdateIconLetter();
    }

        private void UpdateInstallButtonText()
        {
            var oldText = InstallButtonText;
            
            if (IsInstalled)
            {
                InstallButtonText = _updateAvailable ? Resource.Update : Resource.PluginExtensionsPage_PluginInstalled;
            }
            else
            {
                InstallButtonText = Resource.PluginExtensionsPage_InstallPlugin;
            }
            
            ShouldShowInstallButton = (!IsInstalled || _updateAvailable) && !IsInstalling;
            
            // Add debug logging
            try
            {
                var logger = LenovoLegionToolkit.Lib.Utils.Log.Instance;
                if (logger.IsTraceEnabled)
                {
                    logger.Trace($"UpdateInstallButtonText for {PluginId}: {oldText} -> {InstallButtonText} (IsInstalled={IsInstalled}, UpdateAvailable={_updateAvailable})");
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
                OnPropertyChanged(nameof(UpdateAvailable));
                OnPropertyChanged(nameof(ShouldShowStatusBadge));
                OnPropertyChanged(nameof(UpdateInfoVisible));
                OnPropertyChanged(nameof(HasUpdateDetails));
                OnPropertyChanged(nameof(HasExpandableContent));
                OnPropertyChanged(nameof(ShowExpandedDetails));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CapabilitySummary));
                UpdateInstallButtonText();
            }
        }

        public void ToggleDetails()
        {
            if (!HasExpandableContent)
                return;

            IsDetailsExpanded = !IsDetailsExpanded;
        }

        public void SetIconBackgroundFromStore(string? iconBackgroundValue)
        {
            if (TryParseColor(iconBackgroundValue, out var parsed))
            {
                IconBackground = new SolidColorBrush(parsed);
                return;
            }

            ApplyDeterministicIconBackground();
        }

        private void UpdateIconLetter()
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
                ApplyDeterministicIconBackground();
            }
        }

        private void ApplyDeterministicIconBackground()
        {
            var hash = GetDeterministicHash(PluginId);
            var hue = Math.Abs(hash % 360);
            IconBackground = new SolidColorBrush(HsvToRgb(hue, 0.7, 0.8));
        }

        private static int GetDeterministicHash(string value)
        {
            unchecked
            {
                var hash = 17;
                foreach (var ch in value)
                {
                    hash = hash * 31 + char.ToUpperInvariant(ch);
                }

                return hash;
            }
        }

        private static bool TryParseColor(string? raw, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var candidate = raw.Trim();
            if (!candidate.StartsWith("#", StringComparison.Ordinal) &&
                (candidate.Length == 3 || candidate.Length == 4 || candidate.Length == 6 || candidate.Length == 8) &&
                candidate.All(IsHexDigit))
            {
                candidate = $"#{candidate}";
            }

            try
            {
                var converted = ColorConverter.ConvertFromString(candidate);
                if (converted is Color parsed)
                {
                    color = parsed;
                    return true;
                }
            }
            catch (FormatException)
            {
                return false;
            }

            return false;
        }

        private static bool IsHexDigit(char c) =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F');

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
