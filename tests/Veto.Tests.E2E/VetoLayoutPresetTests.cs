using System.Globalization;
using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for the three layout presets and the gallery that lists them, driven
/// against the demo pages that run each layout full-screen.
/// </summary>
public class VetoLayoutPresetTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private ILocator DockItem ( string testId ) => Component(testId);

    private async Task<double> WidthAsync ( ILocator locator )
        => await locator.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

    private Task<bool> IsHighlightedAsync ( string testId )
        => Component(testId).EvaluateAsync<bool>(
            "el => el.classList.contains('veto-handbook-page__section--highlighted')");

    /// <summary>Polls until a section reaches the expected highlight state, or gives up.</summary>
    private async Task WaitForHighlightAsync ( string testId, bool highlighted, int timeoutMs = 5_000 )
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsHighlightedAsync(testId) == highlighted)
            {
                return;
            }

            await Page.WaitForTimeoutAsync(150);
        }

        Assert.Fail($"'{testId}' never became {(highlighted ? "highlighted" : "unhighlighted")} within {timeoutMs}ms.");
    }

    // ------------------------------------------------------------------ Gallery

    [Fact]
    public async Task The_gallery_lists_every_layout ()
    {
        await GotoComponentAsync("docs/layouts");

        foreach (var slug in new[] { "shell", "docs", "dock", "dashboard" })
        {
            await Expect(Component($"card-{slug}")).ToBeVisibleAsync();
            await Expect(Component($"open-{slug}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Every_link_in_a_demo_navigation_goes_somewhere ()
    {
        // The regression this covers: the demo navigations pointed at routes that did not exist,
        // so clicking an entry did nothing at all.
        await Page.SetViewportSizeAsync(1400, 900);

        foreach (var route in new[] { "layouts/shell", "layouts/dashboard", "layouts/docs" })
        {
            await GotoComponentAsync(route);

            var links = await Page.Locator(".veto-nav-menu__link[href]").AllAsync();
            Assert.NotEmpty(links);

            foreach (var link in links)
            {
                var href = await link.GetAttributeAsync("href");

                if (string.IsNullOrWhiteSpace(href) || href.StartsWith("http"))
                {
                    continue;
                }

                await link.ClickAsync();

                // Blazor's router renders its not-found content in place rather than failing, so
                // a dead link shows as that text instead of an error.
                await Expect(Page.Locator("body")).Not.ToContainTextAsync("Sorry, there's nothing at this address");
                await Expect(Component("page")).ToBeVisibleAsync();
            }
        }
    }

    [Fact]
    public async Task The_shell_demo_switches_drawer_variant ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/shell");

        await Component("variant-temporary").ClickAsync();

        // A temporary drawer overlays, so it is opened deliberately rather than arriving open
        // on top of the content it is meant to sit beside.
        await Expect(Component("drawer")).ToHaveAttributeAsync("data-open", "false");
        await Expect(Component("drawer")).ToHaveCSSAsync("position", "fixed");

        await Component("variant-persistent").ClickAsync();
        await Expect(Component("drawer")).ToHaveCSSAsync("position", "static");
    }

    [Fact]
    public async Task The_shell_demo_swaps_the_drawer_to_the_other_side ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/shell");

        var drawerLeft = await Component("drawer").EvaluateAsync<double>("el => el.getBoundingClientRect().left");

        await Component("swap-side").ClickAsync();
        await Expect(Component("state-anchor")).ToHaveTextAsync("Right");

        var swapped = await Component("drawer").EvaluateAsync<double>("el => el.getBoundingClientRect().left");
        Assert.True(swapped > drawerLeft, $"The drawer should move to the right, but went {drawerLeft}px -> {swapped}px.");
    }

    [Fact]
    public async Task Dashboard_toolbar_buttons_do_something ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dashboard");

        await Component("tool-search").ClickAsync();
        await Expect(Component("status")).ToHaveTextAsync("Search clicked");

        await Component("tile-configure").ClickAsync();
        await Expect(Component("status")).ToHaveTextAsync("Configure throughput clicked");
    }

    [Fact]
    public async Task Dashboard_navigation_changes_the_page ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dashboard");

        await Component("layout-nav-services").ClickAsync();

        await Expect(Component("section-panel")).ToBeVisibleAsync();
        await Expect(Component("layout-nav-services")).ToHaveAttributeAsync("aria-current", "page");
    }

    // ------------------------------------------------------------------ Dock

    [Fact]
    public async Task Dock_items_are_rendered_at_their_configured_size ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");

        // The regression this covers: passing a string parameter without an @ handed the dock the
        // literal text "ItemSize", which produced an invalid custom property and collapsed every
        // item to the size of its icon.
        var width = await WidthAsync(DockItem("dock-home"));

        Assert.True(width > 40, $"Dock items should be about 3rem, but one measured {width}px.");
    }

    [Fact]
    public async Task Dock_items_activate ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");

        await DockItem("dock-terminal").ClickAsync();

        await Expect(Page.GetByTestId("opened-value")).ToHaveTextAsync("Terminal opened");
    }

    [Fact]
    public async Task The_dock_moves_to_every_edge ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");

        var dock = Component("layout-dock");
        var viewport = await Page.EvaluateAsync<double>("() => window.innerHeight");

        await Component("position-top").ClickAsync();
        var top = await dock.EvaluateAsync<double>("el => el.getBoundingClientRect().top");
        Assert.True(top < viewport / 2, $"A top dock should sit in the upper half, but was at {top}px.");

        await Component("position-bottom").ClickAsync();
        var bottom = await dock.EvaluateAsync<double>("el => el.getBoundingClientRect().bottom");
        Assert.True(bottom > viewport / 2, $"A bottom dock should sit in the lower half, but ended at {bottom}px.");

        await Component("position-left").ClickAsync();
        await Expect(Component("layout-dock")).ToHaveClassAsync(new Regex(@"\bveto-dock--left\b"));

        await Component("position-right").ClickAsync();
        await Expect(Component("layout-dock")).ToHaveClassAsync(new Regex(@"\bveto-dock--right\b"));
    }

    [Fact]
    public async Task A_hovered_dock_item_comes_to_the_front ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");

        // Magnified neighbours overlap; the hovered one is lifted so none paints over its label.
        await Component("dock-files").HoverAsync();

        var z = await Component("dock-files").EvaluateAsync<string>(
            "el => getComputedStyle(el.closest('.veto-dock__item') ?? el).zIndex");
        Assert.Equal("1000", z);
    }

    [Fact]
    public async Task Magnification_swells_the_item_under_the_pointer ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");

        var resting = await WidthAsync(DockItem("dock-terminal"));

        var box = await DockItem("dock-terminal").BoundingBoxAsync()
            ?? throw new InvalidOperationException("The dock item has no box.");
        await Page.Mouse.MoveAsync((float) (box.X + box.Width / 2), (float) (box.Y + box.Height / 2), new() { Steps = 4 });

        // Polled: the scale is applied through a 90ms transition, so reading the computed
        // transform in the same tick as the pointer event catches it at its starting value.
        await Expect(DockItem("dock-terminal")).Not.ToHaveCSSAsync("transform", "matrix(1, 0, 0, 1, 0, 0)");

        var magnified = await WidthAsync(DockItem("dock-terminal"));

        Assert.True(magnified > resting, $"Hovering should magnify the item, but it went {resting}px -> {magnified}px.");

        // The swell falls away across neighbours rather than snapping - that is what makes it a
        // dock rather than a row of buttons that grow on hover.
        var neighbour = await WidthAsync(DockItem("dock-files"));
        Assert.True(neighbour > resting && neighbour < magnified,
            $"The neighbour ({neighbour}px) should sit between resting ({resting}px) and magnified ({magnified}px).");
    }

    [Fact]
    public async Task The_flat_appearance_does_not_magnify ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");
        await Component("appearance-flat").ClickAsync();

        var resting = await WidthAsync(DockItem("dock-terminal"));

        var box = await DockItem("dock-terminal").BoundingBoxAsync()
            ?? throw new InvalidOperationException("The dock item has no box.");
        await Page.Mouse.MoveAsync((float) (box.X + box.Width / 2), (float) (box.Y + box.Height / 2), new() { Steps = 4 });

        // Given a moment to be sure nothing happens, rather than reading before it could have.
        await Page.WaitForTimeoutAsync(250);

        Assert.Equal(resting, await WidthAsync(DockItem("dock-terminal")), 1);
    }

    [Fact]
    public async Task The_dock_is_a_toolbar_with_one_tab_stop ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");

        // A toolbar takes a single tab stop and moves between items with the arrow keys, rather
        // than making someone tab through every icon to get past the dock.
        var tabbable = await Page.Locator(".veto-dock__item[tabindex='0']").CountAsync();
        Assert.Equal(1, tabbable);

        await DockItem("dock-home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(DockItem("dock-files")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(DockItem("dock-settings")).ToBeFocusedAsync();

        // Wraps rather than stopping dead at the end.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(DockItem("dock-home")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Auto_hide_slides_the_dock_away_and_focus_brings_it_back ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dock");

        var bar = Page.Locator(".veto-dock__bar");
        var resting = await bar.EvaluateAsync<double>("el => el.getBoundingClientRect().bottom");

        await Component("auto-hide").ClickAsync();
        await Page.Mouse.MoveAsync(700, 200);

        var hidden = await bar.EvaluateAsync<double>("el => el.getBoundingClientRect().bottom");
        Assert.True(hidden > resting, $"An auto-hidden dock should move off the edge, but went {resting}px -> {hidden}px.");

        // Keyboard users must be able to reach it too, not only the pointer.
        await DockItem("dock-home").FocusAsync();
        await Expect(bar).ToHaveCSSAsync("transform", "none");
    }

    // ------------------------------------------------------------------ Dashboard

    [Fact]
    public async Task Dashboard_tiles_reflow_by_breakpoint ()
    {
        await GotoComponentAsync("layouts/dashboard");

        await Page.SetViewportSizeAsync(1400, 900);
        Assert.Equal(4, (await Component("grid").EvaluateAsync<string>(
            "el => getComputedStyle(el).gridTemplateColumns")).Split(' ').Length);

        await Page.SetViewportSizeAsync(1000, 900);
        Assert.Equal(2, (await Component("grid").EvaluateAsync<string>(
            "el => getComputedStyle(el).gridTemplateColumns")).Split(' ').Length);

        await Page.SetViewportSizeAsync(500, 900);
        Assert.Single((await Component("grid").EvaluateAsync<string>(
            "el => getComputedStyle(el).gridTemplateColumns")).Split(' '));
    }

    [Fact]
    public async Task A_wide_tile_is_wider_than_a_single_one ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dashboard");

        var wide = await WidthAsync(Component("tile-throughput"));
        var single = await WidthAsync(Component("tile-requests"));

        Assert.True(wide > single, $"A two-column tile ({wide}px) should be wider than a one-column tile ({single}px).");
    }

    [Fact]
    public async Task A_tile_span_never_exceeds_the_column_count ()
    {
        // A tile asking for two columns in a one-column grid must not overflow the grid.
        await Page.SetViewportSizeAsync(500, 900);
        await GotoComponentAsync("layouts/dashboard");

        var tile = await WidthAsync(Component("tile-throughput"));
        var grid = await WidthAsync(Component("grid"));

        Assert.True(tile <= grid + 1, $"Tile ({tile}px) overflows its grid ({grid}px).");
    }

    [Fact]
    public async Task Dashboard_tiles_are_announced_as_a_list ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/dashboard");

        await Expect(Component("grid")).ToHaveAttributeAsync("role", "list");
        await Expect(Component("tile-requests")).ToHaveAttributeAsync("role", "listitem");
        await Expect(Component("tile-requests")).ToHaveAttributeAsync("aria-label", "Requests");
    }

    // ------------------------------------------------------------------ Docs

    [Fact]
    public async Task The_docs_layout_shows_its_contents_rail_only_when_there_is_room ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/docs");

        await Expect(Component("toc-reading")).ToBeVisibleAsync();

        // Below 1280px the rail is dropped rather than stacked above the content it describes.
        await Page.SetViewportSizeAsync(1000, 900);
        await Expect(Component("toc-reading")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Each_docs_page_has_its_own_content_and_contents_rail ()
    {
        // The regression this covers: every route resolved but rendered identical content under
        // a different heading, so navigating looked as though nothing had happened.
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs");

        await Expect(Component("article").Locator("h1")).ToHaveTextAsync("Handbook");
        await Expect(Component("toc-reading")).ToBeVisibleAsync();

        await Component("layout-nav-getting-started").ClickAsync();

        await Expect(Component("article").Locator("h1")).ToHaveTextAsync("Getting started");
        await Expect(Component("article")).ToContainTextAsync("Guides");

        // The contents rail follows the page rather than listing another page's headings.
        await Expect(Component("toc-install")).ToBeVisibleAsync();
        await Expect(Component("toc-reading")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_reading_column_and_the_contents_rail_stay_together ()
    {
        // Sizing the column to the window rather than to the text left a wide dead gap between
        // the writing and the rail, which made the two look unrelated.
        await Page.SetViewportSizeAsync(1600, 900);
        await GotoComponentAsync("layouts/docs");

        var articleRight = await Page.Locator(".veto-docs-layout__article")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().right");
        var tocLeft = await Page.Locator(".veto-docs-layout__toc")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().left");

        var gap = tocLeft - articleRight;
        Assert.True(gap < 140, $"The rail should sit beside the text, but the gap was {gap}px.");
    }

    [Fact]
    public async Task A_contents_link_scrolls_the_page_instead_of_leaving_it ()
    {
        // The regression this covers: with <base href="/"> a bare "#id" link resolves against
        // the base rather than the current URL, so every contents link went to the home page.
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs/start");

        var link = Component("toc-first");
        var href = await link.GetAttributeAsync("href");

        Assert.Equal("/layouts/docs/start#first", href);

        await link.ClickAsync();

        // Still on the same page, not bounced to the root.
        Assert.Contains("/layouts/docs/start", Page.Url);
        await Expect(Component("article")).ToBeVisibleAsync();

        // And the section is actually in view, which is what the link is for.
        var inView = await Component("article-first").EvaluateAsync<bool>(@"el => {
            const box = el.getBoundingClientRect();
            return box.top >= -2 && box.top < window.innerHeight;
        }");

        Assert.True(inView, "Following a contents link should bring its section into view.");
    }

    [Fact]
    public async Task A_section_keeps_the_anchor_it_was_given ()
    {
        // Components render their own id after splatting attributes, so a consumer's id used to
        // be silently replaced by a generated one - leaving every anchor pointing at nothing.
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs/start");

        await Expect(Component("article-install")).ToHaveAttributeAsync("id", "install");
        await Expect(Component("article-register")).ToHaveAttributeAsync("id", "register");
    }

    [Fact]
    public async Task The_contents_rail_marks_the_section_being_read ()
    {
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs/start");

        await Component("toc-register").ClickAsync();

        await Expect(Component("toc-register")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Component("toc-install")).Not.ToHaveAttributeAsync("aria-current", "true");
    }

    [Fact]
    public async Task The_landing_section_is_highlighted_and_then_fades ()
    {
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs/start");

        Assert.False(await IsHighlightedAsync("article-first"), "Nothing should be lit before a section is chosen.");

        await Component("toc-first").ClickAsync();
        await WaitForHighlightAsync("article-first", highlighted: true);

        // Fades on its own after the default five seconds. The wait must comfortably exceed
        // that, or the check races the very fade it is looking for.
        await WaitForHighlightAsync("article-first", highlighted: false, timeoutMs: 15_000);
    }

    [Fact]
    public async Task The_highlight_can_be_made_to_stay ()
    {
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs/start");

        await Component("toggle-persist").ClickAsync();
        await Component("toc-register").ClickAsync();

        await WaitForHighlightAsync("article-register", highlighted: true);

        // Still lit well past the point at which it would otherwise have faded.
        await Page.WaitForTimeoutAsync(7000);
        Assert.True(await IsHighlightedAsync("article-register"), "A persistent highlight should not fade.");
    }

    [Fact]
    public async Task The_highlight_can_be_turned_off ()
    {
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs/start");

        await Component("toggle-highlight").ClickAsync();
        await Component("toc-first").ClickAsync();
        await Page.WaitForTimeoutAsync(600);
        Assert.False(await IsHighlightedAsync("article-first"), "The highlight was turned off, so nothing should light up.");
    }

    [Fact]
    public async Task Sections_are_long_enough_that_an_anchor_actually_scrolls ()
    {
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync("layouts/docs/start");

        // A page shorter than the viewport cannot demonstrate a contents rail at all.
        var scrollable = await Page.Locator(".veto-main-content")
            .EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight + 200");

        Assert.True(scrollable, "The demo page should be long enough for the anchors to move it.");
    }

    [Fact]
    public async Task The_docs_layout_renders_its_own_footer ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync("layouts/docs");

        await Expect(Component("layout-footer")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Every_preset_is_built_from_the_shell ()
    {
        await Page.SetViewportSizeAsync(1400, 900);

        foreach (var (route, testId) in new[] { ("layouts/docs", "layout"), ("layouts/dashboard", "layout") })
        {
            await GotoComponentAsync(route);

            // The presets are thin wrappers, not parallel implementations: the same app bar,
            // drawer and content region are underneath each of them.
            await Expect(Component($"{testId}-appbar")).ToBeVisibleAsync();
            await Expect(Component($"{testId}-drawer")).ToBeVisibleAsync();
            await Expect(Component($"{testId}-main")).ToBeVisibleAsync();
        }
    }
}
