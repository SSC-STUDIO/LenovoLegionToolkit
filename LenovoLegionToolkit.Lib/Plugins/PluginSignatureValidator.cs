using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin signature validation result
/// </summary>
public enum PluginSignatureStatus
{
    Valid,
    Invalid,
    NotSigned,
    Expired,
    Untrusted,
    ValidationError
}

/// <summary>
/// Plugin signature validation result details
/// </summary>
public class PluginSignatureResult
{
    public PluginSignatureStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public X509Certificate2? Certificate { get; set; }
    public string? Issuer { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public bool IsValid => Status == PluginSignatureStatus.Valid;

    public PluginSignatureResult(PluginSignatureStatus status, string? errorMessage = null)
    {
        Status = status;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Plugin signature validator interface
/// </summary>
public interface IPluginSignatureValidator
{
    /// <summary>
    /// Validate the Authenticode signature of a plugin DLL
    /// </summary>
    Task<PluginSignatureResult> ValidateAsync(string dllPath);
}

/// <summary>
/// Plugin signature validator implementation
/// Validates Authenticode signatures on plugin DLLs to prevent malicious code execution
/// </summary>
public class PluginSignatureValidator : IPluginSignatureValidator
{
    private readonly PluginSignatureSettings _settings;

    public PluginSignatureValidator(PluginSignatureSettings? settings = null)
    {
        _settings = settings ?? new PluginSignatureSettings();
    }

    /// <summary>
    /// Validate the Authenticode signature of a plugin DLL
    /// </summary>
    public async Task<PluginSignatureResult> ValidateAsync(string dllPath)
    {
        try
        {
            // Skip validation if disabled (development mode only)
            if (_settings.ValidationMode == PluginSignatureValidationMode.DisableValidation)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin signature validation disabled. Skipping validation for {dllPath}");

                return new PluginSignatureResult(PluginSignatureStatus.Valid, "Validation disabled");
            }

            // Check if file exists
            if (!File.Exists(dllPath))
            {
                return new PluginSignatureResult(PluginSignatureStatus.ValidationError,
                    $"Plugin file not found: {dllPath}");
            }

            // Try to extract the Authenticode signature certificate
            X509Certificate2? certificate = null;
            try
            {
#pragma warning disable SYSLIB0057 // Suppress obsolete warning - temporary workaround
                // Note: X509Certificate2.CreateFromSignedFile is obsolete in .NET 10
                // This will be replaced with X509CertificateLoader in future versions
                var cert = X509Certificate.CreateFromSignedFile(dllPath);
                certificate = new X509Certificate2(cert);
#pragma warning restore SYSLIB0057
            }
            catch (Exception ex)
            {
                // File is not signed
                if (_settings.ValidationMode == PluginSignatureValidationMode.AllowUnsigned)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Plugin {dllPath} is not signed. Allowing unsigned plugins (development mode).");

                    return new PluginSignatureResult(PluginSignatureStatus.NotSigned,
                        "Plugin is not signed. Allowed per policy.");
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {dllPath} is not signed: {ex.Message}", ex);

                return new PluginSignatureResult(PluginSignatureStatus.NotSigned,
                    "Plugin is not signed. Signature required per policy.");
            }

            // Validate the certificate
            var validationResult = await ValidateCertificateAsync(certificate, dllPath);

            if (Log.Instance.IsTraceEnabled)
            {
                if (validationResult.IsValid)
                    Log.Instance.Trace($"Plugin signature validation passed for {dllPath}. Issuer: {validationResult.Issuer}");
                else
                    Log.Instance.Trace($"Plugin signature validation failed for {dllPath}. Status: {validationResult.Status}, Error: {validationResult.ErrorMessage}");
            }

            return validationResult;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error validating plugin signature for {dllPath}: {ex.Message}", ex);

            return new PluginSignatureResult(PluginSignatureStatus.ValidationError,
                $"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate the certificate chain, expiration, and trust
    /// </summary>
    private async Task<PluginSignatureResult> ValidateCertificateAsync(X509Certificate2 certificate, string dllPath)
    {
        try
        {
            // Check certificate expiration
            var expirationDate = certificate.NotAfter;
            if (expirationDate < DateTime.UtcNow)
            {
                return new PluginSignatureResult(PluginSignatureStatus.Expired,
                    $"Certificate expired on {expirationDate:O}")
                {
                    Certificate = certificate,
                    Issuer = certificate.Issuer,
                    ExpirationDate = expirationDate
                };
            }

            // Check if certificate is valid (not before current time)
            if (certificate.NotBefore > DateTime.UtcNow)
            {
                return new PluginSignatureResult(PluginSignatureStatus.Invalid,
                    $"Certificate not valid until {certificate.NotBefore:O}")
                {
                    Certificate = certificate,
                    Issuer = certificate.Issuer,
                    ExpirationDate = expirationDate
                };
            }

            // Validate certificate chain and trust
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationTime = DateTime.UtcNow;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            var chainIsValid = chain.Build(certificate);

            if (!chainIsValid)
            {
                var chainErrors = new global::System.Collections.Generic.List<string>();
                foreach (var chainStatus in chain.ChainStatus)
                {
                    chainErrors.Add($"{chainStatus.Status}: {chainStatus.StatusInformation}");
                }

                var errorMessage = $"Certificate chain validation failed: {string.Join("; ", chainErrors)}";

                // Check if the error is due to untrusted root
                var hasUntrustedRoot = chain.ChainStatus.Any(s => s.Status == X509ChainStatusFlags.UntrustedRoot);

                if (hasUntrustedRoot && _settings.AllowTestCertificates)
                {
                    // Allow test/self-signed certificates in development mode
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Plugin {dllPath} has untrusted root certificate but test certificates are allowed.");

                    return new PluginSignatureResult(PluginSignatureStatus.Valid,
                        "Certificate chain validation passed (test certificate allowed)")
                    {
                        Certificate = certificate,
                        Issuer = certificate.Issuer,
                        ExpirationDate = expirationDate
                    };
                }

                return new PluginSignatureResult(PluginSignatureStatus.Untrusted, errorMessage)
                {
                    Certificate = certificate,
                    Issuer = certificate.Issuer,
                    ExpirationDate = expirationDate
                };
            }

            // Certificate is valid and trusted
            return new PluginSignatureResult(PluginSignatureStatus.Valid)
            {
                Certificate = certificate,
                Issuer = certificate.Issuer,
                ExpirationDate = expirationDate
            };
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error validating certificate for {dllPath}: {ex.Message}", ex);

            return new PluginSignatureResult(PluginSignatureStatus.ValidationError,
                $"Certificate validation error: {ex.Message}");
        }
    }
}