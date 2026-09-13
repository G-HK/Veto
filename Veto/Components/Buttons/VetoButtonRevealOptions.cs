namespace Veto.Components.Buttons;

/// <summary>Which way a <see cref="VetoButtonReveal"/> unfolds its actions.</summary>
public enum VetoRevealDirection
{
    /// <summary>Out to the trailing side. The default, and the one a toolbar wants.</summary>
    End,

    /// <summary>Out to the leading side, for a trigger near the trailing edge of its row.</summary>
    Start,

    /// <summary>Upwards, for a trigger near the bottom of the page.</summary>
    Up,

    /// <summary>Downwards.</summary>
    Down
}

/// <summary>What reveals the actions.</summary>
public enum VetoRevealTrigger
{
    /// <summary>
    /// The pointer arriving on the cluster. Keyboard focus reveals them too, so the actions are
    /// never reachable by pointer alone.
    /// </summary>
    Hover,

    /// <summary>A click or Enter on the trigger. For a cluster that should not open in passing.</summary>
    Click
}

/// <summary>Turns the reveal enums into the modifier suffixes their CSS uses.</summary>
public static class VetoButtonRevealExtensions
{
    /// <summary>Returns the modifier suffix for a direction, e.g. <c>end</c>.</summary>
    public static string ToModifier ( this VetoRevealDirection direction ) =>
        direction.ToString().ToLowerInvariant();

    /// <summary>True when the cluster unfolds along the inline axis rather than the block one.</summary>
    internal static bool IsInline ( this VetoRevealDirection direction ) =>
        direction is VetoRevealDirection.End or VetoRevealDirection.Start;
}