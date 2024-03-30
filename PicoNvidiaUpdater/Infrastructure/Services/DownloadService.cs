using Microsoft.Extensions.Logging;

namespace PicoNvidiaUpdater.Infrastructure.Services;

internal interface IDownloadService
{
    Task DownloadFile(string url, string path, Action<double> reportDelegate, CancellationToken ct = default);
}

internal class DownloadService : IDownloadService
{
    public const int BUFFER_SIZE =  16384; // 16K bytes

    private static readonly HttpClient _httpClient = new();
    private readonly ILogger _logger;

    static DownloadService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.79.1");
    }

    public DownloadService(ILogger<DownloadService> logger)
    {
        _logger = logger;
    }

    public async Task DownloadFile(string url, string path, Action<double> reportDelegate, CancellationToken ct = default)
    {
        // perform HTTP request
        _logger.LogDebug("Creating HTTP request {URL}", url);
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // get file length
        var contentLength = response.Content.Headers.ContentLength ?? 1;
        _logger.LogDebug("Got success response, total response size is {ContentLength} bytes from {URL}", contentLength, url);

        // check if file exists
        if (File.Exists(path) && new FileInfo(path).Length == contentLength)
        {
            reportDelegate(100);
            _logger.LogDebug("Using cached installer");
            return;
        }

        // open save file stream
        _logger.LogDebug("Downloading {URL} to {Path}", url, path);
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var file = new FileStream(path, FileMode.Create);

        // to hold statistics and buffer
        int bytesRead;
        long totalBytesRead = 0;
        var buffer = new byte[BUFFER_SIZE];

        // download stream to local
        while ((bytesRead = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) != 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            reportDelegate(totalBytesRead / (double)contentLength * 100);
        }

        _logger.LogInformation("File downloaded, total {TotalBytesRead} bytes from {URL} to {Path}", totalBytesRead, url, path);
    }
}
