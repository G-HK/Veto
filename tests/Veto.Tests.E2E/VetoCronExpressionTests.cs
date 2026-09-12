using Veto.Components.Cron;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>
/// Unit tests for the cron parser, describer and scheduler. No browser: this is pure logic,
/// and driving it through the UI would be slower and would find less.
/// </summary>
public class VetoCronExpressionTests
{
    [Theory]
    [InlineData("* * * * *")]
    [InlineData("0 9 * * 1-5")]
    [InlineData("*/15 * * * *")]
    [InlineData("0 0 1 JAN *")]
    [InlineData("0 12 * * SUN")]
    [InlineData("30 9,17 * * MON,WED,FRI")]
    [InlineData("0 0 1,15 * *")]
    [InlineData("0 8-17/2 * * *")]
    public void Accepts_ordinary_expressions ( string text )
    {
        Assert.True(VetoCronExpression.TryParse(text, out var expression, out var error), error);
        Assert.NotNull(expression);
    }

    [Theory]
    [InlineData("", "Enter a cron expression.")]
    [InlineData("* * * *", "Only 4 of 5 or 6 fields.")]
    [InlineData("* * * * * * *", "7 fields; 5 or 6 expected.")]
    public void Rejects_the_wrong_number_of_fields ( string text, string expected )
    {
        Assert.False(VetoCronExpression.TryParse(text, out _, out var error));
        Assert.Equal(expected, error);
    }

    [Theory]
    [InlineData("60 * * * *", "Minutes")]
    [InlineData("* 24 * * *", "Hours")]
    [InlineData("* * 32 * *", "Day of month")]
    [InlineData("* * * 13 *", "Month")]
    [InlineData("* * * * 8", "Day of week")]
    [InlineData("* * * * NOPE", "Day of week")]
    [InlineData("17-5 * * * *", "Minutes")]
    [InlineData("*/0 * * * *", "Minutes")]
    public void Names_the_field_that_is_wrong ( string text, string field )
    {
        Assert.False(VetoCronExpression.TryParse(text, out _, out var error));
        Assert.NotNull(error);
        Assert.StartsWith(field, error);
    }

    [Fact]
    public void Sunday_is_both_zero_and_seven ()
    {
        Assert.True(VetoCronExpression.TryParse("0 0 * * 0", out var zero, out _));
        Assert.True(VetoCronExpression.TryParse("0 0 * * 7", out var seven, out _));

        Assert.Equal(zero!.DaysOfWeek.Values, seven!.DaysOfWeek.Values);
        Assert.Equal([0], zero.DaysOfWeek.Values.Order());
    }

    [Fact]
    public void Names_are_accepted_for_months_and_days ()
    {
        Assert.True(VetoCronExpression.TryParse("0 0 * mar fri", out var expression, out var error), error);

        Assert.Equal([3], expression!.Months.Values.Order());
        Assert.Equal([5], expression.DaysOfWeek.Values.Order());
    }

    [Fact]
    public void A_step_expands_to_the_values_it_selects ()
    {
        Assert.True(VetoCronExpression.TryParse("*/15 * * * *", out var expression, out _));

        Assert.Equal([0, 15, 30, 45], expression!.Minutes.Values.Order());
    }

    [Fact]
    public void A_step_inside_a_range_stays_inside_it ()
    {
        Assert.True(VetoCronExpression.TryParse("0 8-17/2 * * *", out var expression, out _));

        Assert.Equal([8, 10, 12, 14, 16], expression!.Hours.Values.Order());
    }

    [Fact]
    public void A_six_field_expression_is_read_as_carrying_seconds ()
    {
        Assert.True(VetoCronExpression.TryParse("*/30 * * * * *", out var expression, out var error), error);

        Assert.True(expression!.IncludeSeconds);
        Assert.Equal([0, 30], expression.Seconds!.Values.Order());
    }

    [Fact]
    public void The_shape_of_a_field_survives_parsing ()
    {
        // */15 and 0,15,30,45 select the same minutes and deserve different descriptions.
        Assert.True(VetoCronExpression.TryParse("*/15 * * * *", out var stepped, out _));
        Assert.True(VetoCronExpression.TryParse("0,15,30,45 * * * *", out var listed, out _));

        Assert.Equal(stepped!.Minutes.Values.Order(), listed!.Minutes.Values.Order());
        Assert.Equal(VetoCronPartKind.Step, stepped.Minutes.Parts[0].Kind);
        Assert.Equal(4, listed.Minutes.Parts.Count);
    }

    [Fact]
    public void Restricting_both_day_fields_matches_either_of_them ()
    {
        // The rule that catches everybody: this fires on the 1st AND on every Monday.
        Assert.True(VetoCronExpression.TryParse("0 0 1 * MON", out var expression, out _));
        Assert.True(expression!.HasAmbiguousDayRule);

        Assert.True(expression.MatchesDay(new DateTime(2026, 4, 1)));    // a Wednesday, but the 1st
        Assert.True(expression.MatchesDay(new DateTime(2026, 4, 6)));    // a Monday, but not the 1st
        Assert.False(expression.MatchesDay(new DateTime(2026, 4, 7)));   // neither
    }

