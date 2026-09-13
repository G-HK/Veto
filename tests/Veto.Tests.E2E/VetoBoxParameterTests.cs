using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for the box parameters every component shares - size, spacing, overflow,
/// offsets, stacking and the native tooltip - driven against /docs/usage.
/// </summary>
/// <remarks>
/// These belong to the two base classes rather than to any one component, so they are specced
/// once here instead of being re-asserted on every component's own page. The demos use a card,
/// a button and a menu precisely because they come from three different parts of the library:
/// if the parameters worked only on one of them, the mechanism would be in the wrong place.
/// </remarks>
public class VetoBoxParameterTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/usage";

    [Fact]
    public async Task Width_and_margin_land_on_a_component_that_has_neither_parameter_of_its_own ()
    {
        await GotoComponentAsync(Route);

        var card = Component("box-card");

        await Expect(card).ToHaveCSSAsync("width", "288px");

        // VetoSpacing.Medium is --veto-space-3, 0.75rem.
        await Expect(card).ToHaveCSSAsync("margin-top", "12px");
    }

    [Fact]
    public async Task MinWidth_widens_a_button_past_its_text ()
    {
        await GotoComponentAsync(Route);

        var button = Component("box-button");

        await Expect(button).ToHaveCSSAsync("min-width", "192px");

        var width = await button.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(width >= 191, $"The button should be held open to its min-width: {width}px.");
    }

    [Fact]
    public async Task A_card_clips_a_menu_inside_it_until_Overflow_is_set ()
    {
        await GotoComponentAsync(Route);

        // The card's own stylesheet clips, so that a media image is trimmed to its corners.
        await Expect(Component("clipping-card")).ToHaveCSSAsync("overflow", "hidden");

        // Overflow is the shared way out, and it is an inline declaration, so it beats the
        // component's own stylesheet without anyone editing it.
        await Expect(Component("unclipped-card")).ToHaveCSSAsync("overflow", "visible");
    }

    [Fact]
    public async Task ZIndex_lands_on_the_root_and_positions_it ()
    {
        await GotoComponentAsync(Route);

        var menu = Component("unclipped");

        await Expect(menu).ToHaveCSSAsync("z-index", "20");

        // A z-index on a static element does nothing, so asking for one has to position the
        // element too or the parameter is a silent no-op.
        await Expect(menu).ToHaveCSSAsync("position", "relative");
    }

    [Fact]
    public async Task A_component_without_ZIndex_is_left_alone ()
    {
        await GotoComponentAsync(Route);

        // Nothing is emitted for a parameter that was not set, so the stylesheet keeps its say.
        await Expect(Component("box-card")).ToHaveCSSAsync("z-index", "auto");
    }

    [Fact]
    public async Task Tooltip_becomes_the_roots_title_attribute ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("tooltip-button"))
            .ToHaveAttributeAsync("title", "Saves without leaving the page");
    }

    [Fact]
    public async Task A_menu_let_out_of_its_card_is_actually_reachable ()
    {
        await GotoComponentAsync(Route);

        await Component("unclipped-trigger").ClickAsync();

        // Clipped content is still laid out and still "visible" to a naive check, so the real
        // question is whether the reader can hit it. Playwright's click does that check.
        await Component("unclipped-item").ClickAsync();

        await Expect(Component("unclipped-panel")).ToHaveCountAsync(0);
    }
}
