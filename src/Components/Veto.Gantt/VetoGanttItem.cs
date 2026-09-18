using System.Diagnostics.CodeAnalysis;

namespace Veto.Components.Gantt;

/// <summary>
/// One row on a <see cref="VetoGantt"/> - a project, a task beneath one, or a milestone.
/// </summary>
/// <remarks>
/// Hierarchy comes from <see cref="ParentId"/> rather than nesting, so an app can hold its rows
/// as a flat list and hand the same list to a table, an export or a chart without reshaping it.
/// </remarks>
public sealed record VetoGanttItem
{
    /// <summary>Creates an item to fill in with an object initializer: <c>new() { Id = ..., Start = ... }</c>.</summary>
    public VetoGanttItem ()
    {
    }

    /// <summary>
    /// Creates an item from exact timestamps - a job that ran from 08:00 to 08:05 - rather than
    /// from days and times of day. The end is exclusive, as a timestamp's end is: a job ending
    /// at midnight ends with the day before, not a moment into the next one.
    /// </summary>
    [SetsRequiredMembers]
    public VetoGanttItem ( string id, string name, DateTime startsAt, DateTime endsAt )
    {
        Id = id;
        Name = name;

        Start = DateOnly.FromDateTime(startsAt);
        StartTime = startsAt.TimeOfDay == TimeSpan.Zero ? null : TimeOnly.FromDateTime(startsAt);

        if (endsAt.TimeOfDay == TimeSpan.Zero && endsAt > startsAt)
        {
            End = DateOnly.FromDateTime(endsAt).AddDays(-1);
            EndTime = null;
        }
        else
        {
            End = DateOnly.FromDateTime(endsAt);
            EndTime = TimeOnly.FromDateTime(endsAt);
        }
    }

    /// <summary>
    /// Creates an item from exact timestamps with an offset - what an API or a database
    /// usually hands over. They are shown in the local time of wherever the chart runs: the
    /// reader's browser for WebAssembly, the server for Blazor Server.
    /// </summary>
    [SetsRequiredMembers]
    public VetoGanttItem ( string id, string name, DateTimeOffset startsAt, DateTimeOffset endsAt )
        : this(id, name, startsAt.LocalDateTime, endsAt.LocalDateTime)
    {
    }

    /// <summary>Identifies the row. Must be unique across the chart.</summary>
    public required string Id { get; init; }

    /// <summary>The row's name, shown in the panel on the left.</summary>
    public required string Name { get; init; }

    /// <summary>The day the work starts, inclusive.</summary>
    public required DateOnly Start { get; init; }

    /// <summary>The day the work ends, inclusive. Equal to <see cref="Start"/> for a single day.</summary>
    public required DateOnly End { get; init; }

    /// <summary>
    /// The time of day the work starts on <see cref="Start"/>. Null starts it at the beginning of
    /// the day, which is what a whole-day task means.
    /// </summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>
    /// The time of day the work ends on <see cref="End"/>. Null runs it to the end of the day. A
    /// job from 14:00 to 20:00 on one day is <c>Start = End</c> with these two set.
    /// </summary>
    public TimeOnly? EndTime { get; init; }

    /// <summary>When the work starts: <see cref="Start"/> at <see cref="StartTime"/>, or at midnight.</summary>
    public DateTime StartsAt => Start.ToDateTime(StartTime ?? TimeOnly.MinValue);

    /// <summary>
    /// When the work ends, exclusive: <see cref="End"/> at <see cref="EndTime"/>, or the midnight
    /// after <see cref="End"/> for a task that runs the whole day.
    /// </summary>
    public DateTime EndsAt => EndTime is { } time
        ? End.ToDateTime(time)
        : End.AddDays(1).ToDateTime(TimeOnly.MinValue);

    /// <summary>How long the work runs, from <see cref="StartsAt"/> to <see cref="EndsAt"/>.</summary>
    public TimeSpan Duration => EndsAt - StartsAt;

    /// <summary>Whether the row is timed to the hour rather than whole days.</summary>
    public bool HasTimes => StartTime is not null || EndTime is not null;

    /// <summary>
    /// This row's height in pixels, overriding the chart's <c>RowHeight</c> - a taller row for a
    /// project that deserves the attention, a slimmer one for a minor task. Its bar, and any
    /// lanes of stacked jobs, scale with it. Null takes the chart's height.
    /// </summary>
    public double? RowHeight { get; init; }

    /// <summary>The <see cref="Id"/> of the row this one sits under. Null for a top-level row.</summary>
    public string? ParentId { get; init; }

    /// <summary>
    /// The bar's colour, as any CSS colour - including a token such as
    /// <c>var(--veto-color-success)</c>. Falls back to the chart's default.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>How far along the work is, from 0 to 1. Null hides the progress fill for this row.</summary>
    public double? Progress { get; init; }

    /// <summary>A label used for grouping and filtering - a team, a status, a workstream.</summary>
    public string? Category { get; init; }

    /// <summary>Renders the row as a single point in time rather than a bar.</summary>
    public bool IsMilestone { get; init; }

    /// <summary>Extra detail shown in the row's tooltip.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// The <see cref="Id"/>s of rows that must finish before this one starts. Each one is drawn
    /// as an arrow from that row's end to this row's start.
    /// </summary>
    /// <remarks>
    /// The chart only draws these - it does not reschedule anything, so a dependency whose
    /// predecessor finishes after this row starts is still shown, with the arrow routed back on
    /// itself. That is usually worth seeing rather than hiding.
    /// </remarks>
    public IReadOnlyList<string> DependsOn { get; init; } = [];

    /// <summary>The number of days the row spans, counting both ends.</summary>
    public int DurationDays => End.DayNumber - Start.DayNumber + 1;
}