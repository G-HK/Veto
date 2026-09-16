using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Layouts.Shell;

/// <summary>
/// The root of an app's page frame. Place <see cref="VetoAppBar"/>, <see cref="VetoDrawer"/>,
/// <see cref="VetoMainContent"/> and <see cref="VetoFooter"/> inside it, in any order - the
/// frame positions them, so the markup reads top to bottom rather than being nested to match
/// the visual result.
/// </summary>
/// <example>
/// <code>
/// &lt;VetoLayout&gt;
///     &lt;VetoAppBar&gt;Veto&lt;/VetoAppBar&gt;
///     &lt;VetoDrawer @bind-Open="_open"&gt;&lt;VetoNavMenu Items="Nav" /&gt;&lt;/VetoDrawer&gt;
///     &lt;VetoMainContent&gt;@Body&lt;/VetoMainContent&gt;
/// &lt;/VetoLayout&gt;
/// </code>
/// </example>
public partial class VetoLayout : VetoComponentBase, IDisposable
{
    /// <inheritdoc />
    protected override string RootClass => "veto-layout";

    /// <summary>The shell pieces and page content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Makes the layout fill the viewport exactly, with the content region scrolling instead of
    /// the page. True by default, which is what keeps the app bar and drawer from scrolling away.
    /// </summary>
    [Parameter]
    public bool FillViewport { get; set; } = true;

    /// <summary>
    /// The shared state this layout cascades. Exposed so an app can drive the drawer from
    /// outside the markup - a page can call <c>State.ToggleDrawer()</c> directly.
    /// </summary>
    [Parameter]
    public VetoLayoutState State { get; set; } = new();

    /// <inheritdoc />
    protected override void OnInitialized () => State.Changed += OnStateChanged;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("fill"), FillViewport);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-drawer-width", State.DrawerWidth)
            .AddStyle("--veto-drawer-mini-width", State.DrawerMiniWidth)
            .AddStyle("--veto-appbar-height", State.AppBarHeight);

    private void OnStateChanged () => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose () => State.Changed -= OnStateChanged;
}