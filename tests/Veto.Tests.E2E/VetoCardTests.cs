using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoCard</c> and its sections, driven against /docs/components/card.</summary>
public class VetoCardTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/card";

    [Fact]
    public async Task A_composed_card_renders_its_three_sections ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("composed-header")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("composed-content")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("composed-footer")).ToBeVisibleAsync();

        await Expect(Page.GetByTestId("composed-title")).ToHaveTextAsync("Deploy to production");
        await Expect(Page.GetByTestId("composed-description")).ToContainTextAsync("Three commits are waiting");
    }

    [Fact]
    public async Task The_title_is_a_heading_and_the_description_is_not ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("h3", await Page.GetByTestId("composed-title").EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        Assert.Equal("p", await Page.GetByTestId("composed-description").EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
    }

    [Fact]
    public async Task The_automatic_body_wrapper_gets_out_of_the_way_of_the_sections ()
    {
        await GotoComponentAsync(Route);

        // Without this the sections would be padded twice - once by the wrapper, once by
        // themselves - and a media image could never reach the card's edges.
        var display = await Component("composed").Locator(".veto-card__body")
            .EvaluateAsync<string>("el => getComputedStyle(el).display");

        Assert.Equal("contents", display);
    }

    [Fact]
    public async Task The_sections_do_not_pad_each_other_twice ()
    {
        await GotoComponentAsync(Route);

        // The header already paid for the gap, so the content must not add another one on top.
        var contentTop = await Page.GetByTestId("composed-content")
            .EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).paddingTop)");

        Assert.Equal(0, contentTop);
    }

    [Fact]
    public async Task A_media_image_runs_to_the_cards_edges ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var card = await Component("media").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var image = await Page.GetByTestId("media-image").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(Math.Abs(card - image) < 1, $"The image should span the card: {image} vs {card}.");
    }

    [Fact]
    public async Task A_card_clips_its_media_to_its_own_corners ()
    {
        await GotoComponentAsync(Route);

        // Nothing has to ask for this - a square-cornered image poking out of a rounded card
        // is never what anyone wanted.
        var overflow = await Component("media").EvaluateAsync<string>("el => getComputedStyle(el).overflow");

        Assert.Contains("hidden", overflow);
    }

    [Fact]
    public async Task A_statistic_tile_puts_the_label_above_the_figure ()
    {
        await GotoComponentAsync(Route);

        var header = Component("stat-revenue").Locator(".veto-card-header");
        var order = await header.EvaluateAsync<string[]>(
            "el => Array.from(el.children).map(c => c.className.split(' ')[0])");

        // Description, then title - the order a KPI reads in.
        Assert.Equal("veto-card-description", order[0]);
        Assert.Equal("veto-card-title", order[1]);
    }

    [Fact]
    public async Task A_statistic_figure_is_not_a_heading ()
    {
        await GotoComponentAsync(Route);

        // Twenty KPI tiles promoted to <h3> would wreck the page's outline.
        var tag = await Page.GetByTestId("stat-revenue-value").EvaluateAsync<string>("el => el.tagName.toLowerCase()");

        Assert.Equal("div", tag);
        await Expect(Page.GetByTestId("stat-revenue-value")).ToHaveTextAsync("$48,290");
    }

    [Fact]
    public async Task A_statistic_figure_is_drawn_larger_than_an_ordinary_title ()
    {
        await GotoComponentAsync(Route);

        var display = await Page.GetByTestId("stat-revenue-value")
            .EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");
        var ordinary = await Page.GetByTestId("composed-title")
            .EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");

        Assert.True(display > ordinary, $"Display should out-measure a title: {display} vs {ordinary}.");
    }

    [Fact]
    public async Task The_stats_grid_reflows_by_breakpoint ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var wide = await Component("stat-revenue").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        await Page.SetViewportSizeAsync(560, 900);
        var narrow = await Component("stat-revenue").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(narrow > wide, $"One column should be wider than one of four: {narrow} vs {wide}.");
    }

    [Fact]
    public async Task The_plain_parameter_form_still_works ()
    {
        await GotoComponentAsync(Route);

        // The composed sections are an addition, not a replacement.
        await Expect(Component("slots").Locator(".veto-card__header")).ToContainTextAsync("Storage");
        await Expect(Component("slots").Locator(".veto-card__body")).ToContainTextAsync("41.2 GB");
    }

    [Fact]
    public async Task Elevation_lands_as_a_modifier_class ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("flat")).ToHaveClassAsync(new Regex(@"\bveto-card--flat\b"));
        await Expect(Component("outlined")).ToHaveClassAsync(new Regex(@"\bveto-card--outlined\b"));
        await Expect(Component("raised")).ToHaveClassAsync(new Regex(@"\bveto-card--raised\b"));
    }

    [Fact]
    public async Task An_interactive_card_is_a_real_button ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("button", await Component("clickable").EvaluateAsync<string>("el => el.tagName.toLowerCase()"));

        await Component("clickable").FocusAsync();
        await Expect(Component("clickable")).ToBeFocusedAsync();

        await Component("clickable").PressAsync("Enter");
        await Expect(Component("clickable")).ToContainTextAsync("Activated 1 times");
    }

    [Fact]
    public async Task The_footer_can_push_its_actions_to_the_end ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var justify = await Page.GetByTestId("composed-footer")
            .EvaluateAsync<string>("el => getComputedStyle(el).justifyContent");

        Assert.Equal("flex-end", justify);
    }
}
