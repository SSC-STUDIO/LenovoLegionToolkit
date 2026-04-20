using System.Xml.Linq;

namespace PluginTooling.Core;

public sealed class PluginValidationService
{
    private readonly PluginRepository _repository = new();
    private readonly ProcessRunner _processRunner = new();
    private readonly Action<string> _log;
    private readonly Action<StepReportItem>? _stepSink;

    public PluginValidationService(Action<string> log, Action<StepReportItem>? stepSink = null)
    {
        _log = log;
        _stepSink = stepSink;
    }

    public async Task<ValidationReport> RunAsync(ValidationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var report = new ValidationReport();
        AddStep(report.Steps, string.Empty, "INFO", $"Repository root: {repository.RootPath}");
        AddStep(report.Steps, string.Empty, "INFO", $"Profile: {request.Profile}");
        AddStep(report.Steps, string.Empty, "INFO", $"Configuration: {request.Configuration}");

        foreach (var pluginId in _repository.ResolveTargetPluginIds(repository, request.PluginIds))
        {
            var state = new ValidationState(pluginId, report.Steps, AddStep);
            var plugin = repository.Plugins[pluginId];
            await ValidatePluginAsync(repository, plugin, request, state, cancellationToken);
            report.Plugins.Add(state.ToReportItem());
        }

        report.Totals = new ValidationTotals
        {
            PluginCount = report.Plugins.Count,
            Failures = report.Plugins.Sum(item => item.Failures),
            Warnings = report.Plugins.Sum(item => item.Warnings),
        };

        AddStep(
            report.Steps,
            string.Empty,
            report.Totals.Failures == 0 ? "PASS" : "FAIL",
            $"Validation finished. Plugins={report.Totals.PluginCount}, Failures={report.Totals.Failures}, Warnings={report.Totals.Warnings}");

        return report;
    }

    private async Task ValidatePluginAsync(
        RepositoryContext repository,
        PluginContext plugin,
        ValidationRequest request,
        ValidationState state,
        CancellationToken cancellationToken)
    {
        state.Info("Starting validation.");
        ValidateManifest(plugin, state);
        ValidateChangelog(plugin, state);
        ValidateProject(plugin, state);
        ValidateTestProject(plugin, state);

        if (request.Profile is PluginValidationProfile.OfficialCandidate or PluginValidationProfile.OfficialRelease)
            ValidateStoreEntry(plugin, state);

        if (!request.SkipBuild && !string.IsNullOrWhiteSpace(plugin.ProjectPath))
        {
            var exitCode = await _processRunner.RunDotnetAsync(
                ["build", plugin.ProjectPath!, "-c", request.Configuration, "--nologo"],
                repository.RootPath,
                message => state.Info(message),
                cancellationToken);

            if (exitCode != 0)
                state.Fail("dotnet build failed for the plugin project.");
            else
                state.Pass("dotnet build succeeded.");
        }
        else
        {
            state.Info("Build step skipped.");
        }

        ValidateBuildOutput(plugin, state);

        if (!request.SkipTests && !string.IsNullOrWhiteSpace(plugin.TestProjectPath))
        {
            var exitCode = await _processRunner.RunDotnetAsync(
                ["test", plugin.TestProjectPath!, "-c", request.Configuration, "--no-restore", "--nologo"],
                repository.RootPath,
                message => state.Info(message),
                cancellationToken);

            if (exitCode != 0)
                state.Fail("dotnet test failed for the plugin test project.");
            else
                state.Pass("dotnet test succeeded.");
        }
        else
        {
            state.Info("Test step skipped.");
        }

        if (request.Profile == PluginValidationProfile.OfficialRelease)
            ValidateStoreJsonAlignment(repository, plugin, state);

        if (state.Failures == 0)
            state.Pass(state.Warnings == 0 ? "Validation passed." : "Validation passed with warnings.");
        else
            state.Info("Validation completed with failures.");
    }

