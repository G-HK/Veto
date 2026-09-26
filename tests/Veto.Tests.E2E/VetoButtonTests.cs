using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for VetoButton, driven against /docs/components/button.</summary>
public class VetoButtonTests(BlazorAppFixture app) : VetoPageTest(app)
{
    private const string Route = "docs/components/button";

    private Task<string> TagAsync(string testId)
        => Component(testId).EvaluateAsync<string>("el => el.tagName.toLowerCase()");

    [Fact]
    public async Task Renders_a_button_by_default()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("button", await TagAsync("filled"));

        // Defaults to type="button", so a button inside a form never submits it by accident.
        await Expect(Component("filled")).ToHaveAttributeAsync("type", "button");
    }

    [Fact]
    public async Task Renders_an_anchor_when_given_an_href()
    {
        await GotoComponentAsync(Route);

        Assert.Equal("a", await TagAsync("link"));
        await Expect(Component("link")).ToHaveAttributeAsync("href", "/docs/usage");
    }

    [Fact]
    public async Task A_disabled_button_never_becomes_a_link()
    {
        await GotoComponentAsync(Route);

        // A disabled anchor is still clickable and still navigates, so it has to stay a button.
        Assert.Equal("button", await TagAsync("disabled-link"));
        await Expect(Component("disabled-link")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Sizes_differ_in_height()
    {
        await GotoComponentAsync(Route);

        var small = await Component("small").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var large = await Component("large").EvaluateAsync<double>("el => el.getBoundingClientRect().height");

        Assert.True(large > small, $"Large ({large}px) should be taller than small ({small}px).");
        Assert.True(small >= 32, $"Even a small button should stay a comfortable target, but was {small}px.");
    }

    [Fact]
    public async Task Full_width_fills_its_container()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var button = await Component("full-width").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var parent = await Component("full-width").EvaluateAsync<double>("el => el.parentElement.getBoundingClientRect().width");

        Assert.True(Math.Abs(button - parent) < 2, $"Full width should match its container: {button}px vs {parent}px.");
    }

    [Fact]
    public async Task Variants_are_visually_distinct()
    {
        await GotoComponentAsync(Route);

        var filled = await Component("filled").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var text = await Component("text").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        Assert.NotEqual(filled, text);
        Assert.Contains("rgba(0, 0, 0, 0)", text);
    }

    [Fact]
    public async Task The_danger_role_differs_from_the_default()
    {
        await GotoComponentAsync(Route);

        var primary = await Component("filled").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var danger = await Component("danger").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        Assert.NotEqual(primary, danger);
    }

    [Fact]
    public async Task Icons_render_beside_the_label()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("start-icon").Locator("svg")).ToHaveCountAsync(1);
        await Expect(Component("end-icon").Locator("svg")).ToHaveCountAsync(1);

        // Decorative, because the label already says what the button does.
        await Expect(Component("start-icon").Locator("svg")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task A_loading_button_reports_itself_busy_and_cannot_be_clicked_twice()
    {
        await GotoComponentAsync(Route);

        await Component("action").ClickAsync();

        await Expect(Component("action")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Component("action")).ToBeDisabledAsync();

        // And it comes back when the work finishes.
        await Expect(Page.GetByTestId("save-state")).ToHaveTextAsync("Saved", new() { Timeout = 10_000 });
        await Expect(Component("action")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task The_label_does_not_move_when_loading_starts()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var before = await Component("action").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        await Component("action").ClickAsync();
        var during = await Component("action").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        // A button that changes width while it works shifts everything around it.
        Assert.True(Math.Abs(before - during) < 4, $"Width changed from {before}px to {during}px while loading.");
    }

    [Fact]
    public async Task A_disabled_button_cannot_be_activated()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("disabled")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task The_button_is_operable_from_the_keyboard()
    {
        await GotoComponentAsync(Route);

        await Component("action").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.GetByTestId("save-state")).ToHaveTextAsync("Saved", new() { Timeout = 10_000 });
    }

    // ------------------------------------------------------------------ The right-click menu

    [Fact]
    public async Task Right_clicking_opens_the_menu_and_choosing_runs_it()
    {
        await GotoComponentAsync(Route);

        await Component("rightclick").ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Right });

        var menu = Component("rightclick-context-menu");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu).ToHaveAttributeAsync("aria-label", "Report actions");
        await Expect(Component("rightclick-rename")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Component("rightclick-copy")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Component("rightclick-note")).ToHaveTextAsync("Duplicated report");
        await Expect(menu).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_menu_closes_on_escape_or_a_click_away_without_running_anything()
    {
        await GotoComponentAsync(Route);

        await Component("rightclick").ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Right });
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Component("rightclick-context-menu")).ToHaveCountAsync(0);
        await Expect(Component("rightclick")).ToBeFocusedAsync();

        await Component("rightclick").ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Right });
        await Component("rightclick-context-menu-scrim").ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(Component("rightclick-context-menu")).ToHaveCountAsync(0);
        await Expect(Component("rightclick-note")).ToHaveTextAsync("Right-click the button.");
    }

    [Fact]
    public async Task A_button_without_a_menu_leaves_right_click_to_the_browser()
    {
        await GotoComponentAsync(Route);

        var prevented = await Component("filled").EvaluateAsync<bool>(
            "el => !el.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }))");

        Assert.False(prevented);
    }

    [Fact]
    public async Task A_link_button_still_runs_its_click_handler_and_opens_new_tabs_safely ()
    {
        await GotoComponentAsync(Route);

        var link = Component("tab-link");
        await Expect(link).ToHaveAttributeAsync("rel", "noopener noreferrer");

        // Stop the navigation itself; only the handler is under test.
        await link.EvaluateAsync("el => el.addEventListener('click', e => e.preventDefault())");
        await link.ClickAsync();

        await Expect(Page.GetByTestId("link-clicks")).ToHaveTextAsync("1");
    }

    [Fact]
    public async Task Loading_on_click_holds_while_the_handler_runs_and_blocks_a_second_click ()
    {
        await GotoComponentAsync(Route);

        var busy = Component("busy");
        await busy.ClickAsync();

        await Expect(busy).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(busy).ToBeDisabledAsync();
        await busy.ClickAsync(new() { Force = true });

        await Expect(busy).Not.ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Page.GetByTestId("busy-count")).ToHaveTextAsync("1");
    }

    [Fact]
    public async Task A_disabled_icon_link_does_not_navigate ()
    {
        await GotoComponentAsync("docs/components/icon-button");

        var link = Component("link-disabled");
        Assert.Equal("button", await link.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        await Expect(link).ToBeDisabledAsync();
        await Expect(Component("link")).ToHaveAttributeAsync("href", "/docs/usage");
    }
}