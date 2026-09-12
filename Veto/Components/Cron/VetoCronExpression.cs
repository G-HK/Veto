namespace Veto.Components.Cron;

/// <summary>
/// A parsed cron expression: five fields, or six when the expression carries seconds.
/// </summary>
/// <remarks>
/// <para>
/// Standard five-field cron - <c>minute hour day-of-month month day-of-week</c> - with the
/// usual syntax: <c>*</c>, a number, <c>a-b</c>, <c>a,b,c</c>, <c>*/n</c> and <c>a-b/n</c>.
/// Months accept <c>JAN</c>-<c>DEC</c> and days of the week <c>SUN</c>-<c>SAT</c>. Sunday is
/// both <c>0</c> and <c>7</c>, as it is in every cron anyone has shipped.
/// </para>
/// <para>
/// <b>The day trap.</b> When <em>both</em> day-of-month and day-of-week are restricted, cron
/// matches a day if <em>either</em> matches - not both. <c>0 0 1 * MON</c> fires on the first of
/// the month <em>and</em> on every Monday. This surprises nearly everyone, so
/// <see cref="HasAmbiguousDayRule"/> says when an expression is relying on it.
/// </para>
/// </remarks>
public sealed class VetoCronExpression
{
    private static readonly string[] MonthNames =
        ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

    private static readonly string[] DayNames =
        ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"];

    private VetoCronExpression (
        string text,
        bool includeSeconds,
        VetoCronField? seconds,
        VetoCronField minutes,
        VetoCronField hours,
        VetoCronField daysOfMonth,
        VetoCronField months,
        VetoCronField daysOfWeek )
    {
        Text = text;
        IncludeSeconds = includeSeconds;
        Seconds = seconds;
        Minutes = minutes;
        Hours = hours;
        DaysOfMonth = daysOfMonth;
        Months = months;
        DaysOfWeek = daysOfWeek;
    }

    /// <summary>The expression exactly as it was written.</summary>
    public string Text { get; }

    /// <summary>True when the expression carries a leading seconds field.</summary>
    public bool IncludeSeconds { get; }

    /// <summary>The seconds field, or null when the expression has five fields.</summary>
    public VetoCronField? Seconds { get; }

    /// <summary>The minutes field.</summary>
    public VetoCronField Minutes { get; }

    /// <summary>The hours field.</summary>
    public VetoCronField Hours { get; }

    /// <summary>The day-of-month field.</summary>
    public VetoCronField DaysOfMonth { get; }

    /// <summary>The months field.</summary>
    public VetoCronField Months { get; }

    /// <summary>The day-of-week field.</summary>
    public VetoCronField DaysOfWeek { get; }

    /// <summary>
    /// True when both day fields are restricted, so the expression fires on a day matching
    /// <em>either</em> of them. Worth warning about: it is almost never what the author meant.
    /// </summary>
    public bool HasAmbiguousDayRule => !DaysOfMonth.IsUnrestricted && !DaysOfWeek.IsUnrestricted;

    /// <summary>The three-letter month names the parser accepts.</summary>
    public static IReadOnlyList<string> Months3 => MonthNames;

    /// <summary>The three-letter day names the parser accepts.</summary>
    public static IReadOnlyList<string> Days3 => DayNames;

    /// <summary>Parses a cron expression, saying why rather than throwing when it will not parse.</summary>
    /// <param name="text">The expression.</param>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="error">Why parsing failed, when it did.</param>
    /// <param name="includeSeconds">
    /// Null - the default - accepts five or six fields and decides by counting them. True or
    /// false insists on one shape.
    /// </param>
    public static bool TryParse (
        string? text,
        out VetoCronExpression? expression,
        out string? error,
        bool? includeSeconds = null )
    {
        expression = null;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter a cron expression.";
            return false;
        }

        var fields = text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var withSeconds = includeSeconds ?? fields.Length == 6;
        var expected = withSeconds ? 6 : 5;

        if (fields.Length != expected)
        {
            // When the shape was inferred rather than insisted on, both counts are valid and
            // naming only one of them sends the reader looking for the wrong mistake.
            var wanted = includeSeconds is null ? "5 or 6" : expected.ToString();

            error = fields.Length < expected
                ? $"Only {fields.Length} of {wanted} fields."
                : $"{fields.Length} fields; {wanted} expected.";
            return false;
        }

        var index = 0;
        VetoCronField? seconds = null;

        if (withSeconds && !VetoCronField.TryParse(fields[index++], 0, 59, null, out seconds, out error))
        {
            error = $"Seconds: {error}";
            return false;
        }

        if (!VetoCronField.TryParse(fields[index++], 0, 59, null, out var minutes, out error))
        {
            error = $"Minutes: {error}";
            return false;
        }

        if (!VetoCronField.TryParse(fields[index++], 0, 23, null, out var hours, out error))
        {
            error = $"Hours: {error}";
            return false;
        }

        if (!VetoCronField.TryParse(fields[index++], 1, 31, null, out var daysOfMonth, out error))
        {
            error = $"Day of month: {error}";
            return false;
        }

        if (!VetoCronField.TryParse(fields[index++], 1, 12, MonthNames, out var months, out error))
        {
            error = $"Month: {error}";
            return false;
        }

        if (!VetoCronField.TryParse(fields[index], 0, 6, DayNames, out var daysOfWeek, out error))
        {
            error = $"Day of week: {error}";
            return false;
        }

        expression = new VetoCronExpression(
            string.Join(' ', fields), withSeconds, seconds, minutes!, hours!, daysOfMonth!, months!, daysOfWeek!);

        return true;
    }

    /// <summary>True when the expression fires at this moment.</summary>
    public bool Matches ( DateTime moment )
    {
        if (IncludeSeconds && !Seconds!.Matches(moment.Second))
        {
            return false;
        }

        return Minutes.Matches(moment.Minute)
               && Hours.Matches(moment.Hour)
               && Months.Matches(moment.Month)
               && MatchesDay(moment);
    }

    /// <summary>
    /// True when the day matches. Both day fields restricted means "either", not "both" - the
    /// rule cron has always had and nobody expects.
    /// </summary>
    public bool MatchesDay ( DateTime moment )
    {
        var dayOfMonth = DaysOfMonth.Matches(moment.Day);
        var dayOfWeek = DaysOfWeek.Matches((int)moment.DayOfWeek);

        if (HasAmbiguousDayRule)
        {
            return dayOfMonth || dayOfWeek;
        }

        return dayOfMonth && dayOfWeek;
    }

    /// <inheritdoc />
    public override string ToString () => Text;
}
