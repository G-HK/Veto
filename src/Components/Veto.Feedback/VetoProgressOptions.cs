namespace Veto.Components.Feedback;

/// <summary>The shape a <see cref="VetoProgress"/> is drawn as.</summary>
public enum VetoProgressShape
{
    /// <summary>A horizontal bar. The default, and what a page of work in progress wants.</summary>
    Linear,

    /// <summary>
    /// A ring. For a figure that has to sit inside something small and square - a tile, an
    /// avatar, a button - where a bar would either be too short to read or force the layout
    /// wider than it wants to be.
    /// </summary>
    Circle
}