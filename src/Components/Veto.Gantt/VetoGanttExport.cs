using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Veto.Components.Gantt;

/// <summary>The formats <see cref="VetoGantt"/> can produce on its own.</summary>
public enum VetoGanttExportFormat
{
    /// <summary>Comma-separated values, for a spreadsheet.</summary>
    Csv,

    /// <summary>JSON, for another program.</summary>
    Json
}

/// <summary>
/// Raised before a <see cref="VetoGantt"/> exports, carrying the rows that would be written.
/// </summary>
/// <remarks>
/// Set <see cref="Handled"/> to take the export over - to post the rows to a server, or produce
/// a format the component does not, such as XLSX. Leave it alone and the component downloads
/// the file itself.
/// </remarks>
public sealed class VetoGanttExportRequest
{
    /// <summary>Creates the request. Constructed by the component, not by apps.</summary>
    public VetoGanttExportRequest ( VetoGanttExportFormat format, IReadOnlyList<VetoGanttItem> items, string fileName )
    {
        Format = format;
        Items = items;
        FileName = fileName;
    }

    /// <summary>The format that was asked for.</summary>
    public VetoGanttExportFormat Format { get; }

    /// <summary>
    /// The rows as currently filtered and ordered - what is on screen, not the whole data set.
    /// Exporting what the reader can see is almost always what they meant.
    /// </summary>
    public IReadOnlyList<VetoGanttItem> Items { get; }

    /// <summary>The file name the component would use, without an extension.</summary>
    public string FileName { get; }

    /// <summary>Set to true to suppress the built-in download and handle the export yourself.</summary>
    public bool Handled { get; set; }
}

/// <summary>
/// The formats the chart's export menu writes, public so an app that handles
/// <c>OnExport</c> itself - to upload the file, say - can produce exactly the same content.
/// </summary>
public static class VetoGanttExporter
{
    /// <summary>Renders the rows as CSV.</summary>
    /// <remarks>
    /// Dates are written round-trippable (yyyy-MM-dd) in the invariant culture, so a spreadsheet
    /// in another locale reads them as the same days - and a Thai or Saudi browser, whose
    /// calendars are not Gregorian, does not write the year as 2569 or 1448.
    /// </remarks>
    public static string ToCsv ( IReadOnlyList<VetoGanttItem> items )
    {
        ArgumentNullException.ThrowIfNull(items);

        var inv = CultureInfo.InvariantCulture;
        var csv = new StringBuilder();
        csv.AppendLine("Id,Name,Parent,Start,End,Days,Category,Progress,Milestone,StartTime,EndTime");

        foreach (var item in items)
        {
            csv.Append(Escape(item.Id)).Append(',')
               .Append(Escape(item.Name)).Append(',')
               .Append(Escape(item.ParentId)).Append(',')
               .Append(item.Start.ToString("yyyy-MM-dd", inv)).Append(',')
               .Append(item.End.ToString("yyyy-MM-dd", inv)).Append(',')
               .Append(item.DurationDays.ToString(inv)).Append(',')
               .Append(Escape(item.Category)).Append(',')
               .Append(item.Progress?.ToString("0.##", inv) ?? string.Empty).Append(',')
               .Append(item.IsMilestone ? "true" : "false").Append(',')
               .Append(item.StartTime?.ToString("HH:mm", inv) ?? string.Empty).Append(',')
               .Append(item.EndTime?.ToString("HH:mm", inv) ?? string.Empty)
               .AppendLine();
        }

        return csv.ToString();
    }

    /// <summary>Renders the rows as JSON.</summary>
    public static string ToJson ( IReadOnlyList<VetoGanttItem> items )
    {
        ArgumentNullException.ThrowIfNull(items);

        var inv = CultureInfo.InvariantCulture;

        return JsonSerializer.Serialize(
            items.Select(i => new
            {
                i.Id,
                i.Name,
                Parent = i.ParentId,
                Start = i.Start.ToString("yyyy-MM-dd", inv),
                End = i.End.ToString("yyyy-MM-dd", inv),
                Days = i.DurationDays,
                i.Category,
                i.Progress,
                Milestone = i.IsMilestone,
                StartTime = i.StartTime?.ToString("HH:mm", inv),
                EndTime = i.EndTime?.ToString("HH:mm", inv)
            }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Makes a text field safe for CSV. Quoted when it holds a comma, a quote or a newline, which
    /// would otherwise split the row. And a field starting with = + - @ (or a tab or carriage
    /// return) gets a leading apostrophe: Excel and friends run such a cell as a formula, so a
    /// task named <c>=HYPERLINK(...)</c> would execute when the export is opened - the OWASP
    /// "CSV injection".
    /// </summary>
    internal static string Escape ( string? value )
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            value = "'" + value;
        }

        var needsQuoting = value.Contains(',')
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        return needsQuoting
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
