using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoStack</c>'s dividers, driven against /docs/components/stack.</summary>
public class VetoStackTests(BlazorAppFixture app) : VetoPageTest(app)
{
    private const string Route = "docs/components/stack";

    // Per child: [borderTop, borderLeft, borderLeftStyle, paddingTop, paddingLeft, gap of the stack].
    private Task<string[][]> ChildrenAsync(string testId) =>
        Component(testId).EvaluateAsync<string[][]>(
            """
            el => [...el.children].map(c => {
                const s = getComputedStyle(c);
                return [s.borderTopWidth, s.borderLeftWidth, s.borderLeftStyle, s.paddingTop, s.paddingLeft, getComputedStyle(el).rowGap];
            })
            """);

    [Fact]
    public async Task A_column_draws_a_line_between_children_but_not_before_the_first()
    {
        await GotoComponentAsync(Route);
        var children = await ChildrenAsync("divided-column");

        Assert.Equal("0px", children[0][0]);
        Assert.All(children.Skip(1), c =>
        {
            Assert.Equal("1px", c[0]);
            // Padding equal to the gap is what centres the line between the two children.
            Assert.Equal(c[5], c[3]);
        });
    }

    [Fact]
    public async Task A_row_draws_vertical_lines_in_the_requested_style_and_thickness()
    {
        await GotoComponentAsync(Route);
        var children = await ChildrenAsync("divided-row");

        Assert.Equal("0px", children[0][1]);
        Assert.All(children.Skip(1), c =>
        {
            Assert.Equal("0px", c[0]);
            Assert.Equal("2px", c[1]);
            Assert.Equal("dashed", c[2]);
        });
    }

    [Fact]
    public async Task An_inset_paints_the_line_as_a_gradient_that_stops_short_of_both_ends()
    {
        await GotoComponentAsync(Route);

        var images = await Component("divided-inset").EvaluateAsync<string[]>(
            "el => [...el.children].map(c => getComputedStyle(c).borderImageSource)");

        Assert.Equal("none", images[0]);
        Assert.All(images.Skip(1), image =>
        {
            Assert.StartsWith("linear-gradient", image);
            Assert.Contains("15%", image);
        });

        // Without an inset, the plain border draws the line - no gradient.
        var plain = await Component("divided-column").EvaluateAsync<string>(
            "el => getComputedStyle(el.children[1]).borderImageSource");
        Assert.Equal("none", plain);
    }

    [Fact]
    public async Task A_start_aligned_inset_line_begins_at_the_edge_and_keeps_its_length()
    {
        await GotoComponentAsync(Route);

        // Resolved gradient stops: [line start, line end] as px along the child's width.
        const string Stops =
            """
            el => {
                const c = el.children[1];
                const w = c.getBoundingClientRect().width;
                const s = getComputedStyle(c);
                const px = v => v.trim().endsWith('%') ? parseFloat(v) / 100 * w : parseFloat(v);
                return [px(s.getPropertyValue('--veto-stack-divider-from')), w,
                        px(s.getPropertyValue('--veto-stack-divider-inset'))];
            }
            """;

        var start = await Component("divided-start").EvaluateAsync<double[]>(Stops);
        Assert.Equal(0, start[0], 1);

        var center = await Component("divided-inset").EvaluateAsync<double[]>(Stops);
        Assert.Equal(center[2], center[0], 1);
    }
}