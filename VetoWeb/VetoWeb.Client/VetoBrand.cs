namespace VetoWeb.Client;

/// <summary>
/// The documentation site's own brand marks. Site branding rather than library API, which is
/// why it lives here and not in <c>Veto.Components.Icon.VetoIcons</c>.
/// </summary>
/// <remarks>
/// The mark is drawn on the same 24x24 grid as every icon the library ships, with the same
/// round caps and joins, so it sits beside them without looking imported - and because it is a
/// stroke rather than a letter, it is identical at every size and does not wait on Space
/// Grotesk to load. The same path is in <c>assets/veto-icon.svg</c> and in the favicon; keep
/// the three in step.
/// </remarks>
public static class VetoBrand
{
    /// <summary>The V, as the inner markup of a 24x24 SVG. Drawn heavier than an ordinary icon.</summary>
    public const string Mark = "<path d=\"M6.5 7.75 12 16.75 17.5 7.75\" />";

    /// <summary>The stroke width the mark is drawn at, against the icon set's default of 2.</summary>
    public const double MarkStrokeWidth = 3.4;
}
