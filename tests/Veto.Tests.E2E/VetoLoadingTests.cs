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
}
