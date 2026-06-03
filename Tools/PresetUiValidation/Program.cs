using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.WPF;
using UniversalDeviceToolkit.WPF.Controls;
using UniversalDeviceToolkit.WPF.Windows.Dashboard;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;

namespace PresetUiValidation;

internal static class Program
{
    private static readonly List<string> OutputLines = [];
    private static int CurrentExitCode;

    [STAThread]
    private static int Main(string[] args)
    {
        return MainAsync(args).GetAwaiter().GetResult();
    }

    private static async Task<int> MainAsync(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Log.Instance.IsTraceEnabled = true;

        var resultFilePath = GetStringArg(args, "--result-file");

        if (!IsAdministrator())
            return ExitWithResult(5, "Preset UI validation requires administrator privileges.", resultFilePath);

        try
        {
            var exitCodeSource = new TaskCompletionSource<int>();
            var app = new UniversalDeviceToolkit.WPF.App
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
            InitializeApplicationResources(app);
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(app.Dispatcher));

            _ = app.Dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    WriteLine("Stage: Startup");
                    IoCContainer.Initialize(
                        new LenovoLegionToolkit.Lib.IoCModule(),
                        new UniversalDeviceToolkit.Lib.Automation.IoCModule(),
                        new UniversalDeviceToolkit.Lib.Macro.IoCModule(),
                        new UniversalDeviceToolkit.WPF.IoCModule());

                    WriteLine("Stage: IoCInitialized");
                    await LocalizationHelper.SetLanguageAsync(false);
                    WriteLine("Stage: LanguageApplied");

                    var exitCode = await await app.Dispatcher.InvokeAsync(
                        RunValidationAsync,
                        DispatcherPriority.Normal);
                    exitCodeSource.TrySetResult(SetExitCode(exitCode));
                }
                catch (Exception ex)
                {
                    exitCodeSource.TrySetResult(ExitWithResult(1, ex.ToString(), resultFilePath));
                }
                finally
                {
                    app.Dispatcher.InvokeShutdown();
                }
            }, DispatcherPriority.Normal);

            Dispatcher.Run();
            return exitCodeSource.Task.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return ExitWithResult(1, ex.ToString(), resultFilePath);
        }
        finally
        {
            FlushResultFile(resultFilePath);
        }
    }

    private static async Task<int> RunValidationAsync()
    {
        WriteLine("Stage: ValidationStart");
        var controller = IoCContainer.Resolve<GodModeController>();
        var originalState = await controller.GetStateAsync().ConfigureAwait(true);
        WriteLine("Stage: StateLoaded");

        var window = new GodModeSettingsWindow();
        var restoreVerificationPassed = false;

        try
        {
            window.Show();
            WriteLine("Stage: WindowShown");
            await WaitUntilAsync(() => !GetLoader(window).IsLoading && GetButtonsPanel(window).Visibility == Visibility.Visible);
            WriteLine("Stage: WindowReady");

            var comboBox = GetPresetsComboBox(window);
            var addButton = GetAddButton(window);
            var deleteButton = GetDeleteButton(window);
            var editButton = GetEditButton(window);

            var originalNames = GetPresetNames(comboBox);
            var originalCount = originalNames.Count;
            var originalSelected = GetSelectedPresetName(comboBox);

            const string createRequestedName = "Temporary UI Validation Preset";
            const string renameRequestedName = "Temporary UI Validation Preset Renamed";

            _ = window.Dispatcher.BeginInvoke(() => addButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            WriteLine("Stage: AddClicked");
            var createDialog = await WaitForWindowAsync<InputDialogWindow>();
            WriteLine("Stage: AddDialogShown");
            SubmitInputDialog(createDialog, createRequestedName);
            await WaitUntilAsync(() => GetPresetNames(comboBox).Count == originalCount + 1);
            WriteLine("Stage: AddVerified");

            var namesAfterCreate = GetPresetNames(comboBox);
            var createdName = namesAfterCreate.Except(originalNames, StringComparer.OrdinalIgnoreCase).Single();
            var selectedAfterCreate = GetSelectedPresetName(comboBox);
            var createCountPassed = namesAfterCreate.Count == originalCount + 1;
            var createActivePassed = string.Equals(selectedAfterCreate, createdName, StringComparison.Ordinal);
            var createNamePassed = createdName.StartsWith(createRequestedName, StringComparison.Ordinal);
            var persistedAfterCreate = await controller.GetStateAsync().ConfigureAwait(true);
            var persistedCreateActiveName = persistedAfterCreate.Presets[persistedAfterCreate.ActivePresetId].Name;
            var persistedCreateVerificationPassed = persistedAfterCreate.Presets.Count == originalCount + 1
                                                     && persistedAfterCreate.Presets.Values.Any(p => string.Equals(p.Name, createdName, StringComparison.Ordinal))
                                                     && string.Equals(persistedCreateActiveName, createdName, StringComparison.Ordinal);

            _ = window.Dispatcher.BeginInvoke(() => editButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            WriteLine("Stage: RenameClicked");
            var renameDialog = await WaitForWindowAsync<InputDialogWindow>();
            WriteLine("Stage: RenameDialogShown");
            SubmitInputDialog(renameDialog, renameRequestedName);
            await WaitUntilAsync(() => GetPresetNames(comboBox).Any(n => n.StartsWith(renameRequestedName, StringComparison.Ordinal)));
            WriteLine("Stage: RenameVerified");

            var namesAfterRename = GetPresetNames(comboBox);
            var renamedName = namesAfterRename.Single(n => n.StartsWith(renameRequestedName, StringComparison.Ordinal));
            var renameCountPassed = namesAfterRename.Count == originalCount + 1;
            var renameActivePassed = string.Equals(GetSelectedPresetName(comboBox), renamedName, StringComparison.Ordinal);
            var renameNamePassed = !namesAfterRename.Contains(createdName, StringComparer.OrdinalIgnoreCase)
                                   && namesAfterRename.Contains(renamedName, StringComparer.Ordinal);
            var persistedAfterRename = await controller.GetStateAsync().ConfigureAwait(true);
            var persistedRenameActiveName = persistedAfterRename.Presets[persistedAfterRename.ActivePresetId].Name;
            var persistedRenameVerificationPassed = persistedAfterRename.Presets.Count == originalCount + 1
                                                     && !persistedAfterRename.Presets.Values.Any(p => string.Equals(p.Name, createdName, StringComparison.OrdinalIgnoreCase))
                                                     && persistedAfterRename.Presets.Values.Any(p => string.Equals(p.Name, renamedName, StringComparison.Ordinal))
                                                     && string.Equals(persistedRenameActiveName, renamedName, StringComparison.Ordinal);

            _ = window.Dispatcher.BeginInvoke(() => deleteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            WriteLine("Stage: DeleteClicked");
            await WaitUntilAsync(() => GetPresetNames(comboBox).Count == originalCount);
            WriteLine("Stage: DeleteVerified");

            var namesAfterDelete = GetPresetNames(comboBox);
            var deleteMissingPassed = !namesAfterDelete.Contains(renamedName, StringComparer.OrdinalIgnoreCase);
            var deleteCountPassed = namesAfterDelete.Count == originalCount;
            var selectedAfterDelete = GetSelectedPresetName(comboBox);
            var deleteActivePassed = !string.Equals(selectedAfterDelete, renamedName, StringComparison.OrdinalIgnoreCase)
                                     && namesAfterDelete.Contains(selectedAfterDelete, StringComparer.Ordinal);

            var persistedState = await controller.GetStateAsync().ConfigureAwait(true);
            var persistedNames = persistedState.Presets.Values.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var expectedNames = originalNames.OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var persistedDeleteVerificationPassed = persistedNames.SequenceEqual(expectedNames, StringComparer.Ordinal);

            PrintValue("OriginalPresetCount", originalCount);
            PrintValue("CreatePresetExists", createNamePassed);
            PrintValue("CreateCountVerificationPassed", createCountPassed);
            PrintValue("CreateActiveVerificationPassed", createActivePassed);
            PrintValue("CreateNameVerificationPassed", createNamePassed);
            PrintValue("CreatePersistedVerificationPassed", persistedCreateVerificationPassed);
            PrintValue("RenameCountVerificationPassed", renameCountPassed);
            PrintValue("RenameActiveVerificationPassed", renameActivePassed);
            PrintValue("RenameNameVerificationPassed", renameNamePassed);
            PrintValue("RenamePersistedVerificationPassed", persistedRenameVerificationPassed);
            PrintValue("DeleteMissingVerificationPassed", deleteMissingPassed);
            PrintValue("DeleteCountVerificationPassed", deleteCountPassed);
            PrintValue("DeleteActiveVerificationPassed", deleteActivePassed);
            PrintValue("PersistedDeleteVerificationPassed", persistedDeleteVerificationPassed);

            var passed = createCountPassed
                         && createActivePassed
                         && createNamePassed
                         && persistedCreateVerificationPassed
                         && renameCountPassed
                         && renameActivePassed
                         && renameNamePassed
                         && persistedRenameVerificationPassed
                         && deleteMissingPassed
                         && deleteCountPassed
                         && deleteActivePassed
                         && persistedDeleteVerificationPassed;

            PrintValue("PresetUiCrudVerificationPassed", passed);
            return passed ? 0 : 2;
        }
        finally
        {
            try
            {
                await controller.SetStateAsync(originalState).ConfigureAwait(true);
                var restored = await controller.GetStateAsync().ConfigureAwait(true);
                var restoredNames = restored.Presets.Values.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                var expectedNames = originalState.Presets.Values.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                restoreVerificationPassed = restored.ActivePresetId == originalState.ActivePresetId
                                            && restoredNames.SequenceEqual(expectedNames, StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                WriteLine($"Restore failed: {ex}");
            }

            PrintValue("RestorePresetStateVerificationPassed", restoreVerificationPassed);

            if (window.IsVisible)
                window.Close();
        }
    }

    private static void InitializeApplicationResources(UniversalDeviceToolkit.WPF.App app)
    {
        var resources = new ResourceDictionary();
        app.Resources = resources;

        resources["SnackbarShadowColor"] = System.Windows.Media.Color.FromArgb(0x40, 0x00, 0x00, 0x00);
        resources["AppSurfaceBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20));
        resources["AppSurfaceCardBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x30, 0x30));
        resources["AppNavigationBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A));

        resources.MergedDictionaries.Add(new ControlsDictionary());
        resources.MergedDictionaries.Add(new ThemesDictionary { Theme = ApplicationTheme.Dark });
        resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/ColorPicker;component/Styles/DefaultColorPickerStyle.xaml", UriKind.Absolute) });

        foreach (var resourcePath in new[]
                 {
                     "DesignTokens.xaml",
                     "ElevationTokens.xaml",
                     "AnimationTokens.xaml",
                     "Animations.xaml",
                     "ButtonStyles.xaml",
                     "Typography.xaml",
                     "Loading.xaml",
                     "Badge.xaml",
                     "CardAction.xaml",
                     "CardControl.xaml",
                     "CardExpander.xaml",
                     "DynamicScrollBar.xaml",
                     "InfoBar.xaml",
                     "NavigationStore.xaml"
                 })
        {
            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Universal Device Toolkit;component/Styles/{resourcePath}", UriKind.Absolute)
            });
        }
    }

    private static async Task<TWindow> WaitForWindowAsync<TWindow>() where TWindow : Window
    {
        return await WaitUntilAsync(
            () => Application.Current.Windows.OfType<TWindow>().FirstOrDefault(w => w.IsVisible),
            value => value is not null).ConfigureAwait(true);
    }

    private static void SubmitInputDialog(InputDialogWindow dialog, string value)
    {
        var inputTextBox = GetField<TextBox>(dialog, "_textBox");
        var confirmButton = GetField<Button>(dialog, "_confirmButton");
        inputTextBox.Text = value;
        confirmButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private static ComboBox GetPresetsComboBox(GodModeSettingsWindow window) => GetField<ComboBox>(window, "_presetsComboBox");
    private static Button GetAddButton(GodModeSettingsWindow window) => GetField<Button>(window, "_addPresetsButton");
    private static Button GetDeleteButton(GodModeSettingsWindow window) => GetField<Button>(window, "_deletePresetsButton");
    private static FrameworkElement GetButtonsPanel(GodModeSettingsWindow window) => GetField<FrameworkElement>(window, "_buttonsStackPanel");
    private static LoadableControl GetLoader(GodModeSettingsWindow window) => GetField<LoadableControl>(window, "_loader");

    private static Button GetEditButton(GodModeSettingsWindow window)
    {
        var topGrid = (Grid)GetPresetsComboBox(window).Parent;
        var editButton = topGrid.Children
            .OfType<Button>()
            .FirstOrDefault(button => Grid.GetColumn(button) == 1);

        return editButton ?? throw new InvalidOperationException("Edit preset button not found.");
    }

    private static List<string> GetPresetNames(ComboBox comboBox) =>
        comboBox.Items.Cast<object>().Select(item => item.ToString() ?? string.Empty).ToList();

    private static string GetSelectedPresetName(ComboBox comboBox) => comboBox.SelectedItem?.ToString() ?? string.Empty;

    private static T GetField<T>(object instance, string fieldName) where T : class
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var field = instance.GetType().GetField(fieldName, flags)
                    ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {instance.GetType().FullName}.");
        return field.GetValue(instance) as T
               ?? throw new InvalidOperationException($"Field '{fieldName}' was not a {typeof(T).FullName}.");
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 15000, int pollMs = 50)
    {
        var start = Environment.TickCount64;
        while (!predicate())
        {
            if (Environment.TickCount64 - start > timeoutMs)
                throw new TimeoutException("Timed out waiting for condition.");

            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(pollMs).ConfigureAwait(true);
        }
    }

    private static async Task<T> WaitUntilAsync<T>(Func<T?> provider, Func<T?, bool> predicate, int timeoutMs = 15000, int pollMs = 50) where T : class
    {
        var start = Environment.TickCount64;
        while (true)
        {
            var value = provider();
            if (predicate(value))
                return value!;

            if (Environment.TickCount64 - start > timeoutMs)
                throw new TimeoutException("Timed out waiting for value.");

            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(pollMs).ConfigureAwait(true);
        }
    }

    private static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void PrintValue(string key, object? value) => WriteLine($"{key}: {value}");

    private static void WriteLine(string message)
    {
        OutputLines.Add(message);
        Console.WriteLine(message);
    }

    private static int ExitWithResult(int exitCode, string message, string? resultFilePath)
    {
        CurrentExitCode = exitCode;
        WriteLine(message);
        FlushResultFile(resultFilePath);
        return exitCode;
    }

    private static int SetExitCode(int exitCode)
    {
        CurrentExitCode = exitCode;
        return exitCode;
    }

    private static void FlushResultFile(string? resultFilePath)
    {
        if (string.IsNullOrWhiteSpace(resultFilePath))
            return;

        try
        {
            var lines = new List<string> { $"ExitCode: {CurrentExitCode}" };
            if (OutputLines.Count > 0)
            {
                lines.Add("Output:");
                lines.AddRange(OutputLines);
            }

            File.WriteAllLines(resultFilePath, lines);
        }
        catch
        {
            // Ignore result-file write failures; console output still exists when available.
        }
    }

    private static string? GetStringArg(string[] args, string key)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                return arg[(key.Length + 1)..].Trim('"');
        }

        return null;
    }
}
