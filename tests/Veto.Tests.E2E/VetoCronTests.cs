using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for the <c>VetoCron</c> builder, driven against /docs/components/cron.
/// The parser, describer and scheduler are covered directly in <see cref="VetoCronExpressionTests"/>;
/// what is left for the browser is the building, which is all this file does.
/// </summary>
public class VetoCronTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/cron";

    private ILocator Expression ( string testId ) => Page.GetByTestId($"{testId}-input");

    private ILocator Description ( string testId ) => Page.GetByTestId($"{testId}-description");

    private ILocator Mode ( string testId, string mode ) => Page.GetByTestId($"{testId}-mode-{mode}");

    [Fact]
    public async Task Starts_on_the_expression_it_was_given ()
    {
        await GotoComponentAsync(Route);

        await Expect(Expression("basic")).ToHaveValueAsync("0 9 * * 1-5");
        await Expect(Description("basic"))
            .ToHaveTextAsync("At 09:00, only on Monday, Tuesday, Wednesday, Thursday and Friday");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-valid", "true");
    }

    [Fact]
    public async Task Picking_a_shape_writes_the_expression ()
    {
        await GotoComponentAsync(Route);

        await Mode("basic", "minutes").ClickAsync();

        await Expect(Component("basic")).ToHaveAttributeAsync("data-mode", "minutes");
        await Expect(Expression("basic")).ToHaveValueAsync("*/5 * * * *");
        await Expect(Description("basic")).ToHaveTextAsync("Every 5 minutes");
        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("*/5 * * * *");
    }

    [Fact]
    public async Task Changing_a_control_rewrites_the_expression ()
    {
        await GotoComponentAsync(Route);

        await Mode("basic", "minutes").ClickAsync();
        await Page.GetByTestId("basic-interval").FillAsync("20");
        await Page.GetByTestId("basic-interval").BlurAsync();

        await Expect(Expression("basic")).ToHaveValueAsync("*/20 * * * *");
        await Expect(Description("basic")).ToHaveTextAsync("Every 20 minutes");
    }

    [Fact]
    public async Task The_daily_shape_builds_a_time_of_day ()
    {
        await GotoComponentAsync(Route);

        await Mode("basic", "daily").ClickAsync();
        await Page.GetByTestId("basic-hour").FillAsync("17");
        await Page.GetByTestId("basic-hour").BlurAsync();
        await Page.GetByTestId("basic-minute").FillAsync("45");
        await Page.GetByTestId("basic-minute").BlurAsync();

        await Expect(Expression("basic")).ToHaveValueAsync("45 17 * * *");
        await Expect(Description("basic")).ToHaveTextAsync("At 17:45");
    }

    [Fact]
    public async Task The_weekly_shape_toggles_days ()
    {
        await GotoComponentAsync(Route);

        await Mode("basic", "weekly").ClickAsync();

        // The page starts on weekdays, so the builder adopts 1-5.
        await Expect(Expression("basic")).ToHaveValueAsync("0 9 * * 1,2,3,4,5");

        await Page.GetByTestId("basic-day-1").ClickAsync();
        await Expect(Expression("basic")).ToHaveValueAsync("0 9 * * 2,3,4,5");

        await Page.GetByTestId("basic-day-6").ClickAsync();
        await Expect(Expression("basic")).ToHaveValueAsync("0 9 * * 2,3,4,5,6");
    }

    [Fact]
    public async Task A_selected_day_says_it_is_selected ()
    {
        await GotoComponentAsync(Route);

        await Mode("basic", "weekly").ClickAsync();

        await Expect(Page.GetByTestId("basic-day-1")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.GetByTestId("basic-day-0")).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Fact]
    public async Task The_last_day_cannot_be_unticked ()
    {
        await GotoComponentAsync(Route);

        await Mode("basic", "weekly").ClickAsync();

        foreach (var day in new[] { 1, 2, 3, 4 })
        {
            await Page.GetByTestId($"basic-day-{day}").ClickAsync();
        }

        await Expect(Expression("basic")).ToHaveValueAsync("0 9 * * 5");

        // Unticking the last one would silently mean "every day", which is not what was asked.
        await Page.GetByTestId("basic-day-5").ClickAsync();
        await Expect(Expression("basic")).ToHaveValueAsync("0 9 * * 5");
    }

    [Fact]
    public async Task Switching_shape_keeps_the_time_already_chosen ()
    {
        await GotoComponentAsync(Route);

        await Mode("basic", "daily").ClickAsync();
        await Page.GetByTestId("basic-hour").FillAsync("22");
        await Page.GetByTestId("basic-hour").BlurAsync();
        await Expect(Expression("basic")).ToHaveValueAsync("0 22 * * *");

        // Moving to another tab should carry the schedule over, not reset it to a default.
        await Mode("basic", "weekly").ClickAsync();
        await Expect(Expression("basic")).ToHaveValueAsync("0 22 * * 1,2,3,4,5");
    }

    [Fact]
    public async Task Typing_an_expression_moves_to_the_advanced_shape ()
    {
        await GotoComponentAsync(Route);

        await Expression("basic").FillAsync("0 0 29 2 *");

        await Expect(Component("basic")).ToHaveAttributeAsync("data-mode", "advanced");
        await Expect(Description("basic")).ToHaveTextAsync("At 00:00, on the 29th, only in February");
    }

    [Fact]
    public async Task The_next_runs_are_listed ()
    {
        await GotoComponentAsync(Route);

        var runs = Page.GetByTestId("basic-runs").Locator(".veto-cron__run");

        await Expect(runs).ToHaveCountAsync(5);

        // The page pins "now" to 08:00 on Saturday 12 September 2026, so the first weekday
        // run is the following Monday. A moving clock would make this meaningless.
        await Expect(runs.First).ToHaveTextAsync("Mon 14 Sep 2026, 09:00");
    }

    [Fact]
    public async Task An_expression_that_never_fires_says_so ()
    {
        await GotoComponentAsync(Route);

        await Expression("basic").FillAsync("0 0 30 2 *");

        await Expect(Page.GetByTestId("basic-runs").Locator(".veto-cron__run--none"))
            .ToContainTextAsync("Never");
    }

    [Fact]
    public async Task A_bad_expression_names_the_field_that_is_wrong ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("invalid")).ToHaveAttributeAsync("data-valid", "false");
        await Expect(Description("invalid")).ToHaveTextAsync("Hours: '99' is outside 0-23.");
        await Expect(Description("invalid")).ToHaveAttributeAsync("role", "alert");
        await Expect(Expression("invalid")).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Fact]
    public async Task The_either_day_rule_is_warned_about ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("trap-warning"))
            .ToContainTextAsync("fires when either matches");

        // And the runs prove it: the 1st, then Mondays.
        var runs = Page.GetByTestId("trap-runs").Locator(".veto-cron__run");
        await Expect(runs.First).ToHaveTextAsync("Mon 14 Sep 2026, 00:00");
    }

    [Fact]
    public async Task An_ordinary_expression_is_not_warned_about ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("basic-warning")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_supplied_describer_replaces_the_built_in_one ()
    {
        await GotoComponentAsync(Route);

        // English through CronExpressionDescriptor reads differently from Veto's own describer,
        // which is how we know the seam is actually being used.
        await Expect(Description("localised")).ToContainTextAsync("09:00");

        await Page.GetByTestId("lang-fr").ClickAsync();
        await Expect(Description("localised")).ToContainTextAsync("09:00");
        await Expect(Description("localised")).Not.ToContainTextAsync("only on Monday");

        await Page.GetByTestId("lang-de").ClickAsync();
        var german = await Description("localised").TextContentAsync();

        await Page.GetByTestId("lang-es").ClickAsync();
        var spanish = await Description("localised").TextContentAsync();

        Assert.NotEqual(german, spanish);
    }

    [Fact]
    public async Task Only_the_allowed_shapes_are_offered ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("narrow-modes").Locator("button")).ToHaveCountAsync(3);
        await Expect(Mode("narrow", "weekly")).ToBeVisibleAsync();
        await Expect(Mode("narrow", "minutes")).ToHaveCountAsync(0);
        await Expect(Component("narrow")).ToHaveAttributeAsync("data-mode", "weekly");
    }

    [Fact]
    public async Task The_parts_can_be_switched_off ()
    {
        await GotoComponentAsync(Route);

        // Reviewing a schedule rather than building one.
        await Expect(Page.GetByTestId("review-modes")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("review-input")).ToHaveCountAsync(0);
        await Expect(Description("review")).ToHaveTextAsync("Every 10 minutes");
        await Expect(Page.GetByTestId("review-runs").Locator(".veto-cron__run")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task A_read_only_builder_still_explains_itself ()
    {
        await GotoComponentAsync(Route);

        await Expect(Expression("readonly")).ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(Description("readonly")).ToHaveTextAsync("At 03:00, only on Sunday");
        await Expect(Page.GetByTestId("readonly-runs").Locator(".veto-cron__run")).ToHaveCountAsync(2);

        // Focusable and readable, but it will not change.
        await Expression("readonly").ClickAsync();
        await Expression("readonly").PressAsync("9");
        await Expect(Expression("readonly")).ToHaveValueAsync("0 3 * * 0");
    }

    [Fact]
    public async Task A_disabled_builder_cannot_be_touched ()
    {
        await GotoComponentAsync(Route);

        await Expect(Expression("disabled")).ToBeDisabledAsync();
        await Expect(Page.GetByTestId("disabled-hour")).ToBeDisabledAsync();
        await Expect(Component("disabled")).ToHaveClassAsync(new Regex(@"\bveto-cron--disabled\b"));
    }

    [Fact]
    public async Task Borderless_drops_the_box_and_every_control_inside_it ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("borderless")).ToHaveClassAsync(new Regex(@"\bveto-cron--borderless\b"));

        var builder = await Page.GetByTestId("borderless-builder")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor");
        var input = await Expression("borderless")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor");
        var number = await Page.GetByTestId("borderless-hour")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor");
        var day = await Page.GetByTestId("borderless-day-0")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor");

        // Every one of them, not just the outer box.
        Assert.Equal("rgba(0, 0, 0, 0)", builder);
        Assert.Equal("rgba(0, 0, 0, 0)", input);
        Assert.Equal("rgba(0, 0, 0, 0)", number);
        Assert.Equal("rgba(0, 0, 0, 0)", day);
    }

    [Fact]
    public async Task A_borderless_builder_still_builds ()
    {
        await GotoComponentAsync(Route);

        await Expect(Expression("borderless")).ToHaveValueAsync("0 7 * * 1,3,5");

        await Page.GetByTestId("borderless-day-0").ClickAsync();

        await Expect(Expression("borderless")).ToHaveValueAsync("0 7 * * 0,1,3,5");
        await Expect(Page.GetByTestId("borderless-day-0")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Fact]
    public async Task A_borderless_control_keeps_its_focus_ring ()
    {
        await GotoComponentAsync(Route);

        // The borders were decoration; the ring is how a keyboard user knows where they are,
        // so losing one must not lose the other.
        await Expression("borderless").FocusAsync();

        var shadow = await Expression("borderless")
            .EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");

        Assert.NotEqual("none", shadow);
    }

    [Fact]
    public async Task The_describer_is_usable_without_the_builder ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("sample-quarter-text")).ToHaveTextAsync("Every 15 minutes");
        await Expect(Page.GetByTestId("sample-newyear-text"))
            .ToHaveTextAsync("At 00:00, on the 1st, only in January");
    }
}
