using System.Text.RegularExpressions;

namespace Veto.Core;

/// <summary>
/// The opt-in visual effects every Veto component shares: a breathing neon glow, and a ping
/// ring that expands out of the component's border and fades, to draw the eye to something.
/// </summary>
/// <remarks>
/// <para>
/// This is the one implementation both base classes use, in the same way and for the same
/// reason as <see cref="VetoComponentCore"/>: a form component cannot derive from
/// <see cref="VetoComponentBase"/>, so anything shared has to live somewhere neither of them
/// owns.
/// </para>
/// <para>
/// Both effects are drawn on a pseudo-element of the root, which means the root has to be able
/// to have one. Every Veto component's root is an ordinary element, but a bare
/// <c>&lt;input&gt;</c> or <c>&lt;img&gt;</c> cannot host a pseudo-element - which is why
/// <c>VetoTextBox</c> carries the effect on its wrapper rather than on the field itself.
/// </para>
/// </remarks>
internal readonly partial record struct VetoEffects ( bool Glow, string? GlowColor, bool Ping, string? PingColor )
{
    /// <summary>True when neither effect is on and there is nothing to add.</summary>
    public bool IsEmpty => !Glow && !Ping;

    /// <summary>Adds the effect classes, before the consumer's own so theirs still win.</summary>
    public CssBuilder Apply ( CssBuilder builder ) =>
        builder
            .AddClass("veto-glow", Glow)
            .AddClass("veto-ping", Ping);

    /// <summary>
    /// Adds the colour overrides as custom properties. Nothing is added when a colour was not
    /// given, so the stylesheet's fallback to the theme accent stays in force.
    /// </summary>
    public StyleBuilder Apply ( StyleBuilder builder ) =>
        builder
            .AddStyle("--veto-glow-color", SafeColor(GlowColor), Glow)
            .AddStyle("--veto-ping-color", SafeColor(PingColor), Ping);

    /// <summary>
    /// Returns the colour if it is safe to write into a <c>style</c> attribute, otherwise null.
    /// Rejects anything that could end the declaration it is written into and start another.
    /// These values come from app code rather than from end users, but they are still
    /// concatenated into a <c>style</c> attribute, and a colour has no reason to contain a
    /// semicolon or an angle bracket.
    /// </summary>
    internal static string? SafeColor ( string? value ) =>
        !string.IsNullOrWhiteSpace(value)
            && SafeValue().IsMatch(value)
                ? value
                : null;

    [GeneratedRegex(@"^[^<>{};@\\""]{1,120}$")]
    private static partial Regex SafeValue ();
}