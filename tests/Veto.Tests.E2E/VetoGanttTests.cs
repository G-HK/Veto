using System.Globalization;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for VetoGantt, driven against /docs/components/gantt.</summary>
public class VetoGanttTests(BlazorAppFixture app) : VetoPageTest(app)
{
    private const string Route = "docs/components/gantt";

    private Task<double> LeftAsync(string testId)
        => Component(testId).EvaluateAsync<double>("el => el.getBoundingClientRect().left");

    private Task<double> WidthAsync(string testId)
        => Component(testId).EvaluateAsync<double>("el => el.getBoundingClientRect().width");

    private Task<double> IndentAsync(string testId)
        => Component(testId).EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).paddingInlineStart)");

    [Fact]
    public async Task Rows_are_nested_under_their_project()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // A task indents past the project it belongs to, so the tree is legible at a glance.
        var project = await IndentAsync("chart-label-platform");
        var task = await IndentAsync("chart-label-design");

        Assert.True(task > project, $"A task ({task}px) should indent past its project ({project}px).");
    }

    [Fact]
    public async Task Collapsing_a_project_hides_its_tasks()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("chart-label-design")).ToBeVisibleAsync();

        await Component("chart-twisty-platform").ClickAsync();

        await Expect(Component("chart-twisty-platform")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Component("chart-label-design")).ToHaveCountAsync(0);

        // The project itself stays put.
        await Expect(Component("chart-label-platform")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_later_task_starts_further_along_the_timeline()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Design starts 2 March, App shell starts 9 March.
        var earlier = await LeftAsync("chart-bar-design");
        var later = await LeftAsync("chart-bar-shell");

        Assert.True(later > earlier, $"A later task should start further right: {earlier}px vs {later}px.");
    }

    [Fact]
    public async Task A_longer_task_draws_a_wider_bar()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Design is 12 days, the feasibility spike is 5.
        var longer = await WidthAsync("chart-bar-design");
        var shorter = await WidthAsync("chart-bar-spike");

        Assert.True(longer > shorter, $"A longer task should be wider: {longer}px vs {shorter}px.");
    }

    [Fact]
    public async Task Changing_the_scale_redraws_the_timeline()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var atDay = await WidthAsync("chart-bar-design");

        await Component("chart-scale-month").ClickAsync();
        await Expect(Component("chart-scale-month")).ToHaveAttributeAsync("aria-pressed", "true");

        var atMonth = await WidthAsync("chart-bar-design");

        Assert.True(atMonth < atDay, $"The same task should be narrower at month scale: {atDay}px -> {atMonth}px.");
    }

    [Fact]
    public async Task The_text_filter_narrows_the_rows_but_keeps_their_project()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Component("chart-search").Locator("input").FillAsync("spike");

        await Expect(Component("chart-label-spike")).ToBeVisibleAsync();
        await Expect(Component("chart-label-design")).ToHaveCountAsync(0);

        // The project stays, so a matching task is never shown detached from its parent.
        await Expect(Component("chart-label-mobile")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_category_can_be_filtered_out()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("chart-label-copy")).ToBeVisibleAsync();

        await Component("chart-category-marketing").ClickAsync();

        await Expect(Component("chart-category-marketing")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Component("chart-label-copy")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Filtering_everything_out_says_so()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("empty-empty")).ToBeVisibleAsync();
        await Expect(Component("empty-empty")).ToContainTextAsync("No tasks match");
    }

    [Fact]
    public async Task Selecting_a_bar_reports_it()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Component("chart-bar-shell").ClickAsync();

        await Expect(Page.GetByTestId("selection")).ToContainTextAsync("shell");
    }

    [Fact]
    public async Task Bars_are_reachable_by_keyboard()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Bars are buttons, not decorated divs, so they focus and activate like controls.
        await Component("chart-bar-shell").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.GetByTestId("selection")).ToContainTextAsync("shell");
    }

    [Fact]
    public async Task The_today_marker_sits_after_a_task_that_started_earlier()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("chart-today-marker")).ToBeVisibleAsync();

        // The demo pins today to 18 March; the component library task starts on the 16th.
        var marker = await Component("chart-today-marker").EvaluateAsync<double>("el => parseFloat(el.style.left)");
        var bar = await Component("chart-bar-components").EvaluateAsync<double>("el => parseFloat(el.style.left)");

        Assert.True(marker > bar, $"Today ({marker}px) should sit after a task that started earlier ({bar}px).");
    }

    [Fact]
    public async Task Today_scrolls_the_timeline_back_to_now()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var scroller = Component("chart-scroller");
        await scroller.EvaluateAsync("el => el.scrollTo({ left: 0 })");

        await Component("chart-today").ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        var scrollLeft = await scroller.EvaluateAsync<double>("el => el.scrollLeft");

        Assert.True(scrollLeft > 0, "Today should scroll the timeline away from the start of the axis.");
    }

    [Fact]
    public async Task Earlier_and_later_move_the_timeline()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var scroller = Component("chart-scroller");
        var start = await scroller.EvaluateAsync<double>("el => el.scrollLeft");

        await Component("chart-next").ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        var later = await scroller.EvaluateAsync<double>("el => el.scrollLeft");

        Assert.True(later > start, $"Later should scroll forward: {start}px -> {later}px.");

        await Component("chart-previous").ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        var back = await scroller.EvaluateAsync<double>("el => el.scrollLeft");

        Assert.True(back < later, $"Earlier should scroll back: {later}px -> {back}px.");
    }

    [Fact]
    public async Task Export_reports_the_rows_that_are_currently_filtered()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // The formats live behind the toolbar's export menu, which opens on hover.
        await Component("chart-export").HoverAsync();
        await Component("chart-export-csv").ClickAsync();
        await Expect(Page.GetByTestId("export-note")).ToContainTextAsync("as Csv");

        // Narrowing the filter narrows the export: what you see is what you get.
        await Component("chart-search").Locator("input").FillAsync("spike");
        await Component("chart-export").HoverAsync();
        await Component("chart-export-json").ClickAsync();

        await Expect(Page.GetByTestId("export-note")).ToContainTextAsync("Exported 2 rows as Json");
    }

    [Fact]
    public async Task A_milestone_renders_as_a_point_rather_than_a_bar()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var width = await WidthAsync("colors-bar-b");
        var span = await WidthAsync("colors-bar-a");

        Assert.True(width < span, $"A milestone ({width}px) should be a point, not a span ({span}px).");
    }

    [Fact]
    public async Task A_chained_task_is_linked_to_the_one_it_waits_for()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("chain-link-survey-draft")).ToHaveCountAsync(1);
        await Expect(Component("chain-link-draft-review")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task An_arrow_runs_from_the_end_of_one_bar_to_the_start_of_the_next()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var path = await Component("chain-link-survey-draft").GetAttributeAsync("d");
        Assert.NotNull(path);

        // The path starts at the predecessor's right edge and finishes at the successor's left.
        var startX = double.Parse(path!.Split(' ')[1], CultureInfo.InvariantCulture);
        var endX = double.Parse(path.Split(' ')[^1], CultureInfo.InvariantCulture);

        var surveyRight = await Component("chain-bar-survey").EvaluateAsync<double>(
            "el => parseFloat(el.style.left) + parseFloat(el.style.width)");
        var draftLeft = await Component("chain-bar-draft").EvaluateAsync<double>(
            "el => parseFloat(el.style.left)");

        Assert.True(Math.Abs(startX - surveyRight) < 2, $"The arrow should leave the predecessor's end ({surveyRight}px), but started at {startX}px.");
        Assert.True(Math.Abs(endX - draftLeft) < 4, $"The arrow should arrive at the successor's start ({draftLeft}px), but ended at {endX}px.");
    }

    [Fact]
    public async Task A_broken_dependency_is_still_drawn()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Publish starts before Review ends. Rather than hide it or cut a line straight through
        // the rows, the arrow routes back - which is exactly the case worth seeing.
        var path = await Component("chain-link-review-publish").GetAttributeAsync("d");

        Assert.NotNull(path);
        Assert.True(path!.Count(c => c == 'V') >= 2, $"A back-routed arrow needs two vertical runs, but the path was '{path}'.");
    }

    [Fact]
    public async Task Selecting_a_row_picks_out_the_arrows_that_touch_it()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("chain-link-draft-review")).Not.ToHaveClassAsync(new Regex(@"--active"));

        await Component("chain-bar-draft").ClickAsync();

        // Both the arrow into the selected row and the one out of it light up.
        await Expect(Component("chain-link-survey-draft")).ToHaveClassAsync(new Regex(@"--active"));
        await Expect(Component("chain-link-draft-review")).ToHaveClassAsync(new Regex(@"--active"));

        // An arrow that does not touch it stays quiet.
        await Expect(Component("chain-link-review-publish")).Not.ToHaveClassAsync(new Regex(@"--active"));
    }

    [Fact]
    public async Task An_arrow_is_dropped_when_its_predecessor_is_collapsed_away()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("chart-link-design-shell")).ToHaveCountAsync(1);

        // Collapsing Platform hides both ends of that dependency, so the arrow goes with them
        // rather than pointing at a row that is no longer on screen.
        await Component("chart-twisty-platform").ClickAsync();

        await Expect(Component("chart-link-design-shell")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Arrows_never_swallow_a_click_meant_for_a_bar()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Expect(Component("chain-links")).ToHaveCSSAsync("pointer-events", "none");

        // Proves it end to end: the bar still receives the click.
        await Component("chain-bar-review").ClickAsync();
        await Expect(Component("chain-link-draft-review")).ToHaveClassAsync(new Regex(@"--active"));
    }

    [Fact]
    public async Task A_category_chip_is_tinted_with_that_categorys_colour()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // The chip takes its colour from the rows in that category, so the filter is also the
        // key to the bars - and the two cannot drift apart.
        var engineering = await Component("chart-category-engineering")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderColor");
        var research = await Component("chart-category-research")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderColor");

        Assert.NotEqual(engineering, research);

        var bar = await Component("chart-bar-spike")
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        Assert.Equal(bar, research);
    }

    [Fact]
    public async Task Turning_a_category_off_hollows_out_its_swatch()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var swatch = Component("chart-category-research").Locator(".veto-gantt__swatch");

        var filled = await swatch.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        Assert.DoesNotContain("rgba(0, 0, 0, 0)", filled);

        await Component("chart-category-research").ClickAsync();

        // Off reads as off without relying on the tint alone, which is easy to miss.
        var hollow = await swatch.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        Assert.Contains("rgba(0, 0, 0, 0)", hollow);
    }

    [Fact]
    public async Task A_row_can_carry_its_own_colour()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var first = await Component("colors-bar-a").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var second = await Component("colors-bar-c").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task The_chart_wide_actions_are_held_to_the_trailing_edge()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Time navigation, export and reset act on the chart as a whole; the search, the
        // category chips and the scale picker change what it shows. The chart-wide group is
        // held to the trailing edge, away from the others.
        //
        // Measured against the toolbar's own content edge rather than against the scale
        // picker: the toolbar wraps when it runs out of room, and once it has wrapped a
        // left-versus-right comparison between two groups on different rows says nothing.
        var gap = await Component("chart-actions").EvaluateAsync<double>(
            """
            el => {
                const toolbar = el.closest('.veto-gantt__toolbar');
                const padding = parseFloat(getComputedStyle(toolbar).paddingRight);

                return toolbar.getBoundingClientRect().right - padding - el.getBoundingClientRect().right;
            }
            """);

        Assert.True(Math.Abs(gap) < 2, $"The actions should end at the toolbar's trailing edge, off by {gap}px.");
    }

    [Fact]
    public async Task Today_sits_between_the_two_nudge_buttons_as_one_control()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var earlier = await Component("chart-previous").EvaluateAsync<double>("el => el.getBoundingClientRect().left");
        var today = await Component("chart-today").EvaluateAsync<double>("el => el.getBoundingClientRect().left");
        var later = await Component("chart-next").EvaluateAsync<double>("el => el.getBoundingClientRect().left");

        Assert.True(earlier < today && today < later,
            $"Earlier, Today and Later should read left to right: {earlier}px, {today}px, {later}px.");

        // The three share one border rather than carrying three of their own, which is what
        // makes them read as a single control.
        await Expect(Component("chart-actions").Locator(".veto-gantt__nav"))
            .ToHaveAttributeAsync("role", "group");
    }

    [Fact]
    public async Task The_export_menu_is_the_last_thing_in_the_toolbar()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var today = await Component("chart-today").EvaluateAsync<double>("el => el.getBoundingClientRect().right");
        var export = await Component("chart-export").EvaluateAsync<double>("el => el.getBoundingClientRect().left");

        Assert.True(export >= today, $"Export should sit after the time navigation: {today}px vs {export}px.");
    }
}
