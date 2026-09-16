using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for <c>VetoTerminalBox</c> and <c>VetoTerminalLine</c>, driven against
/// /docs/components/terminal-box.
/// </summary>
public class VetoTerminalBoxTests(BlazorAppFixture app) : VetoPageTest(app)
{
    private const string Route = "docs/components/terminal-box";

    private async Task GrantClipboardAsync()
    {
        // Scoped to the app's origin. Granted without one it applies to whatever the context
        // is showing - about:blank, before the first navigation - and the real page is still
        // refused with NotAllowedError.
        await Context.GrantPermissionsAsync(
            ["clipboard-read", "clipboard-write"],
            new() { Origin = App.BaseUrl });
    }

    [Fact]
    public async Task The_shorthand_command_renders_one_line()
    {
        await GotoComponentAsync(Route);

        var line = Component("basic-line");

        await Expect(line).ToBeVisibleAsync();
        await Expect(line).ToContainTextAsync("dotnet add package Veto");
        await Expect(line.Locator("[data-terminal-command]")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task The_prompt_is_hidden_from_assistive_technology()
    {
        await GotoComponentAsync(Route);

        var prompt = Component("basic-line").Locator(".veto-terminal-line__prompt");

        // It is decoration, and reading it before every line would be noise.
        await Expect(prompt).ToHaveTextAsync("$");
        await Expect(prompt).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task The_title_and_icon_name_the_window()
    {
        await GotoComponentAsync(Route);

        var title = Component("chrome-named").Locator(".veto-terminal-box__title");

        await Expect(title).ToContainTextAsync("deploy@veto-prod");
        await Expect(title.Locator("svg")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ShowHeader_false_removes_the_bar()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("chrome-headless").Locator(".veto-terminal-box__head")).ToHaveCountAsync(0);

        // And with no bar there is no copy button either, since that is where it lives.
        await Expect(Component("chrome-headless-copy")).ToHaveCountAsync(0);

        // The body then carries the window's rounding, or the corners would be square.
        var radius = await Component("chrome-headless").Locator(".veto-terminal-box__body")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius");

        Assert.NotEqual("0px", radius);
    }

    [Fact]
    public async Task A_typed_line_reveals_text_that_is_already_present()
    {
        await GotoComponentAsync(Route);

        var text = Component("typed-loop-line").Locator(".veto-terminal-line__text");

        // The animation is a width reveal, not an insertion: the whole command is in the DOM
        // from the first frame, so it stays selectable and is read out in one piece.
        Assert.Equal("npx create-veto-app@latest", await text.TextContentAsync());

        var animation = await text.EvaluateAsync<string>("el => getComputedStyle(el).animationName");

        // Blazor's CSS isolation appends the scope id to keyframe names too, so the running
        // animation is never the bare name.
        Assert.StartsWith("veto-terminal-type", animation);
    }

    [Fact]
    public async Task The_typing_animation_steps_once_per_character()
    {
        await GotoComponentAsync(Route);

        var line = Component("typed-loop-line");
        var chars = await line.EvaluateAsync<string>(
            "el => getComputedStyle(el).getPropertyValue('--veto-terminal-chars').trim()");

        Assert.Equal("npx create-veto-app@latest".Length.ToString(), chars);

        var timing = await line.Locator(".veto-terminal-line__text")
            .EvaluateAsync<string>("el => getComputedStyle(el).animationTimingFunction");

        Assert.Contains(chars, timing);
    }

    [Fact]
    public async Task Loop_false_types_once_and_stays()
    {
        await GotoComponentAsync(Route);

        var text = Component("typed-once-line").Locator(".veto-terminal-line__text");

        Assert.Equal("1", await text.EvaluateAsync<string>("el => getComputedStyle(el).animationIterationCount"));
        Assert.Equal("forwards", await text.EvaluateAsync<string>("el => getComputedStyle(el).animationFillMode"));
        Assert.Equal("3s", await text.EvaluateAsync<string>("el => getComputedStyle(el).animationDuration"));
    }

    [Fact]
    public async Task An_untyped_line_carries_no_animation()
    {
        await GotoComponentAsync(Route);

        var text = Component("basic-line").Locator(".veto-terminal-line__text");

        Assert.Equal("none", await text.EvaluateAsync<string>("el => getComputedStyle(el).animationName"));
    }

    [Fact]
    public async Task Output_lines_render_muted_and_without_a_prompt()
    {
        await GotoComponentAsync(Route);

        var output = Component("session-output");

        await Expect(output).ToContainTextAsync("Adding PackageReference");
        await Expect(output.Locator(".veto-terminal-line__output")).ToHaveCountAsync(1);
        await Expect(output.Locator(".veto-terminal-line__prompt")).ToHaveCountAsync(0);

        // Output is the program's, not something you would paste back into a shell.
        await Expect(output.Locator("[data-terminal-command]")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Copy_takes_the_commands_and_leaves_the_output()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        await Component("session-copy").ClickAsync();
        await Expect(Component("session-copy")).ToHaveAttributeAsync("aria-label", "Copied");

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        // Line endings are normalised before comparing: the component joins commands with \n,
        // but the Windows clipboard hands them back as \r\n. That is the platform's business,
        // not the component's, and asserting on it would make this spec pass only on Windows.
        Assert.Equal(
            "dotnet add package Veto\ndotnet run",
            clipboard.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task Copy_leaves_the_prompt_behind()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        await Component("basic-copy").ClickAsync();

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        // Pasting a prompt into a shell is an error, so it is never part of what is copied.
        Assert.Equal("dotnet add package Veto", clipboard);
        Assert.DoesNotContain("$", clipboard);
    }

    [Fact]
    public async Task CopyText_overrides_what_the_window_shows()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        await Component("copy-explicit-copy").ClickAsync();
        await Expect(Component("copy-result")).ToHaveTextAsync("Copy accepted: True");

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        Assert.Equal(
            "docker compose --profile full --env-file .env.production up --build --detach",
            clipboard);
    }

    [Fact]
    public async Task The_copy_confirmation_returns_to_its_resting_label()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        var copy = Component("basic-copy");

        await copy.ClickAsync();
        await Expect(copy).ToHaveAttributeAsync("aria-label", "Copied");

        // The confirmation is feedback, not the button's new name.
        await Expect(copy).ToHaveAttributeAsync("aria-label", "Copy command", new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task Copyable_false_renders_no_copy_button()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("copy-none-copy")).ToHaveCountAsync(0);
        await Expect(Component("copy-none").Locator(".veto-terminal-box__head")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task The_window_is_dark_in_either_theme()
    {
        await GotoComponentAsync(Route);

        var body = Component("basic").Locator(".veto-terminal-box__body");

        // Compared against the token the component resolves rather than a literal colour: the
        // docs site runs its own theme, and a terminal is dark because it is a terminal, not
        // because the page happens to be.
        var expected = await body.EvaluateAsync<string>(
            "el => getComputedStyle(el).getPropertyValue('--veto-color-terminal-surface').trim()");
        var actual = await body.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        Assert.False(string.IsNullOrWhiteSpace(expected), "the terminal surface token should resolve");

        // Both normalised through the browser so #000000 and rgb(0, 0, 0) compare equal.
        var normalised = await Page.EvaluateAsync<string>(
            @"value => { const d = document.createElement('div'); d.style.color = value;
                document.body.appendChild(d);
                const c = getComputedStyle(d).color; d.remove(); return c; }",
            expected);

        Assert.Equal(normalised, actual);
    }
}