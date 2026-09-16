using Microsoft.AspNetCore.Components;
using Veto.Components.Navigation;
using Veto.Core;
using Veto.Layouts.Shell;

namespace Veto.Layouts.DocsLayout;

/// <summary>
/// A documentation frame: app bar, navigation drawer, a reading column, and an optional table
/// of contents down the right-hand side.
/// </summary>
/// <remarks>
/// A preset assembled from the shell pieces. The table of contents is hidden below 1280px
/// rather than stacked, because a contents list above the content it describes is noise.
/// </remarks>
public partial class VetoDocsLayout : VetoComponentBase
{
    private readonly VetoLayoutState _layout = new();
    private bool _drawerOpen = true;

    /// <inheritdoc />
    protected override string RootClass => "veto-docs-layout";

    /// <summary>The page content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The product name or logo, at the leading end of the app bar.</summary>
    [Parameter]
    public RenderFragment? Brand { get; set; }

    /// <summary>Controls at the trailing end of the app bar - search, theme, links.</summary>
    [Parameter]
    public RenderFragment? Toolbar { get; set; }

    /// <summary>The table of contents for the current page.</summary>
    [Parameter]
    public RenderFragment? Toc { get; set; }

    /// <summary>Footer content. No footer is rendered without it.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>The navigation entries.</summary>
    [Parameter]
    public IReadOnlyList<VetoNavEntry> Items { get; set; } = [];

    /// <summary>The navigation drawer's width.</summary>
    [Parameter]
    public string DrawerWidth { get; set; } = "15rem";

    /// <summary>Whether to reserve a column for the table of contents.</summary>
    [Parameter]
    public bool ShowToc { get; set; } = true;

    private string? Sub ( string suffix ) => TestId is null ? null : $"{TestId}-{suffix}";

    private void ToggleDrawer () => _drawerOpen = !_drawerOpen;
}
