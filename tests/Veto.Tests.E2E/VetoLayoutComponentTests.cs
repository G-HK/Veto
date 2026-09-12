using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for the layout components and icons, driven against /components/layout.
/// These assert computed layout rather than class names wherever the class name would not
/// prove the component actually laid anything out.
/// </summary>
public class VetoLayoutComponentTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "components/layout";

    private Task<string> ComputedAsync ( string testId, string property )
        => Component(testId).EvaluateAsync<string>($"el => getComputedStyle(el).{property}");

    [Fact]
    public async Task Stack_lays_children_out_on_the_requested_axis ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("flex", await ComputedAsync("stack-row", "display"));
        Assert.Equal("row", await ComputedAsync("stack-row", "flexDirection"));
        Assert.Equal("column", await ComputedAsync("stack-column", "flexDirection"));
    }

    [Fact]
    public async Task Stack_gap_comes_from_the_spacing_scale ()
    {
        await GotoComponentAsync(Route);

        // VetoSpacing.Medium is --veto-space-3, 0.75rem, which is 12px at the default root size.
        Assert.Equal("12px", await ComputedAsync("stack-row", "columnGap"));
    }

    [Fact]
    public async Task Spacer_absorbs_the_free_space ()
    {
        await GotoComponentAsync(Route);

        var width = await Component("spacer").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(width > 1, $"A flexible spacer should take up the slack, but it was {width}px wide.");
    }

    [Fact]
    public async Task Grid_column_count_follows_the_viewport ()
    {
        await GotoComponentAsync(Route);

        await Page.SetViewportSizeAsync(400, 900);
        Assert.Single((await ComputedAsync("grid", "gridTemplateColumns")).Split(' '));

        await Page.SetViewportSizeAsync(800, 900);
        Assert.Equal(2, (await ComputedAsync("grid", "gridTemplateColumns")).Split(' ').Length);

        await Page.SetViewportSizeAsync(1400, 900);
        Assert.Equal(4, (await ComputedAsync("grid", "gridTemplateColumns")).Split(' ').Length);
    }

    [Fact]
    public async Task Grid_item_spans_more_than_one_column_at_its_breakpoint ()
    {
        await GotoComponentAsync(Route);
        await Page.SetViewportSizeAsync(1400, 900);

        // Spanning two of four columns makes the item wider than a plain single-column cell.
        var spanned = await Component("grid-wide").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var single = await Component("card-outlined").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(spanned > single, $"Spanned cell ({spanned}px) should be wider than a single cell ({single}px).");
    }

    [Fact]
    public async Task Card_renders_a_button_only_when_it_is_interactive ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("div", await Component("card-outlined").EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        Assert.Equal("button", await Component("card-interactive").EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
    }

    [Fact]
    public async Task Interactive_card_is_operable_from_the_keyboard ()
    {
        await GotoComponentAsync(Route);

        await Component("card-interactive").FocusAsync();
        await Expect(Component("card-interactive")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.GetByTestId("card-output")).ToHaveTextAsync("Card activated");
    }

    [Fact]
    public async Task Card_elevations_are_visually_distinct ()
    {
        await GotoComponentAsync(Route);

        Assert.NotEqual("none", await ComputedAsync("card-raised", "boxShadow"));
        Assert.Equal("none", await ComputedAsync("card-outlined", "boxShadow"));
    }

    [Fact]
    public async Task Card_footer_is_rendered_in_its_own_region ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("card-footer").Locator(".veto-card__footer")).ToHaveTextAsync("Footer content");
        await Expect(Component("card-footer").Locator(".veto-card__header")).ToContainTextAsync("With a footer");
    }

    [Fact]
    public async Task Plain_divider_is_an_hr_and_a_labelled_one_is_not ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("hr", await Component("divider-plain").EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        await Expect(Component("divider-labelled")).ToHaveAttributeAsync("role", "separator");
        await Expect(Component("divider-labelled").Locator(".veto-divider__label")).ToHaveTextAsync("or");
    }

    [Fact]
    public async Task Divider_reports_the_orientation_of_the_line_itself ()
    {
        await GotoComponentAsync(Route);

        // aria-orientation describes the rule, not the axis of the content it separates.
        await Expect(Component("divider-plain")).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await Expect(Component("divider-vertical")).ToHaveAttributeAsync("aria-orientation", "vertical");
    }

    [Fact]
    public async Task Container_constrains_width_and_centres_itself ()
    {
        await GotoComponentAsync(Route);
        await Page.SetViewportSizeAsync(1400, 900);

        var width = await Component("container").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(width <= 480 + 1, $"Small container should cap at 30rem, but it was {width}px.");

        // getComputedStyle resolves `margin: auto` to the pixels it worked out to, so equal
        // non-zero margins either side is what actually proves the container is centred.
        var left = await Component("container").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).marginLeft)");
        var right = await Component("container").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).marginRight)");

        Assert.True(left > 0, $"Expected the container to be centred, but its left margin was {left}px.");
        Assert.True(Math.Abs(left - right) < 1, $"Margins should match when centred: {left}px vs {right}px.");
    }

    [Fact]
    public async Task Decorative_icons_are_hidden_from_assistive_technology ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("icon-decorative")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Component("icon-decorative").Locator("title")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_titled_icon_becomes_an_image_with_an_accessible_name ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("icon-labelled")).ToHaveAttributeAsync("role", "img");
        await Expect(Component("icon-labelled")).ToHaveAttributeAsync("aria-label", "Delete");
        await Expect(Component("icon-labelled").Locator("title")).ToHaveTextAsync("Delete");
    }

    [Fact]
    public async Task Icon_rotation_is_applied ()
    {
        await GotoComponentAsync(Route);

        var rotation = await Component("icon-rotated")
            .EvaluateAsync<string>("el => getComputedStyle(el).rotate");

        Assert.Equal("180deg", rotation);
    }

    [Fact]
    public async Task Icon_button_carries_its_label_as_an_accessible_name ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("icon-button")).ToHaveAttributeAsync("aria-label", "Search");
        await Expect(Component("icon-button")).ToHaveAttributeAsync("title", "Search");
        await Expect(Component("icon-button")).ToHaveAttributeAsync("type", "button");
    }

    [Fact]
    public async Task Icon_button_raises_its_click ()
    {
        await GotoComponentAsync(Route);

        await Component("icon-button").ClickAsync();
        await Expect(Page.GetByTestId("icon-button-output")).ToHaveTextAsync("Search clicked");
    }

    [Fact]
    public async Task Toggle_icon_button_reports_state_through_aria_pressed ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("icon-button-toggle")).ToHaveAttributeAsync("aria-pressed", "false");
        await Component("icon-button-toggle").ClickAsync();
        await Expect(Component("icon-button-toggle")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Fact]
    public async Task Disabled_icon_button_cannot_be_activated ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("icon-button-disabled")).ToBeDisabledAsync();
    }
}
