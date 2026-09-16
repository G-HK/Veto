using Microsoft.AspNetCore.Components;
using Veto.Core;
using Veto.Layouts.Dock;
using Veto.Layouts.Shell;

namespace Veto.Layouts.DockLayout;

/// <summary>
/// A full-bleed page frame with a floating dock over it - a desktop, rather than a document.
/// Content fills the viewport and the dock sits on top of it.
/// </summary>
/// <remarks>
/// The dock's items are supplied through <see cref="Items"/> rather than as nested children,
/// because the layout owns the dock itself. Compose <see cref="VetoDock"/> directly when you
/// want full control of where it sits.
/// </remarks>
public partial class VetoDockLayout : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-dock-layout";

    /// <summary>The page content, filling the surface beneath the dock.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The dock's items - a sequence of <see cref="VetoDockItem"/>.</summary>
    [Parameter]
    public RenderFragment? Items { get; set; }

    /// <summary>Content painted behind the surface: an image, a gradient, a canvas.</summary>
    [Parameter]
    public RenderFragment? Wallpaper { get; set; }

    /// <summary>Which edge the dock is attached to.</summary>
    [Parameter]
    public VetoAnchor Position { get; set; } = VetoAnchor.Bottom;

    /// <summary>Whether dock items magnify under the pointer or stay a fixed size.</summary>
    [Parameter]
    public VetoDockAppearance Appearance { get; set; } = VetoDockAppearance.Magnified;

    /// <summary>Slides the dock off the edge until the pointer approaches.</summary>
    [Parameter]
    public bool AutoHide { get; set; }

    /// <summary>How much the item under the pointer grows.</summary>
    [Parameter]
    public double Magnification { get; set; } = 1.75;

    /// <summary>The resting size of a dock item.</summary>
    [Parameter]
    public string ItemSize { get; set; } = "3rem";

    /// <summary>The dock's accessible name.</summary>
    [Parameter]
    public string DockLabel { get; set; } = "Dock";

    /// <summary>
    /// Whether the content surface scrolls on its own. True by default, which is what keeps the
    /// dock in place while content moves beneath it.
    /// </summary>
    [Parameter]
    public bool Scrollable { get; set; } = true;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier($"dock-{Position.ToModifier()}"))
            .AddClass(Modifier("scrollable"), Scrollable);
}