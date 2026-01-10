using System;
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
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Only bypass certificate validation when explicitly allowed for proxy scenarios
                    // When allowAllCerts is enabled in proxy scenarios, accept certificates despite errors
                    // to handle self-signed or improperly configured proxy certificates
                    return true;
                };
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