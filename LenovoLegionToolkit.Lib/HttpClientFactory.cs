using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace LenovoLegionToolkit.Lib;

public class HttpClientFactory
{
    private Uri? _url;
    private string? _username;
    private string? _password;
    private bool _allowAllCerts;

    public virtual HttpClientHandler CreateHandler()
    {
        var handler = new HttpClientHandler();

        if (_url is not null)
        {
            handler.UseProxy = true;
            handler.Proxy = new WebProxy(_url)
            {
                UseDefaultCredentials = false,
                BypassProxyOnLocal = false,
            };

            if (_username is not null && _password is not null)
                handler.DefaultProxyCredentials = new NetworkCredential(_username, _password);

            if (_allowAllCerts)
            {
                // SECURITY FIX: Always validate certificates in production
                // Only allow bypass in explicit development builds
                // Never bypass based on debugger attachment status
                #if DEBUG
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // In DEBUG builds, log certificate errors but still validate
                    if (errors != SslPolicyErrors.None)
                    {
                        Debug.WriteLine($"SSL Certificate validation error: {errors}");
                        // Still return false to enforce validation
                    }
                    return errors == SslPolicyErrors.None;
                };
                #else
                // RELEASE builds: Never bypass certificate validation
                // This prevents MITM attacks even if --proxy-allow-all-certs is specified
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Always validate certificates in production
                    // Certificate errors cannot be bypassed
                    return errors == SslPolicyErrors.None;
                };
                #endif
            }
        }

        return handler;
    }

    public virtual HttpClient Create() => new(CreateHandler(), true);

    public void SetProxy(Uri? url, string? username, string? password, bool allowAllCerts)
    {
        _url = url;
        _username = username;
        _password = password;
        // SECURITY: Ignore allowAllCerts in release builds
        // This prevents users from bypassing certificate validation
        #if DEBUG
        _allowAllCerts = allowAllCerts;
        #else
        _allowAllCerts = false;
        #endif
    }
}
