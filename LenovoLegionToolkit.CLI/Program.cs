using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using LenovoLegionToolkit.CLI.Lib;

namespace LenovoLegionToolkit.CLI;

public class Program
{
    public static Task<int> Main(string[] args) => BuildCommandLine().InvokeAsync(args);

    private static Parser BuildCommandLine()
    {
        var root = new RootCommand("Utility that controls Lenovo Legion Toolkit from command line.\n\n" +
                                   "Lenovo Legion Toolkit must be running in the background and CLI setting must be " +
                                   "turned on for this utility to work.");

        var builder = new CommandLineBuilder(root)
            .UseDefaults()
            .UseExceptionHandler(OnException);

        root.AddCommand(BuildQuickActionsCommand());
        root.AddCommand(BuildFeatureCommand());
        root.AddCommand(BuildSpectrumCommand());
        root.AddCommand(BuildRGBCommand());
        root.AddCommand(BuildShellCommand()); // Add shell management command

        return builder.Build();
    }

    private static Command BuildQuickActionsCommand()
    {
        var nameArgument = new Argument<string>("name", "Name of the Quick Action") { Arity = ArgumentArity.ZeroOrOne };

        var listOption = new Option<bool>("--list", "List available Quick Actions") { Arity = ArgumentArity.ZeroOrOne };
        listOption.AddAlias("-l");

        var cmd = new Command("quickAction", "Run Quick Action");
        cmd.AddAlias("qa");
        cmd.AddArgument(nameArgument);
        cmd.AddOption(listOption);
        cmd.SetHandler(async (name, list) =>
        {
            if (list)
            {
                var result = await IpcClient.ListQuickActionsAsync();
                Console.WriteLine(result);
                return;
            }

            await IpcClient.RunQuickActionAsync(name);
        }, nameArgument, listOption);
        cmd.AddValidator(result =>
        {
            if (result.FindResultFor(nameArgument) is not null)
                return;

            if (result.FindResultFor(listOption) is not null)
                return;

            result.ErrorMessage = $"{nameArgument.Name} or --{listOption.Name} should be specified";
        });

        return cmd;
    }

    private static Command BuildFeatureCommand()
    {
        var getCmd = BuildGetFeatureCommand();
        var setCmd = BuildSetFeatureCommand();

        var listOption = new Option<bool?>("--list", "List available features") { Arity = ArgumentArity.ZeroOrOne };
        listOption.AddAlias("-l");

        var cmd = new Command("feature", "Control features");
        cmd.AddAlias("f");
        cmd.AddCommand(getCmd);
        cmd.AddCommand(setCmd);
        cmd.AddOption(listOption);
        cmd.SetHandler(async list =>
        {
            if (!list.HasValue || !list.Value)
                return;

            var value = await IpcClient.ListFeaturesAsync();
            Console.WriteLine(value);
        }, listOption);
        cmd.AddValidator(result =>
        {
            if (result.FindResultFor(getCmd) is not null)
                return;

            if (result.FindResultFor(setCmd) is not null)
                return;

            if (result.FindResultFor(listOption) is not null)
                return;

            result.ErrorMessage = $"{getCmd.Name}, {setCmd.Name} or --{listOption.Name} should be specified";
        });

        return cmd;
    }

    private static Command BuildGetFeatureCommand()
    {
        var nameArgument = new Argument<string>("name", "Name of the feature") { Arity = ArgumentArity.ExactlyOne };

        var cmd = new Command("get", "Get value of a feature");
        cmd.AddAlias("g");
        cmd.AddArgument(nameArgument);
        cmd.SetHandler(async name =>
        {
            var result = await IpcClient.GetFeatureValueAsync(name);
            Console.WriteLine(result);
        }, nameArgument);

        return cmd;
    }

    private static Command BuildSetFeatureCommand()
    {
        var nameArgument = new Argument<string>("name", "Name of the feature") { Arity = ArgumentArity.ExactlyOne };
        var valueArgument = new Argument<string>("value", "Value of the feature") { Arity = ArgumentArity.ZeroOrOne };

        var listOption = new Option<bool>("--list", "List available feature values") { Arity = ArgumentArity.ZeroOrOne };
        listOption.AddAlias("-l");

        var cmd = new Command("set", "Set value of a feature");
        cmd.AddAlias("s");
        cmd.AddArgument(nameArgument);
        cmd.AddArgument(valueArgument);
        cmd.AddOption(listOption);
        cmd.SetHandler(async (name, value, list) =>
        {
            if (list)
            {
                var result = await IpcClient.ListFeatureValuesAsync(name);
                Console.WriteLine(result);
                return;
            }

            await IpcClient.SetFeatureValueAsync(name, value);
        }, nameArgument, valueArgument, listOption);
        cmd.AddValidator(result =>
        {
            if (result.FindResultFor(nameArgument) is not null)
                return;

            if (result.FindResultFor(listOption) is not null)
                return;

            result.ErrorMessage = $"{nameArgument.Name} or --{listOption.Name} should be specified";
        });

        return cmd;
    }

