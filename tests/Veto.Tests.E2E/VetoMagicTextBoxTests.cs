using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for <c>VetoMagicTextBox</c>, driven against /docs/components/magic-text-box.
/// </summary>
public class VetoMagicTextBoxTests(BlazorAppFixture app) : VetoPageTest(app)
{
    private const string Route = "docs/components/magic-text-box";

    /// <summary>
    /// Waits until the field has been measured. Whether the text overflows is the browser's
    /// answer, not the component's, so it is not known during the first render - a spec that
    /// looks for the toggle as soon as the page is interactive is racing the measurement
    /// rather than testing it. The field says when it has an answer; this waits for that.
    /// </summary>
    private async Task MeasuredAsync(string testId)
    {
        // Waits for the attribute to exist at all. Asserting on its value would pass the
        // instant the element appeared, because "not true" is also what an absent attribute
        // reads as - and the spec would then race the measurement it is waiting for.
        await Page.Locator($"[data-testid='{testId}'][data-overflowing]")
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15_000 });
    }

    private ILocator TextArea(string testId) => Component(testId).Locator("textarea");

    private async Task GrantClipboardAsync()
    {
        // Scoped to the app's origin. Granted without one it applies to whatever the context
        // is showing - about:blank, before the first navigation - and the real page is still
        // refused with NotAllowedError.
        await Context.GrantPermissionsAsync(
            ["clipboard-read", "clipboard-write"],
            new() { Origin = App.BaseUrl });
    }

    private Task<double> HeightAsync(string testId) =>
        TextArea(testId).EvaluateAsync<double>("el => el.getBoundingClientRect().height");

    [Fact]
    public async Task A_value_that_overflows_gets_a_toggle()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("basic-long");

        await Expect(Component("basic-long")).ToHaveAttributeAsync("data-overflowing", "true");
        await Expect(Component("basic-long-toggle")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_value_that_fits_gets_no_toggle()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("basic-short");

        await Expect(Component("basic-short")).ToHaveAttributeAsync("data-overflowing", "false");
        await Expect(Component("basic-short-toggle")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_toggle_opens_and_closes_the_field()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("basic-long");

        var field = Component("basic-long");
        var toggle = Component("basic-long-toggle");

        await Expect(field).ToHaveAttributeAsync("data-expanded", "false");
        var closed = await HeightAsync("basic-long");

        await toggle.ClickAsync();
        await Expect(field).ToHaveAttributeAsync("data-expanded", "true");

        // The open field is taller than the closed one. Compared against itself rather than
        // against a pixel count, which the theme's font and spacing tokens decide.
        await Expect(field).ToHaveAttributeAsync("data-expanded", "true");
        var open = await HeightAsync("basic-long");
        Assert.True(open > closed, $"expected the open field ({open}px) to be taller than the closed one ({closed}px)");

        await toggle.ClickAsync();
        await Expect(field).ToHaveAttributeAsync("data-expanded", "false");
    }

    [Fact]
    public async Task The_toggle_reports_its_state_to_assistive_technology()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("basic-long");

        var toggle = Component("basic-long-toggle");
        var textareaId = await TextArea("basic-long").GetAttributeAsync("id");

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToHaveAttributeAsync("aria-controls", textareaId!);

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public async Task Opening_the_field_keeps_the_toggle_that_closes_it()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("basic-long");

        await Component("basic-long-toggle").ClickAsync();
        await Expect(Component("basic-long")).ToHaveAttributeAsync("data-expanded", "true");

        // The measurement is taken against the closed height whether or not the field is open.
        // Taken against the current height it would read "fits" once open, and the control that
        // closes the field again would disappear underneath the reader.
        await Expect(Component("basic-long")).ToHaveAttributeAsync("data-overflowing", "true");
        await Expect(Component("basic-long-toggle")).ToBeVisibleAsync();
    }

    /// <summary>
    /// The cluster the copy button sits in. Opacity is set on the cluster, not the button, so
    /// the button keeps its own hover and focus styling.
    /// </summary>
    private ILocator CopyCorner(string testId) => Component(testId).Locator(".veto-magic-textbox__corner--end");

    [Fact]
    public async Task The_copy_button_is_hidden_until_hover()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("copy-hover");

        // ToHaveCSS retries; reading the value once would sample the fade rather than its
        // resting state and see some fraction on the way through.
        await Expect(CopyCorner("copy-hover")).ToHaveCSSAsync("opacity", "0");

        // Transparent rather than removed, so it keeps its place in the tab order.
        await Expect(Component("copy-hover-copy")).ToHaveCountAsync(1);

        await Component("copy-hover").HoverAsync();

        await Expect(CopyCorner("copy-hover")).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public async Task Keyboard_focus_reveals_the_copy_button_too()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("copy-hover");

        // Revealing on hover alone would put the button out of reach of anyone not using a
        // pointer, so focus anywhere inside the field has to reveal it as well.
        await TextArea("copy-hover").FocusAsync();

        await Expect(CopyCorner("copy-hover")).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public async Task CopyReveal_Always_shows_the_button_without_hover()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("copy-always");

        await Expect(CopyCorner("copy-always")).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public async Task The_field_has_no_border_unless_asked_for()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("border-off");
        await MeasuredAsync("border-on");

        // Transparent rather than absent, so turning the border on does not shift the text.
        await Expect(TextArea("border-off")).ToHaveCSSAsync("border-top-width", "1px");
        Assert.Equal(
            "rgba(0, 0, 0, 0)",
            await TextArea("border-off").EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor"));

        Assert.NotEqual(
            "rgba(0, 0, 0, 0)",
            await TextArea("border-on").EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor"));
    }

    [Fact]
    public async Task The_open_and_close_motion_is_gradual_and_adjustable()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("border-off");
        await MeasuredAsync("motion-slow");

        var defaultDuration = await TextArea("border-off")
            .EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration.split(',')[0].trim()");
        var slowDuration = await TextArea("motion-slow")
            .EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration.split(',')[0].trim()");

        // Gradual: the height is eased over its own duration rather than snapping.
        Assert.Equal("0.32s", defaultDuration);
        Assert.Equal("1.2s", slowDuration);
    }

    [Fact]
    public async Task Copying_raises_a_toast()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);
        await MeasuredAsync("copy-always");

        await Component("copy-always-copy").ClickAsync();

        // The icon swapping to a tick is small and easy to miss; the toast is what actually
        // confirms the copy to someone whose eye is on the text. The host renders one stack
        // per position, so the toasts are found by class rather than under a single testid.
        await Expect(Page.Locator(".veto-toast").First).ToContainTextAsync("Copied to clipboard");
    }

    [Fact]
    public async Task NotifyOnCopy_false_copies_silently()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);
        await MeasuredAsync("copy-quiet");

        var expected = await TextArea("copy-quiet").InputValueAsync();

        await Component("copy-quiet-copy").ClickAsync();
        await Expect(Component("copy-quiet-copy")).ToHaveAttributeAsync("aria-label", "Copied");

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.Equal(expected, clipboard);

        // The copy happened; it just said nothing about it.
        await Expect(Page.Locator(".veto-toast")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Copyable_false_renders_no_copy_button()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("copy-none");

        await Expect(Component("copy-none-copy")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Copying_puts_the_value_on_the_clipboard_and_confirms_it()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);
        await MeasuredAsync("copy-always");

        var expected = await TextArea("copy-always").InputValueAsync();

        await Component("copy-always-copy").ClickAsync();

        // The button confirms by swapping its accessible name, which is the only feedback a
        // screen reader gets - the icon change is invisible to it.
        await Expect(Component("copy-always-copy")).ToHaveAttributeAsync("aria-label", "Copied");

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.Equal(expected, clipboard);
    }

    [Fact]
    public async Task Corner_content_sits_in_both_corners_and_stays_visible()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("corners");

        await Expect(Component("corner-bold")).ToBeVisibleAsync();
        await Expect(Component("corner-clear")).ToBeVisibleAsync();

        var field = Component("corners");
        await Expect(field.Locator(".veto-magic-textbox__corner--start")).ToHaveCountAsync(1);
        await Expect(field.Locator(".veto-magic-textbox__corner--end")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task A_corner_reserves_a_gutter_so_text_never_runs_under_it()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("corners");
        await MeasuredAsync("basic-long");

        var withCorners = await TextArea("corners").EvaluateAsync<double>(
            "el => parseFloat(getComputedStyle(el).paddingInlineStart)");
        var without = await TextArea("basic-long").EvaluateAsync<double>(
            "el => parseFloat(getComputedStyle(el).paddingInlineStart)");

        Assert.True(
            withCorners > without,
            $"expected a leading gutter ({withCorners}px) wider than the plain field's padding ({without}px)");
    }

    [Fact]
    public async Task ExpandedLines_caps_how_far_the_field_opens()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("lines-grow");
        await MeasuredAsync("lines-capped");

        await Component("lines-grow-toggle").ClickAsync();
        await Component("lines-capped-toggle").ClickAsync();

        await Expect(Component("lines-grow")).ToHaveAttributeAsync("data-expanded", "true");
        await Expect(Component("lines-capped")).ToHaveAttributeAsync("data-expanded", "true");

        var grown = await HeightAsync("lines-grow");
        var capped = await HeightAsync("lines-capped");

        Assert.True(grown > capped, $"expected the uncapped field ({grown}px) to open further than the capped one ({capped}px)");
    }

    [Fact]
    public async Task The_open_state_is_bindable_from_the_page()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("bound");

        await Expect(Component("bound")).ToHaveAttributeAsync("data-expanded", "false");
        await Expect(Component("bound-state")).ToHaveTextAsync("Expanded: False");

        await Component("bound-page-toggle").ClickAsync();

        await Expect(Component("bound")).ToHaveAttributeAsync("data-expanded", "true");
        await Expect(Component("bound-state")).ToHaveTextAsync("Expanded: True");

        // And the field's own toggle reports back the other way, so the page is never left
        // believing the field is open after the reader has closed it.
        await Component("bound-toggle").ClickAsync();

        await Expect(Component("bound")).ToHaveAttributeAsync("data-expanded", "false");
        await Expect(Component("bound-state")).ToHaveTextAsync("Expanded: False");
    }

    [Fact]
    public async Task Expandable_false_never_offers_a_toggle()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("state-fixed");

        // The text still overflows - it simply has no way to rearrange the page.
        await Expect(Component("state-fixed")).ToHaveAttributeAsync("data-overflowing", "true");
        await Expect(Component("state-fixed-toggle")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Disabled_and_readonly_reach_the_textarea()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("state-readonly");
        await MeasuredAsync("state-disabled");

        await Expect(TextArea("state-readonly")).ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(TextArea("state-disabled")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task The_label_is_wired_to_the_textarea()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("basic-long");

        var id = await TextArea("basic-long").GetAttributeAsync("id");
        var label = Component("basic-long").Locator("label");

        await Expect(label).ToHaveAttributeAsync("for", id!);
    }

    [Fact]
    public async Task Typing_updates_the_bound_value()
    {
        await GotoComponentAsync(Route);
        await MeasuredAsync("bound");

        var textarea = TextArea("bound");

        await textarea.FillAsync("one\ntwo\nthree\nfour\nfive\nsix");
        await textarea.BlurAsync();

        Assert.Equal("one\ntwo\nthree\nfour\nfive\nsix", await textarea.InputValueAsync());

        // A value replaced wholesale raises no resize the observer can see, so the component
        // re-measures on render; the toggle has to reflect the new value, not the old one.
        await Expect(Component("bound")).ToHaveAttributeAsync("data-overflowing", "true");
    }
}