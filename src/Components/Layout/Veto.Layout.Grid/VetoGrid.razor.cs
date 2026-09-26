using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// A responsive CSS grid. Set a column count per breakpoint and the grid picks the right one
/// at each width; children are plain markup or <see cref="VetoGridItem"/> when one needs to
/// span more than a single column.
/// </summary>
/// <remarks>
/// Breakpoints are mobile-first: <see cref="Columns"/> applies at every width, and each of
/// <see cref="ColumnsSmall"/>, <see cref="ColumnsMedium"/> and <see cref="ColumnsLarge"/>
/// takes over from its breakpoint upwards. Counts are passed to CSS as custom properties
/// rather than as classes, so any column count works without the library shipping a class
/// for it.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoGrid Columns="1" ColumnsMedium="2" ColumnsLarge="4" Gap="VetoSpacing.Large"&gt;
///     &lt;VetoCard&gt;One&lt;/VetoCard&gt;
///     &lt;VetoGridItem SpanLarge="2"&gt;&lt;VetoCard&gt;Twice as wide&lt;/VetoCard&gt;&lt;/VetoGridItem&gt;
/// &lt;/VetoGrid&gt;
/// </code>
/// </example>
public partial class VetoGrid : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-grid";

    /// <summary>The cells.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Columns at every width, unless a larger breakpoint overrides it.</summary>
    [Parameter]
    public int Columns { get; set; } = 1;

    /// <summary>Columns from 640px up.</summary>
    [Parameter]
    public int? ColumnsSmall { get; set; }

    /// <summary>Columns from 960px up.</summary>
    [Parameter]
    public int? ColumnsMedium { get; set; }

    /// <summary>Columns from 1280px up.</summary>
    [Parameter]
    public int? ColumnsLarge { get; set; }

    /// <summary>The space between cells, both directions.</summary>
    [Parameter]
    public VetoSpacing Gap { get; set; } = VetoSpacing.Large;

    /// <summary>Alignment of cell content within its row.</summary>
    [Parameter]
    public VetoAlign Align { get; set; } = VetoAlign.Stretch;

    /// <summary>
    /// Lays out as many columns as fit, none narrower than this CSS length (<c>16rem</c>,
    /// <c>240px</c>) - responsive without breakpoints. Overrides <see cref="Columns"/> and its
    /// per-breakpoint variants when set.
    /// </summary>
    [Parameter]
    public string? MinItemWidth { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier("auto"), MinItemWidth is not null)
            .AddClass(Modifier($"gap-{Gap.ToModifier()}"))
            .AddClass(Modifier($"align-{Align.ToModifier()}"));

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            // At least one column: repeat(0, ...) is invalid, and the whole declaration drops.
            .AddStyle("--veto-grid-columns", Count(Columns))
            .AddStyle("--veto-grid-columns-sm", ColumnsSmall is { } sm ? Count(sm) : null)
            .AddStyle("--veto-grid-columns-md", ColumnsMedium is { } md ? Count(md) : null)
            .AddStyle("--veto-grid-columns-lg", ColumnsLarge is { } lg ? Count(lg) : null)
            .AddStyle("--veto-grid-min-item-width", MinItemWidth, MinItemWidth is not null);

    private static string Count ( int value )
        => Math.Max(1, value).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
