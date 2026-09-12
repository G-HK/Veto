namespace Veto.Components.Gantt;

/// <summary>How a row was changed by dragging.</summary>
public enum VetoGanttEdit
{
    /// <summary>The whole bar was moved, keeping its duration.</summary>
    Moved,

    /// <summary>The start was dragged, changing the duration.</summary>
    StartChanged,

    /// <summary>The end was dragged, changing the duration.</summary>
    EndChanged
}

/// <summary>
/// A change a reader made to a row by dragging it.
/// </summary>
/// <remarks>
/// The chart reports the change rather than editing the caller's list, which it does not own.
/// An app can apply it, store it, reject it, or leave it to the chart's own per-user storage.
/// </remarks>
public sealed record VetoGanttChange
{
    /// <summary>The row that was changed.</summary>
    public required string Id { get; init; }

    /// <summary>What kind of change it was.</summary>
    public required VetoGanttEdit Edit { get; init; }

    /// <summary>The row's start before the change.</summary>
    public required DateOnly PreviousStart { get; init; }

    /// <summary>The row's end before the change.</summary>
    public required DateOnly PreviousEnd { get; init; }

    /// <summary>The row's start after the change.</summary>
    public required DateOnly Start { get; init; }

    /// <summary>The row's end after the change.</summary>
    public required DateOnly End { get; init; }

    /// <summary>How many days the change moved things by. Negative for earlier.</summary>
    public required int Days { get; init; }
}
