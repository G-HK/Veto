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

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Orientation.ToModifier()))
            .AddClass(Modifier($"gap-{Gap.ToModifier()}"))
            .AddClass(Modifier($"align-{Align.ToModifier()}"))
            .AddClass(Modifier($"justify-{Justify.ToModifier()}"))
            .AddClass(Modifier("wrap"), Wrap)
            .AddClass(Modifier("reverse"), Reverse)
            .AddClass(Modifier("full-height"), FullHeight);
}
