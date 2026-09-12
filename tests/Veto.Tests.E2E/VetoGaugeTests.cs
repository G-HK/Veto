using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoGauge</c>, driven against /docs/components/gauge.</summary>
public class VetoGaugeTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/gauge";

    private ILocator Dial ( string testId ) => Component(testId).Locator("svg.veto-gauge__dial");

    [Fact]
    public async Task Reports_itself_as_a_meter_with_its_whole_range ()
    {
        await GotoComponentAsync(Route);

        var dial = Dial("basic");

        await Expect(dial).ToHaveAttributeAsync("role", "meter");
        await Expect(dial).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(dial).ToHaveAttributeAsync("aria-valuemax", "100");
        await Expect(dial).ToHaveAttributeAsync("aria-valuenow", "42");
        await Expect(dial).ToHaveAttributeAsync("aria-label", "CPU");
    }

    [Fact]
    public async Task The_reading_is_printed_with_its_unit ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("42%");
    }

    [Fact]
    public async Task Changing_the_value_moves_the_dial ()
    {
        await GotoComponentAsync(Route);

        await Component("basic-input").Locator("input.veto-slider__input--lower").FillAsync("77");

        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "77");
        await Expect(Dial("basic")).ToHaveAttributeAsync("aria-valuenow", "77");
        await Expect(Page.GetByTestId("basic-value")).ToHaveTextAsync("77%");
    }

    [Fact]
    public async Task The_arc_lengthens_as_the_reading_rises ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var arc = Component("basic").Locator(".veto-gauge__value");

        // The arc is revealed with a dash offset, so a rising value shortens the offset.
        var before = await arc.EvaluateAsync<double>("el => parseFloat(el.getAttribute('stroke-dashoffset'))");

        await Component("basic-input").Locator("input.veto-slider__input--lower").FillAsync("90");
        await Expect(Component("basic")).ToHaveAttributeAsync("data-value", "90");

        var after = await arc.EvaluateAsync<double>("el => parseFloat(el.getAttribute('stroke-dashoffset'))");

        Assert.True(after < before, $"A higher reading should reveal more of the arc: {before} -> {after}.");
    }

    [Fact]
    public async Task Bands_are_drawn_on_the_dial ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("banded").Locator(".veto-gauge__band")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task The_reading_takes_the_colour_of_the_band_it_falls_in ()
    {
        await GotoComponentAsync(Route);

        var input = Component("banded-input").Locator("input.veto-slider__input--lower");

        await input.FillAsync("20");
        await Expect(Component("banded")).ToHaveAttributeAsync("data-band", "Healthy");

        await input.FillAsync("80");
        await Expect(Component("banded")).ToHaveAttributeAsync("data-band", "Filling up");

        await input.FillAsync("97");
        await Expect(Component("banded")).ToHaveAttributeAsync("data-band", "Nearly full");
    }

    [Fact]
    public async Task A_banded_gauge_announces_which_band_it_is_in ()
    {
        await GotoComponentAsync(Route);

        await Component("banded-input").Locator("input.veto-slider__input--lower").FillAsync("95");

        await Expect(Dial("banded")).ToHaveAttributeAsync("aria-valuetext", "95%, Nearly full");
    }

    [Fact]
    public async Task A_needle_gauge_draws_a_pointer_rather_than_an_arc ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("speedo")).ToHaveClassAsync(new Regex(@"\bveto-gauge--needle\b"));
        await Expect(Component("speedo").Locator(".veto-gauge__needle")).ToHaveCountAsync(1);
        await Expect(Component("speedo").Locator(".veto-gauge__value")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_needle_swings_round_as_the_value_rises ()
    {
        await GotoComponentAsync(Route);

        var needle = Component("speedo").Locator(".veto-gauge__needle");
        var input = Component("speed-input").Locator("input.veto-slider__input--lower");

        await input.FillAsync("20");
        var low = await needle.EvaluateAsync<string>("el => el.style.getPropertyValue('--veto-gauge-angle')");

        await input.FillAsync("200");
        var high = await needle.EvaluateAsync<string>("el => el.style.getPropertyValue('--veto-gauge-angle')");

        var lowAngle = double.Parse(low.Replace("deg", string.Empty), System.Globalization.CultureInfo.InvariantCulture);
        var highAngle = double.Parse(high.Replace("deg", string.Empty), System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(highAngle > lowAngle, $"The needle should swing clockwise: {lowAngle} -> {highAngle}.");
    }

    [Fact]
    public async Task Ticks_and_their_numbers_are_drawn_around_the_dial ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("speedo").Locator(".veto-gauge__tick").First).ToBeVisibleAsync();
        await Expect(Component("speedo").Locator(".veto-gauge__tick-label").First).ToBeVisibleAsync();

        // Every fourth tick is major, and only the major ones are numbered.
        var ticks = await Component("speedo").Locator(".veto-gauge__tick").CountAsync();
        var labels = await Component("speedo").Locator(".veto-gauge__tick-label").CountAsync();

        Assert.True(ticks > labels, $"Not every tick should be numbered: {ticks} ticks, {labels} labels.");
    }

    [Fact]
    public async Task A_tachometer_turns_red_on_its_own_at_the_red_line ()
    {
        await GotoComponentAsync(Route);

        var input = Component("rpm-input").Locator("input.veto-slider__input--lower");

        await input.FillAsync("3000");
        await Expect(Component("tacho")).ToHaveAttributeAsync("data-value", "3000");

        // Outside every band the attribute is absent rather than empty - Blazor drops a null.
        Assert.False(await Component("tacho").EvaluateAsync<bool>("el => el.hasAttribute('data-band')"));

        await input.FillAsync("7500");
        await Expect(Component("tacho")).ToHaveAttributeAsync("data-band", "Red line");
    }

    [Fact]
    public async Task Tick_numbers_stay_inside_the_drawing ()
    {
        await GotoComponentAsync(Route);

        // The bug this pins: the numbers were laid out beyond the arc, at radius 106 in a
        // viewBox whose radius is 100, so they were clipped and collided with the dial.
        var outside = await Component("speedo").EvaluateAsync<int>(
            "el => Array.from(el.querySelectorAll('.veto-gauge__tick-label'))" +
            "  .filter(t => { const x = +t.getAttribute('x'), y = +t.getAttribute('y');" +
            "                 return Math.hypot(x - 100, y - 100) > 100; }).length");

        Assert.Equal(0, outside);
    }

    [Fact]
    public async Task Tick_numbers_sit_inside_the_arc_not_beyond_it ()
    {
        await GotoComponentAsync(Route);

        var labelRadius = await Component("speedo").EvaluateAsync<double>(
            "el => { const t = el.querySelector('.veto-gauge__tick-label');" +
            "        return Math.hypot(+t.getAttribute('x') - 100, +t.getAttribute('y') - 100); }");
        var tickRadius = await Component("speedo").EvaluateAsync<double>(
            "el => { const t = el.querySelector('.veto-gauge__tick');" +
            "        return Math.hypot(+t.getAttribute('x1') - 100, +t.getAttribute('y1') - 100); }");

        Assert.True(labelRadius < tickRadius,
            $"The numbers should sit inside the ticks: {labelRadius} vs {tickRadius}.");
    }

    [Fact]
    public async Task The_reading_never_runs_wider_than_the_dial ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        // "140 km/h" at the large size used to push the reading out over the ticks.
        var dial = await Component("tuned").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var reading = await Page.GetByTestId("tuned-value")
            .EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(reading < dial, $"The reading should fit the dial: {reading} vs {dial}.");
    }

    [Fact]
    public async Task The_reading_size_can_be_set ()
    {
        await GotoComponentAsync(Route);

        var tuned = await Page.GetByTestId("tuned-value")
            .EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");
        var untouched = await Page.GetByTestId("size-lg-value")
            .EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");

        Assert.True(tuned < untouched, $"ValueFontSize should have shrunk it: {tuned} vs {untouched}.");
    }

    [Fact]
    public async Task The_needle_and_its_hub_can_be_changed ()
    {
        await GotoComponentAsync(Route);

        var needle = Component("tuned").Locator(".veto-gauge__needle line");
        var hub = Component("tuned").Locator(".veto-gauge__needle circle").First;

        await Expect(needle).ToHaveAttributeAsync("stroke-width", "5");
        await Expect(needle).ToHaveAttributeAsync("stroke", "var(--veto-color-danger)");
        await Expect(hub).ToHaveAttributeAsync("r", "10");
        await Expect(hub).ToHaveAttributeAsync("fill", "var(--veto-color-text)");
    }

    [Fact]
    public async Task A_hub_can_be_left_off_entirely ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("hubless").Locator(".veto-gauge__needle circle")).ToHaveCountAsync(0);
        await Expect(Component("hubless").Locator(".veto-gauge__needle line")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task The_needle_stops_short_of_the_numbers_it_points_at ()
    {
        await GotoComponentAsync(Route);

        // A pointer that crossed its own scale would obscure the number it is indicating.
        var reach = await Component("speedo").EvaluateAsync<double>(
            "el => { const l = el.querySelector('.veto-gauge__needle line');" +
            "        return 100 - +l.getAttribute('y2'); }");
        var labelRadius = await Component("speedo").EvaluateAsync<double>(
            "el => { const t = el.querySelector('.veto-gauge__tick-label');" +
            "        return Math.hypot(+t.getAttribute('x') - 100, +t.getAttribute('y') - 100); }");

        Assert.True(reach <= labelRadius, $"The needle should stop short: {reach} vs {labelRadius}.");
    }

    [Fact]
    public async Task A_polled_gauge_keeps_fetching_its_own_value ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("live")).ToHaveAttributeAsync("data-polling", "true");

        // The provider is called every second; a handful of samples should land quickly.
        await Expect(Page.GetByTestId("live-samples")).ToHaveTextAsync(new Regex(@"^[1-9]\d* samples$"),
            new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task A_gauge_that_is_not_polling_says_so ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic")).Not.ToHaveAttributeAsync("data-polling", "true");
    }

    [Fact]
    public async Task Size_and_colour_land_as_modifier_classes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("size-sm")).ToHaveClassAsync(new Regex(@"\bveto-gauge--sm\b"));
        await Expect(Component("size-lg")).ToHaveClassAsync(new Regex(@"\bveto-gauge--lg\b"));
        await Expect(Component("size-lg")).ToHaveClassAsync(new Regex(@"\bveto-gauge--success\b"));
        await Expect(Component("half")).ToHaveClassAsync(new Regex(@"\bveto-gauge--warning\b"));
    }

    [Fact]
    public async Task A_large_gauge_is_drawn_larger_than_a_small_one ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        var small = await Component("size-sm").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var large = await Component("size-lg").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        Assert.True(large > small, $"Large should out-measure small: {small} vs {large}.");
    }

    [Fact]
    public async Task The_dials_sweep_is_its_own_geometry ()
    {
        await GotoComponentAsync(Route);

        // A half circle and a full circle draw tracks of very different lengths.
        var half = await Component("half").Locator(".veto-gauge__value")
            .EvaluateAsync<double>("el => parseFloat(el.getAttribute('stroke-dasharray'))");
        var full = await Component("full").Locator(".veto-gauge__value")
            .EvaluateAsync<double>("el => parseFloat(el.getAttribute('stroke-dasharray'))");

        Assert.True(full > half * 1.5, $"A full circle should be much longer than a half: {half} vs {full}.");
    }
}
