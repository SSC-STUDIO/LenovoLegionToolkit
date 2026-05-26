using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using UniversalDeviceToolkit.CLI.Lib;

namespace UniversalDeviceToolkit.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            return await BuildCommandLine()
                .Parse(args)
                .InvokeAsync(new InvocationConfiguration
                {
                    EnableDefaultExceptionHandler = false
                }, default)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return OnException(ex);
        }
    }

    private static RootCommand BuildCommandLine()
    {
        var root = new RootCommand("Utility that controls Universal Device Toolkit from command line.\n\n" +
                                   "Universal Device Toolkit must be running in the background and CLI setting must be " +
                                   "turned on for this utility to work.");

        root.Add(BuildQuickActionsCommand());
        root.Add(BuildFeatureCommand());
        root.Add(BuildSpectrumCommand());
        root.Add(BuildRGBCommand());
        root.Add(BuildShellCommand());
        root.Add(BuildStatusCommand());

        return root;
    }

    private static Command BuildStatusCommand()
    {
        var cmd = new Command("status", "Show running app status and startup-related switches");
        cmd.Aliases.Add("st");
        cmd.SetAction(async _ =>
        {
            var result = await IpcClient.GetAppStatusAsync().ConfigureAwait(false);
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildQuickActionsCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "Name of the Quick Action",
            Arity = ArgumentArity.ZeroOrOne
        };

        var listOption = new Option<bool>("--list", "-l")
        {
            Description = "List available Quick Actions",
            Arity = ArgumentArity.ZeroOrOne
        };

        var cmd = new Command("quickAction", "Run Quick Action");
        cmd.Aliases.Add("qa");
        cmd.Add(nameArgument);
        cmd.Add(listOption);
        cmd.SetAction(async parseResult =>
        {
            if (parseResult.GetValue(listOption))
            {
                var result = await IpcClient.ListQuickActionsAsync().ConfigureAwait(false);
                Console.WriteLine(result);
                return;
            }

            var name = parseResult.GetRequiredValue(nameArgument);
            await IpcClient.RunQuickActionAsync(name).ConfigureAwait(false);
        });
        cmd.Validators.Add(result =>
        {
            if (HasArgument(result, nameArgument))
                return;

            if (HasOption(result, listOption))
                return;

            result.AddError($"{nameArgument.Name} or --list should be specified");
        });

        return cmd;
    }

    private static Command BuildFeatureCommand()
    {
        var getCmd = BuildGetFeatureCommand();
        var setCmd = BuildSetFeatureCommand();

        var listOption = new Option<bool>("--list", "-l")
        {
            Description = "List available features",
            Arity = ArgumentArity.ZeroOrOne
        };

        var cmd = new Command("feature", "Control features");
        cmd.Aliases.Add("f");
        cmd.Add(getCmd);
        cmd.Add(setCmd);
        cmd.Add(listOption);
        cmd.SetAction(async parseResult =>
        {
            if (!parseResult.GetValue(listOption))
                return;

            var value = await IpcClient.ListFeaturesAsync().ConfigureAwait(false);
            Console.WriteLine(value);
        });
        cmd.Validators.Add(result =>
        {
            if (HasCommand(result, getCmd))
                return;

            if (HasCommand(result, setCmd))
                return;

            if (HasOption(result, listOption))
                return;

            result.AddError($"{getCmd.Name}, {setCmd.Name} or --list should be specified");
        });

        return cmd;
    }

    private static Command BuildGetFeatureCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "Name of the feature",
            Arity = ArgumentArity.ExactlyOne
        };

        var cmd = new Command("get", "Get value of a feature");
        cmd.Aliases.Add("g");
        cmd.Add(nameArgument);
        cmd.SetAction(async parseResult =>
        {
            var name = parseResult.GetRequiredValue(nameArgument);
            var result = await IpcClient.GetFeatureValueAsync(name).ConfigureAwait(false);
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildSetFeatureCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "Name of the feature",
            Arity = ArgumentArity.ExactlyOne
        };
        var valueArgument = new Argument<string>("value")
        {
            Description = "Value of the feature",
            Arity = ArgumentArity.ZeroOrOne
        };

        var listOption = new Option<bool>("--list", "-l")
        {
            Description = "List available feature values",
            Arity = ArgumentArity.ZeroOrOne
        };

        var cmd = new Command("set", "Set value of a feature");
        cmd.Aliases.Add("s");
        cmd.Add(nameArgument);
        cmd.Add(valueArgument);
        cmd.Add(listOption);
        cmd.SetAction(async parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);

            if (parseResult.GetValue(listOption))
            {
                var result = await IpcClient.ListFeatureValuesAsync(name!).ConfigureAwait(false);
                Console.WriteLine(result);
                return;
            }

            var value = parseResult.GetValue(valueArgument);
            await IpcClient.SetFeatureValueAsync(name!, value!).ConfigureAwait(false);
        });
        cmd.Validators.Add(result =>
        {
            if (HasArgument(result, nameArgument))
                return;

            if (HasOption(result, listOption))
                return;

            result.AddError($"{nameArgument.Name} or --list should be specified");
        });

        return cmd;
    }

    private static Command BuildSpectrumCommand()
    {
        var profileCommand = BuildSpectrumProfileCommand();
        var brightnessCommand = BuildSpectrumBrightnessCommand();

        var cmd = new Command("spectrum", "Control Spectrum backlight");
        cmd.Aliases.Add("s");
        cmd.Add(profileCommand);
        cmd.Add(brightnessCommand);
        return cmd;
    }

    private static Command BuildSpectrumProfileCommand()
    {
        var getCmd = BuildGetSpectrumProfileCommand();
        var setCmd = BuildSetSpectrumProfileCommand();

        var cmd = new Command("profile", "Control Spectrum backlight profile");
        cmd.Aliases.Add("p");
        cmd.Add(getCmd);
        cmd.Add(setCmd);

        return cmd;
    }

    private static Command BuildGetSpectrumProfileCommand()
    {
        var cmd = new Command("get", "Get current Spectrum profile");
        cmd.Aliases.Add("g");
        cmd.SetAction(async _ =>
        {
            var result = await IpcClient.GetSpectrumProfileAsync().ConfigureAwait(false);
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildSetSpectrumProfileCommand()
    {
        var valueArgument = new Argument<int>("profile")
        {
            Description = "Profile to set",
            Arity = ArgumentArity.ExactlyOne
        };

        var cmd = new Command("set", "Set current Spectrum profile");
        cmd.Aliases.Add("s");
        cmd.Add(valueArgument);
        cmd.SetAction(async parseResult =>
        {
            var value = parseResult.GetRequiredValue(valueArgument);
            await IpcClient.SetSpectrumProfileAsync($"{value}").ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command BuildSpectrumBrightnessCommand()
    {
        var getCmd = BuildGetSpectrumBrightnessCommand();
        var setCmd = BuildSetSpectrumBrightnessCommand();

        var cmd = new Command("brightness", "Control Spectrum brightness");
        cmd.Aliases.Add("b");
        cmd.Add(getCmd);
        cmd.Add(setCmd);

        return cmd;
    }

    private static Command BuildGetSpectrumBrightnessCommand()
    {
        var cmd = new Command("get", "Get current Spectrum brightness");
        cmd.Aliases.Add("g");
        cmd.SetAction(async _ =>
        {
            var result = await IpcClient.GetSpectrumBrightnessAsync().ConfigureAwait(false);
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildSetSpectrumBrightnessCommand()
    {
        var valueArgument = new Argument<int>("brightness")
        {
            Description = "Brightness to set",
            Arity = ArgumentArity.ExactlyOne
        };

        var cmd = new Command("set", "Set current Spectrum brightness");
        cmd.Aliases.Add("s");
        cmd.Add(valueArgument);
        cmd.SetAction(async parseResult =>
        {
            var value = parseResult.GetRequiredValue(valueArgument);
            await IpcClient.SetSpectrumBrightnessAsync($"{value}").ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command BuildRGBCommand()
    {
        var getCmd = BuildGetRGBCommand();
        var setCmd = BuildSetRGBCommand();

        var cmd = new Command("rgb", "Control RGB backlight preset");
        cmd.Aliases.Add("r");
        cmd.Add(getCmd);
        cmd.Add(setCmd);

        return cmd;
    }

    private static Command BuildGetRGBCommand()
    {
        var cmd = new Command("get", "Get current RGB preset");
        cmd.Aliases.Add("g");
        cmd.SetAction(async _ =>
        {
            var result = await IpcClient.GetRGBPresetAsync().ConfigureAwait(false);
            Console.WriteLine(result);
        });

        return cmd;
    }

    private static Command BuildSetRGBCommand()
    {
        var valueArgument = new Argument<int>("preset")
        {
            Description = "Preset to set",
            Arity = ArgumentArity.ExactlyOne
        };

        var cmd = new Command("set", "Set current RGB preset");
        cmd.Aliases.Add("s");
        cmd.Add(valueArgument);
        cmd.SetAction(async parseResult =>
        {
            var value = parseResult.GetRequiredValue(valueArgument);
            await IpcClient.SetRGBPresetAsync($"{value}").ConfigureAwait(false);
        });

        return cmd;
    }

    private static Command BuildShellCommand()
    {
        var statusOption = CreateFlagOption("--status", "-s", "Check current registration status");
        var installOption = CreateFlagOption("--install", "-i", "Install Nilesoft Shell");
        var uninstallOption = CreateFlagOption("--uninstall", "-x", "Uninstall Nilesoft Shell");
        var installStatusOption = CreateFlagOption("--install-status", "-is", "Check current installation status");

        var cmd = new Command("shell", "Manage shell context menu extension (Nilesoft Shell)");
        cmd.Aliases.Add("sh");
        cmd.Add(statusOption);
        cmd.Add(installOption);
        cmd.Add(uninstallOption);
        cmd.Add(installStatusOption);
        cmd.SetAction(async parseResult =>
        {
            var status = parseResult.GetValue(statusOption);
            var install = parseResult.GetValue(installOption);
            var uninstall = parseResult.GetValue(uninstallOption);
            var installStatus = parseResult.GetValue(installStatusOption);

            if (status)
            {
                var isRegistered = await IpcClient.IsShellRegisteredAsync().ConfigureAwait(false);
                Console.WriteLine(isRegistered ? "Shell is registered" : "Shell is not registered");
                return;
            }

            if (installStatus)
            {
                var isInstalled = await IpcClient.IsShellInstalledAsync().ConfigureAwait(false);
                Console.WriteLine(isInstalled ? "Shell is installed" : "Shell is not installed");
                return;
            }

            if (install)
            {
                await IpcClient.InstallShellAsync().ConfigureAwait(false);
                Console.WriteLine("Shell installation initiated");
                return;
            }

            if (uninstall)
            {
                await IpcClient.UninstallShellAsync().ConfigureAwait(false);
                Console.WriteLine("Shell uninstallation initiated");
                return;
            }

            Console.WriteLine("Please specify an action:");
            Console.WriteLine("  --status (-s): Check current registration status");
            Console.WriteLine("  --install (-i): Install Nilesoft Shell");
            Console.WriteLine("  --uninstall (-x): Uninstall Nilesoft Shell");
            Console.WriteLine("  --install-status (-is): Check current installation status");
        });
        cmd.Validators.Add(result =>
        {
            var optionCount = 0;
            if (HasOption(result, statusOption)) optionCount++;
            if (HasOption(result, installStatusOption)) optionCount++;
            if (HasOption(result, installOption)) optionCount++;
            if (HasOption(result, uninstallOption)) optionCount++;

            if (optionCount > 1)
            {
                result.AddError("Please specify only one action at a time");
                return;
            }

            if (optionCount == 0)
                result.AddError("At least one action option should be specified");
        });

        return cmd;
    }

    private static Option<bool> CreateFlagOption(string name, string alias, string description) =>
        new(name, alias)
        {
            Description = description,
            Arity = ArgumentArity.ZeroOrOne
        };

    private static bool HasArgument<T>(SymbolResult result, Argument<T> argument) =>
        result.GetResult(argument) is not null;

    private static bool HasCommand(SymbolResult result, Command command) =>
        result.GetResult(command) is not null;

    private static bool HasOption(SymbolResult result, Option option) =>
        result.GetResult(option) is { Implicit: false };

    private static int OnException(Exception ex)
    {
        var message = ex switch
        {
            IpcConnectException => "Failed to connect. " +
                                   "Make sure that Universal Device Toolkit is running " +
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

        Console.Error.WriteLine(message);

        if (!Console.IsOutputRedirected)
            Console.ResetColor();

        return exitCode;
    }
}
