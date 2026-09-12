using System.Globalization;
using System.Text;

namespace Veto.Components.Cron;

/// <summary>
/// Reads a cron expression back in plain English.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately modest, and deliberately built in: <c>VetoCron</c> has to say something useful
/// the moment it is dropped into an app, and a component library that drags in a NuGet package
/// to render one sentence is a component library nobody can adopt casually.
/// </para>
/// <para>
/// It describes English only. For the two dozen languages
/// <a href="https://github.com/bradymholt/cron-expression-descriptor">CronExpressionDescriptor</a>
/// covers, hand that package's <c>ExpressionDescriptor.GetDescription</c> to
/// <c>VetoCron.Describer</c> - the parameter exists precisely so the better describer can
/// replace this one without the library taking the dependency.
/// </para>
/// </remarks>
public static class VetoCronDescriber
{
    private static readonly string[] MonthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    private static readonly string[] DayNames =
    [
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    ];

    /// <summary>Describes an expression, or returns the parse error when it will not parse.</summary>
    /// <param name="text">The cron expression.</param>
    public static string Describe ( string? text )
        => VetoCronExpression.TryParse(text, out var expression, out var error)
            ? Describe(expression!)
            : error ?? "Not a valid cron expression.";

    /// <summary>Describes a parsed expression.</summary>
    /// <param name="expression">The expression.</param>
    public static string Describe ( VetoCronExpression expression )
    {
        ArgumentNullException.ThrowIfNull(expression);

        var sentence = new StringBuilder();

        sentence.Append(DescribeTime(expression));

        var days = DescribeDays(expression);

        if (days is not null)
        {
            sentence.Append(", ").Append(days);
        }

        var months = DescribeMonths(expression.Months);

        if (months is not null)
        {
            sentence.Append(", ").Append(months);
        }

        var result = sentence.ToString();

        return char.ToUpperInvariant(result[0]) + result[1..];
    }

    private static string DescribeTime ( VetoCronExpression expression )
    {
        var seconds = expression.Seconds;
        var minutes = expression.Minutes;
        var hours = expression.Hours;

        // Every-n cases first: they read as a frequency, not as a time of day.
        if (seconds is not null && Every(seconds) is { } everySecond && minutes.IsUnrestricted && hours.IsUnrestricted)
        {
            return everySecond == 1 ? "every second" : $"every {everySecond} seconds";
        }

        if (Every(minutes) is { } everyMinute && hours.IsUnrestricted && IsZeroOrAbsent(seconds))
        {
            return everyMinute == 1 ? "every minute" : $"every {everyMinute} minutes";
        }

        if (minutes.IsUnrestricted && hours.IsUnrestricted)
        {
            return "every minute";
        }

        if (Every(hours) is { } everyHour && minutes.IsSingle)
        {
            var at = minutes.Values.Single();
            var past = at == 0 ? string.Empty : $" at {at} minutes past the hour";
            return everyHour == 1 ? $"every hour{past}" : $"every {everyHour} hours{past}";
        }

        if (minutes.IsUnrestricted && !hours.IsUnrestricted)
        {
            return $"every minute {DescribeHourWindow(hours)}";
        }

        if (hours.IsUnrestricted && !minutes.IsUnrestricted)
        {
            return $"every hour at {Join(minutes.Values.Order().Select(m => $"{m} minutes past"))}";
        }

        // The ordinary case: a list of clock times.
        var times = new List<string>();

        foreach (var hour in hours.Values.Order())
        {
            foreach (var minute in minutes.Values.Order())
            {
                times.Add(Clock(hour, minute));

                if (times.Count > 12)
                {
                    // Past a dozen, listing them stops being a description.
                    return $"at {Join(minutes.Values.Order().Select(m => $"{m} minutes past"))} {DescribeHourWindow(hours)}";
                }
            }
        }

        return $"at {Join(times)}";
    }

    private static string DescribeHourWindow ( VetoCronField hours )
    {
        if (hours.IsSingle)
        {
            return $"of hour {hours.Values.Single():00}";
        }

        var part = hours.Parts.Count == 1 ? hours.Parts[0] : null;

        if (part is { Kind: VetoCronPartKind.Range })
        {
            return $"between {part.From:00}:00 and {part.To:00}:59";
        }

        return $"of hours {Join(hours.Values.Order().Select(h => h.ToString("00", CultureInfo.InvariantCulture)))}";
    }

    private static string? DescribeDays ( VetoCronExpression expression )
    {
        var dom = expression.DaysOfMonth;
        var dow = expression.DaysOfWeek;

        if (dom.IsUnrestricted && dow.IsUnrestricted)
        {
            return null;
        }

        var parts = new List<string>();

        if (!dom.IsUnrestricted)
        {
            parts.Add(Every(dom) is { } everyDay
                ? $"every {Ordinal(everyDay)} day of the month"
                : $"on the {Join(dom.Values.Order().Select(Ordinal))}");
        }

        if (!dow.IsUnrestricted)
        {
            parts.Add($"only on {Join(dow.Values.Order().Select(d => DayNames[d]))}");
        }

        // The rule that catches everybody: restrict both and cron matches either.
        return expression.HasAmbiguousDayRule
            ? string.Join(" or ", parts)
            : string.Join(", ", parts);
    }

    private static string? DescribeMonths ( VetoCronField months )
    {
        if (months.IsUnrestricted)
        {
            return null;
        }

        return Every(months) is { } every
            ? $"every {Ordinal(every)} month"
            : $"only in {Join(months.Values.Order().Select(m => MonthNames[m - 1]))}";
    }

    /// <summary>
    /// The step, when the field is a single piece covering its whole range - <c>*/n</c>, or a
    /// bare <c>*</c>, which is a step of one over everything and has to count as one here or
    /// <c>0 * * * *</c> describes itself as "every hour at 0 minutes past".
    /// </summary>
    private static int? Every ( VetoCronField field )
        => field.Parts.Count == 1
           && field.Parts[0] is { Kind: VetoCronPartKind.Step or VetoCronPartKind.All } part
           && part.From == field.Min
           && part.To == field.Max
            ? part.Step
            : null;

    private static bool IsZeroOrAbsent ( VetoCronField? field )
        => field is null || (field.IsSingle && field.Values.Single() == 0);

    private static string Clock ( int hour, int minute )
        => $"{hour:00}:{minute:00}";

    private static string Ordinal ( int value )
    {
        var suffix = (value % 100) is >= 11 and <= 13
            ? "th"
            : (value % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

        return $"{value}{suffix}";
    }

    /// <summary>Joins a list the way a person would: "a, b and c".</summary>
    private static string Join ( IEnumerable<string> items )
    {
        var list = items.ToList();

        return list.Count switch
        {
            0 => string.Empty,
            1 => list[0],
            2 => $"{list[0]} and {list[1]}",
            _ => $"{string.Join(", ", list.Take(list.Count - 1))} and {list[^1]}"
        };
    }
}
