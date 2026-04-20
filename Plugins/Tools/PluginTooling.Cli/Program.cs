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
            "doctor" => RunDoctor(root),
            "new" => await RunNewAsync(root, args),
            "build" => await RunBuildAsync(root, args),
            "preview" => await RunPreviewAsync(root, args),
            "validate" => await RunValidateAsync(root, args),
            "pack" => await RunPackAsync(root, args),
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
                continue;

            if (i + 1 >= argv.Length)
                throw new InvalidOperationException("Missing value for --repository-root.");

            return Path.GetFullPath(argv[i + 1]);
        }

        return PluginRepository.FindRepositoryRoot(Environment.CurrentDirectory);
    }

    int RunDoctor(string repositoryRoot)
    {
        var service = new DoctorService();
        var result = service.Run(repositoryRoot);
        foreach (var check in result.Checks)
            Console.WriteLine($"[{check.Status}] {check.Message}");

        return result.FailureCount == 0 ? 0 : 1;
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
            MinimumHostVersion = OptionalValue(argv, "--min-llt-version") ?? "3.6.14",
            Official = HasFlag(argv, "--official"),
        };

        var scaffolder = new PluginScaffolder();
        var result = await scaffolder.CreateAsync(request, Console.WriteLine);
        Console.WriteLine($"Plugin scaffold created: {result.PluginDirectory}");
        Console.WriteLine($"Test project created: {result.TestDirectory}");
        if (!string.IsNullOrWhiteSpace(result.StoreEntryPath))
            Console.WriteLine($"Official metadata scaffolded: {result.StoreEntryPath}");
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
        var workbenchProject = Path.Combine(repositoryRoot, "Tools", "PluginWorkbench", "PluginWorkbench.csproj");

        var runner = new ProcessRunner();
        return await runner.RunDotnetAsync(
            ["run", "--project", workbenchProject, "--", "--repository-root", repositoryRoot, "--plugin-id", pluginId, "--theme", theme, "--view", view],
            repositoryRoot,
            Console.WriteLine);
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
            await JsonReportFile.WriteAsync(Path.GetFullPath(outputPath), report);

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

    int RunGenerateStore(string repositoryRoot, string[] argv)
    {
        var generator = new StoreJsonGenerator();
        var pluginIds = OptionalValue(argv, "--plugin-ids")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        var path = generator.Write(new StoreGenerationRequest
        {
            RepositoryRoot = repositoryRoot,
            OutputPath = OptionalValue(argv, "--output"),
            ReleaseRepositoryUrl = OptionalValue(argv, "--release-repository-url") ?? "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases",
            AssetRoot = OptionalValue(argv, "--asset-root"),
            PluginIds = pluginIds,
        });

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

static bool HasFlag(string[] args, string option)
{
    return args.Any(arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
}

static string? OptionalValue(string[] args, string option)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (!string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase))
            continue;

        if (i + 1 >= args.Length)
            throw new InvalidOperationException($"Missing value for {option}.");

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
plugin-tooling doctor [--repository-root <path>]
plugin-tooling new --template <settings-only|feature-settings|runtime-optimization> --folder <FolderName> --id <plugin-id> --name <DisplayName> [--author <Author>] [--description <Text>] [--min-llt-version <X.Y.Z>] [--official]
plugin-tooling build --plugin <plugin-id> [--configuration Release]
plugin-tooling preview --plugin <plugin-id> [--theme system|light|dark] [--view feature|settings|optimization]
plugin-tooling validate [--plugin <plugin-id>|--plugin-ids <id,id>] [--profile contributor|official-candidate|official-release] [--skip-build] [--skip-tests] [--json-report-path <path>]
plugin-tooling pack --plugin <plugin-id> [--configuration Release] [--output-dir <path>] [--build-first]
plugin-tooling promote --plugin <plugin-id> [--overwrite]
plugin-tooling generate-store [--output <path>] [--asset-root <path>] [--release-repository-url <url>]
""");
}
