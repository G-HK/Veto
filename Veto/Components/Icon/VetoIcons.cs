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
}
