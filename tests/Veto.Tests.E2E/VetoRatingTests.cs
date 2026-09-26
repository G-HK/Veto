using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoRating</c>, driven against /docs/components/rating.</summary>
public class VetoRatingTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/rating";

    /// <summary>The focusable row - one tab stop for the whole rating.</summary>
    private ILocator Row ( string testId ) => Component(testId).Locator(".veto-rating__items");

    private ILocator Items ( string testId ) => Component(testId).Locator(".veto-rating__item");

    [Fact]
    public async Task Renders_six_icons_by_default_and_reports_itself_as_a_slider ()
    {
        await GotoComponentAsync(Route);

        await Expect(Items("basic")).ToHaveCountAsync(6);

        var row = Row("basic");
        await Expect(row).ToHaveAttributeAsync("role", "slider");
        await Expect(row).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(row).ToHaveAttributeAsync("aria-valuemax", "6");
        await Expect(row).ToHaveAttributeAsync("aria-valuenow", "3");
        await Expect(row).ToHaveAttributeAsync("aria-valuetext", "3 of 6");
    }

    [Fact]
    public async Task The_label_is_bound_to_the_row ()
    {
        await GotoComponentAsync(Route);

        var id = await Row("basic").GetAttributeAsync("id");

        Assert.False(string.IsNullOrWhiteSpace(id));
        await Expect(Row("basic")).ToHaveAttributeAsync("id", new Regex(@"^veto-rating-\d+$"));

        // A div cannot be the target of <label for>; the name has to come through aria-labelledby.
        var labelId = await Component("basic").Locator("label").GetAttributeAsync("id");
        await Expect(Row("basic")).ToHaveAttributeAsync("aria-labelledby", labelId!);
        await Expect(Row("basic")).ToHaveAccessibleNameAsync("How was it?");
    }

    [Fact]
    public async Task Clicking_the_label_focuses_the_row ()
    {
        await GotoComponentAsync(Route);

        await Component("basic").Locator("label").ClickAsync();

        await Expect(Row("basic")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Home_and_end_jump_to_the_ends_without_scrolling ()
    {
        await Page.SetViewportSizeAsync(1200, 500);
        await GotoComponentAsync(Route);

        var row = Row("basic");
        await row.ScrollIntoViewIfNeededAsync();
        await row.FocusAsync();
        var before = await Page.EvaluateAsync<double>("() => window.scrollY");

        await row.PressAsync("End");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "6");

        await row.PressAsync("ArrowDown");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "5");

        await row.PressAsync("Home");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "0");

        Assert.Equal(before, await Page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [Fact]
    public async Task Item_label_names_the_score_and_follows_the_pointer ()
    {
        await GotoComponentAsync(Route);

        var label = Page.GetByTestId("named-item-label");
        await Expect(label).ToHaveTextAsync("Good");

        await Page.GetByTestId("named-pick-5").HoverAsync();
        await Expect(label).ToHaveTextAsync("Great");
    }

    [Fact]
    public async Task Each_icon_reports_how_much_of_it_is_earned ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("basic-item-3")).ToHaveAttributeAsync("data-fill", "full");
        await Expect(Page.GetByTestId("basic-item-4")).ToHaveAttributeAsync("data-fill", "empty");
    }

    [Fact]
    public async Task Clicking_an_icon_sets_the_score ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("basic-pick-5").ClickAsync();

        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "5");
        await Expect(Page.GetByTestId("basic-output")).ToHaveTextAsync("5");
        await Expect(Page.GetByTestId("basic-item-5")).ToHaveAttributeAsync("data-fill", "full");
    }

    [Fact]
    public async Task Clicking_the_current_score_clears_it ()
    {
        await GotoComponentAsync(Route);

        // The demo starts at three.
        await Page.GetByTestId("basic-pick-3").ClickAsync();

        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "0");
        await Expect(Page.GetByTestId("basic-item-1")).ToHaveAttributeAsync("data-fill", "empty");
    }

    [Fact]
    public async Task Arrow_keys_walk_the_scale ()
    {
        await GotoComponentAsync(Route);

        await Row("basic").FocusAsync();

        await Row("basic").PressAsync("ArrowRight");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "4");

        await Row("basic").PressAsync("ArrowLeft");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "3");

        await Row("basic").PressAsync("Delete");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "0");
    }

    [Fact]
    public async Task The_score_cannot_be_driven_past_either_end ()
    {
        await GotoComponentAsync(Route);

        await Row("basic").FocusAsync();

        for (var i = 0; i < 8; i++)
        {
            await Row("basic").PressAsync("ArrowRight");
        }

        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "6");

        for (var i = 0; i < 8; i++)
        {
            await Row("basic").PressAsync("ArrowLeft");
        }

        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "0");
    }

    [Fact]
    public async Task Max_sets_the_length_of_the_row ()
    {
        await GotoComponentAsync(Route);

        await Expect(Items("max-3")).ToHaveCountAsync(3);
        await Expect(Items("max-10")).ToHaveCountAsync(10);
        await Expect(Row("max-10")).ToHaveAttributeAsync("aria-valuemax", "10");
    }

    [Fact]
    public async Task Halves_can_be_picked_and_are_drawn_as_half_an_icon ()
    {
        await GotoComponentAsync(Route);

        // The demo starts at 2.5, so the third icon is half earned.
        await Expect(Component("half")).ToHaveAttributeAsync("data-value", "2.5");
        await Expect(Page.GetByTestId("half-item-3")).ToHaveAttributeAsync("data-fill", "half");

        await Page.GetByTestId("half-half-5").ClickAsync();

        await Expect(Component("half")).ToHaveAttributeAsync("data-value", "4.5");
        await Expect(Page.GetByTestId("half-item-5")).ToHaveAttributeAsync("data-fill", "half");
        await Expect(Page.GetByTestId("half-item-4")).ToHaveAttributeAsync("data-fill", "full");
    }

    [Fact]
    public async Task A_half_is_clipped_to_half_the_icon_whichever_icon_it_is ()
    {
        await GotoComponentAsync(Route);

        // The clip is what makes halves work for every icon, so measure it on the hearts.
        await Expect(Page.GetByTestId("half-heart-item-4")).ToHaveAttributeAsync("data-fill", "half");

        var filled = Page.GetByTestId("half-heart-item-4").Locator(".veto-rating__icon--filled");
        var width = await filled.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var whole = await Page.GetByTestId("half-heart-item-4")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(Math.Abs(width - whole / 2) < 1.5, $"Half of {whole}px should be clipped, got {width}px.");
    }

    [Fact]
    public async Task Arrow_keys_move_by_a_half_when_halves_are_allowed ()
    {
        await GotoComponentAsync(Route);

        await Row("half").FocusAsync();
        await Row("half").PressAsync("ArrowRight");

        await Expect(Component("half")).ToHaveAttributeAsync("data-value", "3");

        await Row("half").PressAsync("ArrowRight");
        await Expect(Component("half")).ToHaveAttributeAsync("data-value", "3.5");
    }

    [Fact]
    public async Task Hovering_previews_the_score_without_committing_it ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("basic-pick-6").HoverAsync();

        await Expect(Component("basic")).ToHaveAttributeAsync("data-preview", "6");
        await Expect(Page.GetByTestId("basic-item-6")).ToHaveAttributeAsync("data-fill", "full");

        // Nothing was picked, so the bound value is untouched.
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "3");
    }

    [Fact]
    public async Task Named_scores_reach_the_tooltip_and_the_announced_value ()
    {
        await GotoComponentAsync(Route);

        await Expect(Row("named")).ToHaveAttributeAsync("aria-valuetext", "4 of 5, Good");
        await Expect(Page.GetByTestId("named-pick-5")).ToHaveAttributeAsync("title", "5 of 5, Great");
        await Expect(Page.GetByTestId("named-value")).ToHaveTextAsync("4 of 5, Good");
    }

    [Fact]
    public async Task Size_and_colour_land_as_modifier_classes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("size-sm")).ToHaveClassAsync(new Regex(@"\bveto-rating--sm\b"));
        await Expect(Component("size-lg")).ToHaveClassAsync(new Regex(@"\bveto-rating--lg\b"));
        await Expect(Component("color-success")).ToHaveClassAsync(new Regex(@"\bveto-rating--success\b"));
        await Expect(Component("half")).ToHaveClassAsync(new Regex(@"\bveto-rating--half\b"));
    }

    [Fact]
    public async Task A_large_rating_draws_larger_icons ()
    {
        await GotoComponentAsync(Route);

        var small = await Component("size-sm").Locator(".veto-rating__item").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var large = await Component("size-lg").Locator(".veto-rating__item").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(large > small, $"Large should out-measure small: {small}px vs {large}px.");
    }

    [Fact]
    public async Task ReadOnly_and_disabled_ratings_drop_their_hit_targets ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("readonly").Locator(".veto-rating__hit")).ToHaveCountAsync(0);
        await Expect(Component("disabled").Locator(".veto-rating__hit")).ToHaveCountAsync(0);

        await Expect(Row("readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Row("disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Row("disabled")).ToHaveAttributeAsync("tabindex", "-1");

        // Still focusable and still keyboard-inert.
        await Row("readonly").FocusAsync();
        await Row("readonly").PressAsync("ArrowRight");
        await Expect(Component("readonly")).ToHaveAttributeAsync("data-value", "4.5");
    }

    [Fact]
    public async Task Validation_message_replaces_the_helper_text ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("submit").ClickAsync();

        await Expect(Row("score")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Component("score").Locator("[role='alert']")).ToContainTextAsync("Pick at least one star.");

        await Page.GetByTestId("score-pick-4").ClickAsync();
        await Page.GetByTestId("submit").ClickAsync();

        await Expect(Page.GetByTestId("submit-value")).ToHaveTextAsync("Saved: 4");
    }
}
