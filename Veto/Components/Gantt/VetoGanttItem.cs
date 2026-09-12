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
    /// <summary>Identifies the row. Must be unique across the chart.</summary>
    public required string Id { get; init; }

    /// <summary>The row's name, shown in the panel on the left.</summary>
    public required string Name { get; init; }

    /// <summary>The day the work starts, inclusive.</summary>
    public required DateOnly Start { get; init; }

    /// <summary>The day the work ends, inclusive. Equal to <see cref="Start"/> for a single day.</summary>
    public required DateOnly End { get; init; }

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