using System.Diagnostics;

namespace PicoNvidiaUpdater.Infrastructure.Services;

public interface IInstallerService
{
    Task Configure(string rootDir, CancellationToken ct = default);
    Task Install(string rootDir, bool minimal, bool silent, CancellationToken ct = default);
}

public class InstallerService : IInstallerService
{
    private static readonly string[] CONFIG_DELETE = { "${{EulaHtmlFile}}", "${{FunctionalConsentFile}}", "${{PrivacyPolicyFile}}" };

    public async Task Configure(string rootDir, CancellationToken ct = default)
    {
        var originalPath = Path.Combine(rootDir, "setup.cfg");
        var backupPath = Path.Combine(rootDir, "setup.cfg.bak");

        // rename original config
        File.Move(originalPath, backupPath, true);

        // recreate config file, excluding some lines
        await using var writer = new StreamWriter(originalPath);
        await foreach (var line in File.ReadLinesAsync(backupPath, ct))
        {
            // if the line has ignored token, skip
            if (CONFIG_DELETE.Any(line.Contains))
            {
                continue;
            }

            // write to new config
            await writer.WriteLineAsync(line);
        }
    }

    public async Task Install(string installerPath, bool minimal, bool silent, CancellationToken ct = default)
    {
        // create process start info
        var startInfo = new ProcessStartInfo
        {
            FileName = minimal ? Path.Combine(Path.GetDirectoryName(installerPath), "setup.exe") : installerPath,
            WorkingDirectory = Path.GetDirectoryName(installerPath),
            Arguments = silent ? $"/s /noreboot" : "/nosplash",
            UseShellExecute = true
        };

        // if this is silent, hide window
        if (silent)
        {
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        // start process
        var process = Process.Start(startInfo);
        if (process == null)
        {
            return;
        }

        // wait for exit
        await process.WaitForExitAsync(ct);
    }
}