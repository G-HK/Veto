using Microsoft.AspNetCore.Components;
using System.Globalization;
using Veto.Core;

namespace Veto.Components.Feedback;

/// <summary>
/// A horizontal progress bar. Determinate when you can say how far along the work is,
/// indeterminate when you cannot.
/// </summary>
/// <remarks>
/// Prefer a determinate bar wherever a total is known: a bar that fills tells someone how long
/// to wait, where a spinner only tells them to.
/// </remarks>
public partial class VetoProgress : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-progress";

    /// <summary>How far along the work is, from 0 to <see cref="Max"/>.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>The value that counts as finished.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>Animates continuously instead of filling, for work with no known total.</summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>Text above the bar saying what is happening.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Shows the percentage beside the label. Ignored when indeterminate.</summary>
    [Parameter]
    public bool ShowValue { get; set; }

    /// <summary>The bar's thickness.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The colour role.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Primary;

    /// <summary>Rounds the ends of the bar. True by default.</summary>
    [Parameter]
    public bool Rounded { get; set; } = true;

    /// <summary>
    /// The accessible name used when no <see cref="Label"/> is given. A progress bar with no
    /// name is announced as a bare number.
    /// </summary>
    [Parameter]
    public string AccessibleLabel { get; set; } = "Progress";

    /// <summary>Whether the progress is drawn as a bar or a ring.</summary>
    [Parameter]
    public VetoProgressShape Shape { get; set; } = VetoProgressShape.Linear;

    /// <summary>
    /// The ring's stroke width, in the drawing's own units where the ring is 100 across.
    /// Ignored by the linear shape, whose thickness comes from <see cref="Size"/>.
    /// </summary>
    [Parameter]
    public double Thickness { get; set; } = 10;

    /// <summary>
    /// A second value drawn behind the fill and left behind when <see cref="Value"/> drops -
    /// the damage bar of a health meter. Null draws none.
    /// </summary>
    /// <remarks>
    /// It is what makes a hit readable: a bar that simply shortens says the number changed,
    /// where a bar that shortens and leaves a bright remainder says how much was lost. Hold
    /// the old value here and ease it down after a moment.
    /// </remarks>
    [Parameter]
    public double? Trail { get; set; }

    /// <summary>
    /// Divides the bar into notched segments - the pip-by-pip look of a game's health or
    /// experience meter. 1, the default, leaves it continuous.
    /// </summary>
    /// <remarks>
    /// Drawn as notches over a single fill rather than as a segment per element, so the fill
    /// still animates as one box and a part-filled segment is shown part-filled.
    /// </remarks>
    [Parameter]
    public int Segments { get; set; } = 1;

    /// <summary>
    /// Content ridden along the leading edge of the fill - a car on a road, a jet climbing.
    /// Null rides nothing. The linear shape only.
    /// </summary>
    /// <remarks>
    /// Given the component, so the content can read <see cref="Value"/> and
    /// <see cref="Percent"/> and change with it - a jet that raises its nose as it goes, a car
    /// that only shows its brake lights at the end.
    /// </remarks>
    [Parameter]
    public RenderFragment<VetoProgress>? Runner { get; set; }

    /// <summary>
    /// The printed reading, when a percentage is not what the number means. Given the
    /// component, so it can print bytes, items, or a time remaining from <see cref="Value"/>
    /// and <see cref="Max"/>.
    /// </summary>
    [Parameter]
    public RenderFragment<VetoProgress>? ValueContent { get; set; }

    /// <summary>
    /// A .NET format string for the printed reading, applied to the percentage. Ignored when
    /// <see cref="ValueContent"/> is given.
    /// </summary>
    [Parameter]
    public string ValueFormat { get; set; } = "0";

    /// <summary>What follows the printed reading. Set it to an empty string for a bare number.</summary>
    [Parameter]
    public string ValueSuffix { get; set; } = "%";

    /// <summary>
    /// The value as a percentage of <see cref="Max"/>, clamped to 0-100. Public so a
    /// <see cref="Runner"/> or <see cref="ValueContent"/> can read how far along it is.
    /// </summary>
    public double Percent => ToPercent(Value);

    /// <summary>The trail as a percentage of <see cref="Max"/>, clamped to 0-100.</summary>
    public double TrailPercent => Trail is null ? 0 : ToPercent(Trail.Value);

    // NaN and infinities - a division by a zero total upstream, say - read as empty. Clamp
    // passes NaN straight through, which rendered "width: NaN%" and "NaN%".
    private double ToPercent ( double value )
        => Max <= 0 || !double.IsFinite(Max) || !double.IsFinite(value) ? 0 : Math.Clamp(value / Max * 100, 0, 100);

    private string ValueText =>
        Percent.ToString(ValueFormat, CultureInfo.InvariantCulture) + ValueSuffix;

    private string? FillStyle => !Indeterminate
        ? string.Create(CultureInfo.InvariantCulture, $"width: {Percent:0.##}%")
        : null;

    private string? TrailStyle => !Indeterminate
        ? string.Create(CultureInfo.InvariantCulture, $"width: {TrailPercent:0.##}%")
        : null;

    /// <summary>
    /// Positions the runner at the leading edge of the fill. The rail is inset by the runner's
    /// own width so it stops at the end of the bar rather than half-way past it.
    /// </summary>
    private string? RunnerStyle => !Indeterminate
        ? string.Create(CultureInfo.InvariantCulture, $"left: {Percent:0.##}%")
        : null;

    private string NotchStyle =>
        string.Create(CultureInfo.InvariantCulture, $"--veto-progress-segments: {Math.Max(1, Segments)}");

    /// <summary>
    /// The ring's radius, kept inside the drawing so the stroke is not clipped at the edge:
    /// a stroke straddles its path, so half of it sits outside the radius.
    /// </summary>
    private double RingRadius => 50 - Math.Max(1, Thickness) / 2;

    private double RingCircumference => 2 * Math.PI * RingRadius;

    private string RingRadiusText => RingRadius.ToString("0.###", CultureInfo.InvariantCulture);

    private string ThicknessText => Math.Max(1, Thickness).ToString("0.###", CultureInfo.InvariantCulture);

    private string RingCircumferenceText => RingCircumference.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>
    /// How much of the ring to hide. Offsetting the dash is what lets the arc grow smoothly -
    /// an SVG path's <c>d</c> cannot be transitioned, so redrawing it would jump.
    /// </summary>
    private string RingOffsetText ( double percent ) =>
        (RingCircumference * (1 - percent / 100)).ToString("0.###", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier("indeterminate"), Indeterminate)
            .AddClass(Modifier("rounded"), Rounded)
            .AddClass(Modifier("circle"), Shape is VetoProgressShape.Circle)
            .AddClass(Modifier("segmented"), Segments > 1);
}