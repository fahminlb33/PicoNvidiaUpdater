using System.Text.RegularExpressions;

namespace PicoNvidiaUpdater.Infrastructure.Helpers;

public partial class RegexPatterns
{
    [GeneratedRegex("Windows 11")]
    public static partial Regex IsWindows11();

    [GeneratedRegex(@"^NVIDIA")]
    public static partial Regex ContainsNvidia();

    [GeneratedRegex(@".*(\(Notebook|Quadro Blade).*")]
    public static partial Regex IsNotebook();

    [GeneratedRegex(@"(?<=NVIDIA )(.*(?= \([A-Z]+\))|.*(?= [0-9]+GB)|.*(?= with Max-Q Design)|.*(?= COLLECTORS EDITION)|.*)")]
    public static partial Regex GPUName();

    [GeneratedRegex(@".*(?= \([A-Z]+\))|.*(?= [0-9]+GB)|.*")]
    public static partial Regex GPUVariant();

    [GeneratedRegex(@"[0-9]{1,2}%")]
    public static partial Regex Percentage();

    [GeneratedRegex(@"<a.+>")]
    public static partial Regex AnchorHtml();
}


