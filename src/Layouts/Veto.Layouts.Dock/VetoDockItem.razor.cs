using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Core;

namespace Veto.Layouts.Dock;

/// <summary>
/// One item in a <see cref="VetoDock"/>. Renders an anchor when <see cref="Href"/> is set and a
/// button otherwise, so navigation stays navigation.
/// </summary>
/// <remarks>
/// The dock is a toolbar: it takes a single tab stop and moves between items with the arrow
/// keys, rather than making someone tab through every icon to get past it.
/// </remarks>
public partial class VetoDockItem : VetoComponentBase, IDisposable
{
    /// <inheritdoc />
    protected override string RootClass => "veto-dock__item";

    /// <summary>The dock this item belongs to.</summary>
    [CascadingParameter]
    public VetoDock? Dock { get; set; }

    /// <summary>The item's icon, normally a constant from <c>VetoIcons</c>.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Custom content in place of <see cref="Icon"/> - an image, an avatar, a logo.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The item's name. Shown as a label on hover and used as the accessible name - an icon on
    /// its own is announced as nothing, so this is not optional in practice.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Short text on a badge - an unread count, a status.</summary>
    [Parameter]
    public string? Badge { get; set; }

    /// <summary>Where the item navigates. Renders an anchor rather than a button.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>The link target, when <see cref="Href"/> is set.</summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>Marks the item as the current one.</summary>
    [Parameter]
    public bool IsActive { get; set; }

    /// <summary>Shows the running dot beneath the item, as a taskbar does for an open app.</summary>
    [Parameter]
    public bool ShowIndicator { get; set; }

    /// <summary>Disables the item. Ignored when <see cref="Href"/> is set.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Raised when the item is activated.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// 0 for the dock's single tab stop, -1 for every other item. Reachable by arrow keys once
    /// the dock has focus.
    /// </summary>
    private int TabIndex => (Dock?.IsTabStop(this) ?? true) ? 0 : -1;

    /// <inheritdoc />
    protected override void OnInitialized () => Dock?.Register(this);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass("veto-dock__item--active", IsActive)
            .AddClass("veto-dock__item--disabled", Disabled);

    /// <inheritdoc />
    public void Dispose () => Dock?.Unregister(this);
}