using Veto.Theming;

namespace VetoWeb.Client;

/// <summary>
/// The documentation site's brand palette: red on white in light mode, red on black in dark.
/// </summary>
/// <remarks>
/// This is the theming engine used the way an app would use it - overriding a handful of
/// tokens rather than restyling any component. Every Veto component on the site picks these up
/// through <c>--veto-*</c> custom properties without knowing the site exists.
/// </remarks>
public static class VetoDocsTheme
{
    /// <summary>The palette applied by <c>VetoThemeProvider</c> in <c>MainLayout</c>.</summary>
    public static readonly VetoTheme Theme = new()
    {
        Light = new VetoPalette
        {
            // 6.9:1 on white, so it is legible as body-weight text and not only as a fill.
            Primary = "#C2121F",
            PrimaryHover = "#9C0D18",
            PrimaryContrast = "#FFFFFF",
            Secondary = "#6B5F61",

            Surface = "#FFFFFF",
            SurfaceRaised = "#FAF7F7",
            Text = "#1A1517",
            TextMuted = "#6B5F61",
            Border = "#E7DDDE",

            // Danger stays distinctly darker and cooler than the brand red, so an error is not
            // mistaken for an accent. Colour alone never carries the meaning - error states
            // also set aria-invalid and render a message.
            Danger = "#8B1116",
            Warning = "#B45309",
            Success = "#15803D"
        },

        Dark = new VetoPalette
        {
            Primary = "#FF3B47",
            PrimaryHover = "#FF6A73",
            PrimaryContrast = "#15070A",
            Secondary = "#A29698",

            Surface = "#0B0A0B",
            SurfaceRaised = "#171416",
            Text = "#F4EFEF",
            TextMuted = "#A29698",
            Border = "#2E2729",

            Danger = "#FF8A8A",
            Warning = "#FBBF24",
            Success = "#4ADE80"
        },

        RadiusMedium = "6px",
        FontSans = "\"Space Grotesk\", system-ui, -apple-system, sans-serif",
        FontMono = "\"JetBrains Mono\", ui-monospace, SFMono-Regular, Consolas, monospace"
    };
}
