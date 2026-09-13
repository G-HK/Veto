using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Buttons;

/// <summary>
/// A non-interactive label sitting inside a <see cref="VetoButtonGroup"/>.
/// </summary>
/// <remarks>
/// Drawn on a muted background so it reads as a caption rather than as a button nobody can
/// click - which is what it would otherwise be mistaken for. It takes the group's height and
/// joins the same shared border, so it sits in the cluster rather than beside it. Use it to
/// name a section of a toolbar, or to prefix a group with a unit or a field name.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoButtonGroup&gt;
///     &lt;VetoButtonGroupText&gt;Zoom&lt;/VetoButtonGroupText&gt;
///     &lt;VetoButton&gt;50%&lt;/VetoButton&gt;
///     &lt;VetoButton&gt;100%&lt;/VetoButton&gt;
/// &lt;/VetoButtonGroup&gt;
/// </code>
/// </example>
public partial class VetoButtonGroupText : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-button-group__text";

    /// <summary>The label's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// When false the label loses its border and its muted background, becoming plain text
    /// beside the buttons rather than a cap among them.
    /// </summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass().AddClass(Modifier("borderless"), !Bordered);
}