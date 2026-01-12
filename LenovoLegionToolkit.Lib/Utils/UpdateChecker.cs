using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Settings;
using NeoSmart.AsyncLock;
using Octokit;
using Octokit.Internal;

namespace LenovoLegionToolkit.Lib.Utils;

public class UpdateChecker
{
    private readonly HttpClientFactory _httpClientFactory;
    private readonly UpdateCheckSettings _updateCheckSettings = IoCContainer.Resolve<UpdateCheckSettings>();
    private readonly AsyncLock _updateSemaphore = new();

    private DateTime _lastUpdate;
    private TimeSpan _minimumTimeSpanForRefresh;
    private Update[] _updates = [];

    public bool Disable { get; set; }
    public UpdateCheckStatus Status { get; set; }

    public UpdateChecker(HttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;

        UpdateMinimumTimeSpanForRefresh();
        _lastUpdate = _updateCheckSettings.Store.LastUpdateCheckDateTime ?? DateTime.MinValue;
    }

    public async Task<Version?> CheckAsync(bool forceCheck)
    {
        using (await _updateSemaphore.LockAsync().ConfigureAwait(false))
        {
            if (Disable)
            {
                _lastUpdate = DateTime.UtcNow;
                _updates = [];
                return null;
            }

            try
            {
                var timeSpanSinceLastUpdate = DateTime.UtcNow - _lastUpdate;
                var shouldCheck = timeSpanSinceLastUpdate > _minimumTimeSpanForRefresh;

                if (!forceCheck && !shouldCheck)
                    return _updates.Length != 0 ? _updates.First().Version : null;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Checking...");

                var adapter = new HttpClientAdapter(_httpClientFactory.CreateHandler);
                var productInformation = new ProductHeaderValue("LenovoLegionToolkit-UpdateChecker");
                var connection = new Connection(productInformation, adapter);
                var githubClient = new GitHubClient(connection);
                
                // Get update repository from settings, fallback to default if not configured
                const string DefaultUpdateRepositoryOwner = "Crs10259";
                const string DefaultUpdateRepositoryName = "LenovoLegionToolkit";
                var repositoryOwner = !string.IsNullOrWhiteSpace(_updateCheckSettings.Store.UpdateRepositoryOwner) 
                    ? _updateCheckSettings.Store.UpdateRepositoryOwner 
                    : DefaultUpdateRepositoryOwner;
                var repositoryName = !string.IsNullOrWhiteSpace(_updateCheckSettings.Store.UpdateRepositoryName) 
                    ? _updateCheckSettings.Store.UpdateRepositoryName 
                    : DefaultUpdateRepositoryName;
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Checking updates from repository: {repositoryOwner}/{repositoryName}");
                
                var releases = await githubClient.Repository.Release.GetAll(repositoryOwner, repositoryName, new ApiOptions { PageSize = 10 }).ConfigureAwait(false);

                var thisReleaseVersion = Assembly.GetEntryAssembly()?.GetName().Version;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Current version: {thisReleaseVersion}, Found releases: {releases.Count}");

                var publicReleases = releases.Where(r => !r.Draft && !r.Prerelease).ToArray();
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Public releases (non-draft, non-prerelease): {publicReleases.Length}");

                var updates = publicReleases
                    .Select(r =>
                    {
                        try
                        {
                            var update = new Update(r);
                            // Only compare if thisReleaseVersion is not null
                            var isNewer = thisReleaseVersion != null && update.Version > thisReleaseVersion;
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Release {update.Version} (tag: {r.TagName}) is {(isNewer ? "newer" : "not newer")} than current version {thisReleaseVersion}");
                            return (Update: (Update?)update, IsNewer: isNewer);
                        }
                        catch (Exception ex)
                        {
                            // Skip releases with invalid version tags (e.g., "v3" without minor/patch)
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Skipping release with invalid version tag: {r.TagName}", ex);
                            return (Update: (Update?)null, IsNewer: false);
                        }
                    })
                    .Where(r => r.Update.HasValue && r.IsNewer)
                    .Select(r => r.Update!.Value)
                    .OrderByDescending(r => r.Version)
                    .ToArray();

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Checked [updates.Length={updates.Length}]");

                _updates = updates;
                Status = UpdateCheckStatus.Success;

                return _updates.Length != 0 ? _updates.First().Version : null;
            }
            catch (RateLimitExceededException ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Reached API Rate Limitation.", ex);

                Status = UpdateCheckStatus.RateLimitReached;
                return null;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error checking for updates.", ex);

                Status = UpdateCheckStatus.Error;
                return null;
            }
            finally
            {
                _lastUpdate = DateTime.UtcNow;
                _updateCheckSettings.Store.LastUpdateCheckDateTime = _lastUpdate;
                _updateCheckSettings.SynchronizeStore();
            }
        }
    }

    public async Task<Update[]> GetUpdatesAsync()
    {
        using (await _updateSemaphore.LockAsync().ConfigureAwait(false))
            return _updates;
    }

    public async Task<string> DownloadLatestUpdateAsync(IProgress<float>? progress = null, CancellationToken cancellationToken = default)
    {
        using (await _updateSemaphore.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            var tempPath = Path.Combine(Folders.Temp, $"LenovoLegionToolkitSetup_{Guid.NewGuid()}.exe");
            var latestUpdate = _updates.OrderByDescending(u => u.Version).FirstOrDefault();

            if (latestUpdate.Equals(default))
                throw new InvalidOperationException("No updates available");

            if (latestUpdate.Url is null)
                throw new InvalidOperationException("Setup file URL could not be found");

            await using var fileStream = File.OpenWrite(tempPath);
            using var httpClient = _httpClientFactory.Create();
            await httpClient.DownloadAsync(latestUpdate.Url, fileStream, progress, cancellationToken).ConfigureAwait(false);

            return tempPath;
        }
    }

    public void UpdateMinimumTimeSpanForRefresh() => _minimumTimeSpanForRefresh = _updateCheckSettings.Store.UpdateCheckFrequency switch
    {
        UpdateCheckFrequency.PerHour => TimeSpan.FromHours(1),
        UpdateCheckFrequency.PerThreeHours => TimeSpan.FromHours(3),
        UpdateCheckFrequency.PerTwelveHours => TimeSpan.FromHours(13),
        UpdateCheckFrequency.PerDay => TimeSpan.FromDays(1),
        UpdateCheckFrequency.PerWeek => TimeSpan.FromDays(7),
        UpdateCheckFrequency.PerMonth => TimeSpan.FromDays(30),
        _ => throw new ArgumentException(nameof(_updateCheckSettings.Store.UpdateCheckFrequency))
    };
}
