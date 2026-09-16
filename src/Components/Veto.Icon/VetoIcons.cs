namespace Veto.Components.Icon;

/// <summary>
/// The built-in icon set. Each constant is the inner markup of a 24x24 SVG drawn with
/// <c>currentColor</c> strokes, so icons inherit their colour and size from context.
/// </summary>
/// <remarks>
/// Deliberately a small set of string constants rather than an icon font or a package
/// dependency: an app pays only for the icons it renders, and
/// <see cref="VetoIcon.Icon"/> accepts any SVG markup, so nothing here is a ceiling.
/// </remarks>
public static class VetoIcons
{
    /// <summary>Hamburger menu.</summary>
    public const string Menu = "<path d=\"M3 6h18M3 12h18M3 18h18\" />";

    /// <summary>Close / dismiss.</summary>
    public const string Close = "<path d=\"M18 6 6 18M6 6l12 12\" />";

    /// <summary>Chevron pointing down; rotate for the other directions.</summary>
    public const string ChevronDown = "<path d=\"m6 9 6 6 6-6\" />";

    /// <summary>Chevron pointing up.</summary>
    public const string ChevronUp = "<path d=\"m18 15-6-6-6 6\" />";

    /// <summary>Chevron pointing left.</summary>
    public const string ChevronLeft = "<path d=\"m15 18-6-6 6-6\" />";

    /// <summary>Chevron pointing right.</summary>
    public const string ChevronRight = "<path d=\"m9 18 6-6-6-6\" />";

    /// <summary>House.</summary>
    public const string Home = "<path d=\"M3 10.5 12 3l9 7.5\" /><path d=\"M5 9.5V21h14V9.5\" />";

    /// <summary>Magnifying glass.</summary>
    public const string Search = "<circle cx=\"11\" cy=\"11\" r=\"7\" /><path d=\"m20 20-3.5-3.5\" />";

    /// <summary>Gear.</summary>
    public const string Settings = "<circle cx=\"12\" cy=\"12\" r=\"3\" />" +
        "<path d=\"M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9 7 7M17 17l2.1 2.1M19.1 4.9 17 7M7 17l-2.1 2.1\" />";

    /// <summary>Person.</summary>
    public const string User = "<circle cx=\"12\" cy=\"8\" r=\"4\" /><path d=\"M4 21a8 8 0 0 1 16 0\" />";

    /// <summary>Bell.</summary>
    public const string Bell = "<path d=\"M18 16V11a6 6 0 1 0-12 0v5l-2 3h16Z\" /><path d=\"M10 21h4\" />";

    /// <summary>Sun.</summary>
    public const string Sun = "<circle cx=\"12\" cy=\"12\" r=\"4\" />" +
        "<path d=\"M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4\" />";

    /// <summary>Crescent moon.</summary>
    public const string Moon = "<path d=\"M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z\" />";

    /// <summary>Monitor.</summary>
    public const string Monitor = "<rect x=\"2\" y=\"4\" width=\"20\" height=\"13\" rx=\"2\" /><path d=\"M8 21h8M12 17v4\" />";

    /// <summary>Checkmark.</summary>
    public const string Check = "<path d=\"m4 12 5 5L20 6\" />";

    /// <summary>Plus.</summary>
    public const string Plus = "<path d=\"M12 5v14M5 12h14\" />";

    /// <summary>Minus.</summary>
    public const string Minus = "<path d=\"M5 12h14\" />";

    /// <summary>Pencil.</summary>
    public const string Edit = "<path d=\"M4 20h4L20 8l-4-4L4 16v4Z\" /><path d=\"m14 6 4 4\" />";

    /// <summary>Waste basket.</summary>
    public const string Trash = "<path d=\"M4 7h16M9 7V4h6v3M6 7l1 14h10l1-14\" />";

    /// <summary>Two overlapping sheets.</summary>
    public const string Copy = "<rect x=\"9\" y=\"9\" width=\"11\" height=\"11\" rx=\"2\" /><path d=\"M5 15V5a2 2 0 0 1 2-2h10\" />";

    /// <summary>Information.</summary>
    public const string Info = "<circle cx=\"12\" cy=\"12\" r=\"9\" /><path d=\"M12 11v6M12 7.5v.5\" />";

