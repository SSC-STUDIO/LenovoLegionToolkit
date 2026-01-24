using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace LenovoLegionToolkit.Lib;

public class HttpClientFactory
{
    private Uri? _url;
    private string? _username;
    private string? _password;
    private bool _allowAllCerts;

    public HttpClientHandler CreateHandler()
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
                // SECURITY: Only bypass certificate validation in development environment
                // or when explicitly allowed for proxy scenarios with self-signed certificates
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Allow certificate bypass only in development environment
                    if (Debugger.IsAttached)
                        return true;

                    // For production, only allow certificate validation bypass when explicitly configured
                    // This is primarily for proxy scenarios with self-signed certificates
                    // Never bypass errors in production environments without explicit configuration
                    return false;
                };
            }
        }

        return handler;
    }

    public HttpClient Create() => new(CreateHandler(), true);

    public void SetProxy(Uri? url, string? username, string? password, bool allowAllCerts)
    {
        _url = url;
        _username = username;
        _password = password;
        _allowAllCerts = allowAllCerts;
    }
}