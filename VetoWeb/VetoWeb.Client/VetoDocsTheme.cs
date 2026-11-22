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
            // 6.2:1 on white, so it is legible as body-weight text and not only as a fill.
            Primary = "#C2121F",
            PrimaryHover = "#9C0D18",
            PrimaryContrast = "#FFFFFF",
            Secondary = "#6B5F61",

            Surface = "#FFFFFF",
            SurfaceRaised = "#FAF7F7",
            Text = "#1A1517",
            TextMuted = "#6B5F61",
            Border = "#E7DDDE",
            // Field and switch edges: 3.8:1 on white, where the divider colour above is 1.3:1.
            BorderStrong = "#8F7F81",

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
            BorderStrong = "#76696B",

            Danger = "#FF8A8A",
            Warning = "#FBBF24",
            Success = "#4ADE80"
        },

        RadiusMedium = "6px",
        FontSans = "\"Space Grotesk\", system-ui, -apple-system, sans-serif",
        FontMono = "\"JetBrains Mono\", ui-monospace, SFMono-Regular, Consolas, monospace"
    };

    /// <summary>
    /// The custom themes offered on the theme provider page, the site's own red first. Each
    /// changes only the accent and keeps the site's type and radius; the neutrals fall back to
    /// the library's. No green, amber or red: those already mean success, warning and danger.
    /// Every accent is at least 5.4:1 against its surface and against the text drawn on it.
    /// </summary>
    public static readonly IReadOnlyList<(string Name, VetoTheme Theme)> Presets =
    [
        ("Veto red", Theme),
        ("Ocean", Accent("#1D4ED8", "#1E40AF", "#60A5FA", "#93C5FD", "#0B1220")),
        ("Teal", Accent("#0F766E", "#115E59", "#2DD4BF", "#5EEAD4", "#042F2E")),
        ("Violet", Accent("#6D28D9", "#5B21B6", "#A78BFA", "#C4B5FD", "#1E1038")),
        ("Fuchsia", Accent("#A21CAF", "#86198F", "#E879F9", "#F0ABFC", "#2A0730"))
    ];

    private static VetoTheme Accent ( string light, string lightHover, string dark, string darkHover, string darkContrast )
        => Theme with
        {
            Light = new VetoPalette { Primary = light, PrimaryHover = lightHover, PrimaryContrast = "#FFFFFF" },
            Dark = new VetoPalette { Primary = dark, PrimaryHover = darkHover, PrimaryContrast = darkContrast }
        };
}