    /// <summary>Exclamation in a triangle.</summary>
    public const string Warning = "<path d=\"M12 3 2.5 20h19L12 3Z\" /><path d=\"M12 10v4M12 17.5v.5\" />";

    /// <summary>Cross in a circle.</summary>
    public const string Error = "<circle cx=\"12\" cy=\"12\" r=\"9\" /><path d=\"m15 9-6 6M9 9l6 6\" />";

    /// <summary>Checkmark in a circle.</summary>
    public const string Success = "<circle cx=\"12\" cy=\"12\" r=\"9\" /><path d=\"m8 12 3 3 5-6\" />";

    /// <summary>Document.</summary>
    public const string Document = "<path d=\"M6 3h8l4 4v14H6Z\" /><path d=\"M14 3v4h4\" />";

    /// <summary>Folder.</summary>
    public const string Folder = "<path d=\"M3 7h6l2 2h10v10H3Z\" />";

    /// <summary>Grid of four squares.</summary>
    public const string Grid = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\" />" +
        "<rect x=\"14\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\" />" +
        "<rect x=\"3\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\" />" +
        "<rect x=\"14\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\" />";

    /// <summary>Sidebar panel.</summary>
    public const string Sidebar = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"2\" /><path d=\"M9 4v16\" />";

    /// <summary>Six dots, the conventional drag handle.</summary>
    public const string DragHandle = "<circle cx=\"9\" cy=\"6\" r=\"1.5\" /><circle cx=\"15\" cy=\"6\" r=\"1.5\" />" +
        "<circle cx=\"9\" cy=\"12\" r=\"1.5\" /><circle cx=\"15\" cy=\"12\" r=\"1.5\" />" +
        "<circle cx=\"9\" cy=\"18\" r=\"1.5\" /><circle cx=\"15\" cy=\"18\" r=\"1.5\" />";

    /// <summary>Arrow leaving a box.</summary>
    public const string ExternalLink = "<path d=\"M14 4h6v6\" /><path d=\"m20 4-8 8\" />" +
        "<path d=\"M18 14v6H4V6h6\" />";

    /// <summary>Terminal prompt.</summary>
    public const string Terminal = "<rect x=\"2\" y=\"4\" width=\"20\" height=\"16\" rx=\"2\" /><path d=\"m7 10 3 2-3 2M13 14h4\" />";

    /// <summary>Paint palette.</summary>
    public const string Palette = "<path d=\"M12 3a9 9 0 1 0 0 18c1.7 0 2-1.3 1.2-2.2-.8-1 0-2.3 1.3-2.3H17a4 4 0 0 0 4-4c0-5.2-4-9.5-9-9.5Z\" />" +
        "<circle cx=\"8\" cy=\"11\" r=\"1\" /><circle cx=\"12\" cy=\"8\" r=\"1\" /><circle cx=\"16\" cy=\"11\" r=\"1\" />";

    /// <summary>Five-pointed star - the default rating icon.</summary>
    public const string Star = "<path d=\"m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1-5.4-2.9-5.4 2.9 1-6.1L3.2 9.5l6.1-.9Z\" />";

    /// <summary>Heart.</summary>
    public const string Heart = "<path d=\"M12 20s-7.5-4.4-7.5-9.4A4.1 4.1 0 0 1 12 7.6a4.1 4.1 0 0 1 7.5 3C19.5 15.6 12 20 12 20Z\" />";

    /// <summary>Circle, for a rating that counts plain pips.</summary>
    public const string Circle = "<circle cx=\"12\" cy=\"12\" r=\"8\" />";

    /// <summary>Lightning bolt.</summary>
    public const string Bolt = "<path d=\"M13 3 5 14h6l-1 7 8-11h-6Z\" />";

    /// <summary>Bold "B" - a text-editor toolbar staple.</summary>
    public const string Bold = "<path d=\"M7 4h6a4 4 0 0 1 0 8H7Z\" /><path d=\"M7 12h7a4 4 0 0 1 0 8H7Z\" />";

    /// <summary>Italic.</summary>
    public const string Italic = "<path d=\"M14 4h-4M14 20h-4M15 4 9 20\" />";

    /// <summary>Underline.</summary>
    public const string Underline = "<path d=\"M7 4v7a5 5 0 0 0 10 0V4\" /><path d=\"M5 20h14\" />";

