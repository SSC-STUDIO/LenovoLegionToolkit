using System;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin signature validation modes
/// </summary>
public enum PluginSignatureValidationMode
{
    /// <summary>
    /// Require valid signature for all plugins (recommended for production)
    /// </summary>
    RequireSignature,

    /// <summary>
    /// Allow unsigned plugins but log warnings (development mode)
    /// </summary>
    AllowUnsigned,

    /// <summary>
    /// Disable signature validation entirely (only for local development)
    /// </summary>
    DisableValidation
}

/// <summary>
/// Plugin signature validation settings
/// Controls how strictly plugin signatures are validated
/// </summary>
public class PluginSignatureSettings
{
    public const string ValidationModeEnvironmentVariable = "LLT_PLUGIN_SIGNATURE_MODE";

    /// <summary>
    /// Signature validation mode
    /// Default: RequireSignature (most secure)
    /// </summary>
    public PluginSignatureValidationMode ValidationMode { get; set; } = PluginSignatureValidationMode.RequireSignature;

    /// <summary>
    /// Allow test/self-signed certificates
    /// Default: false (only accept certificates from trusted CA)
    /// Set to true for development/testing scenarios
    /// </summary>
    public bool AllowTestCertificates { get; set; } = false;

    /// <summary>
    /// List of trusted certificate issuers (thumbprints)
    /// Plugins signed by these issuers will be automatically trusted
    /// </summary>
    public string[] TrustedIssuers { get; set; } = Array.Empty<string>();

    /// <summary>
    /// List of explicitly allowed unsigned plugin IDs
    /// Use only for trusted local plugins in development
    /// </summary>
    public string[] AllowedUnsignedPlugins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Validate certificate revocation status
    /// Default: true (check if certificate has been revoked)
    /// </summary>
    public bool CheckRevocationStatus { get; set; } = true;

    /// <summary>
    /// Create default production settings (strict validation)
    /// </summary>
    public static PluginSignatureSettings Production => new PluginSignatureSettings
    {
        ValidationMode = PluginSignatureValidationMode.RequireSignature,
        AllowTestCertificates = false,
        CheckRevocationStatus = true
    };

    /// <summary>
    /// Create development settings (relaxed validation)
    /// </summary>
    public static PluginSignatureSettings Development => new PluginSignatureSettings
    {
        ValidationMode = PluginSignatureValidationMode.AllowUnsigned,
        AllowTestCertificates = true,
        CheckRevocationStatus = false
    };

    /// <summary>
    /// Create disabled settings (no validation - only for local testing)
    /// WARNING: Only use in isolated development environments
    /// </summary>
    public static PluginSignatureSettings Disabled => new PluginSignatureSettings
    {
        ValidationMode = PluginSignatureValidationMode.DisableValidation,
        AllowTestCertificates = true,
        CheckRevocationStatus = false
    };

    public static PluginSignatureSettings CreateForCurrentProcess()
    {
        return TryCreateFromEnvironmentValue(
            Environment.GetEnvironmentVariable(ValidationModeEnvironmentVariable),
            out var settings)
            ? settings
            : Production;
    }

    public static bool TryCreateFromEnvironmentValue(string? value, out PluginSignatureSettings settings)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "require":
            case "require-signature":
            case "requiresignature":
            case "production":
                settings = Production;
                return true;
            case "allowunsigned":
            case "allow-unsigned":
            case "development":
                settings = Development;
                return true;
            case "disable":
            case "disabled":
            case "disablevalidation":
            case "disable-validation":
                settings = Disabled;
                return true;
            default:
                settings = Production;
                return false;
        }
    }
}
