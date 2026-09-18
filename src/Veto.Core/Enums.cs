namespace Veto.Core;

/// <summary>
/// Which palette a component is drawn in. <see cref="Inherit"/> follows the page - the theme
/// the app set, or the reader's system setting; the other two pin one component, whatever the
/// page around it is doing.
/// </summary>
public enum VetoColorScheme
{
    /// <summary>Follow the page's theme.</summary>
    Inherit,

    /// <summary>Always the light palette, even on a dark page.</summary>
    Light,

    /// <summary>Always the dark palette, even on a light page.</summary>
    Dark
}

/// <summary>The attribute value that pins a palette, or null to follow the page.</summary>
public static class VetoColorSchemeExtensions
{
    /// <summary>The value for <c>data-veto-theme</c>, or null for <see cref="VetoColorScheme.Inherit"/>.</summary>
    public static string? ToThemeAttribute ( this VetoColorScheme scheme ) => scheme switch
    {
        VetoColorScheme.Light => "light",
        VetoColorScheme.Dark => "dark",
        _ => null
    };
}

/// <summary>Relative size of a component. Maps to a <c>--sm</c>/<c>--md</c>/<c>--lg</c> modifier class.</summary>
public enum VetoSize
{
    /// <summary>Compact.</summary>
    Small,

    /// <summary>The default size.</summary>
    Medium,

    /// <summary>Roomy.</summary>
    Large
}

/// <summary>Semantic color role of a component. Maps to a <c>--primary</c>-style modifier class.</summary>
public enum VetoColor
{
    /// <summary>The app's main accent.</summary>
    Primary,

    /// <summary>A quieter accent.</summary>
    Secondary,

    /// <summary>No accent; inherits surface and text tokens.</summary>
    Neutral,

    /// <summary>Positive outcome.</summary>
    Success,

    /// <summary>Needs attention.</summary>
    Warning,

    /// <summary>Destructive or invalid.</summary>
    Danger
}

/// <summary>Visual treatment of a component. Maps to a <c>--filled</c>-style modifier class.</summary>
public enum VetoVariant
{
    /// <summary>Solid background.</summary>
    Filled,

    /// <summary>Transparent background with a border.</summary>
    Outlined,

    /// <summary>No background and no border.</summary>
    Text
}

/// <summary>How a link draws its underline.</summary>
public enum VetoUnderline
{
    /// <summary>No underline, ever. For links that are already obvious from their surroundings.</summary>
    None,

    /// <summary>
    /// Bare until the pointer or the keyboard reaches it, then a thin neon line grows out from
    /// the middle. The default: it keeps a page of links quiet without hiding what is a link.
    /// </summary>
    Reveal,

    /// <summary>Always drawn. For a link in the middle of a paragraph, where it has to be findable.</summary>
    Always
}

/// <summary>Extension helpers that turn enum values into CSS modifier class names.</summary>
public static class VetoEnumExtensions
{
    /// <summary>Returns the modifier suffix for a size, e.g. <c>sm</c>.</summary>
    public static string ToModifier ( this VetoSize size ) => size switch
    {
        VetoSize.Small => "sm",
        VetoSize.Large => "lg",
        _ => "md"
    };

    /// <summary>Returns the modifier suffix for a color, e.g. <c>primary</c>.</summary>
    public static string ToModifier ( this VetoColor color ) => color.ToString().ToLowerInvariant();

    /// <summary>Returns the modifier suffix for a variant, e.g. <c>outlined</c>.</summary>
    public static string ToModifier ( this VetoVariant variant ) => variant.ToString().ToLowerInvariant();

    /// <summary>The global effect classes that draw this underline, or null for none.</summary>
    public static string? ToUnderlineClass ( this VetoUnderline underline ) =>
        underline switch
        {
            VetoUnderline.Reveal => "veto-underline-reveal",
            VetoUnderline.Always => "veto-underline-reveal veto-underline-static",
            _ => null
        };
}

/// <summary>The axis a component lays its children out on.</summary>
public enum VetoOrientation
{
    /// <summary>Top to bottom.</summary>
    Vertical,

    /// <summary>Left to right.</summary>
    Horizontal
}

/// <summary>
/// A step on the spacing scale. Each value maps to a <c>--veto-space-*</c> token, so spacing
/// stays consistent across every component and shifts with the theme rather than being
/// hard-coded per layout.
/// </summary>
public enum VetoSpacing
{
    /// <summary>No space at all.</summary>
    None,

    /// <summary><c>--veto-space-1</c>, 0.25rem.</summary>
    ExtraSmall,

    /// <summary><c>--veto-space-2</c>, 0.5rem.</summary>
    Small,

    /// <summary><c>--veto-space-3</c>, 0.75rem.</summary>
    Medium,

    /// <summary><c>--veto-space-4</c>, 1rem.</summary>
    Large,

    /// <summary><c>--veto-space-5</c>, 1.5rem.</summary>
    ExtraLarge,

    /// <summary><c>--veto-space-6</c>, 2rem.</summary>
    Huge
}

/// <summary>Alignment across the layout axis - <c>align-items</c>.</summary>
public enum VetoAlign
{
    /// <summary>Fill the cross axis.</summary>
    Stretch,

    /// <summary>Pack to the start.</summary>
    Start,

    /// <summary>Centre on the cross axis.</summary>
    Center,

    /// <summary>Pack to the end.</summary>
    End,

    /// <summary>Line children up on their text baselines.</summary>
    Baseline
}

/// <summary>Distribution along the layout axis - <c>justify-content</c>.</summary>
public enum VetoJustify
{
    /// <summary>Pack to the start.</summary>
    Start,

    /// <summary>Centre along the main axis.</summary>
    Center,

    /// <summary>Pack to the end.</summary>
    End,

    /// <summary>First and last flush to the edges, equal space between.</summary>
    SpaceBetween,

    /// <summary>Equal space around each child, so edge gaps are half the inner ones.</summary>
    SpaceAround,

    /// <summary>Equal space everywhere, edges included.</summary>
    SpaceEvenly
}

/// <summary>Turns the layout enums into the modifier suffixes their CSS uses.</summary>
public static class VetoLayoutExtensions
{
    /// <summary>Returns the modifier suffix for an orientation: <c>row</c> or <c>column</c>.</summary>
    public static string ToModifier ( this VetoOrientation orientation ) =>
        orientation == VetoOrientation.Horizontal
            ? "row"
            : "column";

    /// <summary>Returns the spacing step as a number, which the CSS feeds to <c>--veto-space-*</c>.</summary>
    public static string ToModifier ( this VetoSpacing spacing ) =>
        ((int)spacing).ToString();

    /// <summary>Returns the modifier suffix for cross-axis alignment, e.g. <c>center</c>.</summary>
    public static string ToModifier ( this VetoAlign align ) =>
        align.ToString().ToLowerInvariant();

    /// <summary>Returns the modifier suffix for main-axis distribution, e.g. <c>space-between</c>.</summary>
    public static string ToModifier ( this VetoJustify justify ) =>
        justify switch
        {
            VetoJustify.SpaceBetween => "space-between",
            VetoJustify.SpaceAround => "space-around",
            VetoJustify.SpaceEvenly => "space-evenly",
            _ => justify.ToString().ToLowerInvariant()
        };
}