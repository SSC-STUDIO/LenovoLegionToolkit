using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Script;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Statistics;
using NeoSmart.AsyncLock;
using IDnsOptimizationService = LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Dns.IDnsOptimizationService;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.ReverseProxy;

/// <summary>
/// Reverse proxy service implementation
/// </summary>
public class ReverseProxyService : IReverseProxyService
{
    private const ushort DefaultProxyPort = 7777;
    private const string DefaultProxyIp = "127.0.0.1";
    private const int BufferSize = 8192;

    private readonly AsyncLock _stateLock = new();
    private bool _isRunning;
    private TcpListener? _tcpListener;
    private Task? _listenerTask;
    private ushort _proxyPort = DefaultProxyPort;
    private string _proxyIp = DefaultProxyIp;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HttpClient _httpClient;
    private readonly IScriptManagerService? _scriptManagerService;
    private readonly INetworkStatisticsService? _statisticsService;
    private readonly IDnsOptimizationService? _dnsOptimizationService;
    private readonly HashSet<string> _githubDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "github.com",
        "githubusercontent.com",
        "github.io",
        "githubapp.com",
        "githubassets.com",
        "github.dev",
        "githubstatus.com"
    };

    public bool IsRunning
    {
        get
        {
            using (_stateLock.Lock())
                return _isRunning;
        }
    }

    public ushort ProxyPort
    {
        get
        {
            using (_stateLock.Lock())
                return _proxyPort;
        }
        private set
        {
            using (_stateLock.Lock())
                _proxyPort = value;
        }
    }

    public string ProxyIp
    {
        get
        {
            using (_stateLock.Lock())
                return _proxyIp;
        }
        private set
        {
            using (_stateLock.Lock())
                _proxyIp = value;
        }
    }

    public ReverseProxyService(
        IScriptManagerService? scriptManagerService = null,
        INetworkStatisticsService? statisticsService = null,
        IDnsOptimizationService? dnsOptimizationService = null)
    {
        _scriptManagerService = scriptManagerService;
        _statisticsService = statisticsService;
        _dnsOptimizationService = dnsOptimizationService;

        // Create HttpClient with proper configuration
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            UseCookies = false // We'll handle cookies manually
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<bool> StartAsync()
    {
        // Check if already running and acquire lock
        string ipToUse;
        ushort portToUse;
        using (await _stateLock.LockAsync().ConfigureAwait(false))
        {
            if (_isRunning)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Reverse proxy service is already running.");
                return true;
            }
            ipToUse = _proxyIp;
            portToUse = _proxyPort;
        }

        // Perform async operations outside the lock
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting reverse proxy service on {ipToUse}:{portToUse}...");

            // Find an available port
            var availablePort = FindAvailablePort(IPAddress.Parse(ipToUse), portToUse);

            var tcpListener = new TcpListener(IPAddress.Parse(ipToUse), availablePort);
            tcpListener.Start();

            var listenerTask = Task.Run(() => ListenForConnections(_cancellationTokenSource.Token));

            // Update state under lock
            using (await _stateLock.LockAsync().ConfigureAwait(false))
            {
                if (_isRunning)
                {
                    // Another thread started it while we were setting up
                    tcpListener.Stop();
                    return true;
                }
                _proxyPort = availablePort;
                _tcpListener = tcpListener;
                _listenerTask = listenerTask;
                _isRunning = true;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Reverse proxy service started on {ProxyIp}:{ProxyPort}.");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error starting reverse proxy service: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        // Check if running and acquire lock
        TcpListener? tcpListenerToStop = null;
        Task? listenerTaskToWait = null;
        using (await _stateLock.LockAsync().ConfigureAwait(false))
        {
            if (!_isRunning)
            {
                return true;
            }
            tcpListenerToStop = _tcpListener;
            listenerTaskToWait = _listenerTask;
            _isRunning = false;
        }

        // Perform async operations outside the lock
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping reverse proxy service...");

            _cancellationTokenSource.Cancel();

            tcpListenerToStop?.Stop();

            if (listenerTaskToWait != null)
            {
                try
                {
                    await Task.WhenAny(listenerTaskToWait, Task.Delay(5000));
                }
                catch
                {
                    // Ignore cancellation exceptions
                }
            }

            // Clear references under lock
            using (await _stateLock.LockAsync().ConfigureAwait(false))
            {
                _tcpListener = null;
                _listenerTask = null;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Reverse proxy service stopped.");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping reverse proxy service: {ex.Message}", ex);
            return false;
        }
    }

    private async Task ListenForConnections(CancellationToken cancellationToken)
    {
        TcpListener? listener;
        using (await _stateLock.LockAsync().ConfigureAwait(false))
        {
            listener = _tcpListener;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            bool isRunning;
            using (await _stateLock.LockAsync().ConfigureAwait(false))
            {
                isRunning = _isRunning;
                listener = _tcpListener;
            }

            if (!isRunning || listener == null)
                break;

            try
            {
                var client = await listener.AcceptTcpClientAsync();
                if (client != null)
                {
                    // Handle client connection asynchronously
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                }
            }
            catch (ObjectDisposedException)
            {
                // Listener was stopped
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error accepting client connection: {ex.Message}", ex);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        NetworkStream? clientStream = null;
        try
        {
            clientStream = client.GetStream();
            clientStream.ReadTimeout = 30000;
            clientStream.WriteTimeout = 30000;

            // Read the request
            var buffer = new byte[BufferSize];
            var bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead == 0)
            {
                return;
            }

            var requestData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var requestLines = requestData.Split(new[] { "\r\n" }, StringSplitOptions.None);

            if (requestLines.Length == 0)
            {
                return;
            }

            // Parse the first line (method, URL, version)
            var firstLine = requestLines[0];
            var parts = firstLine.Split(' ', 3);
            if (parts.Length < 3)
            {
                return;
            }

            var method = parts[0];
            var requestUri = parts[1];
            var httpVersion = parts[2];

            // Handle CONNECT method for HTTPS
            if (method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                await HandleHttpsConnectAsync(clientStream, requestUri, cancellationToken);
                return;
            }

            // Handle HTTP requests
            await HandleHttpRequestAsync(clientStream, method, requestUri, requestLines, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error handling client: {ex.Message}", ex);
        }
        finally
        {
            try
            {
                clientStream?.Close();
                client?.Close();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private async Task HandleHttpsConnectAsync(NetworkStream clientStream, string requestUri, CancellationToken cancellationToken)
    {
        // Parse host:port from CONNECT request
        var parts = requestUri.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
        {
            var errorResponse = Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\n\r\n");
            await clientStream.WriteAsync(errorResponse, 0, errorResponse.Length, cancellationToken);
            return;
        }

        var host = parts[0];
        IPAddress[] addresses;

        try
        {
            // Use DNS optimization service if available, otherwise fall back to system DNS
            if (_dnsOptimizationService != null && _dnsOptimizationService.IsEnabled)
            {
                var resolvedAddresses = await _dnsOptimizationService.ResolveAsync(host);
                if (resolvedAddresses == null || resolvedAddresses.Length == 0)
                {
                    throw new Exception($"Could not resolve host: {host}");
                }
                addresses = resolvedAddresses;
            }
            else
            {
                // Resolve DNS asynchronously using system DNS
                addresses = await System.Net.Dns.GetHostAddressesAsync(host);
                if (addresses.Length == 0)
                {
                    throw new Exception($"Could not resolve host: {host}");
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error resolving host {host}: {ex.Message}", ex);

            var errorResponse = Encoding.UTF8.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\n");
            await clientStream.WriteAsync(errorResponse, 0, errorResponse.Length, cancellationToken);
            return;
        }

        var targetEndPoint = new IPEndPoint(addresses[0], port);

        // Connect to target server
        using var targetClient = new TcpClient();
        try
        {
            await targetClient.ConnectAsync(targetEndPoint.Address, targetEndPoint.Port);
            var targetStream = targetClient.GetStream();

            // Send 200 Connection Established
            var successResponse = Encoding.UTF8.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
            await clientStream.WriteAsync(successResponse, 0, successResponse.Length, cancellationToken);

            // Tunnel the connection (client -> target = upload, target -> client = download)
            var uploadTask = TunnelStreamAsync(clientStream, targetStream, cancellationToken, trackUpload: true);
            var downloadTask = TunnelStreamAsync(targetStream, clientStream, cancellationToken, trackUpload: false);
            await Task.WhenAny(uploadTask, downloadTask);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error handling HTTPS CONNECT: {ex.Message}", ex);

            var errorResponse = Encoding.UTF8.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\n");
            await clientStream.WriteAsync(errorResponse, 0, errorResponse.Length, cancellationToken);
        }
    }

    private async Task HandleHttpRequestAsync(
        NetworkStream clientStream,
        string method,
        string requestUri,
        string[] requestLines,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse headers
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var bodyStartIndex = 0;

            for (int i = 1; i < requestLines.Length; i++)
            {
                if (string.IsNullOrEmpty(requestLines[i]))
                {
                    bodyStartIndex = i + 1;
                    break;
                }

                var colonIndex = requestLines[i].IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = requestLines[i].Substring(0, colonIndex).Trim();
                    var value = requestLines[i].Substring(colonIndex + 1).Trim();
                    headers[key] = value;
                }
            }

            // Build full URL
            Uri? targetUri;
            if (Uri.TryCreate(requestUri, UriKind.Absolute, out var absoluteUri))
            {
                targetUri = absoluteUri;
            }
            else if (headers.TryGetValue("Host", out var host))
            {
                var scheme = headers.ContainsKey("X-Forwarded-Proto") ? "https" : "http";
                targetUri = new Uri($"{scheme}://{host}{requestUri}");
            }
            else
            {
                var errorResponse = Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\n\r\n");
                await clientStream.WriteAsync(errorResponse, 0, errorResponse.Length, cancellationToken);
                return;
            }

            // Check if this is a GitHub domain and should be accelerated
            var shouldAccelerate = _githubDomains.Any(domain => targetUri.Host.EndsWith(domain, StringComparison.OrdinalIgnoreCase));

            // Create HTTP request
            var httpRequest = new HttpRequestMessage(new HttpMethod(method), targetUri);

            // Copy headers (except Host and Connection)
            foreach (var header in headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Proxy-Connection", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Let HttpClient handle these
                    }

                    if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                    {
                        httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                catch
                {
                    // Skip invalid headers
                }
            }

            // Handle request body
            long requestBodySize = 0;
            if (bodyStartIndex < requestLines.Length && headers.ContainsKey("Content-Length"))
            {
                var bodyLines = requestLines.Skip(bodyStartIndex);
                var bodyContent = string.Join("\r\n", bodyLines);
                if (!string.IsNullOrEmpty(bodyContent))
                {
                    var bodyBytes = Encoding.UTF8.GetBytes(bodyContent);
                    requestBodySize = bodyBytes.Length;
                    httpRequest.Content = new ByteArrayContent(bodyBytes);
                }
            }

            // Track upload statistics
            if (_statisticsService != null && requestBodySize > 0)
            {
                _statisticsService.RecordUpload(requestBodySize);
            }

            // Send request
            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Read response
            var responseBody = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            // Track download statistics (actual bytes read)
            if (_statisticsService != null)
            {
                _statisticsService.RecordDownload(responseBody.Length);
            }

            // Inject scripts if enabled
            if (_scriptManagerService?.IsEnabled == true && 
                response.Content.Headers.ContentType?.MediaType?.Contains("text/html") == true)
            {
                responseBody = await InjectScriptsAsync(responseBody, cancellationToken);
            }

            // Build and send response
            var responseBuilder = new StringBuilder();
            responseBuilder.AppendLine($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}");

            // Copy response headers
            foreach (var header in response.Headers)
            {
                foreach (var value in header.Value)
                {
                    responseBuilder.AppendLine($"{header.Key}: {value}");
                }
            }

            foreach (var header in response.Content.Headers)
            {
                if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    responseBuilder.AppendLine($"Content-Length: {responseBody.Length}");
                    continue;
                }

                foreach (var value in header.Value)
                {
                    responseBuilder.AppendLine($"{header.Key}: {value}");
                }
            }

            responseBuilder.AppendLine();
            var responseHeaderBytes = Encoding.UTF8.GetBytes(responseBuilder.ToString());
            await clientStream.WriteAsync(responseHeaderBytes, 0, responseHeaderBytes.Length, cancellationToken);
            await clientStream.WriteAsync(responseBody, 0, responseBody.Length, cancellationToken);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error handling HTTP request: {ex.Message}", ex);

            var errorResponse = Encoding.UTF8.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\n");
            await clientStream.WriteAsync(errorResponse, 0, errorResponse.Length, cancellationToken);
        }
    }

    private async Task<byte[]> InjectScriptsAsync(byte[] htmlContent, CancellationToken cancellationToken)
    {
        try
        {
            if (_scriptManagerService == null)
                return htmlContent;

            var html = Encoding.UTF8.GetString(htmlContent);
            var scripts = await _scriptManagerService.GetScriptsAsync();

            var scriptTags = new StringBuilder();
            foreach (var script in scripts.Where(s => s.IsEnabled))
            {
                var scriptContent = await _scriptManagerService.LoadScriptContentAsync(script.Id);
                if (!string.IsNullOrEmpty(scriptContent))
                {
                    scriptTags.AppendLine($"<script>{scriptContent}</script>");
                }
            }

            if (scriptTags.Length == 0)
                return htmlContent;

            // Inject before </body> or at the end
            var injectionPoint = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (injectionPoint < 0)
            {
                injectionPoint = html.Length;
            }

            html = html.Insert(injectionPoint, scriptTags.ToString());
            return Encoding.UTF8.GetBytes(html);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error injecting scripts: {ex.Message}", ex);
            return htmlContent;
        }
    }

    private async Task TunnelStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken, bool trackUpload = false)
    {
        var buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            await destination.FlushAsync(cancellationToken);

            // Track statistics for HTTPS tunneled connections
            if (_statisticsService != null && bytesRead > 0)
            {
                if (trackUpload)
                {
                    _statisticsService.RecordUpload(bytesRead);
                }
                else
                {
                    _statisticsService.RecordDownload(bytesRead);
                }
            }
        }
    }

    private static ushort FindAvailablePort(IPAddress ip, ushort startPort)
    {
        for (ushort port = startPort; port < startPort + 100; port++)
        {
            try
            {
                using var listener = new TcpListener(ip, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch
            {
                // Port is in use, try next
            }
        }
        return startPort;
    }

    public void Dispose()
    {
        bool isRunning;
        using (_stateLock.Lock())
        {
            isRunning = _isRunning;
        }

        if (isRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        _cancellationTokenSource?.Dispose();
        _httpClient?.Dispose();
    }
}

