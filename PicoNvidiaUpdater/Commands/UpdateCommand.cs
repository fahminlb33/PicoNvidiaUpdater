using Humanizer;
using Microsoft.Extensions.Logging;
using PicoNvidiaUpdater.Infrastructure.Helpers;
using PicoNvidiaUpdater.Infrastructure.Schemas;
using PicoNvidiaUpdater.Infrastructure.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;

namespace PicoNvidiaUpdater.Commands;

internal class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--interactive")]
        public bool Interactive { get; set; }

        [CommandOption("-q|--quiet")]
        public bool Quiet { get; set; }

        [CommandOption("-c|--check")]
        public bool Check { get; set; }

        [CommandOption("-m|--minimal")]
        [DefaultValue(true)]
        public bool Minimal { get; set; }

        [CommandOption("-t|--driver-type")]
        [DefaultValue(NvidiaDriverType.GameReady)]
        public NvidiaDriverType DriverType { get; set; }

        [CommandOption("-o|--output-path")]
        public string? OutputPath { get; set; }

        [CommandOption("-d|--download")]
        public bool DownloadOnly { get; set; }

        [CommandOption("--override-dekstop")]
        public bool OverrideDesktop { get; set; }

        [CommandOption("--override-notebook")]
        public bool OverrideNotebook { get; set; }
    }

    private readonly ILogger _logger;
    private readonly INvidiaWebService _api;
    private readonly INvidiaGpuService _gpu;
    private readonly IDownloadService _downloader;
    private readonly IExtractorService _extractor;
    private readonly IInstallerService _installer;

    public UpdateCommand(ILogger<UpdateCommand> logger, INvidiaWebService api, INvidiaGpuService gpu, IDownloadService downloader, IExtractorService extractor, IInstallerService installer)
    {
        _logger = logger;
        _api = api;
        _gpu = gpu;
        _downloader = downloader;
        _extractor = extractor;
        _installer = installer;
    }

    private void InteractiveInput(ref Settings settings)
    {
        // get override notebook
        var overrideNotebook = AnsiConsole.Prompt(
         new SelectionPrompt<string>()
             .Title("Do you want to override [green]e-GPU detection[/]?")
             .PageSize(10)
             .AddChoices(["No", "Desktop", "Notebook"]));

        switch (overrideNotebook)
        {
            case "No":
                break;
            case "Desktop":
                settings.OverrideDesktop = true;
                break;
            case "Notebook":
                settings.OverrideNotebook = true;
                break;
            default:
                AnsiConsole.MarkupLine("Unknown input for e-GPU detection, defaults to No");
                break;
        }

        // driver type
        var driverTypePrompt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What's your preferred [green]driver type[/]?")
                .PageSize(10)
                .AddChoices(["Game Ready", "Studio Ready"]));
        settings.DriverType = driverTypePrompt == "Game Ready" ? NvidiaDriverType.GameReady : NvidiaDriverType.StudioReady;

        // set minimal install
        settings.Minimal = AnsiConsole.Confirm("Do you want to install the [green]driver only[/]?");

        // set silent install
        settings.Quiet = AnsiConsole.Confirm("Do you want to install [green]silently[/]?");
    }

    private int GetChasisOverride(Settings settings)
    {
        if (settings.OverrideDesktop)
        {
            return NvidiaWebService.METADATA_TYPE_ID_DESKTOP;
        }
        else if (settings.OverrideNotebook)
        {
            return NvidiaWebService.METADATA_TYPE_ID_NOTEBOOK;
        }

        return NvidiaWebService.METADATA_TYPE_ID_DEFAULT;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (settings.OverrideDesktop && settings.OverrideNotebook)
        {
            return ValidationResult.Error("The --override-desktop and --override-notebook cannot be specified at the same time");
        }

        if (string.IsNullOrWhiteSpace(settings.OutputPath) && settings.DownloadOnly)
        {
            return ValidationResult.Error("The --output-path are required when --download are specified");
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputPath) && !settings.DownloadOnly)
        {
            AnsiConsole.MarkupLine("Specifying --output-path when --download is not specified will delete the installer file after installation");
        }

        return ValidationResult.Success();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // --- print welcome
        ConsoleMessages.Welcome();
        
        // check if we need to run in interactive mode
        if (settings.Interactive)
        {
            _logger.LogInformation("Starting interactive mode...");
            InteractiveInput(ref settings);
        }

        // --- print system information
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("System Information") { Justification = Justify.Left });

        // get available GPU and OS from Nvidia
        var gpus = await _api.GetGpus();
        var oses = await _api.GetOperatingSystems();

        // determine this machine driver, OS, and GPU
        var isDch = _gpu.IsDCHDriver();
        var os = _gpu.DetermineOS(oses);
        var (gpu, driverVersion) = _gpu.DetermineGPU(gpus, GetChasisOverride(settings));

        // print driver info
        var table = new Table { Border = TableBorder.None };
        table.HideHeaders();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("OS", os.Name);
        table.AddRow("GPU", gpu.Name);
        table.AddRow("Driver version", driverVersion);
        table.AddRow("Driver is DCH?", isDch.ToString());

        AnsiConsole.Write(table);
        _logger.LogInformation("System information: {OS}, {GPU}, {DriverVersion}, {IsDCH}", os.Name, gpu.Name, driverVersion, isDch);

        // --- print driver updates
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("Driver Updates") { Justification = Justify.Left });

        // check for drivers
        var drivers = await _api.GetDrivers(gpu, os, isDch, settings.DriverType);
        _logger.LogInformation("Found {DriverCount} drivers", drivers.Ids.Length);

        var currentDriver = drivers.Ids.FirstOrDefault();
        if (drivers.Success && _gpu.IsSystemSupportsDCH() && !isDch)
        {
            // this system supports DCH drivers but the installed drivers are not DCH
            // check if we can install DCH
            drivers = await _api.GetDrivers(gpu, os, true, settings.DriverType);
            if (drivers.Success)
            {
                currentDriver = drivers.Ids.First();
                AnsiConsole.MarkupLine("[yellow]Your system supports DCH drivers but the installed drivers are not DCH.[/]");
                AnsiConsole.MarkupLine("[yellow]This update will install DCH drivers.[/]");
                _logger.LogWarning("This PC supports DCH and will install DCH driver");
            }
            else
            {
                _logger.LogWarning("This PC supports DCH but no DCH drivers are found");
            }
        }

        // check if there is an update
        if (currentDriver == null || float.Parse(currentDriver.DownloadInfo.Version) <= float.Parse(driverVersion))
        {
            _logger.LogWarning("No driver update are available");
            AnsiConsole.WriteLine("No updates available");

            return 0;
        }

        // is this check mode?
        if (settings.Check)
        {
            return -1;
        }

        // print details
        _logger.LogDebug("Selected driver {Data}", JsonSerializer.Serialize(currentDriver.DownloadInfo));
        var releaseDelta = DateTime.Now - currentDriver.DownloadInfo.ReleaseDateTime;
        var releaseAgo = DateTime.Now.Subtract(releaseDelta).Humanize();

        AnsiConsole.MarkupLine("Update [blue]available![/]\n");
        AnsiConsole.WriteLine($"Released {releaseAgo}");
        AnsiConsole.WriteLine($"Driver version {currentDriver.DownloadInfo.Version} -> {driverVersion}");
        AnsiConsole.WriteLine($"Download size: {currentDriver.DownloadInfo.DownloadUrlFileSize}");
        AnsiConsole.WriteLine($"Download URL:\n{currentDriver.DownloadInfo.DownloadUrl}");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("Release Notes") { Justification = Justify.Right });
        AnsiConsole.WriteLine(currentDriver.DownloadInfo.DetailsUrl.ToString());
        AnsiConsole.WriteLine(ConsoleMessages.ParseHtml(currentDriver.DownloadInfo.ReleaseNotes));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // --- install confirmation
        if (settings.Interactive && !AnsiConsole.Confirm("Do you want to continue with [green]driver update[/]?"))
        {
            _logger.LogInformation("Update cancelled by user action");
            return -2;
        }

        // create target directory
        var fileName = Path.GetFileName(currentDriver.DownloadInfo.DownloadUrl.ToString());
        var saveDirectory = 
            string.IsNullOrWhiteSpace(settings.OutputPath) ?
            Path.Combine(Path.GetTempPath(), "piconvidiaupdater") :
            settings.OutputPath;
        var setupFileName = Path.Combine(saveDirectory, fileName);

        Directory.CreateDirectory(saveDirectory);

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("Download to: {0}", saveDirectory);
        AnsiConsole.WriteLine();

        // start process
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                // task 1 - download
                _logger.LogInformation("Starting DOWNLOAD task...");
                var downloadBar = ctx.AddTask("Downloading");
                await _downloader.DownloadFile(currentDriver.DownloadInfo.DownloadUrl.ToString(), setupFileName, x =>
                {
                    downloadBar.Value = x;
                });

                // task 2 - extract
                if (settings.Minimal)
                {
                    _logger.LogInformation("Starting EXTRACT task...");
                    var extractBar = ctx.AddTask("Extracting");

                    // ensure 7z is available
                    await _extractor.EnsureSevenZipExecutable();

                    // extract installer
                    await _extractor.Extract(setupFileName, saveDirectory, x =>
                    {
                        extractBar.Value = x;
                    });
                }

                // task 3 - install
                _logger.LogInformation("Starting INSTALL task...");
                var installBar = ctx.AddTask("Installing");
                installBar.IsIndeterminate = true;

                // if the current config are minimal, edit config file
                if (settings.Minimal)
                {
                    await _installer.Configure(saveDirectory);
                }

                // launch installer
                if (settings.DownloadOnly)
                {
                    await _installer.Install(setupFileName, settings.Minimal, settings.Quiet);
                }

                installBar.IsIndeterminate = false;
                installBar.Value = 100;

                // task 4 - clean up
                _logger.LogInformation("Starting CLEANUP task...");
                var cleanupBar = ctx.AddTask("Clean-up");
                cleanupBar.IsIndeterminate = true;
                
                // delete installer if this is not download only mode
                if (!settings.DownloadOnly)
                {
                    Directory.Delete(saveDirectory, true);
                }
                
                cleanupBar.IsIndeterminate = false;
                cleanupBar.Value = 100;
            });

        _logger.LogInformation("All task completed!");
        return 0;
    }
}
