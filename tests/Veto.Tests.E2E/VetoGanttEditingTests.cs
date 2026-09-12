using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for editing a VetoGantt: dragging, keyboard moves, ordering, and the
/// per-user storage behind them.
/// </summary>
public class VetoGanttEditingTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/gantt";

    private Task<double> BarLeftAsync ( string id )
        => Component($"editable-bar-{id}").EvaluateAsync<double>("el => parseFloat(el.style.left)");

    private Task<double> BarWidthAsync ( string id )
        => Component($"editable-bar-{id}").EvaluateAsync<double>("el => parseFloat(el.style.width)");

    /// <summary>
    /// Waits until the chart can actually be edited. Attaching the pointer handler and opening
    /// the store both happen after the first paint - PGlite in particular is fetched - so a
    /// spec that drags as soon as the page renders is racing the component rather than testing
    /// it. The chart says when it is ready; this waits for it to say so.
    /// </summary>
    private async Task ReadyToEditAsync ()
    {
        var chart = Page.Locator(".veto-gantt[data-testid='editable']");

        await Expect(chart).ToHaveAttributeAsync("data-draggable", "true", new() { Timeout = 15_000 });
        await Expect(chart).Not.ToHaveAttributeAsync("data-store", "none", new() { Timeout = 15_000 });
    }

    /// <summary>Starts from a clean slate: these specs share one browser-local store.</summary>
    private async Task ResetChartAsync ()
    {
        if (await Component("editable-reset").CountAsync() > 0)
        {
            await Component("editable-reset").ClickAsync();
            await Expect(Component("editable-reset")).ToHaveCountAsync(0);
        }
    }

    private async Task DragAsync ( string id, double days )
    {
        var bar = Component($"editable-bar-{id}");

        // The mouse is driven in viewport coordinates and does not scroll to reach anything, so
        // a bar below the fold would be dragged at a point that belongs to the page behind it.
        await bar.ScrollIntoViewIfNeededAsync();

        var box = await bar.BoundingBoxAsync() ?? throw new InvalidOperationException($"'{id}' has no box.");

        // Grabbed in the middle, away from the resize grips at either end.
        var fromX = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync((float) fromX, (float) y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float) (fromX + days * 40), (float) y, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task Dragging_a_bar_moves_it ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        var before = await BarLeftAsync("wiring");
        var width = await BarWidthAsync("wiring");

        await DragAsync("wiring", 3);

        await Expect(Page.GetByTestId("change-note")).ToContainTextAsync("Moved by 3 days");

        var after = await BarLeftAsync("wiring");

        Assert.True(after > before, $"The bar should have moved right: {before}px -> {after}px.");

        // Moving keeps the duration; only resizing changes it.
        Assert.Equal(width, await BarWidthAsync("wiring"), 1);
    }

    [Fact]
    public async Task A_drag_snaps_to_whole_days ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        var before = await BarLeftAsync("rollout");

        // Two and a half columns: it should land on three, not somewhere between.
        await DragAsync("rollout", 2.5);

        var after = await BarLeftAsync("rollout");
        var days = (after - before) / 40;

        Assert.Equal(Math.Round(days), days, 1);
    }

    [Fact]
    public async Task Arrow_keys_move_a_bar_without_a_mouse ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        var before = await BarLeftAsync("audit");

        await Component("editable-bar-audit").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Expect(Page.GetByTestId("change-note")).ToContainTextAsync("Moved by 1 days");

        Assert.True(await BarLeftAsync("audit") > before, "Arrow right should move the bar later.");
    }

    [Fact]
    public async Task Shift_and_an_arrow_key_resizes ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        var left = await BarLeftAsync("audit");
        var width = await BarWidthAsync("audit");

        await Component("editable-bar-audit").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowRight");

        await Expect(Page.GetByTestId("change-note")).ToContainTextAsync("EndChanged");

        // The end moved, so the start did not.
        Assert.Equal(left, await BarLeftAsync("audit"), 1);
        Assert.True(await BarWidthAsync("audit") > width, "Shift+ArrowRight should lengthen the bar.");
    }

    [Fact]
    public async Task A_bar_cannot_be_dragged_inside_out ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        await Component("editable-bar-design").FocusAsync();

        // Blueprint is five days; ten presses of Shift+ArrowLeft would take the end past the
        // start if nothing stopped it.
        for (var press = 0; press < 10; press++)
        {
            await Page.Keyboard.PressAsync("Shift+ArrowLeft");
        }

        var width = await BarWidthAsync("design");

        Assert.True(width > 0, $"A bar should never collapse past zero, but measured {width}px.");
    }

    [Fact]
    public async Task Edits_survive_a_reload ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        var before = await BarLeftAsync("wiring");

        await Component("editable-bar-wiring").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Page.GetByTestId("change-note")).ToContainTextAsync("Moved");

        var moved = await BarLeftAsync("wiring");

        await Page.ReloadAsync();
        await Page.Locator("[data-testid='page'][data-ready='true']")
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
        await ReadyToEditAsync();

        // Whichever backend the store settled on, the edit is this reader's and it comes back.
        await Expect(Component("editable-bar-wiring")).ToBeVisibleAsync();

        var restored = await BarLeftAsync("wiring");

        Assert.NotEqual(before, restored);
        Assert.Equal(moved, restored, 1);
    }

    [Fact]
    public async Task Reset_clears_what_was_stored ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        var original = await BarLeftAsync("wiring");

        await Component("editable-bar-wiring").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        Assert.NotEqual(original, await BarLeftAsync("wiring"));

        // The control only appears once there is something to clear.
        await Expect(Component("editable-reset")).ToBeVisibleAsync();
        await Component("editable-reset").ClickAsync();

        Assert.Equal(original, await BarLeftAsync("wiring"), 1);
        await Expect(Component("editable-reset")).ToHaveCountAsync(0);

        // And it is gone for good, not just from the screen.
        await Page.ReloadAsync();
        await Page.Locator("[data-testid='page'][data-ready='true']")
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
        await ReadyToEditAsync();

        Assert.Equal(original, await BarLeftAsync("wiring"), 1);
    }

    [Fact]
    public async Task A_summary_row_cannot_be_dragged ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);

        // Its dates are the span of its children, so moving it would contradict them.
        await Expect(Component("chart-bar-platform")).Not.ToHaveAttributeAsync("data-editable", "true");
        await Expect(Component("editable-bar-wiring")).ToHaveAttributeAsync("data-editable", "true");
    }

    [Fact]
    public async Task Ordering_can_be_switched_to_alphabetical ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);

        async Task<IReadOnlyList<string>> NamesAsync ()
            => await Component("editable-sidebar").Locator(".veto-gantt__name").AllTextContentsAsync();

        var supplied = await NamesAsync();
        Assert.Equal(["Wiring", "Audit", "Rollout", "Blueprint"], supplied);

        await Component("sort-name").ClickAsync();

        var alphabetical = await NamesAsync();
        Assert.Equal(["Audit", "Blueprint", "Rollout", "Wiring"], alphabetical);
    }

    [Fact]
    public async Task Ordering_by_start_date_is_also_available ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);
        await ReadyToEditAsync();
        await ResetChartAsync();

        await Component("sort-start").ClickAsync();

        var names = await Component("editable-sidebar").Locator(".veto-gantt__name").AllTextContentsAsync();

        Assert.Equal(["Blueprint", "Audit", "Wiring", "Rollout"], names);
    }

    [Fact]
    public async Task A_read_only_chart_stays_read_only ()
    {
        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync(Route);

        // The first chart on the page is not editable, and nothing about the editing work
        // should have made it so.
        await Expect(Component("chart-bar-design")).Not.ToHaveAttributeAsync("data-editable", "true");
        await Expect(Component("chart-reset")).ToHaveCountAsync(0);
    }
}
