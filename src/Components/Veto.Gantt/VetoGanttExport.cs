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