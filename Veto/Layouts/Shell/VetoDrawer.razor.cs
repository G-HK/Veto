using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Veto.Core;

namespace Veto.Layouts.Shell;

/// <summary>
/// The side panel of a <see cref="VetoLayout"/> - usually a <c>VetoNavMenu</c>. Every
/// behaviour ships and is selected with <see cref="Variant"/>: persistent, temporary, a mini
/// icon rail, or responsive, which switches between them by viewport width.
/// </summary>
/// <remarks>
/// The responsive behaviour is pure CSS. The drawer's open state is the only thing C# tracks,
/// which means there is no viewport-width listener, no layout-shift on boot, and the drawer
/// behaves correctly in prerendered HTML before WebAssembly has started.
/// </remarks>
public partial class VetoDrawer : VetoComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private bool _scrollLocked;
    private bool _checkedInitialWidth;

    /// <inheritdoc />
    protected override string RootClass => "veto-drawer";

    /// <inheritdoc />
    /// <remarks>A temporary or responsive drawer is fixed to the edge of the viewport. Letting <c>ZIndex</c> make it relative
    /// would drop it back into the flow.</remarks>
    protected override bool PositionsItself => true;

    /// <summary>The drawer's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The enclosing layout's shared state.</summary>
    [CascadingParameter]
    public VetoLayoutState? Layout { get; set; }

    /// <summary>Whether the drawer is open. Supports <c>@bind-Open</c>.</summary>
    [Parameter]
    public bool Open { get; set; } = true;

    /// <summary>Raised when the drawer opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>How the drawer behaves. Responsive by default.</summary>
    [Parameter]
    public VetoDrawerVariant Variant { get; set; } = VetoDrawerVariant.Responsive;

    /// <summary>Which edge the drawer is attached to.</summary>
    [Parameter]
    public VetoAnchor Anchor { get; set; } = VetoAnchor.Left;

    /// <summary>The drawer's width when fully open, as a CSS length.</summary>
    [Parameter]
    public string Width { get; set; } = "16rem";

    /// <summary>The drawer's width when collapsed to an icon rail, as a CSS length.</summary>
    [Parameter]
    public string MiniWidth { get; set; } = "4rem";

    /// <summary>
    /// For <see cref="VetoDrawerVariant.Responsive"/>: the width below which the drawer stops
    /// pushing content aside and becomes a temporary overlay.
    /// </summary>
    [Parameter]
    public VetoBreakpoint Breakpoint { get; set; } = VetoBreakpoint.Medium;

    /// <summary>
    /// For <see cref="VetoDrawerVariant.Responsive"/>: when set, the drawer collapses to an
    /// icon rail between this breakpoint and <see cref="Breakpoint"/>, giving the full three
    /// steps of full width, rail, then overlay.
    /// </summary>
    [Parameter]
    public VetoBreakpoint? MiniBreakpoint { get; set; }

    /// <summary>Whether the drawer starts below the app bar or runs the full height.</summary>
    [Parameter]
    public VetoClipMode ClipMode { get; set; } = VetoClipMode.Clipped;

    /// <summary>Expands a mini drawer while the pointer is over it.</summary>
    [Parameter]
    public bool ExpandOnHover { get; set; } = true;

    /// <summary>Whether the drawer draws a border along its inner edge. True by default.</summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <summary>The drawer's accessible name, for when it is a landmark users navigate to.</summary>
    [Parameter]
    public string Label { get; set; } = "Navigation";

    /// <summary>
    /// For <see cref="VetoDrawerVariant.Responsive"/>: whether a drawer that starts open should
    /// close itself on first render when the viewport is below <see cref="Breakpoint"/>. True by
    /// default - otherwise a drawer defaulting to open covers the entire screen on a phone as
    /// soon as the page loads.
    /// </summary>
    [Parameter]
    public bool CloseOnNarrowStart { get; set; } = true;

    /// <summary>
    /// True when the drawer is currently overlaying content rather than sitting beside it.
    /// Temporary always overlays; responsive does so below its breakpoint, which only CSS can
    /// know - so the scrim is rendered for both and CSS decides whether it is visible.
    /// </summary>
    private bool ShowScrim => Variant is VetoDrawerVariant.Temporary or VetoDrawerVariant.Responsive;

    /// <summary>
    /// A closed temporary drawer is hidden from assistive technology; the other variants stay
    /// in the accessibility tree because they remain on screen in some form.
    /// </summary>
    private bool IsHiddenFromAssistiveTech => Variant == VetoDrawerVariant.Temporary && !Open;

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        Layout?.RegisterDrawer(Width, MiniWidth, Anchor, Variant, Breakpoint);

        if (Layout is not null && Layout.DrawerOpen != Open)
        {
            Layout.DrawerOpen = Open;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        if (firstRender)
        {
            await CloseIfStartingNarrowAsync();
        }

        // A temporary drawer overlays the page, so the page behind it must not scroll. Nothing
        // else here needs JavaScript.
        var shouldLock = Variant == VetoDrawerVariant.Temporary && Open;

        if (shouldLock != _scrollLocked)
        {
            _scrollLocked = shouldLock;
            await SetScrollLockAsync(shouldLock);
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Variant.ToModifier()))
            .AddClass(Modifier(Anchor.ToModifier()))
            .AddClass(Modifier(Anchor.IsVertical() ? "side" : "horizontal"))
            .AddClass(Modifier("open"), Open)
            .AddClass(Modifier("closed"), !Open)
            .AddClass(Modifier("bordered"), Bordered)
            .AddClass(Modifier("hover-expand"), ExpandOnHover && Variant == VetoDrawerVariant.Mini)
            .AddClass(Modifier("clipped"), ClipMode == VetoClipMode.Clipped)
            .AddClass(Modifier($"breakpoint-{Breakpoint.ToModifier()}"))
            // Lazy overload: the eager one would evaluate MiniBreakpoint.Value before the
            // condition is checked, and throw whenever no mini breakpoint is set.
            .AddClass(() => Modifier($"mini-breakpoint-{MiniBreakpoint!.Value.ToModifier()}"), MiniBreakpoint.HasValue);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () => base.StartStyle()
            .AddStyle("--veto-drawer-width", Width)
            .AddStyle("--veto-drawer-mini-width", MiniWidth);

    private async Task OnKeyDownAsync ( KeyboardEventArgs args )
    {
        if (args.Key == "Escape" && Open && Variant is VetoDrawerVariant.Temporary or VetoDrawerVariant.Responsive)
        {
            await CloseAsync();
        }
    }

    private async Task CloseAsync ()
    {
        if (!Open)
        {
            return;
        }

        Open = false;

        if (Layout is not null)
        {
            Layout.DrawerOpen = false;
        }

        await OpenChanged.InvokeAsync(false);
    }

    /// <summary>
    /// Closes a responsive drawer that starts open on a viewport narrower than its breakpoint,
    /// where it would be an overlay rather than a docked panel. One measurement at startup; the
    /// stylesheet handles every width change after that.
    /// </summary>
    private async Task CloseIfStartingNarrowAsync ()
    {
        if (_checkedInitialWidth || !CloseOnNarrowStart || !Open || Variant != VetoDrawerVariant.Responsive)
        {
            return;
        }

        _checkedInitialWidth = true;

        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-layout.js");

            if (!await _module.InvokeAsync<bool>("matchesMinWidth", BreakpointPixels))
            {
                await CloseAsync();
                StateHasChanged();
            }
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering or a dead page: leave the drawer as the app configured it.
        }
    }

    private int BreakpointPixels => Breakpoint switch
    {
        VetoBreakpoint.Small => 640,
        VetoBreakpoint.Large => 1280,
        _ => 960
    };

    private async Task SetScrollLockAsync ( bool locked )
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-layout.js");
            await _module.InvokeVoidAsync("setScrollLock", locked);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or the page has gone away. The drawer still works; the page behind
            // it just stays scrollable.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        if (_module is not null)
        {
            try
            {
                if (_scrollLocked)
                {
                    await _module.InvokeVoidAsync("setScrollLock", false);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Nothing to clean up on a connection that is already gone.
            }
        }
    }
}