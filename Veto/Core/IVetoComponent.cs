namespace Veto.Core;

/// <summary>
/// The baseline surface every Veto component exposes, regardless of which base
/// class it derives from. Form components derive from <see cref="VetoInputBase{TValue}"/>
/// (which must inherit Blazor's <c>InputBase&lt;T&gt;</c>) and everything else from
/// <see cref="VetoComponentBase"/>; this interface is what keeps the two in step.
/// </summary>
public interface IVetoComponent
{
    /// <summary>Extra CSS classes appended after the component's own classes.</summary>
    string? Class { get; set; }

    /// <summary>Inline styles appended after the component's own.</summary>
    string? Style { get; set; }

    /// <summary>
    /// The root element's <c>id</c>. When not supplied a stable one is generated so
    /// labels and <c>aria-*</c> references always have something to point at.
    /// </summary>
    string? Id { get; set; }

    /// <summary>Rendered as <c>data-testid</c>; the stable hook for end-to-end tests.</summary>
    string? TestId { get; set; }

    /// <summary>When false the component renders nothing at all.</summary>
    bool Visible { get; set; }

    /// <summary>
    /// Surrounds the component with a neon glow that breathes in and out. Off by default:
    /// a glow means "look here", and a page where everything glows says nothing.
    /// </summary>
    bool Glow { get; set; }

    /// <summary>
    /// The glow's colour - any CSS colour. Defaults to the component's own accent where it has
    /// one, and to the theme's accent everywhere else.
    /// </summary>
    string? GlowColor { get; set; }

    /// <summary>
    /// Sends a ring expanding out of the component's border and fading, over and over, to draw
    /// the eye to something that has changed. Off by default.
    /// </summary>
    bool Ping { get; set; }

    /// <summary>The ring's colour. Defaults to the same accent the glow uses.</summary>
    string? PingColor { get; set; }

    /// <summary>Unmatched attributes, splatted onto the root element.</summary>
    IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}
