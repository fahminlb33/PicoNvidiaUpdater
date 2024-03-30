using Spectre.Console;
using System.Reflection;
using System.Text;

namespace PicoNvidiaUpdater.Infrastructure.Helpers;

internal static class ConsoleMessages
{
    public static void Welcome()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AnsiConsole.Markup("[bold yellow on blue]PicoNvidiaUpdater[/]\n");
        AnsiConsole.WriteLine($"Version {version.Major}.{version.Minor} {version.Revision}");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    public static string ParseHtml(string html)
    {
        // phase 1 -- remove HTML tags
        var sb = new StringBuilder(html);
        sb.Replace("<br/>", "\n");
        sb.Replace("<br />", "\n");
        
        sb.Replace("<ul>", "\n");
        sb.Replace("</ul>", "\n");

        sb.Replace("<b>", ">> ");
        sb.Replace("</b>", " <<");

        sb.Replace("<li>", "- ");
        sb.Replace("</li>", "\n");

        sb.Replace("<p>", "");
        sb.Replace("</p>", "");

        sb.Replace("</a>", "");
        sb.Replace("\t", "");

        var temp = RegexPatterns.AnchorHtml().Replace(sb.ToString(), "");

        // phase 2 - remove Learn more part
        temp = temp[..temp.IndexOf("Learn more")].Trim();

        // phase 3 - process line by line
        sb = new StringBuilder();
        foreach (var line in temp.Split('\n'))
        {
            sb.AppendLine(line.Trim());
        }

        return sb.ToString();
    }
}
