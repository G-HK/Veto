namespace Veto.Components.Gantt;

/// <summary>How much time one column of a <see cref="VetoGantt"/> covers.</summary>
public enum VetoGanttScale
{
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
    /// How wide one day is at this scale, in pixels. Bars are always positioned by day and
    /// multiplied by this, so a bar never has to know which scale is showing.
    /// </summary>
    public static double PixelsPerDay ( this VetoGanttScale scale ) => scale switch
    {
        VetoGanttScale.Day => 40,
        VetoGanttScale.Week => 16,
        _ => 4.4
    };

    /// <summary>How far the Previous and Next buttons move, in days.</summary>
    public static int StepDays ( this VetoGanttScale scale ) => scale switch
    {
        VetoGanttScale.Day => 7,
        VetoGanttScale.Week => 28,
        _ => 90
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
        VetoGanttScale.Day => 1,
        VetoGanttScale.Week => 7,
        _ => 30
    };
}