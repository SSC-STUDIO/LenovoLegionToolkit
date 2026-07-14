using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.ResourcesCatalog;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;

const string pass = "PASS";
const string fail = "FAIL";

var failures = 0;

try
{
    Environment.SetEnvironmentVariable(
        OnlineResourceCatalogClient.CatalogUrlEnvironmentVariable,
        "https://example.test/resources/stable/catalog.json");

    failures += await RunProgressReportsEndAtOneAsync() ? 0 : 1;
    failures += await RunInstallsResourceFilesAsync() ? 0 : 1;
    failures += RunUiProgressLabelThresholds() ? 0 : 1;
}
finally
{
    Environment.SetEnvironmentVariable(OnlineResourceCatalogClient.CatalogUrlEnvironmentVariable, null);
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? $"{pass}: All language pack progress smoke checks passed." : $"{fail}: {failures} check(s) failed.");
return failures == 0 ? 0 : 1;

static async Task<bool> RunProgressReportsEndAtOneAsync()
{
    var culture = new CultureInfo("ff-latn-sn");
    var version = GetCurrentVersion();
    var fullZipName = $"{AppIdentity.CompactName}_v{version}_Full_win-x64.zip";
    var hashName = $"{AppIdentity.CompactName}_v{version}_SHA256.txt";
    var fullZip = CreateFullPortableZip();
    var fullZipSha256 = Convert.ToHexString(SHA256.HashData(fullZip)).ToLowerInvariant();
    var hashText = $"{fullZipSha256}  {fullZipName}";

    var responses = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
    {
        [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{hashName}"] = Encoding.UTF8.GetBytes(hashText),
        [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{fullZipName}"] = fullZip
    };

    var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new TestHttpClientFactory(responses)));
    var installRoot = AppContext.BaseDirectory;
    var directories = new[]
    {
        Path.Combine(installRoot, "ff"),
        Path.Combine(installRoot, "ff-Latn"),
        Path.Combine(installRoot, "ff-Latn-SN")
    };

    var progressValues = new ConcurrentBag<float>();

    try
    {
        foreach (var directory in directories)
            TryDeleteDirectory(directory);

        var progress = new Progress<float>(progressValues.Add);
        await manager.InstallAsync(culture, progress);

        var ordered = progressValues.OrderBy(value => value).ToArray();
        if (ordered.Length == 0)
        {
            Console.WriteLine($"{fail}: No progress values were reported.");
            return false;
        }

        if (Math.Abs(ordered[^1] - 1f) > 0.0001f)
        {
            Console.WriteLine($"{fail}: Final progress was {ordered[^1]}, expected 1.");
            return false;
        }

        if (ordered.Any(value => value is < 0f or > 1f))
        {
            Console.WriteLine($"{fail}: Progress contained out-of-range values: {string.Join(", ", ordered)}");
            return false;
        }

        if (!progressValues.Any(value => value >= 0.85f))
        {
            Console.WriteLine($"{fail}: Progress never reached apply phase (>= 0.85). Values: {string.Join(", ", ordered)}");
            return false;
        }

        Console.WriteLine($"{pass}: Progress reports ({ordered.Length} samples, final={ordered[^1]:P0}, apply phase reached).");
        Console.WriteLine($"      Samples: {string.Join(" -> ", ordered.Select(v => v.ToString("P0")))}");
        return true;
    }
    finally
    {
        foreach (var directory in directories)
            TryDeleteDirectory(directory);
    }
}

static bool RunUiProgressLabelThresholds()
{
    // Mirrors SettingsAppearanceControl / LanguageSelectorWindow: < 0.85 => download, >= 0.85 => applying.
    static string Phase(float value) => value >= 0.85f ? "applying" : "download";

    var cases = new (float Value, string Expected)[]
    {
        (0f, "download"),
        (0.5f, "download"),
        (0.84f, "download"),
        (0.85f, "applying"),
        (1f, "applying")
    };

    foreach (var (value, expected) in cases)
    {
        if (Phase(value) != expected)
        {
            Console.WriteLine($"{fail}: UI phase label for progress {value} expected '{expected}', got '{Phase(value)}'.");
            return false;
        }
    }

    Console.WriteLine($"{pass}: UI progress phase labels switch at 85% as designed.");
    return true;
}

