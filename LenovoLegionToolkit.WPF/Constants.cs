using System;

namespace LenovoLegionToolkit.WPF;

public static class Constants
{
    // Update repository configuration - modify these to change update source
    // Also update the same constants in UpdateChecker.cs in the Lib project
    public const string UpdateRepositoryOwner = "Crs10259";
    public const string UpdateRepositoryName = "LenovoLegionToolkit";
    public static readonly Uri LatestReleaseUri = new($"https://github.com/{UpdateRepositoryOwner}/{UpdateRepositoryName}/releases/latest");

}
