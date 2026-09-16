using System.Globalization;

namespace Veto.Core;

/// <summary>How a component's root element treats content that does not fit.</summary>
public enum VetoOverflow
{
    /// <summary>Content spills out of the box, as it does by default.</summary>
    Visible,

    /// <summary>Content is clipped to the box and cannot be reached.</summary>
    Hidden,

    /// <summary>Scrollbars appear on whichever axis needs one.</summary>
    Auto,

    /// <summary>Scrollbars are always present, whether or not they are needed.</summary>
    Scroll
}

/// <summary>
/// The box-level knobs every Veto component shares: its size, the space around and inside it,
/// how it treats overflow, where it sits, and how it stacks.
/// </summary>
/// <remarks>
/// <para>
/// This exists for the same reason as <see cref="VetoEffects"/> and
/// <see cref="VetoComponentCore"/>: a form component must derive from Blazor's
/// <c>InputBase&lt;T&gt;</c>, so anything both base classes share has to live somewhere
/// neither of them owns. Gathering the knobs into one value also keeps
/// <see cref="VetoComponentCore.BuildStyle"/> to a signature a person can read.
/// </para>
/// <para>
/// Everything here is emitted <em>before</em> the consumer's own <c>Style</c>, so an explicit
/// inline style still wins - and before a component's own stylesheet loses, because these are
/// inline declarations and a stylesheet cannot outrank them. That is the point: they are the
/// escape hatch for the one place a component needs to be a different size or sit above
/// something, without a wrapper element whose only job is to hold a style attribute.
/// </para>
/// </remarks>
internal readonly record struct VetoBox ( string? Width, string? Height, string? MinWidth, string? MaxWidth, string? MinHeight, string? MaxHeight, VetoSpacing? Margin, VetoSpacing? Padding, VetoOverflow? Overflow, string? Top, string? Right, string? Bottom, string? Left, int? ZIndex, bool? Bordered, bool PositionsItself )
{
    /// <summary>True when nothing was set and there is no work to do.</summary>
    public bool IsEmpty =>
        Width is null && Height is null
        && MinWidth is null && MaxWidth is null
        && MinHeight is null && MaxHeight is null
        && Margin is null && Padding is null && Overflow is null
        && !HasOffset && !ZIndex.HasValue && Bordered is null;

    private bool HasOffset => Top is not null || Right is not null || Bottom is not null || Left is not null;

    /// <summary>
    /// Whether the root has to be positioned for what was asked for to mean anything.
    /// </summary>
    /// <remarks>
    /// A <c>z-index</c> or an offset on a statically positioned element does nothing at all -
    /// the kind of silent no-op that costs an afternoon - so asking for either also makes the
    /// root <c>position: relative</c>. Not for a component that positions itself: overwriting
    /// a fixed overlay's own <c>position</c> inline would tear it out of place, which is what
    /// <c>PositionsItself</c> is there to prevent.
    /// </remarks>
    private bool NeedsPositioning => !PositionsItself && (ZIndex.HasValue || HasOffset);

    /// <summary>Adds the declarations for whatever was set, and nothing for what was not.</summary>
    public StyleBuilder Apply ( StyleBuilder builder )
    {
        if (IsEmpty)
        {
            return builder;
        }

        return builder
            .AddStyle("position", "relative", NeedsPositioning)
            .AddStyle("z-index", ZIndex?.ToString(CultureInfo.InvariantCulture), ZIndex.HasValue)
            .AddStyle("width", Length(Width))
            .AddStyle("height", Length(Height))
            .AddStyle("min-width", Length(MinWidth))
            .AddStyle("max-width", Length(MaxWidth))
            .AddStyle("min-height", Length(MinHeight))
            .AddStyle("max-height", Length(MaxHeight))
            .AddStyle("margin", Space(Margin))
            .AddStyle("padding", Space(Padding))
            .AddStyle("overflow", Overflow?.ToCss())
            .AddStyle("top", Length(Top))
            .AddStyle("right", Length(Right))
            .AddStyle("bottom", Length(Bottom))
            .AddStyle("left", Length(Left))

            // Transparent rather than none: the border keeps its width, so turning it off never
            // shifts the content, and the focus and invalid states still have a colour to set.
            // A custom property rather than a border declaration because the border is usually
            // drawn on an element inside the component - custom properties inherit, so one
            // declaration on the root reaches it.
            .AddStyle("--veto-border-color", "transparent", Bordered is false);
    }

    /// <summary>
    /// A spacing step as a CSS length. <see cref="VetoSpacing.None"/> has no token of its own -
    /// the scale starts at <c>--veto-space-1</c> - so it resolves to a plain zero rather than
    /// to a variable that does not exist.
    /// </summary>
    private static string? Space ( VetoSpacing? spacing ) => spacing switch
    {
        null => null,
        VetoSpacing.None => "0",
        _ => $"var(--veto-space-{spacing.Value.ToModifier()})"
    };

    /// <summary>
    /// A length, if it is safe to write into a <c>style</c> attribute. These values come from
    /// app code rather than from end users, but they are still concatenated into one, and a
    /// length has no reason to contain a semicolon or an angle bracket.
    /// </summary>
    private static string? Length ( string? value ) => VetoEffects.SafeColor(value);
}

/// <summary>Turns the box enums into the CSS keywords they stand for.</summary>
public static class VetoBoxExtensions
{
    /// <summary>Returns the CSS keyword for an overflow mode.</summary>
    public static string ToCss ( this VetoOverflow overflow ) => overflow.ToString().ToLowerInvariant();
}