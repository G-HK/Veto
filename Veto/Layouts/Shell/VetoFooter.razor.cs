using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Layouts.Shell;

/// <summary>The bar across the bottom of a <see cref="VetoLayout"/>.</summary>
public partial class VetoFooter : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-footer";

    /// <inheritdoc />
    /// <remarks>A sticky footer is positioned by its own stylesheet. Letting <c>ZIndex</c> make it relative
    /// would drop it back into the flow.</remarks>
    protected override bool PositionsItself => true;

    /// <summary>The footer's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Keeps the footer visible at the bottom of the viewport as content scrolls.</summary>
    [Parameter]
    public bool Sticky { get; set; }

    /// <summary>Whether the footer draws a top border. True by default.</summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("sticky"), Sticky)
            .AddClass(Modifier("bordered"), Bordered);
}