    /// <summary>Text aligned to the left.</summary>
    public const string AlignLeft = "<path d=\"M4 6h16M4 12h10M4 18h13\" />";

    /// <summary>Text centred.</summary>
    public const string AlignCenter = "<path d=\"M4 6h16M7 12h10M6 18h12\" />";

    /// <summary>Text aligned to the right.</summary>
    public const string AlignRight = "<path d=\"M4 6h16M10 12h10M7 18h13\" />";

    /// <summary>Text justified.</summary>
    public const string AlignJustify = "<path d=\"M4 6h16M4 12h16M4 18h16\" />";

    /// <summary>Chain link.</summary>
    public const string Link = "<path d=\"M10 13a5 5 0 0 0 7 0l2-2a5 5 0 0 0-7-7l-1 1\" />" +
        "<path d=\"M14 11a5 5 0 0 0-7 0l-2 2a5 5 0 0 0 7 7l1-1\" />";

    /// <summary>Undo arrow.</summary>
    public const string Undo = "<path d=\"M9 14 4 9l5-5\" /><path d=\"M4 9h10a6 6 0 0 1 0 12h-3\" />";

    /// <summary>Redo arrow.</summary>
    public const string Redo = "<path d=\"m15 14 5-5-5-5\" /><path d=\"M20 9H10a6 6 0 0 0 0 12h3\" />";

    /// <summary>Bulleted list.</summary>
    public const string ListBullet = "<path d=\"M9 6h11M9 12h11M9 18h11\" />" +
        "<circle cx=\"4.5\" cy=\"6\" r=\"1.2\" /><circle cx=\"4.5\" cy=\"12\" r=\"1.2\" /><circle cx=\"4.5\" cy=\"18\" r=\"1.2\" />";

    /// <summary>Upward trend arrow, for a statistic that has risen.</summary>
    public const string TrendUp = "<path d=\"m3 17 6-6 4 4 8-8\" /><path d=\"M15 7h6v6\" />";

    /// <summary>Downward trend arrow, for a statistic that has fallen.</summary>
    public const string TrendDown = "<path d=\"m3 7 6 6 4-4 8 8\" /><path d=\"M15 17h6v-6\" />";

    /// <summary>Tray with a downward arrow - download or export.</summary>
    public const string Download = "<path d=\"M12 3v12\" /><path d=\"m7 11 5 5 5-5\" /><path d=\"M4 20h16\" />";

    /// <summary>Three dots in a row - a horizontal overflow menu.</summary>
    public const string MoreHorizontal = "<circle cx=\"5\" cy=\"12\" r=\"1\" /><circle cx=\"12\" cy=\"12\" r=\"1\" />" +
        "<circle cx=\"19\" cy=\"12\" r=\"1\" />";

    /// <summary>Three stacked dots - a vertical overflow menu.</summary>
    public const string MoreVertical = "<circle cx=\"12\" cy=\"5\" r=\"1\" /><circle cx=\"12\" cy=\"12\" r=\"1\" />" +
        "<circle cx=\"12\" cy=\"19\" r=\"1\" />";

    // ---------------------------------------------------------------- File formats
    //
    // Drawn as the shape of the thing rather than its letters: three characters of text in a
    // 24px box is unreadable at every size the icons are actually used at, so CSV is a document
    // with a table in it and JSON is a document with braces.

    /// <summary>Document with a table in it - comma-separated values.</summary>
    public const string Csv = "<path d=\"M6 3h8l4 4v14H6Z\" /><path d=\"M14 3v4h4\" />" +
        "<path d=\"M8.5 13h7M8.5 17h7M12 13v4\" />";

    /// <summary>Document with braces in it - JSON.</summary>
    public const string Json = "<path d=\"M6 3h8l4 4v14H6Z\" /><path d=\"M14 3v4h4\" />" +
        "<path d=\"M10.5 12c-1 0-1 1.5-1 2.5s0 2.5-1 2.5\" /><path d=\"M13.5 12c1 0 1 1.5 1 2.5s0 2.5 1 2.5\" />";

    /// <summary>Document with a banded footer - a PDF.</summary>
    public const string Pdf = "<path d=\"M6 3h8l4 4v14H6Z\" /><path d=\"M14 3v4h4\" />" +
        "<rect x=\"6\" y=\"14\" width=\"12\" height=\"5\" rx=\"1\" />";

