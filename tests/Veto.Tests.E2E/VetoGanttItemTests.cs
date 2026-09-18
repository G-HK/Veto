using Veto.Components.Gantt;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// Unit tests for building a <see cref="VetoGanttItem"/> from timestamps. No browser: the
/// conversions are pure logic, and the edge cases - midnight, offsets - are what matter.
/// </summary>
public class VetoGanttItemTests
{
    [Fact]
    public void A_timestamp_pair_keeps_its_minutes()
    {
        var item = new VetoGanttItem("ping", "Health ping", new DateTime(2026, 3, 18, 9, 10, 0), new DateTime(2026, 3, 18, 9, 15, 0));

        Assert.Equal(new DateOnly(2026, 3, 18), item.Start);
        Assert.Equal(new TimeOnly(9, 10), item.StartTime);
        Assert.Equal(new TimeOnly(9, 15), item.EndTime);
        Assert.Equal(TimeSpan.FromMinutes(5), item.Duration);
    }

    [Fact]
    public void An_end_at_midnight_closes_the_day_before()
    {
        // Midnight to midnight is the whole of the 18th - not a moment of the 19th.
        var item = new VetoGanttItem("day", "All day", new DateTime(2026, 3, 18), new DateTime(2026, 3, 19));

        Assert.Equal(new DateOnly(2026, 3, 18), item.Start);
        Assert.Equal(new DateOnly(2026, 3, 18), item.End);
        Assert.Null(item.StartTime);
        Assert.Null(item.EndTime);
        Assert.False(item.HasTimes);
        Assert.Equal(TimeSpan.FromDays(1), item.Duration);
    }

    [Fact]
    public void A_run_over_midnight_spans_both_days()
    {
        var item = new VetoGanttItem("late", "Late export", new DateTime(2026, 3, 17, 23, 30, 0), new DateTime(2026, 3, 18, 0, 45, 0));

        Assert.Equal(new DateOnly(2026, 3, 17), item.Start);
        Assert.Equal(new DateOnly(2026, 3, 18), item.End);
        Assert.Equal(new DateTime(2026, 3, 18, 0, 45, 0), item.EndsAt);
    }

    [Fact]
    public void An_offset_is_shown_in_local_time()
    {
        var start = new DateTimeOffset(2026, 3, 18, 8, 0, 0, TimeSpan.Zero);
        var item = new VetoGanttItem("utc", "From the API", start, start.AddMinutes(30));

        Assert.Equal(start.LocalDateTime, item.StartsAt);
        Assert.Equal(TimeSpan.FromMinutes(30), item.Duration);
    }

    [Fact]
    public void The_initializer_form_still_works()
    {
        var item = new VetoGanttItem { Id = "a", Name = "A", Start = new(2026, 3, 18), End = new(2026, 3, 20) };

        Assert.Equal(TimeSpan.FromDays(3), item.Duration);
        Assert.Equal(3, item.DurationDays);
    }
}