static async Task<bool> RunInstallsResourceFilesAsync()
{
    var culture = new CultureInfo("ff-latn-sn");
    var version = GetCurrentVersion();
    var fullZipName = $"{AppIdentity.CompactName}_v{version}_Full_win-x64.zip";
    var hashName = $"{AppIdentity.CompactName}_v{version}_SHA256.txt";
    var fullZip = CreateFullPortableZip();
    var fullZipSha256 = Convert.ToHexString(SHA256.HashData(fullZip)).ToLowerInvariant();
    var hashText = $"{fullZipSha256}  {fullZipName}";

    var responses = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
    {
        [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{hashName}"] = Encoding.UTF8.GetBytes(hashText),
        [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{fullZipName}"] = fullZip
    };

    var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new TestHttpClientFactory(responses)));
    var installRoot = AppContext.BaseDirectory;
    var ffDirectory = Path.Combine(installRoot, "ff");
    var ffLatnDirectory = Path.Combine(installRoot, "ff-Latn");
    var ffLatnSnDirectory = Path.Combine(installRoot, "ff-Latn-SN");

    try
    {
        TryDeleteDirectory(ffDirectory);
        TryDeleteDirectory(ffLatnDirectory);
        TryDeleteDirectory(ffLatnSnDirectory);

        await manager.InstallAsync(culture);

        var ok = File.Exists(Path.Combine(ffDirectory, "Universal Device Toolkit.resources.dll"))
                 && File.Exists(Path.Combine(ffLatnDirectory, "Humanizer.resources.dll"))
                 && File.Exists(Path.Combine(ffLatnSnDirectory, "Wpf.Ui.resources.dll"));

        if (!ok)
        {
            Console.WriteLine($"{fail}: Installed language resource files were missing after install.");
            return false;
        }

        Console.WriteLine($"{pass}: Language pack files installed to app base directory.");
        return true;
    }
    finally
    {
        TryDeleteDirectory(ffDirectory);
        TryDeleteDirectory(ffLatnDirectory);
        TryDeleteDirectory(ffLatnSnDirectory);
    }
}

static byte[] CreateFullPortableZip()
{
    using var stream = new MemoryStream();
    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
    {
        AddZipEntry(archive, "ff/Universal Device Toolkit.resources.dll", "resource");
        AddZipEntry(archive, "ff-Latn/Humanizer.resources.dll", "humanizer");
        AddZipEntry(archive, "ff-Latn-SN/Wpf.Ui.resources.dll", "wpf");
    }

    return stream.ToArray();
}

static void AddZipEntry(ZipArchive archive, string name, string content)
{
    var entry = archive.CreateEntry(name);
    using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
    writer.Write(content);
}

static string GetCurrentVersion()
{
    var version = Assembly.GetEntryAssembly()?.GetName().Version ?? typeof(LanguagePackManager).Assembly.GetName().Version;
    return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
}

static void TryDeleteDirectory(string directory)
{
    try
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }
    catch
    {
        // best-effort cleanup
    }
}

sealed class TestHttpClientFactory(IReadOnlyDictionary<string, byte[]> responses) : HttpClientFactory
{
    public override HttpClient Create() => new(new TestHandler(responses), disposeHandler: true);
}

sealed class TestHandler(IReadOnlyDictionary<string, byte[]> responses) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = request.RequestUri?.ToString() ?? string.Empty;
        if (!responses.TryGetValue(key, out var response))
            return Task.FromException<HttpResponseMessage>(new HttpRequestException("Simulated network failure."));

        var content = new ByteArrayContent(response);
        content.Headers.ContentLength = response.Length;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }
}
