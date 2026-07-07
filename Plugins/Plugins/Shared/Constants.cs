namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Common constants used across all plugins.
/// Centralizes magic numbers and configuration values.
/// </summary>
public static class Constants
{
    #region Timeout values

    /// <summary>
    /// Default timeout in seconds for HTTP requests and general operations.
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Timeout in seconds for file downloads.
    /// </summary>
    public const int DownloadTimeoutSeconds = 120;

    /// <summary>
    /// Timeout in seconds for external process execution.
    /// </summary>
    public const int ProcessTimeoutSeconds = 60;

    #endregion

    #region Buffer sizes

    /// <summary>
    /// Default buffer size for I/O operations (8KB).
    /// </summary>
    public const int DefaultBufferSize = 8192;

    /// <summary>
    /// Large buffer size for high-throughput operations (64KB).
    /// </summary>
    public const int LargeBufferSize = 65536;

    #endregion

    #region UI dimensions

    /// <summary>
    /// Default width for fallback UI panels.
    /// </summary>
    public const double FallbackPanelWidth = 300;

    /// <summary>
    /// Default height for fallback UI panels.
    /// </summary>
    public const double FallbackPanelHeight = 200;

    /// <summary>
    /// Default spacing between UI elements in pixels.
    /// </summary>
    public const double DefaultSpacing = 8;

    #endregion

    #region File size limits

    /// <summary>
    /// Maximum allowed size for configuration files (1 MB).
    /// </summary>
    public const long MaxConfigFileSizeBytes = 1048576;

    /// <summary>
    /// Maximum allowed size for downloaded files (100 MB).
    /// </summary>
    public const long MaxDownloadFileSizeBytes = 104857600;

    #endregion

    #region Download estimates

    /// <summary>
    /// Estimated download size for ViVeTool ZIP archive (~3 MB).
    /// Used for progress reporting during downloads.
    /// </summary>
    public const long EstimatedViveToolDownloadBytes = 3145728;

    #endregion

    #region Version constraints

    /// <summary>
    /// Minimum required version of Universal Device Toolkit.
    /// </summary>
    public const string MinLLTVersion = "3.6.1";

    #endregion

    #region Retry configuration

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// </summary>
    public const int MaxRetryAttempts = 3;

    /// <summary>
    /// Delay in milliseconds between retry attempts.
    /// </summary>
    public const int RetryDelayMs = 1000;

    #endregion
}
