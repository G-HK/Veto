using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoGrid</c>, driven against /docs/components/grid.</summary>
public class VetoGridTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/grid";

    private Task<int> ColumnCountAsync ( string testId )
        => Component(testId).EvaluateAsync<int>("el => getComputedStyle(el).gridTemplateColumns.split(' ').length");

    [Fact]
    public async Task A_span_wider_than_the_grid_is_clamped_on_a_narrow_screen ()
    {
        await Page.SetViewportSizeAsync(420, 900);
        await GotoComponentAsync(Route);

        // One column here. Span 2 used to add an implicit second column and overflow.
        Assert.Equal(1, await ColumnCountAsync("clamp"));

        var grid = Component("clamp");
        var overflows = await grid.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1");
        Assert.False(overflows, "The grid should not grow a second column for the wide item.");
    }

    [Fact]
    public async Task The_span_applies_where_there_is_room_for_it ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);

        Assert.Equal(3, await ColumnCountAsync("clamp"));

        var wide = await Component("clamp-wide").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var grid = await Component("clamp").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(wide > grid / 2, $"A span of 2 of 3 should be over half the grid: {wide} of {grid}.");
    }

    [Fact]
    public async Task Min_item_width_fits_as_many_columns_as_there_is_room_for ()
    {
        await Page.SetViewportSizeAsync(1400, 900);
        await GotoComponentAsync(Route);
        var wide = await ColumnCountAsync("auto");

        await Page.SetViewportSizeAsync(420, 900);
        var narrow = await ColumnCountAsync("auto");

        Assert.True(wide > narrow, $"More room should mean more columns: {wide} vs {narrow}.");
        Assert.Equal(1, narrow);
    }
}
