using PluginTooling.Core;

return await ProgramMainAsync(args);

static async Task<int> ProgramMainAsync(string[] args)
{
    if (args.Length == 0 || IsHelp(args[0]))
    {
        PrintHelp();
        return 0;
    }

    var repository = new PluginRepository();
    var root = ResolveRepositoryRoot(args);
    var command = args[0].ToLowerInvariant();

    try
    {
        return command switch
        {
            "doctor" => await RunDoctorAsync(root, args),
            "inspect" => await RunInspectAsync(root, args),
            "init" => await RunNewAsync(root, args),
            "new" => await RunNewAsync(root, args),
            "dev" => await RunDevAsync(root, args),
            "build" => await RunBuildAsync(root, args),
            "test" => await RunTestAsync(root, args),
            "preview" => await RunPreviewAsync(root, args),
            "validate" => await RunValidateAsync(root, args),
            "package" => await RunPackAsync(root, args),
            "pack" => await RunPackAsync(root, args),
            "migrate" => RunMigrate(root, args),
            "sync-version" => RunSyncVersion(root, args),
            "bump-version" => RunBumpVersion(root, args),
            "promote" => RunPromote(root, args),
            "generate-store" => RunGenerateStore(root, args),
            _ => Fail($"Unknown command '{args[0]}'."),
        };
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }

    string ResolveRepositoryRoot(string[] argv)
    {
        for (var i = 0; i < argv.Length; i++)
        {
            if (!string.Equals(argv[i], "--repository-root", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 >= argv.Length)
            {
                throw new InvalidOperationException("Missing value for --repository-root.");
            }

            return Path.GetFullPath(argv[i + 1]);
        }

        return PluginRepository.FindRepositoryRoot(Environment.CurrentDirectory);
    }

    async Task<int> RunDoctorAsync(string repositoryRoot, string[] argv)
    {
        var service = new DoctorService();
        var result = service.Run(repositoryRoot);
        foreach (var check in result.Checks)
        {
            Console.WriteLine($"[{check.Status}] {check.Message}");
        }

        var outputPath = OptionalValue(argv, "--json-report-path");
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await JsonReportFile.WriteAsync(Path.GetFullPath(outputPath), result);
        }

        return result.FailureCount == 0 ? 0 : 1;
    }

    async Task<int> RunInspectAsync(string repositoryRoot, string[] argv)
    {
        var service = new PluginInspectionService();
        var report = service.Inspect(repositoryRoot, ParsePluginSelection(argv));

        Console.WriteLine($"Repository: {report.RepositoryRoot}");
        Console.WriteLine($"Plugins: {report.PluginCount}");
        foreach (var plugin in report.Plugins)
        {
            var storeState = plugin.StoreJsonEntry is null
                ? plugin.HasStoreMetadata ? "store: metadata ready" : "store: metadata missing"
                : plugin.StoreJsonEntry.MatchesManifestVersion ? "store: aligned" : "store: version mismatch";
            var manifestState = plugin.HasUnifiedManifest ? "manifest: unified" : "manifest: legacy";
            var outputState = plugin.HasPluginAssembly && plugin.HasOutputUnifiedManifest ? "build: ready" : "build: missing";
            var testsState = plugin.HasTestProject ? "tests: present" : "tests: missing";
            var changelogState = plugin.HasChangelog
                ? plugin.HasUnreleasedChangelog ? "changelog: unreleased" : "changelog: present"
                : "changelog: missing";

            Console.WriteLine($"[{plugin.PluginId}] {plugin.Name} {plugin.Version} ({manifestState}, {storeState}, {outputState}, {testsState}, {changelogState})");
            Console.WriteLine($"  path: {plugin.DirectoryPath}");
            Console.WriteLine($"  output: {plugin.OutputDirectory}");
        }

        var outputPath = OptionalValue(argv, "--json-report-path");
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await JsonReportFile.WriteAsync(Path.GetFullPath(outputPath), report);
        }

        return 0;
    }

    async Task<int> RunNewAsync(string repositoryRoot, string[] argv)
    {
        var request = new ScaffoldRequest
        {
            RepositoryRoot = repositoryRoot,
            Template = ParseTemplate(RequireValue(argv, "--template")),
            FolderName = RequireValue(argv, "--folder"),
            PluginId = RequireValue(argv, "--id"),
            DisplayName = RequireValue(argv, "--name"),
            Author = OptionalValue(argv, "--author") ?? Environment.UserName,
            Description = OptionalValue(argv, "--description") ?? string.Empty,
            MinimumHostVersion = OptionalValue(argv, "--min-llt-version") ?? "5.0.0",
            Official = HasFlag(argv, "--official"),
        };

        var scaffolder = new PluginScaffolder();
        var result = await scaffolder.CreateAsync(request, Console.WriteLine);
        Console.WriteLine($"Plugin scaffold created: {result.PluginDirectory}");
        Console.WriteLine($"Test project created: {result.TestDirectory}");
        if (!string.IsNullOrWhiteSpace(result.StoreEntryPath))
        {
            Console.WriteLine($"Official metadata scaffolded: {result.StoreEntryPath}");
        }

        return 0;
    }

    async Task<int> RunBuildAsync(string repositoryRoot, string[] argv)
    {
        var repo = repository.Load(repositoryRoot);
        var pluginId = repository.ResolveTargetPluginIds(repo, [RequireValue(argv, "--plugin")]).Single();
        var plugin = repo.Plugins[pluginId];
        var configuration = OptionalValue(argv, "--configuration") ?? "Release";

        var runner = new ProcessRunner();
        var exitCode = await runner.RunDotnetAsync(["build", plugin.ProjectPath!, "-c", configuration, "--nologo"], repo.RootPath, Console.WriteLine);
        return exitCode;
    }

    async Task<int> RunPreviewAsync(string repositoryRoot, string[] argv)
    {
        var pluginId = RequireValue(argv, "--plugin");
        var theme = OptionalValue(argv, "--theme") ?? "system";
        var view = OptionalValue(argv, "--view") ?? "feature";
        var workbenchProject = Path.Combine(repositoryRoot, "Tooling", "PluginWorkbench", "PluginWorkbench.csproj");

        var runner = new ProcessRunner();
        return await runner.RunDotnetAsync(
            ["run", "--project", workbenchProject, "--", "--repository-root", repositoryRoot, "--plugin-id", pluginId, "--theme", theme, "--view", view],
            repositoryRoot,
            Console.WriteLine);
    }

    async Task<int> RunDevAsync(string repositoryRoot, string[] argv)
    {
        var pluginId = RequireValue(argv, "--plugin");
        var configuration = OptionalValue(argv, "--configuration") ?? "Release";
        var buildExitCode = await RunBuildAsync(repositoryRoot, ["build", "--plugin", pluginId, "--configuration", configuration]);
        if (buildExitCode != 0)
        {
            return buildExitCode;
        }

        var previewArgs = new List<string>
        {
            "preview",
            "--plugin",
            pluginId,
            "--theme",
            OptionalValue(argv, "--theme") ?? "system",
            "--view",
            OptionalValue(argv, "--view") ?? "feature",
        };

        return await RunPreviewAsync(repositoryRoot, previewArgs.ToArray());
    }

    async Task<int> RunTestAsync(string repositoryRoot, string[] argv)
    {
        var repo = repository.Load(repositoryRoot);
        var pluginId = repository.ResolveTargetPluginIds(repo, [RequireValue(argv, "--plugin")]).Single();
        var plugin = repo.Plugins[pluginId];
        var configuration = OptionalValue(argv, "--configuration") ?? "Release";
        if (string.IsNullOrWhiteSpace(plugin.TestProjectPath))
        {
            return Fail($"Plugin '{pluginId}' does not have a test project.");
        }

        var runner = new ProcessRunner();
        return await runner.RunDotnetAsync(["test", plugin.TestProjectPath!, "-c", configuration, "--nologo"], repo.RootPath, Console.WriteLine);
    }

    async Task<int> RunValidateAsync(string repositoryRoot, string[] argv)
    {
        var service = new PluginValidationService(Console.WriteLine);
        var profile = ParseProfile(OptionalValue(argv, "--profile") ?? "contributor");
        var rawPluginIds = OptionalValue(argv, "--plugin-ids");
        var pluginId = OptionalValue(argv, "--plugin");
        var selection = rawPluginIds is not null
            ? rawPluginIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : pluginId is not null ? [pluginId] : Array.Empty<string>();

        var report = await service.RunAsync(new ValidationRequest
        {
            RepositoryRoot = repositoryRoot,
            Configuration = OptionalValue(argv, "--configuration") ?? "Release",
            Profile = profile,
            SkipBuild = HasFlag(argv, "--skip-build"),
            SkipTests = HasFlag(argv, "--skip-tests"),
            PluginIds = selection,
        });

        var outputPath = OptionalValue(argv, "--json-report-path");
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await JsonReportFile.WriteAsync(Path.GetFullPath(outputPath), report);
        }

        return report.Totals.Failures == 0 ? 0 : 1;
    }

    async Task<int> RunPackAsync(string repositoryRoot, string[] argv)
    {
        var packager = new PluginPackager();
        var result = await packager.PackAsync(new PackRequest
        {
            RepositoryRoot = repositoryRoot,
            PluginId = RequireValue(argv, "--plugin"),
            Configuration = OptionalValue(argv, "--configuration") ?? "Release",
            OutputDirectory = OptionalValue(argv, "--output-dir"),
            BuildFirst = HasFlag(argv, "--build-first"),
        }, Console.WriteLine);

        Console.WriteLine(result.ZipPath);
        return 0;
    }

    int RunPromote(string repositoryRoot, string[] argv)
    {
        var scaffolder = new PluginScaffolder();
        var result = scaffolder.Promote(new PromoteRequest
        {
            RepositoryRoot = repositoryRoot,
            PluginId = RequireValue(argv, "--plugin"),
            Overwrite = HasFlag(argv, "--overwrite"),
        });

        Console.WriteLine(result.Created
            ? $"Created {result.StoreEntryPath}"
            : $"Already exists: {result.StoreEntryPath}");
        return 0;
    }

    int RunMigrate(string repositoryRoot, string[] argv)
    {
        return RunSyncVersion(repositoryRoot, argv);
    }

    int RunSyncVersion(string repositoryRoot, string[] argv)
    {
        var selection = ResolvePluginSelection(argv);
        var checkOnly = HasFlag(argv, "--check");
        var synchronizer = new PluginVersionSynchronizer();
        var reports = synchronizer.SyncRepository(repositoryRoot, selection, checkOnly, Console.WriteLine);

        foreach (var report in reports)
        {
            if (report.DriftMessages.Count == 0)
            {
                Console.WriteLine($"[{report.PluginId}] aligned at {report.ManifestVersion}");
                continue;
            }

            Console.WriteLine($"[{report.PluginId}] manifest {report.ManifestVersion}");
            foreach (var drift in report.DriftMessages)
            {
                Console.WriteLine($"  - {drift}");
            }

            if (report.Changed)
            {
                Console.WriteLine($"  synced: {string.Join(", ", report.Actions)}");
            }
        }

        if (checkOnly && reports.Any(report => !report.IsAligned))
        {
            return 1;
        }

        Console.WriteLine(checkOnly
            ? $"Checked {reports.Count} plugin version graph(s)."
            : $"Synced {reports.Count} plugin version graph(s).");
        return 0;
    }

    int RunBumpVersion(string repositoryRoot, string[] argv)
    {
        var pluginId = RequireValue(argv, "--plugin");
        var explicitVersion = OptionalValue(argv, "--version");
        var partRaw = OptionalValue(argv, "--part");
        VersionBumpPart? part = partRaw is null
            ? null
            : partRaw.ToLowerInvariant() switch
            {
                "patch" => VersionBumpPart.Patch,
                "minor" => VersionBumpPart.Minor,
                "major" => VersionBumpPart.Major,
                _ => throw new InvalidOperationException($"Unknown --part value '{partRaw}'. Use patch, minor, or major."),
            };

        if (explicitVersion is null && part is null)
        {
            part = VersionBumpPart.Patch;
        }

        var repository = new PluginRepository();
        var plugin = repository.Load(repositoryRoot).Plugins[pluginId];
        var synchronizer = new PluginVersionSynchronizer();
        var report = synchronizer.Bump(plugin, part, explicitVersion, writeChanges: !HasFlag(argv, "--check"), Console.WriteLine);

        if (HasFlag(argv, "--check"))
        {
            foreach (var drift in report.DriftMessages)
            {
                Console.WriteLine($"  - {drift}");
            }

            return 0;
        }

        Console.WriteLine($"[{report.PluginId}] bumped to {report.ManifestVersion}");
        return 0;
    }

    static string[] ResolvePluginSelection(string[] argv)
    {
        var rawPluginIds = OptionalValue(argv, "--plugin-ids");
        var pluginId = OptionalValue(argv, "--plugin");
        return rawPluginIds is not null
            ? rawPluginIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : pluginId is not null ? [pluginId] : Array.Empty<string>();
    }

    int RunGenerateStore(string repositoryRoot, string[] argv)
    {
        var generator = new StoreJsonGenerator();
        var pluginIds = OptionalValue(argv, "--plugin-ids")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? (OptionalValue(argv, "--plugin") is { } pluginId ? [pluginId] : Array.Empty<string>());

        var request = new StoreGenerationRequest
        {
            RepositoryRoot = repositoryRoot,
            OutputPath = OptionalValue(argv, "--output"),
            ReleaseRepositoryUrl = OptionalValue(argv, "--release-repository-url") ?? "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog",
            AssetRoot = OptionalValue(argv, "--asset-root"),
            PluginIds = pluginIds,
            ReleaseDate = ParseReleaseDate(OptionalValue(argv, "--release-date")),
            MergeExisting = HasFlag(argv, "--merge-existing"),
            RequireAssets = HasFlag(argv, "--require-assets"),
        };

        if (HasFlag(argv, "--check"))
        {
            var result = generator.Check(request);
            Console.WriteLine(result.Message);
            Console.WriteLine(result.StorePath);
            return result.Matches ? 0 : 1;
        }

        var path = generator.Write(request);
        Console.WriteLine(path);
        return 0;
    }
}

