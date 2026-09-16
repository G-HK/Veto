using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Layouts.Shell;

/// <summary>
/// The bar across the top of a <see cref="VetoLayout"/>. Put the app's name, a drawer toggle
/// and any global actions in it; a <see cref="Veto.Components.Layout.VetoSpacer"/> pushes the
/// trailing items to the far end.
/// </summary>
public partial class VetoAppBar : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-appbar";

    /// <summary>The bar's content, laid out in a row.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The enclosing layout's shared state.</summary>
    [CascadingParameter]
    public VetoLayoutState? Layout { get; set; }

    /// <summary>The bar's height, as a CSS length. The content region offsets itself by it.</summary>
    [Parameter]
    public string Height { get; set; } = "3.5rem";

    /// <summary>Reduces the height and padding for dense interfaces.</summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>Whether the bar draws a bottom border. True by default.</summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <summary>Casts a shadow instead of drawing a border.</summary>
    [Parameter]
    public bool Elevated { get; set; }

    /// <summary>
    /// Lets content scroll underneath a translucent, blurred bar. Falls back to a solid
    /// background where <c>backdrop-filter</c> is unsupported.
    /// </summary>
    [Parameter]
    public bool Translucent { get; set; }

    private string ResolvedHeight => Dense ? "3rem" : Height;

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        // Registering on every parameter change is what keeps the content region's offset
        // correct when the height is changed at runtime.
        Layout?.RegisterAppBar(ResolvedHeight);
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("dense"), Dense)
            .AddClass(Modifier("bordered"), Bordered && !Elevated)
            .AddClass(Modifier("elevated"), Elevated)
            .AddClass(Modifier("translucent"), Translucent);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("height", ResolvedHeight);
}