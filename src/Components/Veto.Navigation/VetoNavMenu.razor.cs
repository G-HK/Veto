using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Veto.Core;

namespace Veto.Components.Navigation;

/// <summary>
/// A vertical navigation list, normally the contents of a <c>VetoDrawer</c>. Takes its
/// entries as data rather than markup, so the same navigation can feed a drawer, a dock and a
/// footer without being written out three times.
/// </summary>
/// <remarks>
/// Active entries are matched against the current URL and marked with <c>aria-current="page"</c>,
/// and any group containing the active entry starts expanded - so a deep link opens with the
/// user's place in the tree already visible.
/// </remarks>
public partial class VetoNavMenu : VetoComponentBase, IDisposable
{
    private readonly HashSet<string> _collapsed = [];
    private string _currentPath = string.Empty;

    /// <inheritdoc />
    protected override string RootClass => "veto-nav-menu";

    /// <summary>The entries to render.</summary>
    [Parameter]
    public IReadOnlyList<VetoNavEntry> Items { get; set; } = [];

    /// <summary>Extra content rendered after the list.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The menu's accessible name, which distinguishes it from other nav landmarks.</summary>
    [Parameter]
    public string Label { get; set; } = "Main";

    /// <summary>
    /// Hides the labels and shows icons only, for when the drawer is collapsed to a rail.
    /// Labels remain in the markup as accessible names.
    /// </summary>
    [Parameter]
    public bool IconsOnly { get; set; }

    /// <summary>Reduces the height of each row.</summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>
    /// Gives each link the underline reveal - a thin neon line growing out from the middle of
    /// the label as the row is hovered or focused. Off by default, because a drawer already
    /// marks the current page and a menu full of moving lines is busy.
    /// </summary>
    [Parameter]
    public VetoUnderline Underline { get; set; } = VetoUnderline.None;

    /// <summary>The underline's colour. Defaults to the theme's accent.</summary>
    [Parameter]
    public string? UnderlineColor { get; set; }

    /// <summary>The effect class for the label, or null when the underline is off.</summary>
    private string? UnderlineClass => Underline.ToUnderlineClass();

    /// <summary>
    /// Marks the whole row as what the underline answers to. The line belongs to the label but
    /// the pointer target is the row, padding included.
    /// </summary>
    private string? UnderlineHostClass
        => Underline == VetoUnderline.None ? null : "veto-underline-host";

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        _currentPath = VetoRouteMatch.CurrentPath(Navigation);
        Navigation.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier("icons-only"), IconsOnly)
            .AddClass(Modifier("dense"), Dense);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-underline-color", VetoEffects.SafeColor(UnderlineColor));

    private bool IsActive ( VetoNavEntry entry )
    {
        if (entry.Href is null)
        {
            return false;
        }

        return VetoRouteMatch.IsActive(_currentPath, entry.Href, entry.MatchExact);
    }

    /// <summary>
    /// Groups are expanded unless explicitly collapsed, and a group holding the active entry
    /// cannot be collapsed by accident of navigation - it re-opens so the current page is
    /// always visible in the tree.
    /// </summary>
    private bool IsExpanded ( VetoNavEntry entry )
        => ContainsActive(entry) || !_collapsed.Contains(entry.Label);

    private bool ContainsActive ( VetoNavEntry entry )
        => entry.Children.Any(child => IsActive(child) || ContainsActive(child));

    private void ToggleGroup ( VetoNavEntry entry )
    {
        if (!_collapsed.Remove(entry.Label))
        {
            _collapsed.Add(entry.Label);
        }
    }

    private string? EntryTestId ( VetoNavEntry entry )
        => TestId is null ? null : $"{TestId}-{entry.Label.ToLowerInvariant().Replace(' ', '-')}";

    private void OnLocationChanged ( object? sender, LocationChangedEventArgs args )
    {
        _currentPath = VetoRouteMatch.CurrentPath(Navigation);
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public void Dispose () => Navigation.LocationChanged -= OnLocationChanged;
}
