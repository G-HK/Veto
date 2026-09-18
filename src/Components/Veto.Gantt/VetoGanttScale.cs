namespace Veto.Components.Gantt;

/// <summary>How much time one column of a <see cref="VetoGantt"/> covers.</summary>
public enum VetoGanttScale
{
    /// <summary>
    /// One column per quarter of an hour, under the hour it belongs to. For jobs that run for
    /// minutes, where an hour column would lump a five-minute run with a fifty-minute one.
    /// </summary>
    QuarterHour,

    /// <summary>
    /// One column per hour. For a single day's jobs, and what the today view uses by default.
    /// </summary>
    Hour,

    /// <summary>One column per day. For a few weeks of detailed work.</summary>
    Day,

    /// <summary>One column per week. For a quarter.</summary>
    Week,

    /// <summary>One column per month. For a year or more.</summary>
    Month
}

/// <summary>How the rows of a <see cref="VetoGantt"/> are ordered.</summary>
public enum VetoGanttSort
{
    /// <summary>
    /// The order the items were given in. The default, because that order usually means
    /// something the chart does not know about.
    /// </summary>
    Source,

    /// <summary>Alphabetically by name.</summary>
    Name,

    /// <summary>Earliest start first.</summary>
    Start,

    /// <summary>Earliest end first.</summary>
    End,

    /// <summary>Shortest first.</summary>
    Duration
}

/// <summary>Turns a scale into the measurements and labels the chart draws with.</summary>
public static class VetoGanttScaleExtensions
{
    /// <summary>
    /// How wide one day is at this scale, in pixels. Bars are positioned in days - fractional
    /// for times of day - multiplied by this, so a bar never has to know which scale is showing.
    /// </summary>
    public static double PixelsPerDay ( this VetoGanttScale scale ) => scale switch
    {
        // 48px an hour: room for an "08:00" label, and a half-hour job is still a visible bar.
        VetoGanttScale.QuarterHour => 24 * 4 * 36,
        VetoGanttScale.Hour => 24 * 48,
        VetoGanttScale.Day => 40,
        VetoGanttScale.Week => 16,
        _ => 4.4
    };

    /// <summary>How far the Previous and Next buttons move, in days.</summary>
    public static int StepDays ( this VetoGanttScale scale ) => scale switch
    {
        VetoGanttScale.QuarterHour => 1,
        VetoGanttScale.Hour => 1,
        VetoGanttScale.Day => 7,
        VetoGanttScale.Week => 28,
        _ => 90
    };

    /// <summary>Whether the scale shows the time of day rather than whole days.</summary>
    public static bool ShowsClockTime ( this VetoGanttScale scale ) =>
        scale is VetoGanttScale.Hour or VetoGanttScale.QuarterHour;

    /// <summary>The scale's name in the picker.</summary>
    public static string ToLabel ( this VetoGanttScale scale ) => scale switch
    {
        VetoGanttScale.QuarterHour => "15 min",
        _ => scale.ToString()
    };

    /// <summary>The modifier suffix for this scale, e.g. <c>day</c>.</summary>
    public static string ToModifier ( this VetoGanttScale scale ) =>
        scale.ToString()
            .ToLowerInvariant();

    /// <summary>
    /// How many days one step of this scale covers, for snapping a drag or an arrow key.
    /// </summary>
    /// <remarks>
    /// A month is treated as 30 days. Snapping to real calendar months would mean a bar landing
    /// on a different day of the month depending on which months it was dragged through, which
    /// is harder to predict than a fixed step.
    /// </remarks>
    public static int SnapDays ( this VetoGanttScale scale ) => scale switch
    {
        VetoGanttScale.QuarterHour => 1,
        VetoGanttScale.Hour => 1,
        VetoGanttScale.Day => 1,
        VetoGanttScale.Week => 7,
        _ => 30
    };
}
/// <summary>What span of time the chart opens on.</summary>
public enum VetoGanttView
{
    /// <summary>
    /// Only today: the axis covers today alone, stretched across the chart, and only the rows
    /// with work on today are listed. Today means the current day - not tomorrow, and not a
    /// window around it.
    /// </summary>
    Today,

    /// <summary>The whole schedule: an axis wide enough for every row, with every row listed.</summary>
    Schedule
}

/// <summary>How a parent's sub-items are laid out.</summary>
public enum VetoGanttSubItemLayout
{
    /// <summary>One row per sub-item, nested under its parent.</summary>
    Rows,

    /// <summary>
    /// Every sub-item drawn side by side on its parent's line - many jobs, one row. Expanding the
    /// parent, when folding is allowed, lists them as rows again.
    /// </summary>
    Inline
}