using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoButtonReveal</c>, driven against /docs/components/button-reveal.</summary>
public class VetoButtonRevealTests(BlazorAppFixture app) : VetoPageTest(app)
{
    private const string Route = "docs/components/button-reveal";

    /// <summary>
    /// Hovers a cluster and waits until its strip has actually been laid out.
    /// </summary>
    /// <remarks>
    /// The reveal is a transition, so <c>data-open</c> flips well before the strip has any
    /// geometry. Measuring between the two reads the folded positions and turns every
    /// direction assertion into a race - which is exactly how it failed under load, passing
    /// on its own and failing in a full class run.
    /// </remarks>
    private async Task RevealAsync(string testId)
    {
        await Component(testId).HoverAsync();
        await Expect(Component(testId)).ToHaveAttributeAsync("data-open", "true");

        await Page.WaitForFunctionAsync(
            """
            id => {
                const el = document.querySelector(`[data-testid='${id}-actions']`);

                if (!el) {
                    return false;
                }

                const box = el.getBoundingClientRect();

                // One icon button is wider and taller than this either way round, so the
                // check holds whichever axis the cluster unfolds along.
                return box.width > 20 && box.height > 20;
            }
            """,
            testId);
    }

    [Fact]
    public async Task A_cluster_starts_as_one_button()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic-trigger")).ToHaveAttributeAsync("aria-expanded", "false");

        // The actions are in the DOM but inert, so they hold their place without taking focus.
        var actions = Component("basic-actions");