    /// <summary>Ruled grid of cells - a spreadsheet.</summary>
    public const string Spreadsheet = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"2\" />" +
        "<path d=\"M3 10h18M3 15h18M9 4v16M15 4v16\" />";

    /// <summary>Curly braces - source code.</summary>
    public const string Braces = "<path d=\"M8 4c-2 0-2 4-2 6s0 2-2 2 2 0 2 2 0 6 2 6\" />" +
        "<path d=\"M16 4c2 0 2 4 2 6s0 2 2 2-2 0-2 2 0 6-2 6\" />";

    /// <summary>Framed picture with a horizon - an image file.</summary>
    public const string Image = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"2\" />" +
        "<circle cx=\"8.5\" cy=\"9.5\" r=\"1.5\" /><path d=\"m21 16-5-5-9 9\" />";

    /// <summary>Lidded box with a clasp - a compressed archive.</summary>
    public const string Archive = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"5\" rx=\"1\" />" +
        "<path d=\"M5 9v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V9\" /><path d=\"M10 13h4\" />";

    // ---------------------------------------------------------------- Brands
    //
    // Simplified marks drawn in the same 24x24 stroke style as the rest of the set, so a social
    // row does not suddenly look like it came from somewhere else. The two marks that only read
    // as themselves when solid - GitHub and X - set their own fill.
    //
    // These are other companies' trademarks. Veto only draws them; whether a given app may use
    // a given mark is that app's question to answer, and each brand publishes its own terms.

    /// <summary>Google's G.</summary>
    public const string Google = "<path d=\"M21 12a9 9 0 1 1-2.64-6.36\" /><path d=\"M21 12h-8.5\" />";

    /// <summary>The GitHub mark.</summary>
    public const string GitHub = "<path fill=\"currentColor\" stroke=\"none\" d=\"M12 2a10 10 0 0 0-3.16 19.49c.5.09.68-.22.68-.48" +
        "v-1.7c-2.78.6-3.37-1.34-3.37-1.34-.45-1.16-1.11-1.47-1.11-1.47-.91-.62.07-.6.07-.6 1 .07 1.53 1.03 1.53 1.03" +
        ".89 1.52 2.34 1.08 2.91.83.09-.65.35-1.09.63-1.34-2.22-.25-4.56-1.11-4.56-4.95 0-1.09.39-1.98 1.03-2.68" +
        "-.1-.25-.45-1.27.1-2.65 0 0 .84-.27 2.75 1.02a9.5 9.5 0 0 1 5 0c1.91-1.29 2.75-1.02 2.75-1.02.55 1.38.2 2.4.1 2.65" +
        ".64.7 1.03 1.59 1.03 2.68 0 3.85-2.34 4.7-4.57 4.95.36.31.68.92.68 1.85v2.74c0 .27.18.58.69.48A10 10 0 0 0 12 2Z\" />";

    /// <summary>Instagram's rounded square with a lens.</summary>
    public const string Instagram = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"5\" />" +
        "<circle cx=\"12\" cy=\"12\" r=\"4\" /><path d=\"M17.5 6.5h.01\" />";

    /// <summary>Facebook's f.</summary>
    public const string Facebook = "<path d=\"M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3Z\" />";

    /// <summary>TikTok's note.</summary>
    public const string TikTok = "<path d=\"M9.5 11.5a4.5 4.5 0 1 0 4.5 4.5V3c.7 2.8 2.9 4.8 5.5 5\" />";

    /// <summary>The X mark, formerly Twitter.</summary>
    public const string X = "<path fill=\"currentColor\" stroke=\"none\" d=\"M4 3h4.2l4.3 5.8L17.8 3H21l-6.8 7.7L21.4 21h-4.2" +
        "l-4.6-6.2L7 21H3.8l7.1-8.1L4 3Zm2.3 1.6 10.9 14.8h1.6L7.9 4.6H6.3Z\" />";

    /// <summary>LinkedIn's in.</summary>
    public const string LinkedIn = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\" />" +
        "<path d=\"M7 10v7M7 7v.01M11 17v-4a2 2 0 0 1 4 0v4\" />";

    /// <summary>YouTube's play badge.</summary>
    public const string YouTube = "<rect x=\"2\" y=\"5\" width=\"20\" height=\"14\" rx=\"4\" />" +
        "<path d=\"m10 9 5 3-5 3V9Z\" />";

