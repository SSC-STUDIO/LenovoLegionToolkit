using System;

namespace UniversalDeviceToolkit.WPF;

public static class Constants
{
    // Update repository configuration - modify these to change update source
    // Also update the same constants in UpdateChecker.cs in the Lib project
    public const string UpdateRepositoryOwner = LenovoLegionToolkit.Lib.Utils.AppIdentity.RepositoryOwner;
    public const string UpdateRepositoryName = LenovoLegionToolkit.Lib.Utils.AppIdentity.RepositoryName;
    public const string ProjectWebsiteUrl = $"https://github.com/{UpdateRepositoryOwner}/{UpdateRepositoryName}";
    public const string LatestReleaseUrl = $"https://github.com/{UpdateRepositoryOwner}/{UpdateRepositoryName}/releases/latest";
    public const string ContributionUrl = $"https://github.com/{UpdateRepositoryOwner}/{UpdateRepositoryName}";
    public static readonly Uri ProjectWebsiteUri = new(ProjectWebsiteUrl);
    public static readonly Uri LatestReleaseUri = new(LatestReleaseUrl);
    public static readonly Uri ContributionUri = new(ContributionUrl);
}
