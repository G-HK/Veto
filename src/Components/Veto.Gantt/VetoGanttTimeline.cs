namespace Veto.Components.Gantt;

/// <summary>
/// The horizontal axis: the span of time on show, and the header cells that label it.
/// </summary>
/// <remarks>
/// Kept separate from the component so the arithmetic can be reasoned about - and corrected -
/// without touching rendering. Everything is measured in days, fractional where hours are
/// involved, from <see cref="AxisStart"/>, which is what lets one positioning rule serve every
/// scale from hours to months.
/// </remarks>
internal sealed class VetoGanttTimeline
{
    private VetoGanttTimeline ( DateOnly start, DateOnly end, TimeOnly? startTime, TimeOnly? endTime, VetoGanttScale scale, double fitWidth, bool fitExactly = false, double minHourWidth = 0 )
    {
        Start = start;
        End = end;
        Scale = scale;

        AxisStart = start.ToDateTime(startTime ?? TimeOnly.MinValue);
        AxisEnd = endTime is { } until ? end.ToDateTime(until) : end.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // A window that ends before it starts - 20:00 to 14:00 on one day - is read as a slip,
        // not an empty chart: it gets an hour, so something is still drawn.
        if (AxisEnd <= AxisStart)
        {
            AxisEnd = AxisStart.AddHours(1);
        }

        TotalDays = (AxisEnd - AxisStart).TotalDays;

        // A short range at the scale's own density would leave most of the chart empty, so the
        // days are widened until the axis fills the space it has. Not narrowed, as a rule: a long
        // range keeps its density and scrolls. The today view is the exception - one day is
        // meant to be seen whole, so it is fitted either way.
        var natural = scale.PixelsPerDay();
        PixelsPerDay = fitWidth > 0 && (fitExactly || fitWidth > TotalDays * natural)
            ? fitWidth / TotalDays
            : natural;

        // Fitting a whole day into a narrow chart would squeeze each hour until "14:00" no longer
        // fits its cell. Below the floor, the hours keep a readable width and the axis scrolls.
        // A quarter-hour column only has ":15" to fit, so it may be narrower than an hour.
        if (minHourWidth > 0 && scale == VetoGanttScale.Hour)
        {
            PixelsPerDay = Math.Max(PixelsPerDay, minHourWidth * 24);
        }
        else if (minHourWidth > 0 && scale == VetoGanttScale.QuarterHour)
        {
            PixelsPerDay = Math.Max(PixelsPerDay, 28 * 4 * 24);
        }
        IsFitted = Math.Abs(PixelsPerDay - natural) > 0.001;

        Columns = Clip(BuildColumns());
        Groups = Clip(BuildGroups());
    }

    /// <summary>The first day on the axis.</summary>
    public DateOnly Start { get; }

    /// <summary>The last day on the axis.</summary>
    public DateOnly End { get; }

    /// <summary>The instant the axis begins: <see cref="Start"/> at the chart's start time, or midnight.</summary>
    public DateTime AxisStart { get; }

    /// <summary>The instant the axis ends, exclusive.</summary>
    public DateTime AxisEnd { get; }

    /// <summary>The scale the axis is drawn at.</summary>
    public VetoGanttScale Scale { get; }

    /// <summary>Pixels per day: the scale's own figure, or wider when fitted to the chart.</summary>
    public double PixelsPerDay { get; }

    /// <summary>Whether the days were resized - widened, or for the today view narrowed - to fit the chart.</summary>
    public bool IsFitted { get; }

    /// <summary>How many days the axis spans - fractional when it starts or ends mid-day.</summary>
    public double TotalDays { get; }

    /// <summary>The full width of the axis in pixels.</summary>
    public double Width => TotalDays * PixelsPerDay;

    /// <summary>The lower header row: one cell per hour, day, week or month.</summary>
    public IReadOnlyList<Column> Columns { get; }

    /// <summary>The upper header row: days above hours, months above days and weeks, years above months.</summary>
    public IReadOnlyList<Column> Groups { get; }

