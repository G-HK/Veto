using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// A cell in a <see cref="VetoGrid"/> that spans more than one column. Only needed for cells
/// that are wider than one column - ordinary children can be placed in the grid directly.
/// </summary>
public partial class VetoGridItem : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-grid-item";

    /// <summary>The cell's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Columns to span at every width, unless a larger breakpoint overrides it.</summary>
    [Parameter]
    public int Span { get; set; } = 1;

    /// <summary>Columns to span from 640px up.</summary>
    [Parameter]
    public int? SpanSmall { get; set; }

    /// <summary>Columns to span from 960px up.</summary>
    [Parameter]
    public int? SpanMedium { get; set; }

    /// <summary>Columns to span from 1280px up.</summary>
    [Parameter]
    public int? SpanLarge { get; set; }

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-grid-span", Span.ToString())
            .AddStyle("--veto-grid-span-sm", SpanSmall?.ToString())
            .AddStyle("--veto-grid-span-md", SpanMedium?.ToString())
            .AddStyle("--veto-grid-span-lg", SpanLarge?.ToString());
}
