using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoTextBox</c>, driven against /components/textbox.</summary>
public class VetoTextBoxTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "components/textbox";

    [Fact]
    public async Task Renders_label_bound_to_the_input ()
    {
        await GotoComponentAsync(Route);

        var input = Input("basic");
        var id = await input.GetAttributeAsync("id");

        Assert.False(string.IsNullOrWhiteSpace(id));
        await Expect(Component("basic").Locator("label")).ToHaveAttributeAsync("for", id!);
        await Expect(Component("basic").Locator("label")).ToContainTextAsync("Project name");
    }

    [Fact]
    public async Task Generates_an_id_when_none_is_supplied ()
    {
        await GotoComponentAsync(Route);

        await Expect(Input("basic")).ToHaveAttributeAsync("id", new Regex(@"^veto-textbox-\d+$"));
    }

    [Fact]
    public async Task Updates_the_bound_value_on_blur_by_default ()
    {
        await GotoComponentAsync(Route);

        await Input("basic").FillAsync("Hello Veto");

        // Still unbound while focused - the default binds on change, not on input.
        await Expect(Page.GetByTestId("basic-output")).ToBeEmptyAsync();

        await Input("basic").BlurAsync();
        await Expect(Page.GetByTestId("basic-output")).ToHaveTextAsync("Hello Veto");
    }

    [Fact]
    public async Task Updates_the_bound_value_per_keystroke_when_immediate ()
    {
        await GotoComponentAsync(Route);

        await Input("immediate").PressSequentiallyAsync("abc");

        // No blur: Immediate="true" binds on every input event.
        await Expect(Page.GetByTestId("immediate-output")).ToHaveTextAsync("abc");
    }

    [Fact]
    public async Task Disabled_field_cannot_be_edited ()
    {
        await GotoComponentAsync(Route);

        await Expect(Input("disabled")).ToBeDisabledAsync();
        await Expect(Component("disabled")).ToHaveClassAsync(new Regex(@"\bveto-textbox--disabled\b"));
    }

    [Fact]
    public async Task ReadOnly_field_can_be_focused_but_not_edited ()
    {
        await GotoComponentAsync(Route);

        var input = Input("readonly");
        await Expect(input).ToHaveAttributeAsync("readonly", string.Empty);

        await input.ClickAsync();
        await Expect(input).ToBeFocusedAsync();

        await input.PressAsync("X");
        await Expect(input).ToHaveValueAsync("Cannot edit this");
    }

    [Fact]
    public async Task Helper_text_is_linked_through_aria_describedby ()
    {
        await GotoComponentAsync(Route);

        var describedBy = await Input("basic").GetAttributeAsync("aria-describedby");
        Assert.False(string.IsNullOrWhiteSpace(describedBy));

        await Expect(Page.Locator($"#{describedBy}"))
            .ToHaveTextAsync("Shown under the field until it becomes invalid.");
    }

    [Fact]
    public async Task Required_field_reports_itself_as_required ()
    {
        await GotoComponentAsync(Route);

        await Expect(Input("username")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Component("username").Locator(".veto-textbox__required")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Failing_validation_shows_the_message_and_marks_the_field_invalid ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("submit").ClickAsync();

        await Expect(Input("username")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Component("username")).ToHaveClassAsync(new Regex(@"\bveto-textbox--invalid\b"));

        var errorId = await Input("username").GetAttributeAsync("aria-describedby");
        await Expect(Page.Locator($"#{errorId}")).ToHaveTextAsync("Username is required.");
        await Expect(Page.Locator($"#{errorId}")).ToHaveAttributeAsync("role", "alert");
    }

    [Fact]
    public async Task Fixing_validation_clears_the_message_and_submits ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("submit").ClickAsync();
        await Expect(Input("username")).ToHaveAttributeAsync("aria-invalid", "true");

        await Input("username").FillAsync("veto-user");
        await Page.GetByTestId("submit").ClickAsync();

        await Expect(Component("username").Locator(".veto-textbox__message--error")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("submit-output")).ToHaveTextAsync("Submitted: veto-user");
    }

    [Fact]
    public async Task Typed_binding_parses_numeric_input ()
    {
        await GotoComponentAsync(Route);

        await Input("number").FillAsync("42");
        await Input("number").BlurAsync();

        await Expect(Page.GetByTestId("number-output")).ToHaveTextAsync("42");
    }

    [Fact]
    public async Task Size_and_variant_render_modifier_classes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("size-sm")).ToHaveClassAsync(new Regex(@"\bveto-textbox--sm\b"));
        await Expect(Component("size-lg")).ToHaveClassAsync(new Regex(@"\bveto-textbox--lg\b"));
        await Expect(Component("variant-filled")).ToHaveClassAsync(new Regex(@"\bveto-textbox--filled\b"));
        await Expect(Component("variant-text")).ToHaveClassAsync(new Regex(@"\bveto-textbox--text\b"));
    }

    [Fact]
    public async Task Consumer_class_and_style_are_appended_not_replaced ()
    {
        await GotoComponentAsync(Route);

        var root = Component("splat");
        await Expect(root).ToHaveClassAsync(new Regex(@"\bveto-textbox\b"));
        await Expect(root).ToHaveClassAsync(new Regex(@"\bdemo-custom\b"));
        await Expect(root).ToHaveAttributeAsync("style", new Regex("max-width: 12rem"));
    }

    [Fact]
    public async Task Unmatched_attributes_are_splatted_onto_the_input ()
    {
        await GotoComponentAsync(Route);

        await Expect(Input("splat")).ToHaveAttributeAsync("maxlength", "5");
        await Expect(Input("splat")).ToHaveAttributeAsync("autocomplete", "off");
    }

    [Fact]
    public async Task Focus_ring_is_shown_on_keyboard_focus ()
    {
        await GotoComponentAsync(Route);

        await Input("basic").FocusAsync();
        await Input("basic").PressAsync("Tab");
        await Input("basic").FocusAsync();

        // :focus-visible must produce a real, non-empty box-shadow, not just an outline reset.
        var shadow = await Input("basic").EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");
        Assert.NotEqual("none", shadow);
    }

    [Fact]
    public async Task Component_styles_come_from_veto_tokens ()
    {
        await GotoComponentAsync(Route);

        var token = await Page.EvaluateAsync<string>(
            "() => getComputedStyle(document.documentElement).getPropertyValue('--veto-color-primary').trim()");

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task Clear_button_empties_the_field_and_the_binding ()
    {
        await GotoComponentAsync(Route);

        await Expect(Input("clearable")).ToHaveValueAsync("Veto");
        await Page.GetByTestId("clearable-clear").ClickAsync();

        await Expect(Input("clearable")).ToHaveValueAsync(string.Empty);
        await Expect(Page.GetByTestId("clearable-output")).ToHaveTextAsync("null");
        await Expect(Page.GetByTestId("clearable-clear")).ToHaveCountAsync(0);
        await Expect(Input("clearable")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Counter_tracks_typing_before_the_value_is_bound ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("clearable-counter")).ToHaveTextAsync("4 / 10");
        await Expect(Input("clearable")).ToHaveAttributeAsync("maxlength", "10");

        await Input("clearable").FillAsync("Blazor");
        await Expect(Page.GetByTestId("clearable-counter")).ToHaveTextAsync("6 / 10");
    }

    [Fact]
    public async Task Prefix_and_suffix_render_inside_the_field ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("affix").Locator(".veto-textbox__affix--prefix")).ToHaveTextAsync("$");
        await Expect(Component("affix").Locator(".veto-textbox__affix--suffix")).ToHaveTextAsync("USD");
    }

    [Fact]
    public async Task Format_is_used_to_parse_as_well_as_to_display ()
    {
        await GotoComponentAsync(Route);

        await Expect(Input("formatted")).ToHaveValueAsync("30/09/2026");

        await Input("formatted").FillAsync("25/12/2026");
        await Input("formatted").BlurAsync();

        await Expect(Page.GetByTestId("formatted-output")).ToHaveTextAsync("2026-12-25");
    }
}
