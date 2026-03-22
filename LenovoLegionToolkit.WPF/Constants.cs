using System;

namespace LenovoLegionToolkit.WPF;

public static class Constants
{
    // Update repository configuration - modify these to change update source
    // Also update the same constants in UpdateChecker.cs in the Lib project
    public const string UpdateRepositoryOwner = "SSC-STUDIO";
    public const string UpdateRepositoryName = "LenovoLegionToolkit";
    public const string ProjectWebsiteUrl = $"https://github.com/{UpdateRepositoryOwner}/{UpdateRepositoryName}";
    public const string LatestReleaseUrl = $"https://github.com/{UpdateRepositoryOwner}/{UpdateRepositoryName}/releases/latest";
    public const string ContributionUrl = $"https://github.com/{UpdateRepositoryOwner}/{UpdateRepositoryName}#contribution";
    public static readonly Uri ProjectWebsiteUri = new(ProjectWebsiteUrl);
    public static readonly Uri LatestReleaseUri = new(LatestReleaseUrl);
    public static readonly Uri ContributionUri = new(ContributionUrl);
}
