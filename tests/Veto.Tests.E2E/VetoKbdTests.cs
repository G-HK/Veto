using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoKbd</c> and the shortcut list, driven against /docs/components/kbd.</summary>
public class VetoKbdTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/kbd";

    [Fact]
    public async Task A_single_key_is_one_cap_inside_a_kbd ()
    {
        await GotoComponentAsync(Route);

        var kbd = Component("single");

        Assert.Equal("kbd", await kbd.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        await Expect(kbd.Locator(".veto-kbd__key")).ToHaveCountAsync(1);
        await Expect(kbd).ToContainTextAsync("Esc");
    }

    [Fact]
    public async Task A_combination_is_split_into_a_cap_per_key ()
    {
        await GotoComponentAsync(Route);

        var keys = Component("combo").Locator(".veto-kbd__key");

        await Expect(keys).ToHaveCountAsync(3);
        await Expect(keys.Nth(0)).ToHaveTextAsync("Ctrl");
        await Expect(keys.Nth(1)).ToHaveTextAsync("Shift");
        await Expect(keys.Nth(2)).ToHaveTextAsync("P");

        // Nested kbd is what the HTML spec prescribes for a chord.
        Assert.Equal("kbd", await keys.First.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        await Expect(Component("combo")).ToHaveAttributeAsync("data-keys", "Ctrl+Shift+P");
    }

    [Fact]
    public async Task The_separator_between_caps_is_hidden_from_assistive_technology ()
    {
        await GotoComponentAsync(Route);

        var separators = Component("combo").Locator(".veto-kbd__separator");

        // Three keys, two separators.
        await Expect(separators).ToHaveCountAsync(2);
        await Expect(separators.First).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(separators.First).ToHaveTextAsync("+");
    }

    [Fact]
    public async Task Sizes_are_visibly_different ()
    {
        await GotoComponentAsync(Route);

        var small = await Component("size-sm-alone").Locator(".veto-kbd__key").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var medium = await Component("size-md").Locator(".veto-kbd__key").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var large = await Component("size-lg").Locator(".veto-kbd__key").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");

        Assert.True(small < medium, $"Small should be shorter than medium: {small} vs {medium}.");
        Assert.True(medium < large, $"Medium should be shorter than large: {medium} vs {large}.");
    }

    [Fact]
    public async Task An_inline_key_does_not_push_the_line_apart ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        // The small size exists precisely so a key can sit in a sentence; if the cap were
        // taller than the line box it would shove the surrounding text around.
        var cap = await Component("size-sm").Locator(".veto-kbd__key").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var line = await Page.Locator(".demo-inline").First
            .EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).lineHeight)");

        Assert.True(cap <= line, $"An inline cap should fit its line: {cap} vs {line}.");
    }

    [Fact]
    public async Task Symbols_replace_the_modifiers_that_have_a_glyph ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("plain")).ToContainTextAsync("Cmd");
        await Expect(Component("plain")).ToContainTextAsync("Shift");

        var symbols = Component("symbols").Locator(".veto-kbd__key");
        await Expect(symbols.Nth(0)).ToHaveTextAsync("⌘");
        await Expect(symbols.Nth(1)).ToHaveTextAsync("⇧");
        await Expect(symbols.Nth(2)).ToHaveTextAsync("↵");
    }

    [Fact]
    public async Task The_written_key_names_survive_when_symbols_are_off ()
    {
        await GotoComponentAsync(Route);

        // Off by default, because a screen reader makes a poor job of reading a glyph aloud.
        await Expect(Component("plain").Locator(".veto-kbd__key").Nth(2)).ToHaveTextAsync("Enter");
    }

    [Fact]
    public async Task A_combination_can_be_written_with_another_separator ()
    {
        await GotoComponentAsync(Route);

        var keys = Component("sep-dash").Locator(".veto-kbd__key");

        await Expect(keys).ToHaveCountAsync(3);
        await Expect(keys.Nth(2)).ToHaveTextAsync("Del");
        await Expect(Component("sep-dash").Locator(".veto-kbd__separator").First).ToHaveTextAsync("-");
    }

    [Fact]
    public async Task Keys_pressed_in_sequence_are_drawn_without_a_separator ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("sep-none").Locator(".veto-kbd__key")).ToHaveCountAsync(2);
        await Expect(Component("sep-none").Locator(".veto-kbd__separator")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_borderless_key_loses_its_cap_but_not_its_tint ()
    {
        await GotoComponentAsync(Route);

        var capped = Page.GetByTestId("capped").Locator(".veto-kbd__key").First;
        var flat = Page.GetByTestId("flat").Locator(".veto-kbd__key").First;

        Assert.NotEqual("rgba(0, 0, 0, 0)",
            await capped.EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor"));
        Assert.Equal("rgba(0, 0, 0, 0)",
            await flat.EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor"));
        Assert.Equal("none", await flat.EvaluateAsync<string>("el => getComputedStyle(el).boxShadow"));

        // The background stays: without it and without the border there would be nothing left
        // to say this is a key at all.
        Assert.Equal(
            await capped.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor"),
            await flat.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor"));
    }

    [Fact]
    public async Task A_shortcut_list_is_a_description_list ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("dl", await Component("shortcuts").EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        Assert.Equal("dt", await Page.GetByTestId("sc-palette").Locator(".veto-shortcut__description")
            .EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        Assert.Equal("dd", await Page.GetByTestId("sc-palette").Locator(".veto-shortcut__keys")
            .EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
    }

    [Fact]
    public async Task Each_row_puts_its_description_left_and_its_keys_right ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var row = Page.GetByTestId("sc-palette");
        var description = await row.Locator(".veto-shortcut__description")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().left");
        var keys = await row.Locator(".veto-shortcut__keys")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().left");

        Assert.True(keys > description, $"The keys should sit to the right: {description} vs {keys}.");
    }

    [Fact]
    public async Task Every_rows_keys_line_up_on_the_same_edge ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        // This is the whole point of the pattern: the eye runs down one column of keys
        // rather than hunting for them at the end of sentences of different lengths.
        var rights = await Component("shortcuts").EvaluateAsync<double[]>(
            "el => Array.from(el.querySelectorAll('.veto-shortcut__keys')).map(k => k.getBoundingClientRect().right)");

        Assert.Equal(7, rights.Length);
        Assert.True(rights.Max() - rights.Min() < 1, $"Keys should share an edge: {rights.Min()} to {rights.Max()}.");
    }

    [Fact]
    public async Task A_row_renders_the_keys_it_was_given ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("sc-findall")).ToContainTextAsync("Find across the project");
        await Expect(Page.GetByTestId("sc-findall-kbd").Locator(".veto-kbd__key")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task A_divided_list_draws_a_line_between_its_rows ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("shortcuts")).ToHaveClassAsync(new Regex(@"\bveto-shortcut-list--divided\b"));

        var first = await Page.GetByTestId("sc-palette")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth");
        var second = await Page.GetByTestId("sc-save")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth");

        // The line goes between rows, so the first one has none above it.
        Assert.Equal("0px", first);
        Assert.Equal("1px", second);
    }

    [Fact]
    public async Task A_compact_list_takes_less_room_per_row ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var compact = await Component("compact").Locator(".veto-shortcut").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var roomy = await Component("roomy").Locator(".veto-shortcut").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");

        Assert.True(compact < roomy, $"The compact row should be shorter: {compact} vs {roomy}.");
    }
}
