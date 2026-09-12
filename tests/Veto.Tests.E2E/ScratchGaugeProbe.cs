using Veto.Tests.E2E.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Veto.Tests.E2E;

/// <summary>
/// Not an assertion - a measurement. Answers how long it takes a slider's input event to reach
/// the gauge's DOM, so "the needle feels laggy" can be split into render cost and the CSS
/// transition rather than guessed at.
/// </summary>
public class ScratchGaugeProbe ( BlazorAppFixture app, ITestOutputHelper output ) : VetoPageTest(app)
{
    [Fact]
    public async Task Measure_input_to_dom_latency ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await Page.GotoAsync($"{App.BaseUrl}/docs/components/gauge");
        await Page.Locator("[data-testid='page'][data-ready='true']")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached, Timeout = 60_000 });

        // Drive the rpm slider the way a drag does - a burst of input events - and time how
        // long each one takes to come back out as a changed attribute on the gauge.
        var result = await Page.EvaluateAsync<Dictionary<string, double>>(@"
            async () => {
                const input = document.querySelector(
                    ""[data-testid='rpm-input'] input.veto-slider__input--lower"");
                const gauge = document.querySelector(""[data-testid='tacho']"");
                const setter = Object.getOwnPropertyDescriptor(
                    window.HTMLInputElement.prototype, 'value').set;

                const samples = [];

                for (let i = 0; i < 40; i++) {
                    const target = 1000 + i * 150;

                    const latency = await new Promise(resolve => {
                        const started = performance.now();
                        const observer = new MutationObserver(() => {
                            observer.disconnect();
                            resolve(performance.now() - started);
                        });
                        observer.observe(gauge, { attributes: true, attributeFilter: ['data-value'] });

                        setter.call(input, String(target));
                        input.dispatchEvent(new Event('input', { bubbles: true }));

                        setTimeout(() => { observer.disconnect(); resolve(-1); }, 2000);
                    });

                    samples.push(latency);
                    await new Promise(r => requestAnimationFrame(r));
                }

                const good = samples.filter(s => s >= 0).sort((a, b) => a - b);
                return {
                    count: good.length,
                    min: good[0],
                    median: good[Math.floor(good.length / 2)],
                    p90: good[Math.floor(good.length * 0.9)],
                    max: good[good.length - 1],
                };
            }");

        var report =
            $"input -> DOM latency over {result["count"]} events:{Environment.NewLine}" +
            $"  min    {result["min"]:0.0} ms{Environment.NewLine}" +
            $"  median {result["median"]:0.0} ms{Environment.NewLine}" +
            $"  p90    {result["p90"]:0.0} ms{Environment.NewLine}" +
            $"  max    {result["max"]:0.0} ms";

        output.WriteLine(report);

        // Written to disk as well: a passing test's console output is swallowed by the build log.
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "veto-gauge-latency.txt"), report);

        // The needle's CSS transition is 150ms on top of whatever this measures.
        Assert.True(result["count"] > 0, "No input event reached the gauge at all.");
    }
}