    /// <summary>Discord's face.</summary>
    public const string Discord = "<path d=\"M16.5 18.5S19 17.5 20 15c0-4-1-7-3-9l-1.5 1a10 10 0 0 0-7 0L7 6c-2 2-3 5-3 9 1 2.5 3.5 3.5 3.5 3.5\" />" +
        "<path d=\"m8 17-1 2m9-2 1 2\" /><path d=\"M9.5 12.5h.01M14.5 12.5h.01\" />";

    /// <summary>WhatsApp's speech bubble.</summary>
    public const string WhatsApp = "<path d=\"M21 12a9 9 0 0 1-13.4 7.8L3 21l1.2-4.5A9 9 0 1 1 21 12Z\" />" +
        "<path d=\"M9 9.5c0 3 2.5 5.5 5.5 5.5l1-1.5-2-1-1 1a6 6 0 0 1-2-2l1-1-1-2-1.5 1Z\" />";

    /// <summary>Telegram's paper plane.</summary>
    public const string Telegram = "<path d=\"m21 4-9 16-2.2-6.3L3.5 11 21 4Z\" /><path d=\"m9.8 13.7 11.2-9.7\" />";

    /// <summary>Slack's pinwheel.</summary>
    public const string Slack = "<path d=\"M8.5 3.5h2V9h-2a2.75 2.75 0 0 1 0-5.5Z\" />" +
        "<path d=\"M15.5 20.5h-2V15h2a2.75 2.75 0 0 1 0 5.5Z\" />" +
        "<path d=\"M3.5 15.5v-2H9v2a2.75 2.75 0 0 1-5.5 0Z\" />" +
        "<path d=\"M20.5 8.5v2H15v-2a2.75 2.75 0 0 1 5.5 0Z\" />";

    /// <summary>Reddit's face.</summary>
    public const string Reddit = "<circle cx=\"12\" cy=\"13\" r=\"8\" /><path d=\"M12 5V3l4 1\" />" +
        "<path d=\"M9.5 12.5h.01M14.5 12.5h.01\" /><path d=\"M9.5 16a4 4 0 0 0 5 0\" />";

    /// <summary>Twitch's screen.</summary>
    public const string Twitch = "<path d=\"M4 3h16v11l-4 4h-3l-3 3H8v-3H4V3Z\" /><path d=\"M11 8v4M15 8v4\" />";

    /// <summary>Apple's fruit.</summary>
    public const string Apple = "<path d=\"M12 7.2c-1.4 0-2.1-.8-3.4-.8-2 0-4.1 1.8-4.1 5.2 0 3.4 2.5 8.4 4.6 8.4 1.1 0 1.7-.7 2.9-.7s1.8.7 2.9.7c2.1 0 4.6-5 4.6-8.4 0-2.4-1.6-4.2-3.6-4.2-1.3 0-2.5.8-3.9.8Z\" />" +
        "<path d=\"M13.2 4.6c.8-1 1.1-2.1.9-2.6-1 .1-2.1.7-2.7 1.5-.6.8-.9 1.8-.7 2.6 1 .1 1.9-.6 2.5-1.5Z\" />";

    /// <summary>Microsoft's four panes.</summary>
    public const string Microsoft = "<rect x=\"3\" y=\"3\" width=\"8\" height=\"8\" /><rect x=\"13\" y=\"3\" width=\"8\" height=\"8\" />" +
        "<rect x=\"3\" y=\"13\" width=\"8\" height=\"8\" /><rect x=\"13\" y=\"13\" width=\"8\" height=\"8\" />";

    // ---------------------------------------------------------------- Data and transfer

    /// <summary>Tray with an upward arrow - upload.</summary>
    public const string Upload = "<path d=\"M12 16V4\" /><path d=\"m7 9 5-5 5 5\" /><path d=\"M4 20h16\" />";

    /// <summary>Cloud.</summary>
    public const string Cloud = "<path d=\"M7 18a4 4 0 0 1 .5-8 5.5 5.5 0 0 1 10.4 1.6A3.5 3.5 0 0 1 17.5 18H7Z\" />";

