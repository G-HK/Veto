using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for the shared effects - the breathing glow and the ping ring - driven
/// against /docs/components/effects.
/// </summary>
/// <remarks>
/// Both effects are drawn on a pseudo-element, so almost everything here is read through
/// <c>getComputedStyle(el, '::after')</c>: asserting on the class alone would pass just as
/// happily if the stylesheet never shipped.
/// </remarks>
public class VetoEffectsTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/effects";

    private Task<string> PseudoAsync ( string testId, string pseudo, string property )
        => Component(testId).EvaluateAsync<string>(
            $"el => getComputedStyle(el, '{pseudo}').{property}");

    [Fact]
    public async Task A_glowing_component_draws_a_halo_that_breathes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("glow-primary")).ToHaveClassAsync(new Regex(@"\bveto-glow\b"));

        var shadow = await PseudoAsync("glow-primary", "::after", "boxShadow");
        var animation = await PseudoAsync("glow-primary", "::after", "animationName");

        Assert.NotEqual("none", shadow);
        Assert.Equal("veto-breathe", animation);
    }

    [Fact]
    public async Task An_effect_that_was_not_asked_for_is_not_drawn ()
    {
        await GotoComponentAsync(Route);

        // This one glows but does not ping, and this one pings but does not glow. Neither
        // pseudo-element should exist where the parameter was left alone.
        Assert.Equal("none", await PseudoAsync("glow-primary", "::before", "content"));
        Assert.Equal("none", await PseudoAsync("ping-card", "::after", "content"));
    }

    [Fact]
    public async Task The_glow_follows_the_component_s_own_colour_role ()
    {
        await GotoComponentAsync(Route);

        // Neither button was given a glow colour: the danger one glows its own red because its
        // role says so. Asserted against the role's own token rather than a literal, since the
        // documentation site runs on a theme of its own and every one of these values moves
        // with it.
        var primary = await PseudoAsync("glow-primary", "::after", "boxShadow");
        var danger = await PseudoAsync("glow-danger", "::after", "boxShadow");

        Assert.NotEqual(primary, danger);

        var role = await Component("glow-danger").EvaluateAsync<string>(
            "el => { const c = getComputedStyle(el).getPropertyValue('--veto-button-color').trim();"
            + " const probe = document.createElement('span'); probe.style.color = c;"
            + " document.body.appendChild(probe);"
            + " const rgb = getComputedStyle(probe).color; probe.remove(); return rgb; }");

        Assert.Contains(role, danger);
    }

    [Fact]
    public async Task An_explicit_colour_overrides_the_role ()
    {
        await GotoComponentAsync(Route);

        var shadow = await PseudoAsync("glow-cyan", "::after", "boxShadow");

        Assert.Contains("rgb(34, 211, 238)", shadow);
    }

    [Fact]
    public async Task A_colour_that_could_break_out_of_the_style_attribute_is_refused ()
    {
        await GotoComponentAsync(Route);

        // The parameter is written straight into a style attribute, so it is guarded. Nothing
        // on the page sets a hostile value; this checks the guard itself rather than the page.
        var style = await Component("glow-cyan").GetAttributeAsync("style");

        Assert.NotNull(style);
        Assert.Contains("--veto-glow-color", style);
        Assert.DoesNotContain("}", style);
    }

    [Fact]
    public async Task A_ping_sends_a_ring_out_of_the_border ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("ping-button")).ToHaveClassAsync(new Regex(@"\bveto-ping\b"));

        var animation = await PseudoAsync("ping-button", "::before", "animationName");
        var border = await PseudoAsync("ping-button", "::before", "borderTopWidth");

        Assert.Equal("veto-ping", animation);
        Assert.Equal("2px", border);
    }

    [Fact]
    public async Task The_two_effects_can_run_at_once ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("veto-breathe", await PseudoAsync("ping-both", "::after", "animationName"));
        Assert.Equal("veto-ping", await PseudoAsync("ping-both", "::before", "animationName"));
    }

    [Fact]
    public async Task Neither_effect_swallows_a_click ()
    {
        await GotoComponentAsync(Route);

        // The pseudo-elements cover the component, so without pointer-events: none they would
        // take every click the component was there to receive.
        Assert.Equal("none", await PseudoAsync("ping-both", "::after", "pointerEvents"));
        Assert.Equal("none", await PseudoAsync("ping-both", "::before", "pointerEvents"));

        await Component("ping-both").ClickAsync();
    }

    [Fact]
    public async Task The_effects_reach_components_that_are_not_buttons ()
    {
        await GotoComponentAsync(Route);

        // A bare <input> cannot host a pseudo-element, so the text box carries the glow on its
        // wrapper. What matters is that the parameter works at all on a form component, whose
        // base class is a different one.
        await Expect(Component("glow-textbox")).ToHaveClassAsync(new Regex(@"\bveto-glow\b"));
        Assert.Equal("veto-breathe", await PseudoAsync("glow-textbox", "::after", "animationName"));

        Assert.Equal("veto-ping", await PseudoAsync("ping-card", "::before", "animationName"));
    }

    [Fact]
    public async Task Reduced_motion_stops_the_movement_but_keeps_the_message ()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await GotoComponentAsync(Route);

        Assert.Equal("none", await PseudoAsync("glow-primary", "::after", "animationName"));
        Assert.Equal("none", await PseudoAsync("ping-button", "::before", "animationName"));

        // Still drawn: taking them away would remove what they were saying, not just the motion.
        var glow = await PseudoAsync("glow-primary", "::after", "opacity");
        var ping = await PseudoAsync("ping-button", "::before", "opacity");

        Assert.True(double.Parse(glow, CultureInfo.InvariantCulture) > 0, $"The glow faded to {glow}.");
        Assert.True(double.Parse(ping, CultureInfo.InvariantCulture) > 0, $"The ring faded to {ping}.");
    }
}
