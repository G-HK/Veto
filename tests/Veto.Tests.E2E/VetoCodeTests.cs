using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoCode</c>, driven against /docs/components/code.</summary>
public class VetoCodeTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/code";

    private async Task GrantClipboardAsync ()
        => await Context.GrantPermissionsAsync(
            ["clipboard-read", "clipboard-write"],
            new() { Origin = App.BaseUrl });

    [Fact]
    public async Task Code_is_shown_as_text_not_markup ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic").Locator("code")).ToContainTextAsync("<VetoButton");
        await Expect(Component("basic").Locator("code button")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Line_numbers_follow_the_first_line_number ()
    {
        await GotoComponentAsync(Route);

        var lines = Component("numbered").Locator(".veto-code__line");
        await Expect(lines).ToHaveCountAsync(8);
        await Expect(lines.First).ToHaveAttributeAsync("data-line", "1");

        var excerpt = Component("excerpt").Locator(".veto-code__line");
        await Expect(excerpt.First).ToHaveAttributeAsync("data-line", "41");
        await Expect(excerpt.Last).ToHaveAttributeAsync("data-line", "48");
    }

    [Fact]
    public async Task Highlighted_lines_take_numbers_and_ranges ()
    {
        await GotoComponentAsync(Route);

        var highlighted = Component("numbered").Locator(".veto-code__line--highlight");
        await Expect(highlighted).ToHaveCountAsync(3);
        await Expect(highlighted.Nth(0)).ToHaveAttributeAsync("data-line", "3");
        await Expect(highlighted.Nth(2)).ToHaveAttributeAsync("data-line", "6");

        await Expect(Component("excerpt").Locator(".veto-code__line--highlight")).ToHaveAttributeAsync("data-line", "43");
    }

    [Fact]
    public async Task Wrap_folds_long_lines_instead_of_scrolling ()
    {
        await GotoComponentAsync(Route);

        var scrolls = await Component("nowrap").Locator("pre")
            .EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth");
        var wraps = await Component("wrap").Locator("pre")
            .EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth");

        Assert.True(scrolls, "Without Wrap the long line should scroll sideways.");
        Assert.False(wraps, "With Wrap the long line should fold.");
    }

    [Fact]
    public async Task Copying_numbered_code_copies_only_the_code ()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        await Page.GetByTestId("numbered-copy").ClickAsync();
        await Expect(Page.GetByTestId("numbered-copy")).ToHaveAttributeAsync("aria-label", "Copied");

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.StartsWith("public sealed class GreetingService", clipboard);
    }

    [Fact]
    public async Task Copying_works_for_code_given_as_content ()
    {
        await GrantClipboardAsync();
        await GotoComponentAsync(Route);

        await Page.GetByTestId("content-copy").ClickAsync();
        await Expect(Page.GetByTestId("content-copy")).ToHaveAttributeAsync("aria-label", "Copied");

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.Equal("dotnet add package Veto", clipboard);
    }
}
