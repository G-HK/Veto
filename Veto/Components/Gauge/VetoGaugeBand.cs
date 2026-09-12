namespace Veto.Components.Gauge;

/// <summary>
/// A coloured stretch of a gauge's dial - the red line on a tachometer, the amber zone before
/// a disk fills up.
/// </summary>
/// <remarks>
/// Bands are drawn on the track, under the value arc, so the reading stays legible over them.
/// They are also what a gauge uses to colour its own reading: when <c>FollowBands</c> is on,
/// the needle and the value take the colour of whichever band the reading currently falls in,
/// so a gauge turns red by itself rather than being told to.
/// </remarks>
/// <param name="From">Where the band starts, on the gauge's own scale.</param>
/// <param name="To">Where it ends.</param>
/// <param name="Color">
/// Any CSS colour. Prefer a Veto token - <c>var(--veto-color-danger)</c> - so the band follows
/// the theme instead of fighting it.
/// </param>
/// <param name="Label">Optional name for the band, used in the gauge's accessible description.</param>
public sealed record VetoGaugeBand ( double From, double To, string Color, string? Label = null );