        await Expect(actions).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(actions).ToHaveAttributeAsync("inert", string.Empty);
    }

    [Fact]
    public async Task The_actions_take_no_room_until_the_cluster_opens()
    {
        await GotoComponentAsync(Route);

        // The strip is what is clipped, not the buttons inside it: they keep their own size and
        // their place in the layout - which is what lets the reveal be a transition - while the
        // strip they sit in is squeezed to nothing.
        var closed = await Component("basic-actions")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(closed < 1, $"A folded strip should take no room: {closed}px.");

        await RevealAsync("basic");
        await Expect(Component("basic-trigger")).ToHaveAttributeAsync("aria-expanded", "true");

        var open = await Component("basic-actions")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(open > 50, $"An open strip should carry its buttons: {closed}px -> {open}px.");
    }

    [Fact]
    public async Task A_folded_action_is_scaled_down_and_a_revealed_one_is_not()
    {
        await GotoComponentAsync(Route);

        // The stagger is applied to the cluster's children through ::deep, so this also proves
        // those rules reach buttons that carry a different component's scope attribute.
        var folded = await Component("basic-github")
            .EvaluateAsync<string>("el => getComputedStyle(el).transform");

        Assert.NotEqual("none", folded);

        await Component("basic").HoverAsync();

        await Expect(Component("basic-github")).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public async Task Hovering_reveals_the_actions_and_leaving_folds_them_away()
    {
        await GotoComponentAsync(Route);

        await Component("basic").HoverAsync();
        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "true");

        await Page.Mouse.MoveAsync(5, 5);
        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "false");
    }

    [Fact]
    public async Task A_revealed_action_can_be_clicked()
    {
        await GotoComponentAsync(Route);

        await Component("basic").HoverAsync();
        await Component("basic-linkedin").ClickAsync();

        await Expect(Page.GetByTestId("shared")).ToHaveTextAsync("LinkedIn");
    }

    [Fact]
    public async Task Keyboard_focus_reveals_the_actions_so_hover_is_not_the_only_way_in()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();

        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "true");
        await Expect(Component("basic-actions")).Not.ToHaveAttributeAsync("inert", string.Empty);
    }

    [Fact]
    public async Task Tab_walks_from_the_trigger_into_the_revealed_actions()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();
        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "true");

        await Page.Keyboard.PressAsync("Tab");

        // The actions stop being inert as the cluster opens, which is what lets focus reach
        // them without a second render to wait for.
        await Expect(Component("basic-github")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Focus_leaving_the_cluster_folds_it_away()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();
        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "true");

        await Page.Locator("h1").ClickAsync();

        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "false");
    }

    [Fact]
    public async Task Focus_moving_between_the_actions_does_not_fold_the_cluster()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");

        // Focus left one action for the next, not the cluster - the pending close has to be
        // cancelled by the focusin that follows, or walking the actions shuts them.
        await Expect(Component("basic-x")).ToBeFocusedAsync();
        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "true");
    }

    [Fact]
    public async Task Escape_folds_the_cluster_and_returns_focus_to_the_trigger()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Component("basic-github")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Component("basic")).ToHaveAttributeAsync("data-open", "false");
        await Expect(Component("basic-trigger")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task A_click_trigger_ignores_the_pointer_passing_over_it()
    {
        await GotoComponentAsync(Route);

        await Component("click").HoverAsync();

        await Expect(Component("click")).ToHaveAttributeAsync("data-open", "false");

        await Component("click-trigger").ClickAsync();

        await Expect(Component("click")).ToHaveAttributeAsync("data-open", "true");
    }

    [Fact]
    public async Task A_click_trigger_toggles_rather_than_only_opening()
    {
        await GotoComponentAsync(Route);

        await Component("click-trigger").ClickAsync();
        await Expect(Component("click")).ToHaveAttributeAsync("data-open", "true");

        await Component("click-trigger").ClickAsync();
        await Expect(Component("click")).ToHaveAttributeAsync("data-open", "false");
    }

    [Fact]
    public async Task A_trigger_given_its_own_action_runs_it_instead_of_toggling()
    {
        await GotoComponentAsync(Route);

        await Component("action-trigger").ClickAsync();

        // The click saved; it did not merely disclose. The reveal is left to hover and focus.
        await Expect(Page.GetByTestId("acted")).ToHaveTextAsync("Save");
    }

    [Fact]
    public async Task The_trigger_comes_before_its_actions_in_the_tab_order_whichever_way_it_points()
    {
        await GotoComponentAsync(Route);

        // Start points the actions the other way on screen, but the trigger is still first in
        // the DOM - otherwise Tab would reach the actions before the thing disclosing them.
        await Component("dir-start-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Component("dir-start-edit")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Start_draws_its_actions_on_the_leading_side_of_the_trigger()
    {
        await GotoComponentAsync(Route);

        await RevealAsync("dir-start");

        var trigger = await Component("dir-start-trigger").BoundingBoxAsync();
        var action = await Component("dir-start-edit").BoundingBoxAsync();

        Assert.NotNull(trigger);
        Assert.NotNull(action);
        Assert.True(action!.X < trigger!.X, $"A start cluster should draw its actions to the left: {trigger.X} vs {action.X}.");
    }

    [Fact]
    public async Task End_draws_its_actions_on_the_trailing_side_of_the_trigger()
    {
        await GotoComponentAsync(Route);

        await RevealAsync("dir-end");

        var trigger = await Component("dir-end-trigger").BoundingBoxAsync();
        var actions = await Component("dir-end-actions").BoundingBoxAsync();

        Assert.NotNull(trigger);
        Assert.NotNull(actions);
        Assert.True(actions!.X > trigger!.X, $"An end cluster should draw its actions to the right: {trigger.X} vs {actions.X}.");
    }

    [Fact]
    public async Task Up_draws_its_actions_above_the_trigger_and_down_below_it()
    {
        await GotoComponentAsync(Route);

        await RevealAsync("dir-up");

        var upTrigger = await Component("dir-up-trigger").BoundingBoxAsync();
        var upAction = await Component("dir-up-csv").BoundingBoxAsync();

        Assert.NotNull(upTrigger);
        Assert.NotNull(upAction);
        Assert.True(upAction!.Y < upTrigger!.Y, $"An up cluster should draw above: {upTrigger.Y} vs {upAction.Y}.");

        await RevealAsync("dir-down");

        var downTrigger = await Component("dir-down-trigger").BoundingBoxAsync();
        var downAction = await Component("dir-down-csv").BoundingBoxAsync();

        Assert.NotNull(downTrigger);
        Assert.NotNull(downAction);
        Assert.True(downAction!.Y > downTrigger!.Y, $"A down cluster should draw below: {downTrigger.Y} vs {downAction.Y}.");
    }

    [Fact]
    public async Task A_disabled_cluster_cannot_be_opened()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("disabled-trigger")).ToBeDisabledAsync();

        await Component("disabled").HoverAsync();

        await Expect(Component("disabled")).ToHaveAttributeAsync("data-open", "false");
    }

    [Fact]
    public async Task The_revealed_group_is_announced_with_the_triggers_name()
    {
        await GotoComponentAsync(Route);

        var actions = Component("basic-actions");

        await Expect(actions).ToHaveAttributeAsync("role", "group");
        await Expect(actions).ToHaveAttributeAsync("aria-label", "Share");
    }

    [Fact]
    public async Task An_unlabelled_trigger_still_has_an_accessible_name()
    {
        await GotoComponentAsync(Route);

        // The label is not drawn, so it has to be the accessible name and the tooltip instead.
        var trigger = Component("basic-trigger");

        await Expect(trigger).ToHaveAttributeAsync("aria-label", "Share");
        await Expect(trigger).ToHaveAttributeAsync("title", "Share");
    }

    [Fact]
    public async Task A_labelled_trigger_draws_its_text_rather_than_hiding_it()
    {
        await GotoComponentAsync(Route);

        var trigger = Component("action-trigger");

        await Expect(trigger).ToContainTextAsync("Save");
        await Expect(trigger).Not.ToHaveAttributeAsync("aria-label", "Save");
    }

    [Fact]
    public async Task Stagger_can_be_turned_off()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("no-stagger")).Not.ToHaveClassAsync(new Regex(@"\bveto-button-reveal--stagger\b"));
        await Expect(Component("basic")).ToHaveClassAsync(new Regex(@"\bveto-button-reveal--stagger\b"));
    }

    [Fact]
    public async Task Sizes_are_visibly_different()
    {
        await GotoComponentAsync(Route);

        var small = await Component("size-sm-trigger").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var large = await Component("size-lg-trigger").EvaluateAsync<double>("el => el.getBoundingClientRect().height");

        Assert.True(small < large, $"Small should be shorter than large: {small} vs {large}.");
    }
}