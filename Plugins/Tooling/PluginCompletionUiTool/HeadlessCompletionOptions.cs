using System.IO;

namespace PluginCompletionUiTool;

internal sealed class HeadlessCompletionOptions
{
    public string RepositoryRoot { get; init; } = string.Empty;

    public string ReportPath { get; init; } = string.Empty;

    public string Configuration { get; init; } = "Release";

    public bool SkipBuild { get; init; }

    public bool SkipTests { get; init; }

    public IReadOnlyList<string> PluginIds { get; init; } = Array.Empty<string>();

    public static HeadlessCompletionOptions? Parse(string[] args, out string? error)
    {
        error = null;

        if (!args.Any(static arg => string.Equals(arg, "--headless", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var repositoryRoot = Environment.CurrentDirectory;
        string? reportPath = null;
        var configuration = "Release";
        var skipBuild = false;
        var skipTests = false;
        var pluginIds = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--headless":
                    break;
                case "--repository-root":
                    repositoryRoot = RequireValue(args, ref index, argument, out error);
                    if (error is not null)
                    {
                        return null;
                    }

                    break;
                case "--json-report-path":
                    reportPath = RequireValue(args, ref index, argument, out error);
                    if (error is not null)
                    {
                        return null;
                    }

                    break;
                case "--configuration":
                    configuration = RequireValue(args, ref index, argument, out error);
                    if (error is not null)
                    {
                        return null;
                    }

                    break;
                case "--skip-build":
                    skipBuild = true;
                    break;
                case "--skip-tests":
                    skipTests = true;
                    break;
                case "--plugin-id":
                    var pluginId = RequireValue(args, ref index, argument, out error);
                    if (error is not null)
                    {
                        return null;
                    }

                    if (!string.IsNullOrWhiteSpace(pluginId))
                    {
                        pluginIds.Add(pluginId);
                    }

                    break;
                case "--plugin-ids":
                    var pluginIdList = RequireValue(args, ref index, argument, out error);
                    if (error is not null)
                    {
                        return null;
                    }

                    pluginIds.AddRange(SplitPluginIds(pluginIdList));
                    break;
                default:
                    error = $"Unknown headless argument: {argument}";
                    return null;
            }
        }

        var resolvedRepositoryRoot = Path.GetFullPath(repositoryRoot);
        var resolvedReportPath = reportPath is null
            ? Path.Combine(resolvedRepositoryRoot, "artifacts", "plugin-completion-report.json")
            : Path.GetFullPath(reportPath);

        return new HeadlessCompletionOptions
        {
            RepositoryRoot = resolvedRepositoryRoot,
            ReportPath = resolvedReportPath,
            Configuration = string.IsNullOrWhiteSpace(configuration) ? "Release" : configuration.Trim(),
            SkipBuild = skipBuild,
            SkipTests = skipTests,
            PluginIds = pluginIds
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static string RequireValue(string[] args, ref int index, string argumentName, out string? error)
    {
        if (index + 1 >= args.Length)
        {
            error = $"Missing value for {argumentName}.";
            return string.Empty;
        }

        index++;
        error = null;
        return args[index];
    }

    private static IEnumerable<string> SplitPluginIds(string rawText)
    {
        return rawText
            .Split([',', ';', '\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