    /// <summary>
    /// Builds the axis. An explicit start and end win; otherwise the today view covers today
    /// alone, and the schedule view covers every item, padded a little at each end so bars do
    /// not start or finish flush against the edge - or a month around today when there is
    /// nothing to show. <c>startTime</c> and <c>endTime</c> trim the first and last day to a
    /// window, such as 14:00 to 20:00. <c>fitWidth</c> is the width the axis has to fill, in
    /// pixels; zero leaves every scale at its own density. <c>minHourWidth</c> is the narrowest
    /// an hour may be drawn at the hour scale, however little room there is.
    /// </summary>
    public static VetoGanttTimeline Create (
        IReadOnlyList<VetoGanttItem> items,
        VetoGanttScale scale,
        DateOnly? start,
        DateOnly? end,
        DateOnly today,
        VetoGanttView view = VetoGanttView.Schedule,
        double fitWidth = 0,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        double minHourWidth = 0 )
    {
        if (start is not null && end is not null)
        {
            return new(start.Value, end.Value, startTime, endTime, scale, fitWidth, minHourWidth: minHourWidth);
        }

        if (view == VetoGanttView.Today)
        {
            return new(start ?? today, end ?? today, startTime, endTime, scale, fitWidth,
                fitExactly: start is null && end is null, minHourWidth: minHourWidth);
        }

        if (items.Count == 0)
        {
            return new(today.AddDays(-7), today.AddDays(21), startTime, endTime, scale, fitWidth, minHourWidth: minHourWidth);
        }

        var earliest = items.Min(i => i.Start);
        var latest = items.Max(i => i.End);

        // Padding scales with the zoom, so the breathing room looks the same at every scale.
        var padding = scale switch
        {
            VetoGanttScale.QuarterHour or VetoGanttScale.Hour => 0,
            VetoGanttScale.Day => 2,
            VetoGanttScale.Week => 7,
            _ => 31
        };

        return new VetoGanttTimeline(
            start ?? earliest.AddDays(-padding),
            end ?? latest.AddDays(padding),
            startTime,
            endTime,
            scale,
            fitWidth,
            minHourWidth: minHourWidth);
    }

    /// <summary>How many pixels from the left edge an instant sits at.</summary>
    public double OffsetAt ( DateTime instant ) => (instant - AxisStart).TotalDays * PixelsPerDay;

    /// <summary>How many pixels from the left edge the start of a day sits at.</summary>
    public double OffsetOf ( DateOnly day ) => OffsetAt(day.ToDateTime(TimeOnly.MinValue));

    /// <summary>How wide a span of whole days is, in pixels.</summary>
    public double WidthOf ( DateOnly from, DateOnly to ) =>
        Math.Max(PixelsPerDay, (to.DayNumber - from.DayNumber + 1) * PixelsPerDay);

    /// <summary>Whether a day falls within the axis.</summary>
    public bool Contains ( DateOnly day ) => day >= Start && day <= End;

    /// <summary>Whether an instant falls within the axis.</summary>
    public bool Contains ( DateTime instant ) => instant >= AxisStart && instant < AxisEnd;

