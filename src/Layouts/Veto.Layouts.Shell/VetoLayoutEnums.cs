namespace Veto.Layouts.Shell;

/// <summary>Which edge a panel is attached to.</summary>
public enum VetoAnchor
{
    /// <summary>The start edge - left in left-to-right languages.</summary>
    Left,

    /// <summary>The end edge.</summary>
    Right,

    /// <summary>The top edge.</summary>
    Top,

    /// <summary>The bottom edge.</summary>
    Bottom
}

/// <summary>
/// How a <see cref="VetoDrawer"/> behaves. Every behaviour ships; an app picks the one it
/// wants rather than being given one.
/// </summary>
public enum VetoDrawerVariant
{
    /// <summary>
    /// Always visible, pushing content aside. Best when the navigation is the point and the
    /// app is desktop-only.
    /// </summary>
    Persistent,

    /// <summary>
    /// Hidden until opened, then overlaid on the content with a scrim. Traps focus and closes
    /// on Escape.
    /// </summary>
    Temporary,

    /// <summary>
    /// Collapsed to an icon rail, expanding on hover or when toggled. Keeps navigation
    /// reachable without spending the width.
    /// </summary>
    Mini,

    /// <summary>
    /// Persistent above <see cref="VetoDrawer.Breakpoint"/> and temporary below it. With
    /// <see cref="VetoDrawer.MiniBreakpoint"/> set it steps full width, then rail, then overlay.
    /// The default.
    /// </summary>
    Responsive
}

/// <summary>Viewport widths the layout responds at.</summary>
public enum VetoBreakpoint
{
    /// <summary>640px.</summary>
    Small,

    /// <summary>960px.</summary>
    Medium,

    /// <summary>1280px.</summary>
    Large
}

/// <summary>How a drawer relates to the app bar.</summary>
public enum VetoClipMode
{
    /// <summary>The drawer starts below the app bar, so the bar spans the full width.</summary>
    Clipped,

    /// <summary>The drawer runs the full height, so it sits beside the app bar.</summary>
    Full
}

/// <summary>Turns the shell enums into the CSS modifier suffixes and pixel values they map to.</summary>
public static class VetoShellExtensions
{
    /// <summary>Returns the modifier suffix for an anchor, e.g. <c>left</c>.</summary>
    public static string ToModifier ( this VetoAnchor anchor ) => anchor.ToString().ToLowerInvariant();

    /// <summary>Returns the modifier suffix for a drawer variant, e.g. <c>temporary</c>.</summary>
    public static string ToModifier ( this VetoDrawerVariant variant ) => variant.ToString().ToLowerInvariant();

    /// <summary>Returns the modifier suffix for a breakpoint, e.g. <c>md</c>.</summary>
    public static string ToModifier ( this VetoBreakpoint breakpoint ) =>
        breakpoint switch
        {
            VetoBreakpoint.Small => "sm",
            VetoBreakpoint.Large => "lg",
            _ => "md"
        };

    /// <summary>True when the anchor runs down a side rather than across the top or bottom.</summary>
    public static bool IsVertical ( this VetoAnchor anchor ) =>
        anchor is VetoAnchor.Left or VetoAnchor.Right;
}