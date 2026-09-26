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
        Assert.Equal("backwards", await text.EvaluateAsync<string>("el => getComputedStyle(el).animationFillMode"));
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
    [Fact]
    public async Task The_prompt_sign_is_whatever_you_give_it()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("prompt-arrow-line").Locator(".veto-terminal-line__prompt"))
            .ToHaveTextAsync("›");
        await Expect(Component("prompt-powershell-line").Locator(".veto-terminal-line__prompt"))
            .ToHaveTextAsync("PS>");
    }

    [Fact]
    public async Task PromptContent_replaces_the_sign_and_is_not_hidden()
    {
        await GotoComponentAsync(Route);

        var prompt = Component("prompt-content-line").Locator(".veto-terminal-line__prompt");

        await Expect(prompt.Locator("svg")).ToHaveCountAsync(1);

        // A bare $ before every line is noise and is hidden; content put there deliberately is
        // presumed to be saying something, so it stays in the accessibility tree.
        await Expect(prompt).Not.ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task An_editable_prompt_is_labelled_rather_than_hidden()
    {
        await GotoComponentAsync(Route);

        var prompt = Component("prompt-editable-line-prompt");

        // Focusable, so aria-hidden would strand a keyboard user on a control their screen
        // reader will not announce. It carries a real name instead.
        await Expect(prompt).Not.ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(prompt).ToHaveAttributeAsync("aria-label", "Prompt");

        await prompt.FocusAsync();
        await Expect(prompt).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Editing_the_prompt_reports_back_to_the_page()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("prompt-readout")).ToHaveTextAsync("Bound prompt: $");

        var prompt = Component("prompt-editable-line-prompt");

        await prompt.FillAsync("veto@host:~#");
        await prompt.BlurAsync();

        // Two-way: the page is told, which is the whole point of making it editable.
        await Expect(Component("prompt-readout")).ToHaveTextAsync("Bound prompt: veto@host:~#");
        Assert.Equal("veto@host:~#", await prompt.InputValueAsync());
    }

    [Fact]
    public async Task The_prompt_is_never_part_of_what_is_copied()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        await Component("prompt-powershell-copy").ClickAsync();

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        Assert.Equal("dotnet test", clipboard);
        Assert.DoesNotContain("PS>", clipboard);
    }

    [Fact]
    public async Task Copying_raises_a_toast()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        await Component("basic-copy").ClickAsync();

        // The icon swapping to a tick is small and easy to miss; the toast is what actually
        // confirms the copy to someone whose eye is on the command.
        await Expect(Page.Locator(".veto-toast").First)
            .ToContainTextAsync("Command copied to clipboard");
    }

    [Fact]
    public async Task A_code_block_copy_raises_a_toast_too()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        // Every copy button on the site reports the same way - the code samples under each
        // demo are VetoCode, and they toast just as the terminal does.
        await Component("demo-basic-code-copy").ClickAsync();

        await Expect(Page.Locator(".veto-toast").First)
            .ToContainTextAsync("Code copied to clipboard");
    }

    [Fact]
    public async Task The_site_shows_at_most_five_toasts_at_the_bottom_right()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        foreach (var id in new[]
                 {
                     "basic-copy", "session-copy", "chrome-named-copy",
                     "prompt-arrow-copy", "prompt-powershell-copy", "copy-explicit-copy"
                 })
        {
            await Component(id).ClickAsync();
        }

        // Six raised, five kept: the oldest steps aside rather than the stack growing down the
        // whole page. MaxToasts is what decides that, not the page.
        await Expect(Page.Locator(".veto-toast")).ToHaveCountAsync(5);

        var stack = Page.GetByTestId("toasts-bottom-right");
        await Expect(stack).ToHaveCountAsync(1);

        // Anchored to the bottom right corner, so a toast never covers what was just clicked
        // at the top of the page.
        var box = await stack.BoundingBoxAsync();
        var viewport = Page.ViewportSize!;

        Assert.NotNull(box);
        Assert.True(
            box!.Y + box.Height > viewport.Height / 2,
            $"expected the stack in the lower half, its bottom edge was at {box.Y + box.Height}px of {viewport.Height}px");
        Assert.True(
            box.X + box.Width > viewport.Width / 2,
            $"expected the stack on the right, its right edge was at {box.X + box.Width}px of {viewport.Width}px");
    }


    [Fact]
    public async Task Wide_characters_are_not_clipped_once_typed ()
    {
        await GotoComponentAsync(Route);

        var text = Component("typed-wide").Locator(".veto-terminal-line__text");

        // TypingDuration is 0.6s and Loop is off, so it has long finished by now.
        await Page.WaitForTimeoutAsync(1500);
        var clipped = await text.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1");
        Assert.False(clipped, "The typed command should end fully revealed.");
    }

    [Fact]
    public async Task Max_height_scrolls_a_long_session ()
    {
        await GotoComponentAsync(Route);

        var scrolls = await Component("long").Locator(".veto-terminal-box__body")
            .EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight");
        Assert.True(scrolls);
    }

    [Fact]
    public async Task A_line_typed_once_stays_visible_when_it_finishes ()
    {
        await GotoComponentAsync(Route);

        var text = Component("typed-once-line").Locator(".veto-terminal-line__text");

        // TypingDuration is 3s. The animation used to hold its last frame, width 0.
        await Page.WaitForTimeoutAsync(4000);
        var width = await text.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var natural = await text.EvaluateAsync<double>("el => el.scrollWidth");

        Assert.True(width > 0 && width + 1 >= natural, $"Expected the whole command visible, but it was {width} of {natural}px wide.");
    }
}