    /// <summary>Cloud with an upward arrow - sync up.</summary>
    public const string CloudUpload = "<path d=\"M7 17a4 4 0 0 1 .5-8 5.5 5.5 0 0 1 10.4 1.6A3.5 3.5 0 0 1 17.5 17\" />" +
        "<path d=\"M12 21v-8\" /><path d=\"m9 16 3-3 3 3\" />";

    /// <summary>Stacked discs - a database.</summary>
    public const string Database = "<ellipse cx=\"12\" cy=\"6\" rx=\"8\" ry=\"3\" />" +
        "<path d=\"M4 6v12c0 1.7 3.6 3 8 3s8-1.3 8-3V6\" /><path d=\"M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3\" />";

    /// <summary>Racked boxes - a server.</summary>
    public const string Server = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"7\" rx=\"2\" />" +
        "<rect x=\"3\" y=\"13\" width=\"18\" height=\"7\" rx=\"2\" /><path d=\"M7 7.5h.01M7 16.5h.01\" />";

    /// <summary>Chip with legs - a processor.</summary>
    public const string Chip = "<rect x=\"7\" y=\"7\" width=\"10\" height=\"10\" rx=\"1\" />" +
        "<path d=\"M10 3v4M14 3v4M10 17v4M14 17v4M3 10h4M3 14h4M17 10h4M17 14h4\" />";

    /// <summary>Radiating arcs - a wireless connection.</summary>
    public const string Wifi = "<path d=\"M3 9a15 15 0 0 1 18 0\" /><path d=\"M6.5 12.5a10 10 0 0 1 11 0\" />" +
        "<path d=\"M10 16a5 5 0 0 1 4 0\" /><path d=\"M12 19.5h.01\" />";

    /// <summary>Battery with a charge level.</summary>
    public const string Battery = "<rect x=\"2\" y=\"8\" width=\"17\" height=\"9\" rx=\"2\" />" +
        "<path d=\"M22 11.5v2\" /><path d=\"M5 11h6v3H5Z\" />";

    /// <summary>Globe with meridians.</summary>
    public const string Globe = "<circle cx=\"12\" cy=\"12\" r=\"9\" /><path d=\"M3 12h18\" />" +
        "<path d=\"M12 3a14 14 0 0 1 0 18 14 14 0 0 1 0-18Z\" />";

    /// <summary>Folded map.</summary>
    public const string Map = "<path d=\"m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3V6Z\" /><path d=\"M9 3v15M15 6v15\" />";

    /// <summary>Map pin.</summary>
    public const string Location = "<path d=\"M12 21s7-5.5 7-11a7 7 0 1 0-14 0c0 5.5 7 11 7 11Z\" />" +
        "<circle cx=\"12\" cy=\"10\" r=\"2.5\" />";

    // ---------------------------------------------------------------- Actions

    /// <summary>Funnel - filter.</summary>
    public const string Filter = "<path d=\"M3 5h18l-7 8v6l-4-2v-4L3 5Z\" />";

    /// <summary>Bars descending with an up arrow - sort ascending.</summary>
    public const string SortAscending = "<path d=\"M4 7h10M4 12h7M4 17h4\" /><path d=\"M18 18V7\" /><path d=\"m15 10 3-3 3 3\" />";

    /// <summary>Bars ascending with a down arrow - sort descending.</summary>
    public const string SortDescending = "<path d=\"M4 7h4M4 12h7M4 17h10\" /><path d=\"M18 7v11\" /><path d=\"m15 15 3 3 3-3\" />";

    /// <summary>Circling arrow - refresh or retry.</summary>
    public const string Refresh = "<path d=\"M21 12a9 9 0 1 1-3-6.7\" /><path d=\"M21 4v5h-5\" />";

    /// <summary>Floppy outline - save.</summary>
    public const string Save = "<path d=\"M5 3h11l3 3v15H5Z\" /><path d=\"M8 3v6h8V3\" /><rect x=\"8\" y=\"13\" width=\"8\" height=\"6\" />";

    /// <summary>Printer.</summary>
    public const string Print = "<path d=\"M7 9V3h10v6\" /><rect x=\"3\" y=\"9\" width=\"18\" height=\"7\" rx=\"2\" />" +
        "<path d=\"M7 16h10v5H7Z\" />";

    /// <summary>Node branching out - share.</summary>
    public const string Share = "<circle cx=\"6\" cy=\"12\" r=\"2.5\" /><circle cx=\"18\" cy=\"6\" r=\"2.5\" />" +
        "<circle cx=\"18\" cy=\"18\" r=\"2.5\" /><path d=\"m8.2 10.9 7.6-3.8M8.2 13.1l7.6 3.8\" />";

