using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Layouts.Dashboard;

/// <summary>
/// One panel in a <see cref="VetoDashboard"/>, spanning a number of columns and rows.
/// </summary>
public partial class VetoDashboardTile : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-dashboard-tile";

    /// <summary>The tile's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Controls shown in the tile's header - a menu, a filter, a refresh button.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>The tile's heading, which is also its accessible name.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>An icon beside the heading.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Columns the tile spans at every width, unless a larger breakpoint overrides it.</summary>
    [Parameter]
    public int ColumnSpan { get; set; } = 1;

    /// <summary>Columns the tile spans from 960px up.</summary>
    [Parameter]
    public int? ColumnSpanMedium { get; set; }

    /// <summary>Columns the tile spans from 1280px up.</summary>
    [Parameter]
    public int? ColumnSpanLarge { get; set; }

    /// <summary>Rows the tile spans, for a panel that needs to be taller than its neighbours.</summary>
    [Parameter]
    public int RowSpan { get; set; } = 1;

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-tile-span", ColumnSpan.ToString())
            .AddStyle("--veto-tile-span-md", ColumnSpanMedium?.ToString())
            .AddStyle("--veto-tile-span-lg", ColumnSpanLarge?.ToString())
            .AddStyle("--veto-tile-rows", RowSpan.ToString());
}
