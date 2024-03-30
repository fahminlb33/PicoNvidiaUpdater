using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.Extensions.Logging;
using PicoNvidiaUpdater.Infrastructure.Helpers;
using PicoNvidiaUpdater.Infrastructure.Schemas;

namespace PicoNvidiaUpdater.Infrastructure.Services;

public interface INvidiaWebService
{
    Task<NvidiaGpu[]> GetGpus(CancellationToken ct = default);
    Task<NvidiaOS[]> GetOperatingSystems(CancellationToken ct = default);
    Task<NvidiaDriverResponse> GetDrivers(NvidiaGpu gpu, NvidiaOS os, bool isDchDriver, NvidiaDriverType driverType, CancellationToken ct = default);
}

public class NvidiaWebService : INvidiaWebService
{
    public const int METADATA_TYPE_ID_OS = 4;
    public const int METADATA_TYPE_ID_DEFAULT = 0;
    public const int METADATA_TYPE_ID_NOTEBOOK = 2;
    public const int METADATA_TYPE_ID_DESKTOP = 3;

    private static readonly HttpClient _client = new();
    private readonly ILogger _logger;

    public NvidiaWebService(ILogger<NvidiaWebService> logger)
    {
        _logger = logger;
    }

    public async Task<NvidiaDriverResponse> GetDrivers(NvidiaGpu gpu, NvidiaOS os, bool isDchDriver, NvidiaDriverType driverType, CancellationToken ct = default)
    {
        // build URI
        var query = new Dictionary<string, string>
        {
            { "func", "DriverManualLookup" },
            { "pfid", $"{gpu.ID}" },
            { "osID", $"{os.ID}" },
            { "dch", $"{(isDchDriver ? 1: 0)}" },
            { "upCRD", $"{(int)driverType}" },
        };

        var uri = new UriBuilder(@"https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php")
        {
            Query = string.Join('&', query.Select(q => $"{q.Key}={q.Value}"))
        };
        
        // create HTTP request
        _logger.LogDebug("Creating HTTP request {URL}", uri);
        var res = await _client.GetAsync(uri.ToString(), ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        // deserialize JSON
        var body = await res.Content.ReadFromJsonAsync<NvidiaDriverResponse>(Converter.Settings, ct).ConfigureAwait(false);
        _logger.LogDebug("Response from API HTTP {Code} {URI}", res.StatusCode, uri);

        return body!;
    }

    public async Task<NvidiaOS[]> GetOperatingSystems(CancellationToken ct = default)
    {
        var nodes = await GetMetadataString(METADATA_TYPE_ID_OS, ct).ConfigureAwait(false);
        return nodes.Select(NvidiaOS.Parse).ToArray();
    }

    public async Task<NvidiaGpu[]> GetGpus(CancellationToken ct = default)
    {
        // get metadata
        var nodesDesktop = await GetMetadataString(METADATA_TYPE_ID_DESKTOP, ct).ConfigureAwait(false);
        var nodesNotebook = await GetMetadataString(METADATA_TYPE_ID_NOTEBOOK, ct).ConfigureAwait(false);

        // get notebook IDs
        var notebookIds = nodesNotebook
            .Select(x => NvidiaGpu.Parse(x, NvidiaGpuKind.Notebook))
            .Where(x => RegexPatterns.IsNotebook().IsMatch(x.Name))
            .Select(x => x.ID)
            .ToList();

        // reproject data to the correct type
        // this two step process is to deduplicate entries from Desktop and Notebook
        return nodesDesktop.Select(x =>
        {
            var kind = notebookIds.Contains(x.Attributes!["ParentID"]!.InnerText) ? NvidiaGpuKind.Notebook : NvidiaGpuKind.Desktop;
            return NvidiaGpu.Parse(x, kind);
        }).ToArray();
    }

    private async ValueTask<IEnumerable<XmlNode>> GetMetadataString(int typeId, CancellationToken ct = default)
    {
        // build URI
        var uri = $"https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID={typeId}";

        // create HTTP request
        _logger.LogDebug("Creating HTTP request {URL}", uri);
        var res = await _client.GetAsync(uri, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        // get body XML
        var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Response from API HTTP {Code} {URI}", res.StatusCode, uri);
        
        // parse XML
        var doc = new XmlDocument();
        doc.LoadXml(body);

        // return nodes
        return doc!["LookupValueSearch"]!["LookupValues"]!.ChildNodes.Cast<XmlNode>();
    }

}