static PluginArchetype ParseTemplate(string rawValue)
{
    return rawValue.ToLowerInvariant() switch
    {
        "settings-only" => PluginArchetype.SettingsOnly,
        "feature-settings" => PluginArchetype.FeatureSettings,
        "runtime-optimization" => PluginArchetype.RuntimeOptimization,
        _ => throw new InvalidOperationException($"Unknown template '{rawValue}'."),
    };
}

static PluginValidationProfile ParseProfile(string rawValue)
{
    return rawValue.ToLowerInvariant() switch
    {
        "contributor" => PluginValidationProfile.Contributor,
        "official-candidate" => PluginValidationProfile.OfficialCandidate,
        "official-release" => PluginValidationProfile.OfficialRelease,
        _ => throw new InvalidOperationException($"Unknown validation profile '{rawValue}'."),
    };
}

static IReadOnlyList<string> ParsePluginSelection(string[] args)
{
    var rawPluginIds = OptionalValue(args, "--plugin-ids");
    var pluginId = OptionalValue(args, "--plugin");
    return rawPluginIds is not null
        ? rawPluginIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : pluginId is not null ? [pluginId] : Array.Empty<string>();
}

static DateTimeOffset? ParseReleaseDate(string? rawValue)
{
    if (string.IsNullOrWhiteSpace(rawValue))
    {
        return null;
    }

    if (DateTimeOffset.TryParse(rawValue, out var releaseDate))
    {
        return releaseDate;
    }

    throw new InvalidOperationException($"Invalid ISO-8601 release date: {rawValue}");
}

