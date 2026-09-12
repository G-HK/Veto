using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Layouts.Dashboard;

/// <summary>
/// A responsive grid of <see cref="VetoDashboardTile"/> panels. Tiles reflow by breakpoint, so
/// a wall of widgets on a desktop becomes a single readable column on a phone.
/// </summary>
public partial class VetoDashboard : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-dashboard";

    /// <summary>The tiles.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Columns at every width, unless a larger breakpoint overrides it.</summary>
    [Parameter]
    public int Columns { get; set; } = 1;

    /// <summary>Columns from 960px up.</summary>
    [Parameter]
    public int? ColumnsMedium { get; set; } = 2;

    /// <summary>Columns from 1280px up.</summary>
    [Parameter]
    public int? ColumnsLarge { get; set; } = 4;

    /// <summary>The space between tiles.</summary>
    [Parameter]
    public VetoSpacing Gap { get; set; } = VetoSpacing.Large;

    /// <summary>The height of one grid row, as a CSS length. Tiles span whole rows.</summary>
    [Parameter]
    public string RowHeight { get; set; } = "11rem";

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier($"gap-{Gap.ToModifier()}"));

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-dashboard-columns", Columns.ToString())
            .AddStyle("--veto-dashboard-columns-md", ColumnsMedium?.ToString())
            .AddStyle("--veto-dashboard-columns-lg", ColumnsLarge?.ToString())
            .AddStyle("--veto-dashboard-row-height", RowHeight);
}
