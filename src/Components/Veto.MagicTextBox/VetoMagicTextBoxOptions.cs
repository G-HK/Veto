namespace Veto.Components.MagicTextBox;

/// <summary>When a <see cref="VetoMagicTextBox{TValue}"/> shows its copy button.</summary>
public enum VetoCopyReveal
{
    /// <summary>
    /// Only while the pointer is over the field, or something inside it has keyboard focus.
    /// The default: the button is an occasional convenience rather than part of the field's
    /// resting appearance. Focus counts as well as hover, so the button is never reachable by
    /// pointer alone.
    /// </summary>
    Hover,

    /// <summary>
    /// Always, whatever the pointer is doing. For a field whose whole point is to be copied -
    /// a generated key, a connection string, a share link - where hiding the button until
    /// hover hides the feature.
    /// </summary>
    Always
}