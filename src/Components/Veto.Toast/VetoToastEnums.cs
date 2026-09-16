namespace Veto.Components.Toast;

/// <summary>Where a toast stack sits on the screen.</summary>
public enum VetoToastPosition
{
    /// <summary>Top left corner.</summary>
    TopLeft,

    /// <summary>Top centre.</summary>
    TopCenter,

    /// <summary>Top right corner. The default - out of the way of most page content.</summary>
    TopRight,

    /// <summary>Bottom left corner.</summary>
    BottomLeft,

    /// <summary>Bottom centre.</summary>
    BottomCenter,

    /// <summary>Bottom right corner.</summary>
    BottomRight
}

/// <summary>
/// What a toast is telling the user. Decides its colour, its icon and the ARIA politeness it
/// is announced with.
/// </summary>
public enum VetoToastType
{
    /// <summary>No accent - inherits the surface and text tokens. For plain status updates.</summary>
    Default,

    /// <summary>Something happened that the user may want to know.</summary>
    Info,

    /// <summary>Something worked.</summary>
    Success,

    /// <summary>Something needs attention but nothing failed.</summary>
    Warning,

    /// <summary>Something failed. Announced assertively rather than politely.</summary>
    Error
}

/// <summary>
/// How a toast arrives and leaves. The same value drives both directions - the in animation is
/// played on arrival and its mirror on dismissal - so a toast never enters one way and leaves
/// another by accident.
/// </summary>
public enum VetoToastAnimation
{
    /// <summary>No animation. What reduced-motion users get regardless of this setting.</summary>
    None,

    /// <summary>Fades in, fades out.</summary>
    Fade,

    /// <summary>Grows from the middle, shrinks back into it.</summary>
    Zoom,

    /// <summary>Slides in from the left edge and leaves the same way.</summary>
    SlideLeft,

    /// <summary>Slides in from the right edge and leaves the same way.</summary>
    SlideRight,

    /// <summary>Slides down from above and leaves upwards.</summary>
    SlideDown,

    /// <summary>Slides up from below and leaves downwards.</summary>
    SlideUp,

    /// <summary>Flips about the horizontal axis, top edge tipping towards you.</summary>
    FlipXDown,

    /// <summary>Flips about the horizontal axis, bottom edge tipping towards you.</summary>
    FlipXUp,

    /// <summary>Flips about the vertical axis, left edge swinging towards you.</summary>
    FlipYLeft,

    /// <summary>Flips about the vertical axis, right edge swinging towards you.</summary>
    FlipYRight
}

/// <summary>What happens when a toast is shown whose message is already on screen.</summary>
public enum VetoToastDuplicates
{
    /// <summary>Show it anyway. The default.</summary>
    Allow,

    /// <summary>
    /// Drop the new one and leave the existing toast alone. Use when a repeated message means
    /// nothing new happened.
    /// </summary>
    Block,

    /// <summary>
    /// Drop the new one but restart the existing toast's timer and raise its count, so the user
    /// sees that it happened again. Use when the repeat itself is the information.
    /// </summary>
    Replace
}
