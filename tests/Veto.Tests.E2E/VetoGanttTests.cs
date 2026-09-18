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

    /// <summary>
    /// Waits until an element's width has settled to within a pixel of the scroller it sits in.
    /// The chart widens its days only after the page reports its width, one render later, so
    /// sampling straight after load would read the unfitted axis.
    /// </summary>
    private async Task<(double Width, double Available)> FittedWidthAsync(string testId, string scrollerTestId)
    {
        var scroller = Component(scrollerTestId);
        var element = Component(testId);

        for (var attempt = 0; attempt < 50; attempt++)
        {
            var width = await element.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
            var available = await scroller.EvaluateAsync<double>("el => el.clientWidth");

            if (Math.Abs(width - available) <= 2)
            {
                return (width, available);
            }

            await Page.WaitForTimeoutAsync(100);
        }

        return (await element.EvaluateAsync<double>("el => el.getBoundingClientRect().width"),
                await scroller.EvaluateAsync<double>("el => el.clientWidth"));
    }

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

    [Fact]
    public async Task A_bar_carries_its_details_as_a_tooltip_by_default()
    {
        await GotoComponentAsync(Route);

        var title = await Component("chart-bar-design").GetAttributeAsync("title");

        Assert.NotNull(title);
        Assert.Contains("Design system", title);
        Assert.Contains("100", title);
    }

    [Fact]
    public async Task TooltipText_rewrites_the_native_tooltip()
    {
        await GotoComponentAsync(Route);

        // Design runs 2-13 March: twelve days, both ends counted.
        await Expect(Component("tooltip-text-bar-design")).ToHaveAttributeAsync("title", "Design system - 12 days");

        // The accessible name keeps the full details whatever the tooltip says.
        var label = await Component("tooltip-text-bar-design").GetAttributeAsync("aria-label");
        Assert.Contains("Design system ·", label);
    }

    [Fact]
    public async Task TooltipTemplate_shows_a_card_while_a_bar_is_hovered()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var bar = Component("tooltip-card-bar-design");
        var card = Component("tooltip-card-tooltip");

        // The card replaces the native tooltip rather than adding to it.
        await Expect(bar).Not.ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex(".*"));
        await Expect(card).ToHaveCountAsync(0);

        await bar.HoverAsync();

        await Expect(card).ToBeVisibleAsync();
        await Expect(card).ToContainTextAsync("Design system");
        await Expect(card).ToContainTextAsync("12 days");
        await Expect(bar).ToHaveAttributeAsync("aria-describedby", new System.Text.RegularExpressions.Regex("-tooltip$"));

        await Page.Mouse.MoveAsync(0, 0);

        await Expect(card).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task TooltipTemplate_opens_on_keyboard_focus_too()
    {
        await GotoComponentAsync(Route);

        await Component("tooltip-card-bar-shell").FocusAsync();

        await Expect(Component("tooltip-card-tooltip")).ToContainTextAsync("App shell");
    }

    [Fact]
    public async Task The_tooltip_card_is_never_cut_off_by_the_chart()
    {
        await Page.SetViewportSizeAsync(1500, 900);
        await GotoComponentAsync(Route);

        var card = Component("tooltip-card-tooltip");

        // The last rows sit at the bottom of a chart that scrolls; a card kept inside the chart
        // would be clipped there. Placed against the window, it is whole wherever the bar is.
        foreach (var bar in new[] { "tooltip-card-bar-design", "tooltip-card-bar-launch", "tooltip-card-bar-ship" })
        {
            await Component(bar).HoverAsync();
            await Expect(card).ToBeVisibleAsync();

            var box = (await card.BoundingBoxAsync())!;
            var anchor = (await Component(bar).BoundingBoxAsync())!;
            var viewport = Page.ViewportSize!;

            Assert.True(box.X >= 0 && box.Y >= 0 && box.X + box.Width <= viewport.Width && box.Y + box.Height <= viewport.Height,
                $"The card for {bar} ({box.X},{box.Y} {box.Width}x{box.Height}) should be wholly inside the window.");

            // Beside the bar, not on top of it: above or below.
            Assert.True(box.Y >= anchor.Y + anchor.Height - 1 || box.Y + box.Height <= anchor.Y + 1,
                $"The card for {bar} should open above or below it, not over it.");
        }

        Assert.Equal("fixed", await card.EvaluateAsync<string>("el => getComputedStyle(el).position"));
    }

    [Fact]
    public async Task The_tooltip_card_layers_at_the_shared_token_or_its_own_z_index()
    {
        await GotoComponentAsync(Route);

        // Default: the library's --veto-z-tooltip.
        await Component("tooltip-card-bar-design").HoverAsync();
        Assert.Equal("1000", await Component("tooltip-card-tooltip").EvaluateAsync<string>("el => getComputedStyle(el).zIndex"));

        // TooltipZIndex overrides it for one chart.
        await Page.GetByTestId("ref-tooltip").ClickAsync();
        Assert.Equal("2500", await Component("driven-tooltip").EvaluateAsync<string>("el => getComputedStyle(el).zIndex"));
    }

    [Fact]
    public async Task SelectAsync_through_a_ref_selects_and_reports_like_a_click()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("ref-select").ClickAsync();

        // The bound SelectedId moved with it, so the page's own state is in step.
        await Expect(Page.GetByTestId("driven-state")).ToContainTextAsync("Selected: shell");
    }

    [Fact]
    public async Task CollapseAll_and_ExpandAll_fold_the_whole_tree()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("driven-label-design")).ToBeVisibleAsync();

        await Page.GetByTestId("ref-collapse-all").ClickAsync();

        await Expect(Component("driven-label-design")).ToHaveCountAsync(0);
        await Expect(Component("driven-label-spike")).ToHaveCountAsync(0);
        await Expect(Component("driven-label-platform")).ToBeVisibleAsync();

        await Page.GetByTestId("ref-expand-all").ClickAsync();

        await Expect(Component("driven-label-design")).ToBeVisibleAsync();
        await Expect(Component("driven-label-spike")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SetScaleAsync_through_a_ref_zooms_and_updates_the_binding()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var atDay = await WidthAsync("driven-bar-design");

        await Page.GetByTestId("ref-week").ClickAsync();

        await Expect(Page.GetByTestId("driven-state")).ToContainTextAsync("Scale: Week");
        var atWeek = await WidthAsync("driven-bar-design");
        Assert.True(atWeek < atDay, $"Week view should draw the same task narrower: {atDay}px -> {atWeek}px.");
    }

    [Fact]
    public async Task ScrollToDateAsync_brings_that_date_into_view()
    {
        await Page.SetViewportSizeAsync(1200, 1000);
        await GotoComponentAsync(Route);

        var scroller = Component("driven-scroller");
        Assert.Equal(0, await scroller.EvaluateAsync<double>("el => el.scrollLeft"));

        await Page.GetByTestId("ref-launch").ClickAsync();

        // Scrolling is smooth, so wait for it to have moved rather than sampling it once.
        await scroller.EvaluateAsync("el => new Promise(done => { const check = () => el.scrollLeft > 0 ? done() : requestAnimationFrame(check); check(); })");
        Assert.True(await scroller.EvaluateAsync<double>("el => el.scrollLeft") > 0);
    }

    [Fact]
    public async Task ShowTooltip_opens_a_rows_card_from_code_and_HideTooltip_closes_it()
    {
        await GotoComponentAsync(Route);

        var card = Component("driven-tooltip");
        await Expect(card).ToHaveCountAsync(0);

        await Page.GetByTestId("ref-tooltip").ClickAsync();

        await Expect(card).ToContainTextAsync("Design system");

        await Page.GetByTestId("ref-hide").ClickAsync();

        await Expect(card).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_chart_opens_on_todays_work_only()
    {
        await GotoComponentAsync(Route);

        // The demo pins today to 18 March. App shell (9-24 March) and the component library
        // (from 16 March) are under way, so they are listed with the project they belong to.
        await Expect(Component("today-view-label-shell")).ToBeVisibleAsync();
        await Expect(Component("today-view-label-components")).ToBeVisibleAsync();
        await Expect(Component("today-view-label-platform")).ToBeVisibleAsync();

        // Design finished on the 13th, the spike starts on the 23rd and launch in April: none of
        // it is today's work.
        await Expect(Component("today-view-label-design")).ToHaveCountAsync(0);
        await Expect(Component("today-view-label-spike")).ToHaveCountAsync(0);
        await Expect(Component("today-view-label-launch")).ToHaveCountAsync(0);

        await Expect(Component("today-view-view-today")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Fact]
    public async Task Today_spans_the_whole_axis_and_at_least_the_chart()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var scroller = Component("today-view-scroller");
        var canvas = scroller.Locator(".veto-gantt__canvas");
        var available = await scroller.EvaluateAsync<double>("el => el.clientWidth");

        // Wait for the chart to hear its width: the axis is then never narrower than the chart.
        for (var attempt = 0; attempt < 50
             && await canvas.EvaluateAsync<double>("el => el.getBoundingClientRect().width") < available - 2; attempt++)
        {
            await Page.WaitForTimeoutAsync(100);
        }

        var axis = await canvas.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(axis >= available - 2, $"Today's axis ({axis}px) should fill at least the chart ({available}px).");

        // App shell runs through today, so its bar is the whole axis: today, and nothing else.
        var bar = await WidthAsync("today-view-bar-shell");
        Assert.True(Math.Abs(bar - axis) <= 2, $"Today's bar ({bar}px) should span the whole axis ({axis}px).");
    }

    [Fact]
    public async Task The_view_switch_opens_the_whole_schedule()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("today-view-label-design")).ToHaveCountAsync(0);

        await Component("today-view-view-schedule").ClickAsync();

        await Expect(Component("today-view-view-schedule")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Component("today-view-label-design")).ToBeVisibleAsync();
        await Expect(Component("today-view-label-launch")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_short_range_is_stretched_to_fill_the_width()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Seven days at the day scale's own 40px would be 280px wide.
        var canvas = Component("fit-scroller").Locator(".veto-gantt__canvas");
        var available = await Component("fit-scroller").EvaluateAsync<double>("el => el.clientWidth");

        for (var attempt = 0; attempt < 50
             && Math.Abs(await canvas.EvaluateAsync<double>("el => el.getBoundingClientRect().width") - available) > 2; attempt++)
        {
            await Page.WaitForTimeoutAsync(100);
        }

        var width = await canvas.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(Math.Abs(width - available) <= 2, $"The axis ({width}px) should fill the chart ({available}px).");
        Assert.True(width > 280, $"The axis ({width}px) should be wider than seven days at 40px.");

        // Nothing scrolls sideways: the whole range is on screen.
        Assert.True(await Component("fit-scroller").EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1"));
    }

    [Fact]
    public async Task Work_outside_an_explicit_range_draws_no_bar()
    {
        await GotoComponentAsync(Route);

        // Design ended on the 13th, before the demo's 14-20 March range: it keeps its row in the
        // schedule view, but no stub is pinned to the edge.
        await Expect(Component("fit-label-design")).ToBeVisibleAsync();
        await Expect(Component("fit-bar-design")).ToHaveCountAsync(0);
        await Expect(Component("fit-bar-shell")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Inline_jobs_share_their_parents_line()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // No row per job...
        await Expect(Component("inline-label-compile")).ToHaveCountAsync(0);
        await Expect(Component("inline-label-test")).ToHaveCountAsync(0);

        // ...but every job has its bar, on the pipeline's own line.
        var row = await Component("inline-row-nightly").BoundingBoxAsync();

        foreach (var job in new[] { "compile", "test", "package", "deploy" })
        {
            var bar = await Component($"inline-bar-{job}").BoundingBoxAsync();

            Assert.NotNull(bar);
            Assert.True(bar!.Y >= row!.Y && bar.Y + bar.Height <= row.Y + row.Height + 1,
                $"The {job} bar ({bar.Y}px) should sit on the pipeline's line ({row.Y}-{row.Y + row.Height}px).");
        }

        // And side by side: a later job starts further along the line.
        Assert.True(await LeftAsync("inline-bar-test") > await LeftAsync("inline-bar-compile"));
    }

    [Fact]
    public async Task Unfolding_an_inline_parent_lists_its_jobs_as_rows()
    {
        await GotoComponentAsync(Route);

        var twisty = Component("inline-twisty-nightly");
        await Expect(twisty).ToHaveAttributeAsync("aria-expanded", "false");

        await twisty.ClickAsync();

        await Expect(twisty).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Component("inline-label-compile")).ToBeVisibleAsync();
        await Expect(Component("inline-label-deploy")).ToBeVisibleAsync();

        // The pipeline goes back to a summary bar over its jobs.
        await Expect(Component("inline-bar-nightly")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("veto-gantt__bar--summary"));
    }

    [Fact]
    public async Task Collapsible_off_keeps_jobs_on_one_line_with_no_fold_arrow()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("inline-fixed-twisty-nightly")).ToHaveCountAsync(0);
        await Expect(Component("inline-fixed-label-compile")).ToHaveCountAsync(0);
        await Expect(Component("inline-fixed-bar-compile")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task An_inline_job_is_selected_on_its_own()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Component("inline-bar-package").ClickAsync();

        await Expect(Component("inline-bar-package")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("veto-gantt__bar--selected"));
        await Expect(Component("inline-bar-compile")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("veto-gantt__bar--selected"));
    }

    [Fact]
    public async Task The_today_view_is_drawn_in_hours()
    {
        await GotoComponentAsync(Route);

        var header = Component("hours-scroller").Locator(".veto-gantt__head-cell");

        await Expect(header.Filter(new() { HasText = "14:00" })).ToHaveCountAsync(1);

        // The day is named once, on the date line - not repeated above the hours.
        await Expect(Component("hours-dateline")).ToHaveTextAsync("Wednesday 18 March 2026");
        await Expect(header.Filter(new() { HasText = "Wed 18 March" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_single_day_header_keeps_the_names_level_with_their_bars()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // One header row instead of two: the sidebar heading has to shrink with it.
        var label = (await Component("hours-label-support").BoundingBoxAsync())!;
        var line = (await Component("hours-row-support").BoundingBoxAsync())!;

        Assert.True(Math.Abs(label.Y - line.Y) <= 1, $"The name ({label.Y}px) should be level with its row ({line.Y}px).");
    }

    [Fact]
    public async Task Timed_jobs_are_placed_to_the_hour()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);
        await FittedWidthAsync("hours-scroller", "hours-scroller");

        // Deploy runs 14:00-15:30, Reports 09:00-10:00: half as long again, and later.
        var reports = await WidthAsync("hours-bar-reports");
        var deploy = await WidthAsync("hours-bar-deploy");

        Assert.InRange(deploy / reports, 1.4, 1.6);
        Assert.True(await LeftAsync("hours-bar-deploy") > await LeftAsync("hours-bar-reports"));

        // The tooltip names the times, not just the day.
        await Expect(Component("hours-bar-deploy")).ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex("14:00–15:30"));
    }

    [Fact]
    public async Task Today_leaves_out_last_night_and_tomorrow()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("hours-label-support")).ToBeVisibleAsync();

        // Backup ran 22:00-23:30 yesterday and the release is 09:00 tomorrow. Close, but not today.
        await Expect(Component("hours-label-backup")).ToHaveCountAsync(0);
        await Expect(Component("hours-label-release")).ToHaveCountAsync(0);
        await Expect(Component("hours-bar-backup")).ToHaveCountAsync(0);
        await Expect(Component("hours-bar-release")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_whole_day_of_jobs_shares_one_line()
    {
        await GotoComponentAsync(Route);

        // Inline: every Operations job is a bar, none of them a row.
        await Expect(Component("hours-label-deploy")).ToHaveCountAsync(0);

        foreach (var job in new[] { "etl", "reports", "deploy", "cleanup" })
        {
            await Expect(Component($"hours-bar-{job}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task The_now_marker_sits_at_the_time_of_day()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);
        await FittedWidthAsync("hours-scroller", "hours-scroller");

        // Now is pinned to 11:30: after Reports ends at 10:00, before Deploy starts at 14:00.
        var marker = await LeftAsync("hours-today-marker");
        var reports = await Component("hours-bar-reports").BoundingBoxAsync();
        var deploy = await LeftAsync("hours-bar-deploy");

        Assert.True(marker > reports!.X + reports.Width && marker < deploy,
            $"Now ({marker}px) should sit between 10:00 ({reports.X + reports.Width}px) and 14:00 ({deploy}px).");
    }

    [Fact]
    public async Task A_time_window_trims_the_day_and_fills_the_width()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        var header = Component("window-scroller").Locator(".veto-gantt__head-cell");

        // 14:00 to 20:00: six hours, and nothing either side of them.
        await Expect(header.Filter(new() { HasText = "14:00" })).ToHaveCountAsync(1);
        await Expect(header.Filter(new() { HasText = "19:00" })).ToHaveCountAsync(1);
        await Expect(header.Filter(new() { HasText = "13:00" })).ToHaveCountAsync(0);
        await Expect(header.Filter(new() { HasText = "20:00" })).ToHaveCountAsync(0);

        // The morning's data load is outside the window, so it is not listed; Deploy is.
        await Expect(Component("window-label-etl")).ToHaveCountAsync(0);
        await Expect(Component("window-bar-deploy")).ToBeVisibleAsync();

        var canvas = Component("window-scroller").Locator(".veto-gantt__canvas");
        var available = await Component("window-scroller").EvaluateAsync<double>("el => el.clientWidth");

        for (var attempt = 0; attempt < 50
             && Math.Abs(await canvas.EvaluateAsync<double>("el => el.getBoundingClientRect().width") - available) > 2; attempt++)
        {
            await Page.WaitForTimeoutAsync(100);
        }

        var width = await canvas.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(Math.Abs(width - available) <= 2, $"The six hours ({width}px) should fill the chart ({available}px).");
    }

    [Fact]
    public async Task An_app_filter_narrows_the_rows_and_reports_it()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("filters-label-design")).ToBeVisibleAsync();

        await Component("filters-filter-underway").ClickAsync();

        await Expect(Component("filters-filter-underway")).ToHaveAttributeAsync("aria-pressed", "true");

        // Design is finished and launch not started, so neither is under way; the app shell is.
        await Expect(Component("filters-label-design")).ToHaveCountAsync(0);
        await Expect(Component("filters-label-launch")).ToHaveCountAsync(0);
        await Expect(Component("filters-label-shell")).ToBeVisibleAsync();

        // OnFilterChanged hands the page the whole picture: platform, shell, components, mobile, spike.
        await Expect(Page.GetByTestId("filters-state")).ToContainTextAsync("Filters: underway · Hidden: none · 5 rows");
    }

    [Fact]
    public async Task A_category_click_reaches_the_pages_binding()
    {
        await GotoComponentAsync(Route);

        await Component("filters-category-design").ClickAsync();

        await Expect(Page.GetByTestId("filters-state")).ToContainTextAsync("Hidden: Design");
        await Expect(Component("filters-label-design")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_page_can_set_the_filters_itself()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("filters-hide-marketing").ClickAsync();

        await Expect(Component("filters-category-marketing")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Component("filters-label-launch")).ToHaveCountAsync(0);

        await Page.GetByTestId("filters-underway").ClickAsync();

        await Expect(Component("filters-filter-underway")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Component("filters-label-design")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ClearFiltersAsync_puts_every_row_back()
    {
        await GotoComponentAsync(Route);

        await Component("filters-filter-underway").ClickAsync();
        await Component("filters-category-marketing").ClickAsync();
        await Expect(Component("filters-label-design")).ToHaveCountAsync(0);

        await Page.GetByTestId("filters-clear").ClickAsync();

        await Expect(Component("filters-label-design")).ToBeVisibleAsync();
        await Expect(Component("filters-label-launch")).ToBeVisibleAsync();
        await Expect(Component("filters-filter-underway")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.GetByTestId("filters-state")).ToContainTextAsync("Filters: none · Hidden: none");
    }

    [Fact]
    public async Task One_scale_means_no_scale_picker()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("limited-toolbar")).ToBeVisibleAsync();
        await Expect(Component("limited-scale-day")).ToHaveCountAsync(0);
        await Expect(Component("limited-scale-week")).ToHaveCountAsync(0);
        await Expect(Component("limited-scale-month")).ToHaveCountAsync(0);

        // The full chart still offers every level, hours included.
        await Expect(Component("chart-scale-hour")).ToBeVisibleAsync();
        await Expect(Component("chart-scale-month")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_left_out_export_format_is_not_offered()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        await Component("limited-export").HoverAsync();

        await Expect(Component("limited-export-csv")).ToBeVisibleAsync();
        await Expect(Component("limited-export-json")).ToHaveCountAsync(0);

        // And a chart with export switched off has no menu at all.
        await Expect(Component("filters-export")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_narrow_chart_scrolls_its_hours_rather_than_squeezing_them()
    {
        await Page.SetViewportSizeAsync(1100, 900);
        await GotoComponentAsync(Route);

        var scroller = Component("today-view-scroller");
        var hour = Component("today-view-scroller").Locator(".veto-gantt__head-row:not(.veto-gantt__head-row--groups) .veto-gantt__head-cell").First;

        // Every hour keeps room for its label...
        await Expect(hour).ToHaveTextAsync("00:00");
        var width = await hour.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(width >= 43.5, $"An hour ({width}px) should not be squeezed below 44px.");

        var fits = await hour.EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth");
        Assert.True(fits, "The hour label should fit its cell without being cut off.");

        // ...and the day scrolls instead.
        Assert.True(await scroller.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1"),
            "A day that does not fit should scroll.");
    }

    [Fact]
    public async Task The_today_view_lists_times_without_the_day()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Support runs 08:00-17:00 today; on the today view the day goes without saying.
        await Expect(Component("hours-label-support").Locator(".veto-gantt__dates")).ToHaveTextAsync("08:00–17:00");
    }

    [Fact]
    public async Task GroupBy_puts_each_servers_runs_on_its_own_line()
    {
        await GotoComponentAsync(Route);

        foreach (var server in new[] { "web-01", "batch-01", "db-01" })
        {
            await Expect(Component($"runs-label-group-{server}")).ToBeVisibleAsync();
        }

        // Inline: no row per job, but a bar for each.
        await Expect(Component("runs-label-deploy")).ToHaveCountAsync(0);
        await Expect(Component("runs-bar-deploy")).ToBeVisibleAsync();
        await Expect(Component("runs-bar-import")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Overlapping_runs_are_stacked_into_lanes()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Import 08:00-09:30, reindex 08:45-10:00 and the ping at 09:10 all overlap: three lanes.
        var row = Component("runs-row-group-batch-01");
        await Expect(row).ToHaveAttributeAsync("data-lanes", "3");

        var tops = new List<double>();
        foreach (var job in new[] { "import", "reindex", "ping" })
        {
            tops.Add((await Component($"runs-bar-{job}").BoundingBoxAsync())!.Y);
        }

        Assert.Equal(3, tops.Select(y => Math.Round(y)).Distinct().Count());

        // The sidebar row grows with it, so names stay level with their bars.
        var timeline = await row.BoundingBoxAsync();
        var label = await Component("runs-label-group-batch-01").BoundingBoxAsync();
        Assert.True(Math.Abs(timeline!.Height - label!.Height) <= 1,
            $"The label row ({label.Height}px) should be as tall as the stacked row ({timeline.Height}px).");
        Assert.True(timeline.Height >= 3 * 35, $"Three lanes should make a taller row ({timeline.Height}px).");
    }

    [Fact]
    public async Task Runs_that_only_touch_share_a_lane()
    {
        await GotoComponentAsync(Route);

        // Deploy ends at 08:30 as the smoke test starts: back to back, not overlapping.
        await Expect(Component("runs-row-group-web-01")).Not.ToHaveAttributeAsync("data-lanes", new System.Text.RegularExpressions.Regex(".*"));
    }

    [Fact]
    public async Task A_five_minute_run_is_drawn_to_scale()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);
        await FittedWidthAsync("runs-scroller", "runs-scroller");

        // The ping runs 5 minutes, the import 90: eighteen times as long.
        var ping = await WidthAsync("runs-bar-ping");
        var import = await WidthAsync("runs-bar-import");

        Assert.InRange(import / ping, 14, 22);
    }

    [Fact]
    public async Task Yesterdays_run_is_not_on_todays_schedule()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("runs-bar-backup")).ToBeVisibleAsync();
        await Expect(Component("runs-bar-yesterday")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_quarter_hour_scale_labels_every_fifteen_minutes()
    {
        await GotoComponentAsync(Route);

        var cells = Component("runs-quarter-scroller").Locator(".veto-gantt__head-cell");

        // 08:00 to 10:00: two hours overhead, eight quarters beneath them.
        await Expect(cells.Filter(new() { HasText = ":45" })).ToHaveCountAsync(2);
        await Expect(cells.Filter(new() { HasText = "09:00" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task The_timeline_scrolls_itself_to_the_now_marker()
    {
        // Narrow enough that a day of readable hours does not fit, so the timeline scrolls.
        await Page.SetViewportSizeAsync(1100, 900);
        await GotoComponentAsync(Route);

        var scroller = Component("hours-scroller");
        var marker = Component("hours-today-marker");

        // Now is 11:30, most of the way across a day that is wider than the chart.
        for (var attempt = 0; attempt < 50 && await scroller.EvaluateAsync<double>("el => el.scrollLeft") == 0; attempt++)
        {
            await Page.WaitForTimeoutAsync(100);
        }

        var view = (await scroller.BoundingBoxAsync())!;
        var needle = (await marker.BoundingBoxAsync())!;

        Assert.True(needle.X >= view.X && needle.X <= view.X + view.Width,
            $"The now marker ({needle.X}px) should be in view ({view.X}-{view.X + view.Width}px) without anyone scrolling.");
    }

    [Fact]
    public async Task A_row_can_be_taller_than_the_rest()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // RowHeight 28 for the chart, 48 for each project.
        var project = (await Component("heights-label-platform").BoundingBoxAsync())!;
        var task = (await Component("heights-label-design").BoundingBoxAsync())!;

        Assert.InRange(project.Height, 47, 49);
        Assert.InRange(task.Height, 27, 29);

        // The timeline row matches, and rows further down stay level with their names.
        var line = (await Component("heights-row-platform").BoundingBoxAsync())!;
        Assert.InRange(line.Height, 47, 49);

        var launchLabel = (await Component("heights-label-launch").BoundingBoxAsync())!;
        var launchLine = (await Component("heights-row-launch").BoundingBoxAsync())!;
        Assert.True(Math.Abs(launchLabel.Y - launchLine.Y) <= 1,
            $"Launch's name ({launchLabel.Y}px) should be level with its row ({launchLine.Y}px).");
    }

    [Fact]
    public async Task The_built_in_filters_can_be_switched_off()
    {
        await GotoComponentAsync(Route);

        // The limited demo turns off search and the category chips; the full chart has both.
        await Expect(Component("limited-toolbar")).ToBeVisibleAsync();
        await Expect(Component("limited-search")).ToHaveCountAsync(0);
        await Expect(Component("limited-category-design")).ToHaveCountAsync(0);

        await Expect(Component("chart-search")).ToBeVisibleAsync();
        await Expect(Component("chart-category-design")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Todays_date_has_a_line_of_its_own()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("hours-dateline")).ToHaveTextAsync("Wednesday 18 March 2026");

        // Above the timeline, not inside it.
        var line = (await Component("hours-dateline").BoundingBoxAsync())!;
        var timeline = (await Component("hours-scroller").BoundingBoxAsync())!;
        Assert.True(line.Y + line.Height <= timeline.Y + 1, "The date line should sit above the timeline.");
    }

    [Fact]
    public async Task Todays_date_stays_in_view_however_the_timeline_is_scrolled()
    {
        await Page.SetViewportSizeAsync(1100, 900);
        await GotoComponentAsync(Route);

        var scroller = Component("hours-scroller");
        await scroller.ScrollIntoViewIfNeededAsync();
        var dateline = (await Component("hours-dateline").BoundingBoxAsync())!;

        foreach (var left in new[] { 0, 400, 100000 })
        {
            await scroller.EvaluateAsync($"el => el.scrollLeft = {left}");

            // The date line is outside the part that scrolls: it does not move at all.
            var now = (await Component("hours-dateline").BoundingBoxAsync())!;
            Assert.Equal(dateline.X, now.X, 1);
            await Expect(Component("hours-dateline")).ToBeInViewportAsync();
        }
    }

    [Fact]
    public async Task A_month_label_sticks_to_the_edge_as_the_timeline_scrolls()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // The schedule at day scale: March spans far wider than the timeline shows.
        var scroller = Component("chart-scroller");
        var march = scroller.Locator(".veto-gantt__head-label", new() { HasText = "March 2026" });

        foreach (var left in new[] { 0, 300, 600 })
        {
            await scroller.EvaluateAsync($"el => el.scrollLeft = {left}");

            var label = (await march.BoundingBoxAsync())!;
            var view = (await scroller.BoundingBoxAsync())!;
            Assert.True(label.X >= view.X - 1 && label.X < view.X + view.Width,
                $"Scrolled to {left}px, the March label ({label.X}px) should still be in view ({view.X}-{view.X + view.Width}px).");
        }
    }

    [Fact]
    public async Task The_date_line_header_and_toolbar_texts_are_the_apps_to_word()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("wording-dateline")).ToHaveTextAsync("Wed 18 Mar");
        await Expect(Component("wording-view-today")).ToHaveTextAsync("Now");
        await Expect(Component("wording-view-schedule")).ToHaveTextAsync("Plan");
        await Expect(Component("wording-today")).ToContainTextAsync("Back to now");
        await Expect(Component("wording-export")).ToContainTextAsync("Download");
        await Expect(Component("wording-search").Locator("input")).ToHaveAttributeAsync("placeholder", "Search jobs");

        var header = Component("wording-scroller").Locator(".veto-gantt__head-cell");
        await Expect(header.Filter(new() { HasText = "Wednesday" })).ToHaveCountAsync(1);
        Assert.True(await header.Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex("^9:00$") }).CountAsync() > 0,
            "The hours should read in the app's own format, 9:00 rather than 09:00.");
    }

    [Fact]
    public async Task The_date_line_can_be_switched_off()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("limited-toolbar")).ToBeVisibleAsync();
        await Expect(Component("limited-dateline")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_now_marker_carries_the_current_time()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        // Now is pinned to 11:30 in this demo.
        var label = Component("hours-now-label");
        await Expect(label).ToHaveTextAsync("11:30");

        // Centred on the marker below it.
        var tab = (await label.BoundingBoxAsync())!;
        var needle = (await Component("hours-today-marker").BoundingBoxAsync())!;
        Assert.True(Math.Abs(tab.X + tab.Width / 2 - (needle.X + needle.Width / 2)) <= 2,
            $"The time ({tab.X + tab.Width / 2}px) should sit on the marker ({needle.X + needle.Width / 2}px).");

        // At day scale the marker stands for today, so the tab gives the date.
        await Expect(Component("chart-now-label")).ToHaveTextAsync("18 Mar");
    }

    [Fact]
    public async Task The_time_on_the_marker_never_covers_a_header_label()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync(Route);

        foreach (var chart in new[] { "hours", "chart", "runs-quarter" })
        {
            var tab = (await Component($"{chart}-now-label").BoundingBoxAsync())!;
            var cells = Component($"{chart}-scroller").Locator(".veto-gantt__head-cell");

            for (var i = 0; i < await cells.CountAsync(); i++)
            {
                var cell = await cells.Nth(i).BoundingBoxAsync();
                if (cell is null) continue;

                Assert.True(tab.Y >= cell.Y + cell.Height - 1,
                    $"In {chart}, the time tab ({tab.Y}px) should sit below the header labels, not over one ending at {cell.Y + cell.Height}px.");
            }

            // And the band it sits in does not push the rows out of line with their names.
            var firstLabel = Component($"{chart}-sidebar").Locator(".veto-gantt__row").First;
            var firstRow = Component($"{chart}-scroller").Locator(".veto-gantt__grid > .veto-gantt__row").First;
            var y1 = (await firstLabel.BoundingBoxAsync())!.Y;
            var y2 = (await firstRow.BoundingBoxAsync())!.Y;
            Assert.True(Math.Abs(y1 - y2) <= 1, $"In {chart}, the first name ({y1}px) should be level with its row ({y2}px).");
        }
    }
}
