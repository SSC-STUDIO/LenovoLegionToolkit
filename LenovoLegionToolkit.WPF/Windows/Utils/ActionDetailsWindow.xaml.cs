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
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Windows.Utils
{
    public partial class ActionDetailsWindow : UiWindow
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
                // 设置标题和描述
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

                // 获取技术实现细节
                var details = GetActionImplementationDetails(_actionKey);
                _implementationTypeTextBlock.Text = details.ImplementationType;
                
                // 清空并填充详细信息
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

                // 如果没有详细信息，显示提示
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
                // 根据操作key获取实现细节
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
                details.Add($"获取实现细节时出错: {ex.Message}");
            }

            return (implementationType, details);
        }

        private List<string> GetCleanupCommands(string actionKey)
        {
            var commands = new List<string>();
            
            // 根据actionKey返回对应的命令
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
            
            // 这里需要从WindowsOptimizationService获取注册表项
            // 由于无法直接访问私有字段，我们根据已知的key返回描述
            switch (actionKey)
            {
                case "explorer.taskbar":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - TaskbarDa: 0 (禁用任务栏动画)");
                    tweaks.Add("  - TaskbarAnimations: 0 (禁用任务栏动画效果)");
                    break;
                case "explorer.responsiveness":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - DesktopProcess: 1 (优化桌面进程)");
                    tweaks.Add("  - DisablePreviewDesktop: 1 (禁用桌面预览)");
                    break;
                case "explorer.visibility":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - Hidden: 1 (显示隐藏文件)");
                    tweaks.Add("  - ShowSuperHidden: 0 (不显示系统保护文件)");
                    break;
                case "explorer.suggestions":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    tweaks.Add("  - ShowTaskViewButton: 0 (隐藏任务视图按钮)");
                    tweaks.Add("  - ShowCortanaButton: 0 (隐藏Cortana按钮)");
                    break;
                case "performance.multimedia":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Multimedia\\SystemProfile");
                    tweaks.Add("  - SystemResponsiveness: 0 (优化多媒体响应)");
                    tweaks.Add("  - NetworkThrottlingIndex: 4294967295 (禁用网络节流)");
                    break;
                case "performance.memory":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management");
                    tweaks.Add("  - DisablePagingExecutive: 1 (禁用分页执行)");
                    tweaks.Add("  - LargeSystemCache: 0 (优化系统缓存)");
                    break;
                case "performance.telemetry":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection");
                    tweaks.Add("  - AllowTelemetry: 0 (禁用遥测)");
                    break;
                case "performance.notifications":
                    tweaks.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings");
                    tweaks.Add("  - 禁用各种通知相关的注册表项");
                    break;
                case "network.acceleration":
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters");
                    tweaks.Add("  - TcpAckFrequency: 1 (优化TCP确认频率)");
                    tweaks.Add("  - TCPNoDelay: 1 (禁用Nagle算法)");
                    tweaks.Add("  - Tcp1323Opts: 3 (启用TCP时间戳和窗口缩放)");
                    tweaks.Add("  - DefaultTTL: 64 (设置默认TTL)");
                    tweaks.Add("  - EnablePMTUDiscovery: 1 (启用路径MTU发现)");
                    tweaks.Add("  - GlobalMaxTcpWindowSize: 65535 (增大TCP窗口大小)");
                    tweaks.Add("  - SackOpts: 1 (启用选择性确认)");
                    tweaks.Add("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters");
                    tweaks.Add("  - MaxCacheTtl: 3600 (DNS缓存最大TTL)");
                    tweaks.Add("  - MaxNegativeCacheTtl: 300 (DNS负缓存TTL)");
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
                    services.Add("服务名称: DiagTrack");
                    services.Add("服务名称: diagnosticshub.standardcollector.service");
                    services.Add("服务名称: DoSvc");
                    services.Add("操作: 禁用并停止服务");
                    break;
                case "services.sysmain":
                    services.Add("服务名称: SysMain (Superfetch)");
                    services.Add("操作: 禁用并停止服务");
                    break;
                case "services.search":
                    services.Add("服务名称: WSearch (Windows Search)");
                    services.Add("操作: 禁用并停止服务");
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
                details.Add("注册表修改:");
                details.Add("  HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                details.Add("    - Start_NotifyNewApps: 0");
                details.Add("PowerShell脚本:");
                details.Add("  使用Get-StartApps和Remove-AppxPackage禁用开始菜单应用");
            }
            else if (actionKey == "explorer.winKeySearch")
            {
                details.Add("注册表修改:");
                details.Add("  HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                details.Add("    - Start_SearchFiles: 1 (将Windows键设置为打开搜索)");
                details.Add("");
                details.Add("系统通知:");
                details.Add("  发送 WM_SETTINGCHANGE 消息通知系统设置已更改");
                details.Add("");
                details.Add("资源管理器重启:");
                details.Add("  重启 Windows 资源管理器以立即应用更改");
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

