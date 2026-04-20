namespace PluginCompletionUiTool;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var options = HeadlessCompletionOptions.Parse(e.Args, out var error);
        if (error is not null)
        {
            Console.Error.WriteLine(error);
            Shutdown(1);
            return;
        }

        if (options is null)
        {
            base.OnStartup(e);
            return;
        }

        try
        {
            Console.WriteLine($"[completion] Repository root: {options.RepositoryRoot}");

            var checker = new CompletionChecker(static line => Console.WriteLine(line));
            var report = await checker.RunAsync(new CompletionCheckRequest
            {
                RepositoryRoot = options.RepositoryRoot,
                Configuration = options.Configuration,
                SkipBuild = options.SkipBuild,
                SkipTests = options.SkipTests,
                PluginIds = options.PluginIds
            });

            await CompletionReportFile.WriteAsync(options.ReportPath, report);
            Console.WriteLine($"[completion] Report written to: {options.ReportPath}");
            Shutdown(report.Totals.Failures == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Shutdown(1);
        }
    }
}
