using PicoNvidiaUpdater.Infrastructure.Helpers;
using System.Text.Json.Serialization;

namespace PicoNvidiaUpdater.Infrastructure.Schemas;


public partial class NvidiaDriverResponse
{
    [JsonPropertyName("Success")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool Success { get; set; }

    [JsonPropertyName("IDS")]
    public NvidiaDriverRoot[] Ids { get; set; }
}

public partial class NvidiaDriverRoot
{
    [JsonPropertyName("downloadInfo")]
    public NvidiaDriver DownloadInfo { get; set; }
}

public partial class NvidiaDriver
{
    [JsonPropertyName("Success")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool Success { get; set; }

    // ids

    [JsonPropertyName("ID")]
    public string Id { get; set; }

    [JsonPropertyName("DownloadTypeID")]
    [JsonConverter(typeof(ParseStringConverter))]
    public long DownloadTypeId { get; set; }

    [JsonPropertyName("DownloadStatusID")]
    [JsonConverter(typeof(ParseStringConverter))]
    public long DownloadStatusId { get; set; }

    // names

    [JsonPropertyName("Name")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string Name { get; set; }

    [JsonPropertyName("NameLocalized")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string NameLocalized { get; set; }

    [JsonPropertyName("ShortDescription")]
    public string ShortDescription { get; set; }

    [JsonPropertyName("DeviceToProductFamilyName")]
    public string DeviceToProductFamilyName { get; set; }

    // versions

    [JsonPropertyName("Release")]
    public string Release { get; set; }

    [JsonPropertyName("Version")]
    public string Version { get; set; }

    [JsonPropertyName("DisplayVersion")]
    public string DisplayVersion { get; set; }

    [JsonPropertyName("GFE_DisplayVersion")]
    public string GfeDisplayVersion { get; set; }

    [JsonPropertyName("CDKitUSBEmitterDriverVersion")]
    public string CdKitUsbEmitterDriverVersion { get; set; }

    [JsonPropertyName("CDKitGPUDriverVersion")]
    public string CdKitGpuDriverVersion { get; set; }

    [JsonPropertyName("CudaToolkitVersion")]
    public string CudaToolkitVersion { get; set; }

    // flags

    [JsonPropertyName("Is64Bit")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool Is64Bit { get; set; }

    [JsonPropertyName("IsBeta")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsBeta { get; set; }

    [JsonPropertyName("IsWHQL")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsWhql { get; set; }

    [JsonPropertyName("IsRecommended")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsRecommended { get; set; }

    [JsonPropertyName("IsFeaturePreview")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsFeaturePreview { get; set; }

    [JsonPropertyName("IsNewest")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsNewest { get; set; }

    [JsonPropertyName("IsDC")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsDc { get; set; }

    [JsonPropertyName("IsCRD")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsCrd { get; set; }

    [JsonPropertyName("HasNetInst")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool HasNetInst { get; set; }

    [JsonPropertyName("IsArchive")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsArchive { get; set; }

    [JsonPropertyName("IsActive")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsActive { get; set; }

    [JsonPropertyName("IsEmailRequired")]
    [JsonConverter(typeof(ParseBooleanConverter))]
    public bool IsEmailRequired { get; set; }

    // details

    [JsonPropertyName("ReleaseDateTime")]
    [JsonConverter(typeof(ParseNvidiaDateTimeConverter))]
    public DateTime ReleaseDateTime { get; set; }

    [JsonPropertyName("DetailsURL")]
    public Uri DetailsUrl { get; set; }

    [JsonPropertyName("DownloadURL")]
    public Uri DownloadUrl { get; set; }

    [JsonPropertyName("DownloadURLFileSize")]
    public string DownloadUrlFileSize { get; set; }

    [JsonPropertyName("ReleaseNotes")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string ReleaseNotes { get; set; }

    [JsonPropertyName("OtherNotes")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string OtherNotes { get; set; }

    [JsonPropertyName("InstallationNotes")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string InstallationNotes { get; set; }

    [JsonPropertyName("Overview")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string Overview { get; set; }

    [JsonPropertyName("LanguageName")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string LanguageName { get; set; }

    // OS

    [JsonPropertyName("OSName")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string OsName { get; set; }

    [JsonPropertyName("OsCode")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string OsCode { get; set; }

    // compatibility

    [JsonPropertyName("OSList")]
    public NvidiaCompatibleOS[] OsList { get; set; }

    [JsonPropertyName("series")]
    public NvidiaSeries[] Series { get; set; }
}

public partial class NvidiaCompatibleOS
{
    [JsonPropertyName("OSName")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string Name { get; set; }

    [JsonPropertyName("OsCode")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string Code { get; set; }
}

public partial class NvidiaSeries
{
    [JsonPropertyName("seriesname")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string Name { get; set; }

    [JsonPropertyName("products")]
    public NvidiaProduct[] Products { get; set; }
}

public partial class NvidiaProduct
{
    [JsonPropertyName("productName")]
    [JsonConverter(typeof(EscapeQueryStringConverter))]
    public string Name { get; set; }
}
