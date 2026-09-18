namespace Veto.Components.Gantt;

/// <summary>
/// A filter of the app's own, shown as a toggle in the <see cref="VetoGantt"/> toolbar - "Late",
/// "Mine", "Over 80%", anything a predicate can say. While it is on, only rows it keeps are
/// listed, along with the parents they belong to.
/// </summary>
/// <param name="Id">Identifies the filter in <see cref="VetoGantt.ActiveFilters"/>. Must be unique.</param>
/// <param name="Label">The text on its toolbar toggle.</param>
/// <param name="Predicate">Whether a row stays while the filter is on.</param>
/// <example>
/// <code>
/// new VetoGanttFilter("late", "Late", item =&gt; item.End &lt; today &amp;&amp; item.Progress &lt; 1)
/// </code>
/// </example>
public sealed record VetoGanttFilter ( string Id, string Label, Func<VetoGanttItem, bool> Predicate )
{
    /// <summary>SVG markup for an icon before the label, normally a constant from <c>VetoIcons</c>.</summary>
    public string? Icon { get; init; }

    /// <summary>Text shown on hover, for a filter whose label needs explaining.</summary>
    public string? Tooltip { get; init; }
}

/// <summary>
/// Every filter on a <see cref="VetoGantt"/> at one moment, handed to
/// <see cref="VetoGantt.OnFilterChanged"/> whenever any of them changes.
/// </summary>
/// <param name="Search">The text filter, or null.</param>
/// <param name="HiddenCategories">The categories switched off.</param>
/// <param name="ActiveFilters">The ids of the app's own filters that are on.</param>
/// <param name="VisibleItems">The rows the filters leave, parents included, before any folding.</param>
public sealed record VetoGanttFilterState ( string? Search, IReadOnlyCollection<string> HiddenCategories, IReadOnlyCollection<string> ActiveFilters, IReadOnlyList<VetoGanttItem> VisibleItems );