using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Buttons;

/// <summary>
/// A button whose whole content is an icon - toolbar actions, drawer toggles, close buttons.
/// Renders as an anchor when <see cref="Href"/> is set, so navigation stays real navigation.
/// </summary>
/// <remarks>
/// <see cref="Label"/> is required in practice: with no text to read, the label is the only
/// thing that tells a screen reader user what the control does. It doubles as the tooltip.
/// </remarks>
public partial class VetoIconButton : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-icon-button";

    /// <summary>The icon's SVG markup, normally a constant from <see cref="VetoIcons"/>.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>SVG elements to render instead of <see cref="Icon"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The accessible name, also used as the tooltip. Always supply one - an icon on its own
    /// is announced as nothing.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The button's size. The icon scales with it.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The button's visual treatment. Transparent by default.</summary>
    [Parameter]
    public VetoVariant Variant { get; set; } = VetoVariant.Text;

    /// <summary>The colour role.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Neutral;

    /// <summary>Renders the button as a circle rather than a rounded square.</summary>
    [Parameter]
    public bool Round { get; set; }

    /// <summary>Disables the button. Ignored when <see cref="Href"/> is set.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Marks the button as a toggle and reports its state through <c>aria-pressed</c>. Leave
    /// null for an ordinary button.
    /// </summary>
    [Parameter]
    public bool? Pressed { get; set; }

    /// <summary>The <c>type</c> attribute. Defaults to <c>button</c> so it never submits a form by accident.</summary>
    [Parameter]
    public string Type { get; set; } = "button";

    /// <summary>Navigates here instead of raising <see cref="OnClick"/>, rendering an anchor.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>The link target, when <see cref="Href"/> is set.</summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>Raised when the button is activated.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private VetoIconSize IconSize => Size switch
    {
        VetoSize.Small => VetoIconSize.Medium,
        VetoSize.Large => VetoIconSize.ExtraLarge,
        _ => VetoIconSize.Large
    };

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Variant.ToModifier()))
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier("round"), Round)
            .AddClass(Modifier("disabled"), Disabled);
}