using System;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Certificate;

/// <summary>
/// Certificate manager service interface
/// </summary>
public interface ICertificateManagerService : IDisposable
{
    /// <summary>
    /// Setup root certificate for HTTPS interception
    /// </summary>
    Task<bool> SetupRootCertificateAsync();

    /// <summary>
    /// Delete root certificate
    /// </summary>
    Task<bool> DeleteRootCertificateAsync();

    /// <summary>
    /// Trust root certificate
    /// </summary>
    Task<bool> TrustRootCertificateAsync();

    /// <summary>
    /// Check if root certificate is installed and trusted
    /// </summary>
    bool IsCertificateInstalled();
}