    /// <summary>Paper plane - send.</summary>
    public const string Send = "<path d=\"m21 3-9 18-2.5-7.5L2 11l19-8Z\" /><path d=\"m9.5 13.5 11.5-10.5\" />";

    /// <summary>Arrow turning back - reply.</summary>
    public const string Reply = "<path d=\"m9 7-5 5 5 5\" /><path d=\"M4 12h9a7 7 0 0 1 7 7v1\" />";

    /// <summary>Tray - inbox.</summary>
    public const string Inbox = "<path d=\"M3 13 6 4h12l3 9v7H3v-7Z\" /><path d=\"M3 13h5l1 2h6l1-2h5\" />";

    /// <summary>Push pin.</summary>
    public const string Pin = "<path d=\"M12 15v6\" /><path d=\"M8 3h8l-1 5 3 4H6l3-4-1-5Z\" />";

    /// <summary>Label with a hole - tag.</summary>
    public const string Tag = "<path d=\"M12 3H3v9l9 9 9-9-9-9Z\" /><path d=\"M7.5 7.5h.01\" />";

    /// <summary>Flag on a pole.</summary>
    public const string Flag = "<path d=\"M5 21V3\" /><path d=\"M5 4h13l-2.5 4.5L18 13H5\" />";

    /// <summary>Ribbon - bookmark.</summary>
    public const string Bookmark = "<path d=\"M6 3h12v18l-6-4.5L6 21V3Z\" />";

    /// <summary>Gift box.</summary>
    public const string Gift = "<rect x=\"3\" y=\"8\" width=\"18\" height=\"13\" rx=\"1\" /><path d=\"M3 12h18M12 8v13\" />" +
        "<path d=\"M12 8S10.5 4 8.5 4a2 2 0 0 0 0 4H12Zm0 0s1.5-4 3.5-4a2 2 0 0 1 0 4H12Z\" />";

    /// <summary>Shopping cart.</summary>
    public const string Cart = "<path d=\"M3 4h2l2.5 11h10L20 7H6\" /><circle cx=\"9\" cy=\"19\" r=\"1.5\" />" +
        "<circle cx=\"17\" cy=\"19\" r=\"1.5\" />";

    /// <summary>Payment card.</summary>
    public const string CreditCard = "<rect x=\"2\" y=\"5\" width=\"20\" height=\"14\" rx=\"2\" />" +
        "<path d=\"M2 10h20M6 15h4\" />";

    // ---------------------------------------------------------------- Time, mail and security

    /// <summary>Month grid - calendar.</summary>
    public const string Calendar = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"16\" rx=\"2\" />" +
        "<path d=\"M3 10h18M8 3v4M16 3v4\" />";

    /// <summary>Clock face.</summary>
    public const string Clock = "<circle cx=\"12\" cy=\"12\" r=\"9\" /><path d=\"M12 7v5l4 2\" />";

    /// <summary>Hourglass - a timer.</summary>
    public const string Timer = "<path d=\"M7 3h10M7 21h10\" /><path d=\"M8 3v3.5L12 12l-4 5.5V21M16 3v3.5L12 12l4 5.5V21\" />";

    /// <summary>Envelope - mail.</summary>
    public const string Mail = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\" /><path d=\"m3 7 9 6 9-6\" />";

    /// <summary>Handset - phone.</summary>
    public const string Phone = "<path d=\"M5 3h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 12l5 2v4a2 2 0 0 1-2 2A15 15 0 0 1 3 5a2 2 0 0 1 2-2Z\" />";

    /// <summary>Closed padlock.</summary>
    public const string Lock = "<rect x=\"4\" y=\"10\" width=\"16\" height=\"11\" rx=\"2\" />" +
        "<path d=\"M8 10V7a4 4 0 0 1 8 0v3\" /><path d=\"M12 14v3\" />";

    /// <summary>Open padlock.</summary>
    public const string Unlock = "<rect x=\"4\" y=\"10\" width=\"16\" height=\"11\" rx=\"2\" />" +
        "<path d=\"M8 10V7a4 4 0 0 1 7.5-2\" /><path d=\"M12 14v3\" />";

