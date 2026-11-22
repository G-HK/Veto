using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoSlider</c>, driven against /docs/components/slider.</summary>
public class VetoSliderTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/slider";

    private ILocator Lower ( string testId ) => Component(testId).Locator("input.veto-slider__input--lower");

    private ILocator Upper ( string testId ) => Component(testId).Locator("input.veto-slider__input--upper");

    [Fact]
    public async Task Renders_a_range_input_with_a_label_bound_to_it ()
    {
        await GotoComponentAsync(Route);

        var input = Lower("basic");
        var id = await input.GetAttributeAsync("id");

        Assert.False(string.IsNullOrWhiteSpace(id));
        await Expect(input).ToHaveAttributeAsync("type", "range");
        await Expect(input).ToHaveAttributeAsync("id", new Regex(@"^veto-slider-\d+$"));
        await Expect(Component("basic").Locator("label")).ToHaveAttributeAsync("for", id!);
    }

    [Fact]
    public async Task Defaults_to_a_zero_to_hundred_scale ()
    {
        await GotoComponentAsync(Route);

        await Expect(Lower("basic")).ToHaveAttributeAsync("min", "0");
        await Expect(Lower("basic")).ToHaveAttributeAsync("max", "100");
        await Expect(Lower("basic")).ToHaveAttributeAsync("step", "1");
    }

    [Fact]
    public async Task Arrow_keys_move_the_value_by_one_step ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "40");

        await Lower("basic").FocusAsync();
        await Lower("basic").PressAsync("ArrowRight");

        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "41");
        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("41%");
        await Expect(Page.GetByTestId("basic-output")).ToHaveTextAsync("41");
    }

    [Fact]
    public async Task Min_max_and_step_come_from_the_parameters ()
    {
        await GotoComponentAsync(Route);

        var input = Lower("bounds");
        await Expect(input).ToHaveAttributeAsync("min", "-20");
        await Expect(input).ToHaveAttributeAsync("max", "40");
        await Expect(input).ToHaveAttributeAsync("step", "5");

        await input.FocusAsync();
        await input.PressAsync("ArrowRight");

        await Expect(Component("bounds")).ToHaveAttributeAsync("data-value", "25");
    }

    [Fact]
    public async Task Prefix_format_and_suffix_dress_the_printed_value ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("decimals-value")).ToHaveTextAsync("$1.2m");
    }

    [Fact]
    public async Task Generated_marks_land_on_the_rail ()
    {
        await GotoComponentAsync(Route);

        var marks = Component("marks").Locator(".veto-slider__mark");
        var labels = Component("marks").Locator(".veto-slider__mark-label");

        // 0, 25, 50, 75 and 100.
        await Expect(marks).ToHaveCountAsync(5);
        await Expect(labels).ToHaveCountAsync(5);
        await Expect(labels.First).ToHaveTextAsync("0");
        await Expect(labels.Last).ToHaveTextAsync("100");
    }

    [Fact]
    public async Task Marks_can_be_words_rather_than_numbers ()
    {
        await GotoComponentAsync(Route);

        var labels = Component("named").Locator(".veto-slider__mark-label");

        await Expect(labels).ToHaveCountAsync(3);
        await Expect(labels.Nth(0)).ToHaveTextAsync("Low");
        await Expect(labels.Nth(1)).ToHaveTextAsync("Medium");
        await Expect(labels.Nth(2)).ToHaveTextAsync("High");
    }

    [Fact]
    public async Task Snapping_settles_the_thumb_on_a_mark ()
    {
        await GotoComponentAsync(Route);

        // Evenly spaced marks are handed to the browser as the step, so one press is one stop.
        await Expect(Lower("named")).ToHaveAttributeAsync("step", "50");
        await Expect(Component("named")).ToHaveAttributeAsync("data-value", "50");

        await Lower("named").FocusAsync();
        await Lower("named").PressAsync("ArrowRight");

        await Expect(Component("named")).ToHaveAttributeAsync("data-value", "100");
        await Expect(Page.GetByTestId("named-output")).ToHaveTextAsync("100");

        await Lower("named").PressAsync("ArrowLeft");
        await Expect(Component("named")).ToHaveAttributeAsync("data-value", "50");
    }

    [Fact]
    public async Task A_range_renders_two_thumbs_with_their_own_names ()
    {
        await GotoComponentAsync(Route);

        await Expect(Lower("range")).ToHaveAttributeAsync("aria-label", "Lowest price");
        await Expect(Upper("range")).ToHaveAttributeAsync("aria-label", "Highest price");

        await Expect(Component("range")).ToHaveAttributeAsync("data-value", "200");
        await Expect(Component("range")).ToHaveAttributeAsync("data-upper-value", "800");
        await Expect(Page.GetByTestId("range-value")).ToHaveTextAsync("$200 – $800");
    }

    [Fact]
    public async Task The_two_thumbs_of_a_range_move_independently ()
    {
        await GotoComponentAsync(Route);

        await Lower("range").FocusAsync();
        await Lower("range").PressAsync("ArrowRight");
        await Expect(Component("range")).ToHaveAttributeAsync("data-value", "250");
        await Expect(Component("range")).ToHaveAttributeAsync("data-upper-value", "800");

        await Upper("range").FocusAsync();
        await Upper("range").PressAsync("ArrowLeft");
        await Expect(Component("range")).ToHaveAttributeAsync("data-upper-value", "750");
        await Expect(Component("range")).ToHaveAttributeAsync("data-value", "250");
    }

    [Fact]
    public async Task A_range_thumb_cannot_pass_the_other_one ()
    {
        await GotoComponentAsync(Route);

        // Drive the low thumb well past the high one; it must stop where the high thumb sits.
        await Lower("range").FillAsync("950");

        await Expect(Component("range")).ToHaveAttributeAsync("data-value", "800");
        await Expect(Component("range")).ToHaveAttributeAsync("data-upper-value", "800");
    }

    [Fact]
    public async Task Size_colour_and_variant_land_as_modifier_classes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("size-sm")).ToHaveClassAsync(new Regex(@"\bveto-slider--sm\b"));
        await Expect(Component("size-lg")).ToHaveClassAsync(new Regex(@"\bveto-slider--lg\b"));
        await Expect(Component("color-success")).ToHaveClassAsync(new Regex(@"\bveto-slider--success\b"));
        await Expect(Component("variant-filled")).ToHaveClassAsync(new Regex(@"\bveto-slider--filled\b"));
        await Expect(Component("range")).ToHaveClassAsync(new Regex(@"\bveto-slider--range\b"));
    }

    [Fact]
    public async Task The_fill_grows_with_the_value ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var fill = Component("basic").Locator(".veto-slider__fill");
        var before = await fill.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        await Lower("basic").FillAsync("90");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "90");

        var after = await fill.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(after > before, $"The fill should grow: {before}px -> {after}px.");
    }

    [Fact]
    public async Task Disabled_slider_cannot_be_moved ()
    {
        await GotoComponentAsync(Route);

        await Expect(Lower("disabled")).ToBeDisabledAsync();
        await Expect(Component("disabled")).ToHaveClassAsync(new Regex(@"\bveto-slider--disabled\b"));
        await Expect(Component("disabled")).ToHaveAttributeAsync("data-value", "70");
    }

    [Fact]
    public async Task ReadOnly_slider_reports_itself_and_springs_back ()
    {
        await GotoComponentAsync(Route);

        var input = Lower("readonly");
        await Expect(input).ToHaveAttributeAsync("aria-readonly", "true");

        await input.FocusAsync();
        await input.PressAsync("ArrowRight");

        await Expect(Component("readonly")).ToHaveAttributeAsync("data-value", "70");
        await Expect(input).ToHaveValueAsync("70");
    }

    [Fact]
    public async Task Validation_message_replaces_the_helper_text ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("submit").ClickAsync();

        await Expect(Lower("effort")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Component("effort").Locator("[role='alert']")).ToContainTextAsync("Effort must be at least 3.");

        await Lower("effort").FillAsync("5");
        await Page.GetByTestId("submit").ClickAsync();

        await Expect(Page.GetByTestId("submit-value")).ToHaveTextAsync("Saved: 5");
    }

    [Fact]
    public async Task Input_attributes_ignore_the_display_format ()
    {
        await GotoComponentAsync(Route);

        // Format="N0" prints "5,000"; the attribute must stay a number the browser can read,
        // or it falls back to max=100 and the thumb pins to the end.
        await Expect(Lower("bubble")).ToHaveAttributeAsync("max", "5000");
        await Expect(Lower("bubble")).ToHaveAttributeAsync("step", "250");
        await Expect(Lower("bubble")).ToHaveValueAsync("1500");
    }

    [Fact]
    public async Task Always_on_bubble_shows_the_formatted_value ()
    {
        await GotoComponentAsync(Route);

        var bubble = Page.GetByTestId("bubble-bubble");
        await Expect(bubble).ToHaveTextAsync("$1,500");
        await Expect(bubble).ToHaveCSSAsync("opacity", "1");

        await Lower("bubble").FocusAsync();
        await Lower("bubble").PressAsync("ArrowRight");
        await Expect(bubble).ToHaveTextAsync("$1,750");
    }

    [Fact]
    public async Task Active_bubble_shows_only_while_its_thumb_is_in_use ()
    {
        await GotoComponentAsync(Route);

        var bubble = Page.GetByTestId("bubble-range-bubble");
        await Expect(bubble).ToHaveCSSAsync("opacity", "0");

        await Lower("bubble-range").FocusAsync();
        await Expect(bubble).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public async Task Thumbs_that_meet_at_the_top_can_still_be_pulled_apart ()
    {
        await GotoComponentAsync(Route);

        var root = Component("bubble-range");
        await Expect(root).ToHaveClassAsync(new Regex(@"\bveto-slider--lower-on-top\b"));

        // Both thumbs sit at 99. Drag from there to the left: the pointer must find the low thumb.
        await Upper("bubble-range").ScrollIntoViewIfNeededAsync();
        var box = await Upper("bubble-range").BoundingBoxAsync();
        Assert.NotNull(box);
        var y = box!.Y + box.Height / 2;
        await Page.Mouse.MoveAsync(box.X + box.Width - 4, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, y, new() { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Expect(root).Not.ToHaveAttributeAsync("data-value", "99");
        await Expect(root).ToHaveAttributeAsync("data-upper-value", "99");
    }
}
