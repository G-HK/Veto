using System.Globalization;
using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for VetoLink and the underline reveal, driven against
/// /docs/components/link.
/// </summary>
/// <remarks>
/// The line is a pseudo-element scaled from 0 to 1 on the X axis, so it is read as a computed
/// transform: <c>matrix(0, …)</c> means bare, <c>matrix(1, …)</c> means drawn.
/// </remarks>
public class VetoLinkTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/link";

    /// <summary>How far along the X axis the underline is drawn, 0 to 1.</summary>
    private static async Task<double> RevealedAsync ( ILocator target )
    {
        var transform = await target.EvaluateAsync<string>(
            "el => getComputedStyle(el, '::after').transform");

        if (transform is "none")
        {
            return 0;
        }

        // matrix(a, b, c, d, tx, ty) - a is the X scale.
        var scaleX = transform[(transform.IndexOf('(') + 1)..].Split(',')[0];

        return double.Parse(scaleX, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Polls, because the reveal is a transition: read in the same tick as the hover and the
    /// value is still whatever it was before the pointer arrived.
    /// </summary>
    private static async Task<bool> SettlesAtAsync ( ILocator target, double expected )
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (Math.Abs(await RevealedAsync(target) - expected) < 0.05)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return false;
    }

    [Fact]
    public async Task A_link_starts_bare ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal(0, await RevealedAsync(Component("reveal")), 2);

        // Bare means bare: no browser underline either, or the reveal would have nothing to do.
        var decoration = await Component("reveal").EvaluateAsync<string>(
            "el => getComputedStyle(el).textDecorationLine");

        Assert.Equal("none", decoration);
    }

    [Fact]
    public async Task Hovering_draws_the_line_out_from_the_middle ()
    {
        await GotoComponentAsync(Route);

        var link = Component("reveal");

        // Growing from the centre is the whole effect, so the origin is worth pinning down.
        var origin = await link.EvaluateAsync<string>(
            "el => getComputedStyle(el, '::after').transformOrigin.split(' ')[0]");
        var width = await link.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.Equal(width / 2, double.Parse(origin.Replace("px", string.Empty), CultureInfo.InvariantCulture), 1);

        await link.HoverAsync();

        Assert.True(await SettlesAtAsync(link, 1), "Hovering should have drawn the underline.");
    }

    [Fact]
    public async Task The_keyboard_gets_the_same_line_as_the_pointer ()
    {
        await GotoComponentAsync(Route);

        // Focused by tabbing rather than by script: :focus-visible answers the keyboard, and a
        // programmatic focus is not always treated as one.
        await Component("reveal").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        var second = Component("reveal-usage");

        await Expect(second).ToBeFocusedAsync();
        Assert.True(await SettlesAtAsync(second, 1), "A link reached by Tab should show its underline.");
    }

    [Fact]
    public async Task Always_draws_the_line_without_being_asked ()
    {
        await GotoComponentAsync(Route);

        Assert.Equal(1, await RevealedAsync(Component("mode-always")), 2);
    }

    [Fact]
    public async Task None_has_no_line_to_draw ()
    {
        await GotoComponentAsync(Route);

        var content = await Component("mode-none").EvaluateAsync<string>(
            "el => getComputedStyle(el, '::after').content");

        Assert.Equal("none", content);

        // And no underline of the browser's own either, which is the easy way for this mode to
        // end up being the only one that draws a line.
        Assert.Equal("none", await Component("mode-none").EvaluateAsync<string>(
            "el => getComputedStyle(el).textDecorationLine"));

        await Component("mode-none").HoverAsync();

        Assert.Equal("none", await Component("mode-none").EvaluateAsync<string>(
            "el => getComputedStyle(el, '::after').content"));
    }

    [Fact]
    public async Task The_line_takes_the_colour_it_is_given ()
    {
        await GotoComponentAsync(Route);

        var background = await Component("color-cyan").EvaluateAsync<string>(
            "el => getComputedStyle(el, '::after').backgroundColor");

        Assert.Equal("rgb(34, 211, 238)", background);
    }

    [Fact]
    public async Task An_inherited_link_keeps_the_text_colour_but_not_the_line_colour ()
    {
        await GotoComponentAsync(Route);

        var link = Component("prose-theme");

        var text = await link.EvaluateAsync<string>("el => getComputedStyle(el).color");
        var paragraph = await link.EvaluateAsync<string>(
            "el => getComputedStyle(el.closest('p')).color");
        var line = await link.EvaluateAsync<string>(
            "el => getComputedStyle(el, '::after').backgroundColor");

        Assert.Equal(paragraph, text);
        Assert.Equal("rgb(244, 63, 94)", line);
    }

    [Fact]
    public async Task A_link_to_the_current_page_says_so ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("current-self")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Component("current-other")).Not.ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task A_navigation_row_reveals_its_label_from_anywhere_in_the_row ()
    {
        await Page.SetViewportSizeAsync(1400, 1000);
        await GotoComponentAsync(Route);

        var row = Component("link-menu-installation");
        var label = row.Locator(".veto-nav-menu__label");

        Assert.Equal(0, await RevealedAsync(label), 2);

        // Deliberately not over the words: the row is what the pointer aims at, and a line that
        // only appeared over the text itself would flicker as the pointer crossed the padding.
        // Scrolled before it is measured: the box is in viewport coordinates, and measuring
        // first would aim the pointer at wherever the row used to be.
        await row.ScrollIntoViewIfNeededAsync();

        var box = await row.BoundingBoxAsync() ?? throw new InvalidOperationException("The row has no box.");
        await Page.Mouse.MoveAsync((float) (box.X + box.Width - 6), (float) (box.Y + box.Height / 2));

        Assert.True(await SettlesAtAsync(label, 1), "Hovering the row should have revealed the label's underline.");
    }

    [Fact]
    public async Task The_menu_still_marks_the_page_it_is_on ()
    {
        await GotoComponentAsync(Route);

        // The underline is decoration added on top; it must not have replaced what was there.
        await Expect(Component("link-menu-overview")).Not.ToHaveAttributeAsync("aria-current", "page");
        await Expect(Component("link-menu-installation")).ToBeVisibleAsync();
    }
}
