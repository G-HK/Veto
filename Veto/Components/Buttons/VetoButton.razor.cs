using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Buttons;

/// <summary>
/// A button with a label, optional icons either side, and a loading state. Renders an anchor
/// when <see cref="Href"/> is set, so navigation stays real navigation - middle-click, open in
/// a new tab and the status bar all keep working.
/// </summary>
/// <remarks>
/// A loading button is disabled while it waits and reports <c>aria-busy</c>, so a second click
/// cannot start the same work twice. Use <see cref="VetoIconButton"/> when the control is an
/// icon on its own.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoButton Variant="VetoVariant.Filled" StartIcon="@VetoIcons.Check" OnClick="Save"&gt;
///     Save changes
/// &lt;/VetoButton&gt;
/// </code>
/// </example>
public partial class VetoButton : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-button";

    /// <summary>The button's label.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>An icon before the label, normally a constant from <see cref="VetoIcons"/>.</summary>
    [Parameter]
    public string? StartIcon { get; set; }

    /// <summary>An icon after the label - a chevron on a disclosure, an arrow on a link.</summary>
    [Parameter]
    public string? EndIcon { get; set; }

    /// <summary>The button's size.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The button's visual treatment.</summary>
    [Parameter]
    public VetoVariant Variant { get; set; } = VetoVariant.Filled;

    /// <summary>The colour role.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Primary;

    /// <summary>Stretches the button to the width of its container.</summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>Rounds the ends fully, rather than to the usual corner radius.</summary>
    [Parameter]
    public bool Rounded { get; set; }

    /// <summary>
    /// Swaps the leading icon for a spinner and disables the button. The label stays put, so the
    /// button does not change width and shift the layout around it.
    /// </summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Disables the button. A disabled button never renders as a link.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// The <c>type</c> attribute. Defaults to <c>button</c> so a button inside a form never
    /// submits it by accident.
    /// </summary>
    [Parameter]
    public string Type { get; set; } = "button";

    /// <summary>Navigates here instead of raising <see cref="OnClick"/>, rendering an anchor.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>The link target, when <see cref="Href"/> is set.</summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>The link's <c>rel</c>, when <see cref="Href"/> is set.</summary>
    [Parameter]
    public string? Rel { get; set; }

    /// <summary>Raised when the button is activated.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Variant.ToModifier()))
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier("full-width"), FullWidth)
            .AddClass(Modifier("rounded"), Rounded)
            .AddClass(Modifier("loading"), Loading)
            .AddClass(Modifier("disabled"), Disabled);

    private async Task OnClickAsync ( MouseEventArgs args )
    {
        // Guarded as well as disabled: a click can still arrive from code, and the work behind a
        // loading button should not be started twice.
        if (Disabled || Loading)
        {
            return;
        }

        await OnClick.InvokeAsync(args);
    }
}