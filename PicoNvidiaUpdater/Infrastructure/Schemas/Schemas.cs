using System.Xml;

namespace PicoNvidiaUpdater.Infrastructure.Schemas;

public enum NvidiaDriverType
{
    GameReady = 0,
    StudioReady = 1,
}

public enum NvidiaGpuKind
{
    Desktop,
    Notebook,
}

public record NvidiaGpu
{
    public string ID { get; init; }
    public string Name { get; init; }
    public NvidiaGpuKind Kind { get; init; }

    public static NvidiaGpu Parse(XmlNode node, NvidiaGpuKind kind)
    {
        return new NvidiaGpu
        {
            ID = node.SelectSingleNode("Value")!.InnerText,
            Name = node.SelectSingleNode("Name")!.InnerText,
            Kind = kind,
        };
    }
}

public record NvidiaOS
{
    public string ID { get; init; }
    public string Code { get; init; }
    public string Name { get; init; }

    public static NvidiaOS Parse(XmlNode node)
    {
        return new NvidiaOS
        {
            ID = node.SelectSingleNode("Value")!.InnerText,
            Name = node.SelectSingleNode("Name")!.InnerText,
            Code = node.Attributes!["Code"]!.InnerText,
        };
    }
}
