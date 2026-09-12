using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoSwitch</c>, driven against /docs/components/switch.</summary>
public class VetoSwitchTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/switch";

    /// <summary>The switch's control - a button, not an input, so <c>Input()</c> does not apply.</summary>
    private ILocator Control ( string testId ) => Component(testId).Locator("button.veto-switch__button");

    [Fact]
    public async Task Renders_as_a_switch_with_a_label_bound_to_the_control ()
    {
        await GotoComponentAsync(Route);

        var control = Control("basic");
        var id = await control.GetAttributeAsync("id");

        Assert.False(string.IsNullOrWhiteSpace(id));
        await Expect(control).ToHaveAttributeAsync("role", "switch");
        await Expect(control).ToHaveAttributeAsync("id", new Regex(@"^veto-switch-\d+$"));
        await Expect(Component("basic").Locator("label")).ToHaveAttributeAsync("for", id!);
        await Expect(Component("basic").Locator("label")).ToContainTextAsync("Email notifications");
    }

    [Fact]
    public async Task Reports_its_state_through_aria_checked_and_data_state ()
    {
        await GotoComponentAsync(Route);

        // The demo starts on.
        await Expect(Control("basic")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-state", "on");

        await Control("basic").ClickAsync();

        await Expect(Control("basic")).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-state", "off");
    }

    [Fact]
    public async Task Clicking_updates_the_bound_value ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("True");

        await Control("basic").ClickAsync();
        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("False");

        await Control("basic").ClickAsync();
        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("True");
    }

    [Fact]
    public async Task Clicking_the_label_toggles_the_switch ()
    {
        await GotoComponentAsync(Route);

        await Component("basic").Locator("label").ClickAsync();

        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("False");
    }

    [Fact]
    public async Task Space_toggles_the_focused_switch ()
    {
        await GotoComponentAsync(Route);

        await Control("basic").FocusAsync();
        await Control("basic").PressAsync(" ");

        await Expect(Control("basic")).ToHaveAttributeAsync("aria-checked", "false");
    }

    [Fact]
    public async Task Three_state_switch_cycles_off_on_indeterminate ()
    {
        await GotoComponentAsync(Route);

        var control = Control("tri");

        // Three states need a role that may report "mixed"; switch may not, checkbox may.
        await Expect(control).ToHaveAttributeAsync("role", "checkbox");

        // The demo starts indeterminate.
        await Expect(control).ToHaveAttributeAsync("aria-checked", "mixed");
        await Expect(Page.GetByTestId("tri-value")).ToHaveTextAsync("null");

        await control.ClickAsync();
        await Expect(control).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Page.GetByTestId("tri-value")).ToHaveTextAsync("False");

        await control.ClickAsync();
        await Expect(control).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Page.GetByTestId("tri-value")).ToHaveTextAsync("True");

        await control.ClickAsync();
        await Expect(control).ToHaveAttributeAsync("aria-checked", "mixed");
        await Expect(Page.GetByTestId("tri-value")).ToHaveTextAsync("null");
    }

    [Fact]
    public async Task Three_state_switch_shows_its_state_text ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("tri-state")).ToHaveTextAsync("Indeterminate");

        await Control("tri").ClickAsync();
        await Expect(Page.GetByTestId("tri-state")).ToHaveTextAsync("Off");

        await Control("tri").ClickAsync();
        await Expect(Page.GetByTestId("tri-state")).ToHaveTextAsync("On");
    }

    [Fact]
    public async Task Cycle_order_decides_which_state_comes_second ()
    {
        await GotoComponentAsync(Route);

        // Both demos start off; the orders diverge on the very first press.
        await Expect(Component("order-default")).ToHaveAttributeAsync("data-state", "off");
        await Expect(Component("order-middle")).ToHaveAttributeAsync("data-state", "off");

        await Control("order-default").ClickAsync();
        await Control("order-middle").ClickAsync();

        await Expect(Component("order-default")).ToHaveAttributeAsync("data-state", "on");
        await Expect(Component("order-middle")).ToHaveAttributeAsync("data-state", "indeterminate");

        await Control("order-default").ClickAsync();
        await Control("order-middle").ClickAsync();

        await Expect(Component("order-default")).ToHaveAttributeAsync("data-state", "indeterminate");
        await Expect(Component("order-middle")).ToHaveAttributeAsync("data-state", "on");
    }

    [Fact]
    public async Task Arrow_keys_set_a_state_outright ()
    {
        await GotoComponentAsync(Route);

        var control = Control("tri");
        await control.FocusAsync();

        await control.PressAsync("ArrowRight");
        await Expect(control).ToHaveAttributeAsync("aria-checked", "true");

        await control.PressAsync("ArrowLeft");
        await Expect(control).ToHaveAttributeAsync("aria-checked", "false");

        await control.PressAsync("ArrowUp");
        await Expect(control).ToHaveAttributeAsync("aria-checked", "mixed");
    }

    [Fact]
    public async Task Custom_state_text_replaces_the_defaults ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("custom-text-state")).ToHaveTextAsync("Private");

        await Control("custom-text").ClickAsync();

        await Expect(Page.GetByTestId("custom-text-state")).ToHaveTextAsync("Public");
    }

    [Fact]
    public async Task Disabled_switch_cannot_be_toggled ()
    {
        await GotoComponentAsync(Route);

        await Expect(Control("disabled")).ToBeDisabledAsync();
        await Expect(Component("disabled")).ToHaveClassAsync(new Regex(@"\bveto-switch--disabled\b"));
        await Expect(Control("disabled")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [Fact]
    public async Task ReadOnly_switch_can_be_focused_but_does_not_change ()
    {
        await GotoComponentAsync(Route);

        var control = Control("readonly");
        await Expect(control).ToHaveAttributeAsync("aria-readonly", "true");

        await control.ClickAsync();
        await Expect(control).ToBeFocusedAsync();
        await Expect(control).ToHaveAttributeAsync("aria-checked", "true");

        await control.PressAsync(" ");
        await Expect(control).ToHaveAttributeAsync("aria-checked", "true");
    }

    [Fact]
    public async Task Size_colour_and_variant_land_as_modifier_classes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("size-sm")).ToHaveClassAsync(new Regex(@"\bveto-switch--sm\b"));
        await Expect(Component("size-lg")).ToHaveClassAsync(new Regex(@"\bveto-switch--lg\b"));
        await Expect(Component("color-success")).ToHaveClassAsync(new Regex(@"\bveto-switch--success\b"));
        await Expect(Component("variant-filled")).ToHaveClassAsync(new Regex(@"\bveto-switch--filled\b"));
        await Expect(Component("label-start")).ToHaveClassAsync(new Regex(@"\bveto-switch--label-start\b"));
    }

    [Fact]
    public async Task Helper_text_is_announced_and_replaced_by_the_validation_message ()
    {
        await GotoComponentAsync(Route);

        var control = Control("terms");
        var helperId = await control.GetAttributeAsync("aria-describedby");

        Assert.NotNull(helperId);
        await Expect(Page.Locator($"#{helperId}")).ToContainTextAsync("Required before you can continue.");

        await Page.GetByTestId("submit").ClickAsync();

        await Expect(control).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Component("terms").Locator("[role='alert']")).ToContainTextAsync("You must accept the terms.");
        await Expect(Page.GetByTestId("submit-value")).ToBeEmptyAsync();
    }

    [Fact]
    public async Task Turning_the_required_switch_on_lets_the_form_submit ()
    {
        await GotoComponentAsync(Route);

        await Control("terms").ClickAsync();
        await Page.GetByTestId("submit").ClickAsync();

        await Expect(Page.GetByTestId("submit-value")).ToHaveTextAsync("Saved");
    }
}
