using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// A rule that separates content. With a <see cref="Label"/> or child content the rule breaks
/// to make room for it; without one it renders as a plain <c>&lt;hr&gt;</c>.
/// </summary>
public partial class VetoDivider : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-divider";

    /// <summary>
    /// Which way the rule itself runs - horizontal by default, separating stacked content.
    /// A vertical divider needs a parent that gives it a height.
    /// </summary>
    [Parameter]
    public VetoOrientation Orientation { get; set; } = VetoOrientation.Horizontal;

    /// <summary>Text to sit in a gap in the rule.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Content to sit in a gap in the rule. Takes precedence over <see cref="Label"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The orientation as ARIA spells it. <see cref="VetoLayoutExtensions.ToModifier(VetoOrientation)"/>
    /// yields flex direction names, which are not valid <c>aria-orientation</c> values.
    /// </summary>
    private string OrientationName => Orientation.ToString().ToLowerInvariant();

    private bool HasLabel => ChildContent is not null || !string.IsNullOrWhiteSpace(Label);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(OrientationName))
            .AddClass(Modifier("labelled"), HasLabel);

    private RenderFragment RenderLabel () => builder => builder.AddContent(0, Label);
}