    private static Command BuildSpectrumCommand()
    {
        var profileCommand = BuildSpectrumProfileCommand();
        var brightnessCommand = BuildSpectrumBrightnessCommand();

        var cmd = new Command("spectrum", "Control Spectrum backlight");
        cmd.AddAlias("s");
        cmd.AddCommand(profileCommand);
        cmd.AddCommand(brightnessCommand);
        return cmd;
    }

    private static Command BuildSpectrumProfileCommand()
    {
        var getCmd = BuildGetSpectrumProfileCommand();
        var setCmd = BuildSetSpectrumProfileCommand();

        var cmd = new Command("profile", "Control Spectrum backlight profile");
        cmd.AddAlias("p");
        cmd.AddCommand(getCmd);
        cmd.AddCommand(setCmd);

        return cmd;
    }

    private static Command BuildGetSpectrumProfileCommand()
    {
        var cmd = new Command("get", "Get current Spectrum profile");
        cmd.AddAlias("g");
        cmd.SetHandler(async _ =>
        {
            var result = await IpcClient.GetSpectrumProfileAsync();
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildSetSpectrumProfileCommand()
    {
        var valueArgument = new Argument<int>("profile", "Profile to set") { Arity = ArgumentArity.ExactlyOne };

        var cmd = new Command("set", "Set current Spectrum profile");
        cmd.AddAlias("s");
        cmd.AddArgument(valueArgument);
        cmd.SetHandler(async value =>
        {
            await IpcClient.SetSpectrumProfileAsync($"{value}");
        }, valueArgument);

        return cmd;
    }

    private static Command BuildSpectrumBrightnessCommand()
    {
        var getCmd = BuildGetSpectrumBrightnessCommand();
        var setCmd = BuildSetSpectrumBrightnessCommand();

        var cmd = new Command("brightness", "Control Spectrum brightness");
        cmd.AddAlias("b");
        cmd.AddCommand(getCmd);
        cmd.AddCommand(setCmd);

        return cmd;
    }

    private static Command BuildGetSpectrumBrightnessCommand()
    {
        var cmd = new Command("get", "Get current Spectrum brightness");
        cmd.AddAlias("g");
        cmd.SetHandler(async _ =>
        {
            var result = await IpcClient.GetSpectrumBrightnessAsync();
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildSetSpectrumBrightnessCommand()
    {
        var valueArgument = new Argument<int>("brightness", "Brightness to set") { Arity = ArgumentArity.ExactlyOne };

        var cmd = new Command("set", "Set current Spectrum brightness");
        cmd.AddAlias("s");
        cmd.AddArgument(valueArgument);
        cmd.SetHandler(async value =>
        {
            await IpcClient.SetSpectrumBrightnessAsync($"{value}");
        }, valueArgument);

        return cmd;
    }

    private static Command BuildRGBCommand()
    {
        var getCmd = BuildGetRGBCommand();
        var setCmd = BuildSetRGBCommand();

        var cmd = new Command("rgb", "Control RGB backlight preset");
        cmd.AddAlias("r");
        cmd.AddCommand(getCmd);
        cmd.AddCommand(setCmd);

        return cmd;
    }

    private static Command BuildGetRGBCommand()
    {
        var cmd = new Command("get", "Get current RGB preset");
        cmd.AddAlias("g");
        cmd.SetHandler(async _ =>
        {
            var result = await IpcClient.GetRGBPresetAsync();
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildSetRGBCommand()
    {
        var valueArgument = new Argument<int>("preset", "Preset to set") { Arity = ArgumentArity.ExactlyOne };

        var cmd = new Command("set", "Set current RGB preset");
        cmd.AddAlias("s");
        cmd.AddArgument(valueArgument);
        cmd.SetHandler(async value =>
        {
            await IpcClient.SetRGBPresetAsync($"{value}");
        }, valueArgument);

        return cmd;
    }

    private static Command BuildShellCommand()
    {
        var registerOption = new Option<bool>("--register", "Register shell context menu extension") { Arity = ArgumentArity.ZeroOrOne };
        registerOption.AddAlias("-r");

        var unregisterOption = new Option<bool>("--unregister", "Unregister shell context menu extension") { Arity = ArgumentArity.ZeroOrOne };
        unregisterOption.AddAlias("-u");

        var restartOption = new Option<bool>("--restart", "Restart explorer after changes") { Arity = ArgumentArity.ZeroOrOne };
        restartOption.AddAlias("-e");

        var treatOption = new Option<bool>("--treat", "Apply theme treatment") { Arity = ArgumentArity.ZeroOrOne };
        treatOption.AddAlias("-t");

        var statusOption = new Option<bool>("--status", "Check current registration status") { Arity = ArgumentArity.ZeroOrOne };
        statusOption.AddAlias("-s");

        var installOption = new Option<bool>("--install", "Install Nilesoft Shell") { Arity = ArgumentArity.ZeroOrOne };
        installOption.AddAlias("-i");

        var uninstallOption = new Option<bool>("--uninstall", "Uninstall Nilesoft Shell") { Arity = ArgumentArity.ZeroOrOne };
        uninstallOption.AddAlias("-x");

        var installStatusOption = new Option<bool>("--install-status", "Check current installation status") { Arity = ArgumentArity.ZeroOrOne };
        installStatusOption.AddAlias("-is");

        var cmd = new Command("shell", "Manage shell context menu extension (Nilesoft Shell)");
        cmd.AddAlias("sh");
        cmd.AddOption(registerOption);
        cmd.AddOption(unregisterOption);
        cmd.AddOption(restartOption);
        cmd.AddOption(treatOption);
        cmd.AddOption(statusOption);
        cmd.AddOption(installOption);
        cmd.AddOption(uninstallOption);
        cmd.AddOption(installStatusOption);
        cmd.SetHandler(async (register, unregister, restart, treat, status, install, uninstall, installStatus) =>
        {
            // 处理状态查询
            if (status)
            {
                var isRegistered = await IpcClient.IsShellRegisteredAsync();
                Console.WriteLine(isRegistered ? "Shell is registered" : "Shell is not registered");
                return;
            }

            if (installStatus)
            {
                var isInstalled = await IpcClient.IsShellInstalledAsync();
                Console.WriteLine(isInstalled ? "Shell is installed" : "Shell is not installed");
                return;
            }

            // 处理安装/卸载
            if (install)
            {
                await IpcClient.InstallShellAsync();
                Console.WriteLine("Shell installation initiated");
                return;
            }

            if (uninstall)
            {
                await IpcClient.UninstallShellAsync();
                Console.WriteLine("Shell uninstallation initiated");
                return;
            }

            // Shell command execution removed - use install/uninstall instead
            if (register || unregister || restart || treat)
            {
                Console.WriteLine("Shell command execution has been removed. Please use:");
                Console.WriteLine("  --install (-i): Install Nilesoft Shell (includes registration)");
                Console.WriteLine("  --uninstall (-x): Uninstall Nilesoft Shell (includes unregistration)");
                return;
            }
            else
            {
                Console.WriteLine("Please specify an action:");
                Console.WriteLine("  --status (-s): Check current registration status");
                Console.WriteLine("  --install (-i): Install Nilesoft Shell");
                Console.WriteLine("  --uninstall (-x): Uninstall Nilesoft Shell");
                Console.WriteLine("  --install-status (-is): Check current installation status");
            }
        }, registerOption, unregisterOption, restartOption, treatOption, statusOption, installOption, uninstallOption, installStatusOption);
        cmd.AddValidator(result =>
        {
            // 确保只指定了一个操作
            int optionCount = 0;
            if (result.FindResultFor(statusOption) is not null) optionCount++;
            if (result.FindResultFor(installStatusOption) is not null) optionCount++;
            if (result.FindResultFor(installOption) is not null) optionCount++;
            if (result.FindResultFor(uninstallOption) is not null) optionCount++;
            if (result.FindResultFor(registerOption) is not null) optionCount++;
            if (result.FindResultFor(unregisterOption) is not null) optionCount++;
            if (result.FindResultFor(restartOption) is not null) optionCount++;
            if (result.FindResultFor(treatOption) is not null) optionCount++;

            if (optionCount > 1)
            {
                result.ErrorMessage = "Please specify only one action at a time";
                return;
            }

            if (optionCount == 0)
            {
                result.ErrorMessage = "At least one action option should be specified";
                return;
            }
        });



        return cmd;

    }



    private static void OnException(Exception ex, InvocationContext context)

    {

        var message = ex switch

        {

            IpcConnectException => "Failed to connect. " +

                                   "Make sure that Lenovo Legion Toolkit is running " +

                                   "in background and CLI is enabled in Settings.",

            IpcException => ex.Message,

            _ => ex.ToString()

        };

        var exitCode = ex switch

        {

            IpcConnectException => -1,

            IpcException => -2,

            _ => -99

        };



        if (!Console.IsOutputRedirected)

        {

            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;

        }



        context.Console.Error.WriteLine(message);

        context.ExitCode = exitCode;



        if (!Console.IsOutputRedirected)

            Console.ResetColor();

    }
}