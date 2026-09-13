using System.Globalization;

namespace Veto.Components.Cron;

/// <summary>What shape one comma-separated piece of a cron field takes.</summary>
public enum VetoCronPartKind
{
    /// <summary>A bare <c>*</c> - every value the field allows.</summary>
    All,

    /// <summary>A single value, such as <c>5</c>.</summary>
    Single,

    /// <summary>A closed range, such as <c>9-17</c>.</summary>
    Range,

    /// <summary>A step, such as <c>*/15</c> or <c>9-17/2</c>.</summary>
    Step
}

/// <summary>
/// One comma-separated piece of a cron field, kept in the shape it was written in.
/// </summary>
/// <remarks>
/// The expanded values alone would be enough to decide whether a moment matches, but not to
/// describe the expression: <c>*/15</c> and <c>0,15,30,45</c> select the same four minutes and
/// are worth saying differently. Keeping the shape is what lets the describer read an
/// expression back the way its author wrote it.
/// </remarks>
/// <param name="Kind">The shape this piece takes.</param>
/// <param name="From">The first value, or the start of the range.</param>
/// <param name="To">The last value of a range, otherwise equal to <paramref name="From"/>.</param>
/// <param name="Step">The step, or 1.</param>
public sealed record VetoCronPart ( VetoCronPartKind Kind, int From, int To, int Step );

/// <summary>
/// One field of a cron expression - the minutes, the hours, the days, and so on - parsed into
/// its pieces and expanded into the values it selects.
/// </summary>
public sealed class VetoCronField
{
    private VetoCronField ( string text, int min, int max, IReadOnlyList<VetoCronPart> parts, IReadOnlySet<int> values )
    {
        Text = text;
        Min = min;
        Max = max;
        Parts = parts;
        Values = values;
    }

    /// <summary>The field exactly as it was written.</summary>
    public string Text { get; }

    /// <summary>The lowest value the field allows.</summary>
    public int Min { get; }

    /// <summary>The highest value the field allows.</summary>
    public int Max { get; }

    /// <summary>The comma-separated pieces, in the order they were written.</summary>
    public IReadOnlyList<VetoCronPart> Parts { get; }

    /// <summary>Every value the field selects.</summary>
    public IReadOnlySet<int> Values { get; }

    /// <summary>True when the field is a bare <c>*</c> and so restricts nothing.</summary>
    public bool IsUnrestricted => Parts.Count == 1 && Parts[0].Kind == VetoCronPartKind.All && Parts[0].Step == 1;

    /// <summary>True when the field selects exactly one value.</summary>
    public bool IsSingle => Values.Count == 1;

    /// <summary>True when this moment's value for the field is selected.</summary>
    public bool Matches ( int value ) => Values.Contains(value);

    /// <summary>
    /// Parses one field.
    /// </summary>
    /// <param name="text">The field's text.</param>
    /// <param name="min">The lowest value the field allows.</param>
    /// <param name="max">The highest value the field allows.</param>
    /// <param name="names">
    /// Three-letter names the field accepts in place of numbers - <c>MON</c>, <c>JAN</c> -
    /// indexed from <paramref name="min"/>. Null when the field takes numbers only.
    /// </param>
    /// <param name="field">The parsed field.</param>
    /// <param name="error">Why parsing failed, when it did.</param>
    public static bool TryParse ( string? text, int min, int max, IReadOnlyList<string>? names, out VetoCronField? field, out string? error )
    {
        field = null;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "The field is empty.";
            return false;
        }

        text = text.Trim();

        List<VetoCronPart> parts = [];
        HashSet<int> values = [];

        foreach (var piece in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParsePiece(piece, min, max, names, out var part, out error))
            {
                return false;
            }

            parts.Add(part!);

            for (var value = part!.From; value <= part.To; value += part.Step)
            {
                values.Add(value);
            }
        }

        if (parts.Count == 0)
        {
            error = $"'{text}' selects nothing.";
            return false;
        }

        field = new(text, min, max, parts, values);
        return true;
    }

    private static bool TryParsePiece ( string piece, int min, int max, IReadOnlyList<string>? names, out VetoCronPart? part, out string? error )
    {
        part = null;
        error = null;

        var step = 1;
        var body = piece;

        var slash = piece.IndexOf('/');

        if (slash >= 0)
        {
            body = piece[..slash];
            var stepText = piece[(slash + 1)..];

            if (!int.TryParse(stepText, NumberStyles.Integer, CultureInfo.InvariantCulture, out step) || step < 1)
            {
                error = $"'{stepText}' is not a step of one or more.";
                return false;
            }
        }

        if (body is "*")
        {
            part = new(step == 1 ? VetoCronPartKind.All : VetoCronPartKind.Step, min, max, step);
            return true;
        }

        var dash = body.IndexOf('-');

        if (dash > 0)
        {
            if (!TryValue(body[..dash], min, max, names, out var from, out error) ||
                !TryValue(body[(dash + 1)..], min, max, names, out var to, out error))
            {
                return false;
            }

            if (to < from)
            {
                error = $"'{body}' runs backwards.";
                return false;
            }

            part = new(step == 1 ? VetoCronPartKind.Range : VetoCronPartKind.Step, from, to, step);
            return true;
        }

        if (!TryValue(body, min, max, names, out var single, out error))
        {
            return false;
        }

        // A bare value with a step means "from here to the end of the field, every n" - which
        // is what `5/15` means to cron, however rarely anyone writes it.
        part = step == 1
            ? new(VetoCronPartKind.Single, single, single, 1)
            : new(VetoCronPartKind.Step, single, max, step);

        return true;
    }

    private static bool TryValue ( string text, int min, int max, IReadOnlyList<string>? names, out int value, out string? error )
    {
        error = null;
        text = text.Trim();

        if (names is not null)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], text, StringComparison.OrdinalIgnoreCase))
                {
                    value = min + i;
                    return true;
                }
            }
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = $"'{text}' is not a number{(names is null ? string.Empty : " or a name")} this field accepts.";
            return false;
        }

        // Sunday is both 0 and 7 in every cron anyone has ever shipped.
        if (value == 7 && max == 6 && min == 0)
        {
            value = 0;
            return true;
        }

        if (value < min || value > max)
        {
            error = $"'{value}' is outside {min}-{max}.";
            return false;
        }

        return true;
    }
}