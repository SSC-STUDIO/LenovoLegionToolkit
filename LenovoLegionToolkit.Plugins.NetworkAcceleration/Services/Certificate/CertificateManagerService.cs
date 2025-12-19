using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Certificate;

/// <summary>
/// Certificate manager service implementation
/// </summary>
public class CertificateManagerService : ICertificateManagerService
{
    private const string CertificateName = "LenovoLegionToolkit Network Acceleration Root CA";
    private static readonly string CertificatePath = Path.Combine(
        Folders.AppData,
        "NetworkAcceleration",
        "root_certificate.pfx");

    public async Task<bool> SetupRootCertificateAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Setting up root certificate...");

            // Check if certificate already exists
            if (IsCertificateInstalled())
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Root certificate already installed.");
                return true;
            }

            // Generate or load certificate
            X509Certificate2? certificate = null;

            if (File.Exists(CertificatePath))
            {
                try
                {
                    // Try to load existing certificate
                    certificate = new X509Certificate2(CertificatePath, "", X509KeyStorageFlags.Exportable);
                }
                catch
                {
                    // Certificate file exists but is invalid, regenerate
                }
            }

            if (certificate == null)
            {
                // Generate new self-signed root certificate
                certificate = await GenerateRootCertificateAsync();
                if (certificate == null)
                {
                    return false;
                }

                // Save certificate for future use
                var directory = Path.GetDirectoryName(CertificatePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var pfxBytes = certificate.Export(X509ContentType.Pkcs12, "");
                await File.WriteAllBytesAsync(CertificatePath, pfxBytes);
            }

            // Install certificate to LocalMachine\Root store
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            try
            {
                // Check if already in store
                var existing = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    CertificateName,
                    false);

                if (existing.Count == 0)
                {
                    store.Add(certificate);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Root certificate installed to LocalMachine\\Root store.");
                }
            }
            finally
            {
                store.Close();
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Root certificate setup completed.");

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Access denied when installing certificate. Administrator privileges required.");
            return false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error setting up root certificate: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<X509Certificate2?> GenerateRootCertificateAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var rsa = RSA.Create(2048);
                var request = new CertificateRequest(
                    $"CN={CertificateName}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Set certificate as CA
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                // Set key usage
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                        false));

                // Set subject key identifier
                var subjectKeyIdentifier = new X509SubjectKeyIdentifierExtension(
                    request.PublicKey,
                    X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                    false);
                request.CertificateExtensions.Add(subjectKeyIdentifier);

                // Create self-signed certificate (valid for 10 years)
                var certificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(10));

                return new X509Certificate2(certificate.Export(X509ContentType.Pkcs12), "", X509KeyStorageFlags.Exportable);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error generating root certificate: {ex.Message}", ex);
                return null;
            }
        });
    }

    public async Task<bool> DeleteRootCertificateAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Deleting root certificate...");

            // Remove from certificate store
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            try
            {
                var certificates = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    CertificateName,
                    false);

                foreach (var cert in certificates)
                {
                    store.Remove(cert);
                }
            }
            finally
            {
                store.Close();
            }

            // Delete certificate file
            if (File.Exists(CertificatePath))
            {
                File.Delete(CertificatePath);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Root certificate deleted.");

            return await Task.FromResult(true);
        }
        catch (UnauthorizedAccessException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Access denied when deleting certificate. Administrator privileges required.");
            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error deleting root certificate: {ex.Message}", ex);
            return await Task.FromResult(false);
        }
    }

    public async Task<bool> TrustRootCertificateAsync()
    {
        // Certificate is automatically trusted when installed to LocalMachine\Root store
        // This method is kept for compatibility but the trust happens during SetupRootCertificateAsync
        var result = IsCertificateInstalled();
        await Task.CompletedTask;
        return result;
    }

    public bool IsCertificateInstalled()
    {
        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                CertificateName,
                false);

            return certificates.Count > 0;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error checking certificate: {ex.Message}", ex);
            return false;
        }
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

