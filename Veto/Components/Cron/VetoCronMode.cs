namespace Veto.Components.Cron;

/// <summary>
/// The shape of schedule a <see cref="VetoCron"/> tab builds. Each one is a familiar phrasing
/// of a schedule; <see cref="Advanced"/> is the escape hatch back to raw cron.
/// </summary>
public enum VetoCronMode
{
    /// <summary>Every <i>n</i> minutes.</summary>
    Minutes,

    /// <summary>Every <i>n</i> hours, at a given minute past.</summary>
    Hourly,

    /// <summary>Every <i>n</i> days, at a given time.</summary>
    Daily,

    /// <summary>On chosen days of the week, at a given time.</summary>
    Weekly,

    /// <summary>On a chosen day of every <i>n</i> months, at a given time.</summary>
    Monthly,

    /// <summary>On one day of one month each year, at a given time.</summary>
    Yearly,

    /// <summary>The expression typed by hand - anything the other tabs cannot say.</summary>
    Advanced
}
