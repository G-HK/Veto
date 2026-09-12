using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Layouts.Shell;

/// <summary>
/// The content region of a <see cref="VetoLayout"/>. Offsets itself from the app bar and the
/// drawer, and owns its own scrolling so the frame around it stays put.
/// </summary>
public partial class VetoMainContent : VetoComponentBase, IDisposable
{
    /// <inheritdoc />
    protected override string RootClass => "veto-main-content";

    /// <summary>The page content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The enclosing layout's shared state.</summary>
    [CascadingParameter]
    public VetoLayoutState? Layout { get; set; }

    /// <summary>
    /// Whether this region scrolls rather than the page. True by default, which is what keeps
    /// the app bar and drawer fixed while content moves.
    /// </summary>
    [Parameter]
    public bool Scrollable { get; set; } = true;

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        if (Layout is not null)
        {
            Layout.Changed += OnLayoutChanged;
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
    {
        var hasDrawer = Layout is { HasDrawer: true };
        var drawerOpen = Layout is { HasDrawer: true, DrawerOpen: true };

        return base.StartCssClass()
            .AddClass(Modifier("scrollable"), Scrollable)
            .AddClass(Modifier("appbar"), Layout?.HasAppBar == true)
            // Which side the drawer is on decides which grid column the content takes, so it
            // applies whether or not the drawer happens to be open.
            .AddClass(() => Modifier($"drawer-{Layout!.DrawerAnchor.ToModifier()}"), hasDrawer)
            // The state classes say only what the drawer is doing; whether that changes anything
            // at this viewport width is left to the media queries in CSS, so the drawer and the
            // content never disagree about the breakpoint.
            .AddClass(() => Modifier($"drawer-variant-{Layout!.DrawerVariant.ToModifier()}"), drawerOpen)
            .AddClass(() => Modifier($"drawer-breakpoint-{Layout!.DrawerBreakpoint.ToModifier()}"), drawerOpen);
    }

    private void OnLayoutChanged () => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose ()
    {
        if (Layout is not null)
        {
            Layout.Changed -= OnLayoutChanged;
        }
    }
}
