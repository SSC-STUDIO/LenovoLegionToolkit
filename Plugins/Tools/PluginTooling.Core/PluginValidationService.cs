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
        ValidateUnifiedManifest(plugin, state);
        ValidateChangelog(plugin, state);
        ValidateProject(plugin, state);
        ValidateTestProject(plugin, state);

        if (request.Profile is PluginValidationProfile.OfficialCandidate or PluginValidationProfile.OfficialRelease)
        {
            ValidateStoreMetadata(plugin, state);
        }

        if (!request.SkipBuild && !string.IsNullOrWhiteSpace(plugin.ProjectPath))
        {
            var exitCode = await _processRunner.RunDotnetAsync(
                ["build", plugin.ProjectPath!, "-c", request.Configuration, "--nologo"],
                repository.RootPath,
                message => state.Info(message),
                cancellationToken);

            if (exitCode != 0)
            {
                state.Fail("dotnet build failed for the plugin project.");
            }
            else
            {
                state.Pass("dotnet build succeeded.");
            }
        }
        else
        {
            state.Info("Build step skipped.");
        }

        ValidateBuildOutput(plugin, state);
        ValidatePackageContents(plugin, state);

        if (!request.SkipTests && !string.IsNullOrWhiteSpace(plugin.TestProjectPath))
        {
            var exitCode = await _processRunner.RunDotnetAsync(
                ["test", plugin.TestProjectPath!, "-c", request.Configuration, "--nologo"],
                repository.RootPath,
                message => state.Info(message),
                cancellationToken);

            if (exitCode != 0)
            {
                state.Fail("dotnet test failed for the plugin test project.");
            }
            else
            {
                state.Pass("dotnet test succeeded.");
            }
        }
        else
        {
            state.Info("Test step skipped.");
        }

        if (request.Profile == PluginValidationProfile.OfficialRelease)
        {
            ValidateStoreJsonAlignment(repository, plugin, state);
        }

        if (state.Failures == 0)
        {
            state.Pass(state.Warnings == 0 ? "Validation passed." : "Validation passed with warnings.");
        }
        else
        {
            state.Info("Validation completed with failures.");
        }
    }

    private static void ValidateManifest(PluginContext plugin, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(plugin.Manifest.Id))
        {
            state.Fail("Runtime manifest is missing id.");
        }
        else
        {
            state.Pass("Runtime manifest id found.");
        }

        if (string.IsNullOrWhiteSpace(plugin.Manifest.Name))
        {
            state.Fail("Runtime manifest is missing name.");
        }
        else
        {
            state.Pass("Runtime manifest name found.");
        }

        if (string.IsNullOrWhiteSpace(plugin.Manifest.Version))
        {
            state.Fail("Runtime manifest is missing version.");
        }
        else
        {
            state.Pass("Runtime manifest version found.");
        }

        if (string.IsNullOrWhiteSpace(plugin.Manifest.MinLltVersion))
        {
            state.Fail("Runtime manifest is missing minLLTVersion.");
        }
        else
        {
            state.Pass("Runtime manifest minLLTVersion found.");
        }
    }

    private static void ValidateUnifiedManifest(PluginContext plugin, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(plugin.UnifiedManifestPath) || !File.Exists(plugin.UnifiedManifestPath))
        {
            state.Fail("plugin.manifest.json is missing.");
            return;
        }

        state.Pass("plugin.manifest.json found.");

        var manifest = plugin.UnifiedManifest;
        ValidateEqual(manifest.Id, plugin.Manifest.Id, "Unified manifest id does not match runtime manifest id.", state);
        ValidateEqual(manifest.Version, plugin.Manifest.Version, "Unified manifest version does not match runtime manifest version.", state);
        ValidateEqual(manifest.MinHostVersion, plugin.Manifest.MinLltVersion, "Unified manifest minHostVersion does not match runtime minLLTVersion.", state);
        ValidateLegacyManifestCompatibility(plugin, manifest, state);

        if (string.IsNullOrWhiteSpace(manifest.Package.AssetName))
        {
            state.Fail("plugin.manifest.json package.assetName is missing.");
        }
        else if (!string.Equals(manifest.Package.AssetName, $"{manifest.Id}-v{manifest.Version}.zip", StringComparison.OrdinalIgnoreCase))
        {
            state.Fail("plugin.manifest.json package.assetName must be '<plugin-id>-v<version>.zip'.");
        }
        else
        {
            state.Pass("plugin.manifest.json package asset name matches convention.");
        }

        ValidateContributionType(plugin, manifest.Contributes.FeaturePage, "featurePage", state);
        ValidateContributionType(plugin, manifest.Contributes.SettingsPage, "settingsPage", state);
        ValidateContributionType(plugin, manifest.Contributes.Runtime, "runtime", state);

        foreach (var action in manifest.Contributes.OptimizationActions ?? [])
        {
            if (string.IsNullOrWhiteSpace(action.Id) || string.IsNullOrWhiteSpace(action.Title))
            {
                state.Fail("optimizationActions entries must include id and title.");
            }
            else
            {
                state.Pass($"optimization action '{action.Id}' found.");
            }
        }
    }

    private static void ValidateContributionType(PluginContext plugin, PluginPageContribution? contribution, string contributionName, ValidationState state)
    {
        if (contribution is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(contribution.Class))
        {
            state.Fail($"{contributionName} contribution is missing class.");
            return;
        }

        ValidateContributionClassExists(plugin, contribution.Class, contributionName, state);
    }

    private static void ValidateContributionType(PluginContext plugin, PluginRuntimeContribution? contribution, string contributionName, ValidationState state)
    {
        if (contribution is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(contribution.Class))
        {
            state.Fail($"{contributionName} contribution is missing class.");
            return;
        }

        ValidateContributionClassExists(plugin, contribution.Class, contributionName, state);
    }

    private static void ValidateLegacyManifestCompatibility(PluginContext plugin, UnifiedPluginManifest manifest, ValidationState state)
    {
        var legacyManifestPath = Path.Combine(plugin.DirectoryPath, "plugin.json");
        if (!File.Exists(legacyManifestPath))
        {
            state.Fail("plugin.json compatibility manifest is missing.");
            return;
        }

        var legacyManifest = PluginRepository.ReadJsonFile<PluginManifest>(legacyManifestPath);
        ValidateEqual(legacyManifest.Id, manifest.Id, "plugin.json id does not match plugin.manifest.json id.", state);
        ValidateEqual(legacyManifest.Name, manifest.Name, "plugin.json name does not match plugin.manifest.json name.", state);
        ValidateEqual(legacyManifest.Version, manifest.Version, "plugin.json version does not match plugin.manifest.json version.", state);
        ValidateEqual(legacyManifest.MinLltVersion, manifest.MinHostVersion, "plugin.json minLLTVersion does not match plugin.manifest.json minHostVersion.", state);
        ValidateEqual(legacyManifest.Author, manifest.Author, "plugin.json author does not match plugin.manifest.json author.", state);

        if (legacyManifest.IsSystemPlugin != manifest.IsSystemPlugin)
        {
            state.Fail("plugin.json isSystemPlugin does not match plugin.manifest.json isSystemPlugin.");
        }
        else
        {
            state.Pass("plugin.json isSystemPlugin matches plugin.manifest.json.");
        }
    }

    private static void ValidateContributionClassExists(PluginContext plugin, string fullTypeName, string contributionName, ValidationState state)
    {
        var typeName = fullTypeName.Split('.').Last();
        var found = Directory.EnumerateFiles(plugin.DirectoryPath, "*.cs", SearchOption.AllDirectories)
            .Any(path => File.ReadAllText(path).Contains($"class {typeName}", StringComparison.Ordinal));

        if (!found)
        {
            state.Fail($"{contributionName} contribution class '{fullTypeName}' was not found in plugin source.");
        }
        else
        {
            state.Pass($"{contributionName} contribution class found.");
        }
    }

    private static void ValidateChangelog(PluginContext plugin, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(plugin.ChangelogPath) || !File.Exists(plugin.ChangelogPath))
        {
            state.Fail("Plugin CHANGELOG.md is missing.");
        }
        else
        {
            state.Pass("Plugin CHANGELOG.md found.");
        }
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
        {
            state.Fail($"Project file name must be '{expectedProjectName}'.");
        }
        else
        {
            state.Pass("Project file naming matches convention.");
        }

        var document = XDocument.Load(plugin.ProjectPath, LoadOptions.None);
        ValidateEqual(ReadProperty(document, "Version"), plugin.Manifest.Version, "Project Version does not match plugin.manifest.json version.", state);
        ValidateEqual(ReadProperty(document, "FileVersion"), plugin.Manifest.Version, "Project FileVersion does not match plugin.manifest.json version.", state);
        ValidateEqual(ReadProperty(document, "AssemblyVersion"), plugin.Manifest.Version, "Project AssemblyVersion does not match plugin.manifest.json version.", state);
        ValidateEqual(ReadProperty(document, "AssemblyName"), plugin.ExpectedAssemblyName, $"AssemblyName must be '{plugin.ExpectedAssemblyName}'.", state);

        var attributeVersion = PluginVersionSynchronizer.ReadPluginAttributeVersion(plugin.DirectoryPath);
        ValidateEqual(attributeVersion ?? string.Empty, plugin.Manifest.Version, "[Plugin] attribute version does not match plugin.manifest.json version. Run sync-version.", state);

        var outputPath = ReadProperty(document, "OutputPath");
        var relativeOutputPath = Path.GetRelativePath(plugin.DirectoryPath, plugin.OutputDirectory)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var expectedOutputPath = $"{relativeOutputPath}{Path.DirectorySeparatorChar}";
        if (!string.Equals(outputPath, expectedOutputPath, StringComparison.OrdinalIgnoreCase))
        {
            state.Fail($"OutputPath should be '{expectedOutputPath}'.");
        }
        else
        {
            state.Pass("OutputPath matches plugin build convention.");
        }
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

    private static void ValidateStoreMetadata(PluginContext plugin, ValidationState state)
    {
        var store = plugin.UnifiedManifest.Store;
        if (string.IsNullOrWhiteSpace(store.Description))
        {
            state.Fail("plugin.manifest.json store.description is missing.");
        }
        else
        {
            state.Pass("plugin.manifest.json store description found.");
        }

        if (string.IsNullOrWhiteSpace(store.Icon))
        {
            state.Fail("plugin.manifest.json store.icon is missing.");
        }
        else
        {
            state.Pass("plugin.manifest.json store icon found.");
        }

        if (string.IsNullOrWhiteSpace(store.IconBackground))
        {
            state.Fail("plugin.manifest.json store.iconBackground is missing.");
        }
        else
        {
            state.Pass("plugin.manifest.json store iconBackground found.");
        }

        if ((store.Tags ?? []).Count == 0)
        {
            state.Fail("plugin.manifest.json store.tags is missing.");
        }
        else
        {
            state.Pass("plugin.manifest.json store tags found.");
        }

        if ((store.SupportedLanguages ?? []).Count == 0)
        {
            state.Fail("plugin.manifest.json store.supportedLanguages is missing.");
        }
        else
        {
            state.Pass("plugin.manifest.json store supportedLanguages found.");
        }

        if (plugin.StoreEntry is null || string.IsNullOrWhiteSpace(plugin.StoreEntryPath) || !File.Exists(plugin.StoreEntryPath))
        {
            state.Warn("store-entry.json compatibility file is missing; run migrate or promote if legacy release scripts still need it.");
            return;
        }

        var storeEntry = PluginRepository.ToStoreEntry(plugin.UnifiedManifest);
        if (!string.Equals(plugin.StoreEntry.Description, storeEntry.Description, StringComparison.Ordinal) ||
            !string.Equals(plugin.StoreEntry.Icon, storeEntry.Icon, StringComparison.Ordinal) ||
            !string.Equals(plugin.StoreEntry.IconBackground, storeEntry.IconBackground, StringComparison.Ordinal) ||
            !(plugin.StoreEntry.Tags ?? []).SequenceEqual(storeEntry.Tags ?? [], StringComparer.Ordinal) ||
            !(plugin.StoreEntry.SupportedLanguages ?? []).SequenceEqual(storeEntry.SupportedLanguages ?? [], StringComparer.Ordinal) ||
            !(plugin.StoreEntry.Dependencies ?? []).SequenceEqual(storeEntry.Dependencies ?? [], StringComparer.Ordinal) ||
            !string.Equals(plugin.StoreEntry.RepositoryUrl, storeEntry.RepositoryUrl, StringComparison.Ordinal))
        {
            state.Warn("store-entry.json differs from plugin.manifest.json store metadata; run migrate or promote to resync compatibility metadata.");
        }
        else
        {
            state.Pass("store-entry.json compatibility metadata is synchronized.");
        }
    }

    private static void ValidateBuildOutput(PluginContext plugin, ValidationState state)
    {
        if (!Directory.Exists(plugin.OutputDirectory))
        {
            state.Fail($"Expected build output directory not found: {plugin.OutputDirectory}");
            return;
        }

        if (!File.Exists(plugin.ExpectedAssemblyPath))
        {
            state.Fail($"Expected plugin assembly not found: {plugin.ExpectedAssemblyPath}");
        }
        else
        {
            state.Pass("Plugin assembly exists in build output.");
        }

        if (!File.Exists(Path.Combine(plugin.OutputDirectory, "plugin.json")))
        {
            state.Fail("plugin.json is missing from build output.");
        }
        else
        {
            state.Pass("plugin.json exists in build output.");
        }

        if (!File.Exists(Path.Combine(plugin.OutputDirectory, "plugin.manifest.json")))
        {
            state.Fail("plugin.manifest.json is missing from build output.");
        }
        else
        {
            state.Pass("plugin.manifest.json exists in build output.");
        }
    }

    private static void ValidatePackageContents(PluginContext plugin, ValidationState state)
    {
        if (!Directory.Exists(plugin.OutputDirectory))
        {
            return;
        }

        foreach (var requiredFile in plugin.UnifiedManifest.Package.RequiredFiles ?? [])
        {
            var requiredPath = Path.Combine(plugin.OutputDirectory, requiredFile);
            if (!File.Exists(requiredPath))
            {
                state.Fail($"Package required file is missing from build output: {requiredFile}");
            }
            else
            {
                state.Pass($"Package required file found: {requiredFile}");
            }
        }

        foreach (var sidecar in Directory.EnumerateFiles(plugin.OutputDirectory, "Wpf.Ui*.dll", SearchOption.TopDirectoryOnly)
                     .Select(Path.GetFileName)
                     .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
                     .Select(fileName => fileName!))
        {
            state.Pass($"WPF UI sidecar available: {sidecar}");
        }
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
        {
            state.Fail("store.json version does not match plugin.manifest.json version.");
        }
        else
        {
            state.Pass("store.json version matches plugin.manifest.json.");
        }

        if (!string.Equals(existingEntry.Name, plugin.Manifest.Name, StringComparison.OrdinalIgnoreCase))
        {
            state.Fail("store.json name does not match plugin.manifest.json name.");
        }
        else
        {
            state.Pass("store.json name matches plugin.manifest.json.");
        }
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
        {
            state.Fail(failureMessage);
        }
        else
        {
            state.Pass(failureMessage.Replace(" does not ", " ").Replace("must be", "matches").TrimEnd('.'));
        }
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
