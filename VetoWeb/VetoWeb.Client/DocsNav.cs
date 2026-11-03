using Veto.Components.Icon;
using Veto.Components.Navigation;

namespace VetoWeb.Client;

/// <summary>
/// The documentation site's navigation, defined once as data and shared by the drawer and
/// anything else that needs it.
/// </summary>
/// <remarks>
/// Components are filed under the topic they belong to - Buttons holds Button and Icon button -
/// rather than listed flat. Each level of nesting indents, so the tree shows what belongs to what.
/// </remarks>
public static class DocsNav
{
    /// <summary>Every entry in the site's navigation.</summary>
    public static readonly IReadOnlyList<VetoNavEntry> Items =
    [
        new VetoNavEntry { Label = "Overview", Href = "/", Icon = VetoIcons.Home, MatchExact = true },
        new VetoNavEntry { Label = "Installation", Href = "/docs/installation", Icon = VetoIcons.Document },
        new VetoNavEntry { Label = "Usage", Href = "/docs/usage", Icon = VetoIcons.Terminal },

        new VetoNavEntry
        {
            Label = "Components",
            Icon = VetoIcons.Grid,
            Children =
            [
                new VetoNavEntry
                {
                    Label = "Buttons",
                    Children =
                    [
                        new VetoNavEntry { Label = "Button", Href = "/docs/components/button" },
                        new VetoNavEntry { Label = "Icon button", Href = "/docs/components/icon-button" },
                        new VetoNavEntry { Label = "Button group", Href = "/docs/components/button-group" },
                        new VetoNavEntry { Label = "Button reveal", Href = "/docs/components/button-reveal" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Inputs",
                    Children =
                    [
                        new VetoNavEntry { Label = "Text box", Href = "/docs/components/text-box" },
                        new VetoNavEntry { Label = "Magic text box", Href = "/docs/components/magic-text-box" },
                        new VetoNavEntry { Label = "Switch", Href = "/docs/components/switch" },
                        new VetoNavEntry { Label = "Slider", Href = "/docs/components/slider" },
                        new VetoNavEntry { Label = "Rating", Href = "/docs/components/rating" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Navigation",
                    Children =
                    [
                        new VetoNavEntry { Label = "Link", Href = "/docs/components/link" },
                        new VetoNavEntry { Label = "Breadcrumbs", Href = "/docs/components/breadcrumbs" },
                        new VetoNavEntry { Label = "Menu", Href = "/docs/components/menu" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Feedback",
                    Children =
                    [
                        new VetoNavEntry { Label = "Toast", Href = "/docs/components/toast" },
                        new VetoNavEntry { Label = "Terminal box", Href = "/docs/components/terminal-box" },
                        new VetoNavEntry { Label = "Gauge", Href = "/docs/components/gauge" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Layout",
                    Children =
                    [
                        new VetoNavEntry { Label = "Stack", Href = "/docs/components/stack" },
                        new VetoNavEntry { Label = "Grid", Href = "/docs/components/grid" },
                        new VetoNavEntry { Label = "Container", Href = "/docs/components/container" },
                        new VetoNavEntry { Label = "Card", Href = "/docs/components/card" },
                        new VetoNavEntry { Label = "Divider", Href = "/docs/components/divider" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Content",
                    Children =
                    [
                        new VetoNavEntry { Label = "Icon", Href = "/docs/components/icon" },
                        new VetoNavEntry { Label = "Keyboard key", Href = "/docs/components/kbd" },
                        new VetoNavEntry { Label = "Handbook page", Href = "/docs/components/handbook-page" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Data",
                    Children =
                    [
                        new VetoNavEntry { Label = "Table", Href = "/docs/components/table" },
                        new VetoNavEntry { Label = "Gantt", Href = "/docs/components/gantt" },
                        new VetoNavEntry { Label = "Cron", Href = "/docs/components/cron" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Feedback",
                    Children =
                    [
                        new VetoNavEntry { Label = "Loading", Href = "/docs/components/loading" },
                        new VetoNavEntry { Label = "Blink text", Href = "/docs/components/blink-text" }
                    ]
                },
                new VetoNavEntry
                {
                    Label = "Effects",
                    Children =
                    [
                        new VetoNavEntry { Label = "Glow and ping", Href = "/docs/components/effects" },
                        new VetoNavEntry { Label = "Tilt 3D", Href = "/docs/components/tilt" }
                    ]
                }
            ]
        },

        new VetoNavEntry
        {
            Label = "Theming",
            Icon = VetoIcons.Palette,
            Children =
            [
                new VetoNavEntry { Label = "Theme provider", Href = "/docs/theming/provider" },
                new VetoNavEntry { Label = "Tokens", Href = "/docs/theming/tokens" }
            ]
        },

        new VetoNavEntry
        {
            Label = "Layouts",
            Icon = VetoIcons.Sidebar,
            Children =
            [
                new VetoNavEntry { Label = "Gallery", Href = "/docs/layouts", MatchExact = true },
                new VetoNavEntry { Label = "Shell reference", Href = "/docs/layouts/shell" },
                new VetoNavEntry
                {
                    Label = "Live demos",
                    Children =
                    [
                        new VetoNavEntry { Label = "Shell", Href = "/layouts/shell" },
                        new VetoNavEntry { Label = "Docs", Href = "/layouts/docs" },
                        new VetoNavEntry { Label = "Dock", Href = "/layouts/dock" },
                        new VetoNavEntry { Label = "Dashboard", Href = "/layouts/dashboard" }
                    ]
                }
            ]
        }
    ];
}