    /// <summary>Key.</summary>
    public const string Key = "<circle cx=\"8\" cy=\"15\" r=\"4\" /><path d=\"m11 12 8-8 2 2-2 2 2 2-2 2-2-2-2 2\" />";

    /// <summary>Shield.</summary>
    public const string Shield = "<path d=\"M12 3l8 3v6c0 5-3.4 8.3-8 9-4.6-.7-8-4-8-9V6l8-3Z\" />";

    /// <summary>Open eye - visible.</summary>
    public const string Eye = "<path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\" /><circle cx=\"12\" cy=\"12\" r=\"3\" />";

    /// <summary>Struck-through eye - hidden.</summary>
    public const string EyeOff = "<path d=\"M4 5l16 14\" />" +
        "<path d=\"M9.5 6.3A9.8 9.8 0 0 1 12 6c6.5 0 10 6 10 6a17 17 0 0 1-2.8 3.4\" />" +
        "<path d=\"M6.2 8.3A16.7 16.7 0 0 0 2 12s3.5 6 10 6a9.9 9.9 0 0 0 3.6-.65\" />" +
        "<path d=\"M10 10.2a3 3 0 0 0 4 4.2\" />";

    // ---------------------------------------------------------------- Media

    /// <summary>Play triangle.</summary>
    public const string Play = "<path d=\"m7 4 13 8-13 8V4Z\" />";

    /// <summary>Pause bars.</summary>
    public const string Pause = "<path d=\"M8 4v16M16 4v16\" />";

    /// <summary>Stop square.</summary>
    public const string Stop = "<rect x=\"5\" y=\"5\" width=\"14\" height=\"14\" rx=\"1\" />";

    /// <summary>Skip to the previous track.</summary>
    public const string SkipBack = "<path d=\"M19 5v14L8 12l11-7Z\" /><path d=\"M5 5v14\" />";

    /// <summary>Skip to the next track.</summary>
    public const string SkipForward = "<path d=\"M5 5v14l11-7L5 5Z\" /><path d=\"M19 5v14\" />";

    /// <summary>Speaker with waves.</summary>
    public const string Volume = "<path d=\"M4 9h3l5-4v14l-5-4H4V9Z\" /><path d=\"M16 9a4 4 0 0 1 0 6M19 6.5a8 8 0 0 1 0 11\" />";

    /// <summary>Muted speaker.</summary>
    public const string VolumeOff = "<path d=\"M4 9h3l5-4v14l-5-4H4V9Z\" /><path d=\"m16 10 5 4M21 10l-5 4\" />";

    /// <summary>Microphone.</summary>
    public const string Microphone = "<rect x=\"9\" y=\"3\" width=\"6\" height=\"11\" rx=\"3\" />" +
        "<path d=\"M6 12a6 6 0 0 0 12 0\" /><path d=\"M12 18v3M9 21h6\" />";

    /// <summary>Still camera.</summary>
    public const string Camera = "<path d=\"M3 8h4l2-3h6l2 3h4v12H3V8Z\" /><circle cx=\"12\" cy=\"13\" r=\"4\" />";

    /// <summary>Video camera.</summary>
    public const string Video = "<rect x=\"2\" y=\"6\" width=\"13\" height=\"12\" rx=\"2\" /><path d=\"m15 10 7-3v10l-7-3v-4Z\" />";

    // ---------------------------------------------------------------- Size and zoom

    /// <summary>Arrows to the corners - maximise.</summary>
    public const string Maximize = "<path d=\"M9 3H3v6M21 9V3h-6M15 21h6v-6M3 15v6h6\" />";

    /// <summary>Arrows to the centre - minimise.</summary>
    public const string Minimize = "<path d=\"M3 9h6V3M15 3v6h6M21 15h-6v6M9 21v-6H3\" />";

    /// <summary>Magnifier with a plus - zoom in.</summary>
    public const string ZoomIn = "<circle cx=\"11\" cy=\"11\" r=\"7\" /><path d=\"m16 16 5 5\" /><path d=\"M11 8v6M8 11h6\" />";

    /// <summary>Magnifier with a minus - zoom out.</summary>
    public const string ZoomOut = "<circle cx=\"11\" cy=\"11\" r=\"7\" /><path d=\"m16 16 5 5\" /><path d=\"M8 11h6\" />";
}