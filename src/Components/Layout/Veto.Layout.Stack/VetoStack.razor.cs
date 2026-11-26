using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// Lays children out in a row or a column with consistent spacing. The most-used layout
/// component in the library - reach for it instead of writing a flex container by hand.
/// </summary>
/// <example>
/// <code>
/// &lt;VetoStack Orientation="VetoOrientation.Horizontal"
///            Gap="VetoSpacing.Medium"
///            Align="VetoAlign.Center"&gt;
///     &lt;VetoThemeToggle /&gt;
///     &lt;span&gt;Appearance&lt;/span&gt;
/// &lt;/VetoStack&gt;
/// </code>
/// </example>
public partial class VetoStack : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-stack";

    /// <summary>The content to lay out.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The axis children are laid out on. Vertical by default.</summary>
    [Parameter]
    public VetoOrientation Orientation { get; set; } = VetoOrientation.Vertical;

    /// <summary>The space between children.</summary>
    [Parameter]
    public VetoSpacing Gap { get; set; } = VetoSpacing.Medium;

    /// <summary>Alignment across the layout axis.</summary>
    [Parameter]
    public VetoAlign Align { get; set; } = VetoAlign.Stretch;

    /// <summary>Distribution along the layout axis.</summary>
    [Parameter]
    public VetoJustify Justify { get; set; } = VetoJustify.Start;

    /// <summary>Whether children wrap onto further lines when they run out of room.</summary>
    [Parameter]
    public bool Wrap { get; set; }

    /// <summary>Reverses the order children are laid out in, without reordering the markup.</summary>
    [Parameter]
    public bool Reverse { get; set; }

    /// <summary>
    /// Makes the stack fill the height available to it. Useful when it is the direct child
    /// of a layout region that already has a height.
    /// </summary>
    [Parameter]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Draws a line between each pair of children, centred in the gap. Follows the
    /// orientation: horizontal lines in a column, vertical lines in a row.
    /// </summary>
    [Parameter]
    public bool Dividers { get; set; }

    /// <summary>The divider's colour, as any CSS colour. The theme's border colour when null.</summary>
    [Parameter]
    public string? DividerColor { get; set; }

    /// <summary>The divider's thickness, as a CSS length. 1px when null.</summary>
    [Parameter]
    public string? DividerThickness { get; set; }

    /// <summary>Solid, dashed or dotted. Solid by default.</summary>
    [Parameter]
    public VetoLineStyle DividerStyle { get; set; } = VetoLineStyle.Solid;

    /// <summary>
    /// How far each divider stops short of both ends, as a CSS length or percentage - "1rem",
    /// "15%". Full length when null. An inset line is always solid: <see cref="DividerStyle"/>
    /// is ignored while this is set.
    /// </summary>
    [Parameter]
    public string? DividerInset { get; set; }

    /// <summary>
    /// Where an inset divider sits: at the start (left in a column, top in a row), centred, or at
    /// the end. The line keeps the same length - <see cref="DividerInset"/> trimmed from both
    /// ends - and only moves. Centred by default; ignored without an inset.
    /// </summary>
    [Parameter]
    public VetoLineAlign DividerAlign { get; set; } = VetoLineAlign.Center;

    private bool Inset => Dividers && !string.IsNullOrWhiteSpace(DividerInset);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-stack-divider-color", DividerColor, Dividers)
            .AddStyle("--veto-stack-divider-thickness", DividerThickness, Dividers)
            .AddStyle("--veto-stack-divider-style", DividerStyle.ToString().ToLowerInvariant(), Dividers && DividerStyle != VetoLineStyle.Solid)
            .AddStyle("--veto-stack-divider-inset", DividerInset, Inset);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Orientation.ToModifier()))
            .AddClass(Modifier($"gap-{Gap.ToModifier()}"))
            .AddClass(Modifier($"align-{Align.ToModifier()}"))
            .AddClass(Modifier($"justify-{Justify.ToModifier()}"))
            .AddClass(Modifier("wrap"), Wrap)
            .AddClass(Modifier("reverse"), Reverse)
            .AddClass(Modifier("full-height"), FullHeight)
            .AddClass(Modifier("divided"), Dividers)
            .AddClass(Modifier("inset"), Inset)
            .AddClass(Modifier($"divider-{DividerAlign.ToString().ToLowerInvariant()}"), Inset);
}

/// <summary>How a line is drawn.</summary>
public enum VetoLineStyle
{
    /// <summary>A continuous line.</summary>
    Solid,

    /// <summary>A line of short dashes.</summary>
    Dashed,

    /// <summary>A line of dots.</summary>
    Dotted
}
/// <summary>Where a shortened line sits along its own length.</summary>
public enum VetoLineAlign
{
    /// <summary>Against the start: the left of a horizontal line, the top of a vertical one.</summary>
    Start,

    /// <summary>In the middle.</summary>
    Center,

    /// <summary>Against the end: the right of a horizontal line, the bottom of a vertical one.</summary>
    End
}