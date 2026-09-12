namespace Veto.Components.Slider;

/// <summary>
/// One notch on a slider's rail: where it sits, and what - if anything - is written under it.
/// </summary>
/// <param name="Value">
/// The position on the slider's own scale, between its <c>Min</c> and <c>Max</c>. A mark
/// outside that range is not drawn.
/// </param>
/// <param name="Label">
/// The text under the notch. Leave it null for a bare notch, give it a number to label the
/// scale, or give it a word - <c>"Low"</c>, <c>"Medium"</c>, <c>"High"</c> - when the number
/// itself means nothing to the reader.
/// </param>
/// <example>
/// <code>
/// Marks="[new(0, "Low"), new(50, "Medium"), new(100, "High")]"
/// </code>
/// </example>
public sealed record VetoSliderMark ( double Value, string? Label = null );
