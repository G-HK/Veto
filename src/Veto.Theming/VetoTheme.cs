using System.Text;
using System.Text.RegularExpressions;

namespace Veto.Theming;

/// <summary>
/// An app's overrides of the Veto design tokens. Every property is optional: anything left
/// null keeps the value from <c>VetoStyles.css</c>, so an app rebrands by setting the two or
/// three tokens it cares about rather than restyling the library.
/// </summary>
/// <remarks>
/// Overrides are emitted by <see cref="VetoThemeProvider"/> as CSS custom properties on
/// <c>:root</c>, which is why they cascade into every component, including ones rendered
/// into portals outside the provider's own markup.
/// </remarks>
public sealed partial record VetoTheme
{
    /// <summary>The palette used when the app renders light.</summary>
    public VetoPalette Light { get; init; } = new();

    /// <summary>The palette used when the app renders dark.</summary>
    public VetoPalette Dark { get; init; } = new();

    /// <summary>Corner radius of medium-sized components, e.g. <c>6px</c>.</summary>
    public string? RadiusMedium { get; init; }

    /// <summary>The body font stack, e.g. <c>"Inter", system-ui, sans-serif</c>.</summary>
    public string? FontSans { get; init; }

    /// <summary>The monospace font stack.</summary>
    public string? FontMono { get; init; }

    /// <summary>True when at least one token is overridden and a style block is worth rendering.</summary>
    public bool HasOverrides =>
        Light.HasOverrides || Dark.HasOverrides
        || RadiusMedium is not null
        || FontSans is not null || FontMono is not null;

    /// <summary>
    /// Renders the overrides as CSS. Light values and the shape/type tokens go on
    /// <c>:root</c> and on <c>[data-veto-theme="light"]</c>, so a component pinned to the light
    /// palette on a dark page gets the app's light palette rather than the library's default;
    /// dark values go behind both <c>[data-veto-theme="dark"]</c> and
    /// <c>prefers-color-scheme</c>, matching how <c>VetoStyles.css</c> is structured.
    /// </summary>
    public string ToCss ()
    {
        StringBuilder css = new();
        StringBuilder root = new();

        AppendPalette(root, Light);
        Append(root, "--veto-radius-md", RadiusMedium);
        Append(root, "--veto-font-sans", FontSans);
        Append(root, "--veto-font-mono", FontMono);

        if (root.Length > 0)
        {
            css.Append(":root,[data-veto-theme=\"light\"]{").Append(root).Append('}');
        }

        StringBuilder dark = new();
        AppendPalette(dark, Dark);

        if (dark.Length > 0)
        {
            css.Append("[data-veto-theme=\"dark\"]{").Append(dark).Append('}');
            css.Append("@media(prefers-color-scheme:dark){:root:not([data-veto-theme=\"light\"]){")
               .Append(dark)
               .Append("}}");
        }

        return css.ToString();
    }

    private static void AppendPalette ( StringBuilder builder, VetoPalette palette )
    {
        Append(builder, "--veto-color-primary", palette.Primary);
        Append(builder, "--veto-color-primary-hover", palette.PrimaryHover);
        Append(builder, "--veto-color-primary-contrast", palette.PrimaryContrast);
        Append(builder, "--veto-color-secondary", palette.Secondary);
        Append(builder, "--veto-color-success", palette.Success);
        Append(builder, "--veto-color-warning", palette.Warning);
        Append(builder, "--veto-color-danger", palette.Danger);
        Append(builder, "--veto-color-surface", palette.Surface);
        Append(builder, "--veto-color-surface-raised", palette.SurfaceRaised);
        Append(builder, "--veto-color-text", palette.Text);
        Append(builder, "--veto-color-text-muted", palette.TextMuted);
        Append(builder, "--veto-color-border", palette.Border);
    }

    private static void Append ( StringBuilder builder, string name, string? value )
    {
        if (IsSafeValue(value))
        {
            builder.Append(name).Append(':').Append(value).Append(';');
        }
    }

    /// <summary>
    /// Guards the emitted style block. Token values come from app code rather than end users,
    /// but they are still written into a <c>&lt;style&gt;</c> element, so anything that could
    /// close a declaration, a rule or the element itself is rejected outright.
    /// </summary>
    private static bool IsSafeValue ( string? value )
        => !string.IsNullOrWhiteSpace(value) && SafeValue().IsMatch(value);

    [GeneratedRegex(@"^[^<>{};@\\]{1,120}$")]
    private static partial Regex SafeValue ();
}

/// <summary>
/// One colour palette - the values for a single light or dark appearance. Every property is
/// optional; nulls keep the built-in token value.
/// </summary>
public sealed record VetoPalette
{
    /// <summary>The main accent colour.</summary>
    public string? Primary { get; init; }

    /// <summary>The accent colour on hover.</summary>
    public string? PrimaryHover { get; init; }

    /// <summary>Text and icons drawn on top of the accent colour.</summary>
    public string? PrimaryContrast { get; init; }

    /// <summary>The quieter accent colour.</summary>
    public string? Secondary { get; init; }

    /// <summary>Positive outcomes.</summary>
    public string? Success { get; init; }

    /// <summary>Warnings.</summary>
    public string? Warning { get; init; }

    /// <summary>Errors and destructive actions.</summary>
    public string? Danger { get; init; }

    /// <summary>The page background.</summary>
    public string? Surface { get; init; }

    /// <summary>Backgrounds that sit above the page, such as cards and headers.</summary>
    public string? SurfaceRaised { get; init; }

    /// <summary>Body text.</summary>
    public string? Text { get; init; }

    /// <summary>Secondary text: helper copy, captions, placeholders.</summary>
    public string? TextMuted { get; init; }

    /// <summary>Borders and dividers.</summary>
    public string? Border { get; init; }

    /// <summary>True when at least one colour in this palette is set.</summary>
    public bool HasOverrides =>
        Primary is not null || PrimaryHover is not null || PrimaryContrast is not null
           || Secondary is not null || Success is not null || Warning is not null
           || Danger is not null || Surface is not null || SurfaceRaised is not null
           || Text is not null || TextMuted is not null || Border is not null;
}