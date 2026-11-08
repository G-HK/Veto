using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoMenu</c> and its items, driven against /docs/components/menu.</summary>
public class VetoMenuTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/menu";

    [Fact]
    public async Task A_menu_starts_closed_and_says_so ()
    {
        await GotoComponentAsync(Route);

        var trigger = Component("basic-trigger");

        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "menu");
        await Expect(Component("basic-panel")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Clicking_the_trigger_opens_the_panel_and_clicking_it_again_closes_it ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();

        var panel = Component("basic-panel");

        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveAttributeAsync("role", "menu");
        await Expect(Component("basic-trigger")).ToHaveAttributeAsync("aria-expanded", "true");

        await Component("basic-trigger").ClickAsync();

        await Expect(panel).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Choosing_an_item_runs_it_and_closes_the_menu ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();
        await Component("basic-duplicate").ClickAsync();

        await Expect(Page.GetByTestId("chosen")).ToHaveTextAsync("Duplicate");
        await Expect(Component("basic-panel")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Focus_returns_to_the_trigger_when_the_menu_closes ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();
        await Component("basic-share").ClickAsync();

        // The reader chose from a list that no longer exists; focus has to land somewhere
        // deliberate, and the trigger is the only sensible place.
        await Expect(Component("basic-trigger")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Clicking_outside_closes_the_menu_without_choosing_anything ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();
        await Expect(Component("basic-panel")).ToBeVisibleAsync();

        // The scrim covers the page, so a click anywhere else lands on it.
        await Page.Mouse.ClickAsync(5, 5);

        await Expect(Component("basic-panel")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("chosen")).ToHaveTextAsync("nothing yet");
    }

    [Fact]
    public async Task Escape_closes_the_menu_and_hands_focus_back ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();
        await Expect(Component("basic-panel")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Component("basic-panel")).ToHaveCountAsync(0);
        await Expect(Component("basic-trigger")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task A_hover_menu_opens_on_the_pointer_and_closes_when_it_leaves ()
    {
        await GotoComponentAsync(Route);

        await Component("hover").HoverAsync();

        await Expect(Component("hover-panel")).ToBeVisibleAsync();

        // Somewhere else on the page entirely, so pointerleave fires.
        await Page.Mouse.MoveAsync(5, 5);

        await Expect(Component("hover-panel")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_hover_menu_stays_open_while_the_pointer_moves_onto_an_item ()
    {
        await GotoComponentAsync(Route);

        await Component("hover").HoverAsync();
        await Expect(Component("hover-panel")).ToBeVisibleAsync();

        // Crossing the gap between the trigger and the panel must not shut the menu - that is
        // what the bridge over the offset is for. Hovering the item proves the crossing worked.
        await Component("hover-json").HoverAsync();
        await Expect(Component("hover-panel")).ToBeVisibleAsync();

        await Component("hover-json").ClickAsync();
        await Expect(Page.GetByTestId("chosen")).ToHaveTextAsync("JSON");
    }

    [Fact]
    public async Task A_hover_menu_can_still_be_opened_by_a_click ()
    {
        await GotoComponentAsync(Route);

        // Hover is never the only way in: touch has no hover at all.
        await Component("hover-trigger").ClickAsync();

        await Expect(Component("hover-panel")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_disabled_item_is_announced_as_disabled_and_cannot_be_chosen ()
    {
        await GotoComponentAsync(Route);

        await Component("hover-trigger").ClickAsync();

        var disabled = Component("hover-xlsx");

        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).ToBeDisabledAsync();
    }

    [Fact]
    public async Task An_icon_only_item_keeps_its_text_as_its_accessible_name ()
    {
        await GotoComponentAsync(Route);

        await Component("appearance-icons-trigger").ClickAsync();

        var item = Component("appearance-icons-csv");

        // The label is what a screen reader reads and what the tooltip shows, so hiding the
        // text is a saving of space rather than of meaning.
        await Expect(item).ToHaveAttributeAsync("aria-label", "CSV");
        await Expect(item).ToHaveAttributeAsync("title", "CSV");
        await Expect(item).Not.ToContainTextAsync("CSV");
    }

    [Fact]
    public async Task A_text_only_item_drops_the_icon_rather_than_leaving_a_gap ()
    {
        await GotoComponentAsync(Route);

        await Component("appearance-text-trigger").ClickAsync();

        var item = Component("appearance-text-csv");

        await Expect(item).ToContainTextAsync("CSV");
        await Expect(item.Locator(".veto-menu-item__icon")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task An_icon_and_text_item_shows_both ()
    {
        await GotoComponentAsync(Route);

        await Component("appearance-both-trigger").ClickAsync();

        var items = Component("appearance-both-panel").Locator(".veto-menu-item");

        await Expect(items.First).ToContainTextAsync("CSV");
        await Expect(items.First.Locator(".veto-menu-item__icon")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task The_arrow_keys_walk_the_items_and_wrap ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();

        // Down opens the menu on its first item rather than merely opening it, which is what
        // the menu-button pattern prescribes.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Component("basic-rename")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Component("basic-duplicate")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Component("basic-rename")).ToBeFocusedAsync();

        // Up from the first item wraps to the last rather than dead-ending.
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Component("basic-delete")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Home_and_End_jump_to_the_ends_of_the_menu ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Component("basic-delete")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(Component("basic-rename")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Enter_on_the_trigger_opens_the_menu_at_its_first_item ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Component("basic-panel")).ToBeVisibleAsync();
        await Expect(Component("basic-rename")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task The_arrow_keys_skip_a_disabled_item ()
    {
        await GotoComponentAsync(Route);

        await Component("hover-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(Component("hover-csv")).ToBeFocusedAsync();

        // CSV, JSON, PDF, then wrap - the disabled spreadsheet is never landed on.
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Component("hover-pdf")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task The_items_are_the_only_things_in_the_tab_order_the_menu_does_not_own ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();

        // Every item is tabindex="-1": the trigger is the menu's single tab stop, and the
        // arrows move between the items.
        var items = Component("basic-panel").Locator(".veto-menu-item");
        var count = await items.CountAsync();

        Assert.True(count > 0, "The panel should contain items.");

        for (var i = 0; i < count; i++)
        {
            await Expect(items.Nth(i)).ToHaveAttributeAsync("tabindex", "-1");
        }
    }

    [Fact]
    public async Task Tab_closes_the_menu_rather_than_leaving_it_hanging_over_the_page ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();
        await Expect(Component("basic-panel")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Tab");

        await Expect(Component("basic-panel")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_checkable_item_reports_its_state_and_leaves_the_menu_open ()
    {
        await GotoComponentAsync(Route);

        await Component("columns-trigger").ClickAsync();

        var due = Component("columns-due");

        await Expect(due).ToHaveAttributeAsync("role", "menuitemcheckbox");
        await Expect(due).ToHaveAttributeAsync("aria-checked", "false");

        await due.ClickAsync();

        // KeepOpen: a reader toggling columns is about to toggle another one.
        await Expect(Component("columns-panel")).ToBeVisibleAsync();
        await Expect(due).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Page.GetByTestId("columns-value")).ToContainTextAsync("Due date");
    }

    [Fact]
    public async Task A_selected_item_is_a_highlight_rather_than_a_checkbox ()
    {
        await GotoComponentAsync(Route);

        await Component("sort-trigger").ClickAsync();
        await Component("sort-size").ClickAsync();

        await Expect(Page.GetByTestId("sort-value")).ToHaveTextAsync("Size");

        await Component("sort-trigger").ClickAsync();

        var size = Component("sort-size");

        await Expect(size).ToHaveAttributeAsync("role", "menuitem");
        await Expect(size).ToHaveClassAsync(new Regex(@"\bveto-menu-item--selected\b"));
    }

    [Fact]
    public async Task An_item_with_an_href_is_a_real_link ()
    {
        await GotoComponentAsync(Route);

        await Component("links-trigger").ClickAsync();

        var github = Component("links-github");

        Assert.Equal("a", await github.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        await Expect(github).ToHaveAttributeAsync("href", "https://github.com");
        await Expect(github).ToHaveAttributeAsync("role", "menuitem");
    }

    [Fact]
    public async Task A_divider_is_a_separator_and_not_an_item ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();

        await Expect(Component("basic-divider")).ToHaveAttributeAsync("role", "separator");
        await Expect(Component("basic-panel").Locator(".veto-menu-item")).ToHaveCountAsync(4);
    }

    [Fact]
    public async Task A_disabled_menu_cannot_be_opened ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("disabled-trigger")).ToBeDisabledAsync();
        await Expect(Component("disabled-panel")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Placement_puts_the_panel_where_it_was_asked_for ()
    {
        await GotoComponentAsync(Route);

        await Component("place-bottom-start-trigger").ClickAsync();

        var trigger = await Component("place-bottom-start-trigger").BoundingBoxAsync();
        var panel = await Component("place-bottom-start-panel").BoundingBoxAsync();

        Assert.NotNull(trigger);
        Assert.NotNull(panel);
        Assert.True(panel!.Y > trigger!.Y, $"A bottom panel should sit below its trigger: {trigger.Y} vs {panel.Y}.");

        await Page.Keyboard.PressAsync("Escape");
        await Component("place-top-start-trigger").ClickAsync();

        var topTrigger = await Component("place-top-start-trigger").BoundingBoxAsync();
        var topPanel = await Component("place-top-start-panel").BoundingBoxAsync();

        Assert.NotNull(topTrigger);
        Assert.NotNull(topPanel);
        Assert.True(topPanel!.Y < topTrigger!.Y, $"A top panel should sit above its trigger: {topTrigger.Y} vs {topPanel.Y}.");
    }

    [Fact]
    public async Task Sizes_are_visibly_different ()
    {
        await GotoComponentAsync(Route);

        var small = await Component("size-sm-trigger").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var medium = await Component("size-md-trigger").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var large = await Component("size-lg-trigger").EvaluateAsync<double>("el => el.getBoundingClientRect().height");

        Assert.True(small < medium, $"Small should be shorter than medium: {small} vs {medium}.");
        Assert.True(medium < large, $"Medium should be shorter than large: {medium} vs {large}.");
    }

    [Fact]
    public async Task Component_styles_come_from_veto_tokens ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").ClickAsync();

        var background = await Component("basic-panel")
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var token = await Page.EvaluateAsync<string>(
            "() => getComputedStyle(document.documentElement).getPropertyValue('--veto-color-surface-raised').trim()");

        Assert.False(string.IsNullOrWhiteSpace(token), "The raised-surface token should be defined.");
        Assert.False(string.IsNullOrWhiteSpace(background), "The panel should have a painted background.");
    }

    [Fact]
    public async Task Typing_a_letter_jumps_to_the_next_item_starting_with_it ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Component("basic-rename")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("d");
        await Expect(Component("basic-duplicate")).ToBeFocusedAsync();

        // Again from there: the next "d" after Duplicate, not the first one again.
        await Page.Keyboard.PressAsync("d");
        await Expect(Component("basic-delete")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("s");
        await Expect(Component("basic-share")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task The_context_menu_takes_home_end_and_type_ahead ()
    {
        // VetoContextMenu's live demo is the right-click menu on the button page.
        await GotoComponentAsync("docs/components/button");

        await Component("rightclick").ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Component("rightclick-rename")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Component("rightclick-delete")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(Component("rightclick-rename")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("d");
        await Expect(Component("rightclick-copy")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("d");
        await Expect(Component("rightclick-delete")).ToBeFocusedAsync();
    }
}
