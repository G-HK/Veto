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
    public async Task The_car_runs_up_through_its_gears ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("car")).ToHaveAttributeAsync("data-gear", "1");

        // Seven ratios to 350 km/h, so the gearbox has to work its way up on its own.
        await Expect(Component("car")).ToHaveAttributeAsync("data-gear", "4",
            new() { Timeout = 30_000 });

        var speed = await Page.GetByTestId("car-speedo")
            .EvaluateAsync<double>("el => parseFloat(el.getAttribute('data-value'))");

        Assert.True(speed > 100, $"By fourth gear the car should be moving: {speed} km/h.");
    }

    [Fact]
    public async Task The_revs_fall_when_a_gear_goes_in ()
    {
        await GotoComponentAsync(Route);

        // Sampling "whenever the test happens to look" is no good here: it can catch one gear
        // just after its shift and the next one well into its pull, and read the drop
        // backwards. Watch the panel instead and record the revs either side of an actual
        // gear change.
        var drop = await Page.EvaluateAsync<double[]>(@"
            () => new Promise(resolve => {
                const car = document.querySelector(""[data-testid='car']"");
                const tacho = document.querySelector(""[data-testid='car-tacho']"");
                const rpm = () => parseFloat(tacho.getAttribute('data-value'));

                let gear = car.getAttribute('data-gear');
                let last = rpm();

                const observer = new MutationObserver(() => {
                    const now = car.getAttribute('data-gear');

                    if (now !== gear) {
                        const before = last;
                        gear = now;
                        // The revs land on the new ratio a frame or two after the gear changes.
                        setTimeout(() => {
                            observer.disconnect();
                            resolve([before, rpm()]);
                        }, 200);
                        return;
                    }

                    last = rpm();
                });

                observer.observe(car, { attributes: true });
                observer.observe(tacho, { attributes: true, attributeFilter: ['data-value'] });

                setTimeout(() => { observer.disconnect(); resolve([-1, -1]); }, 30000);
            })");

        Assert.True(drop[0] > 0, "No gear change was seen within 30 seconds.");

        // This is the whole character of the thing: the revs fall as the next gear takes up.
        Assert.True(drop[1] < drop[0],
            $"The revs should fall on the upshift: {drop[0]:0} -> {drop[1]:0}.");
    }

    [Fact]
    public async Task The_revs_go_amber_and_then_red ()
    {
        await GotoComponentAsync(Route);

        // Two bands, so the dial warns before it shouts: 6500 amber, 7200 red, 8000 limiter.
        await Expect(Page.GetByTestId("car-tacho"))
            .ToHaveAttributeAsync("data-band", "Approaching the line", new() { Timeout = 30_000 });

        await Expect(Page.GetByTestId("car-tacho"))
            .ToHaveAttributeAsync("data-band", "Red line", new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task The_revs_reach_the_limiter ()
    {
        await GotoComponentAsync(Route);

        var peak = await Page.EvaluateAsync<double>(@"
            () => new Promise(resolve => {
                const tacho = document.querySelector(""[data-testid='car-tacho']"");
                let top = 0;

                const observer = new MutationObserver(() => {
                    const rpm = parseFloat(tacho.getAttribute('data-value'));
                    if (rpm > top) { top = rpm; }
                    if (top >= 8000) { observer.disconnect(); resolve(top); }
                });

                observer.observe(tacho, { attributes: true, attributeFilter: ['data-value'] });
                setTimeout(() => { observer.disconnect(); resolve(top); }, 30000);
            })");

        Assert.Equal(8000, peak);
    }

    [Fact]
    public async Task The_dial_flickers_once_it_is_into_the_red ()
    {
        await GotoComponentAsync(Route);

        var tacho = Page.GetByTestId("car-tacho");

        await Expect(tacho).ToHaveAttributeAsync("data-flicker", "true", new() { Timeout = 30_000 });
        await Expect(tacho).ToHaveClassAsync(new Regex(@"\bveto-gauge--flicker\b"));

        var animation = await tacho.Locator(".veto-gauge__needle")
            .EvaluateAsync<string>("el => getComputedStyle(el).animationName");

        // Blazor's CSS isolation rewrites @keyframes names too, appending the scope id - so
        // the running animation is "veto-gauge-flicker-b-xxxxxxxxxx", not the bare name.
        Assert.StartsWith("veto-gauge-flicker", animation);
    }

    [Fact]
    public async Task A_dial_below_the_flicker_point_stays_still ()
    {
        await GotoComponentAsync(Route);

        // The ordinary gauges have no FlickerFrom at all, so nothing should be pulsing.
        Assert.False(await Component("basic").EvaluateAsync<bool>(
            "el => el.hasAttribute('data-flicker')"));
        Assert.Equal("none", await Component("basic").Locator(".veto-gauge__value")
            .EvaluateAsync<string>("el => getComputedStyle(el).animationName"));
    }

    [Fact]
    public async Task The_shift_light_comes_on_before_the_limiter ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("car").Locator(".car__light--shift"))
            .ToHaveAttributeAsync("data-lit", "true", new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task The_car_holds_top_speed_and_then_starts_again ()
    {
        await GotoComponentAsync(Route);

        // Seven gears to 350, so give it room; the hold is five seconds at the top.
        await Expect(Component("car")).ToHaveAttributeAsync("data-state", "holding",
            new() { Timeout = 90_000 });

        var top = await Page.GetByTestId("car-speedo")
            .EvaluateAsync<double>("el => parseFloat(el.getAttribute('data-value'))");

        Assert.Equal(350, top);
        await Expect(Page.GetByTestId("car-status")).ToContainTextAsync("Top speed");

        // And then back to the line, in first.
        await Expect(Component("car")).ToHaveAttributeAsync("data-gear", "1",
            new() { Timeout = 20_000 });
    }

    [Fact]
    public async Task The_gearbox_has_seven_ratios ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("car")).ToHaveAttributeAsync("data-gear", "7",
            new() { Timeout = 90_000 });
    }

    [Fact]
    public async Task The_car_can_be_paused_and_sent_back_to_the_line ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("car-toggle").ClickAsync();
        await Expect(Component("car")).ToHaveAttributeAsync("data-state", "paused");

        var speedo = Page.GetByTestId("car-speedo");
        var parked = await speedo.EvaluateAsync<double>("el => parseFloat(el.getAttribute('data-value'))");
        await Page.WaitForTimeoutAsync(600);
        var still = await speedo.EvaluateAsync<double>("el => parseFloat(el.getAttribute('data-value'))");

        Assert.Equal(parked, still);

        await Page.GetByTestId("car-reset").ClickAsync();
        await Expect(speedo).ToHaveAttributeAsync("data-value", "0");
        await Expect(Component("car")).ToHaveAttributeAsync("data-gear", "1");
    }

    [Fact]
    public async Task An_animated_dial_is_given_a_transition_it_can_keep_up_with ()
    {
        await GotoComponentAsync(Route);

        // The default 150ms ease would leave a needle driven every 40ms permanently chasing a
        // value it never catches. The car hands each gauge its own step instead.
        var transition = await Page.GetByTestId("car-speedo")
            .EvaluateAsync<string>("el => el.style.getPropertyValue('--veto-gauge-transition')");

        Assert.Equal("40ms linear", transition);
    }

    [Fact]
    public async Task A_dial_can_be_given_a_printed_face ()
    {
        await GotoComponentAsync(Route);

        var face = Component("face-plain").Locator(".veto-gauge__face");

        await Expect(face).ToHaveCountAsync(1);

        var background = await face.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundImage");

        Assert.Contains("gradient", background);
    }

    [Fact]
    public async Task The_face_sits_under_the_dial_the_needle_sweeps_over_it ()
    {
        await GotoComponentAsync(Route);

        // The face is painted first and the svg after it, so the ticks and the needle are
        // drawn on top - which is the whole point of calling it a face.
        var faceFirst = await Component("face-badge").EvaluateAsync<bool>(
            "el => { const kids = Array.from(el.children);" +
            "        return kids.findIndex(k => k.classList.contains('veto-gauge__face'))" +
            "             < kids.findIndex(k => k.classList.contains('veto-gauge__dial')); }");

        Assert.True(faceFirst, "The face should be painted before the dial.");
    }

    [Fact]
    public async Task Face_content_is_rendered_but_hidden_from_assistive_technology ()
    {
        await GotoComponentAsync(Route);

        var face = Component("face-badge").Locator(".veto-gauge__face");

        await Expect(face).ToContainTextAsync("VETO");
        await Expect(face).ToContainTextAsync("reto spec");

        // The reading is what a gauge is for; a badge printed on it is not part of that.
        await Expect(face).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Fact]
    public async Task The_face_is_clipped_to_the_dials_circle ()
    {
        await GotoComponentAsync(Route);

        var radius = await Component("face-plain").Locator(".veto-gauge__face")
            .EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius");
        var overflow = await Component("face-plain").Locator(".veto-gauge__face")
            .EvaluateAsync<string>("el => getComputedStyle(el).overflow");

        // A rectangular image must not show its corners through a round gauge.
        Assert.Equal("50%", radius);
        Assert.Contains("hidden", overflow);
    }

    [Fact]
    public async Task A_gauge_with_no_face_renders_none ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic").Locator(".veto-gauge__face")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_cars_dials_carry_their_makers_plate ()
    {
        await GotoComponentAsync(Route);

        var plate = Page.GetByTestId("car-tacho").Locator(".veto-gauge__face");

        await Expect(plate).ToContainTextAsync("VETO");
        await Expect(plate).ToContainTextAsync("reto spec");

        // The red rule down the left of the name.
        var rule = await plate.Locator(".car__badge-rule")
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        Assert.NotEqual("rgba(0, 0, 0, 0)", rule);
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

    [Fact]
    public async Task A_parent_that_rerenders_on_each_sample_does_not_speed_up_polling ()
    {
        await GotoComponentAsync(Route);

        // The demo's OnValueChanged re-renders the page on every sample. That used to restart
        // polling, which fetches at once - a loop with no delay in it, thousands of samples a
        // second. At PollInterval="1000" three seconds is three or four samples.
        await Expect(Page.GetByTestId("live-samples")).ToHaveTextAsync(new Regex(@"^[1-9]\d* samples$"),
            new() { Timeout = 15_000 });
        await Page.WaitForTimeoutAsync(3000);

        var text = await Page.GetByTestId("live-samples").TextContentAsync();
        var samples = int.Parse(text!.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(samples, 1, 8);
    }

    [Fact]
    public async Task A_target_is_marked_and_announced ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("target-target")).ToHaveCountAsync(1);
        await Expect(Component("target").Locator("svg")).ToHaveAttributeAsync("aria-valuetext", "64%, target 80%");
    }
}
