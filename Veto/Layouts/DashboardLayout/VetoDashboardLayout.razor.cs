using Microsoft.AspNetCore.Components;
using Veto.Components.Navigation;
using Veto.Core;
using Veto.Layouts.Shell;

namespace Veto.Layouts.DashboardLayout;

/// <summary>
/// An admin frame: app bar, responsive navigation drawer and a content region sized for a
/// <c>VetoDashboard</c> of tiles.
/// </summary>
/// <remarks>
/// A preset assembled from the shell pieces. Compose <see cref="VetoLayout"/> yourself when you
/// need something this does not expose - the preset is a shortcut, not the only way in.
/// </remarks>
public partial class VetoDashboardLayout : VetoComponentBase
{
    private readonly VetoLayoutState _layout = new();
    private bool _drawerOpen = true;

    /// <inheritdoc />
    protected override string RootClass => "veto-dashboard-layout";

    /// <summary>The page content - usually a <c>VetoDashboard</c>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Controls at the trailing end of the app bar.</summary>
    [Parameter]
    public RenderFragment? Toolbar { get; set; }

    /// <summary>Content pinned below the navigation - an account switcher, a version string.</summary>
    [Parameter]
    public RenderFragment? DrawerFooter { get; set; }

    /// <summary>The navigation entries.</summary>
    [Parameter]
    public IReadOnlyList<VetoNavEntry> Items { get; set; } = [];

    /// <summary>The app or section name shown in the app bar.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The navigation drawer's width.</summary>
    [Parameter]
    public string DrawerWidth { get; set; } = "15rem";

    private string? Sub ( string suffix ) => TestId is null ? null : $"{TestId}-{suffix}";

    private void ToggleDrawer () => _drawerOpen = !_drawerOpen;
}
