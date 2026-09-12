namespace Veto.Components.Cron;

/// <summary>
/// Works out when a <see cref="VetoCronExpression"/> will actually fire.
/// </summary>
/// <remarks>
/// Walks the calendar by the largest unit that can be ruled out at once - a month that is not
/// selected is skipped whole, then a day, then an hour - rather than testing every minute in
/// turn. Testing minute by minute would be simpler and would take a couple of million
/// iterations to find the next 29 February.
/// </remarks>
public static class VetoCronSchedule
{
    /// <summary>How far ahead the walker will look before giving up, in years.</summary>
    private const int HorizonYears = 8;

    /// <summary>The next moment the expression fires, strictly after <paramref name="after"/>.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="after">The moment to search from. The result is always later than this.</param>
    /// <returns>The next occurrence, or null when there is none inside the search horizon.</returns>
    public static DateTime? Next ( VetoCronExpression expression, DateTime after )
    {
        ArgumentNullException.ThrowIfNull(expression);

        var step = expression.IncludeSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);
        var limit = after.AddYears(HorizonYears);

        // Truncate to the unit we step in, then move one on, so "next" is never "now".
        var moment = expression.IncludeSeconds
            ? new DateTime(after.Year, after.Month, after.Day, after.Hour, after.Minute, after.Second, after.Kind)
            : new DateTime(after.Year, after.Month, after.Day, after.Hour, after.Minute, 0, after.Kind);

        moment = moment.Add(step);

        while (moment <= limit)
        {
            if (!expression.Months.Matches(moment.Month))
            {
                // Skip the whole month.
                moment = new DateTime(moment.Year, moment.Month, 1, 0, 0, 0, moment.Kind).AddMonths(1);
                continue;
            }

            if (!expression.MatchesDay(moment))
            {
                moment = moment.Date.AddDays(1);
                continue;
            }

            if (!expression.Hours.Matches(moment.Hour))
            {
                moment = moment.Date.AddHours(moment.Hour + 1);
                continue;
            }

            if (!expression.Minutes.Matches(moment.Minute))
            {
                moment = moment.AddMinutes(1).AddSeconds(-moment.Second);
                continue;
            }

            if (expression.IncludeSeconds && !expression.Seconds!.Matches(moment.Second))
            {
                moment = moment.AddSeconds(1);
                continue;
            }

            return moment;
        }

        return null;
    }

    /// <summary>The next few moments the expression fires.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="after">The moment to search from.</param>
    /// <param name="count">How many occurrences to return.</param>
    public static IReadOnlyList<DateTime> Next ( VetoCronExpression expression, DateTime after, int count )
    {
        ArgumentNullException.ThrowIfNull(expression);

        var results = new List<DateTime>();
        var moment = after;

        for (var i = 0; i < count; i++)
        {
            var next = Next(expression, moment);

            if (next is null)
            {
                break;
            }

            results.Add(next.Value);
            moment = next.Value;
        }

        return results;
    }
}
