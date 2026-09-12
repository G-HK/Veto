namespace Veto.Core;

/// <summary>
/// Accumulates inline style declarations and joins them into a single
/// <c>style</c> attribute value. Mirrors <see cref="CssBuilder"/>; prefer CSS
/// classes and design tokens over inline styles wherever possible.
/// </summary>
public struct StyleBuilder
{
    private string? _value;

    private StyleBuilder ( string? initial ) => _value = initial;

    /// <summary>Starts a builder with no declarations.</summary>
    public static StyleBuilder Empty () => new(null);

    /// <summary>Starts a builder with an existing style string.</summary>
    public static StyleBuilder Default ( string? initial ) => new(NormalizeChunk(initial));

    /// <summary>Appends a <c>property: value</c> declaration.</summary>
    public StyleBuilder AddStyle ( string property, string? value )
        => string.IsNullOrWhiteSpace(value) ? this : AddRaw($"{property}: {value}");

    /// <summary>Appends a <c>property: value</c> declaration only when <paramref name="when"/> is true.</summary>
    public StyleBuilder AddStyle ( string property, string? value, bool when )
        => when ? AddStyle(property, value) : this;

    /// <summary>Appends a raw declaration list such as <c>"color: red; width: 2rem"</c>.</summary>
    public StyleBuilder AddRaw ( string? value )
    {
        var chunk = NormalizeChunk(value);
        if (chunk is not null)
        {
            _value = _value is null ? chunk : $"{_value}; {chunk}";
        }

        return this;
    }

    /// <summary>Appends the <c>style</c> entry from splatted attributes.</summary>
    public StyleBuilder AddStyleFromAttributes ( IReadOnlyDictionary<string, object>? attributes )
        => attributes is not null && attributes.TryGetValue("style", out var s)
            ? AddRaw(s?.ToString())
            : this;

    /// <summary>Returns the joined style string, or <c>null</c> when nothing was added.</summary>
    public readonly string? Build () => _value;

    /// <inheritdoc />
    public override readonly string ToString () => Build() ?? string.Empty;

    private static string? NormalizeChunk ( string? value )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().TrimEnd(';').Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
