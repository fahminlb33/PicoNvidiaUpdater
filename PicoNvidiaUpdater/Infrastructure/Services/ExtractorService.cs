using Microsoft.Extensions.Logging;
using PicoNvidiaUpdater.Infrastructure.Helpers;
using System.Diagnostics;

namespace PicoNvidiaUpdater.Infrastructure.Services;

internal interface IExtractorService
{
    Task EnsureSevenZipExecutable(CancellationToken ct = default);
    Task Extract(string installerPath, string outputPath, Action<double> reportDelegate, CancellationToken ct = default);
}

internal class ExtractorService : IExtractorService
{
    private const string SEVENZIP_DOWNLOAD_URI = @"https://sourceforge.net/projects/sevenzip/files/7-Zip/23.01/7zr.exe/download";
    private static readonly string[] EXTRACT_FILES = ["Display.Driver", "NVI2", "EULA.txt", "license.txt", "ListDevices.txt", "setup.cfg", "setup.exe"];

    private readonly ILogger _logger;
    private readonly IDownloadService _downloader;

    public ExtractorService(ILogger<ExtractorService> logger, IDownloadService downloader)
    {
        _logger = logger;
        _downloader = downloader;
    }

    private static string GetSevenZipPath()
    {
        return Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "7zr.exe");
    }

    public async Task EnsureSevenZipExecutable(CancellationToken ct = default)
    {
        // check if 7z is available
        var downloadPath = GetSevenZipPath();
        if (File.Exists(downloadPath))
        {
            _logger.LogInformation("7zr.exe is avilable at {Path}", downloadPath);
            return;
        }

        // download 7zr.exe
        _logger.LogInformation("7zr.exe is not available at {Path}, downloading from {URL}", downloadPath, SEVENZIP_DOWNLOAD_URI);
        await _downloader.DownloadFile(SEVENZIP_DOWNLOAD_URI, downloadPath, x => { }, ct);

        _logger.LogInformation("Downloaded 7zr.exe at {Path}", downloadPath);
    }

    public async Task Extract(string installerPath, string outputPath, Action<double> reportDelegate, CancellationToken ct = default)
    {
        // func to handle data from 7zr.exe
        void handleOutput(object sender, DataReceivedEventArgs e)
        {
            _logger.LogDebug("Received output from 7zr {Data}", e.Data);
            var percentage = RegexPatterns.Percentage().Match(e.Data ?? "");
            if (percentage.Success)
            {
                reportDelegate(double.Parse(percentage.Value[..^1]));
            }
        }

        // start 7zr process
        _logger.LogInformation("Starting 7zr.exe process to extract {InstallerPath} to {ExtractPath}", installerPath, outputPath);
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = GetSevenZipPath(),
            WorkingDirectory = Path.GetDirectoryName(installerPath),
            Arguments = $"x -o\"{outputPath}\" -y -bsp1 {installerPath} -- {string.Join(" ", EXTRACT_FILES)}",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        // check if process is started and not exited yet
        if (process == null || process.HasExited)
        {
            _logger.LogDebug("Failed to extract the installer");
            reportDelegate(100);
            return;
        }

        // redirect outputs
        process.OutputDataReceived += new DataReceivedEventHandler(handleOutput);
        process.ErrorDataReceived += new DataReceivedEventHandler(handleOutput);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // wait for exit
        await process.WaitForExitAsync(ct);
        _logger.LogInformation("Extraction completed");

        reportDelegate(100);
    }

}
