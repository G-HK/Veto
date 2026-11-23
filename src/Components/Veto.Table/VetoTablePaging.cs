namespace Veto.Components.Table;

/// <summary>How a <see cref="Table{TItem}"/> splits its items up.</summary>
public enum VetoTablePaging
{
    /// <summary>Every item at once. With AsyncItems, the whole stream is read.</summary>
    None,

    /// <summary>
    /// One page of <c>PageSize</c> rows, with Previous and Next. With AsyncItems each page is read
    /// only when it is asked for, and the page count is known once the stream ends.
    /// </summary>
    Pages,

    /// <summary>
    /// <c>PageSize</c> rows, then the next batch whenever the end of the table scrolls into view
    /// - an infinite feed. A Load more button does the same for the keyboard, or without JavaScript.
    /// </summary>
    Scroll,

    /// <summary>
    /// Every row reachable by scrolling, but only the rows in view are rendered - Blazor's
    /// <c>Virtualize</c>. With Items the list is in memory; with ItemsProvider each visible range
    /// is fetched as it scrolls into view. Give the table a height to scroll within
    /// (<c>RootMaxHeight</c>). Setting ItemsProvider always virtualizes. Rows are not striped:
    /// the rows come and go as you scroll, so alternate shading would flicker.
    /// </summary>
    Virtualize
}