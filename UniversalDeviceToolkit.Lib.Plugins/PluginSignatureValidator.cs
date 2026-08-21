using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Plugins.Resources;

namespace UniversalDeviceToolkit.Lib.Plugins;

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

    /// <summary>
    /// Indicates whether the plugin should be allowed to load
    /// This is true for Valid status, or NotSigned when AllowUnsigned policy is in effect
    /// </summary>
    public bool IsValid => Status == PluginSignatureStatus.Valid ||
                          (Status == PluginSignatureStatus.NotSigned && IsAllowedByPolicy);

    /// <summary>
    /// Indicates if the result is allowed by policy (e.g., AllowUnsigned mode)
    /// </summary>
    public bool IsAllowedByPolicy { get; set; }

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
        _settings = PluginSignatureSettings.RelaxedModesAllowed
            ? settings ?? new PluginSignatureSettings()
            : PluginSignatureSettings.Production;
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

                return new PluginSignatureResult(PluginSignatureStatus.Valid, Resource.Plugin_Error_Signature_Disabled);
            }

            // Check if file exists
            if (!File.Exists(dllPath))
            {
                return new PluginSignatureResult(PluginSignatureStatus.ValidationError,
                    string.Format(Resource.Plugin_Error_Signature_FileNotFound, dllPath));
            }

            if (TrustedPluginPackageStore.IsTrustedFile(dllPath))
            {
                return new PluginSignatureResult(
                    PluginSignatureStatus.Valid,
                    "Authorized by the exact committed repository package trust record.");
            }

            if (!OperatingSystem.IsWindows())
            {
                if (_settings.ValidationMode == PluginSignatureValidationMode.AllowUnsigned)
                {
                    return new PluginSignatureResult(
                        PluginSignatureStatus.NotSigned,
                        Resource.Plugin_Error_Signature_NotSigned_AllowUnsigned)
                    {
                        IsAllowedByPolicy = true,
                    };
                }

                return new PluginSignatureResult(
                    PluginSignatureStatus.NotSigned,
                    Resource.Plugin_Error_Signature_NotSigned_Required);
            }

            // Integrity first: WinVerifyTrust checks the Authenticode digest over file bytes.
            // Extracting a cert blob alone is insufficient (tampered PE can still carry a cert).
            var authenticodeOk = AuthenticodeVerifier.TryVerifyFile(dllPath, out var trustStatus);

            // Try to extract the Authenticode signature certificate (for chain/expiry details)
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
                        Resource.Plugin_Error_Signature_NotSigned_AllowUnsigned)
                    { IsAllowedByPolicy = true };
                }

                Log.Instance.Warning($"Plugin {dllPath} is not signed: {ex.Message}", ex);

                return new PluginSignatureResult(PluginSignatureStatus.NotSigned,
                    Resource.Plugin_Error_Signature_NotSigned_Required);
            }

            if (!authenticodeOk)
            {
                // A certificate with an invalid digest is still invalid, including
                // AllowUnsigned. That mode only permits files that are not signed.
                Log.Instance.Warning($"Plugin {dllPath} Authenticode integrity check failed. WinVerifyTrust=0x{trustStatus:X8}");
                return new PluginSignatureResult(PluginSignatureStatus.Invalid,
                    string.Format(Resource.Plugin_Error_Signature_ValidationFailed, $"WinVerifyTrust=0x{trustStatus:X8}"));
            }

            // Validate the certificate
            var validationResult = await ValidateCertificateAsync(certificate, dllPath);

            if (validationResult.IsValid)
                Log.Instance.Trace($"Plugin signature validation passed for {dllPath}. Issuer: {validationResult.Issuer}");
            else
                Log.Instance.Warning($"Plugin signature validation failed for {dllPath}. Status: {validationResult.Status}, Error: {validationResult.ErrorMessage}");

            return validationResult;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error validating plugin signature for {dllPath}: {ex.Message}", ex);

            return new PluginSignatureResult(PluginSignatureStatus.ValidationError,
                string.Format(Resource.Plugin_Error_Signature_ValidationFailed, ex.Message));
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
                    string.Format(Resource.Plugin_Error_Signature_Expired, expirationDate.ToString("O")))
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
                    string.Format(Resource.Plugin_Error_Signature_NotYetValid, certificate.NotBefore.ToString("O")))
                {
                    Certificate = certificate,
                    Issuer = certificate.Issuer,
                    ExpirationDate = expirationDate
                };
            }

            // Validate certificate chain and trust
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = _settings.CheckRevocationStatus ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
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

                var errorMessage = string.Format(
                    Resource.Plugin_Error_Signature_ChainFailed,
                    string.Join("; ", chainErrors));

                // Check if the error is due to untrusted root
                var hasUntrustedRoot = chain.ChainStatus.Any(s => s.Status == X509ChainStatusFlags.UntrustedRoot);

                if (hasUntrustedRoot && _settings.AllowTestCertificates)
                {
                    // Allow test/self-signed certificates in development mode
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Plugin {dllPath} has untrusted root certificate but test certificates are allowed.");

                    return new PluginSignatureResult(PluginSignatureStatus.Valid,
                        Resource.Plugin_Error_Signature_TestCertificate)
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
            Log.Instance.Warning($"Error validating certificate for {dllPath}: {ex.Message}", ex);

            return new PluginSignatureResult(PluginSignatureStatus.ValidationError,
                string.Format(Resource.Plugin_Error_Signature_CertificateError, ex.Message));
        }
    }
}
