namespace Veto.Layouts.Shell;

/// <summary>
/// Shared state for one <see cref="VetoLayout"/>, cascaded to the pieces inside it.
/// </summary>
/// <remarks>
/// This is what lets the shell pieces coordinate without knowing about each other: the drawer
/// registers its width and variant here, and the main content reads them to offset itself -
/// even though neither is anywhere near the other in the markup.
/// </remarks>
public sealed class VetoLayoutState
{
    private bool _drawerOpen;
    private bool _drawerMini;

    /// <summary>Raised whenever a value changes, so subscribed pieces can re-render.</summary>
    public event Action? Changed;

    /// <summary>Whether a registered drawer is currently open.</summary>
    public bool DrawerOpen
    {
        get => _drawerOpen;
        set => Set(ref _drawerOpen, value);
    }

    /// <summary>Whether the drawer is collapsed to its icon rail.</summary>
    public bool DrawerMini
    {
        get => _drawerMini;
        set => Set(ref _drawerMini, value);
    }

    /// <summary>The registered drawer's full width, as a CSS length. Null when there is no drawer.</summary>
    public string? DrawerWidth { get; private set; }

    /// <summary>The registered drawer's collapsed width, as a CSS length.</summary>
    public string? DrawerMiniWidth { get; private set; }

    /// <summary>Which edge the drawer is attached to.</summary>
    public VetoAnchor DrawerAnchor { get; private set; } = VetoAnchor.Left;

    /// <summary>The registered drawer's variant.</summary>
    public VetoDrawerVariant DrawerVariant { get; private set; } = VetoDrawerVariant.Responsive;

    /// <summary>The width below which the drawer stops pushing content and starts overlaying it.</summary>
    public VetoBreakpoint DrawerBreakpoint { get; private set; } = VetoBreakpoint.Medium;

    /// <summary>True once a drawer has registered itself with this layout.</summary>
    public bool HasDrawer { get; private set; }

    /// <summary>True once an app bar has registered itself with this layout.</summary>
    public bool HasAppBar { get; private set; }

    /// <summary>The app bar's height, as a CSS length.</summary>
    public string? AppBarHeight { get; private set; }

    /// <summary>
    /// Called by <see cref="VetoDrawer"/> as it renders, so the rest of the layout knows what
    /// it is dealing with.
    /// </summary>
    public void RegisterDrawer (
        string width,
        string miniWidth,
        VetoAnchor anchor,
        VetoDrawerVariant variant,
        VetoBreakpoint breakpoint )
    {
        var changed = !HasDrawer
            || DrawerWidth != width
            || DrawerMiniWidth != miniWidth
            || DrawerAnchor != anchor
            || DrawerVariant != variant
            || DrawerBreakpoint != breakpoint;

        HasDrawer = true;
        DrawerWidth = width;
        DrawerMiniWidth = miniWidth;
        DrawerAnchor = anchor;
        DrawerVariant = variant;
        DrawerBreakpoint = breakpoint;

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>Called by <see cref="VetoAppBar"/> as it renders.</summary>
    public void RegisterAppBar ( string height )
    {
        var changed = !HasAppBar || AppBarHeight != height;

        HasAppBar = true;
        AppBarHeight = height;

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>Opens or closes the drawer.</summary>
    public void ToggleDrawer () => DrawerOpen = !DrawerOpen;

    private void Set ( ref bool field, bool value )
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Changed?.Invoke();
    }
}
