using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for the shell's drawer, driven against the documentation site's own layout
/// rather than a purpose-built fixture - the drawer is part of every page, so this is the real
/// thing under test.
/// </summary>
public class VetoDrawerTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/usage";

    private ILocator Drawer => Component("drawer");
    private ILocator Toggle => Component("drawer-toggle");

    private Task<double> DrawerWidthAsync ()
        => Drawer.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

    /// <summary>The rendered left padding of a navigation entry, which is how depth shows.</summary>
    private Task<double> IndentAsync ( string testId )
        => Component(testId).EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).paddingInlineStart)");

    [Fact]
    public async Task Opens_and_closes_at_desktop_width ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync(Route);

        await Expect(Drawer).ToHaveAttributeAsync("data-open", "true");
        Assert.True(await DrawerWidthAsync() > 100, "The drawer should start docked and visible on a wide viewport.");

        await Toggle.ClickAsync();

        await Expect(Drawer).ToHaveAttributeAsync("data-open", "false");
        await Expect(Drawer).ToHaveCSSAsync("width", "0px");

        await Toggle.ClickAsync();

        await Expect(Drawer).ToHaveAttributeAsync("data-open", "true");

        // Polled rather than measured once: the width is animated, so reading it the instant the
        // attribute flips catches the drawer part-way open.
        await Expect(Drawer).ToHaveCSSAsync("width", "240px");
    }

    [Fact]
    public async Task The_toggle_is_reachable_at_every_width ()
    {
        // The regression this covers: hiding the toggle above a breakpoint left wide-screen
        // users with a drawer they could never close.
        foreach (var width in new[] { 480, 900, 1440, 1920 })
        {
            await Page.SetViewportSizeAsync(width, 900);
            await GotoComponentAsync(Route);

            await Expect(Toggle).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Starts_closed_on_a_narrow_viewport ()
    {
        await Page.SetViewportSizeAsync(420, 900);
        await GotoComponentAsync(Route);

        // Below the breakpoint the drawer is an overlay, so starting open would cover the page.
        await Expect(Drawer).ToHaveAttributeAsync("data-open", "false");
    }

    [Fact]
    public async Task Overlays_the_content_when_opened_on_a_narrow_viewport ()
    {
        await Page.SetViewportSizeAsync(420, 900);
        await GotoComponentAsync(Route);

        await Toggle.ClickAsync();

        await Expect(Drawer).ToHaveAttributeAsync("data-open", "true");
        await Expect(Drawer).ToHaveCSSAsync("position", "fixed");

        var scrim = Component("drawer-scrim");
        await Expect(scrim).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Clicking_the_scrim_closes_the_drawer ()
    {
        await Page.SetViewportSizeAsync(420, 900);
        await GotoComponentAsync(Route);

        await Toggle.ClickAsync();
        await Expect(Drawer).ToHaveAttributeAsync("data-open", "true");

        // Clicked away from the drawer: the scrim covers the whole viewport, and its centre is
        // underneath the open drawer, which would intercept the click.
        await Component("drawer-scrim").ClickAsync(new() { Position = new() { X = 380, Y = 450 } });

        await Expect(Drawer).ToHaveAttributeAsync("data-open", "false");
    }

    [Fact]
    public async Task Escape_closes_an_overlay_drawer ()
    {
        await Page.SetViewportSizeAsync(420, 900);
        await GotoComponentAsync(Route);

        await Toggle.ClickAsync();
        await Expect(Drawer).ToHaveAttributeAsync("data-open", "true");

        // Focus moves into the drawer first, as it would for anyone who just opened it and is
        // navigating its links.
        await Drawer.FocusAsync();
        await Drawer.PressAsync("Escape");

        await Expect(Drawer).ToHaveAttributeAsync("data-open", "false");
    }

    [Fact]
    public async Task Is_docked_rather_than_floating_above_the_breakpoint ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync(Route);

        // Docked means it takes part in the layout grid, which is what stops it covering content.
        await Expect(Drawer).ToHaveCSSAsync("position", "static");
    }

    [Fact]
    public async Task The_app_bar_spans_the_whole_frame ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync(Route);

        // The regression this covers: with no explicit grid columns the app bar was auto-placed
        // into the drawer's column, so a trailing VetoSpacer pushed its content only as far as
        // the edge of the drawer rather than the edge of the window.
        var appBarRight = await Component("appbar").EvaluateAsync<double>("el => el.getBoundingClientRect().right");
        var viewportWidth = await Page.EvaluateAsync<double>("() => window.innerWidth");

        Assert.True(
            Math.Abs(appBarRight - viewportWidth) < 2,
            $"The app bar should reach the window edge ({viewportWidth}px), but stopped at {appBarRight}px.");
    }

    [Fact]
    public async Task Content_never_sits_underneath_a_docked_drawer ()
    {
        // Narrow enough that a full-width content region would be overlapped by the drawer.
        await Page.SetViewportSizeAsync(1100, 800);
        await GotoComponentAsync(Route);

        var drawerRight = await Drawer.EvaluateAsync<double>("el => el.getBoundingClientRect().right");
        var mainLeft = await Component("main").EvaluateAsync<double>("el => el.getBoundingClientRect().left");

        Assert.True(
            mainLeft >= drawerRight - 1,
            $"Content starts at {mainLeft}px, which is underneath a drawer ending at {drawerRight}px.");
    }

    [Fact]
    public async Task The_footer_sits_below_the_content_and_spans_the_frame ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync(Route);

        var footer = Component("footer");
        await Expect(footer).ToBeVisibleAsync();

        var footerTop = await footer.EvaluateAsync<double>("el => el.getBoundingClientRect().top");
        var mainBottom = await Component("main").EvaluateAsync<double>("el => el.getBoundingClientRect().bottom");
        var footerRight = await footer.EvaluateAsync<double>("el => el.getBoundingClientRect().right");
        var viewportWidth = await Page.EvaluateAsync<double>("() => window.innerWidth");

        Assert.True(footerTop >= mainBottom - 1, $"The footer ({footerTop}px) overlaps the content ({mainBottom}px).");
        Assert.True(Math.Abs(footerRight - viewportWidth) < 2, $"The footer should span the frame, but ended at {footerRight}px.");
    }

    [Fact]
    public async Task Navigation_marks_the_current_page ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync(Route);

        await Expect(Component("nav-usage")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Component("nav-installation")).Not.ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task Navigation_groups_expand_and_collapse ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync(Route);

        var group = Component("nav-components");
        await Expect(group).ToHaveAttributeAsync("aria-expanded", "true");

        await group.ClickAsync();
        await Expect(group).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task Nested_navigation_indents_by_depth ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync("docs/components/button");

        // Components > Buttons > Button. Each level indents further than the one above it, so
        // the tree shows what belongs to what.
        var top = await IndentAsync("nav-components");
        var group = await IndentAsync("nav-buttons");
        var leaf = await IndentAsync("nav-button");

        Assert.True(group > top, $"A sub-group ({group}px) should indent past its parent ({top}px).");
        Assert.True(leaf > group, $"A leaf ({leaf}px) should indent past its group ({group}px).");
    }

    [Fact]
    public async Task A_deeply_nested_page_opens_with_its_whole_branch_expanded ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync("docs/components/button");

        // Arriving by deep link should show the reader where they are, at every level.
        await Expect(Component("nav-components")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Component("nav-buttons")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Component("nav-button")).ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task The_group_holding_the_current_page_stays_open ()
    {
        await Page.SetViewportSizeAsync(1440, 900);
        await GotoComponentAsync("docs/components/card");

        // A deep link should arrive with the user's place in the tree already visible.
        await Expect(Component("nav-components")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Component("nav-layout")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Component("nav-card")).ToHaveAttributeAsync("aria-current", "page");
    }
}
