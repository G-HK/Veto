using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for the loading components, driven against /docs/components/loading.
/// </summary>
public class VetoLoadingTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/loading";

    // ------------------------------------------------------------------ Spinner

    [Fact]
    public async Task A_plain_spinner_is_hidden_from_assistive_technology ()
    {
        await GotoComponentAsync(Route);

        // Nothing to announce: the surrounding text already says what is happening.
        await Expect(Component("spinner-md")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Component("spinner-md")).Not.ToHaveAttributeAsync("role", "status");
    }

    [Fact]
    public async Task A_labelled_spinner_announces_itself ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("spinner-labelled")).ToHaveAttributeAsync("role", "status");
        await Expect(Component("spinner-labelled")).ToHaveAttributeAsync("aria-label", "Checking availability");
        await Expect(Component("spinner-labelled")).Not.ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task Spinner_sizes_differ ()
    {
        await GotoComponentAsync(Route);

        var small = await Component("spinner-sm").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var large = await Component("spinner-lg").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(large > small, $"Large ({large}px) should be bigger than small ({small}px).");
    }

    // ------------------------------------------------------------------ Progress

    [Fact]
    public async Task Progress_reports_its_value_to_assistive_technology ()
    {
        await GotoComponentAsync(Route);

        var bar = Component("progress").Locator("[role='progressbar']");

        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "40");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemax", "100");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Uploading");
    }

    [Fact]
    public async Task Progress_fills_as_the_value_changes ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var fill = Component("progress").Locator(".veto-progress__fill");
        var before = await fill.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        await Component("progress-more").ClickAsync();
        await Expect(Page.GetByTestId("progress-value")).ToHaveTextAsync("60%");

        // Waited for rather than sampled. The value changes the moment the button is clicked,
        // but the width is a CSS transition and has barely started by then - reading it once
        // measures the animation's first frame, which under load is indistinguishable from no
        // change at all.
        var grew = await fill.EvaluateAsync<bool>(
            @"(el, before) => new Promise(resolve => {
                  const deadline = performance.now() + 2000;
                  const check = () => {
                      if (el.getBoundingClientRect().width > before) { resolve(true); return; }
                      if (performance.now() > deadline) { resolve(false); return; }
                      requestAnimationFrame(check);
                  };
                  check();
              })",
            before);

        Assert.True(grew, $"The bar should grow from {before}px within 2s.");
    }

    [Fact]
    public async Task An_indeterminate_bar_reports_no_value ()
    {
        await GotoComponentAsync(Route);

        var bar = Component("progress-indeterminate").Locator("[role='progressbar']");

        // There is no number to give, so it says what it is doing instead of claiming a figure.
        await Expect(bar).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));
        await Expect(bar).ToHaveAttributeAsync("aria-valuetext", "Working");
    }

    // ------------------------------------------------------------------ Skeleton

    [Fact]
    public async Task Skeletons_are_hidden_from_assistive_technology ()
    {
        await GotoComponentAsync(Route);

        // The wait is announced once on the region that is loading, not once per grey box.
        await Expect(Component("skeleton-text")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Component("skeleton-circle")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task A_multi_line_skeleton_draws_a_line_each_and_ends_short ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var lines = Component("skeleton-text").Locator(".veto-skeleton__line");
        await Expect(lines).ToHaveCountAsync(3);

        var first = await lines.Nth(0).EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var last = await lines.Nth(2).EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        // The last line is short, the way a real paragraph ends.
        Assert.True(last < first, $"The last line ({last}px) should be shorter than the first ({first}px).");
    }

    // ------------------------------------------------------------------ Overlay

    [Fact]
    public async Task The_overlay_veils_content_without_removing_it ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("overlay")).ToHaveAttributeAsync("aria-busy", "false");

        await Component("overlay-toggle").ClickAsync();

        await Expect(Component("overlay")).ToHaveAttributeAsync("aria-busy", "true");

        // The old content is still there underneath - that is the whole point of an overlay.
        await Expect(Component("overlay-inner")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Veiled_content_cannot_be_reached ()
    {
        await GotoComponentAsync(Route);

        await Component("overlay-toggle").ClickAsync();
        await Expect(Component("overlay")).ToHaveAttributeAsync("aria-busy", "true");

        // Marked inert: a control about to be replaced should not be clickable behind the veil.
        var inert = await Component("overlay").Locator(".veto-loading-overlay__content")
            .EvaluateAsync<bool>("el => el.hasAttribute('inert')");

        Assert.True(inert, "Content behind the veil should be inert.");
    }

    [Fact]
    public async Task The_overlay_clears_itself_when_the_work_finishes ()
    {
        await GotoComponentAsync(Route);

        await Component("overlay-toggle").ClickAsync();
        await Expect(Component("overlay")).ToHaveAttributeAsync("aria-busy", "true");

        await Expect(Component("overlay")).ToHaveAttributeAsync(
            "aria-busy", "false", new() { Timeout = 10_000 });
    }

    // ------------------------------------------------------------------ In the Gantt

    [Fact]
    public async Task The_gantt_draws_a_placeholder_chart_before_any_data_arrives ()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync("docs/components/gantt");

        await Expect(Component("loader-skeleton")).ToHaveCountAsync(0);

        await Component("load-first").ClickAsync();

        var skeleton = Component("loader-skeleton");
        await Expect(skeleton).ToBeVisibleAsync();
        await Expect(skeleton).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(skeleton).ToHaveAttributeAsync("aria-label", "Loading tasks");

        // The real rows are gone while there is nothing to show.
        await Expect(Component("loader-bar-survey")).ToHaveCountAsync(0);

        // And they come back.
        await Expect(Component("loader-bar-survey")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task A_refreshing_gantt_keeps_its_rows_on_screen ()
    {
        await Page.SetViewportSizeAsync(1500, 1000);
        await GotoComponentAsync("docs/components/gantt");

        await Component("load-refresh").ClickAsync();

        await Expect(Component("loader-refresh")).ToHaveAttributeAsync("aria-busy", "true");

        // Unlike a first load, the rows stay put - the reader keeps what they were looking at.
        await Expect(Component("loader-bar-survey")).ToBeVisibleAsync();
        await Expect(Component("loader-skeleton")).ToHaveCountAsync(0);
    }
    [Fact]
    public async Task A_ring_reports_itself_as_a_progress_bar()
    {
        await GotoComponentAsync(Route);

        var ring = Component("ring");
        var bar = ring.Locator("[role='progressbar']");

        await Expect(ring).ToHaveAttributeAsync("data-shape", "circle");
        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "40");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Uploading");

        // The drawing itself is decoration; the role and the value carry the meaning.
        await Expect(ring.Locator("svg")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task The_ring_grows_by_offsetting_a_dash()
    {
        await GotoComponentAsync(Route);

        var fill = Component("ring").Locator(".veto-progress__ring-fill");

        var circumference = await fill.EvaluateAsync<double>(
            "el => parseFloat(el.getAttribute('stroke-dasharray'))");
        var offset = await fill.EvaluateAsync<double>(
            "el => parseFloat(el.getAttribute('stroke-dashoffset'))");

        // 40% shown means 60% of the circumference still hidden. Asserted as a ratio rather
        // than a pixel count, because the radius depends on the stroke width.
        Assert.True(circumference > 0, "the ring should have a circumference");
        Assert.Equal(0.6, offset / circumference, 2);
    }

    [Fact]
    public async Task A_thinner_ring_has_a_wider_radius()
    {
        await GotoComponentAsync(Route);

        // The stroke straddles its path, so half of it sits outside the radius - a thick ring
        // has to pull its radius in or the stroke is clipped at the edge of the drawing.
        var thick = await Component("ring").Locator(".veto-progress__ring-fill")
            .EvaluateAsync<double>("el => parseFloat(el.getAttribute('r'))");
        var thin = await Component("ring-thin").Locator(".veto-progress__ring-fill")
            .EvaluateAsync<double>("el => parseFloat(el.getAttribute('r'))");

        Assert.True(thin > thick, $"expected the thin ring's radius ({thin}) to exceed the thick one's ({thick})");
    }

    [Fact]
    public async Task An_indeterminate_ring_turns_and_reports_no_value()
    {
        await GotoComponentAsync(Route);

        var bar = Component("ring-indeterminate").Locator("[role='progressbar']");

        await Expect(bar).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));
        await Expect(bar).ToHaveAttributeAsync("aria-valuetext", "Working");

        var animation = await Component("ring-indeterminate").Locator("svg")
            .EvaluateAsync<string>("el => getComputedStyle(el).animationName");

        // Scoped CSS appends the scope id to keyframe names, so it is never the bare name.
        Assert.StartsWith("veto-progress-spin", animation);
    }

    [Fact]
    public async Task Segments_notch_the_bar_without_splitting_the_fill()
    {
        await GotoComponentAsync(Route);

        var hp = Component("hp");

        await Expect(hp.Locator(".veto-progress__notches")).ToHaveCountAsync(1);

        // One fill, notched over the top - so a part-filled pip still reads as part-filled.
        await Expect(hp.Locator(".veto-progress__fill")).ToHaveCountAsync(1);

        var segments = await hp.Locator(".veto-progress__notches")
            .EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--veto-progress-segments').trim()");

        Assert.Equal("10", segments);
    }

    [Fact]
    public async Task The_trail_is_left_behind_when_the_value_drops()
    {
        await GotoComponentAsync(Route);

        var fill = Component("hp").Locator(".veto-progress__fill");
        var trail = Component("hp").Locator(".veto-progress__trail");

        await Component("hp-hit").ClickAsync();

        // The gap between the two is the hit. Waited for rather than sampled: the fill is a
        // transition and has barely started the moment the button is clicked.
        var gapAppeared = await Page.EvaluateAsync<bool>(
            @"([fillEl, trailEl]) => new Promise(resolve => {
                  const deadline = performance.now() + 2000;
                  const check = () => {
                      const f = fillEl.getBoundingClientRect().width;
                      const t = trailEl.getBoundingClientRect().width;
                      if (t > f + 1) { resolve(true); return; }
                      if (performance.now() > deadline) { resolve(false); return; }
                      requestAnimationFrame(check);
                  };
                  check();
              })",
            new[] { await fill.ElementHandleAsync(), await trail.ElementHandleAsync() });

        Assert.True(gapAppeared, "the damage bar should be left behind the health bar after a hit");
    }

    [Fact]
    public async Task ValueContent_prints_the_number_in_its_own_units()
    {
        await GotoComponentAsync(Route);

        // 40 of 100, scaled to an 84 MB download.
        await Expect(Component("download-value")).ToContainTextAsync("33.6 MB of 84 MB");
    }

    [Fact]
    public async Task ValueFormat_and_suffix_shape_the_printed_reading()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("download-plain-value")).ToHaveTextAsync("40.0% done");
        await Expect(Component("hp-value")).ToContainTextAsync("HP");
    }

    [Fact]
    public async Task A_runner_rides_the_leading_edge_of_the_fill()
    {
        await GotoComponentAsync(Route);

        var runner = Component("runner-car").Locator(".veto-progress__runner");

        var before = await runner.EvaluateAsync<double>("el => parseFloat(el.style.left)");

        await Component("progress-more").ClickAsync();

        var after = await Page.EvaluateAsync<bool>(
            @"([el, before]) => new Promise(resolve => {
                  const deadline = performance.now() + 2000;
                  const check = () => {
                      if (parseFloat(el.style.left) > before) { resolve(true); return; }
                      if (performance.now() > deadline) { resolve(false); return; }
                      requestAnimationFrame(check);
                  };
                  check();
              })",
            new object[] { await runner.ElementHandleAsync(), before });

        Assert.True(after, $"the runner should move past {before}% as the bar fills");

        // Decoration: the bar already announces the value, and a car read out beside it is noise.
        await Expect(Component("runner-car").Locator(".veto-progress__runner-rail"))
            .ToHaveAttributeAsync("aria-hidden", "true");
    }

}
