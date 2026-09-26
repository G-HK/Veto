using System.Globalization;
using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// End-to-end specs for VetoTilt3D, driven against /docs/components/tilt. The effect is
/// pointer-driven, so these move a real mouse and read the resulting transform rather than
/// asserting on markup.
/// </summary>
public class VetoTilt3DTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/tilt";

    private ILocator Surface ( string testId ) => Component(testId).Locator(".veto-tilt-3d__surface");

    private Task<string> TransformAsync ( string testId )
        => Surface(testId).EvaluateAsync<string>("el => getComputedStyle(el).transform");

    /// <summary>
    /// Reads a tilt angle from the element's inline custom property. Parsed with the invariant
    /// culture on purpose: CSS always writes "4.86deg" with a dot, and on a machine whose locale
    /// uses a comma for decimals the current culture reads that dot as a group separator and
    /// turns 4.86 into 486.
    /// </summary>
    private async Task<double> TiltDegreesAsync ( string testId, string axis )
    {
        var raw = await Surface(testId).EvaluateAsync<string>(
            $"el => el.style.getPropertyValue('--veto-tilt-{axis}')");

        return double.Parse(raw.Replace("deg", string.Empty), CultureInfo.InvariantCulture);
    }

    /// <summary>Moves the pointer to a point inside the surface, expressed as a 0-1 fraction of its box.</summary>
    private async Task PointAtAsync ( string testId, double fractionX, double fractionY )
    {
        // The box is in viewport coordinates, so an element below the fold has to be scrolled
        // into view first - otherwise the mouse is moved to a point that is somewhere else
        // entirely on screen.
        await Surface(testId).ScrollIntoViewIfNeededAsync();

        var box = await Surface(testId).BoundingBoxAsync()
            ?? throw new InvalidOperationException($"The '{testId}' tilt surface has no box.");

        await Page.Mouse.MoveAsync(
            (float) (box.X + box.Width * fractionX),
            (float) (box.Y + box.Height * fractionY),
            new() { Steps = 4 });
    }

    [Fact]
    public async Task Renders_flat_until_the_pointer_arrives ()
    {
        await GotoComponentAsync(Route);

        // "none" or the identity matrix, depending on the browser - either means untransformed.
        var transform = await TransformAsync("basic");
        Assert.True(
            transform is "none" or "matrix(1, 0, 0, 1, 0, 0)",
            $"Expected an untouched surface to be flat, but the transform was '{transform}'.");
    }

    [Fact]
    public async Task Tilts_towards_the_pointer ()
    {
        await GotoComponentAsync(Route);

        await PointAtAsync("basic", 0.9, 0.5);
        await Expect(Surface("basic")).ToHaveAttributeAsync("data-tilting", "true");

        var transform = await TransformAsync("basic");
        Assert.StartsWith("matrix3d", transform);
    }

    [Fact]
    public async Task Opposite_edges_tilt_in_opposite_directions ()
    {
        await GotoComponentAsync(Route);

        await PointAtAsync("basic", 0.05, 0.5);
        var leftDegrees = await TiltDegreesAsync("basic", "y");

        await PointAtAsync("basic", 0.95, 0.5);
        var rightDegrees = await TiltDegreesAsync("basic", "y");

        Assert.True(leftDegrees < 0, $"Left edge should tilt negative, but was {leftDegrees}.");
        Assert.True(rightDegrees > 0, $"Right edge should tilt positive, but was {rightDegrees}.");
    }

    [Fact]
    public async Task Settles_back_to_flat_when_the_pointer_leaves ()
    {
        await GotoComponentAsync(Route);

        await PointAtAsync("basic", 0.9, 0.9);
        Assert.StartsWith("matrix3d", await TransformAsync("basic"));

        // Away from the card entirely.
        await Page.Mouse.MoveAsync(2, 2, new() { Steps = 4 });

        await Expect(Surface("basic")).Not.ToHaveAttributeAsync("data-tilting", "true");
        await Expect(Surface("basic")).ToHaveCSSAsync("--veto-tilt-x", "0deg");
        await Expect(Surface("basic")).ToHaveCSSAsync("--veto-tilt-y", "0deg");
    }

    [Fact]
    public async Task MaxTilt_bounds_the_angle ()
    {
        await GotoComponentAsync(Route);

        // Just inside the corner, not exactly on it: a point at exactly x + width sits on the
        // boundary, which the browser can treat as outside the element.
        await PointAtAsync("subtle", 0.95, 0.95);
        var subtleDegrees = Math.Abs(await TiltDegreesAsync("subtle", "y"));

        await PointAtAsync("strong", 0.95, 0.95);
        var strongDegrees = Math.Abs(await TiltDegreesAsync("strong", "y"));

        Assert.True(subtleDegrees <= 5.01, $"MaxTilt 5 should not exceed 5 degrees, but was {subtleDegrees}.");
        Assert.True(strongDegrees > subtleDegrees, $"MaxTilt 25 ({strongDegrees}) should exceed MaxTilt 5 ({subtleDegrees}).");
    }

    [Fact]
    public async Task Reset_false_holds_the_last_angle ()
    {
        await GotoComponentAsync(Route);

        await PointAtAsync("hold", 0.9, 0.9);
        Assert.NotEqual(0, await TiltDegreesAsync("hold", "x"));

        await Page.Mouse.MoveAsync(2, 2, new() { Steps = 4 });

        // The contract is that it does not flatten - not that it keeps the angle from any
        // particular sample. The pointer crosses the surface on its way out, so the angle it
        // holds is the one at the exit point.
        await Expect(Surface("hold")).Not.ToHaveAttributeAsync("data-tilting", "true");
        Assert.NotEqual(0, await TiltDegreesAsync("hold", "x"));

        // The contrast that gives this meaning: a resetting tilt does flatten.
        await PointAtAsync("basic", 0.9, 0.9);
        await Page.Mouse.MoveAsync(2, 2, new() { Steps = 4 });
        await Expect(Surface("basic")).ToHaveCSSAsync("--veto-tilt-x", "0deg");
    }

    [Fact]
    public async Task Disabled_never_tilts ()
    {
        await GotoComponentAsync(Route);

        await PointAtAsync("disabled", 0.9, 0.9);

        await Expect(Component("disabled")).ToHaveClassAsync(
            new Regex(@"\bveto-tilt-3d--disabled\b"));

        var transform = await TransformAsync("disabled");
        Assert.True(
            transform is "none" or "matrix(1, 0, 0, 1, 0, 0)",
            $"A disabled tilt should stay flat, but the transform was '{transform}'.");
    }

    [Fact]
    public async Task Glare_follows_the_pointer_and_ignores_clicks ()
    {
        await GotoComponentAsync(Route);

        var glare = Component("glare").Locator(".veto-tilt-3d__glare");
        await Expect(glare).ToHaveCSSAsync("pointer-events", "none");

        await PointAtAsync("glare", 0.25, 0.25);

        // Polled rather than read once: the glare fades in, so a single read lands mid-transition.
        await Expect(glare).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public async Task The_effect_is_dropped_entirely_under_reduced_motion ()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await GotoComponentAsync(Route);

        await PointAtAsync("basic", 0.9, 0.9);

        // Not merely un-animated: the listeners are never attached, so nothing marks it tilting.
        await Expect(Surface("basic")).Not.ToHaveAttributeAsync("data-tilting", "true");

        var transform = await TransformAsync("basic");
        Assert.True(
            transform is "none" or "matrix(1, 0, 0, 1, 0, 0)",
            $"Reduced motion should leave the surface flat, but the transform was '{transform}'.");
    }

    [Fact]
    public async Task Content_inside_the_tilt_stays_usable ()
    {
        await GotoComponentAsync(Route);

        // The tilt wraps content; it must not swallow it or its accessible text.
        await Expect(Component("basic")).ToContainTextAsync("The whole card is the tilt surface.");
    }

    [Fact]
    public async Task Changed_parameters_apply_while_attached ()
    {
        await GotoComponentAsync(Route);

        // Not right at the edge: at 30 degrees the surface turns far enough that a point at the
        // very edge of its flat box falls outside it, and the pointer "leaves".
        await PointAtAsync("live", 0.8, 0.5);
        var before = await TiltDegreesAsync("live", "y");
        Assert.True(before <= 4.01, $"MaxTilt 4 should bound the angle, but was {before}.");

        await Page.Mouse.MoveAsync(0, 0);
        await Component("live-toggle").ClickAsync();
        await Expect(Component("live")).ToContainTextAsync("MaxTilt 30");

        await PointAtAsync("live", 0.8, 0.5);
        var after = await TiltDegreesAsync("live", "y");
        Assert.True(after > 12, $"After raising MaxTilt to 30 the edge should tilt further, but was {after}.");
    }

    [Fact]
    public async Task Reverse_leans_towards_the_pointer ()
    {
        await GotoComponentAsync(Route);

        await PointAtAsync("reverse", 0.95, 0.5);

        Assert.True(await TiltDegreesAsync("reverse", "y") < 0, "Reverse should flip the sign of the tilt.");
    }

    [Fact]
    public async Task A_horizontal_axis_never_tilts_up_or_down ()
    {
        await GotoComponentAsync(Route);

        await PointAtAsync("axis", 0.9, 0.05);

        Assert.Equal(0, await TiltDegreesAsync("axis", "x"));
        Assert.True(await TiltDegreesAsync("axis", "y") > 0);
    }
}
