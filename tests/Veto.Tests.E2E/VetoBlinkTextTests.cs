using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for VetoBlinkText, driven against /docs/components/blink-text.</summary>
public class VetoBlinkTextTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/blink-text";

    private Task<string> VisibleAsync ( string testId )
        => Component($"{testId}-visible").EvaluateAsync<string>("el => el.textContent");

    [Fact]
    public async Task The_text_appears_over_time_rather_than_at_once ()
    {
        await GotoComponentAsync(Route);

        var first = (await VisibleAsync("basic")).Length;
        await Page.WaitForTimeoutAsync(700);
        var later = (await VisibleAsync("basic")).Length;

        Assert.True(later > first, $"The text should still be arriving: {first} -> {later} characters.");
    }

    [Fact]
    public async Task It_finishes_and_reports_that_it_has ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("done")).ToHaveTextAsync("Finished", new() { Timeout = 20_000 });

        var visible = await VisibleAsync("basic");
        Assert.Contains("mollit anim id est laborum.", visible);
    }

    [Fact]
    public async Task A_screen_reader_gets_the_whole_text_immediately ()
    {
        await GotoComponentAsync(Route);

        // Announcing a character at a time would be unusable, so the full text is present from
        // the start in a copy that stays in the accessibility tree.
        var accessible = await Component("basic-accessible").EvaluateAsync<string>("el => el.textContent");

        Assert.Contains("Lorem ipsum dolor sit amet", accessible);
        Assert.Contains("mollit anim id est laborum.", accessible);
    }

    [Fact]
    public async Task The_animated_copy_is_hidden_from_assistive_technology ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic-visible")).ToHaveAttributeAsync("aria-hidden", "true");

        // The cursor sits inside that span rather than carrying its own aria-hidden - a nested
        // one would be redundant, and what matters is that the whole subtree is out of the tree.
        var cursorHidden = await Component("basic-cursor")
            .EvaluateAsync<bool>("el => el.closest('[aria-hidden=\"true\"]') !== null");

        Assert.True(cursorHidden, "The cursor should sit inside an aria-hidden subtree.");
    }

    [Fact]
    public async Task The_accessible_copy_is_clipped_rather_than_hidden ()
    {
        await GotoComponentAsync(Route);

        // display:none or visibility:hidden would take it out of the accessibility tree, which
        // is the one thing this copy exists for.
        var display = await Component("basic-accessible").EvaluateAsync<string>("el => getComputedStyle(el).display");
        var visibility = await Component("basic-accessible").EvaluateAsync<string>("el => getComputedStyle(el).visibility");

        Assert.NotEqual("none", display);
        Assert.Equal("visible", visibility);
    }

    [Fact]
    public async Task Typing_does_not_move_the_content_below_it ()
    {
        await Page.SetViewportSizeAsync(1400, 1000);
        await GotoComponentAsync(Route);

        // PreserveSpace holds the finished footprint, so a paragraph does not reflow line by
        // line and shove the page around beneath it.
        var before = await Component("below").EvaluateAsync<double>("el => el.getBoundingClientRect().top");

        await Page.WaitForTimeoutAsync(1200);

        var after = await Component("below").EvaluateAsync<double>("el => el.getBoundingClientRect().top");

        Assert.True(Math.Abs(after - before) < 2, $"The paragraph below moved from {before}px to {after}px while typing.");
    }

    [Fact]
    public async Task Restarting_types_it_again ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("done")).ToHaveTextAsync("Finished", new() { Timeout = 20_000 });

        await Component("restart").ClickAsync();

        await Expect(Page.GetByTestId("done")).ToHaveTextAsync("Typing…");

        var justAfter = (await VisibleAsync("basic")).Length;
        Assert.True(justAfter < 120, $"A restart should begin from the start, but {justAfter} characters were already showing.");
    }

    [Fact]
    public async Task A_cursor_blinks_while_it_types ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic-cursor")).ToBeVisibleAsync();

        var animation = await Component("basic-cursor")
            .EvaluateAsync<string>("el => getComputedStyle(el).animationName");

        Assert.NotEqual("none", animation);
    }

    [Fact]
    public async Task Scrambled_glyphs_appear_ahead_of_the_cursor ()
    {
        await GotoComponentAsync(Route);

        var scramble = Component("scramble-scramble");
        await Expect(scramble).ToBeVisibleAsync(new() { Timeout = 10_000 });

        var first = await scramble.EvaluateAsync<string>("el => el.textContent");
        await Page.WaitForTimeoutAsync(400);
        var second = await scramble.EvaluateAsync<string>("el => el.textContent");

        // They flicker: two reads a moment apart should differ.
        Assert.NotEqual(first, second);

        // And they are noise, not content - never selectable as text.
        await Expect(scramble).ToHaveCSSAsync("user-select", "none");
    }

    [Fact]
    public async Task A_stream_is_revealed_as_it_arrives ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal(string.Empty, (await VisibleAsync("stream")).Trim());

        await Component("stream-start").ClickAsync();

        await Expect(Component("stream-visible")).ToContainTextAsync("Lorem ipsum", new() { Timeout = 10_000 });

        // The whole stream eventually lands, drained at the reveal rate rather than dumped.
        await Expect(Component("stream-visible")).ToContainTextAsync("commodo consequat.", new() { Timeout = 20_000 });
    }

    [Fact]
    public async Task Word_mode_reveals_whole_words ()
    {
        await GotoComponentAsync(Route);

        await Page.WaitForTimeoutAsync(900);

        var visible = (await VisibleAsync("by-word")).Trim();

        // A partial word would mean it is still revealing character by character.
        Assert.False(
            visible.Length > 0 && char.IsLetter(visible[^1]) && !WordLineStartsWith(visible),
            $"Word mode should land on whole words, but showed '{visible}'.");
    }

    /// <summary>Whether the revealed text is a whole-word prefix of the source passage.</summary>
    private static bool WordLineStartsWith ( string revealed )
    {
        const string source =
            "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque " +
            "laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi " +
            "architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas " +
            "sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos.";

        if (!source.StartsWith(revealed, StringComparison.Ordinal))
        {
            return false;
        }

        // Either the passage ended, or the next character is a space - so no word is cut in half.
        return revealed.Length == source.Length || char.IsWhiteSpace(source[revealed.Length]);
    }

    [Fact]
    public async Task A_finished_stream_completes_and_drops_its_cursor ()
    {
        await GotoComponentAsync(Route);

        await Component("stream-start").ClickAsync();

        // Before the fix the reveal kept polling an ended stream for ever: no OnCompleted, and
        // the cursor never left.
        await Expect(Page.GetByTestId("stream-done")).ToHaveTextAsync("Finished", new() { Timeout = 20_000 });
        await Expect(Component("stream-cursor")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Clicking_the_text_skips_to_the_end ()
    {
        await GotoComponentAsync(Route);

        await Component("basic").ClickAsync();

        await Expect(Page.GetByTestId("done")).ToHaveTextAsync("Finished", new() { Timeout = 2_000 });
        Assert.Contains("mollit anim id est laborum.", await VisibleAsync("basic"));
    }
}