    private static void ValidateManifest(PluginContext plugin, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(plugin.Manifest.Id))
            state.Fail("plugin.json is missing id.");
        else
            state.Pass("plugin.json id found.");

        if (string.IsNullOrWhiteSpace(plugin.Manifest.Name))
            state.Fail("plugin.json is missing name.");
        else
            state.Pass("plugin.json name found.");

        if (string.IsNullOrWhiteSpace(plugin.Manifest.Version))
            state.Fail("plugin.json is missing version.");
        else
            state.Pass("plugin.json version found.");

        if (string.IsNullOrWhiteSpace(plugin.Manifest.MinLltVersion))
            state.Fail("plugin.json is missing minLLTVersion.");
        else
            state.Pass("plugin.json minLLTVersion found.");
    }

    private static void ValidateChangelog(PluginContext plugin, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(plugin.ChangelogPath) || !File.Exists(plugin.ChangelogPath))
            state.Fail("Plugin CHANGELOG.md is missing.");
        else
            state.Pass("Plugin CHANGELOG.md found.");
    }

    private static void ValidateProject(PluginContext plugin, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(plugin.ProjectPath) || !File.Exists(plugin.ProjectPath))
        {
            state.Fail("Plugin project file is missing.");
            return;
        }

        var expectedProjectName = $"{plugin.ExpectedAssemblyName}.csproj";
        if (!string.Equals(Path.GetFileName(plugin.ProjectPath), expectedProjectName, StringComparison.OrdinalIgnoreCase))
            state.Fail($"Project file name must be '{expectedProjectName}'.");
        else
            state.Pass("Project file naming matches convention.");

        var document = XDocument.Load(plugin.ProjectPath, LoadOptions.None);
        ValidateEqual(ReadProperty(document, "Version"), plugin.Manifest.Version, "Project Version does not match plugin.json version.", state);
        ValidateEqual(ReadProperty(document, "AssemblyName"), plugin.ExpectedAssemblyName, $"AssemblyName must be '{plugin.ExpectedAssemblyName}'.", state);

        var outputPath = ReadProperty(document, "OutputPath");
        var expectedOutputPath = $@"..\..\Build\plugins\{plugin.ExpectedAssemblyName}\";
        if (!string.Equals(outputPath, expectedOutputPath, StringComparison.OrdinalIgnoreCase))
            state.Fail($"OutputPath should be '{expectedOutputPath}'.");
        else
            state.Pass("OutputPath matches plugin build convention.");
    }

    private static void ValidateTestProject(PluginContext plugin, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(plugin.TestProjectPath) || !File.Exists(plugin.TestProjectPath))
        {
            state.Fail("Plugin test project is missing.");
            return;
        }

        state.Pass("Plugin test project found.");
    }

    private static void ValidateStoreEntry(PluginContext plugin, ValidationState state)
    {
        if (plugin.StoreEntry is null || string.IsNullOrWhiteSpace(plugin.StoreEntryPath) || !File.Exists(plugin.StoreEntryPath))
        {
            state.Fail("Official plugin is missing store-entry.json.");
            return;
        }

        if (string.IsNullOrWhiteSpace(plugin.StoreEntry.Description))
            state.Fail("store-entry.json is missing description.");
        else
            state.Pass("store-entry.json description found.");

        if (string.IsNullOrWhiteSpace(plugin.StoreEntry.Icon))
            state.Fail("store-entry.json is missing icon.");
        else
            state.Pass("store-entry.json icon found.");

        if (string.IsNullOrWhiteSpace(plugin.StoreEntry.IconBackground))
            state.Fail("store-entry.json is missing iconBackground.");
        else
            state.Pass("store-entry.json iconBackground found.");

        if (plugin.StoreEntry.Tags.Count == 0)
            state.Fail("store-entry.json is missing tags.");
        else
            state.Pass("store-entry.json tags found.");

        if (plugin.StoreEntry.SupportedLanguages.Count == 0)
            state.Fail("store-entry.json is missing supportedLanguages.");
        else
            state.Pass("store-entry.json supportedLanguages found.");
    }

    private static void ValidateBuildOutput(PluginContext plugin, ValidationState state)
    {
        if (!Directory.Exists(plugin.OutputDirectory))
        {
            state.Fail($"Expected build output directory not found: {plugin.OutputDirectory}");
            return;
        }

        if (!File.Exists(plugin.ExpectedAssemblyPath))
            state.Fail($"Expected plugin assembly not found: {plugin.ExpectedAssemblyPath}");
        else
            state.Pass("Plugin assembly exists in build output.");

        if (!File.Exists(Path.Combine(plugin.OutputDirectory, "plugin.json")))
            state.Fail("plugin.json is missing from build output.");
        else
            state.Pass("plugin.json exists in build output.");
    }

    private static void ValidateStoreJsonAlignment(RepositoryContext repository, PluginContext plugin, ValidationState state)
    {
        if (repository.StoreDocument is null)
        {
            state.Fail("store.json is missing for official-release validation.");
            return;
        }

        var existingEntry = repository.StoreDocument.Plugins.FirstOrDefault(entry =>
            string.Equals(entry.Id, plugin.Manifest.Id, StringComparison.OrdinalIgnoreCase));

        if (existingEntry is null)
        {
            state.Fail("store.json is missing the plugin entry.");
            return;
        }

        if (!string.Equals(existingEntry.Version, plugin.Manifest.Version, StringComparison.OrdinalIgnoreCase))
            state.Fail("store.json version does not match plugin.json version.");
        else
            state.Pass("store.json version matches plugin.json.");

        if (!string.Equals(existingEntry.Name, plugin.Manifest.Name, StringComparison.OrdinalIgnoreCase))
            state.Fail("store.json name does not match plugin.json name.");
        else
            state.Pass("store.json name matches plugin.json.");
    }

    private static string ReadProperty(XDocument document, string propertyName)
    {
        return document.Root?
            .Elements()
            .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.Ordinal))
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
            ?.Value.Trim() ?? string.Empty;
    }

    private static void ValidateEqual(string actual, string expected, string failureMessage, ValidationState state)
    {
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            state.Fail(failureMessage);
        else
            state.Pass(failureMessage.Replace(" does not ", " ").Replace("must be", "matches").TrimEnd('.'));
    }

    private void AddStep(List<StepReportItem> steps, string pluginId, string status, string message)
    {
        var step = new StepReportItem
        {
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            PluginId = pluginId,
            Status = status,
            Message = message,
        };

        steps.Add(step);
        _log($"{(string.IsNullOrWhiteSpace(pluginId) ? "[global]" : $"[{pluginId}]")} [{status}] {message}");
        _stepSink?.Invoke(step);
    }

    private sealed class ValidationState
    {
        private readonly string _pluginId;
        private readonly List<StepReportItem> _steps;
        private readonly Action<List<StepReportItem>, string, string, string> _addStep;

        public ValidationState(string pluginId, List<StepReportItem> steps, Action<List<StepReportItem>, string, string, string> addStep)
        {
            _pluginId = pluginId;
            _steps = steps;
            _addStep = addStep;
        }

        public int Failures { get; private set; }
        public int Warnings { get; private set; }

        public void Info(string message) => _addStep(_steps, _pluginId, "INFO", message);
        public void Pass(string message) => _addStep(_steps, _pluginId, "PASS", message);

        public void Warn(string message)
        {
            Warnings++;
            _addStep(_steps, _pluginId, "WARN", message);
        }

        public void Fail(string message)
        {
            Failures++;
            _addStep(_steps, _pluginId, "FAIL", message);
        }

        public PluginReportItem ToReportItem()
        {
            return new PluginReportItem
            {
                PluginId = _pluginId,
                Failures = Failures,
                Warnings = Warnings,
                Status = Failures == 0 ? "PASS" : "FAIL",
            };
        }
    }
}