static bool HasFlag(string[] args, string option)
{
    return args.Any(arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
}

static string? OptionalValue(string[] args, string option)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (!string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (i + 1 >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        return args[i + 1];
    }

    return null;
}

static string RequireValue(string[] args, string option)
{
    return OptionalValue(args, option)
           ?? throw new InvalidOperationException($"Missing required option {option}.");
}

static bool IsHelp(string argument)
{
    return argument is "-h" or "/h" or "--help" or "help";
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
plugin-tooling doctor [--repository-root <path>] [--json-report-path <path>]
plugin-tooling inspect [--repository-root <path>] [--plugin <plugin-id>|--plugin-ids <id,id>] [--json-report-path <path>]
plugin-tooling init --template <settings-only|feature-settings|runtime-optimization> --folder <FolderName> --id <plugin-id> --name <DisplayName> [--author <Author>] [--description <Text>] [--min-llt-version <X.Y.Z>] [--official]
plugin-tooling dev --plugin <plugin-id> [--configuration Release] [--theme system|light|dark] [--view feature|settings|optimization]
plugin-tooling build --plugin <plugin-id> [--configuration Release]
plugin-tooling test --plugin <plugin-id> [--configuration Release]
plugin-tooling preview --plugin <plugin-id> [--theme system|light|dark] [--view feature|settings|optimization]
plugin-tooling validate [--plugin <plugin-id>|--plugin-ids <id,id>] [--profile contributor|official-candidate|official-release] [--skip-build] [--skip-tests] [--json-report-path <path>]
plugin-tooling package --plugin <plugin-id> [--configuration Release] [--output-dir <path>] [--build-first]
plugin-tooling migrate [--plugin <plugin-id>|--plugin-ids <id,id>] [--check]
plugin-tooling sync-version [--plugin <plugin-id>|--plugin-ids <id,id>] [--check]
plugin-tooling bump-version --plugin <plugin-id> [--part patch|minor|major] [--version <x.y.z>] [--check]
plugin-tooling promote --plugin <plugin-id> [--overwrite]
plugin-tooling generate-store [--output <path>] [--asset-root <path>] [--release-repository-url <url>] [--release-date <iso-8601>] [--plugin <plugin-id>|--plugin-ids <id,id>] [--merge-existing] [--require-assets] [--check]
""");
}
