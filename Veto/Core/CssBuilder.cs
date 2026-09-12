namespace Veto.Core;

/// <summary>
/// Accumulates CSS class names and joins them into a single, space-separated
/// class attribute value. Empty and whitespace-only values are skipped, so a
/// component can add optional modifiers without producing stray spaces.
/// </summary>
public struct CssBuilder
{
    private string? _value;

    private CssBuilder ( string? initial ) => _value = initial;

    /// <summary>Starts a builder with no classes.</summary>
    public static CssBuilder Empty () => new(null);

    /// <summary>Starts a builder with the component's root class, e.g. <c>veto-textbox</c>.</summary>
    public static CssBuilder Default ( string? rootClass ) => new(rootClass);

    /// <summary>Appends a class when it has content.</summary>
    public CssBuilder AddClass ( string? value )
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _value = _value is null ? value : $"{_value} {value}";
        }

        return this;
    }

    /// <summary>Appends a class only when <paramref name="when"/> is true.</summary>
    public CssBuilder AddClass ( string? value, bool when ) => when ? AddClass(value) : this;

    /// <summary>Appends the class produced by <paramref name="value"/> only when <paramref name="when"/> is true.</summary>
    public CssBuilder AddClass ( Func<string?> value, bool when ) => when ? AddClass(value()) : this;

    /// <summary>
    /// Appends the <c>class</c> entry from splatted attributes. Consumers who write
    /// <c>class="..."</c> on a Veto component land here rather than overwriting the
    /// component's own classes.
    /// </summary>
    public CssBuilder AddClassFromAttributes ( IReadOnlyDictionary<string, object>? attributes )
        => attributes is not null && attributes.TryGetValue("class", out var c)
            ? AddClass(c?.ToString())
            : this;

    /// <summary>Returns the joined class string, or <c>null</c> when nothing was added.</summary>
    public readonly string? Build () => _value;

    /// <inheritdoc />
    public override readonly string ToString () => Build() ?? string.Empty;
}
