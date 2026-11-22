using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for VetoHandbookPage and VetoHandbookToc, driven against their own
/// documentation page at /docs/components/handbook-page.
/// </summary>
public class VetoHandbookPageTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/handbook-page";

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

    [Fact]
    public async Task The_page_is_listed_in_the_component_navigation ()
    {
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync(Route);

        await Expect(Component("nav-handbook-page")).ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task Sections_keep_the_anchors_they_were_given ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic-install")).ToHaveAttributeAsync("id", "install");
        await Expect(Component("basic-register")).ToHaveAttributeAsync("id", "register");
        await Expect(Component("basic-verify")).ToHaveAttributeAsync("id", "verify");
    }

    [Fact]
    public async Task Anchor_links_are_path_qualified ()
    {
        await GotoComponentAsync(Route);

        // A bare "#install" would resolve against <base href="/"> and leave the page entirely.
        var heading = Component("basic-install").Locator(".veto-handbook-page__anchor");
        await Expect(heading).ToHaveAttributeAsync("href", $"/{Route}#install");

        var railLink = Component("rail-register");
        await Expect(railLink).ToHaveAttributeAsync("href", $"/{Route}#register");
    }

    [Fact]
    public async Task The_rail_lists_exactly_the_pages_sections ()
    {
        await GotoComponentAsync(Route);

        // Both are built from one list, so the rail cannot list something the page does not have.
        await Expect(Component("rail").Locator(".veto-handbook-toc__link")).ToHaveCountAsync(3);
        await Expect(Component("rail-install")).ToHaveTextAsync("Install");
        await Expect(Component("rail-verify")).ToHaveTextAsync("Verify");
    }

    [Fact]
    public async Task Choosing_a_section_highlights_it_and_the_highlight_fades ()
    {
        await GotoComponentAsync(Route);

        Assert.False(await IsHighlightedAsync("hl-page-register"));

        await Component("hl-rail-register").ClickAsync();
        await WaitForHighlightAsync("hl-page-register", highlighted: true);

        await WaitForHighlightAsync("hl-page-register", highlighted: false, timeoutMs: 15_000);
    }

    [Fact]
    public async Task The_highlight_can_be_made_to_persist ()
    {
        await GotoComponentAsync(Route);

        await Component("opt-persist").ClickAsync();
        await Component("hl-rail-verify").ClickAsync();

        await WaitForHighlightAsync("hl-page-verify", highlighted: true);

        await Page.WaitForTimeoutAsync(7000);
        Assert.True(await IsHighlightedAsync("hl-page-verify"), "A persistent highlight should not fade.");
    }

    [Fact]
    public async Task The_highlight_can_be_switched_off ()
    {
        await GotoComponentAsync(Route);

        await Component("opt-highlight").ClickAsync();
        await Component("hl-rail-install").ClickAsync();

        await Page.WaitForTimeoutAsync(600);
        Assert.False(await IsHighlightedAsync("hl-page-install"), "Nothing should light up when the option is off.");
    }

    [Fact]
    public async Task A_page_without_shared_state_still_renders ()
    {
        await GotoComponentAsync(Route);

        // The basic example shares no state, so it must work without ever highlighting anything.
        await Expect(Component("basic")).ToBeVisibleAsync();
        await Component("rail-install").ClickAsync();

        Assert.False(await IsHighlightedAsync("basic-install"));
    }

    [Fact]
    public async Task The_parameter_reference_covers_all_three_types ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("parameters")).ToBeVisibleAsync();
        await Expect(Component("toc-parameters")).ToBeVisibleAsync();
        await Expect(Component("section-parameters")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Scroll_spy_marks_the_section_being_read ()
    {
        await GotoComponentAsync(Route);

        await Component("spy-page-spy-3").EvaluateAsync("el => el.scrollIntoView({ block: 'start' })");
        await Expect(Component("spy-rail-spy-3")).ToHaveAttributeAsync("aria-current", "true");

        await Component("spy-page-spy-1").EvaluateAsync("el => el.scrollIntoView({ block: 'start' })");
        await Expect(Component("spy-rail-spy-1")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Component("spy-rail-spy-3")).Not.ToHaveAttributeAsync("aria-current", "true");
    }

    [Fact]
    public async Task Going_back_moves_the_current_mark_back_too ()
    {
        await GotoComponentAsync(Route);

        await Component("rail-install").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#install$"));
        await Component("rail-verify").ClickAsync();
        await Expect(Component("rail-verify")).ToHaveAttributeAsync("aria-current", "true");

        await Page.GoBackAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#install$"));

        // A click used to outrank the address for good, so the rail stayed on "verify".
        await Expect(Component("rail-install")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Component("rail-verify")).Not.ToHaveAttributeAsync("aria-current", "true");
    }
}
