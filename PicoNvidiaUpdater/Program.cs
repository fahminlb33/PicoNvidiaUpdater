using Serilog.Events;
using Serilog;
using Serilog.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Formatting.Compact;
using Spectre.Console.Cli;
using PicoNvidiaUpdater.Commands;
using PicoNvidiaUpdater.Infrastructure.DI;
using PicoNvidiaUpdater.Infrastructure.Services;

Log.Logger = new LoggerConfiguration()
            // setup logging levels
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)

            // setup enrichers
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()

            // setup sinks
            .WriteTo.File(new CompactJsonFormatter(), @$"logs\{DateTime.Now:yyyyMMdd_HHmm}.json")

            // create logger
            .CreateLogger();

// create registration
var services = new ServiceCollection();

// register services
services.AddLogging();
services.AddSerilog();
services.AddSingleton<INvidiaGpuService, NvidiaGpuService>();
services.AddSingleton<INvidiaWebService, NvidiaWebService>();
services.AddSingleton<IDownloadService, DownloadService>();
services.AddSingleton<IExtractorService, ExtractorService>();
services.AddSingleton<IInstallerService, InstallerService>();

try
{
    Log.Information("Starting host");

    // run host
    var app = new CommandApp<UpdateCommand>(new TypeRegistrar(services));
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
