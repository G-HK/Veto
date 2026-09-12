using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoButtonGroup</c>, driven against /docs/components/button-group.</summary>
public class VetoButtonGroupTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/button-group";

    private static async Task<double> HeightAsync ( ILocator locator )
        => await locator.EvaluateAsync<double>("el => el.getBoundingClientRect().height");

    [Fact]
    public async Task Reports_itself_as_a_named_group ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic")).ToHaveAttributeAsync("role", "group");
        await Expect(Component("basic")).ToHaveAttributeAsync("aria-label", "View mode");
        await Expect(Component("basic").Locator("button")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task An_attached_group_rounds_only_its_outer_corners ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic")).ToHaveClassAsync(new Regex(@"\bveto-button-group--attached\b"));

        var first = await Component("basic-day").EvaluateAsync<string>(
            "el => getComputedStyle(el).borderTopLeftRadius");
        var firstRight = await Component("basic-day").EvaluateAsync<string>(
            "el => getComputedStyle(el).borderTopRightRadius");
        var middle = await Component("basic-week").EvaluateAsync<string>(
            "el => getComputedStyle(el).borderTopLeftRadius");
        var last = await Component("basic-month").EvaluateAsync<string>(
            "el => getComputedStyle(el).borderTopRightRadius");

        Assert.NotEqual("0px", first);
        Assert.Equal("0px", firstRight);
        Assert.Equal("0px", middle);
        Assert.NotEqual("0px", last);
    }

    [Fact]
    public async Task Size_is_set_once_and_every_button_in_the_group_takes_it ()
    {
        await GotoComponentAsync(Route);

        var small = Component("size-sm").Locator("button");
        var large = Component("size-lg").Locator("button");

        // Uniform within a group...
        var firstSmall = await HeightAsync(small.Nth(0));
        var lastSmall = await HeightAsync(small.Nth(2));
        Assert.True(Math.Abs(firstSmall - lastSmall) < 0.5, $"Small group is uneven: {firstSmall} vs {lastSmall}.");

        // ...and the three sizes are actually different heights.
        var medium = await HeightAsync(Component("size-md").Locator("button").First);
        var big = await HeightAsync(large.First);

        Assert.True(firstSmall < medium, $"Small should be shorter than medium: {firstSmall} vs {medium}.");
        Assert.True(medium < big, $"Medium should be shorter than large: {medium} vs {big}.");
    }

    [Fact]
    public async Task Separators_can_be_drawn_between_every_button ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("sep-all")).ToHaveClassAsync(new Regex(@"\bveto-button-group--separated\b"));

        // No separator element at all - the line is a border on the following neighbour.
        await Expect(Component("sep-all").Locator(".veto-button-group__separator")).ToHaveCountAsync(0);

        var second = await Component("sep-all").Locator("button").Nth(1)
            .EvaluateAsync<string>("el => getComputedStyle(el).borderLeftWidth");

        Assert.Equal("1px", second);
    }

    [Fact]
    public async Task A_separator_can_be_placed_between_only_some_of_them ()
    {
        await GotoComponentAsync(Route);

        var separators = Component("sep-some").Locator(".veto-button-group__separator");

        await Expect(separators).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId("sep-some-line")).ToHaveAttributeAsync("role", "separator");
        await Expect(Page.GetByTestId("sep-some-line")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task A_group_label_is_text_rather_than_a_button ()
    {
        await GotoComponentAsync(Route);

        var label = Page.GetByTestId("text-zoom-label");

        await Expect(label).ToHaveTextAsync("Zoom");
        Assert.Equal("span", await label.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));

        // Muted, so it does not read as a button that simply refuses to work.
        var background = await label.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var buttonBackground = await Component("text-zoom").Locator("button").First
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        Assert.NotEqual(buttonBackground, background);
    }

    [Fact]
    public async Task The_label_takes_the_same_height_as_the_buttons_beside_it ()
    {
        await GotoComponentAsync(Route);

        var label = await HeightAsync(Page.GetByTestId("text-zoom-label"));
        var button = await HeightAsync(Component("text-zoom").Locator("button").First);

        Assert.True(Math.Abs(label - button) < 0.5, $"Label and button should match: {label} vs {button}.");
    }

    [Fact]
    public async Task A_toolbar_holds_several_groups_and_says_it_is_a_toolbar ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("toolbar")).ToHaveAttributeAsync("role", "toolbar");
        await Expect(Component("toolbar")).ToHaveAttributeAsync("aria-label", "Formatting");
        await Expect(Component("toolbar")).ToHaveAttributeAsync("aria-orientation", "horizontal");

        // Four clustered groups, three breaks between them.
        await Expect(Component("toolbar").Locator("[role='group']")).ToHaveCountAsync(4);
        await Expect(Component("toolbar").Locator("> .veto-button-group__separator")).ToHaveCountAsync(3);

        await Expect(Component("toolbar-style")).ToHaveAttributeAsync("aria-label", "Text style");
        await Expect(Component("toolbar-align").Locator("button")).ToHaveCountAsync(4);
    }

    [Fact]
    public async Task Every_toolbar_button_keeps_an_accessible_name ()
    {
        await GotoComponentAsync(Route);

        // Icon-only buttons carry no text, so the label is all a screen reader gets.
        await Expect(Component("toolbar-style").Locator("button").First)
            .ToHaveAttributeAsync("aria-label", "Bold");
        await Expect(Component("toolbar-history").Locator("button").First)
            .ToHaveAttributeAsync("aria-label", "Undo");
    }

    [Fact]
    public async Task Detached_vertical_and_full_width_land_as_modifier_classes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("detached")).Not.ToHaveClassAsync(new Regex(@"\bveto-button-group--attached\b"));
        await Expect(Component("vertical")).ToHaveClassAsync(new Regex(@"\bveto-button-group--column\b"));
        await Expect(Component("full-width")).ToHaveClassAsync(new Regex(@"\bveto-button-group--full-width\b"));
    }

    [Fact]
    public async Task A_vertical_group_stacks_its_buttons ()
    {
        await GotoComponentAsync(Route);

        var buttons = Component("vertical").Locator("button");
        var first = await buttons.Nth(0).EvaluateAsync<double>("el => el.getBoundingClientRect().top");
        var second = await buttons.Nth(1).EvaluateAsync<double>("el => el.getBoundingClientRect().top");

        Assert.True(second > first, $"The second button should sit below the first: {first} vs {second}.");
    }

    [Fact]
    public async Task A_full_width_group_shares_the_space_evenly ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var buttons = Component("full-width").Locator("button");
        var a = await buttons.Nth(0).EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var b = await buttons.Nth(1).EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var c = await buttons.Nth(2).EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(Math.Abs(a - b) < 2 && Math.Abs(b - c) < 2, $"Widths should match: {a}, {b}, {c}.");
    }
}
