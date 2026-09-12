using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// The header of a composed <see cref="VetoCard"/> - normally a <see cref="VetoCardTitle"/> and
/// a <see cref="VetoCardDescription"/>, and whatever else belongs beside them.
/// </summary>
/// <remarks>
/// The order of the title and the description is whatever you write. Title first reads as a
/// heading with a subtitle, which is what an article or a product card wants. Description first
/// puts the label above the figure, which is what a KPI tile wants - there is no parameter for
/// it because writing the two children in the other order already says it.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoCard&gt;
///     &lt;VetoCardHeader&gt;
///         &lt;VetoCardTitle&gt;Monthly revenue&lt;/VetoCardTitle&gt;
///         &lt;VetoCardDescription&gt;Compared with the same month last year&lt;/VetoCardDescription&gt;
///     &lt;/VetoCardHeader&gt;
///     &lt;VetoCardContent&gt;...&lt;/VetoCardContent&gt;
///     &lt;VetoCardFooter&gt;&lt;VetoButton&gt;Export&lt;/VetoButton&gt;&lt;/VetoCardFooter&gt;
/// &lt;/VetoCard&gt;
/// </code>
/// </example>
public partial class VetoCardHeader : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-card-header";

    /// <summary>The header's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>When true a hairline runs under the header, separating it from the content.</summary>
    [Parameter]
    public bool Bordered { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass("veto-card-section")
            .AddClass(Modifier("bordered"), Bordered);
}

/// <summary>The main content of a composed <see cref="VetoCard"/>.</summary>
public partial class VetoCardContent : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-card-content";

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () => base.StartCssClass().AddClass("veto-card-section");
}

/// <summary>
/// The footer of a composed <see cref="VetoCard"/> - typically the card's actions.
/// </summary>
/// <remarks>
/// Lays its children out in a row with a gap, since a footer almost always holds buttons. Set
/// <see cref="Justify"/> to push them to the end, or to split them to both edges.
/// </remarks>
public partial class VetoCardFooter : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-card-footer";

    /// <summary>The footer's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>How the footer's children are distributed along the row.</summary>
    [Parameter]
    public VetoJustify Justify { get; set; } = VetoJustify.Start;

    /// <summary>When true a hairline runs above the footer, separating it from the content.</summary>
    [Parameter]
    public bool Bordered { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass("veto-card-section")
            .AddClass(Modifier(Justify.ToModifier()))
            .AddClass(Modifier("bordered"), Bordered);
}

/// <summary>
/// The title inside a <see cref="VetoCardHeader"/>.
/// </summary>
/// <remarks>
/// Renders an <c>&lt;h3&gt;</c> by default. <see cref="Level"/> changes that, and zero renders a
/// plain <c>&lt;div&gt;</c> - which is what a grid of KPI tiles wants, where the big number is a
/// figure rather than an entry in the page's outline.
/// </remarks>
public partial class VetoCardTitle : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-card-title";

    /// <summary>The title's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Heading level 1-6, or 0 for a plain <c>div</c> that is not a heading at all.</summary>
    [Parameter]
    public int Level { get; set; } = 3;

    /// <summary>
    /// When true the title is rendered at display size, for the figure on a statistic tile.
    /// </summary>
    [Parameter]
    public bool Display { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass().AddClass(Modifier("display"), Display);
}

/// <summary>
/// The supporting line inside a <see cref="VetoCardHeader"/> - muted, one size down from the
/// title. Put it after the title for a subtitle, or before it for a label above a figure.
/// </summary>
public partial class VetoCardDescription : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-card-description";

    /// <summary>The description's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