    /// <summary>Whether any part of a span of whole days falls within the axis.</summary>
    public bool Overlaps ( DateOnly from, DateOnly to ) =>
        Overlaps(from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));

    /// <summary>Whether any part of a span of time, end exclusive, falls within the axis.</summary>
    public bool Overlaps ( DateTime from, DateTime to ) => from < AxisEnd && to > AxisStart;

    /// <summary>
    /// Where a span of time sits once cut to the axis: work that began before the chart starts
    /// at its left edge rather than off-screen, and work that runs past the end stops at the
    /// right. Never narrower than a few pixels, so a five-minute job is still something to see
    /// and click.
    /// </summary>
    public (double Left, double Width) Place ( DateTime from, DateTime to )
    {
        const double minimum = 4;

        var start = from < AxisStart ? AxisStart : from > AxisEnd ? AxisEnd : from;
        var end = to > AxisEnd ? AxisEnd : to < start ? start : to;

        var left = OffsetAt(start);
        var width = Math.Max(minimum, OffsetAt(end) - left);

        return (left, width);
    }

    /// <summary>Where a span of whole days sits once cut to the axis.</summary>
    public (double Left, double Width) Place ( DateOnly from, DateOnly to ) =>
        Place(from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));

    /// <summary>
    /// Cuts header cells to the axis. A week or month that begins before the axis would
    /// otherwise hang off the left edge, and one that ends after it off the right - and with a
    /// start time, even the first day begins before the axis does.
    /// </summary>
    private List<Column> Clip ( List<Column> columns )
    {
        var clipped = new List<Column>(columns.Count);

        foreach (var column in columns)
        {
            var left = Math.Max(0, column.Offset);
            var right = Math.Min(Width, column.Offset + column.Width);

            if (right > left)
            {
                clipped.Add(column with { Offset = left, Width = right - left });
            }
        }

        return clipped;
    }

    private Column Cell ( string label, DateTime from, DateTime to, bool isWeekend = false )
        => new(label, OffsetAt(from), OffsetAt(to) - OffsetAt(from), isWeekend);

    private static DateTime Midnight ( DateOnly day ) => day.ToDateTime(TimeOnly.MinValue);

    private List<Column> BuildColumns ()
    {
        var columns = new List<Column>();

        switch (Scale)
        {
            case VetoGanttScale.QuarterHour:
                var quarter = new DateTime(AxisStart.Year, AxisStart.Month, AxisStart.Day, AxisStart.Hour, AxisStart.Minute / 15 * 15, 0);

                for (; quarter < AxisEnd; quarter = quarter.AddMinutes(15))
                {
                    columns.Add(Cell(quarter.ToString(":mm"), quarter, quarter.AddMinutes(15)));
                }

                break;

            case VetoGanttScale.Hour:
                var hour = new DateTime(AxisStart.Year, AxisStart.Month, AxisStart.Day, AxisStart.Hour, 0, 0);

                for (; hour < AxisEnd; hour = hour.AddHours(1))
                {
                    columns.Add(Cell(hour.ToString("HH:00"), hour, hour.AddHours(1)));
                }

                break;

            case VetoGanttScale.Day:
                for (var day = Start; day <= End; day = day.AddDays(1))
                {
                    columns.Add(Cell(
                        day.Day.ToString(),
                        Midnight(day),
                        Midnight(day.AddDays(1)),
                        day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
                }

                break;

            case VetoGanttScale.Week:
                // Weeks start on Monday, so a column boundary lines up with the working week.
                var weekStart = Start.AddDays(-((int)Start.DayOfWeek + 6) % 7);

                for (var day = weekStart; day <= End; day = day.AddDays(7))
                {
                    columns.Add(Cell(day.Day.ToString(), Midnight(day), Midnight(day.AddDays(7))));
                }

                break;

            default:
                var monthStart = new DateOnly(Start.Year, Start.Month, 1);

                for (var month = monthStart; month <= End; month = month.AddMonths(1))
                {
                    columns.Add(Cell(month.ToString("MMM"), Midnight(month), Midnight(month.AddMonths(1))));
                }

                break;
        }

        return columns;
    }

    /// <summary>
    /// Builds the upper header row, one step coarser than the lower one: days above hours,
    /// months above days and weeks, years above months. Clipped to the axis afterwards, so a
    /// month that starts before the chart does is labelled but not drawn wider than the space
    /// it actually occupies.
    /// </summary>
    private List<Column> BuildGroups ()
    {
        var groups = new List<Column>();

        if (Scale == VetoGanttScale.QuarterHour)
        {
            // The hours head the quarters; the first, and each midnight, also names the day.
            var hour = new DateTime(AxisStart.Year, AxisStart.Month, AxisStart.Day, AxisStart.Hour, 0, 0);

            for (var first = true; hour < AxisEnd; hour = hour.AddHours(1), first = false)
            {
                var label = first || hour.Hour == 0 ? hour.ToString("ddd d MMM, HH:00") : hour.ToString("HH:00");
                groups.Add(Cell(label, hour, hour.AddHours(1)));
            }

            return groups;
        }

        if (Scale == VetoGanttScale.Hour)
        {
            for (var day = Start; day <= End; day = day.AddDays(1))
            {
                groups.Add(Cell(day.ToString("ddd d MMMM"), Midnight(day), Midnight(day.AddDays(1))));
            }

            return groups;
        }

        if (Scale == VetoGanttScale.Month)
        {
            for (var year = Start.Year; year <= End.Year; year++)
            {
                groups.Add(Cell(
                    year.ToString(),
                    Midnight(new DateOnly(year, 1, 1)),
                    Midnight(new DateOnly(year + 1, 1, 1))));
            }

            return groups;
        }

        for (var month = new DateOnly(Start.Year, Start.Month, 1); month <= End; month = month.AddMonths(1))
        {
            // Labelled with the first day actually on the axis, so a chart starting mid-month
            // still reads the month it is in.
            var from = month < Start ? Start : month;

            groups.Add(Cell(from.ToString("MMMM yyyy"), Midnight(month), Midnight(month.AddMonths(1))));
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