using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using LenovoLegionToolkit.WPF.Resources;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Windows;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Windows.Utils
{
    public partial class ActionDetailsWindow : BaseWindow
    {
        private readonly string _actionKey;
        private readonly WindowsOptimizationActionDefinition? _actionDefinition;

        public ActionDetailsWindow(string actionKey, WindowsOptimizationActionDefinition? actionDefinition)
        {
            _actionKey = actionKey;
            _actionDefinition = actionDefinition;
            InitializeComponent();
            LoadActionDetails();
        }

        private void LoadActionDetails()
        {
            try
            {
                // Set title and description
                if (_actionDefinition != null)
                {
                    _titleTextBlock.Text = GetResourceString(_actionDefinition.TitleResourceKey) ?? _actionKey;
                    _descriptionTextBlock.Text = GetResourceString(_actionDefinition.DescriptionResourceKey) ?? string.Empty;
                }
                else
                {
                    _titleTextBlock.Text = _actionKey;
                    _descriptionTextBlock.Text = Resource.ActionDetailsWindow_NotFound;
                }

                // Get technical implementation details
                var details = GetActionImplementationDetails(_actionKey);
                _implementationTypeTextBlock.Text = details.ImplementationType;
                
                // Clear and fill in detailed information
                _detailsStackPanel.Children.Clear();
                foreach (var detail in details.Details)
                {
                    var textBlock = new TextBlock
                    {
                        Text = detail,
                        FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New"),
                        FontSize = 12,
                        Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorPrimaryBrush"),
                        Margin = new Thickness(0, 0, 0, 8),
                        TextWrapping = TextWrapping.Wrap
                    };
                    _detailsStackPanel.Children.Add(textBlock);
                }

                // Show prompt if no detailed information is available
                if (details.Details.Count == 0)
                {
                    var noDetailsText = new TextBlock
                    {
                        Text = Resource.ActionDetailsWindow_NoDetailsAvailable,
                        FontSize = 12,
                        Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    _detailsStackPanel.Children.Add(noDetailsText);
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load action details.", ex);
                
                _titleTextBlock.Text = Resource.ActionDetailsWindow_LoadFailed;
                _descriptionTextBlock.Text = ex.Message;
            }
        }

        private (string ImplementationType, List<string> Details) GetActionImplementationDetails(string actionKey)
        {
            var details = new List<string>();
            string implementationType = Resource.ActionDetailsWindow_UnknownImplementation;

            try
            {
                // Get implementation details based on action key
                if (actionKey.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
                {
                    implementationType = Resource.ActionDetailsWindow_CommandExecution;
                    details.AddRange(GetCleanupCommands(actionKey));
                }
                else if (actionKey.StartsWith("explorer.", StringComparison.OrdinalIgnoreCase) ||
                         actionKey.StartsWith("performance.", StringComparison.OrdinalIgnoreCase))
                {
                    implementationType = Resource.ActionDetailsWindow_RegistryModification;
                    details.AddRange(GetRegistryTweaks(actionKey));
                }
                else if (actionKey.StartsWith("services.", StringComparison.OrdinalIgnoreCase))
                {
                    implementationType = Resource.ActionDetailsWindow_ServiceManagement;
                    details.AddRange(GetServiceDetails(actionKey));
                }
                else if (actionKey == "performance.powerPlan")
                {
                    implementationType = Resource.ActionDetailsWindow_CommandExecution;
                    details.AddRange(GetPowerPlanCommands());
                }
                else if (actionKey == "explorer.startMenu" || actionKey == "explorer.winKeySearch")
                {
                    implementationType = Resource.ActionDetailsWindow_RegistryAndScript;
                    details.AddRange(GetExplorerSpecialActions(actionKey));
                }
                else if (actionKey == "cleanup.registry")
            {
                implementationType = Resource.ActionDetailsWindow_RegistryCleanup;
                details.Add(Resource.ActionDetailsWindow_CleanupRegistry);
            }
                else if (actionKey == "cleanup.componentStore")
                {
                    implementationType = Resource.ActionDetailsWindow_DISMCommand;
                    details.AddRange(GetComponentStoreCommands());
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to get implementation details for action: {actionKey}", ex);
                details.Add($"Error getting implementation details: {ex.Message}");
            }

            return (implementationType, details);
        }

        private List<string> GetCleanupCommands(string actionKey)
        {
            var commands = new List<string>();
            
            // Return the corresponding command based on actionKey
            switch (actionKey)
            {
                case "cleanup.browserCache":
                    commands.Add("del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCache\\*\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCookies\\*\" >nul 2>&1");
                    break;
                case "cleanup.thumbnailCache":
                    commands.Add("del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%LocalAppData%\\Local\\D3DSCache\\*\" >nul 2>&1");
                    break;
                case "cleanup.windowsUpdate":
                    commands.Add("del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\Download\\*\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\DeliveryOptimization\\*\" >nul 2>&1");
                    break;
                case "cleanup.tempFiles":
                    commands.Add("del /f /s /q \"%SystemRoot%\\Temp\\*\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%SystemDrive%\\Windows\\Temp\\*\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%TEMP%\\*\" >nul 2>&1");
                    break;
                case "cleanup.logs":
                    commands.Add("del /f /s /q \"%SystemRoot%\\Logs\\*\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%ProgramData%\\Microsoft\\Windows\\WER\\ReportQueue\\*\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%ProgramData%\\Microsoft\\Diagnosis\\*\" >nul 2>&1");
                    break;
                case "cleanup.crashDumps":
                    commands.Add("del /f /s /q \"%SystemRoot%\\Minidump\\*.dmp\" >nul 2>&1");
                    commands.Add("del /f /q \"%SystemRoot%\\memory.dmp\" >nul 2>&1");
                    commands.Add("del /f /s /q \"%SystemDrive%\\*.dmp\" >nul 2>&1");
                    break;
                case "cleanup.recycleBin":
                    commands.Add("rd /s /q \"%SystemDrive%\\$Recycle.bin\" >nul 2>&1");
                    break;
                case "cleanup.defender":
                    commands.Add("del /f /s /q \"%ProgramData%\\Microsoft\\Windows Defender\\Scans\\*\" >nul 2>&1");
                    break;
                case "cleanup.prefetch":
                    commands.Add("del /f /s /q \"%SystemRoot%\\Prefetch\\*\" >nul 2>&1");
                    break;
                case "cleanup.remoteDesktopCache":
                    commands.Add("del /f /s /q \"%LocalAppData%\\Microsoft\\Terminal Server Client\\Cache\\*\" >nul 2>&1");
                    break;
                case "cleanup.dotnetNative":
                    commands.Add("rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_32\" >nul 2>&1");
                    commands.Add("rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_64\" >nul 2>&1");
                    break;
                case "network.optimization":
                    commands.Add(Resource.ActionDetailsWindow_NetworkFlushDNS);
                    commands.Add(Resource.ActionDetailsWindow_NetworkResetWinsock);
                    commands.Add(Resource.ActionDetailsWindow_NetworkResetTCPIP);
                    break;
            }

            return commands;
        }

        private List<string> GetRegistryTweaks(string actionKey)
        {
            var tweaks = new List<string>();
            
            // Need to get registry entries from WindowsOptimizationService here
            // Since we can't directly access private fields, we return descriptions based on known keys
            switch (actionKey)
            {
                case "explorer.taskbar":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - TaskbarDa: 0 (Disable taskbar animations)");
                    tweaks.Add("  - TaskbarAnimations: 0 (Disable taskbar animation effects)");
                    break;
                case "explorer.responsiveness":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - DesktopProcess: 1 (Optimize desktop process)");
                    tweaks.Add("  - DisablePreviewDesktop: 1 (Disable desktop preview)");
                    break;
                case "explorer.visibility":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - Hidden: 1 (Show hidden files)");
                    tweaks.Add("  - ShowSuperHidden: 0 (Don't show system protected files)");
                    break;
                case "explorer.suggestions":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - ShowTaskViewButton: 0 (Hide Task View button)");
                    tweaks.Add("  - ShowCortanaButton: 0 (Hide Cortana button)");
                    break;
                case "performance.multimedia":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Multimedia\\SystemProfile");
                    tweaks.Add("  - SystemResponsiveness: 0 (Optimize multimedia responsiveness)");
                    tweaks.Add("  - NetworkThrottlingIndex: 4294967295 (Disable network throttling)");
                    break;
                case "performance.memory":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management");
                    tweaks.Add("  - DisablePagingExecutive: 1 (Disable paging executive)");
                    tweaks.Add("  - LargeSystemCache: 0 (Optimize system cache)");
                    break;
                case "performance.telemetry":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection");
                    tweaks.Add("  - AllowTelemetry: 0 (Disable telemetry)");
                    break;
                case "performance.notifications":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings");
                    tweaks.Add("  - Disable various notification-related registry entries");
                    break;
                case "network.acceleration":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters");
                    tweaks.Add("  - TcpAckFrequency: 1 (Optimize TCP acknowledgment frequency)");
                    tweaks.Add("  - TCPNoDelay: 1 (Disable Nagle algorithm)");
                    tweaks.Add("  - Tcp1323Opts: 3 (Enable TCP timestamps and window scaling)");
                    tweaks.Add("  - DefaultTTL: 64 (Set default TTL)");
                    tweaks.Add("  - EnablePMTUDiscovery: 1 (Enable Path MTU Discovery)");
                    tweaks.Add("  - GlobalMaxTcpWindowSize: 65535 (Increase TCP window size)");
                    tweaks.Add("  - SackOpts: 1 (Enable selective acknowledgment)");
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters");
                    tweaks.Add("  - MaxCacheTtl: 3600 (DNS cache max TTL)");
                    tweaks.Add("  - MaxNegativeCacheTtl: 300 (DNS negative cache TTL)");
                    break;
            }

            return tweaks;
        }

        private List<string> GetServiceDetails(string actionKey)
        {
            var services = new List<string>();
            
            switch (actionKey)
            {
                case "services.diagnostics":
                    services.Add("Service name: DiagTrack");
                    services.Add("Service name: diagnosticshub.standardcollector.service");
                    services.Add("Service name: DoSvc");
                    services.Add("Action: Disable and stop service");
                    break;
                case "services.sysmain":
                    services.Add("Service name: SysMain (Superfetch)");
                    services.Add("Action: Disable and stop service");
                    break;
                case "services.search":
                    services.Add("Service name: WSearch (Windows Search)");
                    services.Add("Action: Disable and stop service");
                    break;
            }

            return services;
        }

        private List<string> GetPowerPlanCommands()
        {
            return new List<string>
            {
                "powercfg -setactive SCHEME_MIN",
                "powercfg -h off"
            };
        }

        private List<string> GetExplorerSpecialActions(string actionKey)
        {
            var details = new List<string>();
            
            if (actionKey == "explorer.startMenu")
            {
                details.Add("Registry modification:");
                details.Add("  HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                details.Add("    - Start_NotifyNewApps: 0");
                details.Add("PowerShell script:");
                details.Add("  Disable Start Menu apps using Get-StartApps and Remove-AppxPackage");
            }
            else if (actionKey == "explorer.winKeySearch")
            {
                details.Add("Registry modification:");
                details.Add("  HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                details.Add("    - Start_SearchFiles: 1 (Set Windows key to open search)");
                details.Add("");
                details.Add("System notification:");
                details.Add("  Send WM_SETTINGCHANGE message to notify system settings changes");
                details.Add("");
                details.Add("Explorer restart:");
                details.Add("  Restart Windows Explorer to apply changes immediately");
            }

            return details;
        }

        private List<string> GetComponentStoreCommands()
        {
            return new List<string>
            {
                "dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                "del /f /s /q \"%SystemRoot%\\WinSxS\\Temp\\*\" >nul 2>&1"
            };
        }

        private string? GetResourceString(string resourceKey)
        {
            try
            {
                var resourceType = typeof(Resources.Resource);
                var property = resourceType.GetProperty(resourceKey);
                return property?.GetValue(null) as string;
            }
            catch
            {
                return null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