    [Fact]
    public void Restricting_one_day_field_matches_only_that_one ()
    {
        Assert.True(VetoCronExpression.TryParse("0 0 * * MON", out var expression, out _));
        Assert.False(expression!.HasAmbiguousDayRule);

        Assert.True(expression.MatchesDay(new DateTime(2026, 4, 6)));
        Assert.False(expression.MatchesDay(new DateTime(2026, 4, 7)));
    }

    [Fact]
    public void Finds_the_next_occurrence ()
    {
        Assert.True(VetoCronExpression.TryParse("0 9 * * *", out var expression, out _));

        var next = VetoCronSchedule.Next(expression!, new DateTime(2026, 9, 12, 10, 30, 0));

        Assert.Equal(new DateTime(2026, 9, 13, 9, 0, 0), next);
    }

    [Fact]
    public void The_next_occurrence_is_always_later_than_the_moment_given ()
    {
        Assert.True(VetoCronExpression.TryParse("* * * * *", out var expression, out _));

        var from = new DateTime(2026, 9, 12, 10, 30, 0);
        var next = VetoCronSchedule.Next(expression!, from);

        Assert.Equal(from.AddMinutes(1), next);
    }

    [Fact]
    public void Walks_weekdays_only ()
    {
        Assert.True(VetoCronExpression.TryParse("0 9 * * 1-5", out var expression, out _));

        // Friday 11 September 2026 at 10:00 - the next run is Monday.
        var next = VetoCronSchedule.Next(expression!, new DateTime(2026, 9, 11, 10, 0, 0));

        Assert.Equal(new DateTime(2026, 9, 14, 9, 0, 0), next);
        Assert.Equal(DayOfWeek.Monday, next!.Value.DayOfWeek);
    }

    [Fact]
    public void Skips_whole_months_it_cannot_use ()
    {
        // 29 February only exists in a leap year, so the walker has to cross three of them.
        Assert.True(VetoCronExpression.TryParse("0 0 29 2 *", out var expression, out _));

        var next = VetoCronSchedule.Next(expression!, new DateTime(2026, 3, 1));

        Assert.Equal(new DateTime(2028, 2, 29, 0, 0, 0), next);
    }

    [Fact]
    public void Returns_a_run_of_occurrences_in_order ()
    {
        Assert.True(VetoCronExpression.TryParse("0 9,17 * * *", out var expression, out _));

        var next = VetoCronSchedule.Next(expression!, new DateTime(2026, 9, 12, 8, 0, 0), 4);

        Assert.Equal(
        [
            new DateTime(2026, 9, 12, 9, 0, 0),
            new DateTime(2026, 9, 12, 17, 0, 0),
            new DateTime(2026, 9, 13, 9, 0, 0),
            new DateTime(2026, 9, 13, 17, 0, 0),
        ], next);
    }

    [Fact]
    public void Gives_up_rather_than_hanging_on_an_expression_that_never_fires ()
    {
        // 30 February: parses, and will never happen.
        Assert.True(VetoCronExpression.TryParse("0 0 30 2 *", out var expression, out _));

        Assert.Null(VetoCronSchedule.Next(expression!, new DateTime(2026, 1, 1)));
    }

    [Theory]
    [InlineData("* * * * *", "Every minute")]
    [InlineData("*/5 * * * *", "Every 5 minutes")]
    [InlineData("0 * * * *", "Every hour")]
    [InlineData("0 9 * * *", "At 09:00")]
    [InlineData("30 9 * * *", "At 09:30")]
    [InlineData("0 9 * * 1-5", "At 09:00, only on Monday, Tuesday, Wednesday, Thursday and Friday")]
    [InlineData("0 0 1 * *", "At 00:00, on the 1st")]
    [InlineData("0 0 1 1 *", "At 00:00, on the 1st, only in January")]
    [InlineData("0 9,17 * * *", "At 09:00 and 17:00")]
    public void Describes_an_expression_in_English ( string text, string expected )
    {
        Assert.Equal(expected, VetoCronDescriber.Describe(text));
    }

    [Fact]
    public void Describes_the_either_day_rule_as_an_either ()
    {
        // The description has to warn, because the expression does not mean what it looks like.
        Assert.Contains(" or ", VetoCronDescriber.Describe("0 0 1 * MON"));
    }

    [Fact]
    public void Describing_something_unparseable_returns_the_reason ()
    {
        Assert.Equal("Minutes: '99' is outside 0-59.", VetoCronDescriber.Describe("99 * * * *"));
    }
}
