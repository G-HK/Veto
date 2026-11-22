using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoIcon</c>, driven against /docs/components/icon.</summary>
public class VetoIconTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/icon";

    [Fact]
    public async Task Icons_are_decorative_unless_titled ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("decorative")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Component("named")).ToHaveAttributeAsync("role", "img");
        await Expect(Component("named")).ToHaveAccessibleNameAsync("Delete");
    }

    [Fact]
    public async Task Color_uses_the_theme_token ()
    {
        await GotoComponentAsync(Route);

        var expected = await Page.EvaluateAsync<string>(
            "() => { const probe = document.createElement('span'); probe.style.color = 'var(--veto-color-danger)'; document.body.append(probe); const c = getComputedStyle(probe).color; probe.remove(); return c; }");

        await Expect(Component("color-danger")).ToHaveCSSAsync("color", expected);
    }

    [Fact]
    public async Task Flip_mirrors_the_icon ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("flipped")).ToHaveCSSAsync("scale", "-1 1");
    }

    [Fact]
    public async Task Stroke_width_is_written_as_a_number ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("thin")).ToHaveAttributeAsync("stroke-width", "1.5");
    }
}

/// <summary>
/// The same page under a German browser. Blazor WebAssembly takes its culture from the browser,
/// so this is where a number formatted with the current culture shows up as "1,5".
/// </summary>
public class VetoIconCultureTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    public override BrowserNewContextOptions ContextOptions () => new() { Locale = "de-DE" };

    [Fact]
    public async Task Stroke_width_ignores_a_comma_decimal_culture ()
    {
        await GotoComponentAsync("docs/components/icon");

        await Expect(Component("thin")).ToHaveAttributeAsync("stroke-width", "1.5");
    }
}
