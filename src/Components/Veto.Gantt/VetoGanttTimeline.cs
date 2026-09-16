namespace Veto.Components.Gantt;

/// <summary>
/// The horizontal axis: the span of days on show, and the header cells that label them.
/// </summary>
/// <remarks>
/// Kept separate from the component so the arithmetic can be reasoned about - and corrected -
/// without touching rendering. Everything is measured in whole days from <see cref="Start"/>,
/// which is what lets one positioning rule serve every scale.
/// </remarks>
internal sealed class VetoGanttTimeline
{
    private VetoGanttTimeline ( DateOnly start, DateOnly end, VetoGanttScale scale )
    {
        Start = start;
        End = end;
        Scale = scale;
        PixelsPerDay = scale.PixelsPerDay();
        TotalDays = Math.Max(1, end.DayNumber - start.DayNumber + 1);
        Columns = BuildColumns(start, end, scale);
        Groups = BuildGroups(start, end, scale);
    }

    /// <summary>The first day on the axis.</summary>
    public DateOnly Start { get; }

    /// <summary>The last day on the axis.</summary>
    public DateOnly End { get; }

    /// <summary>The scale the axis is drawn at.</summary>
    public VetoGanttScale Scale { get; }

    /// <summary>Pixels per day at this scale.</summary>
    public double PixelsPerDay { get; }

    /// <summary>The number of days on the axis, counting both ends.</summary>
    public int TotalDays { get; }

    /// <summary>The full width of the axis in pixels.</summary>
    public double Width => TotalDays * PixelsPerDay;

    /// <summary>The lower header row: one cell per day, week or month.</summary>
    public IReadOnlyList<Column> Columns { get; }

    /// <summary>The upper header row: months above days or weeks, years above months.</summary>
    public IReadOnlyList<Column> Groups { get; }

    /// <summary>
    /// Builds an axis covering every item, padded a little at each end so bars do not start or
    /// finish flush against the edge. Falls back to a month around today when there is nothing
    /// to show.
    /// </summary>
    public static VetoGanttTimeline Create ( IReadOnlyList<VetoGanttItem> items, VetoGanttScale scale, DateOnly? start, DateOnly? end, DateOnly today )
    {
        if (start is not null && end is not null)
        {
            return new(start.Value, end.Value, scale);
        }

        if (items.Count == 0)
        {
            return new(today.AddDays(-7), today.AddDays(21), scale);
        }

        var earliest = items.Min(i => i.Start);
        var latest = items.Max(i => i.End);

        // Padding scales with the zoom, so the breathing room looks the same at every scale.
        var padding = scale switch
        {
            VetoGanttScale.Day => 2,
            VetoGanttScale.Week => 7,
            _ => 31
        };

        return new VetoGanttTimeline(
            start ?? earliest.AddDays(-padding),
            end ?? latest.AddDays(padding),
            scale);
    }

    /// <summary>How many pixels from the left edge a given day sits at.</summary>
    public double OffsetOf ( DateOnly day ) => (day.DayNumber - Start.DayNumber) * PixelsPerDay;

    /// <summary>How wide a span of days is, in pixels.</summary>
    public double WidthOf ( DateOnly from, DateOnly to ) =>
        Math.Max(PixelsPerDay, (to.DayNumber - from.DayNumber + 1) * PixelsPerDay);

    /// <summary>Whether a day falls within the axis.</summary>
    public bool Contains ( DateOnly day ) => day >= Start && day <= End;

    private static List<Column> BuildColumns ( DateOnly start, DateOnly end, VetoGanttScale scale )
    {
        var columns = new List<Column>();
        var pixelsPerDay = scale.PixelsPerDay();

        switch (scale)
        {
            case VetoGanttScale.Day:
                for (var day = start; day <= end; day = day.AddDays(1))
                {
                    columns.Add(new Column(
                        day.Day.ToString(),
                        (day.DayNumber - start.DayNumber) * pixelsPerDay,
                        pixelsPerDay,
                        day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
                }

                break;

            case VetoGanttScale.Week:
                // Weeks start on Monday, so a column boundary lines up with the working week.
                var weekStart = start.AddDays(-((int)start.DayOfWeek + 6) % 7);

                for (var day = weekStart; day <= end; day = day.AddDays(7))
                {
                    columns.Add(new Column(
                        day.Day.ToString(),
                        (day.DayNumber - start.DayNumber) * pixelsPerDay,
                        7 * pixelsPerDay,
                        false));
                }

                break;

            default:
                var monthStart = new DateOnly(start.Year, start.Month, 1);

                for (var month = monthStart; month <= end; month = month.AddMonths(1))
                {
                    columns.Add(new Column(
                        month.ToString("MMM"),
                        (month.DayNumber - start.DayNumber) * pixelsPerDay,
                        DateTime.DaysInMonth(month.Year, month.Month) * pixelsPerDay,
                        false));
                }

                break;
        }

        return columns;
    }

    /// <summary>
    /// Builds the upper header row, one step coarser than the lower one: months above days and
    /// weeks, years above months. Cells are clipped to the axis, so a month that starts before
    /// the chart does is labelled but not drawn wider than the space it actually occupies.
    /// </summary>
    private static List<Column> BuildGroups ( DateOnly start, DateOnly end, VetoGanttScale scale )
    {
        var groups = new List<Column>();
        var pixelsPerDay = scale.PixelsPerDay();

        if (scale == VetoGanttScale.Month)
        {
            for (var year = start.Year; year <= end.Year; year++)
            {
                var from = year == start.Year ? start : new DateOnly(year, 1, 1);
                var to = year == end.Year ? end : new DateOnly(year, 12, 31);

                groups.Add(new Column(
                    year.ToString(),
                    (from.DayNumber - start.DayNumber) * pixelsPerDay,
                    (to.DayNumber - from.DayNumber + 1) * pixelsPerDay,
                    false));
            }

            return groups;
        }

        var month = new DateOnly(start.Year, start.Month, 1);

        while (month <= end)
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);

            var from = month < start ? start : month;
            var to = monthEnd > end ? end : monthEnd;

            groups.Add(new Column(
                from.ToString("MMMM yyyy"),
                (from.DayNumber - start.DayNumber) * pixelsPerDay,
                (to.DayNumber - from.DayNumber + 1) * pixelsPerDay,
                false));

            month = month.AddMonths(1);
        }

        return groups;
    }

    /// <summary>One header cell.</summary>
    /// <param name="Label">The text shown in the cell.</param>
    /// <param name="Offset">Pixels from the left edge of the axis.</param>
    /// <param name="Width">The cell's width in pixels.</param>
    /// <param name="IsWeekend">Whether the cell covers a Saturday or Sunday.</param>
    public sealed record Column ( string Label, double Offset, double Width, bool IsWeekend );
}