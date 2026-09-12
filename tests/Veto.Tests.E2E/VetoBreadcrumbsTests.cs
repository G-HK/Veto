using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoBreadcrumbs</c>, driven against /docs/components/breadcrumbs.</summary>
public class VetoBreadcrumbsTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/breadcrumbs";

    private ILocator Steps ( string testId ) => Component(testId).Locator(".veto-breadcrumbs__item");

    private ILocator Separators ( string testId ) => Component(testId).Locator(".veto-breadcrumbs__separator");

    [Fact]
    public async Task Renders_an_ordered_list_inside_a_named_nav ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic")).ToHaveAttributeAsync("aria-label", "Breadcrumb");
        await Expect(Component("basic").Locator("ol")).ToHaveCountAsync(1);
        await Expect(Steps("basic")).ToHaveCountAsync(4);
    }

    [Fact]
    public async Task Every_step_but_the_last_is_a_link ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic").Locator("a")).ToHaveCountAsync(3);
        await Expect(Page.GetByTestId("basic-item-0").Locator("a")).ToHaveAttributeAsync("href", "/");
        await Expect(Page.GetByTestId("basic-item-1").Locator("a")).ToHaveAttributeAsync("href", "/docs");
    }

    [Fact]
    public async Task The_last_step_is_the_current_page ()
    {
        await GotoComponentAsync(Route);

        var last = Page.GetByTestId("basic-item-3");

        await Expect(last).ToHaveAttributeAsync("data-current", "true");
        await Expect(last.Locator("a")).ToHaveCountAsync(0);
        await Expect(last.Locator(".veto-breadcrumbs__text")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(last).ToContainTextAsync("Breadcrumbs");
    }

    [Fact]
    public async Task A_separator_sits_between_steps_and_nowhere_else ()
    {
        await GotoComponentAsync(Route);

        // Four steps, three separators.
        await Expect(Separators("basic")).ToHaveCountAsync(3);
        await Expect(Separators("basic").First).ToHaveTextAsync("/");
        await Expect(Separators("basic").First).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task The_separator_can_be_any_character_or_an_icon ()
    {
        await GotoComponentAsync(Route);

        await Expect(Separators("sep-arrow").First).ToHaveTextAsync("›");
        await Expect(Separators("sep-dot").First).ToHaveTextAsync("•");
        await Expect(Separators("sep-chevron").First.Locator("svg")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Steps_written_as_children_lay_out_like_the_list ()
    {
        await GotoComponentAsync(Route);

        await Expect(Steps("children")).ToHaveCountAsync(3);
        await Expect(Separators("children")).ToHaveCountAsync(2);

        await Expect(Page.GetByTestId("children-item-0").Locator("a")).ToHaveAttributeAsync("href", "/");
        await Expect(Page.GetByTestId("children-item-0")).ToContainTextAsync("Home");

        // The last child carries markup of its own, which is the reason to use this form.
        await Expect(Page.GetByTestId("children-item-2")).ToHaveAttributeAsync("data-current", "true");
        await Expect(Page.GetByTestId("children-item-2").Locator(".demo-count")).ToContainTextAsync("new");
    }

    [Fact]
    public async Task A_trail_without_links_renders_no_links_at_all ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("path")).ToHaveAttributeAsync("aria-label", "File path");
        await Expect(Steps("path")).ToHaveCountAsync(4);
        await Expect(Component("path").Locator("a")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("path-item-3")).ToContainTextAsync("VetoBreadcrumbs.razor");
    }

    [Fact]
    public async Task A_long_trail_collapses_its_middle ()
    {
        await GotoComponentAsync(Route);

        // Six steps, kept to one before and two after plus the expander.
        await Expect(Steps("collapsed")).ToHaveCountAsync(4);
        await Expect(Page.GetByTestId("collapsed-expand")).ToHaveTextAsync("…");
        await Expect(Page.GetByTestId("collapsed-expand"))
            .ToHaveAttributeAsync("aria-label", "Show the rest of the trail");
    }

    [Fact]
    public async Task The_expander_restores_the_hidden_steps ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("collapsed-expand").ClickAsync();

        await Expect(Steps("collapsed")).ToHaveCountAsync(6);
        await Expect(Page.GetByTestId("collapsed-expand")).ToHaveCountAsync(0);
        await Expect(Component("collapsed")).ToContainTextAsync("Navigation");
    }

    [Fact]
    public async Task Size_lands_as_a_modifier_class ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("size-sm")).ToHaveClassAsync(new Regex(@"\bveto-breadcrumbs--sm\b"));
        await Expect(Component("size-lg")).ToHaveClassAsync(new Regex(@"\bveto-breadcrumbs--lg\b"));
    }

    [Fact]
    public async Task Activating_a_step_raises_OnItemClick ()
    {
        await GotoComponentAsync(Route);

        // These steps carry no Href - the trail drives state, not the router - so each one
        // renders as a button instead.
        await Expect(Component("clickable").Locator("a")).ToHaveCountAsync(0);
        await Page.GetByTestId("clickable-item-1").Locator("button").ClickAsync();

        await Expect(Page.GetByTestId("clicked")).ToHaveTextAsync("Clicked: Delivery");
    }
}
